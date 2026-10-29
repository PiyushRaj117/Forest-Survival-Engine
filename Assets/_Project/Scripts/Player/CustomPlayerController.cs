using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class CustomPlayerController : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] private float moveSpeed = 3.5f;
    [SerializeField] private float sprintSpeed = 6.0f;
    [SerializeField] private float rotationSpeed = 12.0f;
    [SerializeField] private float gravity = -15.0f;

    [Header("Ground Check")]
    [SerializeField] private LayerMask groundMask;
    [SerializeField] private float groundCheckRadius = 0.2f;

    [Header("References")]
    [SerializeField] private Animator characterAnimator;
    [SerializeField] private Transform cameraTransform;

    private CharacterController characterController;
    private Vector3 velocity;
    private bool isGrounded;
    private int speedHash;

    private void Awake()
    {
        characterController = GetComponent<CharacterController>();

        if (characterAnimator == null)
        {
            characterAnimator = GetComponentInChildren<Animator>();
        }

        if (cameraTransform == null && Camera.main != null)
        {
            cameraTransform = Camera.main.transform;
        }

        speedHash = Animator.StringToHash("Speed");
    }

    private void Update()
    {
        CheckGroundStatus();
        HandleMovement();
        ApplyGravity();
    }

    private void CheckGroundStatus()
    {
        Vector3 spherePosition = new Vector3(transform.position.x, transform.position.y - 0.05f, transform.position.z);
        isGrounded = Physics.CheckSphere(spherePosition, groundCheckRadius, groundMask, QueryTriggerInteraction.Ignore);

        if (isGrounded && velocity.y < 0)
        {
            velocity.y = -2f;
        }
    }

    private void HandleMovement()
    {
        float horizontal = Input.GetAxisRaw("Horizontal");
        float vertical = Input.GetAxisRaw("Vertical");

        // Input Vector
        Vector3 inputVector = new Vector3(horizontal, 0f, vertical).normalized;

        bool isSprinting = Input.GetKey(KeyCode.LeftShift);
        float currentSpeed = isSprinting ? sprintSpeed : moveSpeed;

        if (inputVector.magnitude >= 0.1f)
        {
            // Calculate absolute target movement angle relative to Camera
            float cameraYAngle = cameraTransform != null ? cameraTransform.eulerAngles.y : 0f;
            float targetAngle = Mathf.Atan2(inputVector.x, inputVector.z) * Mathf.Rad2Deg + cameraYAngle;

            // Smooth rotation towards movement direction
            Quaternion targetRotation = Quaternion.Euler(0f, targetAngle, 0f);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * rotationSpeed);

            // True forward movement direction
            Vector3 moveDirection = Quaternion.Euler(0f, targetAngle, 0f) * Vector3.forward;
            characterController.Move(moveDirection * (currentSpeed * Time.deltaTime));

            // Set Animator Blend Speed
            float animSpeed = isSprinting ? 1.0f : 0.5f;
            if (characterAnimator != null)
            {
                characterAnimator.SetFloat(speedHash, animSpeed, 0.1f, Time.deltaTime);
            }
        }
        else
        {
            if (characterAnimator != null)
            {
                characterAnimator.SetFloat(speedHash, 0f, 0.1f, Time.deltaTime);
            }
        }
    }

    private void ApplyGravity()
    {
        velocity.y += gravity * Time.deltaTime;
        characterController.Move(velocity * Time.deltaTime);
    }
}