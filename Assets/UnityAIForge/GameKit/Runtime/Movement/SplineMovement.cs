using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace UnityAIForge.GameKit
{
    /// <summary>
    /// Spline/rail-based movement component for 2.5D games, rail shooters, and side-scrollers.
    /// Moves along a curved path defined by control points (Catmull-Rom spline).
    /// Implements IMovementStrategy for unified movement handling.
    /// </summary>
    [RequireComponent(typeof(GameKitActor))]
    public class SplineMovement : MonoBehaviour, IMovementStrategy
    {
        [Header("Spline Settings")]
        [Tooltip("Control points defining the spline path")]
        [SerializeField] private Transform[] controlPoints;
        
        [Tooltip("Use closed loop (connect last point to first)")]
        [SerializeField] private bool closedLoop = false;
        
        [Tooltip("Spline resolution (segments per control point pair)")]
        [SerializeField] private int resolution = 10;

        [Header("Movement Settings")]
        [Tooltip("Movement speed along spline (units per second)")]
        [SerializeField] private float moveSpeed = 5f;
        
        [Tooltip("Acceleration when starting movement")]
        [SerializeField] private float acceleration = 2f;
        
        [Tooltip("Deceleration when stopping")]
        [SerializeField] private float deceleration = 2f;
        
        [Tooltip("Auto-start moving on Start()")]
        [SerializeField] private bool autoStart = false;
        
        [Tooltip("Auto-restart when reaching end (if not closed loop)")]
        [SerializeField] private bool autoRestart = false;

        [Header("Control Settings")]
        [Tooltip("Allow manual speed control via input")]
        [SerializeField] private bool allowManualControl = false;
        
        [Tooltip("Allow backward movement")]
        [SerializeField] private bool allowBackwardMovement = false;
        
        [Tooltip("Offset from spline path (for multiple lanes)")]
        [SerializeField] private Vector3 lateralOffset = Vector3.zero;

        [Header("Rotation Settings")]
        [Tooltip("Auto-rotate to face movement direction")]
        [SerializeField] private bool autoRotate = true;
        
        [Tooltip("Rotation speed (degrees per second)")]
        [SerializeField] private float rotationSpeed = 360f;
        
        [Tooltip("Rotation axis (for 2D sprites)")]
        [SerializeField] private RotationAxis rotationAxis = RotationAxis.Z;

        [Header("Visualization")]
        [Tooltip("Show spline path in Scene view")]
        [SerializeField] private bool showDebugPath = true;
        
        [Tooltip("Path visualization color")]
        [SerializeField] private Color pathColor = Color.cyan;

        private GameKitActor actor;
        private float currentDistance = 0f; // Distance traveled along spline
        private float totalSplineLength = 0f;
        private float currentSpeed = 0f;
        private bool isMoving = false;
        private List<Vector3> splinePoints;
        private float inputSpeed = 0f; // Speed modifier from input

        // Public properties
        public float Progress => totalSplineLength > 0 ? currentDistance / totalSplineLength : 0f;
        public bool IsMoving => isMoving;
        public float CurrentSpeed => currentSpeed;
        public float SplineLength => totalSplineLength;

        /// <summary>
        /// Movement speed (units per second).
        /// IMovementStrategy implementation.
        /// </summary>
        float IMovementStrategy.MoveSpeed
        {
            get => moveSpeed;
            set => moveSpeed = value;
        }

        public enum RotationAxis
        {
            None,
            X,
            Y,
            Z,
            All
        }

        private void Awake()
        {
            actor = GetComponent<GameKitActor>();
            
            // Subscribe to actor input events
            if (actor != null && actor.OnMoveInput != null)
            {
                actor.OnMoveInput.AddListener(HandleMoveInput);
            }
        }

        private void Start()
        {
            // Build spline path
            RebuildSpline();
            
            // Snap to start position
            if (splinePoints != null && splinePoints.Count > 0)
            {
                transform.position = splinePoints[0] + lateralOffset;
            }

            if (autoStart)
            {
                StartMoving();
            }

            // Ensure subscription
            if (actor != null && actor.OnMoveInput != null)
            {
                actor.OnMoveInput.RemoveListener(HandleMoveInput);
                actor.OnMoveInput.AddListener(HandleMoveInput);
            }
        }

        private void OnDestroy()
        {
            if (actor != null && actor.OnMoveInput != null)
            {
                actor.OnMoveInput.RemoveListener(HandleMoveInput);
            }
        }

        private void Update()
        {
            if (!isMoving || splinePoints == null || splinePoints.Count == 0)
                return;

            // Apply acceleration/deceleration
            float targetSpeed = moveSpeed + inputSpeed;
            if (Mathf.Abs(targetSpeed) > 0.1f)
            {
                currentSpeed = Mathf.MoveTowards(currentSpeed, targetSpeed, acceleration * Time.deltaTime);
            }
            else
            {
                currentSpeed = Mathf.MoveTowards(currentSpeed, 0f, deceleration * Time.deltaTime);
            }

            // Move along spline
            currentDistance += currentSpeed * Time.deltaTime;

            // Handle end of spline
            if (currentDistance >= totalSplineLength)
            {
                if (closedLoop)
                {
                    currentDistance = currentDistance % totalSplineLength;
                }
                else if (autoRestart)
                {
                    currentDistance = 0f;
                }
                else
                {
                    currentDistance = totalSplineLength;
                    isMoving = false;
                    currentSpeed = 0f;
                }
            }
            else if (currentDistance < 0f)
            {
                if (closedLoop)
                {
                    currentDistance = totalSplineLength + currentDistance;
                }
                else
                {
                    currentDistance = 0f;
                    isMoving = false;
                    currentSpeed = 0f;
                }
            }

            // Update position
            Vector3 position = GetPointAtDistance(currentDistance);
            transform.position = position + lateralOffset;

            // Update rotation
            if (autoRotate && currentSpeed != 0f)
            {
                Vector3 forward = GetTangentAtDistance(currentDistance);
                if (currentSpeed < 0f)
                    forward = -forward;
                
                UpdateRotation(forward);
            }
        }

        /// <summary>
        /// Handles move input from the actor hub.
        /// IMovementStrategy implementation.
        /// </summary>
        public void HandleMoveInput(Vector3 direction)
        {
            if (!allowManualControl)
                return;

            // Use input magnitude to control speed
            float inputMagnitude = direction.magnitude;
            
            // For 2D games, use x input; for 3D, use z input
            float speedModifier = 0f;
            if (actor.Behavior == GameKitActor.BehaviorProfile.SplineMovement)
            {
                // Primarily use forward/backward input
                speedModifier = direction.z != 0f ? direction.z : direction.y;
            }

            inputSpeed = speedModifier * moveSpeed;

            // Start moving if not already
            if (!isMoving && Mathf.Abs(speedModifier) > 0.1f)
            {
                StartMoving();
            }
        }

        /// <summary>
        /// Starts movement along the spline.
        /// </summary>
        public void StartMoving()
        {
            isMoving = true;
        }

        /// <summary>
        /// Stops movement along the spline.
        /// </summary>
        public void StopMoving()
        {
            isMoving = false;
            currentSpeed = 0f;
            inputSpeed = 0f;
        }

        /// <summary>
        /// Sets the current position on the spline (0 to 1).
        /// </summary>
        public void SetProgress(float progress)
        {
            progress = Mathf.Clamp01(progress);
            currentDistance = progress * totalSplineLength;
            
            if (splinePoints != null && splinePoints.Count > 0)
            {
                Vector3 position = GetPointAtDistance(currentDistance);
                transform.position = position + lateralOffset;
            }
        }

        /// <summary>
        /// Sets movement speed.
        /// </summary>
        public void SetSpeed(float speed)
        {
            moveSpeed = speed;
        }

        /// <summary>
        /// Sets lateral offset from spline (for lane-based games).
        /// </summary>
        public void SetLateralOffset(Vector3 offset)
        {
            lateralOffset = offset;
        }

        /// <summary>
        /// Rebuilds the spline from control points.
        /// </summary>
        public void RebuildSpline()
        {
            if (controlPoints == null || controlPoints.Length < 2)
            {
                Debug.LogWarning($"[SplineMovement] Need at least 2 control points for {gameObject.name}");
                splinePoints = new List<Vector3>();
                totalSplineLength = 0f;
                return;
            }

            splinePoints = GenerateCatmullRomSpline(controlPoints, resolution, closedLoop);
            totalSplineLength = CalculateSplineLength(splinePoints);
        }

        /// <summary>
        /// Generates a Catmull-Rom spline from control points.
        /// </summary>
        private List<Vector3> GenerateCatmullRomSpline(Transform[] points, int resolution, bool closed)
        {
            var result = new List<Vector3>();
            int count = points.Length;

            if (count < 2)
                return result;

            int segments = closed ? count : count - 1;

            for (int i = 0; i < segments; i++)
            {
                int p0Index = closed ? (i - 1 + count) % count : Mathf.Max(0, i - 1);
                int p1Index = i;
                int p2Index = (i + 1) % count;
                int p3Index = closed ? (i + 2) % count : Mathf.Min(count - 1, i + 2);

                Vector3 p0 = points[p0Index].position;
                Vector3 p1 = points[p1Index].position;
                Vector3 p2 = points[p2Index].position;
                Vector3 p3 = points[p3Index].position;

                for (int j = 0; j < resolution; j++)
                {
                    float t = j / (float)resolution;
                    Vector3 point = CalculateCatmullRomPoint(p0, p1, p2, p3, t);
                    result.Add(point);
                }
            }

            // Add final point
            if (!closed && points.Length > 0)
            {
                result.Add(points[points.Length - 1].position);
            }

            return result;
        }

        private Vector3 CalculateCatmullRomPoint(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, float t)
        {
            // Catmull-Rom spline formula
            float t2 = t * t;
            float t3 = t2 * t;

            return 0.5f * (
                (2f * p1) +
                (-p0 + p2) * t +
                (2f * p0 - 5f * p1 + 4f * p2 - p3) * t2 +
                (-p0 + 3f * p1 - 3f * p2 + p3) * t3
            );
        }

        private float CalculateSplineLength(List<Vector3> points)
        {
            float length = 0f;
            for (int i = 1; i < points.Count; i++)
            {
                length += Vector3.Distance(points[i - 1], points[i]);
            }
            return length;
        }

        private Vector3 GetPointAtDistance(float distance)
        {
            if (splinePoints == null || splinePoints.Count == 0)
                return transform.position;

            if (distance <= 0f)
                return splinePoints[0];

            float accumulated = 0f;
            for (int i = 1; i < splinePoints.Count; i++)
            {
                float segmentLength = Vector3.Distance(splinePoints[i - 1], splinePoints[i]);
                
                if (accumulated + segmentLength >= distance)
                {
                    float t = (distance - accumulated) / segmentLength;
                    return Vector3.Lerp(splinePoints[i - 1], splinePoints[i], t);
                }
                
                accumulated += segmentLength;
            }

            return splinePoints[splinePoints.Count - 1];
        }

        private Vector3 GetTangentAtDistance(float distance)
        {
            if (splinePoints == null || splinePoints.Count < 2)
                return Vector3.forward;

            float lookAhead = 0.1f;
            Vector3 currentPoint = GetPointAtDistance(distance);
            Vector3 nextPoint = GetPointAtDistance(distance + lookAhead);
            
            return (nextPoint - currentPoint).normalized;
        }

        private void UpdateRotation(Vector3 forward)
        {
            if (forward.sqrMagnitude < 0.001f)
                return;

            Quaternion targetRotation = Quaternion.identity;

            switch (rotationAxis)
            {
                case RotationAxis.None:
                    return;

                case RotationAxis.X:
                    // 2D side view (rotate around X axis)
                    float angleX = Mathf.Atan2(forward.y, forward.z) * Mathf.Rad2Deg;
                    targetRotation = Quaternion.Euler(angleX, 0, 0);
                    break;

                case RotationAxis.Y:
                    // Top-down (rotate around Y axis)
                    float angleY = Mathf.Atan2(forward.x, forward.z) * Mathf.Rad2Deg;
                    targetRotation = Quaternion.Euler(0, angleY, 0);
                    break;

                case RotationAxis.Z:
                    // 2D front view (rotate around Z axis)
                    float angleZ = Mathf.Atan2(forward.y, forward.x) * Mathf.Rad2Deg - 90f;
                    targetRotation = Quaternion.Euler(0, 0, angleZ);
                    break;

                case RotationAxis.All:
                    // Full 3D rotation
                    targetRotation = Quaternion.LookRotation(forward);
                    break;
            }

            transform.rotation = Quaternion.RotateTowards(
                transform.rotation,
                targetRotation,
                rotationSpeed * Time.deltaTime
            );
        }

        /// <summary>
        /// Gets a point on the spline at normalized position (0-1).
        /// </summary>
        public Vector3 GetPointAtProgress(float progress)
        {
            progress = Mathf.Clamp01(progress);
            return GetPointAtDistance(progress * totalSplineLength);
        }

        /// <summary>
        /// Gets tangent (forward direction) at normalized position (0-1).
        /// </summary>
        public Vector3 GetTangentAtProgress(float progress)
        {
            progress = Mathf.Clamp01(progress);
            return GetTangentAtDistance(progress * totalSplineLength);
        }

        /// <summary>
        /// Sets control points programmatically.
        /// </summary>
        public void SetControlPoints(Transform[] points)
        {
            controlPoints = points;
            RebuildSpline();
        }

        /// <summary>
        /// Teleports to a specific progress on the spline.
        /// </summary>
        public void TeleportToProgress(float progress)
        {
            SetProgress(progress);
            currentSpeed = 0f;
        }

        /// <summary>
        /// Reverses movement direction.
        /// </summary>
        public void ReverseDirection()
        {
            if (allowBackwardMovement)
            {
                currentSpeed = -currentSpeed;
                inputSpeed = -inputSpeed;
            }
        }

        #region IMovementStrategy Implementation

        /// <summary>
        /// Stops any ongoing movement.
        /// IMovementStrategy implementation.
        /// </summary>
        public void StopMovement()
        {
            StopMoving();
        }

        /// <summary>
        /// Teleports to a world position (finds nearest point on spline).
        /// IMovementStrategy implementation.
        /// </summary>
        public void TeleportTo(Vector3 position)
        {
            // Find the closest point on the spline to the target position
            if (splinePoints == null || splinePoints.Count == 0)
            {
                transform.position = position;
                return;
            }

            float closestDistance = float.MaxValue;
            float closestT = 0f;
            float accumulatedDist = 0f;

            for (int i = 1; i < splinePoints.Count; i++)
            {
                float segmentLength = Vector3.Distance(splinePoints[i - 1], splinePoints[i]);

                // Check distance to this segment
                Vector3 closestPoint = ClosestPointOnSegment(splinePoints[i - 1], splinePoints[i], position);
                float dist = Vector3.Distance(position, closestPoint);

                if (dist < closestDistance)
                {
                    closestDistance = dist;
                    float t = Vector3.Distance(splinePoints[i - 1], closestPoint) / segmentLength;
                    closestT = accumulatedDist + segmentLength * t;
                }

                accumulatedDist += segmentLength;
            }

            currentDistance = closestT;
            currentSpeed = 0f;
            inputSpeed = 0f;
            transform.position = GetPointAtDistance(currentDistance) + lateralOffset;
        }

        private Vector3 ClosestPointOnSegment(Vector3 a, Vector3 b, Vector3 p)
        {
            Vector3 ab = b - a;
            float t = Mathf.Clamp01(Vector3.Dot(p - a, ab) / ab.sqrMagnitude);
            return a + t * ab;
        }

        /// <summary>
        /// Initializes the movement strategy.
        /// IMovementStrategy implementation.
        /// </summary>
        void IMovementStrategy.Initialize()
        {
            RebuildSpline();
            if (splinePoints != null && splinePoints.Count > 0)
            {
                transform.position = splinePoints[0] + lateralOffset;
            }
        }

        /// <summary>
        /// Cleans up resources.
        /// IMovementStrategy implementation.
        /// </summary>
        void IMovementStrategy.Cleanup()
        {
            StopMovement();
            if (actor != null && actor.OnMoveInput != null)
            {
                actor.OnMoveInput.RemoveListener(HandleMoveInput);
            }
        }

        #endregion

        private void OnDrawGizmos()
        {
            if (!showDebugPath || controlPoints == null || controlPoints.Length < 2)
                return;

            // Draw control points
            Gizmos.color = Color.yellow;
            foreach (var point in controlPoints)
            {
                if (point != null)
                {
                    Gizmos.DrawWireSphere(point.position, 0.2f);
                }
            }

            // Draw spline path
            if (splinePoints != null && splinePoints.Count > 1)
            {
                Gizmos.color = pathColor;
                for (int i = 1; i < splinePoints.Count; i++)
                {
                    Gizmos.DrawLine(splinePoints[i - 1], splinePoints[i]);
                }

                if (closedLoop && splinePoints.Count > 0)
                {
                    Gizmos.DrawLine(splinePoints[splinePoints.Count - 1], splinePoints[0]);
                }
            }
            else if (Application.isEditor && !Application.isPlaying)
            {
                // Draw preview in editor
                var previewPoints = GenerateCatmullRomSpline(controlPoints, resolution, closedLoop);
                Gizmos.color = pathColor;
                for (int i = 1; i < previewPoints.Count; i++)
                {
                    Gizmos.DrawLine(previewPoints[i - 1], previewPoints[i]);
                }
            }

            // Draw current position
            if (Application.isPlaying)
            {
                Gizmos.color = Color.red;
                Gizmos.DrawWireSphere(transform.position, 0.3f);
            }
        }

        private void OnDrawGizmosSelected()
        {
            if (controlPoints == null || controlPoints.Length < 2)
                return;

            // Draw control point connections
            Gizmos.color = new Color(1f, 0.5f, 0f, 0.5f);
            for (int i = 1; i < controlPoints.Length; i++)
            {
                if (controlPoints[i - 1] != null && controlPoints[i] != null)
                {
                    Gizmos.DrawLine(controlPoints[i - 1].position, controlPoints[i].position);
                }
            }

            if (closedLoop && controlPoints.Length > 2)
            {
                if (controlPoints[controlPoints.Length - 1] != null && controlPoints[0] != null)
                {
                    Gizmos.DrawLine(controlPoints[controlPoints.Length - 1].position, controlPoints[0].position);
                }
            }
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            // Rebuild spline when properties change in editor
            if (controlPoints != null && controlPoints.Length >= 2)
            {
                splinePoints = GenerateCatmullRomSpline(controlPoints, resolution, closedLoop);
                totalSplineLength = CalculateSplineLength(splinePoints);
            }
        }
#endif
    }
}

