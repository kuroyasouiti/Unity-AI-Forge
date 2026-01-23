using UnityEngine;
using UnityEngine.Tilemaps;
using System.Collections;

namespace UnityAIForge.GameKit
{
    /// <summary>
    /// Tile-based grid movement component for 2D games.
    /// Moves in discrete grid units with smooth interpolation.
    /// Supports Unity Tilemap integration for grid size and collision detection.
    /// Implements IMovementStrategy for unified movement handling.
    /// </summary>
    [RequireComponent(typeof(GameKitActor))]
    public class TileGridMovement : MonoBehaviour, IMovementStrategy
    {
        [Header("Tilemap Reference")]
        [Tooltip("Reference to the Grid component (parent of Tilemaps). If set, gridSize is derived from Grid.cellSize.")]
        [SerializeField] private Grid targetGrid;

        [Tooltip("Tilemap used for collision/obstacle detection. If set, tiles with colliders block movement.")]
        [SerializeField] private Tilemap obstacleTilemap;

        [Tooltip("Tilemap that defines walkable areas. If set, only tiles present here are walkable.")]
        [SerializeField] private Tilemap walkableTilemap;

        [Header("Grid Settings")]
        [Tooltip("Size of each grid cell in world units (used when targetGrid is not set)")]
        [SerializeField] private float gridSize = 1f;

        [Tooltip("Time to move from one tile to another")]
        [SerializeField] private float moveSpeed = 0.2f;

        [Tooltip("Snap to grid on start")]
        [SerializeField] private bool snapToGridOnStart = true;

        [Header("Movement Settings")]
        [Tooltip("Allow diagonal movement")]
        [SerializeField] private bool allowDiagonal = false;

        [Tooltip("Queue next move while moving")]
        [SerializeField] private bool allowMoveQueue = true;

        [Tooltip("Layer mask for physics collision detection (in addition to Tilemap)")]
        [SerializeField] private LayerMask obstacleLayer;

        [Header("Animation")]
        [Tooltip("Use smooth interpolation")]
        [SerializeField] private bool smoothMovement = true;

        [Tooltip("Animation curve for movement")]
        [SerializeField] private AnimationCurve movementCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

        private GameKitActor actor;
        private Vector3 targetPosition;
        private Vector3 queuedDirection = Vector3.zero;
        private bool isMoving = false;
        private Coroutine moveCoroutine;

        /// <summary>
        /// Current grid position in cell coordinates.
        /// </summary>
        public Vector2Int GridPosition => WorldToGrid(transform.position);

        /// <summary>
        /// Whether the actor is currently moving.
        /// </summary>
        public bool IsMoving => isMoving;

        /// <summary>
        /// Movement speed (time to move between tiles).
        /// IMovementStrategy implementation.
        /// </summary>
        float IMovementStrategy.MoveSpeed
        {
            get => moveSpeed;
            set => moveSpeed = value;
        }

        /// <summary>
        /// Effective grid cell size (from Grid component or manual setting).
        /// </summary>
        public float GridSize => GetEffectiveGridSize();

        /// <summary>
        /// The referenced Grid component.
        /// </summary>
        public Grid TargetGrid
        {
            get => targetGrid;
            set => targetGrid = value;
        }

        /// <summary>
        /// The Tilemap used for obstacle detection.
        /// </summary>
        public Tilemap ObstacleTilemap
        {
            get => obstacleTilemap;
            set => obstacleTilemap = value;
        }

        /// <summary>
        /// The Tilemap that defines walkable areas.
        /// </summary>
        public Tilemap WalkableTilemap
        {
            get => walkableTilemap;
            set => walkableTilemap = value;
        }

        private float GetEffectiveGridSize()
        {
            if (targetGrid != null)
            {
                // Use the larger of X or Y cell size for consistent movement
                return Mathf.Max(targetGrid.cellSize.x, targetGrid.cellSize.y);
            }
            return gridSize;
        }

        private Vector3 GetEffectiveCellSize()
        {
            if (targetGrid != null)
            {
                return targetGrid.cellSize;
            }
            return new Vector3(gridSize, gridSize, 0);
        }

        private void Awake()
        {
            actor = GetComponent<GameKitActor>();
            targetPosition = transform.position;
            
            // Subscribe to actor input events early for editor tests
            if (actor != null && actor.OnMoveInput != null)
            {
                actor.OnMoveInput.AddListener(HandleMoveInput);
            }
        }

