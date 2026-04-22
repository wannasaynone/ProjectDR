using ProjectDR.Village.Exploration.Combat;
using System;
using UnityEngine;

namespace ProjectDR.Village.Exploration.MoveSpeed
{
    /// <summary>
    /// Calculates move duration based on the player's SPD stat.
    /// Formula: duration = moveSpeedBase - spd * spdMoveSpeedFactor, floored at MinDuration.
    /// Higher SPD = shorter move duration = faster movement.
    /// </summary>
    public class SpdMoveSpeedCalculator : IMoveSpeedCalculator
    {
        private const float MinDuration = 0.05f;

        private readonly float _duration;

        public SpdMoveSpeedCalculator(float moveSpeedBase, float spdMoveSpeedFactor, int spd)
        {
            if (moveSpeedBase <= 0f)
                throw new ArgumentOutOfRangeException(nameof(moveSpeedBase), "Must be > 0.");

            _duration = Mathf.Max(MinDuration, moveSpeedBase - spd * spdMoveSpeedFactor);
        }

        public static SpdMoveSpeedCalculator FromConfig(CombatConfig config, int spd)
        {
            if (config == null) throw new ArgumentNullException(nameof(config));
            return new SpdMoveSpeedCalculator(config.MoveSpeedBase, config.SpdMoveSpeedFactor, spd);
        }

        public float CalculateMoveDuration()
        {
            return _duration;
        }
    }
}
