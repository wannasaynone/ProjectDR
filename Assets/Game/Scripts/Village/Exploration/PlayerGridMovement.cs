using System;
using KahaGameCore.GameEvent;
using UnityEngine;

namespace ProjectDR.Village.Exploration
{
    /// <summary>
    /// Manages the player's logical position on the grid and drives movement events.
    /// The actual visual interpolation is handled externally; this class tracks state only.
    /// </summary>
    public class PlayerGridMovement
    {
        private readonly GridMap _gridMap;
        private readonly IMoveSpeedCalculator _moveSpeedCalculator;

        /// <summary>
        /// Optional predicate to block movement to certain cells (e.g. visible monster cells).
        /// Returns true if the cell is blocked for the player.
        /// </summary>
        private Func<int, int, bool> _additionalBlockCheck;

        /// <summary>The cell the player is currently standing on (or moving from).</summary>
        public Vector2Int CurrentPosition { get; private set; }

        /// <summary>True while a move animation is in progress.</summary>
        public bool IsMoving { get; private set; }

        /// <summary>
        /// True when movement is externally locked (e.g. during gathering).
        /// While locked, TryMove always returns false.
        /// </summary>
        public bool IsMovementLocked { get; private set; }

        /// <summary>
        /// The destination cell when a move is in progress.
        /// Equals <see cref="CurrentPosition"/> when not moving.
        /// </summary>
        public Vector2Int TargetPosition { get; private set; }

        /// <summary>
        /// The lerp duration (in seconds) for the current move.
        /// Set by <see cref="TryMove"/> and consumed by the visual layer.
        /// </summary>
        public float CurrentMoveDuration { get; private set; }

        /// <param name="gridMap">The grid map used for walkability checks and cell reveal.</param>
        /// <param name="startPosition">
        /// The initial player position. Must be in bounds and walkable.
        /// </param>
        /// <param name="moveSpeedCalculator">Provides the duration for each move animation.</param>
        /// <exception cref="ArgumentException">
        /// Thrown when startPosition is out of bounds or not walkable.
        /// </exception>
        public PlayerGridMovement(GridMap gridMap, Vector2Int startPosition, IMoveSpeedCalculator moveSpeedCalculator)
        {
            _gridMap = gridMap ?? throw new ArgumentNullException(nameof(gridMap));
            _moveSpeedCalculator = moveSpeedCalculator ?? throw new ArgumentNullException(nameof(moveSpeedCalculator));

            if (!gridMap.IsWalkable(startPosition.x, startPosition.y))
                throw new ArgumentException("startPosition must be in bounds and walkable.", nameof(startPosition));

            CurrentPosition = startPosition;
            TargetPosition = startPosition;
        }

        /// <summary>
        /// Attempts to move the player one cell in the given direction.
        /// Fails silently (returns false) if already moving or the target cell is not walkable.
        /// </summary>
        /// <returns>True if the move was initiated; false otherwise.</returns>
        /// <summary>
        /// Sets the external movement lock. While locked, TryMove always returns false.
        /// Used by the collection system to prevent movement during gathering (GDD rule 44).
        /// </summary>
        public void SetMovementLock(bool locked)
        {
            IsMovementLocked = locked;
        }

        /// <summary>
        /// Sets an additional block check predicate. When set, TryMove also calls this
        /// predicate and rejects the move if it returns true.
        /// Used for GDD rule 47 (visible monsters block player entry).
        /// </summary>
        public void SetAdditionalBlockCheck(Func<int, int, bool> check)
        {
            _additionalBlockCheck = check;
        }

        public bool TryMove(MoveDirection direction)
        {
            if (IsMoving)
                return false;

            if (IsMovementLocked)
                return false;

            Vector2Int target = CurrentPosition + GetDirectionOffset(direction);

            if (!_gridMap.IsWalkable(target.x, target.y))
                return false;

            // GDD rule 47: visible monster cells block player entry.
            if (_additionalBlockCheck != null && _additionalBlockCheck(target.x, target.y))
                return false;

            float duration = _moveSpeedCalculator.CalculateMoveDuration();

            IsMoving = true;
            TargetPosition = target;
            CurrentMoveDuration = duration;

            EventBus.Publish<PlayerMoveStartedEvent>(new PlayerMoveStartedEvent
            {
                From = CurrentPosition,
                To = target,
                MoveDuration = duration
            });

            return true;
        }

        /// <summary>
        /// Called by the visual layer when the move animation finishes.
        /// Confirms the new position, reveals the cell if unexplored, and publishes
        /// <see cref="PlayerMoveCompletedEvent"/>. Does nothing if not currently moving.
        /// </summary>
        public void CompleteMoveAnimation()
        {
            if (!IsMoving)
                return;

            CurrentPosition = TargetPosition;
            IsMoving = false;

            // Reveal the newly entered cell if it has not been explored yet.
            if (!_gridMap.IsExplored(CurrentPosition.x, CurrentPosition.y))
            {
                _gridMap.RevealCell(CurrentPosition.x, CurrentPosition.y);
            }

            EventBus.Publish<PlayerMoveCompletedEvent>(new PlayerMoveCompletedEvent
            {
                Position = CurrentPosition
            });
        }

        // ------------------------------------------------------------------
        // Private helpers
        // ------------------------------------------------------------------

        private static Vector2Int GetDirectionOffset(MoveDirection direction)
        {
            switch (direction)
            {
                case MoveDirection.Up:    return new Vector2Int(0, -1);
                case MoveDirection.Down:  return new Vector2Int(0, 1);
                case MoveDirection.Left:  return new Vector2Int(-1, 0);
                case MoveDirection.Right: return new Vector2Int(1, 0);
                default:                  return Vector2Int.zero;
            }
        }
    }
}
