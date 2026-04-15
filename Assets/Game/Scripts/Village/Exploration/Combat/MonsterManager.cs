using System;
using System.Collections.Generic;
using KahaGameCore.GameEvent;
using UnityEngine;

namespace ProjectDR.Village.Exploration.Combat
{
    /// <summary>
    /// Manages all active monsters: spawning, AI update, death removal.
    /// Implements IMonsterPositionProvider so GridMap can query monster positions.
    ///
    /// AI Behavior per GDD:
    /// - Rule 16: Monsters move on cooldown. After moving, explored cell numbers recalculate.
    /// - Rule 17: Monsters have a vision range. If player enters vision, chase via shortest path.
    /// - Rule 18: Monster attacks are grid-restricted (based on facing direction).
    /// - Rule 19: Monster attacks have prepare time and cooldown time.
    /// </summary>
    public class MonsterManager : IMonsterPositionProvider
    {
        private readonly GridMap _gridMap;
        private readonly List<MonsterState> _monsters;
        private readonly List<Vector2Int> _positionCache;
        private readonly System.Random _rng;

        /// <summary>All active (alive) monsters.</summary>
        public IReadOnlyList<MonsterState> Monsters => _monsters;

        /// <summary>Callback invoked when gridMap needs recalculation after monster movement.</summary>
        private readonly Action _onMonstersChanged;

        public MonsterManager(GridMap gridMap, Action onMonstersChanged = null, int? seed = null)
        {
            _gridMap = gridMap ?? throw new ArgumentNullException(nameof(gridMap));
            _monsters = new List<MonsterState>();
            _positionCache = new List<Vector2Int>();
            _onMonstersChanged = onMonstersChanged;
            _rng = seed.HasValue ? new System.Random(seed.Value) : new System.Random();
        }

        /// <summary>
        /// Spawns a monster of the given type at the given position.
        /// </summary>
        public MonsterState SpawnMonster(MonsterTypeData typeData, Vector2Int position)
        {
            if (typeData == null) throw new ArgumentNullException(nameof(typeData));

            MonsterState state = new MonsterState(typeData, position);
            _monsters.Add(state);
            UpdatePositionCache();

            EventBus.Publish(new MonsterSpawnedEvent
            {
                MonsterId = state.Id,
                Position = position,
                TypeId = typeData.TypeId
            });

            return state;
        }

        /// <summary>
        /// Returns the monster at the given position, or null if none.
        /// </summary>
        public MonsterState GetMonsterAt(int x, int y)
        {
            for (int i = 0; i < _monsters.Count; i++)
            {
                if (_monsters[i].Position.x == x && _monsters[i].Position.y == y)
                    return _monsters[i];
            }
            return null;
        }

        /// <summary>
        /// Returns the monster by ID, or null.
        /// </summary>
        public MonsterState GetMonsterById(int id)
        {
            for (int i = 0; i < _monsters.Count; i++)
            {
                if (_monsters[i].Id == id)
                    return _monsters[i];
            }
            return null;
        }

        /// <summary>
        /// Updates all monster AI. Called each frame.
        /// </summary>
        /// <param name="deltaTime">Frame delta time in seconds.</param>
        /// <param name="playerPosition">Current player grid position.</param>
        public void Update(float deltaTime, Vector2Int playerPosition)
        {
            if (deltaTime <= 0f) return;

            bool anyMoved = false;

            for (int i = _monsters.Count - 1; i >= 0; i--)
            {
                MonsterState monster = _monsters[i];
                if (monster.IsDead) continue;

                switch (monster.AIState)
                {
                    case MonsterAIState.Idle:
                    case MonsterAIState.Roaming:
                        anyMoved |= UpdateRoamingOrChasing(monster, deltaTime, playerPosition);
                        break;

                    case MonsterAIState.Chasing:
                        anyMoved |= UpdateRoamingOrChasing(monster, deltaTime, playerPosition);
                        break;

                    case MonsterAIState.AttackPreparing:
                        UpdateAttackPreparing(monster, deltaTime, playerPosition);
                        break;

                    case MonsterAIState.AttackCooldown:
                        UpdateAttackCooldown(monster, deltaTime, playerPosition);
                        break;
                }
            }

            if (anyMoved)
            {
                UpdatePositionCache();
                _onMonstersChanged?.Invoke();
            }
        }

