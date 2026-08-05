using UnityEngine;

// Component that exposes ChickenDragon-specific attack patterns.
[DisallowMultipleComponent]
public class ChickenDragonCombat : MonoBehaviour
{
    private BaseMonster monster;

    [Header("Pattern Ranges")]
    [SerializeField]
    private float ChargeMinDistance = 3f;

    [SerializeField]
    private float ChargeMaxDistance = 12f;

    [SerializeField]
    private float BeakRange = 2f;

    [SerializeField]
    private float SweepRange = 2.5f;

    [SerializeField]
    private float RoarRange = 6f;

    private void Awake()
    {
        monster = GetComponent<BaseMonster>();
    }

    public void DoCharge(Transform target, float speedScale = 2f)
    {
        if (monster == null || target == null) return;

        monster.SetRunAnimation(true);
        monster.SetMoveDestination(target.position, speedScale);
    }

    public void DoBeakAttack()
    {
        if (monster == null) return;

        monster.StopMovement();
        monster.PlayAnimation(MonsterAnimationType.Attack);
    }

    public void DoBodySweep()
    {
        if (monster == null) return;

        monster.StopMovement();
        monster.PlayAnimation(MonsterAnimationType.Attack);
    }

    public void DoRoar()
    {
        if (monster == null) return;

        monster.StopMovement();
        monster.PlayAnimation(MonsterAnimationType.Groggy);
    }

    public void DoRetreatAndReposition(Vector3 fallbackPoint, float speedScale = 1f)
    {
        if (monster == null) return;

        monster.SetRunAnimation(true);
        monster.SetMoveDestination(fallbackPoint, speedScale);
    }

    // Capability checks for patterns
    public bool CanCharge(Transform target)
    {
        if (monster == null || target == null) return false;
        float d = Vector3.Distance(monster.transform.position, target.position);
        return d >= ChargeMinDistance && d <= ChargeMaxDistance;
    }

    public bool CanBeak(Transform target)
    {
        if (monster == null || target == null) return false;
        float d = Vector3.Distance(monster.transform.position, target.position);
        return d <= BeakRange;
    }

    public bool CanSweep(Transform target)
    {
        if (monster == null || target == null) return false;
        float d = Vector3.Distance(monster.transform.position, target.position);
        return d <= SweepRange;
    }

    public bool CanRoar(Transform target)
    {
        if (monster == null) return false;
        if (target == null) return true; // can roar without a specific target
        float d = Vector3.Distance(monster.transform.position, target.position);
        return d <= RoarRange;
    }

    public bool CanRetreat(Transform target)
    {
        // retreat is always possible
        return monster != null;
    }
}
