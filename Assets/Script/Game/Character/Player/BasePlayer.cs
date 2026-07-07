using System.Runtime.ConstrainedExecution;
using Unity.Mathematics;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;
using static UnityEngine.InputSystem.InputAction;

public class BasePlayer : MonoBehaviour, IDamageable
{
    [SerializeField]
    private bool IsAlive;

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
    private Vector2 RotateDir;

    private bool IsSprinting;
    private bool DidSprintRequested;

    [SerializeField]
    private float RollRequiredStamina;
    [SerializeField]
    private float RollStaminaChargeDelay;
    [SerializeField]
    private float RollDistance = 0f;
    private float RollDuration = 0.7835f;
    private float RollTime;
    private Vector3 RollStartPosition;
    private Quaternion RollStartRotation;
    private Rigidbody rb;
    private bool IsRollActive;

    [SerializeField]
    private float AttackDamage;
    [SerializeField]
    private PlayerAttackTrigger AttackCollider;
    private bool IsAttackRequested;
    private bool IsAttacking;
    [SerializeField]
    private float AttackDuration;
    private float AttackTime;

    private bool IsSkill1Requested;
    [SerializeField]
    private float Skill1RequiredPoint;
    private bool IsSkill2Requested;
    [SerializeField]
    private float Skill2RequiredPoint;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    private void Start()
    {
        IsAlive = true;
        BarUIController.Instance.HealthBar.ResetValue(Health / MaxHealth);
        BarUIController.Instance.ManaBar.ResetValue(Mana / MaxMana);
        BarUIController.Instance.StaminaBar.ResetValue(Stamina / MaxStamina);

        if (AttackCollider != null)
        {
            AttackCollider.OnAttackTriggerEnter += ProcessAttack;
            AttackCollider.gameObject.SetActive(false);
        }


    }

    private void Update()
    {
        MoveInput = InputManager.Inputs.Player.Move.ReadValue<Vector2>();

        if (_mainCamera != null)
        {
            Vector3 dir3 =
            Quaternion.Euler(0, _mainCamera.GetRotationY(), 0)
            * new Vector3(MoveInput.x, 0, MoveInput.y);

            MoveInput = new Vector2(dir3.x, dir3.z);
        }

        if (InputManager.Inputs.Player.Sprint.WasPressedThisFrame())
            DidSprintRequested = true;

        if (InputManager.Inputs.Player.Sprint.IsPressed())
            IsSprinting = true;
        else
            IsSprinting = false;

        if (InputManager.Inputs.Player.Attack.WasPressedThisFrame())
            IsAttackRequested = true;

        if (InputManager.Inputs.Player.Skill1.WasPressedThisDynamicUpdate())
            IsSkill1Requested = true;
        if (InputManager.Inputs.Player.Skill2.WasPressedThisDynamicUpdate())
            IsSkill2Requested = true;
    }

