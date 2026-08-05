using System.Collections.Generic;
using UnityEngine;

public enum BTNodeState
{
    Success,
    Failure,
    Running
}

public class BTBlackboard
{
    private Dictionary<string, object> data = new Dictionary<string, object>();

    public void Set(string key, object value)
    {
        if (data.ContainsKey(key)) data[key] = value;
        else data.Add(key, value);
    }

    public T Get<T>(string key) where T : class
    {
        if (data.TryGetValue(key, out object val))
        {
            return val as T;
        }

        return null;
    }

    public void Remove(string key)
    {
        if (data.ContainsKey(key)) data.Remove(key);
    }
}

public abstract class BTNode
{
    protected BaseMonster Monster;
    protected BTBlackboard Blackboard;

    public virtual void Initialize(BaseMonster monster, BTBlackboard blackboard)
    {
        Monster = monster;
        Blackboard = blackboard;
    }

    public abstract BTNodeState Tick(float deltaTime);
}

public abstract class BTComposite : BTNode
{
    protected List<BTNode> children = new List<BTNode>();

    public BTComposite(params BTNode[] nodes)
    {
        children.AddRange(nodes);
    }

    public override void Initialize(BaseMonster monster, BTBlackboard blackboard)
    {
        base.Initialize(monster, blackboard);
        foreach (var c in children)
        {
            c.Initialize(monster, blackboard);
        }
    }
}

public class BTSequence : BTComposite
{
    private int current = 0;

    public BTSequence(params BTNode[] nodes) : base(nodes) { }

    public override BTNodeState Tick(float deltaTime)
    {
        while (current < children.Count)
        {
            var state = children[current].Tick(deltaTime);

            if (state == BTNodeState.Running)
                return BTNodeState.Running;

            if (state == BTNodeState.Failure)
            {
                current = 0;
                return BTNodeState.Failure;
            }

            current++;
        }

        current = 0;
        return BTNodeState.Success;
    }
}

public class BTSelector : BTComposite
{
    private int current = 0;

    public BTSelector(params BTNode[] nodes) : base(nodes) { }

    public override BTNodeState Tick(float deltaTime)
    {
        while (current < children.Count)
        {
            var state = children[current].Tick(deltaTime);

            if (state == BTNodeState.Running)
                return BTNodeState.Running;

            if (state == BTNodeState.Success)
            {
                current = 0;
                return BTNodeState.Success;
            }

            current++;
        }

        current = 0;
        return BTNodeState.Failure;
    }
}

public abstract class BTTask : BTNode
{
}

public class BTTaskMoveTo : BTTask
{
    private string targetKey;
    private float stopDistance;
    private float speedScale;

    public BTTaskMoveTo(string targetKey = "target", float stopDistance = 1f, float speedScale = 1f)
    {
        this.targetKey = targetKey;
        this.stopDistance = stopDistance;
        this.speedScale = speedScale;
    }

    public override BTNodeState Tick(float deltaTime)
    {
        if (Monster == null || Blackboard == null)
            return BTNodeState.Failure;

        Transform target = Blackboard.Get<Transform>(targetKey);

        if (target == null)
        {
            Monster.StopMovement();
            return BTNodeState.Failure;
        }

        float dist = Vector3.Distance(Monster.transform.position, target.position);

        if (dist <= stopDistance)
        {
            Monster.StopMovement();
            return BTNodeState.Success;
        }

        Monster.SetMoveDestination(target.position, speedScale);
        return BTNodeState.Running;
    }
}

public class BTTaskWait : BTTask
{
    private float waitTime;
    private float timer;

    public BTTaskWait(float seconds)
    {
        waitTime = seconds;
        timer = 0f;
    }

    public override void Initialize(BaseMonster monster, BTBlackboard blackboard)
    {
        base.Initialize(monster, blackboard);
        timer = 0f;
    }

    public override BTNodeState Tick(float deltaTime)
    {
        timer += deltaTime;

        if (timer >= waitTime)
        {
            timer = 0f;
            return BTNodeState.Success;
        }

        return BTNodeState.Running;
    }
}

public class BTTaskAttack : BTTask
{
    private string targetKey;
    private float attackRange;
    private float attackDuration;
    private float timer;
    private bool started;

