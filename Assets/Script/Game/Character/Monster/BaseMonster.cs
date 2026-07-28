using UnityEngine;

public class BaseMonster : MonoBehaviour, IDamageable, IGrabbable
{
    [Header("Character")]
    [SerializeField]
    private Transform MonsterModel;

    [Header("Status")]
    [SerializeField]
    private float MaxHealth = 100f;

    [SerializeField]
    private float Health = 100f;

    [SerializeField]
    private float MaxGroggy = 100f;

    [SerializeField]
    private float Groggy = 0f;

    [Header("Move")]
    [SerializeField]
    private float MoveSpeed = 3f;

    [Header("Attack")]
    [SerializeField]
    private float AttackDamage = 10f;

    [SerializeField]
    private float AttackInterval = 1f;

    [SerializeField]
    private Vector3 HurtBoxSize = Vector3.one;

    [SerializeField]
    private Vector3 AttackBoxSize = Vector3.one;

    [SerializeField]
    private float AttackForwardOffset = 1f;

    [Header("AI")]
    [SerializeField]
    private BaseMonsterAI MonsterAI;

    [Header("Movement")]
    [SerializeField]
    private BaseMonsterMovement MonsterMovement;

    private float AttackTimer;

    private bool UseRunAnimation;

    private BoxCollider HurtBox;

    [SerializeField]
    private MonsterState State = MonsterState.Normal;

    [SerializeField]
    private MonsterGrabData GrabData = new MonsterGrabData();

    [SerializeField]
    private MonsterAnimationHook AnimationHook;

    [SerializeField]
    private Transform GrabPoint;

    private MonsterEvent Events = new MonsterEvent();

    private bool Invincible;
    private bool GrabInvincible;
    private bool AnimationHookInitialized;
    private bool AiInitialized;
    private bool MovementInitialized;

    public MonsterState CurrentState => State;
    public float MoveSpeedValue => MoveSpeed;
    public bool IsUsingRunAnimation => UseRunAnimation;
    public Transform MonsterModelTransform => MonsterModel;
    public BaseMonsterAI AiComponent => MonsterAI;
    public BaseMonsterMovement MovementComponent => MonsterMovement;
    public bool IsAlive => State != MonsterState.Dead;

    private void Awake()
    {
        CreateHurtBox();
        RefreshDependencies();
    }

    private void Start()
    {
        RefreshDependencies();
    }

    private void RefreshDependencies()
    {
        if (AnimationHook == null)
        {
            AnimationHook = GetComponent<MonsterAnimationHook>();
        }

        if (MonsterAI == null)
        {
            MonsterAI = GetComponent<BaseMonsterAI>();
        }

        if (MonsterMovement == null)
        {
            MonsterMovement = GetComponent<BaseMonsterMovement>();
        }

        if (MonsterMovement == null)
        {
            MonsterMovement = gameObject.AddComponent<MonsterGroundMovement>();
        }

        if (AnimationHook != null && !AnimationHookInitialized)
        {
            AnimationHookInitialized = true;
        }

        if (MonsterAI != null && !AiInitialized)
        {
            MonsterAI.Initialize(this);
            AiInitialized = true;
        }

        if (MonsterMovement != null && !MovementInitialized)
        {
            MonsterMovement.Initialize(this);
            MovementInitialized = true;
        }
    }

    private void Update()
    {
        UpdateAI();
    }

    private void FixedUpdate()
    {
        UpdateMove();
    }

    // AI insert position
    private void UpdateAI()
    {
        RefreshDependencies();

        if (State != MonsterState.Normal)
        {
            MonsterAI?.OnStateChanged(State);
            return;
        }

        MonsterAI?.Tick(this, Time.deltaTime);

        AttackTimer += Time.deltaTime;

        if (AttackTimer >= AttackInterval)
        {
            AttackTimer = 0f;
            Attack();
        }
    }

    // Move insert position
    private void UpdateMove()
    {
        RefreshDependencies();

        if (State != MonsterState.Normal)
        {
            return;
        }

        MonsterMovement?.Tick(Time.fixedDeltaTime);
    }

    public void SetMoveDestination(Vector3 destination, float speedScale = 1f)
    {
        RefreshDependencies();
        MonsterMovement?.MoveTo(destination, speedScale);
    }

    public void StopMovement()
    {
        RefreshDependencies();
        MonsterMovement?.Stop();
    }

    public void PlayAnimation(MonsterAnimationType type)
    {
        RefreshDependencies();
        AnimationHook?.Play(type);
    }

    public void SetMoveSpeed(float speed)
    {
        MoveSpeed = speed;
        RefreshDependencies();
        MonsterMovement?.SetMoveSpeed(speed);
    }

    public void SetRunAnimation(bool useRunAnimation)
    {
        UseRunAnimation = useRunAnimation;
    }

