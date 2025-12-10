using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//This script requires you to have setup your animator with 3 parameters, "InputMagnitude", "InputX", "InputZ"
//With a blend tree to control the inputmagnitude and allow blending between animations.
[RequireComponent(typeof(CharacterController))]
public class MovementInput : MonoBehaviour {

    public float Velocity;
    [Space]

	public float InputX;
	public float InputZ;
	public Vector3 desiredMoveDirection;
	public bool blockRotationPlayer;
	public float desiredRotationSpeed = 0.1f;
	public Animator anim;
	public float Speed;
	public float allowPlayerRotation = 0.1f;
	public Camera cam;
	public CharacterController controller;
	public bool isGrounded;

    [Header("Animation Smoothing")]
    [Range(0, 1f)]
    public float HorizontalAnimSmoothTime = 0.2f;
    [Range(0, 1f)]
    public float VerticalAnimTime = 0.2f;
    [Range(0,1f)]
    public float StartAnimTime = 0.3f;
    [Range(0, 1f)]
    public float StopAnimTime = 0.15f;

    public float verticalVel;
    private Vector3 moveVector;

    [Header("Input")]
    public bool useZQSD = true;

    [Header("Third-Person Tuning")]
    public float rotationSmoothTime = 0.12f;
    public float accelerationTime = 0.12f;
    public float decelerationTime = 0.16f;
    public float gravity = -9.81f;
    public float groundedGravity = -2f;

    private Vector3 horizontalVelocity;
    private Vector3 horizontalVelRef;
    private float currentYawVelocity;
    private Vector3 lastDesiredDir;

    // Optional: override camera reference
    [Header("Camera Reference")]
    public Transform cameraRef;

    [Header("Grenade Throwing")]
    public GameObject grenadePrefab;
    public Transform throwOrigin;      // defaults to cameraRef if null
    public float throwCooldown = 1.2f;
    public float throwForce = 14f;     // horizontal force
    public float upForce = 4.5f;       // vertical arc
    public float throwPitchDeg = 15f;  // upward pitch angle
    private float lastThrowTime;

    [Header("Mouse Camera")]
    public float mouseSensitivity = 2.0f;
    public float minPitch = -35f;
    public float maxPitch = 60f;
    public float cameraDistance = 4.0f;
    public Vector3 cameraPivotOffset = new Vector3(0f, 1.6f, 0f);

    private Transform cameraPivot;
    private float yaw;   // around Y
    private float pitch; // around local X

    [Header("Audio")]
    public AudioSource audioSource;      // for throw
    public AudioClip throwClip;          
    public AudioClip footstepClip;       
    public float footstepVolume = 0.8f;
    private AudioSource footstepSource;  // dedicated looping source
    [Range(0f, 1.5f)] public float masterVolume = 1f;

	// Use this for initialization
	void Start () {
		anim = this.GetComponent<Animator> ();
		cam = Camera.main;
		controller = this.GetComponent<CharacterController> ();
        // Prefer the player's own camera
        if (cameraRef == null)
        {
            var childCam = GetComponentInChildren<Camera>();
            if (childCam != null) cameraRef = childCam.transform;
        }
        // Fallback to main camera if none found
        if (cameraRef == null && Camera.main != null) cameraRef = Camera.main.transform;

        if (throwOrigin == null) throwOrigin = cameraRef; // aim from player camera

        // Setup camera pivot
        if (cameraRef != null)
        {
            cameraPivot = new GameObject("CameraPivot").transform;
            cameraPivot.SetParent(transform);
            cameraPivot.localPosition = cameraPivotOffset;
            // Keep camera under pivot
            cameraRef.SetParent(cameraPivot);
            // Initialize yaw/pitch from current camera
            Vector3 euler = cameraPivot.eulerAngles;
            yaw = euler.y;
            pitch = 15f; // slight upward pitch start
        }

        if (audioSource == null) {
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null) audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
            audioSource.spatialBlend = 0f; // 2D by default; set to 1f for 3D if desired
        }
        audioSource.volume = 1f; // max; scaled by masterVolume when playing