        /// <summary>
        /// Applies damage to a monster. Removes it if dead.
        /// </summary>
        public void DamageMonster(int monsterId, int damage)
        {
            MonsterState monster = GetMonsterById(monsterId);
            if (monster == null || monster.IsDead) return;

            int actual = monster.TakeDamage(damage);

            EventBus.Publish(new MonsterDamagedEvent
            {
                MonsterId = monsterId,
                Damage = actual,
                RemainingHp = monster.CurrentHp,
                Position = monster.Position
            });

            if (monster.IsDead)
            {
                EventBus.Publish(new MonsterDiedEvent
                {
                    MonsterId = monsterId,
                    Position = monster.Position
                });

                _monsters.Remove(monster);
                UpdatePositionCache();
                _onMonstersChanged?.Invoke();
            }
        }

        /// <summary>
        /// Removes all dead monsters from the list.
        /// </summary>
        public void CleanupDead()
        {
            bool changed = false;
            for (int i = _monsters.Count - 1; i >= 0; i--)
            {
                if (_monsters[i].IsDead)
                {
                    _monsters.RemoveAt(i);
                    changed = true;
                }
            }
            if (changed)
            {
                UpdatePositionCache();
                _onMonstersChanged?.Invoke();
            }
        }

        // IMonsterPositionProvider
        public IReadOnlyList<Vector2Int> GetMonsterPositions()
        {
            return _positionCache;
        }

        // ------------------------------------------------------------------
        // AI logic
        // ------------------------------------------------------------------

        private bool UpdateRoamingOrChasing(MonsterState monster, float deltaTime, Vector2Int playerPos)
        {
            // X10: Monsters on unexplored cells only roam — no vision/chase/attack
            bool monsterOnExploredCell = _gridMap.IsExplored(monster.Position.x, monster.Position.y);

            if (monsterOnExploredCell)
            {
                // Check if player is in vision range -> switch to chasing
                int dist = ManhattanDistance(monster.Position, playerPos);
                bool playerInVision = dist <= monster.TypeData.VisionRange;

                // Check if player is adjacent (attack range) -> start attack
                if (dist <= monster.TypeData.AttackRange && dist > 0)
                {
                    monster.AIState = MonsterAIState.AttackPreparing;
                    monster.StateTimer = monster.TypeData.AttackPrepareSeconds;
                    // Face toward player
                    monster.FacingDirection = GetFacingDirection(monster.Position, playerPos);

                    EventBus.Publish(new MonsterAttackPrepareEvent
                    {
                        MonsterId = monster.Id,
                        Position = monster.Position,
                        PrepareSeconds = monster.TypeData.AttackPrepareSeconds,
                        AttackTargetPosition = monster.Position + monster.FacingDirection
                    });
                    return false;
                }

                // Movement cooldown
                monster.MoveCooldownRemaining -= deltaTime;
                if (monster.MoveCooldownRemaining > 0f)
                    return false;

                monster.MoveCooldownRemaining = monster.TypeData.MoveCooldownSeconds;

                Vector2Int newPos;
                if (playerInVision)
                {
                    monster.AIState = MonsterAIState.Chasing;
                    newPos = GetChaseStep(monster.Position, playerPos, monsterOnExploredCell);
                }
                else
                {
                    monster.AIState = MonsterAIState.Roaming;
                    newPos = GetRandomStep(monster.Position, monsterOnExploredCell);
                }

                if (newPos != monster.Position)
                {
                    Vector2Int oldPos = monster.Position;
                    monster.FacingDirection = GetFacingDirection(oldPos, newPos);
                    monster.Position = newPos;

                    EventBus.Publish(new MonsterMovedEvent
                    {
                        MonsterId = monster.Id,
                        From = oldPos,
                        To = newPos
                    });
                    return true;
                }

                return false;
            }
            else
            {
                // X10: Monster on unexplored cell — only roam, no chase/attack
                monster.MoveCooldownRemaining -= deltaTime;
                if (monster.MoveCooldownRemaining > 0f)
                    return false;

                monster.MoveCooldownRemaining = monster.TypeData.MoveCooldownSeconds;
                monster.AIState = MonsterAIState.Roaming;

                Vector2Int newPos = GetRandomStep(monster.Position, monsterOnExploredCell);

                if (newPos != monster.Position)
                {
                    Vector2Int oldPos = monster.Position;
                    monster.FacingDirection = GetFacingDirection(oldPos, newPos);
                    monster.Position = newPos;

                    EventBus.Publish(new MonsterMovedEvent
                    {
                        MonsterId = monster.Id,
                        From = oldPos,
                        To = newPos
                    });
                    return true;
                }

                return false;
            }
        }

