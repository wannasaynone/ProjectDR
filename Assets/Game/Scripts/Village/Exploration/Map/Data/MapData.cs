using ProjectDR.Village.Exploration.Collection;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace ProjectDR.Village.Exploration.Map
{
    /// <summary>
    /// Defines a monster spawn point on the map.
    /// </summary>
    public class MonsterSpawnPoint
    {
        public Vector2Int Position { get; }
        public string TypeId { get; }

        public MonsterSpawnPoint(Vector2Int position, string typeId)
        {
            Position = position;
            TypeId = typeId ?? throw new ArgumentNullException(nameof(typeId));
        }
    }

    /// <summary>
    /// Immutable data container describing the layout, spawn point, and evacuation groups of an exploration map.
    /// All validation is performed at construction time; instances are considered valid once created.
    /// </summary>
    public class MapData
    {
        /// <summary>Width of the map in cells.</summary>
        public int Width { get; }

        /// <summary>Height of the map in cells.</summary>
        public int Height { get; }

        /// <summary>The player spawn position on this map.</summary>
        public Vector2Int SpawnPosition { get; }

        /// <summary>
        /// Groups of evacuation points. Each group represents a distinct set of exit cells
        /// that can be activated together.
        /// </summary>
        public IReadOnlyList<IReadOnlyList<Vector2Int>> EvacuationGroups { get; }

        /// <summary>
        /// Collectible points on this map, keyed by position.
        /// </summary>
        public IReadOnlyList<CollectiblePointData> CollectiblePoints { get; }

        /// <summary>
        /// Monster spawn point definitions on this map.
        /// </summary>
        public IReadOnlyList<MonsterSpawnPoint> MonsterSpawnPoints { get; }

        private readonly CellType[] _cells;
        private readonly Dictionary<long, CollectiblePointData> _collectiblePointLookup;

        /// <summary>
        /// Creates a new MapData instance with full validation.
        /// </summary>
        /// <param name="width">Map width. Must be greater than 0.</param>
        /// <param name="height">Map height. Must be greater than 0.</param>
        /// <param name="cells">
        /// Flat array of cell types in row-major order (index = y * width + x).
        /// Length must equal width * height.
        /// </param>
        /// <param name="spawnPosition">Player spawn position. Must be in bounds and Explorable.</param>
        /// <param name="evacuationGroups">
        /// Groups of evacuation points. Every point must be in bounds and Explorable.
        /// </param>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when width or height is less than or equal to 0.</exception>
        /// <exception cref="ArgumentException">
        /// Thrown when cells.Length does not match width * height, or when spawnPosition or any evacuation
        /// point is out of bounds or Blocked.
        /// </exception>
        public MapData(int width, int height, CellType[] cells, Vector2Int spawnPosition,
            List<List<Vector2Int>> evacuationGroups,
            List<CollectiblePointData> collectiblePoints = null,
            List<MonsterSpawnPoint> monsterSpawnPoints = null)
        {
            if (width <= 0)
                throw new ArgumentOutOfRangeException(nameof(width), "width must be greater than 0.");
            if (height <= 0)
                throw new ArgumentOutOfRangeException(nameof(height), "height must be greater than 0.");

            if (cells == null || cells.Length != width * height)
                throw new ArgumentException($"cells.Length must equal width * height ({width * height}).", nameof(cells));

            // Defensive copy to prevent external mutation.
            _cells = new CellType[cells.Length];
            Array.Copy(cells, _cells, cells.Length);

            Width = width;
            Height = height;

            // Validate spawn position after cells are stored so IsWalkable can be used.
            if (!IsInBounds(spawnPosition.x, spawnPosition.y) || GetCellType(spawnPosition.x, spawnPosition.y) != CellType.Explorable)
                throw new ArgumentException("spawnPosition must be in bounds and Explorable.", nameof(spawnPosition));

            SpawnPosition = spawnPosition;

            // Build read-only evacuation groups and validate every point.
            List<IReadOnlyList<Vector2Int>> groups = new List<IReadOnlyList<Vector2Int>>();
            if (evacuationGroups != null)
            {
                for (int g = 0; g < evacuationGroups.Count; g++)
                {
                    List<Vector2Int> sourceGroup = evacuationGroups[g];
                    if (sourceGroup != null)
                    {
                        for (int p = 0; p < sourceGroup.Count; p++)
                        {
                            Vector2Int point = sourceGroup[p];
                            if (!IsInBounds(point.x, point.y) || GetCellType(point.x, point.y) != CellType.Explorable)
                            {
                                throw new ArgumentException(
                                    $"Evacuation point ({point.x}, {point.y}) in group {g} is out of bounds or Blocked.",
                                    nameof(evacuationGroups));
                            }
                        }
                        groups.Add(sourceGroup.AsReadOnly());
                    }
                }
            }

            EvacuationGroups = groups.AsReadOnly();

            // Build collectible point lookup and validate positions.
            _collectiblePointLookup = new Dictionary<long, CollectiblePointData>();
            List<CollectiblePointData> cpList = new List<CollectiblePointData>();
            if (collectiblePoints != null)
            {
                for (int i = 0; i < collectiblePoints.Count; i++)
                {
                    CollectiblePointData cp = collectiblePoints[i];
                    if (cp == null) continue;

                    if (!IsWalkable(cp.X, cp.Y))
                        throw new ArgumentException(
                            $"Collectible point ({cp.X}, {cp.Y}) is out of bounds or Blocked.",
                            nameof(collectiblePoints));

                    long key = ((long)cp.X << 32) | (uint)cp.Y;
                    _collectiblePointLookup[key] = cp;
                    cpList.Add(cp);
                }
            }
            CollectiblePoints = cpList.AsReadOnly();

            // Build monster spawn points list.
            List<MonsterSpawnPoint> mspList = new List<MonsterSpawnPoint>();
            if (monsterSpawnPoints != null)
            {
                for (int i = 0; i < monsterSpawnPoints.Count; i++)
                {
                    MonsterSpawnPoint msp = monsterSpawnPoints[i];
                    if (msp == null) continue;
                    mspList.Add(msp);
                }
            }
            MonsterSpawnPoints = mspList.AsReadOnly();
        }

        /// <summary>
        /// Returns the CellType at the given coordinates.
        /// Returns Blocked for any out-of-bounds coordinate.
        /// </summary>
        public CellType GetCellType(int x, int y)
        {
            if (!IsInBounds(x, y))
                return CellType.Blocked;

            return _cells[y * Width + x];
        }

        /// <summary>Returns true if the coordinate is within the map boundaries.</summary>
        public bool IsInBounds(int x, int y)
        {
            return x >= 0 && x < Width && y >= 0 && y < Height;
        }

        /// <summary>Returns true if the coordinate is in bounds and the cell type is Explorable.</summary>
        public bool IsWalkable(int x, int y)
        {
            return IsInBounds(x, y) && GetCellType(x, y) == CellType.Explorable;
        }

        /// <summary>
        /// Returns the collectible point data at the given position, or null if there is none.
        /// </summary>
        public CollectiblePointData GetCollectiblePointAt(int x, int y)
        {
            long key = ((long)x << 32) | (uint)y;
            _collectiblePointLookup.TryGetValue(key, out CollectiblePointData result);
            return result;
        }

        /// <summary>Returns true if there is a collectible point at the given position.</summary>
        public bool HasCollectiblePoint(int x, int y)
        {
            return GetCollectiblePointAt(x, y) != null;
        }
    }
}
