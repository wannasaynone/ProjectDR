using KahaGameCore.GameEvent;
using UnityEngine;

namespace ProjectDR.Village.Exploration.Combat
{
    /// <summary>Published when the player's HP changes.</summary>
    public class PlayerHpChangedEvent : GameEventBase
    {
        public int CurrentHp;
        public int MaxHp;
        /// <summary>Positive = damage taken, Negative = healed.</summary>
        public int DamageDealt;
    }

    /// <summary>Published when the player dies (HP reaches 0).</summary>
    public class PlayerDiedEvent : GameEventBase { }

    /// <summary>Published when the player performs a sword attack.</summary>
    public class PlayerAttackEvent : GameEventBase
    {
        /// <summary>World-space origin of the attack.</summary>
        public Vector2 Origin;
        /// <summary>Normalized direction of the attack (toward mouse).</summary>
        public Vector2 Direction;
        /// <summary>Half-angle of the sword sweep in degrees.</summary>
        public float AngleHalf;
        /// <summary>Range of the attack.</summary>
        public float Range;
    }

    /// <summary>Published when a monster takes damage.</summary>
    public class MonsterDamagedEvent : GameEventBase
    {
        public int MonsterId;
        public int Damage;
        public int RemainingHp;
        public Vector2Int Position;
    }

    /// <summary>Published when a monster dies.</summary>
    public class MonsterDiedEvent : GameEventBase
    {
        public int MonsterId;
        public Vector2Int Position;
    }

    /// <summary>Published when a monster starts preparing an attack.</summary>
    public class MonsterAttackPrepareEvent : GameEventBase
    {
        public int MonsterId;
        public Vector2Int Position;
        public float PrepareSeconds;
        /// <summary>The grid cell that will be hit when the attack executes.</summary>
        public Vector2Int AttackTargetPosition;
    }

    /// <summary>Published when a monster executes its attack.</summary>
    public class MonsterAttackExecuteEvent : GameEventBase
    {
        public int MonsterId;
        public Vector2Int Position;
        public Vector2Int FacingDirection;
    }

    /// <summary>Published when the player steps on a hidden monster and gets pushed back.</summary>
    public class PlayerSteppedOnMonsterEvent : GameEventBase
    {
        /// <summary>The position where the monster was hidden.</summary>
        public Vector2Int MonsterPosition;
        /// <summary>The position the player is pushed back to.</summary>
        public Vector2Int ReturnPosition;
        public int DamageDealt;
    }

    /// <summary>Published when a monster changes position.</summary>
    public class MonsterMovedEvent : GameEventBase
    {
        public int MonsterId;
        public Vector2Int From;
        public Vector2Int To;
    }

    /// <summary>Published when a monster spawns on the map.</summary>
    public class MonsterSpawnedEvent : GameEventBase
    {
        public int MonsterId;
        public Vector2Int Position;
        public string TypeId;
    }

    /// <summary>
    /// Published by DeathManager when the death sequence begins.
    /// At this point, backpack has already been restored to pre-departure snapshot.
    /// </summary>
    public class PlayerDeathEvent : GameEventBase { }

    /// <summary>
    /// Published when the death rewind visual sequence completes.
    /// DeathView publishes this after the screen overlay animation finishes.
    /// </summary>
    public class DeathRewindCompletedEvent : GameEventBase { }
}
