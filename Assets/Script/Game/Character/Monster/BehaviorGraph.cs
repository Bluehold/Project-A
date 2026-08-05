#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEngine;

// Simple behavior graph runtime: nodes with transitions (predicates).
public enum BGNodeState { Inactive, Running, Success, Failure }

public class BGTransition
{
    public Func<BaseMonster, BTBlackboard, bool> Condition;
    public BGNode Target;

    public BGTransition(Func<BaseMonster, BTBlackboard, bool> condition, BGNode target)
    {
        Condition = condition;
        Target = target;
    }
}

public abstract class BGNode
{
    public List<BGTransition> Transitions = new List<BGTransition>();

    public virtual void Enter(BaseMonster monster, BTBlackboard blackboard) { }
    public abstract BGNodeState Tick(BaseMonster monster, BTBlackboard blackboard, float deltaTime);
    public virtual void Exit(BaseMonster monster, BTBlackboard blackboard) { }

    public void AddTransition(Func<BaseMonster, BTBlackboard, bool> condition, BGNode target)
    {
        Transitions.Add(new BGTransition(condition, target));
    }
}

public class BGActionNode : BGNode
{
    private Action<BaseMonster, BTBlackboard> onEnter;
    private Func<BaseMonster, BTBlackboard, float, BGNodeState> onTick;
    private Action<BaseMonster, BTBlackboard> onExit;

    public BGActionNode(Func<BaseMonster, BTBlackboard, float, BGNodeState> onTick,
        Action<BaseMonster, BTBlackboard> onEnter = null,
        Action<BaseMonster, BTBlackboard> onExit = null)
    {
        this.onTick = onTick;
        this.onEnter = onEnter;
        this.onExit = onExit;
    }

    public override void Enter(BaseMonster monster, BTBlackboard blackboard)
    {
        onEnter?.Invoke(monster, blackboard);
    }

    public override BGNodeState Tick(BaseMonster monster, BTBlackboard blackboard, float deltaTime)
    {
        if (onTick == null) return BGNodeState.Failure;
        return onTick(monster, blackboard, deltaTime);
    }

    public override void Exit(BaseMonster monster, BTBlackboard blackboard)
    {
        onExit?.Invoke(monster, blackboard);
    }
}

public class BehaviorGraph
{
    private BGNode start;
    private BGNode current;
    private BaseMonster monster;
    private BTBlackboard blackboard;

    public BehaviorGraph(BGNode start, BTBlackboard blackboard)
    {
        this.start = start;
        this.blackboard = blackboard;
    }

    public void Initialize(BaseMonster monster)
    {
        this.monster = monster;
        current = start;
        current?.Enter(monster, blackboard);
    }

    public void Tick(float deltaTime)
    {
        if (current == null) return;

        BGNodeState state = current.Tick(monster, blackboard, deltaTime);

        // evaluate transitions in order
        foreach (var t in current.Transitions)
        {
            try
            {
                if (t.Condition(monster, blackboard))
                {
                    current.Exit(monster, blackboard);
                    current = t.Target;
                    current.Enter(monster, blackboard);
                    break;
                }
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
            }
        }
    }
}
#endif
