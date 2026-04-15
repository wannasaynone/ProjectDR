using System;
using KahaGameCore.GameEvent;
using TMPro;
using UnityEngine;

namespace ProjectDR.Village.Exploration.Combat
{
    /// <summary>
    /// View for player combat visuals:
    /// - HP bar (simple world-space bar above player token)
    /// - Sword attack direction indicator
    /// - Sword sweep flash on attack
    /// </summary>
    public class PlayerCombatView : MonoBehaviour
    {
        private PlayerCombatStats _playerStats;
        private SwordAttack _swordAttack;
        private Transform _playerViewTransform;
        private Camera _mainCamera;

        // HP bar
        private GameObject _hpBarBg;
        private GameObject _hpBarFill;
        private TextMeshPro _hpText;

        // Sword sweep indicator
        private GameObject _sweepIndicator;
        private SpriteRenderer _sweepRenderer;
        private float _sweepFlashTimer;
        private const float SweepFlashDuration = 0.15f;

        // Event handlers
        private Action<PlayerHpChangedEvent> _onHpChanged;
        private Action<PlayerAttackEvent> _onAttack;

        public void Initialize(
            PlayerCombatStats playerStats,
            SwordAttack swordAttack,
            Transform playerViewTransform)
        {
            _playerStats = playerStats;
            _swordAttack = swordAttack;
            _playerViewTransform = playerViewTransform;
            _mainCamera = Camera.main;

            CreateHpBar();
            CreateSweepIndicator();

            _onHpChanged = (e) => UpdateHpBar();
            _onAttack = HandleAttack;

            EventBus.Subscribe<PlayerHpChangedEvent>(_onHpChanged);
            EventBus.Subscribe<PlayerAttackEvent>(_onAttack);

            UpdateHpBar();
        }

        private void Update()
        {
            if (_playerViewTransform != null)
            {
                // Follow player position
                transform.position = _playerViewTransform.position;
            }

            // Sweep flash timer
            if (_sweepFlashTimer > 0f)
            {
                _sweepFlashTimer -= Time.deltaTime;
                if (_sweepFlashTimer <= 0f)
                {
                    _sweepIndicator.SetActive(false);
                }
            }

            // Update HP bar position
            if (_hpBarBg != null)
            {
                _hpBarBg.transform.position = transform.position + new Vector3(0f, 0.6f, 0f);
            }
        }

        private void OnDestroy()
        {
            if (_onHpChanged != null) EventBus.Unsubscribe<PlayerHpChangedEvent>(_onHpChanged);
            if (_onAttack != null) EventBus.Unsubscribe<PlayerAttackEvent>(_onAttack);
        }

        private void CreateHpBar()
        {
            // Background
            _hpBarBg = new GameObject("PlayerHpBarBg");
            _hpBarBg.transform.SetParent(transform);
            SpriteRenderer bgSr = _hpBarBg.AddComponent<SpriteRenderer>();
            bgSr.sprite = CreateWhiteSprite();
            bgSr.color = Color.black;
            bgSr.sortingOrder = 15;
            _hpBarBg.transform.localScale = new Vector3(0.8f, 0.1f, 1f);
            _hpBarBg.transform.localPosition = new Vector3(0f, 0.6f, 0f);

            // Fill
            _hpBarFill = new GameObject("PlayerHpBarFill");
            _hpBarFill.transform.SetParent(_hpBarBg.transform);
            SpriteRenderer fillSr = _hpBarFill.AddComponent<SpriteRenderer>();
            fillSr.sprite = CreateWhiteSprite();
            fillSr.color = Color.green;
            fillSr.sortingOrder = 16;
            _hpBarFill.transform.localPosition = Vector3.zero;
            _hpBarFill.transform.localScale = Vector3.one;

            // HP text
            GameObject textObj = new GameObject("PlayerHpText");
            textObj.transform.SetParent(_hpBarBg.transform);
            textObj.transform.localPosition = new Vector3(0f, 0f, -0.1f);
            _hpText = textObj.AddComponent<TextMeshPro>();
            _hpText.fontSize = 2f;
            _hpText.alignment = TextAlignmentOptions.Center;
            _hpText.sortingOrder = 17;
            RectTransform rt = _hpText.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(2f, 0.5f);
        }

        private void CreateSweepIndicator()
        {
            _sweepIndicator = new GameObject("SwordSweep");
            _sweepIndicator.transform.SetParent(transform);
            _sweepRenderer = _sweepIndicator.AddComponent<SpriteRenderer>();
            _sweepRenderer.sprite = CreateWhiteSprite();
            _sweepRenderer.color = new Color(1f, 1f, 0.5f, 0.5f);
            _sweepRenderer.sortingOrder = 12;
            _sweepIndicator.transform.localScale = new Vector3(1.2f, 0.4f, 1f);
            _sweepIndicator.SetActive(false);
        }

        private void UpdateHpBar()
        {
            if (_playerStats == null) return;

            float ratio = (float)_playerStats.CurrentHp / _playerStats.MaxHp;
            _hpBarFill.transform.localScale = new Vector3(ratio, 1f, 1f);
            _hpBarFill.transform.localPosition = new Vector3((ratio - 1f) * 0.5f, 0f, 0f);

            if (ratio > 0.5f)
                _hpBarFill.GetComponent<SpriteRenderer>().color = Color.green;
            else if (ratio > 0.25f)
                _hpBarFill.GetComponent<SpriteRenderer>().color = Color.yellow;
            else
                _hpBarFill.GetComponent<SpriteRenderer>().color = Color.red;

            if (_hpText != null)
                _hpText.text = $"{_playerStats.CurrentHp}/{_playerStats.MaxHp}";
        }

        private void HandleAttack(PlayerAttackEvent e)
        {
            // Flash the sweep indicator in the attack direction
            if (_sweepIndicator == null) return;

            _sweepIndicator.SetActive(true);
            _sweepFlashTimer = SweepFlashDuration;

            // Rotate to face attack direction
            float angle = Mathf.Atan2(e.Direction.y, e.Direction.x) * Mathf.Rad2Deg;
            _sweepIndicator.transform.localRotation = Quaternion.Euler(0f, 0f, angle);
            _sweepIndicator.transform.localScale = new Vector3(e.Range, 0.6f, 1f);
            _sweepIndicator.transform.localPosition = new Vector3(
                e.Direction.x * e.Range * 0.5f,
                e.Direction.y * e.Range * 0.5f,
                0f);
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
