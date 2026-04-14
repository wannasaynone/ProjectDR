using System;
using KahaGameCore.GameEvent;
using UnityEngine;

namespace ProjectDR.Village.Exploration
{
    /// <summary>
    /// Monitors the player's position and manages the evacuation countdown timer.
    /// When the player arrives at a trigger point (spawn or active evacuation point),
    /// a countdown begins. If the player moves away, the countdown is cancelled.
    /// When the countdown completes, an <see cref="ExplorationCompletedEvent"/> is published.
    ///
    /// This is a pure C# class (no MonoBehaviour). The caller is responsible for:
    /// - Calling <see cref="OnPlayerArrived"/> when the player reaches a new cell.
    /// - Calling <see cref="OnPlayerMoveStarted"/> when the player begins moving.
    /// - Calling <see cref="Update"/> each frame with the elapsed delta time.
    /// </summary>
    public class EvacuationManager
    {
        private readonly GridMap _gridMap;
        private readonly Vector2Int _spawnPosition;
        private readonly float _evacuationDuration;

        private float _remainingTime;
        private bool _isEvacuating;
        private bool _isCompleted;

        /// <summary>Whether the evacuation countdown is currently active.</summary>
        public bool IsEvacuating => _isEvacuating;

        /// <summary>
        /// Remaining countdown time in seconds. Returns 0 when not evacuating.
        /// </summary>
        public float RemainingTime => _isEvacuating ? _remainingTime : (_isCompleted ? 0f : 0f);

        /// <summary>
        /// Countdown progress from 0 (just started) to 1 (complete).
        /// Returns 0 when not evacuating, 1 when completed.
        /// </summary>
        public float Progress
        {
            get
            {
                if (_isCompleted) return 1f;
                if (!_isEvacuating) return 0f;
                return 1f - (_remainingTime / _evacuationDuration);
            }
        }

        /// <summary>
        /// Whether the evacuation has completed. Once true, this never changes back.
        /// </summary>
        public bool IsCompleted => _isCompleted;

        /// <param name="gridMap">The grid map used to check evacuation point positions.</param>
        /// <param name="spawnPosition">The spawn position, which also acts as a trigger point.</param>
        /// <param name="evacuationDuration">
        /// Countdown duration in seconds. Must be greater than 0.
        /// GDD specifies 6 seconds, but the value is externalized here.
        /// </param>
        /// <exception cref="ArgumentNullException">Thrown when gridMap is null.</exception>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when evacuationDuration is &lt;= 0.</exception>
        public EvacuationManager(GridMap gridMap, Vector2Int spawnPosition, float evacuationDuration)
        {
            _gridMap = gridMap ?? throw new ArgumentNullException(nameof(gridMap));

            if (evacuationDuration <= 0f)
                throw new ArgumentOutOfRangeException(nameof(evacuationDuration),
                    "evacuationDuration must be greater than 0.");

            _spawnPosition = spawnPosition;
            _evacuationDuration = evacuationDuration;
        }

        /// <summary>
        /// Returns true if the given position is a trigger point for evacuation
        /// (either the spawn position or an active evacuation point).
        /// </summary>
        public bool IsEvacuationTrigger(int x, int y)
        {
            if (x == _spawnPosition.x && y == _spawnPosition.y)
                return true;

            return _gridMap.IsEvacuationPoint(x, y);
        }

        /// <summary>
        /// Called when the player arrives at a new cell.
        /// If the cell is a trigger point and no countdown is active, starts the countdown.
        /// Does nothing if already evacuating or already completed.
        /// </summary>
        public void OnPlayerArrived(Vector2Int position)
        {
            if (_isCompleted) return;
            if (_isEvacuating) return;

            if (!IsEvacuationTrigger(position.x, position.y)) return;

            _isEvacuating = true;
            _remainingTime = _evacuationDuration;
            EventBus.Publish<EvacuationStartedEvent>(
                new EvacuationStartedEvent { Duration = _evacuationDuration });
        }

        /// <summary>
        /// Called when the player begins moving away from the current cell.
        /// Cancels the active countdown if one is running.
        /// Does nothing if not evacuating or already completed.
        /// </summary>
        public void OnPlayerMoveStarted()
        {
            if (_isCompleted) return;
            if (!_isEvacuating) return;

            _isEvacuating = false;
            _remainingTime = 0f;
            EventBus.Publish<EvacuationCancelledEvent>(new EvacuationCancelledEvent());
        }

        /// <summary>
        /// Advances the countdown timer by <paramref name="deltaTime"/> seconds.
        /// When the timer reaches zero, the evacuation completes and
        /// <see cref="ExplorationCompletedEvent"/> is published.
        /// Does nothing when not evacuating, already completed, or deltaTime &lt;= 0.
        /// </summary>
        public void Update(float deltaTime)
        {
            if (_isCompleted) return;
            if (!_isEvacuating) return;
            if (deltaTime <= 0f) return;

            _remainingTime -= deltaTime;

            if (_remainingTime <= 0f)
            {
                _remainingTime = 0f;
                _isEvacuating = false;
                _isCompleted = true;
                EventBus.Publish<ExplorationCompletedEvent>(new ExplorationCompletedEvent());
            }
        }
    }
}