        private void Start()
        {
            if (snapToGridOnStart)
            {
                SnapToGrid();
            }

            // Ensure subscription (defensive programming)
            if (actor != null && actor.OnMoveInput != null)
            {
                // Remove first to avoid duplicate listeners
                actor.OnMoveInput.RemoveListener(HandleMoveInput);
                actor.OnMoveInput.AddListener(HandleMoveInput);
            }
        }

        private void OnDestroy()
        {
            if (actor != null)
            {
                actor.OnMoveInput.RemoveListener(HandleMoveInput);
            }
        }

        /// <summary>
        /// Handles move input from the actor hub.
        /// IMovementStrategy implementation.
        /// </summary>
        public void HandleMoveInput(Vector3 direction)
        {
            // Normalize to grid directions
            Vector2Int gridDirection = NormalizeToGridDirection(direction);
            
            if (gridDirection == Vector2Int.zero)
                return;

            if (isMoving)
            {
                if (allowMoveQueue)
                {
                    queuedDirection = new Vector3(gridDirection.x, gridDirection.y, 0);
                }
            }
            else
            {
                TryMove(gridDirection);
            }
        }

        /// <summary>
        /// Attempts to move in the specified grid direction.
        /// </summary>
        public bool TryMove(Vector2Int direction)
        {
            if (isMoving)
                return false;

            Vector3 newPosition = GridToWorld(GridPosition + direction);
            
            // Check for obstacles
            if (IsBlocked(newPosition))
                return false;

            // Start movement
            targetPosition = newPosition;
            
            if (smoothMovement)
            {
                if (moveCoroutine != null)
                    StopCoroutine(moveCoroutine);
                moveCoroutine = StartCoroutine(SmoothMove(targetPosition));
            }
            else
            {
                transform.position = targetPosition;
            }

            return true;
        }

        /// <summary>
        /// Smoothly interpolates to target position.
        /// </summary>
        private IEnumerator SmoothMove(Vector3 target)
        {
            isMoving = true;
            Vector3 startPosition = transform.position;
            float elapsed = 0f;

            while (elapsed < moveSpeed)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / moveSpeed);
                float curveValue = movementCurve.Evaluate(t);
                transform.position = Vector3.Lerp(startPosition, target, curveValue);
                yield return null;
            }

            transform.position = target;
            isMoving = false;

