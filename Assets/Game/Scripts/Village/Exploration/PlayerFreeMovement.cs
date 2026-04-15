using System;
using KahaGameCore.GameEvent;
using UnityEngine;

namespace ProjectDR.Village.Exploration
{
    /// <summary>
    /// Manages the player's free movement on the exploration map.
    /// Tracks world position and detects grid cell changes.
    /// Wall collision uses grid walkability checks with sliding.
    /// </summary>
    public class PlayerFreeMovement
    {
        private readonly GridMap _gridMap;
        private readonly float _cellSize;
        private readonly Vector3 _mapOrigin;
        private readonly IMoveSpeedProvider _speedProvider;

        /// <summary>Half the collision radius for wall checks.</summary>
        private const float CollisionRadius = 0.3f;

        /// <summary>Current world position of the player.</summary>
        public Vector2 WorldPosition { get; private set; }

        /// <summary>Current grid cell the player center is in.</summary>
        public Vector2Int CurrentGridCell { get; private set; }

        /// <summary>
        /// True when movement is externally locked (e.g. during gathering).
        /// While locked, Move does nothing.
        /// </summary>
        public bool IsMovementLocked { get; private set; }

        /// <param name="gridMap">The grid map used for walkability checks and cell reveal.</param>
        /// <param name="startGridCell">Initial grid cell. Must be in bounds and walkable.</param>
        /// <param name="cellSize">World size of each grid cell (e.g. 1.0f).</param>
        /// <param name="mapOrigin">World-space origin of the map (top-left cell center).</param>
        /// <param name="speedProvider">Provides movement speed in units/second.</param>
        public PlayerFreeMovement(GridMap gridMap, Vector2Int startGridCell,
            float cellSize, Vector3 mapOrigin, IMoveSpeedProvider speedProvider)
        {
            _gridMap = gridMap ?? throw new ArgumentNullException(nameof(gridMap));
            _speedProvider = speedProvider ?? throw new ArgumentNullException(nameof(speedProvider));

            if (cellSize <= 0f)
                throw new ArgumentOutOfRangeException(nameof(cellSize), "cellSize must be > 0.");

            if (!gridMap.IsWalkable(startGridCell.x, startGridCell.y))
                throw new ArgumentException("startGridCell must be in bounds and walkable.", nameof(startGridCell));

            _cellSize = cellSize;
            _mapOrigin = mapOrigin;

            CurrentGridCell = startGridCell;
            WorldPosition = GridToWorld(startGridCell.x, startGridCell.y);
        }

        /// <summary>
        /// Sets the external movement lock. While locked, Move does nothing.
        /// </summary>
        public void SetMovementLock(bool locked)
        {
            IsMovementLocked = locked;
        }

        /// <summary>
        /// Moves the player in the given input direction for the given delta time.
        /// Handles wall collision with axis-separated sliding.
        /// When the player's center enters a new grid cell, automatically reveals it
        /// and publishes PlayerCellChangedEvent.
        /// </summary>
        /// <param name="inputDirection">Raw input direction (will be normalized if magnitude > 1).</param>
        /// <param name="deltaTime">Frame delta time in seconds.</param>
        public void Move(Vector2 inputDirection, float deltaTime)
        {
            if (IsMovementLocked) return;
            if (deltaTime <= 0f) return;

            Vector2 dir = inputDirection;
            if (dir.sqrMagnitude < 0.001f) return;
            if (dir.sqrMagnitude > 1f) dir = dir.normalized;

            float speed = _speedProvider.GetMoveSpeed();
            Vector2 displacement = dir * speed * deltaTime;

            Vector2 newPos = WorldPosition;

            // Try X axis first
            Vector2 testPosX = new Vector2(newPos.x + displacement.x, newPos.y);
            if (IsWorldPositionWalkable(testPosX))
            {
                newPos = testPosX;
            }

            // Try Y axis
            Vector2 testPosY = new Vector2(newPos.x, newPos.y + displacement.y);
            if (IsWorldPositionWalkable(testPosY))
            {
                newPos = testPosY;
            }

            WorldPosition = newPos;

            // Check grid cell change
            Vector2Int newCell = WorldToGrid(WorldPosition);
            if (newCell != CurrentGridCell)
            {
                Vector2Int prevCell = CurrentGridCell;
                CurrentGridCell = newCell;

                // Reveal if unexplored
                if (!_gridMap.IsExplored(newCell.x, newCell.y))
                {
                    _gridMap.RevealCell(newCell.x, newCell.y);
                }

                EventBus.Publish(new PlayerCellChangedEvent
                {
                    PreviousCell = prevCell,
                    NewCell = newCell
                });
            }
        }

