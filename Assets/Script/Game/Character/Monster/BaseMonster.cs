using UnityEngine;

public class BaseMonster : MonoBehaviour, IDamageable
{
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

    private Rigidbody rb;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
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

    }

    // Move insert position
    private void UpdateMove()
    {

    }

    public void TakeDamage(float damage)
    {
        Health -= damage;

        if (Health < 0f)
            Health = 0f;

        Debug.Log($"Monster HP : {Health}/{MaxHealth}");

        if (Health == 0f)
        {
            Die();
        }
    }

    private void Die()
    {
        Debug.Log("Monster Dead");
    }
}