using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace UnityAIForge.GameKit
{
    /// <summary>
    /// GameKit AI Behavior component: provides common AI behaviors.
    /// Supports patrol, chase, flee, and combined patrol-and-chase patterns.
    /// </summary>
    [AddComponentMenu("UnityAIForge/GameKit/AI Behavior")]
    public class GameKitAIBehavior : MonoBehaviour
    {
        [Header("Identity")]
        [SerializeField] private string aiId;

        [Header("Behavior Type")]
        [SerializeField] private AIBehaviorType behaviorType = AIBehaviorType.Patrol;
        [SerializeField] private AIState currentState = AIState.Idle;

        [Header("Movement")]
        [SerializeField] private float moveSpeed = 3f;
        [SerializeField] private float turnSpeed = 5f;
        [SerializeField] private bool use2DMovement = true;

        [Header("Patrol Settings")]
        [SerializeField] private List<Transform> patrolPoints = new List<Transform>();
        [SerializeField] private PatrolMode patrolMode = PatrolMode.Loop;
        [SerializeField] private float waitTimeAtPoint = 1f;
        [SerializeField] private float arrivalThreshold = 0.5f;

        [Header("Detection Settings")]
        [SerializeField] private Transform chaseTarget;
        [SerializeField] private string chaseTargetTag = "Player";
        [SerializeField] private float detectionRadius = 10f;
        [SerializeField] private float loseTargetDistance = 15f;
        [SerializeField] private float fieldOfView = 360f;
        [SerializeField] private LayerMask detectionLayers = -1;
        [SerializeField] private bool requireLineOfSight = false;
        [SerializeField] private LayerMask obstacleLayers;

        [Header("Attack Settings")]
        [SerializeField] private float attackRange = 2f;
        [SerializeField] private float attackCooldown = 1f;

        [Header("Flee Settings")]
        [SerializeField] private float fleeDistance = 15f;
        [SerializeField] private float safeDistance = 20f;

        [Header("Events")]
        public UnityEvent<Transform> OnTargetDetected = new UnityEvent<Transform>();
        public UnityEvent OnTargetLost = new UnityEvent();
        public UnityEvent<Transform> OnReachedPatrolPoint = new UnityEvent<Transform>();
        public UnityEvent<AIState, AIState> OnStateChanged = new UnityEvent<AIState, AIState>();
        public UnityEvent<Transform> OnAttack = new UnityEvent<Transform>();
        public UnityEvent OnReturnToPatrol = new UnityEvent();

        // State
        private int currentPatrolIndex = 0;
        private bool patrolForward = true;
        private float waitTimer = 0f;
        private float attackTimer = 0f;
        private Vector3 lastKnownTargetPosition;
        private Vector3 startPosition;
        private GameKitActor linkedActor;

        // Properties
        public string AIId => aiId;
        public AIBehaviorType BehaviorType => behaviorType;
        public AIState CurrentState => currentState;
        public Transform ChaseTarget => chaseTarget;
        public float MoveSpeed => moveSpeed;
        public float DetectionRadius => detectionRadius;
        public float AttackRange => attackRange;
        public bool HasTarget => chaseTarget != null;

        private void Awake()
        {
            EnsureEventsInitialized();
            startPosition = transform.position;
            linkedActor = GetComponent<GameKitActor>();
        }

        private void Start()
        {
            // Start in appropriate state
            if (behaviorType == AIBehaviorType.Patrol || behaviorType == AIBehaviorType.PatrolAndChase)
            {
                SetState(AIState.Patrol);
            }
            else
            {
                SetState(AIState.Idle);
            }
        }

        private void Update()
        {
            UpdateAttackCooldown();

            switch (currentState)
            {
                case AIState.Idle:
                    UpdateIdle();
                    break;
                case AIState.Patrol:
                    UpdatePatrol();
                    break;
                case AIState.Chase:
                    UpdateChase();
                    break;
                case AIState.Attack:
                    UpdateAttack();
                    break;
                case AIState.Flee:
                    UpdateFlee();
                    break;
                case AIState.Return:
                    UpdateReturn();
                    break;
            }
        }

        /// <summary>
        /// Initialize the AI behavior with specified settings.
        /// </summary>
        public void Initialize(string id, AIBehaviorType type, bool is2D = true)
        {
            aiId = id;
            behaviorType = type;
            use2DMovement = is2D;
            EnsureEventsInitialized();
        }

        private void EnsureEventsInitialized()
        {
            OnTargetDetected ??= new UnityEvent<Transform>();
            OnTargetLost ??= new UnityEvent();
            OnReachedPatrolPoint ??= new UnityEvent<Transform>();
            OnStateChanged ??= new UnityEvent<AIState, AIState>();
            OnAttack ??= new UnityEvent<Transform>();
            OnReturnToPatrol ??= new UnityEvent();
        }

        #region State Updates

        private void UpdateIdle()
        {
            // Check for target if this behavior can chase
            if (behaviorType == AIBehaviorType.Chase ||
                behaviorType == AIBehaviorType.PatrolAndChase ||
                behaviorType == AIBehaviorType.Flee)
            {
                CheckForTarget();
            }
        }

        private void UpdatePatrol()
        {
            // Check for target if this behavior can chase
            if (behaviorType == AIBehaviorType.PatrolAndChase)
            {
                if (CheckForTarget())
                {
                    SetState(AIState.Chase);
                    return;
                }
            }

            if (patrolPoints.Count == 0)
            {
                SetState(AIState.Idle);
                return;
            }

            // Wait at point
            if (waitTimer > 0)
            {
                waitTimer -= Time.deltaTime;
                return;
            }

            // Move to current patrol point
            var targetPoint = patrolPoints[currentPatrolIndex];
            if (targetPoint == null)
            {
                MoveToNextPatrolPoint();
                return;
            }

            MoveTowards(targetPoint.position);

            // Check if reached
            float distance = use2DMovement
                ? Vector2.Distance(transform.position, targetPoint.position)
                : Vector3.Distance(transform.position, targetPoint.position);

            if (distance <= arrivalThreshold)
            {
                OnReachedPatrolPoint?.Invoke(targetPoint);
                waitTimer = waitTimeAtPoint;
                MoveToNextPatrolPoint();
            }
        }

        private void UpdateChase()
        {
            // Check if lost target
            if (chaseTarget == null || !IsTargetInRange(loseTargetDistance))
            {
                LoseTarget();
                return;
            }

            // Check if in attack range
            float distance = use2DMovement
                ? Vector2.Distance(transform.position, chaseTarget.position)
                : Vector3.Distance(transform.position, chaseTarget.position);

            if (distance <= attackRange)
            {
                SetState(AIState.Attack);
                return;
            }

            // Chase target
            lastKnownTargetPosition = chaseTarget.position;
            MoveTowards(chaseTarget.position);
        }

        private void UpdateAttack()
        {
            if (chaseTarget == null)
            {
                LoseTarget();
                return;
            }

            // Face target
            LookAt(chaseTarget.position);

            // Check if target moved out of range
            float distance = use2DMovement
                ? Vector2.Distance(transform.position, chaseTarget.position)
                : Vector3.Distance(transform.position, chaseTarget.position);

            if (distance > attackRange * 1.2f) // Small buffer
            {
                SetState(AIState.Chase);
                return;
            }

            // Attack if cooldown ready
            if (attackTimer <= 0)
            {
                OnAttack?.Invoke(chaseTarget);
                attackTimer = attackCooldown;

                // Send action input to linked actor if available
                if (linkedActor != null)
                {
                    linkedActor.SendActionInput("Attack");
                }
            }
        }

        private void UpdateFlee()
        {
            if (chaseTarget == null)
            {
                SetState(behaviorType == AIBehaviorType.PatrolAndChase ? AIState.Patrol : AIState.Idle);
                return;
            }

            float distance = use2DMovement
                ? Vector2.Distance(transform.position, chaseTarget.position)
                : Vector3.Distance(transform.position, chaseTarget.position);

            // Check if safe
            if (distance >= safeDistance)
            {
                LoseTarget();
                return;
            }

            // Flee from target
            Vector3 fleeDirection = (transform.position - chaseTarget.position).normalized;
            Vector3 fleeTarget = transform.position + fleeDirection * fleeDistance;
            MoveTowards(fleeTarget);
        }

        private void UpdateReturn()
        {
            // Return to start position or first patrol point
            Vector3 returnTarget = patrolPoints.Count > 0 ? patrolPoints[0].position : startPosition;

            float distance = use2DMovement
                ? Vector2.Distance(transform.position, returnTarget)
                : Vector3.Distance(transform.position, returnTarget);

            if (distance <= arrivalThreshold)
            {
                OnReturnToPatrol?.Invoke();
                SetState(behaviorType == AIBehaviorType.PatrolAndChase || behaviorType == AIBehaviorType.Patrol
                    ? AIState.Patrol
                    : AIState.Idle);
                return;
            }

            MoveTowards(returnTarget);
        }

        private void UpdateAttackCooldown()
        {
            if (attackTimer > 0)
            {
                attackTimer -= Time.deltaTime;
            }
        }

        #endregion

        #region Target Detection

        private bool CheckForTarget()
        {
            if (chaseTarget != null)
                return true;

            // Find target by tag
            var potentialTargets = GameObject.FindGameObjectsWithTag(chaseTargetTag);
            Transform closestTarget = null;
            float closestDistance = float.MaxValue;

            foreach (var target in potentialTargets)
            {
                if (!IsTargetVisible(target.transform))
                    continue;

                float distance = use2DMovement
                    ? Vector2.Distance(transform.position, target.transform.position)
                    : Vector3.Distance(transform.position, target.transform.position);

                if (distance <= detectionRadius && distance < closestDistance)
                {
                    closestDistance = distance;
                    closestTarget = target.transform;
                }
            }

            if (closestTarget != null)
            {
                SetTarget(closestTarget);
                return true;
            }

            return false;
        }

        private bool IsTargetVisible(Transform target)
        {
            // Check distance
            float distance = use2DMovement
                ? Vector2.Distance(transform.position, target.position)
                : Vector3.Distance(transform.position, target.position);

            if (distance > detectionRadius)
                return false;

            // Check field of view
            if (fieldOfView < 360f)
            {
                Vector3 directionToTarget = (target.position - transform.position).normalized;
                Vector3 forward = use2DMovement ? transform.right : transform.forward;
                float angle = Vector3.Angle(forward, directionToTarget);

                if (angle > fieldOfView / 2f)
                    return false;
            }

            // Check line of sight
            if (requireLineOfSight)
            {
                Vector3 direction = target.position - transform.position;
                if (Physics.Raycast(transform.position, direction, out RaycastHit hit, distance, obstacleLayers))
                {
                    if (hit.transform != target)
                        return false;
                }
            }

            return true;
        }

        private bool IsTargetInRange(float range)
        {
            if (chaseTarget == null)
                return false;

            float distance = use2DMovement
                ? Vector2.Distance(transform.position, chaseTarget.position)
                : Vector3.Distance(transform.position, chaseTarget.position);

            return distance <= range;
        }

        #endregion

        #region Movement

        private void MoveTowards(Vector3 targetPosition)
        {
            Vector3 direction;

            if (use2DMovement)
            {
                direction = new Vector3(
                    targetPosition.x - transform.position.x,
                    targetPosition.y - transform.position.y,
                    0
                ).normalized;
            }
            else
            {
                direction = (targetPosition - transform.position).normalized;
                direction.y = 0; // Keep on ground
            }

            // Move
            transform.position += direction * moveSpeed * Time.deltaTime;

            // Rotate towards movement direction
            if (direction != Vector3.zero)
            {
                LookAt(transform.position + direction);
            }

            // Send move input to linked actor if available
            if (linkedActor != null)
            {
                linkedActor.SendMoveInput(direction);
            }
        }

        private void LookAt(Vector3 targetPosition)
        {
            if (use2DMovement)
            {
                Vector2 direction = new Vector2(
                    targetPosition.x - transform.position.x,
                    targetPosition.y - transform.position.y
                );

                if (direction != Vector2.zero)
                {
                    float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
                    Quaternion targetRotation = Quaternion.Euler(0, 0, angle);
                    transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, turnSpeed * Time.deltaTime);
                }
            }
            else
            {
                Vector3 direction = targetPosition - transform.position;
                direction.y = 0;

                if (direction != Vector3.zero)
                {
                    Quaternion targetRotation = Quaternion.LookRotation(direction);
                    transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, turnSpeed * Time.deltaTime);
                }
            }
        }

        private void MoveToNextPatrolPoint()
        {
            if (patrolPoints.Count == 0)
                return;

            switch (patrolMode)
            {
                case PatrolMode.Loop:
                    currentPatrolIndex = (currentPatrolIndex + 1) % patrolPoints.Count;
                    break;

                case PatrolMode.PingPong:
                    if (patrolForward)
                    {
                        currentPatrolIndex++;
                        if (currentPatrolIndex >= patrolPoints.Count)
                        {
                            currentPatrolIndex = patrolPoints.Count - 2;
                            patrolForward = false;
                        }
                    }
                    else
                    {
                        currentPatrolIndex--;
                        if (currentPatrolIndex < 0)
                        {
                            currentPatrolIndex = 1;
                            patrolForward = true;
                        }
                    }
                    currentPatrolIndex = Mathf.Clamp(currentPatrolIndex, 0, patrolPoints.Count - 1);
                    break;

                case PatrolMode.Random:
                    int newIndex;
                    if (patrolPoints.Count > 1)
                    {
                        do
                        {
                            newIndex = UnityEngine.Random.Range(0, patrolPoints.Count);
                        } while (newIndex == currentPatrolIndex);
                        currentPatrolIndex = newIndex;
                    }
                    break;
            }
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Set a specific target to chase or flee from.
        /// </summary>
        public void SetTarget(Transform target)
        {
            var previousTarget = chaseTarget;
            chaseTarget = target;

            if (target != null && previousTarget == null)
            {
                OnTargetDetected?.Invoke(target);

                if (behaviorType == AIBehaviorType.Flee)
                {
                    SetState(AIState.Flee);
                }
                else if (behaviorType == AIBehaviorType.Chase || behaviorType == AIBehaviorType.PatrolAndChase)
                {
                    SetState(AIState.Chase);
                }
            }
        }

        /// <summary>
        /// Clear the current target.
        /// </summary>
        public void ClearTarget()
        {
            LoseTarget();
        }

        /// <summary>
        /// Force a state change.
        /// </summary>
        public void ForceState(AIState newState)
        {
            SetState(newState);
        }

        /// <summary>
        /// Add a patrol point.
        /// </summary>
        public void AddPatrolPoint(Transform point)
        {
            if (point != null && !patrolPoints.Contains(point))
            {
                patrolPoints.Add(point);
            }
        }

        /// <summary>
        /// Clear all patrol points.
        /// </summary>
        public void ClearPatrolPoints()
        {
            patrolPoints.Clear();
        }

        /// <summary>
        /// Set movement speed.
        /// </summary>
        public void SetMoveSpeed(float speed)
        {
            moveSpeed = Mathf.Max(0, speed);
        }

        /// <summary>
        /// Set detection radius.
        /// </summary>
        public void SetDetectionRadius(float radius)
        {
            detectionRadius = Mathf.Max(0, radius);
        }

        #endregion

        #region State Management

        private void SetState(AIState newState)
        {
            if (currentState == newState)
                return;

            var previousState = currentState;
            currentState = newState;
            OnStateChanged?.Invoke(previousState, newState);
        }

        private void LoseTarget()
        {
            if (chaseTarget != null)
            {
                chaseTarget = null;
                OnTargetLost?.Invoke();
            }

            // Return to appropriate state
            if (behaviorType == AIBehaviorType.PatrolAndChase)
            {
                SetState(AIState.Return);
            }
            else if (behaviorType == AIBehaviorType.Patrol)
            {
                SetState(AIState.Patrol);
            }
            else
            {
                SetState(AIState.Idle);
            }
        }

        #endregion

        #region Enums

        public enum AIBehaviorType
        {
            /// <summary>Only patrol between points.</summary>
            Patrol,
            /// <summary>Only chase detected targets.</summary>
            Chase,
            /// <summary>Flee from detected targets.</summary>
            Flee,
            /// <summary>Patrol normally, chase when target detected.</summary>
            PatrolAndChase
        }

        public enum PatrolMode
        {
            /// <summary>Loop back to first point after reaching last.</summary>
            Loop,
            /// <summary>Go back and forth between points.</summary>
            PingPong,
            /// <summary>Pick random next point.</summary>
            Random
        }

        public enum AIState
        {
            Idle,
            Patrol,
            Chase,
            Attack,
            Flee,
            Return
        }

        #endregion

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            // Detection radius
            Gizmos.color = new Color(1, 1, 0, 0.3f);
            Gizmos.DrawWireSphere(transform.position, detectionRadius);

            // Lose target distance
            Gizmos.color = new Color(1, 0.5f, 0, 0.2f);
            Gizmos.DrawWireSphere(transform.position, loseTargetDistance);

            // Attack range
            Gizmos.color = new Color(1, 0, 0, 0.3f);
            Gizmos.DrawWireSphere(transform.position, attackRange);

            // Patrol points
            Gizmos.color = Color.cyan;
            for (int i = 0; i < patrolPoints.Count; i++)
            {
                if (patrolPoints[i] == null) continue;

                Gizmos.DrawWireSphere(patrolPoints[i].position, 0.3f);

                if (i < patrolPoints.Count - 1 && patrolPoints[i + 1] != null)
                {
                    Gizmos.DrawLine(patrolPoints[i].position, patrolPoints[i + 1].position);
                }
            }

            // Loop connection
            if (patrolMode == PatrolMode.Loop && patrolPoints.Count > 1)
            {
                if (patrolPoints[0] != null && patrolPoints[patrolPoints.Count - 1] != null)
                {
                    Gizmos.color = new Color(0, 1, 1, 0.5f);
                    Gizmos.DrawLine(patrolPoints[patrolPoints.Count - 1].position, patrolPoints[0].position);
                }
            }

            // Field of view
            if (fieldOfView < 360f)
            {
                Gizmos.color = new Color(0, 1, 0, 0.3f);
                Vector3 forward = use2DMovement ? transform.right : transform.forward;
                Vector3 leftBoundary = Quaternion.Euler(0, use2DMovement ? 0 : -fieldOfView / 2, use2DMovement ? fieldOfView / 2 : 0) * forward;
                Vector3 rightBoundary = Quaternion.Euler(0, use2DMovement ? 0 : fieldOfView / 2, use2DMovement ? -fieldOfView / 2 : 0) * forward;

                Gizmos.DrawLine(transform.position, transform.position + leftBoundary * detectionRadius);
                Gizmos.DrawLine(transform.position, transform.position + rightBoundary * detectionRadius);
            }
        }
#endif
    }
}
