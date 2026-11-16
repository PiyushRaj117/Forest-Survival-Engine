using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.InputSystem;
using ForestSurvival.Combat;
using ForestSurvival.UI;

[RequireComponent(typeof(CharacterController))]
public class CustomPlayerController : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] private float moveSpeed = 1.9f;      // Realistic natural walking speed
    [SerializeField] private float sprintSpeed = 3.8f;    // Controlled sprint
    [SerializeField] private float rotationSpeed = 6.0f;  // Smooth cinematic turning on A & D
    [SerializeField] private float gravity = -20.0f;
    [SerializeField] private float jumpHeight = 1.5f;
    [SerializeField] private float jumpStaminaCost = 5f;
    [SerializeField] private float attackStaminaCost = 6f;

    [Header("Weapon Attachment Offsets")]
    [SerializeField] private Vector3 swordLocalPosition = new Vector3(0.02f, 0.04f, 0.02f);
    [SerializeField] private Vector3 swordLocalRotation = new Vector3(90f, 90f, 0f);
    [SerializeField] private Vector3 swordLocalScale = Vector3.one;

    [Header("References")]
    [SerializeField] private Animator characterAnimator;
    [SerializeField] private Transform cameraTransform;
    [SerializeField] private WeaponHitbox equippedWeapon;

    [Header("Audio")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private UnityEngine.AudioClip[] footstepSounds;
    [SerializeField] private UnityEngine.AudioClip jumpSound;
    [SerializeField] private UnityEngine.AudioClip attackSwingSound;

    private CharacterController characterController;
    private PlayerVitals playerVitals;
    private Vector3 velocity;
    private bool isGrounded;
    private bool isDead;
    private bool isAttacking;
    private float attackTimer;
    private float stepCycleTimer;

    // Animator Hashes
    private int speedHash;
    private int isGroundedHash;
    private int jumpHash;
    private int attackHash;
    private int dieHash;
    private int respawnHash;

    private void Awake()
    {
        characterController = GetComponent<CharacterController>();
        playerVitals = GetComponent<PlayerVitals>();

        if (characterAnimator == null)
            characterAnimator = GetComponentInChildren<Animator>();

        if (cameraTransform == null && Camera.main != null)
            cameraTransform = Camera.main.transform;

        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
            audioSource = gameObject.AddComponent<AudioSource>();

        // Exact parameter names matching Ellen_AnimatorController
        speedHash = Animator.StringToHash("Speed");
        isGroundedHash = Animator.StringToHash("IsGrounded");
        jumpHash = Animator.StringToHash("Jump");
        attackHash = Animator.StringToHash("Attack");
        dieHash = Animator.StringToHash("Die");
        respawnHash = Animator.StringToHash("Respawn");

        // Ensure Neon Pause & Settings Menu exists in gameplay
        if (FindFirstObjectByType<NeonPauseMenuUI>() == null)
        {
            GameObject menuObj = new GameObject("NeonPauseMenuManager", typeof(NeonPauseMenuUI));
            DontDestroyOnLoad(menuObj);
        }
    }

    private void Start()
    {
        EnsureWeaponEquipped();
    }

    /// <summary>
    /// Ensures weapon is visibly attached to Ellen's right hand bone (LocatorRightHand or Humanoid RightHand).
    /// </summary>
    public void EnsureWeaponEquipped()
    {
        // 1. Try to find right hand transform
        Transform handBone = null;
        if (characterAnimator != null && characterAnimator.isHuman)
        {
            handBone = characterAnimator.GetBoneTransform(HumanBodyBones.RightHand);
        }

        if (handBone == null)
        {
            handBone = FindChildRecursive(transform, "LocatorRightHand");
        }
        if (handBone == null)
        {
            handBone = FindChildRecursive(transform, "RightHand");
        }
        if (handBone == null)
        {
            handBone = FindChildRecursive(transform, "ControllersRightHand");
        }

        if (handBone == null)
        {
            Debug.LogWarning("[CustomPlayerController] Could not find right hand bone for weapon attachment.");
            return;
        }

        // 2. Check if a weapon is already attached under handBone
        WeaponHitbox existingHitbox = handBone.GetComponentInChildren<WeaponHitbox>(true);
        if (existingHitbox != null)
        {
            equippedWeapon = existingHitbox;
            AlignWeaponTransform(equippedWeapon.transform);
            return;
        }

        // 3. Try to load Sword prefab or Staff model
        GameObject weaponPrefab = Resources.Load<GameObject>("Weapons/Sword1_Silver");
        if (weaponPrefab == null)
        {
#if UNITY_EDITOR
            weaponPrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/Blink/Art/Weapons/LowPoly/FreeSwords/Prefabs/Sword1_Silver.prefab");
            if (weaponPrefab == null)
            {
                weaponPrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(
                    "Assets/3D Game Kit - Character Pack/Characters/Ellen/Weapons/Models/Staff.fbx");
            }
#endif
        }

        if (weaponPrefab != null)
        {
            GameObject weaponInst = Instantiate(weaponPrefab, handBone);
            weaponInst.name = "Equipped_Sword";
            AlignWeaponTransform(weaponInst.transform);

            BoxCollider col = weaponInst.GetComponent<BoxCollider>();
            if (col == null) col = weaponInst.AddComponent<BoxCollider>();
            col.isTrigger = true;
            col.size = new Vector3(0.25f, 1.4f, 0.25f);
            col.center = new Vector3(0f, 0.7f, 0f);

            WeaponHitbox hitbox = weaponInst.GetComponent<WeaponHitbox>();
            if (hitbox == null) hitbox = weaponInst.AddComponent<WeaponHitbox>();

            equippedWeapon = hitbox;
            equippedWeapon.EnableHitbox(false);
            Debug.Log("<color=cyan>[Player]</color> Successfully equipped sword to right hand!");
        }
    }

    private void AlignWeaponTransform(Transform weaponTrans)
    {
        if (weaponTrans == null) return;
        weaponTrans.localPosition = swordLocalPosition;
        weaponTrans.localRotation = Quaternion.Euler(swordLocalRotation);
        weaponTrans.localScale = swordLocalScale;
    }

    private Transform FindChildRecursive(Transform parent, string targetName)
    {
        if (parent.name.Equals(targetName, System.StringComparison.OrdinalIgnoreCase))
            return parent;

        for (int i = 0; i < parent.childCount; i++)
        {
            Transform found = FindChildRecursive(parent.GetChild(i), targetName);
            if (found != null) return found;
        }
        return null;
    }

    private void Update()
    {
        if (isDead) return;

        CheckGrounded();
        HandleAttack();
        HandleMovement();
        ApplyGravity();
    }

    private void CheckGrounded()
    {
        isGrounded = characterController.isGrounded;

        if (isGrounded && velocity.y < 0)
        {
            velocity.y = -5f; // Strong downward stick force to terrain
        }

        if (characterAnimator != null)
        {
            characterAnimator.SetBool(isGroundedHash, isGrounded);
        }
    }

    private void HandleAttack()
    {
        if (isAttacking)
        {
            attackTimer -= Time.deltaTime;
            if (attackTimer <= 0f)
            {
                isAttacking = false;
                if (equippedWeapon != null) equippedWeapon.EnableHitbox(false);
            }
            return;
        }

        // Only allow attack if not paused
        if (Time.timeScale <= 0.01f)
            return;

        bool attackPressed = (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame) || Input.GetMouseButtonDown(0);
        if (attackPressed)
        {
            if (playerVitals != null && !playerVitals.ConsumeStamina(attackStaminaCost))
            {
                return; // Not enough stamina
            }

            isAttacking = true;
            attackTimer = 0.85f;

            if (characterAnimator != null)
            {
                for (int i = 0; i < characterAnimator.layerCount; i++)
                {
                    characterAnimator.SetLayerWeight(i, 1f);
                }
                characterAnimator.ResetTrigger(attackHash);
                characterAnimator.SetTrigger(attackHash);
            }

            if (attackSwingSound != null && audioSource != null)
            {
                audioSource.PlayOneShot(attackSwingSound, 0.85f);
            }

            if (equippedWeapon != null)
            {
                equippedWeapon.EnableHitbox(true);
            }
        }
    }

    private void HandleMovement()
    {
        if (isAttacking)
        {
            // Decelerate smoothly during attack swing
            if (characterAnimator != null) characterAnimator.SetFloat(speedHash, 0f, 0.1f, Time.deltaTime);
            return;
        }

        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");
        Vector3 inputDir = new Vector3(h, 0f, v).normalized;

        bool isSprinting = false;

        // Shift key strictly required for sprint condition & stamina check
        if (Input.GetKey(KeyCode.LeftShift) && inputDir.magnitude > 0.1f)
        {
            if (playerVitals != null)
            {
                if (playerVitals.ConsumeStamina(12f * Time.deltaTime))
                {
                    isSprinting = true;
                }
            }
            else
            {
                isSprinting = true;
            }
        }

        // Strict walk vs sprint speed
        float currentSpeed = isSprinting ? sprintSpeed : moveSpeed;

        if (inputDir.magnitude >= 0.1f)
        {
            float targetAngle = Mathf.Atan2(inputDir.x, inputDir.z) * Mathf.Rad2Deg;
            if (cameraTransform != null)
            {
                targetAngle += cameraTransform.eulerAngles.y;
            }

            // Smooth controlled turning rate
            float angle = Mathf.LerpAngle(transform.eulerAngles.y, targetAngle, rotationSpeed * Time.deltaTime);
            transform.rotation = Quaternion.Euler(0f, angle, 0f);

            Vector3 moveDirection = Quaternion.Euler(0f, targetAngle, 0f) * Vector3.forward;
            characterController.Move(moveDirection.normalized * (currentSpeed * Time.deltaTime));

            // Footsteps sound cycle timer: timed accurately to walk vs sprint
            stepCycleTimer += Time.deltaTime * (isSprinting ? 2.2f : 1.4f);
            if (stepCycleTimer >= 0.75f)
            {
                stepCycleTimer = 0f;
                if (isGrounded) PlayStep();
            }

            // Normalized Animator Blend Parameter: 0.5 = Walk, 1.0 = Sprint
            if (characterAnimator != null)
            {
                float targetAnimValue = isSprinting ? 1.0f : 0.5f;
                characterAnimator.SetFloat(speedHash, targetAnimValue, 0.15f, Time.deltaTime);
            }
        }
        else
        {
            stepCycleTimer = 0.5f;
            if (characterAnimator != null)
            {
                characterAnimator.SetFloat(speedHash, 0f, 0.15f, Time.deltaTime);
            }
        }

        // Jump Logic (Triggered on Spacebar)
        if (Input.GetButtonDown("Jump") && isGrounded)
        {
            if (playerVitals == null || playerVitals.ConsumeStamina(jumpStaminaCost))
            {
                velocity.y = Mathf.Sqrt(jumpHeight * -2.0f * gravity);
                isGrounded = false;
                if (characterAnimator != null)
                {
                    characterAnimator.SetBool(isGroundedHash, false);
                    characterAnimator.SetTrigger(jumpHash);
                }
                if (jumpSound != null && audioSource != null)
                {
                    audioSource.PlayOneShot(jumpSound, 0.75f);
                }
            }
        }
    }

    private void ApplyGravity()
    {
        velocity.y += gravity * Time.deltaTime;
        characterController.Move(velocity * Time.deltaTime);
    }

    public void OnDeath()
    {
        if (isDead) return;
        isDead = true;
        velocity = Vector3.zero;

        if (equippedWeapon != null) equippedWeapon.EnableHitbox(false);

        if (characterAnimator != null)
        {
            characterAnimator.SetTrigger(dieHash);
        }
    }

    public void OnRespawn(Vector3 respawnPosition)
    {
        isDead = false;
        velocity = Vector3.zero;
        isAttacking = false;

        if (characterController != null)
        {
            characterController.enabled = false;
            transform.position = respawnPosition;
            characterController.enabled = true;
        }
        else
        {
            transform.position = respawnPosition;
        }

        if (characterAnimator != null)
        {
            characterAnimator.SetTrigger(respawnHash);
            characterAnimator.SetFloat(speedHash, 0f);
            characterAnimator.SetBool(isGroundedHash, true);
        }
    }

    public void SetWeapon(WeaponHitbox weapon)
    {
        equippedWeapon = weapon;
        AlignWeaponTransform(equippedWeapon.transform);
    }

    // Animation Event Receivers for Ellen's attack animations
    public void MeleeAttackStart()
    {
        if (equippedWeapon != null)
        {
            equippedWeapon.EnableHitbox(true);
        }
    }

    public void MeleeAttackEnd()
    {
        isAttacking = false;
        if (equippedWeapon != null)
        {
            equippedWeapon.EnableHitbox(false);
        }
    }

    public void PlayStep()
    {
        if (footstepSounds != null && footstepSounds.Length > 0 && audioSource != null)
        {
            UnityEngine.AudioClip clip = footstepSounds[Random.Range(0, footstepSounds.Length)];
            if (clip != null)
            {
                audioSource.PlayOneShot(clip, 0.45f);
            }
        }
    }
}