    private void FixedUpdate()
    {
        if (IsAlive == false) return;
        if (IsRollActive == false && MoveInput.sqrMagnitude > 0.001f)
        {
            RotateDir = MoveInput;

            Vector3 vel = rb.linearVelocity;

            float speed;
            if (IsSprinting == true)
            {
                if (DidSprintEnd == false)
                {
                    _playerAnimator.SetMovingState(PlayerAnimator.MovingState.Sprint);

                    speed = SprintSpeed;

                    float usedStamina = Time.fixedDeltaTime * SprintRequiredStamina;
                    Stamina -= usedStamina;
                    BarUIController.Instance.StaminaBar.DrainBar(usedStamina / MaxStamina);

                    if (Stamina < 0f)
                    {
                        Stamina = 0f;
                        DidSprintEnd = true;
                    }
                }
                else
                {
                    _playerAnimator.SetMovingState(PlayerAnimator.MovingState.Walk);

                    speed = WalkSpeed;
                }
            }
            else
            {
                _playerAnimator.SetMovingState(PlayerAnimator.MovingState.Walk);

                speed = WalkSpeed;
                DidSprintEnd = false;
            }

            vel.x = MoveInput.x * speed;
            vel.z = MoveInput.y * speed;

            rb.linearVelocity = vel;

            // Roll 감지
            if (DidSprintRequested == true)
            {
                DidSprintRequested = false;
                if (IsRollActive == false && IsAttacking == false && Stamina > RollRequiredStamina)
                {
                    Roll();
                    Stamina -= RollRequiredStamina;
                    StaminaChargeRemainingTime = RollStaminaChargeDelay;

                    BarUIController.Instance.StaminaBar.DrainBar(RollRequiredStamina / MaxStamina);
                }
            }
        }
        else // 이동 없을 시 키 초기화 및 움직임 감속
        {
            DidSprintRequested = false;
            _playerAnimator.SetMovingState(PlayerAnimator.MovingState.Idle);

            Vector3 vel = rb.linearVelocity;

            vel.x *= 0.7f;
            vel.z *= 0.7f;

            rb.linearVelocity = vel;
        }

        // Stamina 회복
        if (StaminaChargeRemainingTime == 0f)
        {
            if ((IsSprinting == false || (IsSprinting && DidSprintEnd)) && IsRollActive == false)
            {
                Stamina += Time.fixedDeltaTime * StaminaChargeAmount;
                if (Stamina > MaxStamina)
                    Stamina = MaxStamina;

                BarUIController.Instance.StaminaBar.AddBar(Time.fixedDeltaTime * StaminaChargeAmount / MaxStamina);
            }
        }
        else
        {
            StaminaChargeRemainingTime -= Time.fixedDeltaTime;
            if (StaminaChargeRemainingTime < 0f)
                StaminaChargeRemainingTime = 0f;
        }

        if (RotateDir.sqrMagnitude > 0.001f)
        {
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

        // Roll
        if (IsRollActive)
        {
            RollTime += Time.fixedDeltaTime;
            float t = Mathf.Clamp01(RollTime / RollDuration);
            float dist = RollDistance * (1f - Mathf.Pow(1f - t, 3));

            Vector3 targetPos = RollStartPosition + RollStartRotation * Vector3.forward * dist;
            targetPos.y = rb.position.y;
            rb.MovePosition(targetPos);
            if (t >= 1f)
            {
                IsRollActive = false;
            }
        }

        // Attack
        if (IsAttackRequested)
        {
            if (IsRollActive == false && IsAttacking == false)
            {
                IsAttacking = true;
                AttackTime = AttackDuration;
                AttackCollider.gameObject.SetActive(true);
            }
            IsAttackRequested = false;
            _playerAnimator.PlayAttack("mixamo_com");
        }

        if (IsAttacking)
        {
            AttackTime -= Time.fixedDeltaTime;
            if (AttackTime < 0f)
            {
                AttackCollider.gameObject.SetActive(false);
                IsAttacking = false;
            }
        }

        // Skill
        if (IsSkill1Requested)
        {
            if (IsRollActive == false && IsAttacking == false)
            {
                if (Stat1 >= Skill1RequiredPoint)
                {
                    Stat1 -= Skill1RequiredPoint;

                    Debug.Log("[Player] Skill 1 working");
                }
            }
            IsSkill1Requested = false;
        }

        if (IsSkill2Requested)
        {
            if (IsRollActive == false && IsAttacking == false)
            {
                if (Stat2 >= Skill2RequiredPoint)
                {
                    Stat2 = Skill2RequiredPoint;

                    Debug.Log("[Player] Skill 2 working");
                }
            }
            IsSkill2Requested = false;
        }
    }

    private void Roll()
    {
        RollStartPosition = rb.position;
        RollStartRotation = PlayerModel.rotation;
        RollTime = 0f;

        _playerAnimator.TriggerRoll();
        IsRollActive = true;
    }

    private void ProcessAttack(Collider col, string hitboxName)
    {
        if (col.TryGetComponent(out IDamageable obj))
        {
            obj.TakeDamage(AttackDamage);
            Stat1 += AttackDamage;
            if (Stat1 > MaxStat1)
                Stat1 = MaxStat1;
        }
    }

    public void TakeDamage(float damage)
    {
        if (IsAlive)
        {
            Health -= damage;
            Stat2 += damage;
            if (Stat2 > MaxStat2)
                Stat2 = MaxStat2;
            if (Health <= 0f)
            {
                Health = 0f;
                Die();
            }
            BarUIController.Instance.HealthBar.DrainBar(damage / MaxHealth);
        }
    }

    private void Die()
    {
        IsAlive = false;
        _playerAnimator.SetMovingState(PlayerAnimator.MovingState.Idle);
        Debug.Log($"[BasePlayer] {name} is dead");
    }
}
