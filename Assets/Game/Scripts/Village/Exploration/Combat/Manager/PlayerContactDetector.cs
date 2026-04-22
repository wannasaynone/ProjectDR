using ProjectDR.Village.Exploration.Movement;
using KahaGameCore.GameEvent;
using UnityEngine;

namespace ProjectDR.Village.Exploration.Combat
{
    /// <summary>
    /// Detects physical collision between the player and monsters using 2D colliders.
    /// When a collision is detected, publishes <see cref="PlayerContactDamageEvent"/>.
    /// Attached to the player visual GameObject.
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    [RequireComponent(typeof(CircleCollider2D))]
    public class PlayerContactDetector : MonoBehaviour
    {
        private PlayerFreeMovement _playerMovement;
        private float _contactCooldown;
        private float _lastContactTime;

        private const float ContactCooldownSeconds = 0.5f;

        public void Initialize(PlayerFreeMovement playerMovement)
        {
            _playerMovement = playerMovement;
            _lastContactTime = -ContactCooldownSeconds;

            // Set up Rigidbody2D as kinematic (movement is handled by PlayerFreeMovement)
            Rigidbody2D rb = GetComponent<Rigidbody2D>();
            rb.bodyType = RigidbodyType2D.Kinematic;

            // Set up trigger collider
            CircleCollider2D collider = GetComponent<CircleCollider2D>();
            collider.isTrigger = true;
            collider.radius = 0.25f;
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            HandleContact(other);
        }

        private void OnTriggerStay2D(Collider2D other)
        {
            HandleContact(other);
        }

        private void HandleContact(Collider2D other)
        {
            if (_playerMovement == null) return;

            // Check cooldown
            if (Time.time - _lastContactTime < ContactCooldownSeconds) return;

            // Check if the collider is a monster
            MonsterView monsterView = other.GetComponent<MonsterView>();
            if (monsterView == null) return;

            int monsterId = monsterView.MonsterId;

            // Calculate knockback direction: from monster to player
            Vector2 playerPos = _playerMovement.WorldPosition;
            Vector2 monsterPos = new Vector2(other.transform.position.x, other.transform.position.y);
            Vector2 knockbackDir = (playerPos - monsterPos).normalized;

            if (knockbackDir.sqrMagnitude < 0.001f)
                knockbackDir = Vector2.up; // fallback direction

            _lastContactTime = Time.time;

            EventBus.Publish(new PlayerContactDamageEvent
            {
                MonsterId = monsterId,
                ContactPosition = monsterPos,
                KnockbackDirection = knockbackDir,
                DamageDealt = 0 // CombatManager calculates actual damage
            });
        }
    }
}
