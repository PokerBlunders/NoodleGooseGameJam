using UnityEngine;
using UnityEngine.AI;
using System.Collections;

/// <summary>
/// Enemy AI that hunts player by sound when moving, and can't see player when stationary.
/// </summary>
[RequireComponent(typeof(NavMeshAgent))]
public class EnemyAI : MonoBehaviour
{
    [Header("Detection")]
    [Tooltip("How far the enemy can see (only works when player is moving).")]
    public float visionRange = 12f;
    [Tooltip("Vision cone angle in degrees.")]
    public float visionAngle = 60f;
    [Tooltip("How often the enemy checks for the player.")]
    public float detectionInterval = 0.2f;

    [Header("Patrol")]
    [Tooltip("Waypoints for patrol route.")]
    public Transform[] patrolPoints;
    [Tooltip("Time to wait at each waypoint.")]
    public float waitTime = 2f;

    [Header("Hunting")]
    [Tooltip("How long to search after losing track.")]
    public float searchDuration = 5f;
    [Tooltip("Search radius around last known position.")]
    public float searchRadius = 3f;

    [Header("Audio Feedback")]
    public AudioSource audioSource;
    public AudioClip alertSound;
    public AudioClip huntingSound;
    public AudioClip lostSound;

    // States
    private enum EnemyState { Patrol, Hunting, Searching }
    private EnemyState currentState = EnemyState.Patrol;

    private NavMeshAgent agent;
    private PlayerStealthState playerState;
    private Transform player;
    private int currentPatrolIndex;
    private float waitTimer;
    private float searchTimer;
    private Vector3 lastKnownPosition;
    private float detectionTimer;
    private bool playerWasDetected;

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        player = GameObject.FindGameObjectWithTag("Player")?.transform;
        if (player != null)
            playerState = player.GetComponent<PlayerStealthState>();

