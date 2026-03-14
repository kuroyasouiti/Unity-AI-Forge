using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace MCP.Editor.Utilities.GraphAnalysis
{
    /// <summary>
    /// Analyzes 3D space using the rule of thirds (3×3×3 grid) to detect
    /// physics object distribution and layout bias in a Unity scene.
    /// Supports both Collider-based (default) and Rigidbody-based detection.
    /// </summary>
    public class SpatialAnalyzer
    {
        #region Result Types

        public class CellInfo
        {
            public int X { get; set; }
            public int Y { get; set; }
            public int Z { get; set; }
            public string Label { get; set; }
            public Bounds CellBounds { get; set; }
            public List<PhysicsObjectInfo> Objects { get; set; } = new List<PhysicsObjectInfo>();

            public int Count => Objects.Count;

            public float OccupancyRate { get; set; }

            public Dictionary<string, object> ToDictionary()
            {
                return new Dictionary<string, object>
                {
                    ["index"] = new[] { X, Y, Z },
                    ["label"] = Label,
                    ["bounds"] = BoundsToDictionary(CellBounds),
                    ["objects"] = Objects.Select(o => o.ToDictionary()).ToList(),
                    ["count"] = Count,
                    ["occupancyRate"] = Math.Round(OccupancyRate, 4)
                };
            }
        }

        public class PhysicsObjectInfo
        {
            public string Path { get; set; }
            public string Tag { get; set; }
            public string Layer { get; set; }
            public Vector3 Position { get; set; }
            public Bounds ObjectBounds { get; set; }
            public bool HasRigidbody { get; set; }
            public bool IsKinematic { get; set; }
            public bool Is2D { get; set; }
            public bool IsTrigger { get; set; }
            public float Mass { get; set; }
            public List<string> ColliderTypes { get; set; } = new List<string>();

            public Dictionary<string, object> ToDictionary()
            {
                var dict = new Dictionary<string, object>
                {
                    ["path"] = Path,
                    ["tag"] = Tag,
                    ["layer"] = Layer,
                    ["position"] = Vec3ToDictionary(Position),
                    ["bounds"] = BoundsToDictionary(ObjectBounds),
                    ["colliderTypes"] = ColliderTypes,
                    ["isTrigger"] = IsTrigger,
                    ["is2D"] = Is2D,
                    ["hasRigidbody"] = HasRigidbody
                };

                if (HasRigidbody)
                {
                    dict["isKinematic"] = IsKinematic;
                    dict["mass"] = Math.Round(Mass, 4);
                }

                return dict;
            }
        }

        public class BiasInfo
        {
            public Vector3 NormalizedCenterOfMass { get; set; }
            public string HorizontalBias { get; set; }
            public string VerticalBias { get; set; }
            public string DepthBias { get; set; }
            public float LeftRightSymmetry { get; set; }
            public float TopBottomSymmetry { get; set; }
            public float FrontBackSymmetry { get; set; }
            public string Distribution { get; set; }
            public float EmptyRate { get; set; }

            public Dictionary<string, object> ToDictionary()
            {
                return new Dictionary<string, object>
                {
                    ["normalizedCenterOfMass"] = Vec3ToDictionary(NormalizedCenterOfMass),
                    ["horizontalBias"] = HorizontalBias,
                    ["verticalBias"] = VerticalBias,
                    ["depthBias"] = DepthBias,
                    ["symmetry"] = new Dictionary<string, object>
                    {
                        ["leftRight"] = Math.Round(LeftRightSymmetry, 4),
                        ["topBottom"] = Math.Round(TopBottomSymmetry, 4),
                        ["frontBack"] = Math.Round(FrontBackSymmetry, 4)
                    },
                    ["distribution"] = Distribution,
                    ["emptyRate"] = Math.Round(EmptyRate, 4)
                };
            }
        }

        public class LayoutResult
        {
            public Bounds SceneBounds { get; set; }
            public int TotalObjects { get; set; }
            public string DetectionMode { get; set; }
            public CellInfo[,,] Grid { get; set; }
            public BiasInfo Bias { get; set; }

            public Dictionary<string, object> ToDictionary()
            {
                var cells = new List<Dictionary<string, object>>();
                for (int z = 0; z < 3; z++)
                    for (int y = 0; y < 3; y++)
                        for (int x = 0; x < 3; x++)
                            cells.Add(Grid[x, y, z].ToDictionary());

                return new Dictionary<string, object>
                {
                    ["bounds"] = BoundsToDictionary(SceneBounds),
                    ["totalObjects"] = TotalObjects,
                    ["detectionMode"] = DetectionMode,
                    ["grid"] = cells,
                    ["bias"] = Bias.ToDictionary()
                };
            }
        }

        #endregion

        #region Public API

        /// <summary>
        /// Analyze the layout of physics objects in the scene using a 3×3×3 grid.
        /// </summary>
        public LayoutResult AnalyzeLayout(
            string rootPath = null,
            string targetTag = null,
            string targetLayer = null,
            bool includeKinematic = true,
            bool include2D = false,
            bool includeTriggers = true,
            string detectionMode = "collider",
            Vector3? customMin = null,
            Vector3? customMax = null)
        {
            var objects = detectionMode == "rigidbody"
                ? CollectByRigidbody(rootPath, targetTag, targetLayer, includeKinematic, include2D)
                : CollectByCollider(rootPath, targetTag, targetLayer, include2D, includeTriggers);

            if (objects.Count == 0)
            {
                return CreateEmptyResult(detectionMode, customMin, customMax);
            }

            var sceneBounds = CalculateBounds(objects, customMin, customMax);
            var grid = BuildGrid(sceneBounds);
            AssignObjectsToCells(objects, grid, sceneBounds);
            CalculateOccupancy(grid, objects.Count);
            var bias = AnalyzeBias(grid, objects, sceneBounds);

            return new LayoutResult
            {
                SceneBounds = sceneBounds,
                TotalObjects = objects.Count,
                DetectionMode = detectionMode,
                Grid = grid,
                Bias = bias
            };
        }

        /// <summary>
        /// Inspect a specific cell in the 3×3×3 grid.
        /// </summary>
        public CellInfo InspectCell(
            int cellX, int cellY, int cellZ,
            string rootPath = null,
            string targetTag = null,
            string targetLayer = null,
            bool includeKinematic = true,
            bool include2D = false,
            bool includeTriggers = true,
            string detectionMode = "collider",
            Vector3? customMin = null,
            Vector3? customMax = null)
        {
            if (cellX < 0 || cellX > 2 || cellY < 0 || cellY > 2 || cellZ < 0 || cellZ > 2)
            {
                throw new ArgumentException($"Cell indices must be 0-2. Got ({cellX}, {cellY}, {cellZ}).");
            }

            var result = AnalyzeLayout(rootPath, targetTag, targetLayer, includeKinematic, include2D,
                includeTriggers, detectionMode, customMin, customMax);
            return result.Grid[cellX, cellY, cellZ];
        }

        #endregion

        #region Collection — Collider Mode

        private List<PhysicsObjectInfo> CollectByCollider(
            string rootPath, string targetTag, string targetLayer,
            bool include2D, bool includeTriggers)
        {
            var result = new List<PhysicsObjectInfo>();
            var visited = new HashSet<int>(); // instanceID dedup
            var roots = GetSearchRoots(rootPath);

            foreach (var root in roots)
            {
                // Collect 3D Colliders
                var colliders3D = root.GetComponentsInChildren<Collider>(true);
                foreach (var col in colliders3D)
                {
                    int id = col.gameObject.GetInstanceID();
                    if (visited.Contains(id)) continue;
                    if (!includeTriggers && col.isTrigger) continue;
                    if (!MatchesFilters(col.gameObject, targetTag, targetLayer)) continue;

                    visited.Add(id);
                    result.Add(BuildInfoFromGameObject3D(col.gameObject));
                }

                // Collect 2D Colliders
                if (include2D)
                {
                    var colliders2D = root.GetComponentsInChildren<Collider2D>(true);
                    foreach (var col in colliders2D)
                    {
                        int id = col.gameObject.GetInstanceID();
                        if (visited.Contains(id)) continue;
                        if (!includeTriggers && col.isTrigger) continue;
                        if (!MatchesFilters(col.gameObject, targetTag, targetLayer)) continue;

                        visited.Add(id);
                        result.Add(BuildInfoFromGameObject2D(col.gameObject));
                    }
                }
            }

            return result;
        }

        private PhysicsObjectInfo BuildInfoFromGameObject3D(GameObject go)
        {
            var colliders = go.GetComponents<Collider>();
            var rb = go.GetComponent<Rigidbody>();

            // Compute merged bounds from all colliders
            var mergedBounds = colliders[0].bounds;
            bool anyTrigger = colliders[0].isTrigger;
            var colliderTypes = new List<string>();

            foreach (var c in colliders)
            {
                mergedBounds.Encapsulate(c.bounds);
                if (c.isTrigger) anyTrigger = true;
                colliderTypes.Add(c.GetType().Name);
            }

            return new PhysicsObjectInfo
            {
                Path = GetGameObjectPath(go),
                Tag = go.tag,
                Layer = LayerMask.LayerToName(go.layer),
                Position = mergedBounds.center,
                ObjectBounds = mergedBounds,
                HasRigidbody = rb != null,
                IsKinematic = rb != null && rb.isKinematic,
                Is2D = false,
                IsTrigger = anyTrigger,
                Mass = rb != null ? rb.mass : 1f,
                ColliderTypes = colliderTypes
            };
        }

        private PhysicsObjectInfo BuildInfoFromGameObject2D(GameObject go)
        {
            var colliders = go.GetComponents<Collider2D>();
            var rb = go.GetComponent<Rigidbody2D>();

            var mergedBounds = colliders[0].bounds;
            bool anyTrigger = colliders[0].isTrigger;
            var colliderTypes = new List<string>();

            foreach (var c in colliders)
            {
                mergedBounds.Encapsulate(c.bounds);
                if (c.isTrigger) anyTrigger = true;
                colliderTypes.Add(c.GetType().Name);
            }

            return new PhysicsObjectInfo
            {
                Path = GetGameObjectPath(go),
                Tag = go.tag,
                Layer = LayerMask.LayerToName(go.layer),
                Position = mergedBounds.center,
                ObjectBounds = mergedBounds,
                HasRigidbody = rb != null,
                IsKinematic = rb != null && rb.bodyType == RigidbodyType2D.Kinematic,
                Is2D = true,
                IsTrigger = anyTrigger,
                Mass = rb != null ? rb.mass : 1f,
                ColliderTypes = colliderTypes
            };
        }

        #endregion

        #region Collection — Rigidbody Mode

        private List<PhysicsObjectInfo> CollectByRigidbody(
            string rootPath, string targetTag, string targetLayer,
            bool includeKinematic, bool include2D)
        {
            var result = new List<PhysicsObjectInfo>();
            var roots = GetSearchRoots(rootPath);

            foreach (var root in roots)
            {
                var bodies3D = root.GetComponentsInChildren<Rigidbody>(true);
                foreach (var rb in bodies3D)
                {
                    if (!includeKinematic && rb.isKinematic) continue;
                    if (!MatchesFilters(rb.gameObject, targetTag, targetLayer)) continue;

                    var col = rb.GetComponent<Collider>();
                    var objBounds = col != null ? col.bounds : new Bounds(rb.transform.position, Vector3.one);

                    result.Add(new PhysicsObjectInfo
                    {
                        Path = GetGameObjectPath(rb.gameObject),
                        Tag = rb.gameObject.tag,
                        Layer = LayerMask.LayerToName(rb.gameObject.layer),
                        Position = col != null ? col.bounds.center : rb.transform.position,
                        ObjectBounds = objBounds,
                        HasRigidbody = true,
                        IsKinematic = rb.isKinematic,
                        Is2D = false,
                        IsTrigger = col != null && col.isTrigger,
                        Mass = rb.mass,
                        ColliderTypes = col != null
                            ? new List<string> { col.GetType().Name }
                            : new List<string>()
                    });
                }

                if (include2D)
                {
                    var bodies2D = root.GetComponentsInChildren<Rigidbody2D>(true);
                    foreach (var rb in bodies2D)
                    {
                        if (!includeKinematic && rb.bodyType == RigidbodyType2D.Kinematic) continue;
                        if (!MatchesFilters(rb.gameObject, targetTag, targetLayer)) continue;

                        var col = rb.GetComponent<Collider2D>();
                        var objBounds = col != null ? col.bounds : new Bounds(rb.transform.position, Vector3.one);

                        result.Add(new PhysicsObjectInfo
                        {
                            Path = GetGameObjectPath(rb.gameObject),
                            Tag = rb.gameObject.tag,
                            Layer = LayerMask.LayerToName(rb.gameObject.layer),
                            Position = col != null ? col.bounds.center : rb.transform.position,
                            ObjectBounds = objBounds,
                            HasRigidbody = true,
                            IsKinematic = rb.bodyType == RigidbodyType2D.Kinematic,
                            Is2D = true,
                            IsTrigger = col != null && col.isTrigger,
                            Mass = rb.mass,
                            ColliderTypes = col != null
                                ? new List<string> { col.GetType().Name }
                                : new List<string>()
                        });
                    }
                }
            }

            return result;
        }

        #endregion

        #region Common Helpers

        private GameObject[] GetSearchRoots(string rootPath)
        {
            if (!string.IsNullOrEmpty(rootPath))
            {
                var go = GameObject.Find(rootPath);
                if (go == null)
                {
                    throw new ArgumentException($"GameObject not found: {rootPath}");
                }
                return new[] { go };
            }

            var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            return scene.GetRootGameObjects();
        }

        private bool MatchesFilters(GameObject go, string targetTag, string targetLayer)
        {
            if (!string.IsNullOrEmpty(targetTag) && !go.CompareTag(targetTag))
                return false;

            if (!string.IsNullOrEmpty(targetLayer))
            {
                int layerIndex = LayerMask.NameToLayer(targetLayer);
                if (layerIndex >= 0 && go.layer != layerIndex)
                    return false;
            }

            return true;
        }

        private string GetGameObjectPath(GameObject go)
        {
            var path = go.name;
            var current = go.transform.parent;
            while (current != null)
            {
                path = current.name + "/" + path;
                current = current.parent;
            }
            return path;
        }

        #endregion

        #region Grid Construction

        private Bounds CalculateBounds(List<PhysicsObjectInfo> objects, Vector3? customMin, Vector3? customMax)
        {
            if (customMin.HasValue && customMax.HasValue)
            {
                var center = (customMin.Value + customMax.Value) * 0.5f;
                var size = customMax.Value - customMin.Value;
                return new Bounds(center, size);
            }

            var min = objects[0].Position;
            var max = objects[0].Position;

            foreach (var obj in objects)
            {
                min = Vector3.Min(min, obj.ObjectBounds.min);
                max = Vector3.Max(max, obj.ObjectBounds.max);
            }

            // Add a small padding so edge objects are not on the boundary
            var padding = (max - min) * 0.01f;
            padding = Vector3.Max(padding, Vector3.one * 0.1f);
            min -= padding;
            max += padding;

            var boundsCenter = (min + max) * 0.5f;
            var boundsSize = max - min;
            return new Bounds(boundsCenter, boundsSize);
        }

        private CellInfo[,,] BuildGrid(Bounds sceneBounds)
        {
            var grid = new CellInfo[3, 3, 3];
            var min = sceneBounds.min;
            var cellSize = sceneBounds.size / 3f;

            for (int x = 0; x < 3; x++)
            {
                for (int y = 0; y < 3; y++)
                {
                    for (int z = 0; z < 3; z++)
                    {
                        var cellMin = min + new Vector3(x * cellSize.x, y * cellSize.y, z * cellSize.z);
                        var cellMax = cellMin + cellSize;
                        var cellCenter = (cellMin + cellMax) * 0.5f;

                        grid[x, y, z] = new CellInfo
                        {
                            X = x,
                            Y = y,
                            Z = z,
                            Label = GetCellLabel(x, y, z),
                            CellBounds = new Bounds(cellCenter, cellSize)
                        };
                    }
                }
            }

            return grid;
        }

        private void AssignObjectsToCells(List<PhysicsObjectInfo> objects, CellInfo[,,] grid, Bounds sceneBounds)
        {
            var min = sceneBounds.min;
            var size = sceneBounds.size;

            foreach (var obj in objects)
            {
                float nx = size.x > 0 ? (obj.Position.x - min.x) / size.x : 0.5f;
                float ny = size.y > 0 ? (obj.Position.y - min.y) / size.y : 0.5f;
                float nz = size.z > 0 ? (obj.Position.z - min.z) / size.z : 0.5f;

                int cx = Mathf.Clamp(Mathf.FloorToInt(nx * 3f), 0, 2);
                int cy = Mathf.Clamp(Mathf.FloorToInt(ny * 3f), 0, 2);
                int cz = Mathf.Clamp(Mathf.FloorToInt(nz * 3f), 0, 2);

                grid[cx, cy, cz].Objects.Add(obj);
            }
        }

        private void CalculateOccupancy(CellInfo[,,] grid, int totalCount)
        {
            if (totalCount == 0) return;

            for (int x = 0; x < 3; x++)
                for (int y = 0; y < 3; y++)
                    for (int z = 0; z < 3; z++)
                        grid[x, y, z].OccupancyRate = (float)grid[x, y, z].Count / totalCount;
        }

        #endregion

        #region Bias Analysis

        private BiasInfo AnalyzeBias(CellInfo[,,] grid, List<PhysicsObjectInfo> objects, Bounds sceneBounds)
        {
            var normalizedCoM = CalculateNormalizedCenterOfMass(objects, sceneBounds);

            int emptyCells = 0;
            for (int x = 0; x < 3; x++)
                for (int y = 0; y < 3; y++)
                    for (int z = 0; z < 3; z++)
                        if (grid[x, y, z].Count == 0) emptyCells++;

            return new BiasInfo
            {
                NormalizedCenterOfMass = normalizedCoM,
                HorizontalBias = ClassifyBias(normalizedCoM.x, "left", "center", "right"),
                VerticalBias = ClassifyBias(normalizedCoM.y, "bottom", "center", "top"),
                DepthBias = ClassifyBias(normalizedCoM.z, "front", "center", "back"),
                LeftRightSymmetry = CalculateAxisSymmetry(grid, 0),
                TopBottomSymmetry = CalculateAxisSymmetry(grid, 1),
                FrontBackSymmetry = CalculateAxisSymmetry(grid, 2),
                Distribution = ClassifyDistribution(grid, objects.Count),
                EmptyRate = emptyCells / 27f
            };
        }

        private Vector3 CalculateNormalizedCenterOfMass(List<PhysicsObjectInfo> objects, Bounds sceneBounds)
        {
            if (objects.Count == 0) return new Vector3(0.5f, 0.5f, 0.5f);

            var totalMass = 0f;
            var weightedPos = Vector3.zero;

            foreach (var obj in objects)
            {
                var mass = Mathf.Max(obj.Mass, 0.001f);
                weightedPos += obj.Position * mass;
                totalMass += mass;
            }

            var com = weightedPos / totalMass;

            var min = sceneBounds.min;
            var size = sceneBounds.size;
            return new Vector3(
                size.x > 0 ? (com.x - min.x) / size.x : 0.5f,
                size.y > 0 ? (com.y - min.y) / size.y : 0.5f,
                size.z > 0 ? (com.z - min.z) / size.z : 0.5f
            );
        }

        private string ClassifyBias(float normalizedValue, string low, string center, string high)
        {
            if (normalizedValue < 1f / 3f) return low;
            if (normalizedValue > 2f / 3f) return high;
            return center;
        }

        /// <summary>
        /// Calculate symmetry along a given axis (0=X, 1=Y, 2=Z).
        /// Returns 0-1 where 1 means perfect mirror symmetry.
        /// </summary>
        private float CalculateAxisSymmetry(CellInfo[,,] grid, int axis)
        {
            int totalCount = 0;
            int symmetricCount = 0;

            for (int a = 0; a < 3; a++)
            {
                for (int b = 0; b < 3; b++)
                {
                    int countLow, countHigh;
                    switch (axis)
                    {
                        case 0:
                            countLow = grid[0, a, b].Count;
                            countHigh = grid[2, a, b].Count;
                            break;
                        case 1:
                            countLow = grid[a, 0, b].Count;
                            countHigh = grid[a, 2, b].Count;
                            break;
                        case 2:
                            countLow = grid[a, b, 0].Count;
                            countHigh = grid[a, b, 2].Count;
                            break;
                        default:
                            continue;
                    }

                    int pairTotal = countLow + countHigh;
                    if (pairTotal > 0)
                    {
                        int minCount = Math.Min(countLow, countHigh);
                        symmetricCount += minCount * 2;
                        totalCount += pairTotal;
                    }
                }
            }

            return totalCount > 0 ? (float)symmetricCount / totalCount : 1f;
        }

        /// <summary>
        /// Classify the overall distribution pattern.
        /// </summary>
        private string ClassifyDistribution(CellInfo[,,] grid, int totalCount)
        {
            if (totalCount == 0) return "empty";

            int occupiedCells = 0;
            float sumSquared = 0;

            for (int x = 0; x < 3; x++)
            {
                for (int y = 0; y < 3; y++)
                {
                    for (int z = 0; z < 3; z++)
                    {
                        int c = grid[x, y, z].Count;
                        if (c > 0) occupiedCells++;
                        float rate = (float)c / totalCount;
                        sumSquared += rate * rate;
                    }
                }
            }

            float hhi = sumSquared;

            if (occupiedCells <= 3 && totalCount >= 3)
                return "clustered";

            if (hhi > 0.25f)
                return "concentrated";

            if (occupiedCells >= 18 && hhi < 0.08f)
                return "uniform";

            if (occupiedCells >= 9)
                return "spread";

            return "sparse";
        }

        #endregion

        #region Labels

        private static readonly string[] XLabels = { "left", "center", "right" };
        private static readonly string[] YLabels = { "bottom", "middle", "top" };
        private static readonly string[] ZLabels = { "front", "center", "back" };

        private string GetCellLabel(int x, int y, int z)
        {
            return $"{YLabels[y]}-{XLabels[x]}-{ZLabels[z]}";
        }

        #endregion

        #region Utility

        private LayoutResult CreateEmptyResult(string detectionMode, Vector3? customMin, Vector3? customMax)
        {
            Bounds bounds;
            if (customMin.HasValue && customMax.HasValue)
            {
                var center = (customMin.Value + customMax.Value) * 0.5f;
                var size = customMax.Value - customMin.Value;
                bounds = new Bounds(center, size);
            }
            else
            {
                bounds = new Bounds(Vector3.zero, Vector3.zero);
            }

            var grid = BuildGrid(bounds);
            return new LayoutResult
            {
                SceneBounds = bounds,
                TotalObjects = 0,
                DetectionMode = detectionMode,
                Grid = grid,
                Bias = new BiasInfo
                {
                    NormalizedCenterOfMass = new Vector3(0.5f, 0.5f, 0.5f),
                    HorizontalBias = "center",
                    VerticalBias = "center",
                    DepthBias = "center",
                    LeftRightSymmetry = 1f,
                    TopBottomSymmetry = 1f,
                    FrontBackSymmetry = 1f,
                    Distribution = "empty",
                    EmptyRate = 1f
                }
            };
        }

        private static Dictionary<string, object> BoundsToDictionary(Bounds b)
        {
            return new Dictionary<string, object>
            {
                ["min"] = Vec3ToDictionary(b.min),
                ["max"] = Vec3ToDictionary(b.max),
                ["size"] = Vec3ToDictionary(b.size),
                ["center"] = Vec3ToDictionary(b.center)
            };
        }

        private static Dictionary<string, object> Vec3ToDictionary(Vector3 v)
        {
            return new Dictionary<string, object>
            {
                ["x"] = Math.Round(v.x, 4),
                ["y"] = Math.Round(v.y, 4),
                ["z"] = Math.Round(v.z, 4)
            };
        }

        #endregion
    }
}
