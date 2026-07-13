using System;
using UnityEngine;

public class BasePlayer : MonoBehaviour, IDamageable
{
    [SerializeField]
    private bool IsAlive;
    public enum PlayerState
    {
        Idle,
        Move,
        Roll
    }

    public enum PlayerAttackState
    {
        Idle,
        Attack,
        Skill1,
        Skill2
    }

    private PlayerState CurrentState;
    private PlayerAttackState CurrentAttackState;

    [SerializeField]
    private float MaxHealth;
    [SerializeField]
    private float Health;

    [SerializeField]
    private float MaxMana;
    [SerializeField]
    private float Mana;
    [SerializeField]
    private float MaxStat1;
    [SerializeField]
    private float Stat1;
    [SerializeField]
    private float MaxStat2;
    [SerializeField]
    private float Stat2;

    [SerializeField]
    private float MaxStamina;
    [SerializeField]
    private float Stamina;

    private float StaminaChargeRemainingTime;
    [SerializeField]
    private float StaminaChargeAmount;

    [SerializeField]
    private MainCamera _mainCamera;
    [SerializeField]
    private Transform PlayerModel;
    [SerializeField]
    private PlayerAnimator _playerAnimator;

    [SerializeField]
    private float WalkSpeed;
    [SerializeField]
    private float SprintSpeed;
    [SerializeField]
    private float SprintRequiredStamina;
    private bool DidSprintEnd;
    private Vector2 MoveInput;
    [SerializeField]
    private float RotateSpeed;

    private bool IsSprinting;
    private bool IsRollRequested;

    [SerializeField]
    private float RollRequiredStamina;
    [SerializeField]
    private float RollStaminaChargeDelay;
    [SerializeField]
    private float RollDistance = 0f;
    private float RollDuration = 0.35f;
    private float RollTime;
    private Vector3 RollStartPosition;
    private Quaternion RollStartRotation;
    private Rigidbody rb;

    [SerializeField]
    private float AttackDamage;
    [SerializeField]
    private PlayerAttackTrigger AttackCollider;
    private bool IsAttackRequested;
    [SerializeField]
    private float AttackDuration;
    private float AttackTime;

    private bool IsSkill1Requested;
    [SerializeField]
    private float Skill1RequiredPoint;
    private bool IsSkill2Requested;
    [SerializeField]
    private float Skill2RequiredPoint;

    private void OnHealthChange(float health, float maxHealth) => BarUIController.Instance.SetHealthBar(health, maxHealth);
    private void OnManaChange(float mana, float maxMana) => BarUIController.Instance.SetManaBar(mana, maxMana);
    private void OnStaminaChange(float stamina, float maxStamina) => BarUIController.Instance.SetStaminaBar(stamina, maxStamina);

    private void OnHealthDrop(float health, float maxHealth) => BarUIController.Instance.DrainHealthBar(health, maxHealth);
    private void OnManaDrop(float mana, float maxMana) => BarUIController.Instance.DrainManaBar(mana, maxMana);
    private void OnStaminaDrop(float stamina, float maxStamina) => BarUIController.Instance.DrainStaminaBar(stamina, maxStamina);

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    private void Start()
    {
        IsAlive = true;
        OnHealthChange(Health, MaxHealth);
        OnManaChange(Mana, MaxMana);
        OnStaminaChange(Stamina, MaxStamina);

        if (AttackCollider != null)
        {
            AttackCollider.OnAttackTriggerEnter += ProcessAttack;
            AttackCollider.gameObject.SetActive(false);
        }
    }

    private void Update()
    {

        SetMoveInput();

        IsSprinting = InputManager.Instance.IsSprinting;

        if (InputManager.Instance.ConsumeSprint())
            IsRollRequested = true;

        if (InputManager.Instance.ConsumeAttack())
            IsAttackRequested = true;

        if (InputManager.Instance.ConsumeSkill1())
            IsSkill1Requested = true;
        if (InputManager.Instance.ConsumeSkill2())
            IsSkill2Requested = true;
    }

