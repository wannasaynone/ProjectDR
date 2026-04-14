using System;
using KahaGameCore.GameEvent;
using TMPro;
using UnityEngine;

namespace ProjectDR.Village.Exploration.Combat
{
    /// <summary>
    /// View for a single monster on the exploration map.
    /// Displays as a colored square with HP bar and attack warning.
    /// Only visible when the monster's cell is explored.
    /// </summary>
    public class MonsterView : MonoBehaviour
    {
        private MonsterState _monsterState;
        private GridMap _gridMap;
        private ExplorationMapView _mapView;
        private SpriteRenderer _spriteRenderer;

        // HP bar
        private GameObject _hpBarBg;
        private GameObject _hpBarFill;

        // Attack warning
        private GameObject _warningObj;
        private SpriteRenderer _warningRenderer;

        // Event handlers
        private Action<MonsterDamagedEvent> _onDamaged;
        private Action<MonsterMovedEvent> _onMoved;
        private Action<MonsterDiedEvent> _onDied;
        private Action<MonsterAttackPrepareEvent> _onAttackPrepare;
        private Action<MonsterAttackExecuteEvent> _onAttackExecute;
        private Action<CellRevealedEvent> _onCellRevealed;

        // Attack warning timer
        private float _warningTimer;

        public void Initialize(MonsterState monsterState, GridMap gridMap, ExplorationMapView mapView)
        {
            _monsterState = monsterState;
            _gridMap = gridMap;
            _mapView = mapView;

            // Create monster visual
            _spriteRenderer = gameObject.AddComponent<SpriteRenderer>();
            _spriteRenderer.sprite = CreateWhiteSprite();
            _spriteRenderer.color = monsterState.TypeData.DisplayColor;
            _spriteRenderer.sortingOrder = 5;
            transform.localScale = new Vector3(0.7f, 0.7f, 1f);

            UpdateWorldPosition();
            CreateHpBar();
            CreateWarningIndicator();
            UpdateVisibility();

            // Subscribe to events
            _onDamaged = (e) => { if (e.MonsterId == _monsterState.Id) OnDamaged(e); };
            _onMoved = (e) => { if (e.MonsterId == _monsterState.Id) OnMoved(e); };
            _onDied = (e) => { if (e.MonsterId == _monsterState.Id) OnDied(); };
            _onAttackPrepare = (e) => { if (e.MonsterId == _monsterState.Id) OnAttackPrepare(e); };
            _onAttackExecute = (e) => { if (e.MonsterId == _monsterState.Id) OnAttackExecute(); };
            _onCellRevealed = (e) => { UpdateVisibility(); };

            EventBus.Subscribe<MonsterDamagedEvent>(_onDamaged);
            EventBus.Subscribe<MonsterMovedEvent>(_onMoved);
            EventBus.Subscribe<MonsterDiedEvent>(_onDied);
            EventBus.Subscribe<MonsterAttackPrepareEvent>(_onAttackPrepare);
            EventBus.Subscribe<MonsterAttackExecuteEvent>(_onAttackExecute);
            EventBus.Subscribe<CellRevealedEvent>(_onCellRevealed);
        }

        private void Update()
        {
            if (_warningTimer > 0f)
            {
                _warningTimer -= Time.deltaTime;
                if (_warningTimer <= 0f)
                {
                    _warningObj.SetActive(false);
                }
                else
                {
                    // Blink warning
                    float alpha = Mathf.PingPong(Time.time * 6f, 1f);
                    _warningRenderer.color = new Color(1f, 0f, 0f, alpha);
                }
            }
        }

