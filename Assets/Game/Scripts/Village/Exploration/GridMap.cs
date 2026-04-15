using System;
using System.Collections.Generic;
using KahaGameCore.GameEvent;
using UnityEngine;

namespace ProjectDR.Village.Exploration
{
    /// <summary>
    /// Manages the runtime exploration state of the map: which cells have been revealed,
    /// which evacuation points are active, and adjacent monster counts.
    /// </summary>
    public class GridMap
    {
        private readonly MapData _mapData;
        private IMonsterPositionProvider _monsterPositionProvider;

        private readonly bool[] _explored;
        private readonly List<Vector2Int> _activeEvacuationPoints;

        private bool _initialized;

        // Cached neighbours offsets for the 8-directional adjacency check.
        private static readonly int[] NeighbourDx = { -1, 0, 1, -1, 1, -1, 0, 1 };
        private static readonly int[] NeighbourDy = { -1, -1, -1, 0, 0, 1, 1, 1 };

        /// <summary>
        /// Updates the monster position provider. Used when the MonsterManager is re-created
        /// after GridMap construction.
        /// </summary>
        public void SetMonsterPositionProvider(IMonsterPositionProvider provider)
        {
            _monsterPositionProvider = provider;
        }

        /// <summary>Width of the underlying map.</summary>
        public int Width => _mapData.Width;

        /// <summary>Height of the underlying map.</summary>
        public int Height => _mapData.Height;

        /// <param name="mapData">The static map layout data.</param>
        /// <param name="monsterPositionProvider">
        /// Provider used to determine adjacent monster counts. May be null; null means no monsters.
        /// </param>
        public GridMap(MapData mapData, IMonsterPositionProvider monsterPositionProvider)
        {
            _mapData = mapData ?? throw new ArgumentNullException(nameof(mapData));
            _monsterPositionProvider = monsterPositionProvider;

            _explored = new bool[mapData.Width * mapData.Height];
            _activeEvacuationPoints = new List<Vector2Int>();
        }

        /// <summary>
        /// Marks cells near the spawn and (optionally) an evacuation group as explored without
        /// publishing any events. Must be called exactly once.
        /// </summary>
        /// <param name="revealRadius">
        /// Manhattan-distance radius around the spawn (and evacuation points) to pre-reveal.
        /// Must be >= 0.
        /// </param>
        /// <param name="evacuationGroupIndex">
        /// Index into MapData.EvacuationGroups to activate and pre-reveal. Pass -1 when no
        /// evacuation group should be activated. When EvacuationGroups is empty, this must be -1.
        /// </param>
        /// <exception cref="InvalidOperationException">Thrown if called more than once.</exception>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Thrown when revealRadius &lt; 0, or when evacuationGroupIndex is invalid for the current
        /// EvacuationGroups count.
        /// </exception>
        public void InitializeExplored(int revealRadius, int evacuationGroupIndex)
        {
            if (_initialized)
                throw new InvalidOperationException("InitializeExplored has already been called.");

            if (revealRadius < 0)
                throw new ArgumentOutOfRangeException(nameof(revealRadius), "revealRadius must be >= 0.");

            int groupCount = _mapData.EvacuationGroups.Count;
            if (groupCount == 0)
            {
                if (evacuationGroupIndex != -1)
                    throw new ArgumentOutOfRangeException(nameof(evacuationGroupIndex),
                        "evacuationGroupIndex must be -1 when EvacuationGroups is empty.");
            }
            else
            {
                if (evacuationGroupIndex < 0 || evacuationGroupIndex >= groupCount)
                    throw new ArgumentOutOfRangeException(nameof(evacuationGroupIndex),
                        $"evacuationGroupIndex must be in [0, {groupCount - 1}].");
            }

            // Pre-reveal cells around the spawn point.
            RevealAreaSilently(_mapData.SpawnPosition, revealRadius);

            // Activate and pre-reveal the chosen evacuation group.
            if (evacuationGroupIndex >= 0)
            {
                IReadOnlyList<Vector2Int> group = _mapData.EvacuationGroups[evacuationGroupIndex];
                for (int i = 0; i < group.Count; i++)
                {
                    _activeEvacuationPoints.Add(group[i]);
                    RevealAreaSilently(group[i], revealRadius);
                }
            }

            _initialized = true;
        }

        /// <summary>
        /// Returns true if the cell at (x, y) is in bounds and Explorable (i.e. the player can walk on it).
        /// </summary>
        public bool IsWalkable(int x, int y)
        {
            return _mapData.IsWalkable(x, y);
        }

        /// <summary>Returns true if the cell at (x, y) has been explored. Returns false for out-of-bounds.</summary>
        public bool IsExplored(int x, int y)
        {
            if (!_mapData.IsInBounds(x, y))
                return false;

            return _explored[y * Width + x];
        }

