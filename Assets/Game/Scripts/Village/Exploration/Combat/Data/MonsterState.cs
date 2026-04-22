using System;
using UnityEngine;

namespace ProjectDR.Village.Exploration.Combat
{
    /// <summary>
    /// AI states for a single monster.
    /// </summary>
    public enum MonsterAIState
    {
        Idle,
        Roaming,
        Chasing,
        AttackPreparing,
        AttackCooldown
    }

    /// <summary>
    /// Runtime state for a single monster instance.
    /// </summary>
    public class MonsterState
    {
        private static int _nextId = 1;

        /// <summary>Unique runtime ID for this monster instance.</summary>
        public int Id { get; }

        /// <summary>Reference to the static type data.</summary>
        public MonsterTypeData TypeData { get; }

        /// <summary>Current grid position.</summary>
        public Vector2Int Position { get; set; }

        /// <summary>Facing direction (used for attack direction calculation).</summary>
        public Vector2Int FacingDirection { get; set; }

        /// <summary>Current HP.</summary>
        public int CurrentHp { get; private set; }

        /// <summary>Whether this monster is dead.</summary>
        public bool IsDead => CurrentHp <= 0;

        /// <summary>Current AI state.</summary>
        public MonsterAIState AIState { get; set; }

        /// <summary>Timer for current state (movement cooldown, attack prepare, attack cooldown).</summary>
        public float StateTimer { get; set; }

        /// <summary>Movement cooldown remaining.</summary>
        public float MoveCooldownRemaining { get; set; }

        public MonsterState(MonsterTypeData typeData, Vector2Int position)
        {
            TypeData = typeData ?? throw new ArgumentNullException(nameof(typeData));
            Id = _nextId++;
            Position = position;
            FacingDirection = new Vector2Int(0, -1); // Default facing up
            CurrentHp = typeData.MaxHp;
            AIState = MonsterAIState.Idle;
            StateTimer = 0f;
            MoveCooldownRemaining = typeData.MoveCooldownSeconds;
        }

        /// <summary>
        /// Applies damage to this monster. Minimum 0 HP.
        /// </summary>
        /// <returns>Actual damage dealt.</returns>
        public int TakeDamage(int damage)
        {
            if (damage <= 0) return 0;
            int actual = Math.Min(damage, CurrentHp);
            CurrentHp -= actual;
            return actual;
        }

        /// <summary>
        /// Resets the static ID counter. Used for testing only.
        /// </summary>
        public static void ResetIdCounter()
        {
            _nextId = 1;
        }
    }
}
