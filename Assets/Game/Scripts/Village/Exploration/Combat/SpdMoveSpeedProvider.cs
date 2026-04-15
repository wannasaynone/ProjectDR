using System;
using UnityEngine;

namespace ProjectDR.Village.Exploration.Combat
{
    /// <summary>
    /// Calculates movement speed based on the player's SPD stat.
    /// Formula: speed = baseSpeed + spd * spdFactor, floored at MinSpeed.
    /// Higher SPD = faster movement speed.
    /// </summary>
    public class SpdMoveSpeedProvider : IMoveSpeedProvider
    {
        private const float MinSpeed = 0.5f;

        private readonly float _speed;

        /// <param name="baseSpeed">Base movement speed in units/second. Must be > 0.</param>
        /// <param name="spdFactor">Speed increase per point of SPD.</param>
        /// <param name="spd">Player's SPD stat value.</param>
        public SpdMoveSpeedProvider(float baseSpeed, float spdFactor, int spd)
        {
            if (baseSpeed <= 0f)
                throw new ArgumentOutOfRangeException(nameof(baseSpeed), "Must be > 0.");

            _speed = Mathf.Max(MinSpeed, baseSpeed + spd * spdFactor);
        }

        /// <summary>
        /// Creates a SpdMoveSpeedProvider from CombatConfig.
        /// Uses FreeMovementBaseSpeed and SpdFreeMovementSpeedFactor from config.
        /// </summary>
        public static SpdMoveSpeedProvider FromConfig(CombatConfig config, int spd)
        {
            if (config == null) throw new ArgumentNullException(nameof(config));
            return new SpdMoveSpeedProvider(config.FreeMovementBaseSpeed, config.SpdFreeMovementSpeedFactor, spd);
        }

        public float GetMoveSpeed()
        {
            return _speed;
        }
    }
}