    public BTTaskAttack(string targetKey = "target", float attackRange = 1.5f, float attackDuration = 0.6f)
    {
        this.targetKey = targetKey;
        this.attackRange = attackRange;
        this.attackDuration = attackDuration;
    }

    public override void Initialize(BaseMonster monster, BTBlackboard blackboard)
    {
        base.Initialize(monster, blackboard);
        timer = 0f;
        started = false;
    }

    public override BTNodeState Tick(float deltaTime)
    {
        if (Monster == null || Blackboard == null)
            return BTNodeState.Failure;

        Transform target = Blackboard.Get<Transform>(targetKey);

        if (target == null)
            return BTNodeState.Failure;

        float dist = Vector3.Distance(Monster.transform.position, target.position);

        if (dist > attackRange)
            return BTNodeState.Failure;

        if (!started)
        {
            Monster.StopMovement();
            Monster.PlayAnimation(MonsterAnimationType.Attack);
            started = true;
            timer = 0f;
        }

        timer += deltaTime;

        if (timer >= attackDuration)
        {
            started = false;
            timer = 0f;
            return BTNodeState.Success;
        }

        return BTNodeState.Running;
    }
}

public class BTTaskDetectTarget : BTTask
{
    private string targetKey;
    private float detectionRange;
    private LayerMask mask;

    public BTTaskDetectTarget(string targetKey, float detectionRange, LayerMask mask)
    {
        this.targetKey = targetKey;
        this.detectionRange = detectionRange;
        this.mask = mask;
    }

    public override BTNodeState Tick(float deltaTime)
    {
        if (Monster == null)
            return BTNodeState.Failure;

        Collider[] hits = Physics.OverlapSphere(Monster.transform.position, detectionRange, mask);

        Transform nearest = null;
        float nearestDist = float.MaxValue;

        foreach (var h in hits)
        {
            if (h.transform == Monster.transform) continue;
            float d = Vector3.Distance(Monster.transform.position, h.transform.position);
            if (d < nearestDist)
            {
                nearest = h.transform;
                nearestDist = d;
            }
        }

        if (nearest != null)
        {
            Blackboard.Set(targetKey, nearest);
            return BTNodeState.Success;
        }

        Blackboard.Remove(targetKey);
        return BTNodeState.Failure;
    }
}

public class BTTaskWander : BTTask
{
    private float radius;
    private float interval;
    private float timer;

    public BTTaskWander(float radius = 6f, float interval = 4f)
    {
        this.radius = radius;
        this.interval = interval;
        timer = 0f;
    }

    public override void Initialize(BaseMonster monster, BTBlackboard blackboard)
    {
        base.Initialize(monster, blackboard);
        timer = Random.Range(0f, interval);
    }

    public override BTNodeState Tick(float deltaTime)
    {
        if (Monster == null)
            return BTNodeState.Failure;

        timer += deltaTime;

        if (timer >= interval)
        {
            timer = 0f;
            Vector3 point = Monster.transform.position + Random.insideUnitSphere * radius;
            point.y = Monster.transform.position.y;
            Monster.SetMoveDestination(point);
            return BTNodeState.Success;
        }

        return BTNodeState.Running;
    }
}

public enum ChickenDragonPattern
{
    Charge,
    Beak,
    Sweep,
    Roar,
    Retreat
}

public class BTTaskDoPattern : BTTask
{
    private ChickenDragonPattern pattern;
    private string targetKey;
    private float duration;
    private float timer;
    private bool started;

    public BTTaskDoPattern(ChickenDragonPattern pattern, string targetKey = "target", float duration = 1f)
    {
        this.pattern = pattern;
        this.targetKey = targetKey;
        this.duration = duration;
        timer = 0f;
        started = false;
    }

    public override void Initialize(BaseMonster monster, BTBlackboard blackboard)
    {
        base.Initialize(monster, blackboard);
        timer = 0f;
        started = false;
    }