    private void SetMoveInput()
    {
        MoveInput = InputManager.Instance.MoveInput;
        
        if (_mainCamera != null)
        {
            Vector3 dir3 =
                Quaternion.Euler(0, _mainCamera.GetRotationY(), 0)
                * new Vector3(MoveInput.x, 0, MoveInput.y);

            MoveInput = new Vector2(dir3.x, dir3.z);
        }
    }

    private void FixedUpdate()
    {
        if (IsAlive == false) return;

        HandleRotation();
        HandleMovement();
        HandleRoll();
        HandleAttack();
        HandleSkill();

        UpdateRoll();
        UpdateAttack();
        UpdateStamina();
    }

    public void HandleMovement()
    {
        if (CurrentState == PlayerState.Roll)
            return;

        if (MoveInput.sqrMagnitude < 0.001f)
        {
            SetIdleVelocity();
            return;
        }

        float speed = HandleMoveSpeed();

        SetMoveVelocity(speed);
    }

    private void SetMoveVelocity(float speed)
    {
        Vector3 vel = rb.linearVelocity;

        vel.x = MoveInput.x * speed;
        vel.z = MoveInput.y * speed;

        rb.linearVelocity = vel;
    }

    private void SetIdleVelocity()
    {
        if (CurrentState == PlayerState.Roll)
            return;

        IsRollRequested = false;
        CurrentState = PlayerState.Idle;
        _playerAnimator.SetMovingState(PlayerAnimator.MovingState.Idle);

        Vector3 vel = rb.linearVelocity;

        vel.x *= 0.7f;
        vel.z *= 0.7f;

        rb.linearVelocity = vel;
    }

    private float HandleMoveSpeed()
    {
        float speed;
        if (IsSprinting == true)
        {
            if (DidSprintEnd == false)
            {
                speed = Sprint();
            }
            else
            {
                speed = Walk();
            }
        }
        else
        {
            speed = Walk();
            DidSprintEnd = false;
        }

        return speed;
    }

    private float Walk()
    {
        _playerAnimator.SetMovingState(PlayerAnimator.MovingState.Walk);
        CurrentState = PlayerState.Move;

        return WalkSpeed;
    }

    private float Sprint()
    {
        _playerAnimator.SetMovingState(PlayerAnimator.MovingState.Sprint);
        CurrentState = PlayerState.Move;

        float usedStamina = Time.fixedDeltaTime * SprintRequiredStamina;
        Stamina -= usedStamina;
        OnStaminaDrop(usedStamina, MaxStamina);
        if (Stamina < 0f)
        {
            Stamina = 0f;
            DidSprintEnd = true;
        }

        return SprintSpeed;
    }

    private void HandleRotation()
    {
        if (MoveInput.sqrMagnitude < 0.001f)
            return;

        Vector2 RotateDir = MoveInput;

        Vector3 dir = new Vector3(
            RotateDir.x,
            0,
            RotateDir.y
        );

        Quaternion rot = Quaternion.LookRotation(dir);

        PlayerModel.rotation = Quaternion.Slerp(
            PlayerModel.rotation,
            rot,
            Time.fixedDeltaTime * RotateSpeed
        );
    }

    private void UpdateStamina()
    {
        if (StaminaChargeRemainingTime > 0f)
        {
            StaminaChargeRemainingTime -= Time.fixedDeltaTime;
            if (StaminaChargeRemainingTime < 0f)
                StaminaChargeRemainingTime = 0f;

            return;
        }

        if (CurrentState == PlayerState.Roll)
            return;

        if (CurrentAttackState != PlayerAttackState.Idle)
            return;

        if (IsSprinting)
        {
            if (DidSprintEnd == false)
                return;
        }

        float healStamina = Time.fixedDeltaTime * StaminaChargeAmount;

        Stamina += healStamina;
        if (Stamina > MaxStamina)
            Stamina = MaxStamina;

        OnStaminaChange(Stamina, MaxStamina);
    }

    private void HandleRoll()
    {
        if (IsRollRequested == false)
            return;

        IsRollRequested = false;

        if (CurrentState == PlayerState.Roll)
            return;

        if (CurrentAttackState != PlayerAttackState.Idle)
            return;

        if (UseRollStamina() == true)
        {
            StartRoll();
        }
    }

