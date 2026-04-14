using System;
using KahaGameCore.GameEvent;
using TMPro;
using UnityEngine;

namespace ProjectDR.Village.Exploration.Combat
{
    /// <summary>
    /// Spawns floating damage numbers when monsters or the player take damage.
    /// Numbers float upward and fade out over a short duration.
    /// </summary>
    public class DamageNumberView : MonoBehaviour
    {
        private ExplorationMapView _mapView;

        private Action<MonsterDamagedEvent> _onMonsterDamaged;
        private Action<PlayerHpChangedEvent> _onPlayerHpChanged;

        public void Initialize(ExplorationMapView mapView)
        {
            _mapView = mapView;

            _onMonsterDamaged = (e) =>
            {
                Vector3 worldPos = _mapView.GridToWorldPosition(e.Position.x, e.Position.y);
                SpawnNumber(worldPos, e.Damage, Color.white);
            };

            _onPlayerHpChanged = (e) =>
            {
                if (e.DamageDealt > 0)
                {
                    // Damage to player - show red number at player position
                    // We don't have player position directly, but damage numbers
                    // will be spawned near the DamageNumberView's position
                    SpawnNumber(transform.position, e.DamageDealt, Color.red);
                }
            };

            EventBus.Subscribe<MonsterDamagedEvent>(_onMonsterDamaged);
            EventBus.Subscribe<PlayerHpChangedEvent>(_onPlayerHpChanged);
        }

        private void OnDestroy()
        {
            if (_onMonsterDamaged != null) EventBus.Unsubscribe<MonsterDamagedEvent>(_onMonsterDamaged);
            if (_onPlayerHpChanged != null) EventBus.Unsubscribe<PlayerHpChangedEvent>(_onPlayerHpChanged);
        }

        private void SpawnNumber(Vector3 worldPosition, int value, Color color)
        {
            GameObject numObj = new GameObject("DmgNum");
            numObj.transform.position = worldPosition + new Vector3(0f, 0.3f, 0f);

            TextMeshPro tmp = numObj.AddComponent<TextMeshPro>();
            tmp.text = value.ToString();
            tmp.fontSize = 4f;
            tmp.color = color;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.sortingOrder = 25;

            RectTransform rt = tmp.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(2f, 1f);

            FloatingNumber floater = numObj.AddComponent<FloatingNumber>();
            floater.Initialize(0.8f, 1.0f);
        }
    }

    /// <summary>
    /// Animates a damage number floating upward and fading out.
    /// Self-destructs when the animation is complete.
    /// </summary>
    public class FloatingNumber : MonoBehaviour
    {
        private float _duration;
        private float _floatDistance;
        private float _elapsed;
        private Vector3 _startPosition;
        private TextMeshPro _text;

        public void Initialize(float duration, float floatDistance)
        {
            _duration = duration;
            _floatDistance = floatDistance;
            _elapsed = 0f;
            _startPosition = transform.position;
            _text = GetComponent<TextMeshPro>();
        }

        private void Update()
        {
            _elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(_elapsed / _duration);

            // Float upward
            transform.position = _startPosition + new Vector3(0f, t * _floatDistance, 0f);

            // Fade out
            if (_text != null)
            {
                Color c = _text.color;
                c.a = 1f - t;
                _text.color = c;
            }

            if (t >= 1f)
            {
                Destroy(gameObject);
            }
        }
    }
}
