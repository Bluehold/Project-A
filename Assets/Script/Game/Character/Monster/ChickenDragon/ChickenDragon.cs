using UnityEngine;

[RequireComponent(typeof(ChickenDragonCombat), typeof(ChickenDragonGrabbable))]
[DisallowMultipleComponent]
public class ChickenDragon : BaseMonster
{
    [Header("ChickenDragon")]
    [SerializeField]
    private float WanderRadius = 6f;

    [SerializeField]
    private float WanderInterval = 4f;

    private float WanderTimer;

    [Header("Components")]
    #if UNITY_EDITOR
    [SerializeField]
    private ChickenDragonAI chickenAI;
    #endif

    [SerializeField]
    private ChickenDragonCombat chickenCombat;

    [SerializeField]
    private ChickenDragonGrabbable chickenGrabbable;

    private void Start()
    {
        WanderTimer = Random.Range(0f, WanderInterval);

        #if UNITY_EDITOR
        if (chickenAI == null) chickenAI = GetComponent<ChickenDragonAI>();
        #endif
        if (chickenCombat == null) chickenCombat = GetComponent<ChickenDragonCombat>();
        if (chickenGrabbable == null) chickenGrabbable = GetComponent<ChickenDragonGrabbable>();
    }

    private void Update()
    {
        WanderTimer += Time.deltaTime;
        if (WanderTimer >= WanderInterval && AiComponent != null && MovementComponent != null)
        {
            WanderTimer = 0f;

            Vector3 randomPoint = transform.position + (Random.insideUnitSphere * WanderRadius);
            randomPoint.y = transform.position.y;

            SetMoveDestination(randomPoint);
        }
    }
}
