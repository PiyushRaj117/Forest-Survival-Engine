using UnityEngine;

namespace ForestSurvival.Combat
{
    /// <summary>
    /// Attach to player's equipped weapon (Staff/Sword) to detect hits on enemies during attack swing.
    /// </summary>
    public class WeaponHitbox : MonoBehaviour
    {
        [SerializeField] private float damage = 35f;
        [SerializeField] private Collider weaponCollider;
        [SerializeField] private AudioSource audioSource;
        [SerializeField] private AudioClip hitSound;

        private bool isActive = false;

        private void Awake()
        {
            if (weaponCollider == null) weaponCollider = GetComponent<Collider>();
            if (weaponCollider != null)
            {
                weaponCollider.isTrigger = true;
                weaponCollider.enabled = false;
            }
            if (audioSource == null) audioSource = GetComponent<AudioSource>();
        }

        public void EnableHitbox(bool enable)
        {
            isActive = enable;
            if (weaponCollider != null)
            {
                weaponCollider.enabled = enable;
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!isActive) return;

            // Check if hit enemy
            EnemyAI enemy = other.GetComponent<EnemyAI>();
            if (enemy == null)
            {
                enemy = other.GetComponentInParent<EnemyAI>();
            }

            if (enemy != null && !enemy.IsDead)
            {
                enemy.TakeDamage(damage);
                if (audioSource != null && hitSound != null)
                {
                    audioSource.PlayOneShot(hitSound, 0.9f);
                }
            }
        }
    }
}