        /// <summary>
        /// Reveals the cell at (x, y) and publishes a <see cref="CellRevealedEvent"/>.
        /// Does nothing and returns false if the cell is already explored, Blocked, or out of bounds.
        /// </summary>
        /// <returns>True if the cell was newly revealed; false otherwise.</returns>
        public bool RevealCell(int x, int y)
        {
            if (!_mapData.IsWalkable(x, y))
                return false;

            if (_explored[y * Width + x])
                return false;

            _explored[y * Width + x] = true;
            EventBus.Publish<CellRevealedEvent>(new CellRevealedEvent { X = x, Y = y });
            return true;
        }

        /// <summary>
        /// Returns the number of monsters occupying the 8 cells adjacent to (x, y).
        /// Returns 0 when x/y is out of bounds or no monster provider is set.
        /// The cell itself is not counted.
        /// </summary>
        public int GetAdjacentMonsterCount(int x, int y)
        {
            if (_monsterPositionProvider == null)
                return 0;

            if (!_mapData.IsInBounds(x, y))
                return 0;

            IReadOnlyList<Vector2Int> positions = _monsterPositionProvider.GetMonsterPositions();
            int count = 0;

            for (int i = 0; i < NeighbourDx.Length; i++)
            {
                int nx = x + NeighbourDx[i];
                int ny = y + NeighbourDy[i];

                if (!_mapData.IsInBounds(nx, ny))
                    continue;

                for (int m = 0; m < positions.Count; m++)
                {
                    if (positions[m].x == nx && positions[m].y == ny)
                    {
                        count++;
                        break; // At most one monster per cell counts toward the count.
                    }
                }
            }

            return count;
        }

        /// <summary>Returns the active evacuation points as a read-only collection.</summary>
        public System.Collections.ObjectModel.ReadOnlyCollection<Vector2Int> GetActiveEvacuationPoints()
        {
            return _activeEvacuationPoints.AsReadOnly();
        }

        /// <summary>Returns true if (x, y) is one of the currently active evacuation points.</summary>
        public bool IsEvacuationPoint(int x, int y)
        {
            for (int i = 0; i < _activeEvacuationPoints.Count; i++)
            {
                if (_activeEvacuationPoints[i].x == x && _activeEvacuationPoints[i].y == y)
                    return true;
            }
            return false;
        }

        /// <summary>
        /// Returns true if any of the 8 adjacent cells is walkable and not yet explored.
        /// Out-of-bounds cells and blocked cells are NOT considered unexplored.
        /// Returns false for out-of-bounds coordinates.
        /// </summary>
        public bool HasAdjacentUnexploredCell(int x, int y)
        {
            if (!_mapData.IsInBounds(x, y))
                return false;

            for (int i = 0; i < NeighbourDx.Length; i++)
            {
                int nx = x + NeighbourDx[i];
                int ny = y + NeighbourDy[i];

                if (!_mapData.IsWalkable(nx, ny))
                    continue;

                if (!_explored[ny * Width + nx])
                    return true;
            }

            return false;
        }

        /// <summary>
        /// Publishes a <see cref="MonsterCountsChangedEvent"/> to notify listeners that all
        /// adjacent monster counts should be recalculated.
        /// </summary>
        public void RecalculateAllMonsterCounts()
        {
            EventBus.Publish<MonsterCountsChangedEvent>(new MonsterCountsChangedEvent());
        }

        /// <summary>
        /// Returns the collectible point data at the given position, or null if there is none.
        /// Delegates to the underlying MapData.
        /// </summary>
        public CollectiblePointData GetCollectiblePointAt(int x, int y)
        {
            return _mapData.GetCollectiblePointAt(x, y);
        }

        /// <summary>Returns true if there is a collectible point at the given position.</summary>
        public bool HasCollectiblePoint(int x, int y)
        {
            return _mapData.HasCollectiblePoint(x, y);
        }

        // ------------------------------------------------------------------
        // Private helpers
        // ------------------------------------------------------------------

        /// <summary>
        /// Marks all walkable cells within Manhattan distance revealRadius of center as explored,
        /// without publishing any events. Used during initialization.
        /// </summary>
        private void RevealAreaSilently(Vector2Int center, int revealRadius)
        {
            for (int dy = -revealRadius; dy <= revealRadius; dy++)
            {
                for (int dx = -revealRadius; dx <= revealRadius; dx++)
                {
                    if (Math.Abs(dx) + Math.Abs(dy) > revealRadius)
                        continue;

                    int cx = center.x + dx;
                    int cy = center.y + dy;

                    if (!_mapData.IsWalkable(cx, cy))
                        continue;

                    _explored[cy * Width + cx] = true;
                }
            }
        }
    }
}