    public override BTNodeState Tick(float deltaTime)
    {
        if (Monster == null || Blackboard == null)
            return BTNodeState.Failure;

        if (!started)
        {
            started = true;
            // Attempt to find the combat component
            var combat = Monster.GetComponent<ChickenDragonCombat>();
            Transform target = Blackboard.Get<Transform>(targetKey);

            switch (pattern)
            {
                case ChickenDragonPattern.Charge:
                    if (combat != null && target != null)
                    {
                        if (!combat.CanCharge(target)) return BTNodeState.Failure;
                        combat.DoCharge(target, 2f);
                    }
                    else if (target != null)
                    {
                        Monster.SetMoveDestination(target.position, 2f);
                    }
                    break;
                case ChickenDragonPattern.Beak:
                    if (combat != null)
                    {
                        if (!combat.CanBeak(target)) return BTNodeState.Failure;
                        combat.DoBeakAttack();
                    }
                    else Monster.PlayAnimation(MonsterAnimationType.Attack);
                    break;
                case ChickenDragonPattern.Sweep:
                    if (combat != null)
                    {
                        if (!combat.CanSweep(target)) return BTNodeState.Failure;
                        combat.DoBodySweep();
                    }
                    else Monster.PlayAnimation(MonsterAnimationType.Attack);
                    break;
                case ChickenDragonPattern.Roar:
                    if (combat != null)
                    {
                        if (!combat.CanRoar(target)) return BTNodeState.Failure;
                        combat.DoRoar();
                    }
                    else Monster.PlayAnimation(MonsterAnimationType.Groggy);
                    break;
                case ChickenDragonPattern.Retreat:
                    if (combat != null && target != null)
                    {
                        if (!combat.CanRetreat(target)) return BTNodeState.Failure;
                        Vector3 dir = Monster.transform.position - target.position;
                        dir.y = 0f;
                        if (dir.sqrMagnitude < 0.001f) dir = -Monster.transform.forward;
                        Vector3 fallback = Monster.transform.position + dir.normalized * 3f;
                        combat.DoRetreatAndReposition(fallback, 1.5f);
                    }
                    else if (target != null)
                    {
                        Vector3 dir = Monster.transform.position - target.position;
                        dir.y = 0f;
                        if (dir.sqrMagnitude < 0.001f) dir = -Monster.transform.forward;
                        Vector3 fallback = Monster.transform.position + dir.normalized * 3f;
                        Monster.SetMoveDestination(fallback, 1.5f);
                    }
                    break;
            }
        }

        timer += deltaTime;

        if (timer >= duration)
        {
            timer = 0f;
            started = false;
            return BTNodeState.Success;
        }

        return BTNodeState.Running;
    }
}

public class BTTaskRandomPattern : BTTask
{
    private string targetKey;
    private float duration;
    private float timer;
    private bool started;

    public BTTaskRandomPattern(string targetKey = "target", float duration = 1f)
    {
        this.targetKey = targetKey;
        this.duration = duration;
        timer = 0f;
        started = false;
    }

    public override void Initialize(BaseMonster monster, BTBlackboard blackboard)
    {
        base.Initialize(monster, blackboard);
        timer = 0f;
        started = false;
    }

    public override BTNodeState Tick(float deltaTime)
    {
        if (Monster == null || Blackboard == null)
            return BTNodeState.Failure;

        if (!started)
        {
            started = true;
            // pick random pattern
            var patterns = System.Enum.GetValues(typeof(ChickenDragonPattern));
            var pick = (ChickenDragonPattern)patterns.GetValue(Random.Range(0, patterns.Length));

            var doPattern = new BTTaskDoPattern(pick, targetKey, duration);
            doPattern.Initialize(Monster, Blackboard);
            // immediately tick once to start
            doPattern.Tick(0f);

            // store in blackboard for potential debugging (optional)
            Blackboard.Set("lastPattern", pick);
        }

        timer += deltaTime;

        if (timer >= duration)
        {
            timer = 0f;
            started = false;
            return BTNodeState.Success;
        }

        return BTNodeState.Running;
    }
}

public class BehaviorTree
{
    private BTNode root;
    private BTBlackboard blackboard = new BTBlackboard();
    private BaseMonster monster;

    public BehaviorTree(BTNode root)
    {
        this.root = root;
    }

    public void Initialize(BaseMonster monster)
    {
        this.monster = monster;
        root.Initialize(monster, blackboard);
    }

    public void Tick(float deltaTime)
    {
        if (root == null) return;
        root.Tick(deltaTime);
    }

    public BTBlackboard Blackboard => blackboard;
}