    private bool UseRollStamina()
    {
        if (Stamina < RollRequiredStamina)
            return false;

        Stamina -= RollRequiredStamina;
        StaminaChargeRemainingTime = RollStaminaChargeDelay;

        OnStaminaDrop(RollRequiredStamina, MaxStamina);

        return true;
    }

    private void StartRoll()
    {
        RollStartPosition = rb.position;
        RollStartRotation = PlayerModel.rotation;

        _playerAnimator.TriggerRoll();

        RollTime = 0f;

        CurrentState = PlayerState.Roll;
    }

    private void UpdateRoll()
    {
        if (CurrentState != PlayerState.Roll)
            return;

        RollTime += Time.fixedDeltaTime;
        float t = Mathf.Clamp01(RollTime / RollDuration);
        float dist = RollDistance * (1f - Mathf.Pow(1f - t, 3));

        Vector3 targetPos = RollStartPosition + RollStartRotation * Vector3.forward * dist;
        targetPos.y = rb.position.y;
        rb.MovePosition(targetPos);
        if (t == 1f)
        {
            EndRoll();
        }
    }

    private void EndRoll()
    {
        CurrentState = PlayerState.Idle;
    }

    private void HandleAttack()
    {
        if (IsAttackRequested == false)
            return;

        IsAttackRequested = false;

        if (CurrentState == PlayerState.Roll)
            return;

        if (CurrentAttackState != PlayerAttackState.Idle)
            return;

        AttackTime = AttackDuration;
        AttackCollider.gameObject.SetActive(true);

        CurrentAttackState = PlayerAttackState.Attack;
    }

    private void UpdateAttack()
    {
        if (CurrentAttackState != PlayerAttackState.Attack)
            return;

        AttackTime -= Time.fixedDeltaTime;
        if (AttackTime < 0f)
        {
            AttackCollider.gameObject.SetActive(false);

            CurrentAttackState = PlayerAttackState.Idle;
        }
    }

    private void ProcessAttack(Collider col, string hitboxName)
    {
        if (col.TryGetComponent(out IDamageable obj))
        {
            obj.TakeDamage(AttackDamage);
            AddHitValueToStat(AttackDamage);
        }
    }

    private void AddHitValueToStat(float damage)
    {
        Stat1 += damage;
        if (Stat1 > MaxStat1)
            Stat1 = MaxStat1;
    }

    private void HandleSkill()
    {
        HandleSkill1();
        HandleSkill2();
    }

    private void HandleSkill1()
    {
        if (IsSkill1Requested == false)
            return;

        IsSkill1Requested = false;

        if (CurrentState == PlayerState.Roll)
            return;

        if (CurrentAttackState != PlayerAttackState.Idle)
            return;

        if (UseSkill1Stat1() == true)
        {
            Debug.Log("[Player] Skill 1 working");
        }
    }

    private bool UseSkill1Stat1()
    {
        if (Stat1 >= Skill1RequiredPoint)
        {
            Stat1 -= Skill1RequiredPoint;

            return true;
        }

        return false;
    }

    private void HandleSkill2()
    {
        if (IsSkill2Requested == false)
            return;

        IsSkill2Requested = false;

        if (CurrentState == PlayerState.Roll)
            return;

        if (CurrentAttackState != PlayerAttackState.Idle)
            return;

        if (UseSkill2Stat2() == true)
        {
            Debug.Log("[Player] Skill 2 working");
        }
    }

    private bool UseSkill2Stat2()
    {
        if (Stat2 >= Skill2RequiredPoint)
        {
            Stat2 -= Skill2RequiredPoint;

            return true;
        }

        return false;
    }

    public void TakeDamage(float damage)
    {
        if (IsAlive)
        {
            Health -= damage;
            AddHurtValueToStat(damage);
            if (Health <= 0f)
            {
                Health = 0f;
                Die();
            }
            OnHealthDrop(damage, MaxHealth);
        }
    }

    private void AddHurtValueToStat(float damage)
    {
        Stat2 += damage;
        if (Stat2 > MaxStat2)
            Stat2 = MaxStat2;
    }

    private void Die()
    {
        IsAlive = false;
        _playerAnimator.SetMovingState(PlayerAnimator.MovingState.Idle);
        Debug.Log($"[BasePlayer] {name} is dead");
    }
}
