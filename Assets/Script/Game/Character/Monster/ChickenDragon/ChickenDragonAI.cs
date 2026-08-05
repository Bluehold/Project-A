#if UNITY_EDITOR
using System;
using UnityEngine;

public class ChickenDragonAI : BaseMonsterAI
{
    private BehaviorGraph graph;

    public float DetectionRange = 8f;
    public float AttackRange = 2f;
    public LayerMask TargetMask = -1;

    private BTBlackboard blackboard = new BTBlackboard();

    public override void Initialize(BaseMonster monster)
    {
        base.Initialize(monster);

        // Build behavior graph nodes
        // Detect node: finds nearest target and stores in blackboard
        var detect = new BGActionNode((m, bb, dt) =>
        {
            if (m == null) return BGNodeState.Failure;
            Collider[] hits = Physics.OverlapSphere(m.transform.position, DetectionRange, TargetMask);
            Transform nearest = null;
            float nearestDist = float.MaxValue;
            foreach (var h in hits)
            {
                if (h == null) continue;
                if (h.transform == m.transform) continue;
                float d = Vector3.Distance(m.transform.position, h.transform.position);
                if (d < nearestDist)
                {
                    nearestDist = d;
                    nearest = h.transform;
                }
            }

            if (nearest != null)
            {
                bb.Set("target", nearest);
                return BGNodeState.Success;
            }

            bb.Remove("target");
            return BGNodeState.Failure;
        });

        // MoveTo node: move toward target until within attack range
        var moveTo = new BGActionNode((m, bb, dt) =>
        {
            var target = bb.Get<Transform>("target");
            if (target == null) return BGNodeState.Failure;
            float dist = Vector3.Distance(m.transform.position, target.position);
            if (dist <= AttackRange)
            {
                m.StopMovement();
                return BGNodeState.Success;
            }
            m.SetMoveDestination(target.position);
            return BGNodeState.Running;
        }, (m, bb) => { m.SetRunAnimation(true); }, null);

        // Pattern node: trigger random pattern (delegates to existing BTTaskRandomPattern logic via BTTaskDoPattern equivalent)
        var pattern = new BGActionNode((m, bb, dt) =>
        {
            var target = bb.Get<Transform>("target");
            // pick a random pattern and execute via ChickenDragonCombat
            var combat = m.GetComponent<ChickenDragonCombat>();
            var patterns = Enum.GetValues(typeof(ChickenDragonPattern));
            var pick = (ChickenDragonPattern)patterns.GetValue(UnityEngine.Random.Range(0, patterns.Length));

            // reuse DoPattern logic from BTTaskDoPattern but inline here
            var gr = m.GetComponent<ChickenDragonCombat>();
            switch (pick)
            {
                case ChickenDragonPattern.Charge:
                    if (gr != null && target != null && gr.CanCharge(target)) gr.DoCharge(target, 2f);
                    else if (target != null) m.SetMoveDestination(target.position, 2f);
                    break;
                case ChickenDragonPattern.Beak:
                    if (gr != null && gr.CanBeak(target)) gr.DoBeakAttack();
                    else m.PlayAnimation(MonsterAnimationType.Attack);
                    break;
                case ChickenDragonPattern.Sweep:
                    if (gr != null && gr.CanSweep(target)) gr.DoBodySweep();
                    else m.PlayAnimation(MonsterAnimationType.Attack);
                    break;
                case ChickenDragonPattern.Roar:
                    if (gr != null && gr.CanRoar(target)) gr.DoRoar();
                    else m.PlayAnimation(MonsterAnimationType.Groggy);
                    break;
                case ChickenDragonPattern.Retreat:
                    if (gr != null && target != null && gr.CanRetreat(target))
                    {
                        Vector3 dir = m.transform.position - target.position; dir.y = 0f;
                        if (dir.sqrMagnitude < 0.001f) dir = -m.transform.forward;
                        Vector3 fallback = m.transform.position + dir.normalized * 3f;
                        gr.DoRetreatAndReposition(fallback, 1.5f);
                    }
                    else if (target != null)
                    {
                        Vector3 dir = m.transform.position - target.position; dir.y = 0f;
                        if (dir.sqrMagnitude < 0.001f) dir = -m.transform.forward;
                        Vector3 fallback = m.transform.position + dir.normalized * 3f;
                        m.SetMoveDestination(fallback, 1.5f);
                    }
                    break;
            }

            return BGNodeState.Success;
        }, (m, bb) => { m.StopMovement(); });

        // Wander node
        var wander = new BGActionNode((m, bb, dt) =>
        {
            // simple wander trigger: set random destination once
            if (bb.Get<Transform>("wanderSet") == null)
            {
                Vector3 point = m.transform.position + UnityEngine.Random.insideUnitSphere * 6f; point.y = m.transform.position.y;
                m.SetMoveDestination(point);
                bb.Set("wanderSet", m.transform);
                return BGNodeState.Running;
            }

            // if reached, clear flag
            // approximate by checking agent speed via MovementComponent
            if (!m.MovementComponent.IsMovingOrBraking())
            {
                bb.Remove("wanderSet");
                return BGNodeState.Success;
            }

            return BGNodeState.Running;
        });

        // transitions
        detect.AddTransition((m, bb) => bb.Get<Transform>("target") != null, moveTo);
        moveTo.AddTransition((m, bb) => bb.Get<Transform>("target") == null, wander);
        moveTo.AddTransition((m, bb) => Vector3.Distance(m.transform.position, bb.Get<Transform>("target")?.position ?? m.transform.position) <= AttackRange, pattern);
        pattern.AddTransition((m, bb) => true, detect);
        wander.AddTransition((m, bb) => true, detect);

        graph = new BehaviorGraph(detect, blackboard);
        graph.Initialize(monster);
    }

    public override void Tick(BaseMonster monster, float deltaTime)
    {
        base.Tick(monster, deltaTime);

        graph?.Tick(deltaTime);
    }

    public override void Stop()
    {
        base.Stop();
        // nothing special
    }
}
#endif