    private void CreateHurtBox()
    {
        HurtBox = GetComponent<BoxCollider>();

        if (HurtBox == null)
        {
            HurtBox = gameObject.AddComponent<BoxCollider>();
        }

        HurtBox.isTrigger = true;
        HurtBox.center = Vector3.zero;
        HurtBox.size = HurtBoxSize;
    }

    private void Attack()
    {
        Vector3 attackCenter = transform.position + transform.forward * AttackForwardOffset;

        Collider[] hits = Physics.OverlapBox(
            attackCenter,
            AttackBoxSize * 0.5f,
            transform.rotation);

        foreach (Collider hit in hits)
        {
            IDamageable target = hit.GetComponentInParent<IDamageable>();

            if (target == null)
                continue;

            MonoBehaviour targetObject = target as MonoBehaviour;

            if (targetObject == null)
                continue;

            if (targetObject.gameObject == gameObject)
                continue;

            target.TakeDamage(AttackDamage);

            Debug.Log($"{name} -> {targetObject.name} Damage : {AttackDamage}");
        }
    }

    public void TakeDamage(float damage)
    {
        if (Invincible)
        {
            return;
        }

        if (GrabInvincible)
        {
            return;
        }

        Health -= damage;

        AddGroggy(damage);

        if (Health < 0f)
        {
            Health = 0f;
        }

        Debug.Log($"{name} HP : {Health}/{MaxHealth}");

        if (Health <= 0f)
        {
            Die();
        }
    }

    private void Die()
    {
        Debug.Log($"{name} Dead");

        State = MonsterState.Dead;

        MonsterAI?.Stop();
        MonsterMovement?.Stop();
        AnimationHook?.Play(MonsterAnimationType.Die);

        Events.OnDead?.Invoke();

        Destroy(gameObject, 2f);
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireCube(
            transform.position,
            HurtBoxSize);

        Gizmos.color = Color.red;

        Vector3 attackCenter = transform.position + transform.forward * AttackForwardOffset;

        Matrix4x4 oldMatrix = Gizmos.matrix;

        Gizmos.matrix = Matrix4x4.TRS(
            attackCenter,
            transform.rotation,
            Vector3.one);

        Gizmos.DrawWireCube(
            Vector3.zero,
            AttackBoxSize);

        Gizmos.matrix = oldMatrix;

        if (GrabPoint != null)
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawSphere(GrabPoint.position, 0.08f);
        }
    }

    private void AddGroggy(float amount)
    {
        if (State != MonsterState.Normal)
        {
            return;
        }

        Groggy += amount;

        if (Groggy >= MaxGroggy)
        {
            Groggy = MaxGroggy;
            EnterGroggy();
        }
    }

    private void EnterGroggy()
    {
        State = MonsterState.Groggy;

        AnimationHook?.Play(MonsterAnimationType.Groggy);

        Events.OnGroggy?.Invoke();
    }

    public bool CanGrab()
    {
        return State == MonsterState.Groggy;
    }

    public Transform GetGrabPoint()
    {
        return GrabPoint == null ? transform : GrabPoint;
    }

    public bool IsGrabInvincible()
    {
        return GrabInvincible;
    }

    public void BeginGrab(Transform player)
    {
        if (!CanGrab())
        {
            return;
        }

        State = MonsterState.Grabbed;
        GrabInvincible = true;

        AnimationHook?.Play(MonsterAnimationType.Grab);

        Events.OnGrabBegin?.Invoke();
    }

    public void ExecuteGrabAttack(float damage)
    {
        if (State != MonsterState.Grabbed)
        {
            return;
        }

        State = MonsterState.Executing;

        Health -= damage;

        if (Health < 0)
        {
            Health = 0;
        }

        if (Health <= 0f)
        {
            Die();
        }

        AnimationHook?.Play(MonsterAnimationType.Execute);

        Events.OnGrabExecute?.Invoke();
    }

    public void EndGrab()
    {
        GrabInvincible = false;

        StartCoroutine(KnockDownRoutine());

        Events.OnGrabEnd?.Invoke();
    }

    private System.Collections.IEnumerator KnockDownRoutine()
    {
        State = MonsterState.KnockBack;

        AnimationHook?.Play(MonsterAnimationType.KnockBack);

        yield return new WaitForSeconds(0.4f);

        State = MonsterState.KnockDown;

        AnimationHook?.Play(MonsterAnimationType.KnockDown);

        yield return new WaitForSeconds(GrabData.KnockDownTime);

        State = MonsterState.Recover;

        AnimationHook?.Play(MonsterAnimationType.Recover);

        yield return new WaitForSeconds(0.5f);

        Groggy = 0f;
        State = MonsterState.Normal;
    }

    public FrontGrabData GetGrabData()
    {
        Transform grabPoint = GetGrabPoint();

        return new FrontGrabData(
            grabPoint.position,
            grabPoint.rotation,
            FrontGrabAnimationType.Grab
        );
    }
}

