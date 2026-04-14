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
    /// - Player steps on hidden monster (unexplored cell) -> take damage, push back to previous cell
    /// - Visible monster cells block player entry (rule 47)
    /// </summary>
    public class CombatManager
    {
        private readonly PlayerCombatStats _playerStats;
        private readonly SwordAttack _swordAttack;
        private readonly MonsterManager _monsterManager;
        private readonly GridMap _gridMap;
        private readonly PlayerGridMovement _playerMovement;
        private readonly ExplorationMapView _mapView;

        private Action<PlayerAttackEvent> _onPlayerAttack;
        private Action<MonsterAttackExecuteEvent> _onMonsterAttackExecute;
        private Action<PlayerMoveCompletedEvent> _onPlayerMoveCompleted;

        /// <summary>
        /// The position the player was at before the current move.
        /// Used for push-back when stepping on hidden monsters.
        /// </summary>
        private Vector2Int _previousPlayerPosition;

        public CombatManager(
            PlayerCombatStats playerStats,
            SwordAttack swordAttack,
            MonsterManager monsterManager,
            GridMap gridMap,
            PlayerGridMovement playerMovement,
            ExplorationMapView mapView)
        {
            _playerStats = playerStats ?? throw new ArgumentNullException(nameof(playerStats));
            _swordAttack = swordAttack ?? throw new ArgumentNullException(nameof(swordAttack));
            _monsterManager = monsterManager ?? throw new ArgumentNullException(nameof(monsterManager));
            _gridMap = gridMap ?? throw new ArgumentNullException(nameof(gridMap));
            _playerMovement = playerMovement ?? throw new ArgumentNullException(nameof(playerMovement));
            _mapView = mapView; // Can be null in tests

            _previousPlayerPosition = playerMovement.CurrentPosition;

            _onPlayerAttack = HandlePlayerAttack;
            _onMonsterAttackExecute = HandleMonsterAttackExecute;
            _onPlayerMoveCompleted = HandlePlayerMoveCompleted;

            EventBus.Subscribe<PlayerAttackEvent>(_onPlayerAttack);
            EventBus.Subscribe<MonsterAttackExecuteEvent>(_onMonsterAttackExecute);
            EventBus.Subscribe<PlayerMoveCompletedEvent>(_onPlayerMoveCompleted);
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
            if (_onPlayerMoveCompleted != null)
                EventBus.Unsubscribe<PlayerMoveCompletedEvent>(_onPlayerMoveCompleted);
        }

        /// <summary>
        /// Checks if a cell contains a visible monster that blocks player entry.
        /// GDD rule 47: Visible monsters on explored cells block player movement.
        /// </summary>
        public bool IsBlockedByVisibleMonster(int x, int y)
        {
            if (!_gridMap.IsExplored(x, y))
                return false;

            return _monsterManager.GetMonsterAt(x, y) != null;
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
            // Check if player is in the attack cells (adjacent in facing direction).
            Vector2Int attackTarget = monster.Position + e.FacingDirection;

            if (_playerMovement.CurrentPosition == attackTarget ||
                (_playerMovement.IsMoving && _playerMovement.TargetPosition == attackTarget))
            {
                int dmg = DamageCalculator.Calculate(monster.TypeData.Atk, _playerStats.Def);
                _playerStats.TakeDamage(dmg);
            }
        }

        private void HandlePlayerMoveCompleted(PlayerMoveCompletedEvent e)
        {
            // GDD rule 15: Stepping on a monster in an unexplored cell -> take damage and push back.
            MonsterState monster = _monsterManager.GetMonsterAt(e.Position.x, e.Position.y);

            if (monster != null)
            {
                // Player stepped on a hidden monster!
                int dmg = DamageCalculator.Calculate(monster.TypeData.Atk, _playerStats.Def);
                _playerStats.TakeDamage(dmg);

                EventBus.Publish(new PlayerSteppedOnMonsterEvent
                {
                    MonsterPosition = e.Position,
                    ReturnPosition = _previousPlayerPosition,
                    DamageDealt = dmg
                });
            }

            _previousPlayerPosition = e.Position;
        }

        /// <summary>
        /// Called by the entry point to record the player's position before each move starts.
        /// </summary>
        public void OnPlayerMoveStarted(Vector2Int fromPosition)
        {
            _previousPlayerPosition = fromPosition;
        }
    }
}
