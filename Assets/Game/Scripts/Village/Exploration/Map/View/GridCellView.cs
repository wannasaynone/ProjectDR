using ProjectDR.Village.Exploration.Core;
using System;
using KahaGameCore.GameEvent;
using ProjectDR.Village.Exploration.Combat;
using TMPro;
using UnityEngine;

namespace ProjectDR.Village.Exploration.Map
{
    /// <summary>
    /// Handles the visual representation of a single grid cell.
    /// Listens to CellRevealedEvent and MonsterCountsChangedEvent to keep the sprite color
    /// and number overlay in sync with the GridMap logic layer.
    /// Also flashes red when a monster is preparing to attack this cell.
    /// </summary>
    public class GridCellView : MonoBehaviour
    {
        private int _gridX;
        private int _gridY;
        private GridMap _gridMap;
        private SpriteRenderer _spriteRenderer;
        private TextMeshPro _numberText;

        private Action<CellRevealedEvent> _onCellRevealed;
        private Action<MonsterCountsChangedEvent> _onMonsterCountsChanged;
        private Action<MonsterAttackPrepareEvent> _onAttackPrepare;
        private Action<MonsterAttackExecuteEvent> _onAttackExecute;
        private Action<MonsterDiedEvent> _onMonsterDied;

        private static readonly Color UnexploredColor = new Color(0.3f, 0.3f, 0.3f);
        private static readonly Color ExploredColor = new Color(0.7f, 0.9f, 0.7f);
        private static readonly Color EvacuationColor = new Color(0.9f, 0.9f, 0.5f);
        private static readonly Color DangerColor = new Color(1f, 0.2f, 0.2f);

        // Attack warning flash state
        private float _dangerTimer;
        private Color _baseColor;
        private int _attackingMonsterId;

        /// <summary>
        /// Binds this view to a specific grid coordinate and subscribes to map events.
        /// Must be called once immediately after the component is added.
        /// </summary>
        public void Initialize(int gridX, int gridY, GridMap gridMap,
            SpriteRenderer spriteRenderer, TextMeshPro numberText)
        {
            _gridX = gridX;
            _gridY = gridY;
            _gridMap = gridMap;
            _spriteRenderer = spriteRenderer;
            _numberText = numberText;

            _onCellRevealed = (e) =>
            {
                if (e.X == _gridX && e.Y == _gridY)
                    UpdateVisual();
            };
            _onMonsterCountsChanged = (e) => UpdateVisual();
            _onAttackPrepare = (e) =>
            {
                if (e.AttackTargetPosition.x == _gridX && e.AttackTargetPosition.y == _gridY)
                {
                    _attackingMonsterId = e.MonsterId;
                    StartDangerFlash(e.PrepareSeconds);
                }
            };
            _onAttackExecute = (e) =>
            {
                Vector2Int target = e.Position + e.FacingDirection;
                if (target.x == _gridX && target.y == _gridY)
                    StopDangerFlash();
            };
            _onMonsterDied = (e) =>
            {
                if (e.MonsterId == _attackingMonsterId && _dangerTimer > 0f)
                    StopDangerFlash();
            };

            EventBus.Subscribe<CellRevealedEvent>(_onCellRevealed);
            EventBus.Subscribe<MonsterCountsChangedEvent>(_onMonsterCountsChanged);
            EventBus.Subscribe<MonsterAttackPrepareEvent>(_onAttackPrepare);
            EventBus.Subscribe<MonsterAttackExecuteEvent>(_onAttackExecute);
            EventBus.Subscribe<MonsterDiedEvent>(_onMonsterDied);
        }

        private void Update()
        {
            if (_dangerTimer <= 0f) return;

            _dangerTimer -= Time.deltaTime;
            if (_dangerTimer <= 0f)
            {
                _spriteRenderer.color = _baseColor;
                return;
            }

            // Flash between base color and danger red
            float t = Mathf.PingPong(Time.time * 8f, 1f);
            _spriteRenderer.color = Color.Lerp(_baseColor, DangerColor, t);
        }

        /// <summary>
        /// Refreshes sprite color and number overlay to match the current GridMap state.
        /// </summary>
        public void UpdateVisual()
        {
            bool explored = _gridMap.IsExplored(_gridX, _gridY);

            if (!explored)
            {
                _baseColor = UnexploredColor;
                if (_dangerTimer <= 0f)
                    _spriteRenderer.color = UnexploredColor;
                _numberText.gameObject.SetActive(false);
                return;
            }

            bool isEvac = _gridMap.IsEvacuationPoint(_gridX, _gridY);
            _baseColor = isEvac ? EvacuationColor : ExploredColor;

            if (_dangerTimer <= 0f)
                _spriteRenderer.color = _baseColor;

            int count = _gridMap.GetAdjacentMonsterCount(_gridX, _gridY);
            bool hasAdjacentUnexplored = _gridMap.HasAdjacentUnexploredCell(_gridX, _gridY);
            if (count > 0 && hasAdjacentUnexplored)
            {
                _numberText.gameObject.SetActive(true);
                _numberText.text = count.ToString();
                _numberText.color = GetNumberColor(count);
            }
            else
            {
                _numberText.gameObject.SetActive(false);
            }
        }

        private void StartDangerFlash(float duration)
        {
            _baseColor = _spriteRenderer.color;
            _dangerTimer = duration;
        }

        private void StopDangerFlash()
        {
            _dangerTimer = 0f;
            _spriteRenderer.color = _baseColor;
        }

        private void OnDestroy()
        {
            if (_onCellRevealed != null)
                EventBus.Unsubscribe<CellRevealedEvent>(_onCellRevealed);
            if (_onMonsterCountsChanged != null)
                EventBus.Unsubscribe<MonsterCountsChangedEvent>(_onMonsterCountsChanged);
            if (_onAttackPrepare != null)
                EventBus.Unsubscribe<MonsterAttackPrepareEvent>(_onAttackPrepare);
            if (_onAttackExecute != null)
                EventBus.Unsubscribe<MonsterAttackExecuteEvent>(_onAttackExecute);
            if (_onMonsterDied != null)
                EventBus.Unsubscribe<MonsterDiedEvent>(_onMonsterDied);
        }

        // Minesweeper-style color coding: 1=blue, 2=green, 3=red, 4+=dark red.
        private static Color GetNumberColor(int count)
        {
            switch (count)
            {
                case 1: return Color.blue;
                case 2: return new Color(0f, 0.5f, 0f);
                case 3: return Color.red;
                default: return new Color(0.5f, 0f, 0f);
            }
        }
    }
}
