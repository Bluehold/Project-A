using System.Collections;
using UnityEngine;

public class ChickenDragonGrabbable : MonoBehaviour, IGrabbable
{
    [SerializeField]
    private MonsterGrabData GrabData = new MonsterGrabData();

    [SerializeField]
    private Transform GrabPoint;

    [Header("Grab Hitbox")]
    [SerializeField]
    private GameObject GrabHitboxObject;

    [SerializeField]
    private Vector3 GrabHitboxSize = new Vector3(1f, 1f, 1f);

    [SerializeField]
    private Vector3 GrabHitboxOffset = new Vector3(0f, 0.5f, 0.5f);

    private BaseMonster monster;

    private Transform defaultGrabPoint;

    private bool grabInvincible = false;
    private bool isGrabbed = false;

    private void Awake()
    {
        monster = GetComponent<BaseMonster>();

        // Ensure a grab hitbox exists (inactive by default). This hitbox is
        // enabled while the monster is in the Groggy state so the player can
        // detect/validate grab opportunities.
        if (GrabHitboxObject == null)
        {
            GrabHitboxObject = new GameObject("GrabHitbox");
            GrabHitboxObject.transform.SetParent(transform);
            GrabHitboxObject.transform.localPosition = GrabHitboxOffset;

            BoxCollider box = GrabHitboxObject.AddComponent<BoxCollider>();
            box.isTrigger = true;
            box.size = GrabHitboxSize;

            GrabHitboxObject.SetActive(false);
        }

        // Create a default grab point to the right of the hitbox at foot height
        if (GrabPoint == null)
        {
            defaultGrabPoint = new GameObject("DefaultGrabPoint").transform;
            defaultGrabPoint.SetParent(transform);
            // initial placement; Update() will keep the y aligned to feet
            Vector3 rightOffset = Vector3.right * (GrabHitboxSize.x * 0.5f);
            defaultGrabPoint.position = GrabHitboxObject.transform.position + rightOffset;
            defaultGrabPoint.rotation = Quaternion.LookRotation((transform.position - defaultGrabPoint.position).normalized);
        }
    }

    private void Update()
    {
        if (GrabHitboxObject != null && monster != null)
        {
            bool shouldEnable = (monster.CurrentState == MonsterState.Groggy) && !isGrabbed;

            if (GrabHitboxObject.activeSelf != shouldEnable)
            {
                GrabHitboxObject.SetActive(shouldEnable);
            }
        }
    }

    public bool CanGrab()
    {
        if (monster == null) return false;
        return monster.CurrentState == MonsterState.Groggy;
    }

    public void BeginGrab(Transform player)
    {
        if (!CanGrab() || monster == null)
            return;

        isGrabbed = true;
        grabInvincible = true;

        // While grabbed, mark monster externally invincible so other damage
        // sources (including the player's normal attacks) do not apply.
        monster.ExternalInvincible = true;

        monster.SetState(MonsterState.Grabbed);
        monster.StopMovement();
        monster.PlayAnimation(MonsterAnimationType.Grab);
    }

    public FrontGrabData GetGrabData()
    {
        Transform gp = GrabPoint == null ? (defaultGrabPoint ?? transform) : GrabPoint;

        // Ensure grab point Y is aligned to the monster's feet (pivot/Y)
        Vector3 pos = gp.position;
        pos.y = transform.position.y;

        // Ensure rotation faces the monster so the player looks at it when grabbed
        Quaternion rot = gp.rotation;
        rot = Quaternion.LookRotation((transform.position - pos).normalized);

        return new FrontGrabData(
            pos,
            rot,
            FrontGrabAnimationType.Grab
        );
    }

    public Transform GetGrabPoint()
    {
        return GrabPoint == null ? (defaultGrabPoint ?? transform) : GrabPoint;
    }

    public void ExecuteGrabAttack(float damage)
    {
        if (!isGrabbed || monster == null)
            return;

        monster.SetState(MonsterState.Executing);
        monster.TakeDamage(damage);
        monster.PlayAnimation(MonsterAnimationType.Execute);
    }

    public void EndGrab()
    {
        if (!isGrabbed || monster == null)
            return;

        // Release invincibility and start knockdown sequence.
        monster.ExternalInvincible = false;
        grabInvincible = false;
        isGrabbed = false;

        StartCoroutine(KnockDownRoutine());
    }

    public bool IsGrabInvincible()
    {
        return grabInvincible;
    }

    private IEnumerator KnockDownRoutine()
    {
        if (monster == null)
            yield break;

        monster.SetState(MonsterState.KnockBack);
        monster.PlayAnimation(MonsterAnimationType.KnockBack);

        yield return new WaitForSeconds(0.4f);

        monster.SetState(MonsterState.KnockDown);
        monster.PlayAnimation(MonsterAnimationType.KnockDown);

        yield return new WaitForSeconds(GrabData.KnockDownTime);

        monster.SetState(MonsterState.Recover);
        monster.PlayAnimation(MonsterAnimationType.Recover);

        yield return new WaitForSeconds(0.5f);

        monster.ResetGroggy();
        monster.SetState(MonsterState.Normal);
    }
}
