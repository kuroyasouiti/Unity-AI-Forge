using UnityEngine;
using System.Collections;

namespace UnityAIForge.GameKit
{
    /// <summary>
    /// Simple AI controller that autonomously controls a GameKitActor.
    /// Provides basic behaviors like patrol, follow, and idle.
    /// </summary>
    [RequireComponent(typeof(GameKitActor))]
    [AddComponentMenu("SkillForUnity/GameKit/Simple AI")]
    public class GameKitSimpleAI : MonoBehaviour
    {
        [Header("AI Behavior")]
        [SerializeField] private AIBehaviorType behaviorType = AIBehaviorType.Idle;
        
        [Header("Patrol Settings")]
        [Tooltip("Patrol waypoints (for Patrol behavior)")]
        [SerializeField] private Transform[] patrolPoints;
        [SerializeField] private float waypointReachDistance = 0.5f;
        [SerializeField] private float waitTimeAtWaypoint = 2f;
        
        [Header("Follow Settings")]
        [Tooltip("Target to follow (for Follow behavior)")]
        [SerializeField] private Transform followTarget;
        [SerializeField] private float followDistance = 5f;
        [SerializeField] private float stopDistance = 2f;
        
        [Header("Wander Settings")]
        [SerializeField] private float wanderRadius = 10f;
        [SerializeField] private float wanderInterval = 3f;

        private GameKitActor actor;
        private int currentPatrolIndex = 0;
        private bool isWaiting = false;
        private Vector3 wanderTarget;

        public enum AIBehaviorType
        {
            Idle,
            Patrol,
            Follow,
            Wander
        }

        private void Awake()
        {
            actor = GetComponent<GameKitActor>();
        }

        private void Start()
        {
            if (behaviorType == AIBehaviorType.Wander)
            {
                StartCoroutine(WanderRoutine());
            }
        }

        private void Update()
        {
            if (actor == null) return;

            switch (behaviorType)
            {
                case AIBehaviorType.Patrol:
                    UpdatePatrol();
                    break;
                case AIBehaviorType.Follow:
                    UpdateFollow();
                    break;
                case AIBehaviorType.Wander:
                    UpdateWander();
                    break;
            }
        }

        private void UpdatePatrol()
        {
            if (patrolPoints == null || patrolPoints.Length == 0 || isWaiting)
                return;

            Transform targetPoint = patrolPoints[currentPatrolIndex];
            if (targetPoint == null)
                return;

            Vector3 direction = (targetPoint.position - transform.position).normalized;
            actor.SendMoveInput(direction);

            // Check if reached waypoint
            float distance = Vector3.Distance(transform.position, targetPoint.position);
            if (distance < waypointReachDistance)
            {
                StartCoroutine(WaitAtWaypoint());
            }
        }

        private IEnumerator WaitAtWaypoint()
        {
            isWaiting = true;
            actor.SendMoveInput(Vector3.zero); // Stop movement
            
            yield return new WaitForSeconds(waitTimeAtWaypoint);
            
            currentPatrolIndex = (currentPatrolIndex + 1) % patrolPoints.Length;
            isWaiting = false;
        }

        private void UpdateFollow()
        {
            if (followTarget == null)
                return;

            float distance = Vector3.Distance(transform.position, followTarget.position);
            
            if (distance > followDistance || distance < stopDistance)
            {
                actor.SendMoveInput(Vector3.zero);
                return;
            }

            Vector3 direction = (followTarget.position - transform.position).normalized;
            actor.SendMoveInput(direction);
        }

        private void UpdateWander()
        {
            if (wanderTarget == Vector3.zero)
                return;

            Vector3 direction = (wanderTarget - transform.position).normalized;
            actor.SendMoveInput(direction);

            // Check if reached wander target
            float distance = Vector3.Distance(transform.position, wanderTarget);
            if (distance < waypointReachDistance)
            {
                wanderTarget = Vector3.zero;
            }
        }

        private IEnumerator WanderRoutine()
        {
            while (behaviorType == AIBehaviorType.Wander)
            {
                yield return new WaitForSeconds(wanderInterval);
                
                // Generate random wander target
                Vector2 randomCircle = Random.insideUnitCircle * wanderRadius;
                wanderTarget = transform.position + new Vector3(randomCircle.x, 0, randomCircle.y);
            }
        }

        public void SetBehavior(AIBehaviorType newBehavior)
        {
            behaviorType = newBehavior;
            
            if (newBehavior == AIBehaviorType.Wander)
            {
                StopAllCoroutines();
                StartCoroutine(WanderRoutine());
            }
        }

        public void SetFollowTarget(Transform target)
        {
            followTarget = target;
            behaviorType = AIBehaviorType.Follow;
        }

        public void SetPatrolPoints(Transform[] points)
        {
            patrolPoints = points;
            currentPatrolIndex = 0;
            behaviorType = AIBehaviorType.Patrol;
        }
    }
}

