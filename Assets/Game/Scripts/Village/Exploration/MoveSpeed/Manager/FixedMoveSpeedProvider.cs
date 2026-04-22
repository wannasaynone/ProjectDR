using System;

namespace ProjectDR.Village.Exploration.MoveSpeed
{
    /// <summary>Returns a fixed, pre-configured movement speed regardless of game state.</summary>
    public class FixedMoveSpeedProvider : IMoveSpeedProvider
    {
        private readonly float _speed;

        /// <summary>
        /// Creates a provider that always returns the given speed.
        /// </summary>
        /// <param name="speed">The fixed movement speed in units/second. Must be greater than 0.</param>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when speed is less than or equal to 0.</exception>
        public FixedMoveSpeedProvider(float speed)
        {
            if (speed <= 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(speed), "speed must be greater than 0.");
            }

            _speed = speed;
        }

        /// <inheritdoc />
        public float GetMoveSpeed()
        {
            return _speed;
        }
    }
}
