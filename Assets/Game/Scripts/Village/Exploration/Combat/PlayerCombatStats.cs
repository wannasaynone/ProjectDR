using System;
using KahaGameCore.GameEvent;

namespace ProjectDR.Village.Exploration.Combat
{
    /// <summary>
    /// Runtime container for the player's combat stats (HP/ATK/DEF/SPD).
    /// Initialized from CombatConfig. Publishes events on HP change.
    /// </summary>
    public class PlayerCombatStats
    {
        private int _currentHp;

        /// <summary>Maximum HP from config.</summary>
        public int MaxHp { get; }

        /// <summary>Current HP. Clamped to [0, MaxHp].</summary>
        public int CurrentHp => _currentHp;

        /// <summary>Attack power.</summary>
        public int Atk { get; }

        /// <summary>Defense power.</summary>
        public int Def { get; }

        /// <summary>Speed stat (affects attack cooldown and move speed).</summary>
        public int Spd { get; }

        /// <summary>Whether the player is dead (HP <= 0).</summary>
        public bool IsDead => _currentHp <= 0;

        public PlayerCombatStats(int maxHp, int atk, int def, int spd)
        {
            if (maxHp <= 0) throw new ArgumentOutOfRangeException(nameof(maxHp), "maxHp must be > 0.");

            MaxHp = maxHp;
            _currentHp = maxHp;
            Atk = atk;
            Def = def;
            Spd = spd;
        }

        /// <summary>
        /// Creates PlayerCombatStats from a CombatConfig.
        /// </summary>
        public static PlayerCombatStats FromConfig(CombatConfig config)
        {
            if (config == null) throw new ArgumentNullException(nameof(config));
            return new PlayerCombatStats(config.PlayerMaxHp, config.PlayerAtk, config.PlayerDef, config.PlayerSpd);
        }

        /// <summary>
        /// Applies damage to the player. Damage is clamped so HP does not go below 0.
        /// Publishes PlayerHpChangedEvent.
        /// </summary>
        /// <returns>Actual damage dealt.</returns>
        public int TakeDamage(int damage)
        {
            if (damage <= 0) return 0;

            int actual = Math.Min(damage, _currentHp);
            _currentHp -= actual;

            EventBus.Publish(new PlayerHpChangedEvent
            {
                CurrentHp = _currentHp,
                MaxHp = MaxHp,
                DamageDealt = actual
            });

            if (_currentHp <= 0)
            {
                EventBus.Publish(new PlayerDiedEvent());
            }

            return actual;
        }

        /// <summary>
        /// Heals the player. HP is clamped to MaxHp.
        /// </summary>
        /// <returns>Actual amount healed.</returns>
        public int Heal(int amount)
        {
            if (amount <= 0) return 0;

            int actual = Math.Min(amount, MaxHp - _currentHp);
            _currentHp += actual;

            if (actual > 0)
            {
                EventBus.Publish(new PlayerHpChangedEvent
                {
                    CurrentHp = _currentHp,
                    MaxHp = MaxHp,
                    DamageDealt = -actual
                });
            }

            return actual;
        }
    }
}
