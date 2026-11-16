using UnityEngine;

namespace ForestSurvival.Combat
{
    [RequireComponent(typeof(CharacterController))]
    public class EnemyAI : MonoBehaviour
    {
        [Header("Stats")]
        [SerializeField] private float maxHealth = 100f;
        [SerializeField] private float currentHealth;
        [SerializeField] private float attackDamage = 12f;
        [SerializeField] private float moveSpeed = 1.8f;
        [SerializeField] private float runSpeed = 3.8f;
        [SerializeField] private float attackRange = 1.8f;
        [SerializeField] private float detectRange = 10.0f;
        [SerializeField] private float loseAggroRange = 18.0f;
        [SerializeField] private float attackCooldown = 2.0f;

        [Header("Audio")]
        [SerializeField] private AudioSource audioSource;
        [SerializeField] private AudioClip roarSound;
        [SerializeField] private AudioClip attackSound;
        [SerializeField] private AudioClip hitSound;
        [SerializeField] private AudioClip stepSound;

        [Header("Patrol Settings")]
        [SerializeField] private float patrolRadius = 10f;
        [SerializeField] private float idleWaitTime = 3.5f;

        private CharacterController controller;
        private Animator animator;
        private Transform playerTarget;
        private Vector3 startPosition;
        private Vector3 currentPatrolTarget;
        private float idleTimer;
        private float nextAttackTime;
        private bool isDead = false;
        private bool isChasing = false;
        private EnemyHealthBar healthBar;

        // Cached Chomper animator parameter hashes (only what Chomper controller has)
        private int speedHash;
        private int attackHash;
        private int hitHash;
        private int dieHash;

        // Track which parameters actually exist so we never call SetBool/SetTrigger on missing ones
        private bool hasSpeedParam;
        private bool hasAttackParam;
        private bool hasHitParam;
        private bool hasDieParam;

        public bool IsDead => isDead;

        private void Awake()
        {
            controller = GetComponent<CharacterController>();
            animator = GetComponentInChildren<Animator>();

            // Add animation event bridge to the Animator child (not root)
            if (animator != null && animator.gameObject != gameObject)
            {
                if (animator.GetComponent<ChomperAnimationEvents>() == null)
                {
                    animator.gameObject.AddComponent<ChomperAnimationEvents>();
                }
            }

            if (audioSource == null) audioSource = GetComponent<AudioSource>();
            if (audioSource == null) audioSource = gameObject.AddComponent<AudioSource>();

            audioSource.spatialBlend = 1.0f;
            audioSource.minDistance = 2f;
            audioSource.maxDistance = 22f;

            currentHealth = maxHealth;
            startPosition = transform.position;
            PickNewPatrolTarget();

            // Pre-hash the parameter names
            speedHash  = Animator.StringToHash("Speed");
            attackHash = Animator.StringToHash("Attack");
            hitHash    = Animator.StringToHash("Hit");
            dieHash    = Animator.StringToHash("Die");
        }

        private void Start()
        {
            FindPlayer();

            // Validate which parameters actually exist in the Chomper controller
            if (animator != null)
            {
                hasSpeedParam  = AnimatorHasParameter(animator, speedHash);
                hasAttackParam = AnimatorHasParameter(animator, attackHash);
                hasHitParam    = AnimatorHasParameter(animator, hitHash);
                hasDieParam    = AnimatorHasParameter(animator, dieHash);
            }

            // Floating health bar
            healthBar = GetComponent<EnemyHealthBar>();
            if (healthBar == null)
                healthBar = gameObject.AddComponent<EnemyHealthBar>();
            if (healthBar != null)
                healthBar.UpdateHealth(currentHealth, maxHealth);
        }

        /// <summary>Returns true if the Animator's RuntimeAnimatorController has the given parameter hash.</summary>
        private static bool AnimatorHasParameter(Animator anim, int hash)
        {
            if (anim == null || anim.runtimeAnimatorController == null) return false;
            foreach (AnimatorControllerParameter p in anim.parameters)
            {
                if (p.nameHash == hash) return true;
            }
            return false;
        }

        // Safe wrappers — only call animator if parameter exists
        private void SetAnimFloat(int hash, float value)
        {
            if (animator != null && hasSpeedParam && hash == speedHash)
                animator.SetFloat(hash, value);
        }

        private void SetAnimTrigger(int hash)
        {
            if (animator == null) return;
            bool valid = (hash == attackHash && hasAttackParam) ||
                         (hash == hitHash    && hasHitParam)    ||
                         (hash == dieHash    && hasDieParam);
            if (valid) animator.SetTrigger(hash);
        }

        private void FindPlayer()
        {
            CustomPlayerController player = FindAnyObjectByType<CustomPlayerController>();
            if (player != null) playerTarget = player.transform;
        }