        // Setup footstep source
        if (footstepSource == null) {
            footstepSource = gameObject.AddComponent<AudioSource>();
            footstepSource.playOnAwake = false;
            footstepSource.loop = true;
            footstepSource.spatialBlend = 0f;
            footstepSource.clip = footstepClip;
        }
        footstepSource.volume = Mathf.Clamp01(footstepVolume * masterVolume);
	}
	
	// Update is called once per frame
	void Update () {
        // Compute input, desired direction and rotation first
        ReadInput();
        ComputeDesiredDirectionAndRotate();

        // Ground check and gravity
        isGrounded = controller.isGrounded;
        if (isGrounded && verticalVel < 0f)
            verticalVel = groundedGravity;
        else
            verticalVel += gravity * Time.deltaTime;

        // Smooth horizontal velocity towards target; scale by input magnitude
        float inputMag = new Vector2(InputX, InputZ).magnitude;
        Vector3 targetHoriz = desiredMoveDirection.normalized * (Velocity * inputMag);

        // Reset smoothing when direction changes sharply to avoid "sticking"
        if (Vector3.Dot(lastDesiredDir, desiredMoveDirection) < 0.0f)
            horizontalVelRef = Vector3.zero;

        float currentSpeed = new Vector2(horizontalVelocity.x, horizontalVelocity.z).magnitude;
        float targetSpeed = targetHoriz.magnitude;
        float smoothTime = targetSpeed > currentSpeed ? accelerationTime : decelerationTime;
        horizontalVelocity = Vector3.SmoothDamp(horizontalVelocity, targetHoriz, ref horizontalVelRef, smoothTime);

        // Single Move call
        Vector3 motion = new Vector3(horizontalVelocity.x, verticalVel, horizontalVelocity.z) * Time.deltaTime;
        controller.Move(motion);

        lastDesiredDir = desiredMoveDirection;

        // Footstep audio: loop while moving and grounded, stop otherwise
        bool shouldFootstep = isGrounded && Speed > allowPlayerRotation && footstepClip != null;
        if (shouldFootstep) {
            footstepSource.volume = Mathf.Clamp01(footstepVolume * masterVolume);
            if (!footstepSource.isPlaying) {
                footstepSource.clip = footstepClip; // ensure assigned
                footstepSource.Play();
            }
        } else {
            if (footstepSource.isPlaying) footstepSource.Stop();
        }

        // Input to throw grenade (Left Mouse or G)
        bool wantsThrow = Input.GetMouseButtonDown(0) || Input.GetKeyDown(KeyCode.G);
        if (wantsThrow && grenadePrefab != null && Time.time - lastThrowTime >= throwCooldown)
        {
            ThrowGrenade();

            // Play throw sound
            if (throwClip != null && audioSource != null) {
                audioSource.PlayOneShot(throwClip, Mathf.Clamp01(masterVolume));
            }
        }
    }

    void LateUpdate()
    {
        // Mouse-look camera orbit
        if (cameraPivot != null && cameraRef != null)
        {
            float mx = Input.GetAxis("Mouse X");
            float my = Input.GetAxis("Mouse Y");

            yaw += mx * mouseSensitivity;
            pitch -= my * mouseSensitivity;
            pitch = Mathf.Clamp(pitch, minPitch, maxPitch);

            // Apply rotation to pivot
            cameraPivot.rotation = Quaternion.Euler(pitch, yaw, 0f);

            // Place camera at distance behind pivot looking at pivot
            Vector3 desiredPos = cameraPivot.position - cameraPivot.forward * cameraDistance;
            cameraRef.position = desiredPos;
            cameraRef.rotation = Quaternion.LookRotation(cameraPivot.position - cameraRef.position, Vector3.up);
        }
    }

    // Centralized input read (ZQSD or fallback to axes)
    void ReadInput()
    {
        if (useZQSD)
        {
            float f = 0f, r = 0f;
            if (Input.GetKey(KeyCode.Z)) f += 1f;
            if (Input.GetKey(KeyCode.S)) f -= 1f;
            if (Input.GetKey(KeyCode.D)) r += 1f;
            if (Input.GetKey(KeyCode.Q)) r -= 1f;
            InputZ = Mathf.Clamp(f, -1f, 1f);
            InputX = Mathf.Clamp(r, -1f, 1f);
        }
        else
        {
            InputX = Input.GetAxis("Horizontal");
            InputZ = Input.GetAxis("Vertical");
        }
    }

    // Build camera-relative desiredMoveDirection and rotate immediately on input
    void ComputeDesiredDirectionAndRotate()
    {
        // Use player camera (cameraRef) for planar forward/right
        Transform camT = cameraRef != null ? cameraRef : (cam != null ? cam.transform : null);
        Vector3 planarForward = Vector3.forward;
        Vector3 planarRight = Vector3.right;

        if (camT != null)
        {
            Vector3 camForward = camT.forward;
            camForward.y = 0f;
            if (camForward.sqrMagnitude < 0.0001f)
                camForward = transform.forward; // fallback
            planarForward = camForward.normalized;

            Vector3 camRight = camT.right;
            camRight.y = 0f;
            if (camRight.sqrMagnitude < 0.0001f)
                camRight = Vector3.Cross(Vector3.up, planarForward);
            planarRight = camRight.normalized;
        }
        else
        {
            // Hard fallback to player forward/right
            planarForward = transform.forward;
            planarForward.y = 0f; planarForward.Normalize();
            planarRight = transform.right;
            planarRight.y = 0f; planarRight.Normalize();
        }

        // Camera-relative desired direction
        desiredMoveDirection = (planarForward * InputZ + planarRight * InputX);
        // Do not normalize away input magnitude; keep for speed scaling
        if (desiredMoveDirection.sqrMagnitude > 1f) desiredMoveDirection.Normalize();

        // Immediate rotation when there is any input
        if (!blockRotationPlayer && desiredMoveDirection.sqrMagnitude > 0.0001f) {
            float targetAngle = Mathf.Atan2(desiredMoveDirection.x, desiredMoveDirection.z) * Mathf.Rad2Deg;
            float angle = Mathf.SmoothDampAngle(transform.eulerAngles.y, targetAngle, ref currentYawVelocity, rotationSmoothTime);
            transform.rotation = Quaternion.Euler(0f, angle, 0f);
        }

        // Drive animation blend with input magnitude
        Speed = new Vector2(InputX, InputZ).magnitude;
        anim.SetFloat("Blend", Speed, Speed > allowPlayerRotation ? StartAnimTime : StopAnimTime, Time.deltaTime);
    }

    // Remove movement and rotation from PlayerMoveAndRotation/InputMagnitude; keep for compatibility
    void PlayerMoveAndRotation() {
        // Movement handled in Update via horizontalVelocity
    }

    void InputMagnitude() {
        // Now handled in Update/ComputeDesiredDirectionAndRotate
    }

    public void LookAt(Vector3 pos)
    {
        transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(pos), desiredRotationSpeed);
    }

    public void RotateToCamera(Transform t)
    {

        var camera = Camera.main;
        var forward = cam.transform.forward;
        var right = cam.transform.right;

        desiredMoveDirection = forward;

        t.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(desiredMoveDirection), desiredRotationSpeed);
    }

    void ThrowGrenade()
    {
        Transform originT = throwOrigin != null ? throwOrigin : transform;
        Vector3 originPos = originT.position;

        // Aim using player camera forward and add an upward pitch
        Vector3 baseDir = (cameraRef != null ? cameraRef.forward : originT.forward);
        baseDir.Normalize();
        // Clamp vertical to avoid extreme angles
        baseDir.y = Mathf.Clamp(baseDir.y, -0.5f, 0.75f);

        // Compute pitched direction around camera's right axis
        Vector3 camRight = (cameraRef != null ? cameraRef.right : originT.right);
        Vector3 aimDir = Quaternion.AngleAxis(throwPitchDeg, camRight) * baseDir;
        aimDir.Normalize();

        // Offset forward/up to avoid self-collision
        float spawnForwardOffset = 0.35f;
        float spawnUpOffset = 0.2f;
        originPos += aimDir * spawnForwardOffset + Vector3.up * spawnUpOffset;

        // Launch velocity using pitched direction plus slight vertical arc
        Vector3 launchVel = aimDir * throwForce + Vector3.up * upForce;

        // Spawn grenade
        GameObject grenade = Instantiate(grenadePrefab, originPos, Quaternion.identity);

        // Ignore collisions with the player
        var grenadeCol = grenade.GetComponent<Collider>();
        if (grenadeCol != null)
        {
            foreach (var col in GetComponentsInChildren<Collider>())
            {
                if (col.enabled) Physics.IgnoreCollision(grenadeCol, col, true);
            }
        }

        var rb = grenade.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.useGravity = true;
            rb.drag = 0f;
            rb.angularDrag = 0.05f;
            rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
            rb.interpolation = RigidbodyInterpolation.Interpolate;

            rb.velocity = launchVel;
            rb.angularVelocity = Vector3.zero;
        }

        lastThrowTime = Time.time;
    }
}
