using System;
using KahaGameCore.GameEvent;
using UnityEngine;

namespace ProjectDR.Village.Exploration.Combat
{
    /// <summary>
    /// Manages the sword attack logic: cooldown, range check, and sector hit detection.
    /// GDD rule 14: Attack direction follows mouse. Left-click to attack (top-down, not grid-restricted).
    /// GDD rule 31: Sword = forward sector damage.
    /// SPD affects attack cooldown: actualCooldown = baseCooldown - spd * factor.
    /// </summary>
    public class SwordAttack
    {
        private readonly float _angleHalf;
        private readonly float _range;
        private readonly float _cooldown;

        private float _cooldownRemaining;

        /// <summary>Whether the sword can attack (cooldown is ready).</summary>
        public bool CanAttack => _cooldownRemaining <= 0f;

        /// <summary>Current cooldown remaining in seconds.</summary>
        public float CooldownRemaining => _cooldownRemaining;

        /// <summary>The half-angle of the sword sweep sector in degrees.</summary>
        public float AngleHalf => _angleHalf;

        /// <summary>Attack range (world units).</summary>
        public float Range => _range;

        /// <summary>Calculated cooldown duration in seconds.</summary>
        public float Cooldown => _cooldown;

        /// <param name="angleHalf">Half-angle of the sweep sector in degrees.</param>
        /// <param name="range">Attack range in world units.</param>
        /// <param name="baseCooldown">Base cooldown in seconds before SPD reduction.</param>
        /// <param name="spdCooldownFactor">Cooldown reduction per point of SPD.</param>
        /// <param name="spd">Player's SPD stat.</param>
        public SwordAttack(float angleHalf, float range, float baseCooldown, float spdCooldownFactor, int spd)
        {
            if (range <= 0f) throw new ArgumentOutOfRangeException(nameof(range));
            if (baseCooldown <= 0f) throw new ArgumentOutOfRangeException(nameof(baseCooldown));

            _angleHalf = angleHalf;
            _range = range;

            // SPD reduces cooldown, but floor at 0.1s to prevent degenerate values.
            float MinCooldown = 0.1f;
            _cooldown = Mathf.Max(MinCooldown, baseCooldown - spd * spdCooldownFactor);
            _cooldownRemaining = 0f;
        }

        /// <summary>
        /// Constructs a SwordAttack from config + player SPD.
        /// </summary>
        public static SwordAttack FromConfig(CombatConfig config, int spd)
        {
            if (config == null) throw new ArgumentNullException(nameof(config));
            return new SwordAttack(
                config.SwordAngleHalf,
                config.SwordRange,
                config.SwordBaseCooldown,
                config.SwordSpdCooldownFactor,
                spd);
        }

        /// <summary>
        /// Attempts to perform a sword attack. Starts cooldown and publishes PlayerAttackEvent.
        /// </summary>
        /// <param name="origin">World-space origin of the attack.</param>
        /// <param name="direction">Normalized direction toward target (mouse).</param>
        /// <returns>True if the attack was performed; false if still on cooldown.</returns>
        public bool TryAttack(Vector2 origin, Vector2 direction)
        {
            if (_cooldownRemaining > 0f)
                return false;

            if (direction.sqrMagnitude < 0.001f)
                return false;

            direction = direction.normalized;
            _cooldownRemaining = _cooldown;

            EventBus.Publish(new PlayerAttackEvent
            {
                Origin = origin,
                Direction = direction,
                AngleHalf = _angleHalf,
                Range = _range
            });

            return true;
        }

        /// <summary>
        /// Checks if a target position is within the sword's attack sector.
        /// </summary>
        /// <param name="origin">Attack origin.</param>
        /// <param name="direction">Attack direction (normalized).</param>
        /// <param name="targetPosition">Position to check.</param>
        /// <returns>True if the target is within range and angle.</returns>
        public bool IsInSector(Vector2 origin, Vector2 direction, Vector2 targetPosition)
        {
            Vector2 toTarget = targetPosition - origin;
            float dist = toTarget.magnitude;

            if (dist > _range || dist < 0.001f)
                return false;

            float angle = Vector2.Angle(direction, toTarget);
            return angle <= _angleHalf;
        }

        /// <summary>
        /// Updates the cooldown timer.
        /// </summary>
        public void Update(float deltaTime)
        {
            if (_cooldownRemaining > 0f)
            {
                _cooldownRemaining -= deltaTime;
                if (_cooldownRemaining < 0f)
                    _cooldownRemaining = 0f;
            }
        }
    }
}