        private void OnDestroy()
        {
            if (_onDamaged != null) EventBus.Unsubscribe<MonsterDamagedEvent>(_onDamaged);
            if (_onMoved != null) EventBus.Unsubscribe<MonsterMovedEvent>(_onMoved);
            if (_onDied != null) EventBus.Unsubscribe<MonsterDiedEvent>(_onDied);
            if (_onAttackPrepare != null) EventBus.Unsubscribe<MonsterAttackPrepareEvent>(_onAttackPrepare);
            if (_onAttackExecute != null) EventBus.Unsubscribe<MonsterAttackExecuteEvent>(_onAttackExecute);
            if (_onCellRevealed != null) EventBus.Unsubscribe<CellRevealedEvent>(_onCellRevealed);
        }

        private void CreateHpBar()
        {
            _hpBarBg = new GameObject("MonsterHpBarBg");
            _hpBarBg.transform.SetParent(transform);
            SpriteRenderer bgSr = _hpBarBg.AddComponent<SpriteRenderer>();
            bgSr.sprite = CreateWhiteSprite();
            bgSr.color = Color.black;
            bgSr.sortingOrder = 6;
            _hpBarBg.transform.localPosition = new Vector3(0f, 0.6f, 0f);
            _hpBarBg.transform.localScale = new Vector3(1f, 0.12f, 1f);

            _hpBarFill = new GameObject("MonsterHpBarFill");
            _hpBarFill.transform.SetParent(_hpBarBg.transform);
            SpriteRenderer fillSr = _hpBarFill.AddComponent<SpriteRenderer>();
            fillSr.sprite = CreateWhiteSprite();
            fillSr.color = Color.red;
            fillSr.sortingOrder = 7;
            _hpBarFill.transform.localPosition = Vector3.zero;
            _hpBarFill.transform.localScale = Vector3.one;
        }

        private void CreateWarningIndicator()
        {
            _warningObj = new GameObject("AttackWarning");
            _warningObj.transform.SetParent(transform);
            _warningRenderer = _warningObj.AddComponent<SpriteRenderer>();
            _warningRenderer.sprite = CreateWhiteSprite();
            _warningRenderer.color = new Color(1f, 0f, 0f, 0.5f);
            _warningRenderer.sortingOrder = 8;
            _warningObj.transform.localPosition = new Vector3(0f, -0.3f, 0f);
            _warningObj.transform.localScale = new Vector3(0.3f, 0.3f, 1f);
            _warningObj.SetActive(false);
        }

        private void UpdateWorldPosition()
        {
            if (_mapView != null)
            {
                transform.position = _mapView.GridToWorldPosition(
                    _monsterState.Position.x, _monsterState.Position.y);
            }
        }

        private void UpdateVisibility()
        {
            bool visible = _gridMap.IsExplored(
                _monsterState.Position.x, _monsterState.Position.y);
            gameObject.SetActive(visible);
        }

        private void UpdateHpBar()
        {
            if (_monsterState == null) return;
            float ratio = (float)_monsterState.CurrentHp / _monsterState.TypeData.MaxHp;
            _hpBarFill.transform.localScale = new Vector3(ratio, 1f, 1f);
            _hpBarFill.transform.localPosition = new Vector3((ratio - 1f) * 0.5f, 0f, 0f);
        }

        private void OnDamaged(MonsterDamagedEvent e)
        {
            UpdateHpBar();
        }

        private void OnMoved(MonsterMovedEvent e)
        {
            UpdateWorldPosition();
            UpdateVisibility();
        }

        private void OnDied()
        {
            Destroy(gameObject);
        }

        private void OnAttackPrepare(MonsterAttackPrepareEvent e)
        {
            _warningObj.SetActive(true);
            _warningTimer = e.PrepareSeconds;
        }

        private void OnAttackExecute()
        {
            _warningObj.SetActive(false);
            _warningTimer = 0f;
        }

        private static Sprite CreateWhiteSprite()
        {
            Texture2D tex = new Texture2D(4, 4);
            Color[] pixels = new Color[16];
            for (int i = 0; i < 16; i++) pixels[i] = Color.white;
            tex.SetPixels(pixels);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, 4, 4), new Vector2(0.5f, 0.5f), 4f);
        }
    }
}