        private void UpdateAttackPreparing(MonsterState monster, float deltaTime, Vector2Int playerPos)
        {
            monster.StateTimer -= deltaTime;
            if (monster.StateTimer <= 0f)
            {
                // Execute attack
                EventBus.Publish(new MonsterAttackExecuteEvent
                {
                    MonsterId = monster.Id,
                    Position = monster.Position,
                    FacingDirection = monster.FacingDirection
                });

                monster.AIState = MonsterAIState.AttackCooldown;
                monster.StateTimer = monster.TypeData.AttackCooldownSeconds;
            }
        }

        private void UpdateAttackCooldown(MonsterState monster, float deltaTime, Vector2Int playerPos)
        {
            monster.StateTimer -= deltaTime;
            if (monster.StateTimer <= 0f)
            {
                monster.AIState = MonsterAIState.Idle;
                monster.StateTimer = 0f;
            }
        }

        private Vector2Int GetChaseStep(Vector2Int from, Vector2Int target, bool currentCellExplored)
        {
            // Simple greedy: move one step toward player (prefer axis with larger distance)
            int dx = target.x - from.x;
            int dy = target.y - from.y;

            Vector2Int best = from;
            int bestDist = ManhattanDistance(from, target);

            // Try horizontal then vertical
            Vector2Int[] candidates = new Vector2Int[4];
            candidates[0] = from + new Vector2Int(dx > 0 ? 1 : (dx < 0 ? -1 : 0), 0);
            candidates[1] = from + new Vector2Int(0, dy > 0 ? 1 : (dy < 0 ? -1 : 0));
            candidates[2] = from + new Vector2Int(dx > 0 ? 1 : -1, 0);
            candidates[3] = from + new Vector2Int(0, dy > 0 ? 1 : -1);

            for (int i = 0; i < candidates.Length; i++)
            {
                Vector2Int c = candidates[i];
                if (c == from) continue;
                if (!IsWalkableForMonster(c, currentCellExplored)) continue;

                int d = ManhattanDistance(c, target);
                if (d < bestDist)
                {
                    bestDist = d;
                    best = c;
                    break; // Take the first improving step
                }
            }

            return best;
        }

        private Vector2Int GetRandomStep(Vector2Int from, bool currentCellExplored)
        {
            // Try random directions
            int startDir = _rng.Next(4);
            Vector2Int[] offsets = new Vector2Int[]
            {
                new Vector2Int(0, -1), new Vector2Int(0, 1),
                new Vector2Int(-1, 0), new Vector2Int(1, 0)
            };

            for (int i = 0; i < 4; i++)
            {
                int idx = (startDir + i) % 4;
                Vector2Int candidate = from + offsets[idx];
                if (IsWalkableForMonster(candidate, currentCellExplored))
                    return candidate;
            }

            return from; // Can't move
        }

        /// <summary>
        /// Checks if a monster can move to the given position.
        /// X9: Monster cannot cross explored/unexplored boundary.
        /// </summary>
        /// <param name="pos">Target position.</param>
        /// <param name="currentCellExplored">Whether the monster's current cell is explored.</param>
        private bool IsWalkableForMonster(Vector2Int pos, bool currentCellExplored)
        {
            if (!_gridMap.IsWalkable(pos.x, pos.y))
                return false;

            // X9: Cannot cross explored/unexplored boundary
            bool targetExplored = _gridMap.IsExplored(pos.x, pos.y);
            if (targetExplored != currentCellExplored)
                return false;

            // Don't overlap with other monsters
            for (int i = 0; i < _monsters.Count; i++)
            {
                if (_monsters[i].IsDead) continue;
                if (_monsters[i].Position == pos)
                    return false;
            }

            return true;
        }

        private static int ManhattanDistance(Vector2Int a, Vector2Int b)
        {
            return Mathf.Abs(a.x - b.x) + Mathf.Abs(a.y - b.y);
        }

        private static Vector2Int GetFacingDirection(Vector2Int from, Vector2Int to)
        {
            int dx = to.x - from.x;
            int dy = to.y - from.y;

            // Normalize to unit direction (prefer larger axis)
            if (Mathf.Abs(dx) >= Mathf.Abs(dy))
                return new Vector2Int(dx > 0 ? 1 : -1, 0);
            else
                return new Vector2Int(0, dy > 0 ? 1 : -1);
        }

        private void UpdatePositionCache()
        {
            _positionCache.Clear();
            for (int i = 0; i < _monsters.Count; i++)
            {
                if (!_monsters[i].IsDead)
                    _positionCache.Add(_monsters[i].Position);
            }
        }
    }
}
