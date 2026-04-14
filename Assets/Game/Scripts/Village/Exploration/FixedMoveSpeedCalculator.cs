using System;

namespace ProjectDR.Village.Exploration
{
    /// <summary>Returns a fixed, pre-configured move duration regardless of game state.</summary>
    public class FixedMoveSpeedCalculator : IMoveSpeedCalculator
    {
        private readonly float _moveDuration;

        /// <summary>
        /// Creates a calculator that always returns the given duration.
        /// </summary>
        /// <param name="moveDuration">The fixed move duration in seconds. Must be greater than 0.</param>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when moveDuration is less than or equal to 0.</exception>
        public FixedMoveSpeedCalculator(float moveDuration)
        {
            if (moveDuration <= 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(moveDuration), "moveDuration must be greater than 0.");
            }

            _moveDuration = moveDuration;
        }

        /// <inheritdoc />
        public float CalculateMoveDuration()
        {
            return _moveDuration;
        }
    }
}