        /// <summary>
        /// Applies an instant knockback displacement, respecting wall collision.
        /// </summary>
        /// <param name="direction">Normalized knockback direction.</param>
        /// <param name="distance">Knockback distance in world units.</param>
        public void ApplyKnockback(Vector2 direction, float distance)
        {
            if (distance <= 0f) return;
            if (direction.sqrMagnitude < 0.001f) return;

            Vector2 dir = direction.normalized;
            // Apply knockback in small steps to avoid passing through walls
            float stepSize = 0.1f;
            float remaining = distance;
            Vector2 pos = WorldPosition;

            while (remaining > 0f)
            {
                float step = Mathf.Min(stepSize, remaining);
                Vector2 testPos = pos + dir * step;

                if (IsWorldPositionWalkable(testPos))
                {
                    pos = testPos;
                }
                else
                {
                    break;
                }

                remaining -= step;
            }

            WorldPosition = pos;

            // Check grid cell change after knockback
            Vector2Int newCell = WorldToGrid(WorldPosition);
            if (newCell != CurrentGridCell)
            {
                Vector2Int prevCell = CurrentGridCell;
                CurrentGridCell = newCell;

                if (!_gridMap.IsExplored(newCell.x, newCell.y))
                {
                    _gridMap.RevealCell(newCell.x, newCell.y);
                }

                EventBus.Publish(new PlayerCellChangedEvent
                {
                    PreviousCell = prevCell,
                    NewCell = newCell
                });
            }
        }

        // ------------------------------------------------------------------
        // Coordinate conversion
        // ------------------------------------------------------------------

        /// <summary>Converts a grid coordinate to world position.</summary>
        public Vector2 GridToWorld(int gridX, int gridY)
        {
            return new Vector2(
                _mapOrigin.x + gridX * _cellSize,
                _mapOrigin.y - gridY * _cellSize);
        }

        /// <summary>Converts a world position to grid coordinate.</summary>
        public Vector2Int WorldToGrid(Vector2 worldPos)
        {
            int gx = Mathf.RoundToInt((worldPos.x - _mapOrigin.x) / _cellSize);
            int gy = Mathf.RoundToInt((_mapOrigin.y - worldPos.y) / _cellSize);
            return new Vector2Int(gx, gy);
        }

        // ------------------------------------------------------------------
        // Wall collision
        // ------------------------------------------------------------------

        /// <summary>
        /// Checks if a world position is walkable by testing the grid cell
        /// at the player's center.
        /// </summary>
        private bool IsWorldPositionWalkable(Vector2 worldPos)
        {
            // Check the cell at the center
            Vector2Int cell = WorldToGrid(worldPos);
            if (!_gridMap.IsWalkable(cell.x, cell.y))
                return false;

            // Additionally check corner cells based on collision radius
            // to prevent clipping into wall cells
            Vector2Int cellTopLeft = WorldToGrid(new Vector2(worldPos.x - CollisionRadius, worldPos.y + CollisionRadius));
            Vector2Int cellTopRight = WorldToGrid(new Vector2(worldPos.x + CollisionRadius, worldPos.y + CollisionRadius));
            Vector2Int cellBottomLeft = WorldToGrid(new Vector2(worldPos.x - CollisionRadius, worldPos.y - CollisionRadius));
            Vector2Int cellBottomRight = WorldToGrid(new Vector2(worldPos.x + CollisionRadius, worldPos.y - CollisionRadius));

            if (!_gridMap.IsWalkable(cellTopLeft.x, cellTopLeft.y)) return false;
            if (!_gridMap.IsWalkable(cellTopRight.x, cellTopRight.y)) return false;
            if (!_gridMap.IsWalkable(cellBottomLeft.x, cellBottomLeft.y)) return false;
            if (!_gridMap.IsWalkable(cellBottomRight.x, cellBottomRight.y)) return false;

            return true;
        }
    }
}
