using ProjectDR.Village.Exploration.Camera;
using ProjectDR.Village.Exploration.Movement;
using ProjectDR.Village.Exploration.Map;
using System;
using System.Collections.Generic;
using KahaGameCore.GameEvent;
using UnityEngine;

namespace ProjectDR.Village.Exploration.Combat
{
    /// <summary>
    /// Manages combat interactions between the player and monsters:
    /// - Player sword attack -> hit detection -> damage monsters
    /// - Monster attack execution -> damage player
    /// - Contact damage: player collides with monster -> take damage + knockback
    /// </summary>
    public class CombatManager
    {
        private readonly PlayerCombatStats _playerStats;
        private readonly SwordAttack _swordAttack;
        private readonly MonsterManager _monsterManager;
        private readonly GridMap _gridMap;
        private readonly PlayerFreeMovement _playerMovement;
        private readonly ExplorationMapView _mapView;
        private readonly float _knockbackDistance;

        private Action<PlayerAttackEvent> _onPlayerAttack;
        private Action<MonsterAttackExecuteEvent> _onMonsterAttackExecute;
        private Action<PlayerContactDamageEvent> _onPlayerContactDamage;

        /// <param name="playerStats">Player combat stats.</param>
        /// <param name="swordAttack">Sword attack logic.</param>
        /// <param name="monsterManager">Monster management.</param>
        /// <param name="gridMap">Grid map for exploration checks.</param>
        /// <param name="playerMovement">Player free movement.</param>
        /// <param name="mapView">Map view for coordinate conversion. Can be null in tests.</param>
        /// <param name="knockbackDistance">Knockback distance in world units on contact.</param>
        public CombatManager(
            PlayerCombatStats playerStats,
            SwordAttack swordAttack,
            MonsterManager monsterManager,
            GridMap gridMap,
            PlayerFreeMovement playerMovement,
            ExplorationMapView mapView,
            float knockbackDistance)
        {
            _playerStats = playerStats ?? throw new ArgumentNullException(nameof(playerStats));
            _swordAttack = swordAttack ?? throw new ArgumentNullException(nameof(swordAttack));
            _monsterManager = monsterManager ?? throw new ArgumentNullException(nameof(monsterManager));
            _gridMap = gridMap ?? throw new ArgumentNullException(nameof(gridMap));
            _playerMovement = playerMovement ?? throw new ArgumentNullException(nameof(playerMovement));
            _mapView = mapView; // Can be null in tests
            _knockbackDistance = knockbackDistance;

            _onPlayerAttack = HandlePlayerAttack;
            _onMonsterAttackExecute = HandleMonsterAttackExecute;
            _onPlayerContactDamage = HandlePlayerContactDamage;

            EventBus.Subscribe<PlayerAttackEvent>(_onPlayerAttack);
            EventBus.Subscribe<MonsterAttackExecuteEvent>(_onMonsterAttackExecute);
            EventBus.Subscribe<PlayerContactDamageEvent>(_onPlayerContactDamage);
        }

        /// <summary>
        /// Unsubscribes from all events. Call on cleanup.
        /// </summary>
        public void Dispose()
        {
            if (_onPlayerAttack != null)
                EventBus.Unsubscribe<PlayerAttackEvent>(_onPlayerAttack);
            if (_onMonsterAttackExecute != null)
                EventBus.Unsubscribe<MonsterAttackExecuteEvent>(_onMonsterAttackExecute);
            if (_onPlayerContactDamage != null)
                EventBus.Unsubscribe<PlayerContactDamageEvent>(_onPlayerContactDamage);
        }

        // ------------------------------------------------------------------
        // Event handlers
        // ------------------------------------------------------------------

        private void HandlePlayerAttack(PlayerAttackEvent e)
        {
            // Check all monsters for hits in the sword sector
            IReadOnlyList<MonsterState> monsters = _monsterManager.Monsters;
            for (int i = monsters.Count - 1; i >= 0; i--)
            {
                MonsterState monster = monsters[i];
                if (monster.IsDead) continue;

                Vector2 monsterWorldPos;
                if (_mapView != null)
                {
                    Vector3 wp = _mapView.GridToWorldPosition(monster.Position.x, monster.Position.y);
                    monsterWorldPos = new Vector2(wp.x, wp.y);
                }
                else
                {
                    // Fallback for tests without view
                    monsterWorldPos = new Vector2(monster.Position.x, -monster.Position.y);
                }

                if (_swordAttack.IsInSector(e.Origin, e.Direction, monsterWorldPos))
                {
                    int dmg = DamageCalculator.Calculate(_playerStats.Atk, monster.TypeData.Def);
                    _monsterManager.DamageMonster(monster.Id, dmg);
                }
            }
        }

        private void HandleMonsterAttackExecute(MonsterAttackExecuteEvent e)
        {
            MonsterState monster = _monsterManager.GetMonsterById(e.MonsterId);
            if (monster == null || monster.IsDead) return;

            // GDD rule 18: Monster attack is grid-restricted based on facing direction.
            Vector2Int attackTarget = monster.Position + e.FacingDirection;
            Vector2Int playerCell = _playerMovement.CurrentGridCell;

            if (playerCell == attackTarget)
            {
                int dmg = DamageCalculator.Calculate(monster.TypeData.Atk, _playerStats.Def);
                _playerStats.TakeDamage(dmg);
            }
        }

        private void HandlePlayerContactDamage(PlayerContactDamageEvent e)
        {
            MonsterState monster = _monsterManager.GetMonsterById(e.MonsterId);
            if (monster == null || monster.IsDead) return;

            // Apply damage
            int dmg = DamageCalculator.Calculate(monster.TypeData.Atk, _playerStats.Def);
            _playerStats.TakeDamage(dmg);

            // Apply knockback
            if (_knockbackDistance > 0f && e.KnockbackDirection.sqrMagnitude > 0.001f)
            {
                _playerMovement.ApplyKnockback(e.KnockbackDirection, _knockbackDistance);

                EventBus.Publish(new PlayerKnockbackEvent
                {
                    Direction = e.KnockbackDirection,
                    Distance = _knockbackDistance
                });
            }
        }
    }
}
