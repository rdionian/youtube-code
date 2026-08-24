using UnityEngine;
using UnityEngine.AI;
using Unity.Netcode;
using System.Collections.Generic;
using Unity.VisualScripting;

[RequireComponent(typeof(NavMeshAgent))]
[RequireComponent(typeof(CreatureBehaviorBase))]
public class AIController : NetworkBehaviour
{
    public enum State { Idle, Wander, Alert, Investigate, Chase, Attack, Flee }

    [Header("Wander")]
    public float wanderRadius = 15f;
    public float wanderInterval = 4f;

    [Header("Alert")]
    public float alertDuration = 5f;
    public float alertTurnSpeed = 4f;

    [Header("Sight")]
    public float sightRange = 12f;
    public float sightAngle = 60f;
    public float eyeHeight = 1.5f;
    public LayerMask obstructionMask;

    [Header("Chase")]
    public float attackRange = 2f;

    [Header("Hearing")]
    public float hearingThreshold = 0.3f;

    private static readonly List<AIController> activeCreatures = new List<AIController>();

    public NavMeshAgent Agent { get; private set; }
    public State CurrentState { get; private set; }
    public float StateTimer { get; set; }
    public Transform ChaseTarget { get; set; }
    public Vector3 AlertPoint { get; set; }
    public float DefaultSpeed { get; private set; }

    private CreatureBehaviorBase behavior;

    private void Awake()
    {
        Agent = GetComponent<NavMeshAgent>();
        DefaultSpeed = Agent.speed;
        behavior = GetComponent<CreatureBehaviorBase>();
    }

    public override void OnNetworkSpawn()
    {
        if (!IsServer)
        {
            enabled = false;
            return;
        }

        EnterState(State.Idle);
    }

    private void OnEnable() => activeCreatures.Add(this);
    private void OnDisable() => activeCreatures.Remove(this);

    private void Update()
    {
        StateTimer -= Time.deltaTime;

        switch (CurrentState)
        {
            case State.Idle: behavior.OnUpdateIdle(); break;
            case State.Wander: behavior.OnUpdateWander(); break;
            case State.Alert: behavior.OnUpdateAlert(); break;
            case State.Investigate: behavior.OnUpdateInvestigate(); break;
            case State.Chase: behavior.OnUpdateChase(); break;
            case State.Attack: behavior.OnUpdateAttack(); break;
            case State.Flee: behavior.OnUpdateFlee(); break;
        }
    }

    public void EnterState(State newState)
    {
        CurrentState = newState;

        switch (newState)
        {
            case State.Idle: behavior.OnEnterIdle(); break;
            case State.Wander: behavior.OnEnterWander(); break;
            case State.Alert: behavior.OnEnterAlert(); break;
            case State.Investigate: behavior.OnEnterInvestigate(); break;
            case State.Chase: behavior.OnEnterChase(); break;
            case State.Attack: behavior.OnEnterAttack(); break;
            case State.Flee: behavior.OnEnterFlee(); break;
        }
    }

    public void FaceTarget(Transform target, float turnSpeed)
    {
        if(target == null) return;

        Vector3 direction = target.position - transform.position;
        direction.y = 0f;
        if (direction.sqrMagnitude < .01f) return;

        Quaternion targetRotation = Quaternion.LookRotation(direction);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, turnSpeed * Time.deltaTime);
    }

    public void FaceAlertPoint()
    {
        Vector3 direction = AlertPoint - transform.position;
        direction.y = 0f;
        if (direction.sqrMagnitude < .01f) return;

        transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(direction), alertTurnSpeed * Time.deltaTime);
    }

    public Transform GetNearestPlayer()
    {
        Transform nearest = null;
        float nearestDist = float.MaxValue;
        foreach(var p in PlayerController.activePlayers)
        {
            float dist = Vector3.Distance(transform.position, p.transform.position);
            if(dist < nearestDist) { nearestDist = dist; nearest = p.transform; }
        }
        return nearest;
    }

    public bool CanSeeTarget(Transform target)
    {
        Vector3 eyePos = transform.position + Vector3.up * eyeHeight;
        Vector3 toPlayer = target.position - eyePos;
        float distance = toPlayer.magnitude;

        if (distance <= attackRange) return true;
        if (distance > sightRange) return false;
        if (Vector3.Angle(transform.forward, toPlayer) > sightAngle * .5f) return false;

        if(Physics.Raycast(eyePos, toPlayer.normalized, out RaycastHit hit, distance, obstructionMask))
        {
            if (hit.transform.root != target.root) return false;
        }

        return true;
    }

    public bool CanSeePlayer()
    {
        Transform target = GetNearestPlayer();
        if (target == null) return false;
        if(!CanSeeTarget(target)) return false;

        ChaseTarget = target;

        return true;
    }

    public Vector3 RandomPointOnNavMesh(Vector3 origin, float radius)
    {
        Vector3 randomDirection = Random.insideUnitSphere * radius + origin;
        if(NavMesh.SamplePosition(randomDirection, out NavMeshHit hit, radius, NavMesh.AllAreas)) return hit.position;
        return origin;
    }

    public static void NotifyNoise(Vector3 position, float radius, float intensity)
    {
        foreach(AIController creature in activeCreatures)
        {
            if (creature.CurrentState == State.Chase || creature.CurrentState == State.Attack || creature.CurrentState == State.Flee) continue;
            if (intensity <= creature.hearingThreshold) continue;
            if(Vector3.Distance(creature.transform.position, position) <= radius){
                creature.TriggerAlert(position);
            }
        }
    }

    public void TriggerAlert(Vector3 sourcePosition)
    {
        AlertPoint = sourcePosition;
        EnterState(State.Alert); 
    }
}