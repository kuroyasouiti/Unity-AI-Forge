using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace UnityAIForge.GameKit
{
    /// <summary>
    /// Waypoint path follower for NPCs, moving platforms, and other objects.
    /// Supports various movement modes with speed and wait time configuration.
    /// </summary>
    public class GameKitWaypoint : MonoBehaviour
    {
        #region Enums

        public enum PathMode
        {
            Once,
            Loop,
            PingPong
        }

        public enum MovementType
        {
            Transform,
            Rigidbody,
            Rigidbody2D
        }

        public enum RotationMode
        {
            None,
            LookAtTarget,
            AlignToPath
        }

        #endregion

        #region Serialized Fields

        [Header("Identification")]
        [SerializeField] private string waypointId;

        [Header("Path Settings")]
        [SerializeField] private List<Transform> waypoints = new List<Transform>();
        [SerializeField] private PathMode pathMode = PathMode.Loop;
        [SerializeField] private bool autoStart = true;
        [SerializeField] private bool useLocalSpace = false;

        [Header("Movement")]
        [SerializeField] private MovementType movementType = MovementType.Transform;
        [SerializeField] private float moveSpeed = 5f;
        [SerializeField] private float rotationSpeed = 360f;
        [SerializeField] private RotationMode rotationMode = RotationMode.LookAtTarget;

        [Header("Timing")]
        [SerializeField] private float waitTimeAtPoint = 0f;
        [SerializeField] private List<float> waitTimesPerPoint = new List<float>();
        [SerializeField] private float startDelay = 0f;

        [Header("Smoothing")]
        [SerializeField] private bool smoothMovement = false;
        [SerializeField] private float smoothTime = 0.3f;
        [SerializeField] private float arrivalThreshold = 0.1f;

        [Header("Events")]
        public UnityEvent<int> OnReachWaypoint;
        public UnityEvent OnPathComplete;
        public UnityEvent OnPathStart;
        public UnityEvent<int> OnDirectionChanged;

        #endregion

        #region Private Fields

        private int _currentIndex;
        private int _direction = 1;
        private bool _isMoving;
        private bool _isWaiting;
        private float _waitTimer;
        private float _startTimer;
        private Vector3 _velocity;
        private Rigidbody _rigidbody;
        private Rigidbody2D _rigidbody2D;
        private bool _pathCompleted;
        private List<Vector3> _waypointPositions = new List<Vector3>();

        #endregion

        #region Properties

        public string WaypointId
        {
            get => waypointId;
            set => waypointId = value;
        }

        public float MoveSpeed
        {
            get => moveSpeed;
            set => moveSpeed = value;
        }

        public PathMode Mode
        {
            get => pathMode;
            set => pathMode = value;
        }

        public float WaitTimeAtPoint
        {
            get => waitTimeAtPoint;
            set => waitTimeAtPoint = value;
        }

        public bool IsMoving => _isMoving;
        public bool IsWaiting => _isWaiting;
        public int CurrentWaypointIndex => _currentIndex;
        public int WaypointCount => waypoints.Count;
        public bool PathCompleted => _pathCompleted;

        public Vector3 CurrentTargetPosition
        {
            get
            {
                if (_waypointPositions.Count == 0) return transform.position;
                return _waypointPositions[_currentIndex];
            }
        }

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            _rigidbody = GetComponent<Rigidbody>();
            _rigidbody2D = GetComponent<Rigidbody2D>();

            if (string.IsNullOrEmpty(waypointId))
            {
                waypointId = $"waypoint_{gameObject.name}_{GetInstanceID()}";
            }

            CacheWaypointPositions();
        }

        private void Start()
        {
            if (autoStart && _waypointPositions.Count > 0)
            {
                if (startDelay > 0)
                {
                    _startTimer = startDelay;
                }
                else
                {
                    StartPath();
                }
            }
        }

        private void Update()
        {
            if (_startTimer > 0)
            {
                _startTimer -= Time.deltaTime;
                if (_startTimer <= 0)
                {
                    StartPath();
                }
                return;
            }

            if (!_isMoving || _waypointPositions.Count == 0) return;

            if (_isWaiting)
            {
                _waitTimer -= Time.deltaTime;
                if (_waitTimer <= 0)
                {
                    _isWaiting = false;
                    MoveToNextWaypoint();
                }
                return;
            }

            if (movementType == MovementType.Transform)
            {
                UpdateTransformMovement();
            }
        }

        private void FixedUpdate()
        {
            if (!_isMoving || _isWaiting || _waypointPositions.Count == 0) return;

            if (movementType == MovementType.Rigidbody && _rigidbody != null)
            {
                UpdateRigidbodyMovement();
            }
            else if (movementType == MovementType.Rigidbody2D && _rigidbody2D != null)
            {
                UpdateRigidbody2DMovement();
            }
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Starts following the path from the current position.
        /// </summary>
        public void StartPath()
        {
            if (_waypointPositions.Count == 0)
            {
                CacheWaypointPositions();
                if (_waypointPositions.Count == 0) return;
            }

            _isMoving = true;
            _pathCompleted = false;
            _currentIndex = 0;
            _direction = 1;
            OnPathStart?.Invoke();
        }

        /// <summary>
        /// Stops the path following.
        /// </summary>
        public void StopPath()
        {
            _isMoving = false;
            _isWaiting = false;

            if (_rigidbody != null)
            {
                _rigidbody.linearVelocity = Vector3.zero;
            }
            if (_rigidbody2D != null)
            {
                _rigidbody2D.linearVelocity = Vector2.zero;
            }
        }

        /// <summary>
        /// Pauses the path following (can be resumed).
        /// </summary>
        public void PausePath()
        {
            _isMoving = false;
        }

        /// <summary>
        /// Resumes the path following.
        /// </summary>
        public void ResumePath()
        {
            if (_waypointPositions.Count > 0)
            {
                _isMoving = true;
            }
        }

        /// <summary>
        /// Resets to the first waypoint.
        /// </summary>
        public void ResetPath()
        {
            StopPath();
            _currentIndex = 0;
            _direction = 1;
            _pathCompleted = false;

            if (_waypointPositions.Count > 0)
            {
                transform.position = _waypointPositions[0];
            }
        }

        /// <summary>
        /// Moves to a specific waypoint index.
        /// </summary>
        public void GoToWaypoint(int index)
        {
            if (index < 0 || index >= _waypointPositions.Count) return;

            _currentIndex = index;
            _isMoving = true;
            _isWaiting = false;
        }

        /// <summary>
        /// Adds a waypoint at runtime.
        /// </summary>
        public void AddWaypoint(Vector3 position)
        {
            _waypointPositions.Add(position);
        }

        /// <summary>
        /// Adds a waypoint transform at runtime.
        /// </summary>
        public void AddWaypoint(Transform waypointTransform)
        {
            waypoints.Add(waypointTransform);
            _waypointPositions.Add(useLocalSpace
                ? waypointTransform.localPosition
                : waypointTransform.position);
        }

        /// <summary>
        /// Clears all waypoints.
        /// </summary>
        public void ClearWaypoints()
        {
            waypoints.Clear();
            _waypointPositions.Clear();
            StopPath();
        }

        /// <summary>
        /// Sets the waypoint positions from a list of Vector3.
        /// </summary>
        public void SetWaypointPositions(List<Vector3> positions)
        {
            _waypointPositions = new List<Vector3>(positions);
            waypoints.Clear();
        }

        /// <summary>
        /// Refreshes cached waypoint positions from transforms.
        /// </summary>
        public void RefreshWaypoints()
        {
            CacheWaypointPositions();
        }

        #endregion

        #region Private Methods

        private void CacheWaypointPositions()
        {
            _waypointPositions.Clear();
            foreach (var wp in waypoints)
            {
                if (wp != null)
                {
                    _waypointPositions.Add(useLocalSpace ? wp.localPosition : wp.position);
                }
            }
        }

        private void UpdateTransformMovement()
        {
            var targetPos = _waypointPositions[_currentIndex];
            var direction = (targetPos - transform.position);
            var distance = direction.magnitude;

            if (distance <= arrivalThreshold)
            {
                OnReachCurrentWaypoint();
                return;
            }

            // Move toward target
            Vector3 newPosition;
            if (smoothMovement)
            {
                newPosition = Vector3.SmoothDamp(transform.position, targetPos, ref _velocity, smoothTime, moveSpeed);
            }
            else
            {
                newPosition = Vector3.MoveTowards(transform.position, targetPos, moveSpeed * Time.deltaTime);
            }

            transform.position = newPosition;

            // Handle rotation
            UpdateRotation(direction.normalized);
        }

        private void UpdateRigidbodyMovement()
        {
            var targetPos = _waypointPositions[_currentIndex];
            var direction = (targetPos - transform.position);
            var distance = direction.magnitude;

            if (distance <= arrivalThreshold)
            {
                _rigidbody.linearVelocity = Vector3.zero;
                OnReachCurrentWaypoint();
                return;
            }

            _rigidbody.linearVelocity = direction.normalized * moveSpeed;
            UpdateRotation(direction.normalized);
        }

        private void UpdateRigidbody2DMovement()
        {
            var targetPos = (Vector2)_waypointPositions[_currentIndex];
            var currentPos = (Vector2)transform.position;
            var direction = (targetPos - currentPos);
            var distance = direction.magnitude;

            if (distance <= arrivalThreshold)
            {
                _rigidbody2D.linearVelocity = Vector2.zero;
                OnReachCurrentWaypoint();
                return;
            }

            _rigidbody2D.linearVelocity = direction.normalized * moveSpeed;
            UpdateRotation2D(direction.normalized);
        }

        private void UpdateRotation(Vector3 direction)
        {
            if (rotationMode == RotationMode.None || direction == Vector3.zero) return;

            Quaternion targetRotation;
            if (rotationMode == RotationMode.LookAtTarget)
            {
                targetRotation = Quaternion.LookRotation(direction);
            }
            else // AlignToPath
            {
                targetRotation = Quaternion.LookRotation(direction, Vector3.up);
            }

            transform.rotation = Quaternion.RotateTowards(
                transform.rotation,
                targetRotation,
                rotationSpeed * Time.deltaTime
            );
        }

        private void UpdateRotation2D(Vector2 direction)
        {
            if (rotationMode == RotationMode.None) return;

            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.Euler(0, 0, angle);
        }

        private void OnReachCurrentWaypoint()
        {
            OnReachWaypoint?.Invoke(_currentIndex);

            float waitTime = GetWaitTimeForCurrentPoint();
            if (waitTime > 0)
            {
                _isWaiting = true;
                _waitTimer = waitTime;
            }
            else
            {
                MoveToNextWaypoint();
            }
        }

        private float GetWaitTimeForCurrentPoint()
        {
            if (waitTimesPerPoint.Count > _currentIndex)
            {
                return waitTimesPerPoint[_currentIndex];
            }
            return waitTimeAtPoint;
        }

        private void MoveToNextWaypoint()
        {
            int nextIndex = _currentIndex + _direction;

            switch (pathMode)
            {
                case PathMode.Once:
                    if (nextIndex >= _waypointPositions.Count)
                    {
                        _isMoving = false;
                        _pathCompleted = true;
                        OnPathComplete?.Invoke();
                        return;
                    }
                    break;

                case PathMode.Loop:
                    if (nextIndex >= _waypointPositions.Count)
                    {
                        nextIndex = 0;
                    }
                    else if (nextIndex < 0)
                    {
                        nextIndex = _waypointPositions.Count - 1;
                    }
                    break;

                case PathMode.PingPong:
                    if (nextIndex >= _waypointPositions.Count)
                    {
                        _direction = -1;
                        nextIndex = _waypointPositions.Count - 2;
                        OnDirectionChanged?.Invoke(_direction);
                    }
                    else if (nextIndex < 0)
                    {
                        _direction = 1;
                        nextIndex = 1;
                        OnDirectionChanged?.Invoke(_direction);
                    }

                    // Handle edge case of only 1-2 waypoints
                    if (nextIndex < 0) nextIndex = 0;
                    if (nextIndex >= _waypointPositions.Count) nextIndex = _waypointPositions.Count - 1;
                    break;
            }

            _currentIndex = nextIndex;
        }

        #endregion

        #region Static Methods

        /// <summary>
        /// Finds a waypoint follower by its ID.
        /// </summary>
        public static GameKitWaypoint FindById(string id)
        {
            var waypoints = FindObjectsByType<GameKitWaypoint>(FindObjectsSortMode.None);
            foreach (var wp in waypoints)
            {
                if (wp.WaypointId == id)
                {
                    return wp;
                }
            }
            return null;
        }

        #endregion

        #region Editor Visualization

#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            if (_waypointPositions == null || _waypointPositions.Count == 0)
            {
                // Draw from transform list in editor
                if (waypoints.Count > 0)
                {
                    Gizmos.color = Color.yellow;
                    for (int i = 0; i < waypoints.Count; i++)
                    {
                        if (waypoints[i] == null) continue;

                        var pos = useLocalSpace ? waypoints[i].localPosition : waypoints[i].position;
                        Gizmos.DrawWireSphere(pos, 0.3f);

                        if (i > 0 && waypoints[i - 1] != null)
                        {
                            var prevPos = useLocalSpace ? waypoints[i - 1].localPosition : waypoints[i - 1].position;
                            Gizmos.DrawLine(prevPos, pos);
                        }
                    }

                    // Draw loop connection
                    if (pathMode == PathMode.Loop && waypoints.Count > 1)
                    {
                        var first = useLocalSpace ? waypoints[0].localPosition : waypoints[0].position;
                        var last = useLocalSpace ? waypoints[waypoints.Count - 1].localPosition : waypoints[waypoints.Count - 1].position;
                        Gizmos.color = Color.cyan;
                        Gizmos.DrawLine(last, first);
                    }
                }
            }
        }
#endif

        #endregion
    }
}