        if (patrolPoints.Length > 0)
            GoToNextPatrolPoint();
    }

    void Update()
    {
        detectionTimer += Time.deltaTime;

        if (detectionTimer >= detectionInterval)
        {
            detectionTimer = 0f;
            DetectPlayer();
        }

        // State machine behavior
        switch (currentState)
        {
            case EnemyState.Patrol:
                PatrolBehavior();
                break;
            case EnemyState.Hunting:
                HuntingBehavior();
                break;
            case EnemyState.Searching:
                SearchingBehavior();
                break;
        }

        // Always face movement direction
        if (agent.velocity.magnitude > 0.1f)
            transform.rotation = Quaternion.LookRotation(agent.velocity);
    }

    void DetectPlayer()
    {
        if (player == null || playerState == null) return;

        Vector3 directionToPlayer = (player.position - transform.position);
        float distanceToPlayer = directionToPlayer.magnitude;

        bool canHearPlayer = false;
        bool canSeePlayer = false;

        // Sound detection: player within sound radius
        canHearPlayer = (distanceToPlayer <= playerState.currentSoundRadius);

        // Vision detection: only works if player is MOVING (visible)
        if (playerState.isMoving && distanceToPlayer <= visionRange)
        {
            float angleToPlayer = Vector3.Angle(transform.forward, directionToPlayer.normalized);
            if (angleToPlayer <= visionAngle * 0.5f)
            {
                // Raycast to check line of sight
                RaycastHit hit;
                if (Physics.Raycast(transform.position + Vector3.up, directionToPlayer.normalized, out hit, visionRange))
                {
                    if (hit.transform.CompareTag("Player"))
                    {
                        canSeePlayer = true;
                    }
                }
            }
        }

        // React to detections
        if (canSeePlayer || canHearPlayer)
        {
            if (currentState != EnemyState.Hunting)
            {
                TransitionToState(EnemyState.Hunting);
            }
            lastKnownPosition = player.position;
            agent.SetDestination(lastKnownPosition);
        }
        else if (currentState == EnemyState.Hunting)
        {
            // Lost track - start searching
            TransitionToState(EnemyState.Searching);
            lastKnownPosition = player.position;
            searchTimer = 0f;
        }
    }

    void PatrolBehavior()
    {
        if (patrolPoints.Length == 0) return;

        if (!agent.pathPending && agent.remainingDistance < 0.5f)
        {
            waitTimer += Time.deltaTime;
            if (waitTimer >= waitTime)
            {
                GoToNextPatrolPoint();
                waitTimer = 0f;
            }
        }
    }

    void HuntingBehavior()
    {
        // Continuously update destination to player's last known position
        if (player != null)
        {
            agent.SetDestination(player.position);
        }
    }

    void SearchingBehavior()
    {
        searchTimer += Time.deltaTime;

        if (searchTimer >= searchDuration)
        {
            // Give up, go back to patrol
            TransitionToState(EnemyState.Patrol);
            GoToNextPatrolPoint();
            return;
        }

        // Search around last known position
        if (!agent.pathPending && agent.remainingDistance < 0.5f)
        {
            Vector3 randomPoint = lastKnownPosition + Random.insideUnitSphere * searchRadius;
            NavMeshHit navHit;
            if (NavMesh.SamplePosition(randomPoint, out navHit, searchRadius, NavMesh.AllAreas))
            {
                agent.SetDestination(navHit.position);
            }
        }
    }

    void GoToNextPatrolPoint()
    {
        if (patrolPoints.Length == 0) return;
        agent.SetDestination(patrolPoints[currentPatrolIndex].position);
        currentPatrolIndex = (currentPatrolIndex + 1) % patrolPoints.Length;
    }

    void TransitionToState(EnemyState newState)
    {
        currentState = newState;

        switch (newState)
        {
            case EnemyState.Hunting:
                agent.speed = 5f;
                PlaySound(alertSound);
                PlaySound(huntingSound);
                break;
            case EnemyState.Searching:
                agent.speed = 3f;
                PlaySound(lostSound);
                break;
            case EnemyState.Patrol:
                agent.speed = 2f;
                break;
        }
    }

    void PlaySound(AudioClip clip)
    {
        if (audioSource != null && clip != null)
        {
            audioSource.PlayOneShot(clip);
        }
    }

    // Visualize detection ranges in editor
    void OnDrawGizmosSelected()
    {
        // Vision cone
        Gizmos.color = Color.red;
        Vector3 forward = transform.forward;
        Quaternion leftRay = Quaternion.AngleAxis(-visionAngle * 0.5f, Vector3.up);
        Quaternion rightRay = Quaternion.AngleAxis(visionAngle * 0.5f, Vector3.up);
        Vector3 leftBoundary = leftRay * forward * visionRange;
        Vector3 rightBoundary = rightRay * forward * visionRange;

        Gizmos.DrawRay(transform.position + Vector3.up, leftBoundary);
        Gizmos.DrawRay(transform.position + Vector3.up, rightBoundary);
        Gizmos.DrawRay(transform.position + Vector3.up, forward * visionRange);

        // Vision arc
        UnityEditor.Handles.color = new Color(1f, 0f, 0f, 0.1f);
        UnityEditor.Handles.DrawSolidArc(
            transform.position + Vector3.up,
            Vector3.up,
            leftBoundary,
            visionAngle,
            visionRange
        );

        // Patrol points
        Gizmos.color = Color.green;
        if (patrolPoints != null)
        {
            for (int i = 0; i < patrolPoints.Length; i++)
            {
                if (patrolPoints[i] != null)
                {
                    Gizmos.DrawSphere(patrolPoints[i].position, 0.3f);
                    if (i < patrolPoints.Length - 1 && patrolPoints[i + 1] != null)
                        Gizmos.DrawLine(patrolPoints[i].position, patrolPoints[i + 1].position);
                }
            }
        }
    }
}