using UnityEngine;

public class MonsterAIComponent : BaseMonsterAI
{
    [Header("Target")]
    [SerializeField]
    private Transform Target;

    [Header("Detection")]
    [SerializeField]
    private float DetectionRange = 8f;

    [SerializeField]
    private float AttackRange = 2f;

    [SerializeField]
    private LayerMask TargetMask = -1;

    public void SetDetectionRange(float range)
    {
        DetectionRange = range;
    }

    public void SetAttackRange(float range)
    {
        AttackRange = range;
    }

    public void SetTargetMask(LayerMask mask)
    {
        TargetMask = mask;
    }

    public override void Tick(BaseMonster monster, float deltaTime)
    {
        if (monster == null)
        {
            return;
        }

        if (Target == null)
        {
            Target = FindNearestTarget();
        }

        if (Target == null)
        {
            return;
        }

        float distance = Vector3.Distance(monster.transform.position, Target.position);

        if (distance <= AttackRange)
        {
            monster.StopMovement();
            return;
        }

        if (distance <= DetectionRange)
        {
            monster.SetMoveDestination(Target.position);
        }
    }

    private Transform FindNearestTarget()
    {
        Collider[] hits = Physics.OverlapSphere(transform.position, DetectionRange, TargetMask);

        Transform nearest = null;
        float nearestDistance = float.MaxValue;

        foreach (Collider hit in hits)
        {
            if (hit.transform == transform)
            {
                continue;
            }

            float distance = Vector3.Distance(transform.position, hit.transform.position);
            if (distance < nearestDistance)
            {
                nearest = hit.transform;
                nearestDistance = distance;
            }
        }

        return nearest;
    }
}