        private void Update()
        {
            if (isDead) return;

            if (playerTarget == null)
            {
                FindPlayer();
                if (playerTarget == null)
                {
                    PatrolBehavior();
                    ApplyGravity();
                    return;
                }
            }

            float distToPlayer = Vector3.Distance(transform.position, playerTarget.position);

            if (!isChasing)
            {
                if (distToPlayer <= detectRange)
                {
                    isChasing = true;
                    if (roarSound != null && audioSource != null)
                        audioSource.PlayOneShot(roarSound, 0.85f);
                }
                else
                {
                    PatrolBehavior();
                }
            }
            else
            {
                if (distToPlayer > loseAggroRange)
                {
                    isChasing = false;
                    PickNewPatrolTarget();
                    PatrolBehavior();
                }
                else if (distToPlayer <= attackRange)
                {
                    // In attack range: stop moving and attack
                    FaceTarget(playerTarget.position);
                    SetAnimFloat(speedHash, 0f);

                    if (Time.time >= nextAttackTime)
                        PerformAttack();
                }
                else if (distToPlayer <= attackRange + 3.5f)
                {
                    // Closing in slowly (player has time to react)
                    ChaseTarget(playerTarget.position, moveSpeed * 0.7f, 0.4f);
                }
                else
                {
                    // Run toward player
                    ChaseTarget(playerTarget.position, runSpeed, 1.0f);
                }
            }

            ApplyGravity();
        }

        private void ApplyGravity()
        {
            if (controller != null && controller.enabled)
                controller.Move(new Vector3(0, -9.81f * Time.deltaTime, 0));
        }

        private void ChaseTarget(Vector3 targetPos, float speed, float animSpeedParam)
        {
            FaceTarget(targetPos);
            Vector3 direction = (targetPos - transform.position);
            direction.y = 0;
            if (direction.magnitude > 0.15f)
            {
                controller.Move(direction.normalized * speed * Time.deltaTime);
                SetAnimFloat(speedHash, animSpeedParam);
            }
            else
            {
                SetAnimFloat(speedHash, 0f);
            }
        }

        private void PatrolBehavior()
        {
            float dist = Vector3.Distance(
                new Vector3(transform.position.x, 0, transform.position.z),
                new Vector3(currentPatrolTarget.x, 0, currentPatrolTarget.z));

            if (dist < 1.0f)
            {
                idleTimer += Time.deltaTime;
                SetAnimFloat(speedHash, 0f);
                if (idleTimer >= idleWaitTime)
                {
                    PickNewPatrolTarget();
                    idleTimer = 0f;
                }
            }
            else
            {
                ChaseTarget(currentPatrolTarget, moveSpeed, 0.4f);
            }
        }

        private void PickNewPatrolTarget()
        {
            Vector2 randomCircle = Random.insideUnitCircle * patrolRadius;
            currentPatrolTarget = startPosition + new Vector3(randomCircle.x, 0, randomCircle.y);
        }

        private void FaceTarget(Vector3 targetPos)
        {
            Vector3 lookDir = (targetPos - transform.position);
            lookDir.y = 0;
            if (lookDir.sqrMagnitude > 0.001f)
            {
                Quaternion targetRot = Quaternion.LookRotation(lookDir);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, Time.deltaTime * 6f);
            }
        }

        private void PerformAttack()
        {
            nextAttackTime = Time.time + attackCooldown;
            SetAnimTrigger(attackHash);

            if (attackSound != null && audioSource != null)
                audioSource.PlayOneShot(attackSound, 0.9f);

            if (playerTarget != null &&
                Vector3.Distance(transform.position, playerTarget.position) <= attackRange + 0.6f)
            {
                PlayerVitals vitals = playerTarget.GetComponent<PlayerVitals>();
                if (vitals != null) vitals.TakeDamage(attackDamage);
            }
        }

        public void TakeDamage(float damage)
        {
            if (isDead) return;

            currentHealth -= damage;

            if (hitSound != null && audioSource != null)
                audioSource.PlayOneShot(hitSound, 0.8f);

            SetAnimTrigger(hitHash);

            if (healthBar != null)
                healthBar.UpdateHealth(currentHealth, maxHealth);

            isChasing = true;

            if (currentHealth <= 0f) Die();
        }

        private void Die()
        {
            if (isDead) return;
            isDead = true;

            SetAnimTrigger(dieHash);

            if (controller != null) controller.enabled = false;
            Collider col = GetComponent<Collider>();
            if (col != null) col.enabled = false;

            Destroy(gameObject, 5f);
        }

        // --- Animation Event Receivers for Chomper FBX ---
        public void PlayStep()
        {
            if (stepSound != null && audioSource != null)
                audioSource.PlayOneShot(stepSound, 0.35f);
        }

        public void MeleeAttackStart() { }
        public void MeleeAttackEnd()   { }
        public void Hit()               { }
    }
}