            // Process queued move
            if (queuedDirection != Vector3.zero)
            {
                Vector2Int queuedDir = new Vector2Int(
                    Mathf.RoundToInt(queuedDirection.x),
                    Mathf.RoundToInt(queuedDirection.y)
                );
                queuedDirection = Vector3.zero;
                TryMove(queuedDir);
            }
        }

        /// <summary>
        /// Snaps current position to nearest grid cell.
        /// </summary>
        public void SnapToGrid()
        {
            Vector2Int gridPos = GridPosition;
            transform.position = GridToWorld(gridPos);
            targetPosition = transform.position;
        }

        /// <summary>
        /// Checks if a position is blocked by obstacles.
        /// Considers: obstacle tilemap, walkable tilemap, and physics colliders.
        /// </summary>
        private bool IsBlocked(Vector3 worldPosition)
        {
            // Convert world position to cell position for tilemap checks
            Vector3Int cellPosition = WorldToCell(worldPosition);

            // Check walkable tilemap (if set, position must have a tile to be walkable)
            if (walkableTilemap != null)
            {
                if (!walkableTilemap.HasTile(cellPosition))
                {
                    return true; // No walkable tile = blocked
                }
            }

            // Check obstacle tilemap (if set, position with a tile is blocked)
            if (obstacleTilemap != null)
            {
                if (obstacleTilemap.HasTile(cellPosition))
                {
                    return true; // Obstacle tile present = blocked
                }
            }

            // Check physics colliders
            if (obstacleLayer != 0)
            {
                float effectiveSize = GetEffectiveGridSize();
                Collider2D hit = Physics2D.OverlapCircle(worldPosition, effectiveSize * 0.4f, obstacleLayer);
                if (hit != null)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Converts world position to cell position using Grid or manual calculation.
        /// </summary>
        private Vector3Int WorldToCell(Vector3 worldPosition)
        {
            if (targetGrid != null)
            {
                return targetGrid.WorldToCell(worldPosition);
            }

            // Manual calculation when no Grid is set
            return new Vector3Int(
                Mathf.FloorToInt((worldPosition.x / gridSize) + 0.5f),
                Mathf.FloorToInt((worldPosition.y / gridSize) + 0.5f),
                0
            );
        }

        /// <summary>
        /// Converts cell position to world position using Grid or manual calculation.
        /// </summary>
        private Vector3 CellToWorld(Vector3Int cellPosition)
        {
            if (targetGrid != null)
            {
                // Get cell center position
                return targetGrid.GetCellCenterWorld(cellPosition);
            }

            // Manual calculation when no Grid is set
            return new Vector3(
                cellPosition.x * gridSize,
                cellPosition.y * gridSize,
                transform.position.z
            );
        }

        /// <summary>
        /// Converts world position to grid coordinates (Vector2Int).
        /// Uses Grid component if available, otherwise uses manual calculation.
        /// </summary>
        private Vector2Int WorldToGrid(Vector3 worldPosition)
        {
            Vector3Int cell = WorldToCell(worldPosition);
            return new Vector2Int(cell.x, cell.y);
        }

        /// <summary>
        /// Converts grid coordinates to world position.
        /// Uses Grid component if available, otherwise uses manual calculation.
        /// </summary>
        private Vector3 GridToWorld(Vector2Int gridPosition)
        {
            Vector3Int cellPosition = new Vector3Int(gridPosition.x, gridPosition.y, 0);
            return CellToWorld(cellPosition);
        }

        /// <summary>
        /// Normalizes input direction to grid direction (4 or 8 directions).
        /// </summary>
        private Vector2Int NormalizeToGridDirection(Vector3 direction)
        {
            if (direction.magnitude < 0.1f)
                return Vector2Int.zero;

            // Get dominant axis
            float absX = Mathf.Abs(direction.x);
            float absY = Mathf.Abs(direction.y);

            if (!allowDiagonal)
            {
                // 4-directional movement
                if (absX > absY)
                {
                    return new Vector2Int(direction.x > 0 ? 1 : -1, 0);
                }
                else
                {
                    return new Vector2Int(0, direction.y > 0 ? 1 : -1);
                }
            }
            else
            {
                // 8-directional movement
                int x = 0, y = 0;
                
                if (absX > 0.3f)
                    x = direction.x > 0 ? 1 : -1;
                
                if (absY > 0.3f)
                    y = direction.y > 0 ? 1 : -1;

                return new Vector2Int(x, y);
            }
        }

        /// <summary>
        /// Teleports to a specific grid position.
        /// </summary>
        public void TeleportToGrid(Vector2Int gridPosition)
        {
            if (moveCoroutine != null)
            {
                StopCoroutine(moveCoroutine);
                isMoving = false;
            }

            transform.position = GridToWorld(gridPosition);
            targetPosition = transform.position;
            queuedDirection = Vector3.zero;
        }

        #region IMovementStrategy Implementation

        /// <summary>
        /// Teleports to a world position (snaps to nearest grid cell).
        /// IMovementStrategy implementation.
        /// </summary>
        public void TeleportTo(Vector3 position)
        {
            if (moveCoroutine != null)
            {
                StopCoroutine(moveCoroutine);
                isMoving = false;
            }

            transform.position = position;
            SnapToGrid();
            queuedDirection = Vector3.zero;
        }

        /// <summary>
        /// Stops any ongoing movement.
        /// IMovementStrategy implementation.
        /// </summary>
        public void StopMovement()
        {
            if (moveCoroutine != null)
            {
                StopCoroutine(moveCoroutine);
                moveCoroutine = null;
            }
            isMoving = false;
            queuedDirection = Vector3.zero;
        }

        /// <summary>
        /// Initializes the movement strategy.
        /// IMovementStrategy implementation.
        /// </summary>
        void IMovementStrategy.Initialize()
        {
            if (snapToGridOnStart)
            {
                SnapToGrid();
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

        /// <summary>
        /// Gets the world position of a grid coordinate.
        /// </summary>
        public Vector3 GetWorldPosition(Vector2Int gridPosition)
        {
            return GridToWorld(gridPosition);
        }

        /// <summary>
        /// Checks if a grid position is blocked.
        /// </summary>
        public bool IsGridPositionBlocked(Vector2Int gridPosition)
        {
            return IsBlocked(GridToWorld(gridPosition));
        }

        /// <summary>
        /// Checks if a specific cell has a tile in the walkable tilemap.
        /// </summary>
        public bool HasWalkableTile(Vector2Int gridPosition)
        {
            if (walkableTilemap == null)
            {
                return true; // No walkable tilemap = all positions walkable (by default)
            }
            return walkableTilemap.HasTile(new Vector3Int(gridPosition.x, gridPosition.y, 0));
        }

        /// <summary>
        /// Checks if a specific cell has a tile in the obstacle tilemap.
        /// </summary>
        public bool HasObstacleTile(Vector2Int gridPosition)
        {
            if (obstacleTilemap == null)
            {
                return false;
            }
            return obstacleTilemap.HasTile(new Vector3Int(gridPosition.x, gridPosition.y, 0));
        }

        /// <summary>
        /// Auto-finds Grid and Tilemaps in scene if not set.
        /// Call this method to automatically configure tilemap references.
        /// </summary>
        public void AutoFindTilemaps()
        {
            if (targetGrid == null)
            {
                targetGrid = FindFirstObjectByType<Grid>();
            }

            if (targetGrid != null && obstacleTilemap == null)
            {
                // Look for obstacle tilemap by common naming patterns
                var tilemaps = targetGrid.GetComponentsInChildren<Tilemap>();
                foreach (var tm in tilemaps)
                {
                    string lowerName = tm.name.ToLowerInvariant();
                    if (lowerName.Contains("obstacle") || lowerName.Contains("collision") ||
                        lowerName.Contains("wall") || lowerName.Contains("block"))
                    {
                        obstacleTilemap = tm;
                        break;
                    }
                }
            }

            if (targetGrid != null && walkableTilemap == null)
            {
                // Look for walkable/ground tilemap by common naming patterns
                var tilemaps = targetGrid.GetComponentsInChildren<Tilemap>();
                foreach (var tm in tilemaps)
                {
                    string lowerName = tm.name.ToLowerInvariant();
                    if (lowerName.Contains("walkable") || lowerName.Contains("ground") ||
                        lowerName.Contains("floor") || lowerName.Contains("path"))
                    {
                        walkableTilemap = tm;
                        break;
                    }
                }
            }
        }

        private void OnDrawGizmosSelected()
        {
            // Get effective cell size for drawing
            Vector3 cellSize = GetEffectiveCellSize();

            // Draw current grid cell
            Gizmos.color = Color.green;
            Vector3 center = transform.position;
            if (Application.isPlaying)
            {
                Vector2Int gridPos = GridPosition;
                center = GridToWorld(gridPos);
            }
            else if (targetGrid != null)
            {
                // Snap to grid for preview in editor
                Vector3Int cell = targetGrid.WorldToCell(transform.position);
                center = targetGrid.GetCellCenterWorld(cell);
            }
            Gizmos.DrawWireCube(center, new Vector3(cellSize.x, cellSize.y, 0.1f));

            // Draw target position if moving
            if (isMoving)
            {
                Gizmos.color = Color.yellow;
                Gizmos.DrawWireCube(targetPosition, new Vector3(cellSize.x, cellSize.y, 0.1f));
            }

            // Draw grid around current position
            Gizmos.color = new Color(0.5f, 0.5f, 0.5f, 0.3f);
            for (int x = -2; x <= 2; x++)
            {
                for (int y = -2; y <= 2; y++)
                {
                    Vector3 gridCenter = center + new Vector3(x * cellSize.x, y * cellSize.y, 0);
                    Gizmos.DrawWireCube(gridCenter, new Vector3(cellSize.x, cellSize.y, 0.1f));
                }
            }

            // Draw obstacle indicators if tilemaps are set
            if (Application.isPlaying && (obstacleTilemap != null || walkableTilemap != null))
            {
                Vector2Int currentGrid = GridPosition;
                for (int x = -2; x <= 2; x++)
                {
                    for (int y = -2; y <= 2; y++)
                    {
                        Vector2Int checkPos = currentGrid + new Vector2Int(x, y);
                        if (IsGridPositionBlocked(checkPos))
                        {
                            Gizmos.color = new Color(1f, 0f, 0f, 0.3f);
                            Vector3 blockedCenter = GridToWorld(checkPos);
                            Gizmos.DrawCube(blockedCenter, new Vector3(cellSize.x * 0.9f, cellSize.y * 0.9f, 0.05f));
                        }
                    }
                }
            }
        }
    }
}

