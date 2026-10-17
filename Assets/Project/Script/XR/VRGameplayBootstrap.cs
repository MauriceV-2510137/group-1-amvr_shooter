using Unity.XR.CoreUtils;
using UnityEngine;
using UnityEngine.InputSystem;

public class VRGameplayBootstrap : MonoBehaviour
{
    public static VRGameplayBootstrap Instance { get; private set; }

    [SerializeField] private float moveSpeed = 2f;
    [SerializeField] private float jumpSpeed = 5.5f;
    [SerializeField] private float roundDuration = 120f;
    [SerializeField] private float additionalTargetSpawnInterval = 2f;
    [SerializeField] private int maximumActiveTargets = 40;
    [SerializeField] private float additionalTargetMinimumRadius = 8f;
    [SerializeField] private float additionalTargetMaximumRadius = 14f;

    private CharacterController characterController;
    private InputAction moveAction;
    private InputAction jumpAction;
    private Transform cameraTransform;
    private Transform xrOriginTransform;
    private float floorHeight;
    private bool targetsSpawned;
    private bool menuCreated;
    private float nextAdditionalTargetTime;
    private int additionalTargetCount;

    private void Start()
    {
        Instance = this;

        Camera playerCamera = FindPlayerCamera();
        if (playerCamera == null)
        {
            Debug.LogError("VRGameplayBootstrap requires an enabled camera tagged MainCamera.");
            return;
        }

        cameraTransform = playerCamera.transform;
        VRScoreManager.ResetRound(roundDuration);

        XROrigin xrOrigin = GetComponentInParent<XROrigin>();
        if (xrOrigin == null)
        {
            xrOrigin = FindFirstObjectByType<XROrigin>();
        }

        if (xrOrigin != null)
        {
            xrOriginTransform = xrOrigin.transform;
            floorHeight = xrOriginTransform.position.y;
            characterController = xrOrigin.GetComponent<CharacterController>();
            if (characterController == null)
            {
                characterController = xrOrigin.gameObject.AddComponent<CharacterController>();
            }

            characterController.height = 1.7f;
            characterController.radius = 0.25f;
            characterController.center = new Vector3(0f, 0.85f, 0f);
            characterController.skinWidth = 0.03f;
            characterController.stepOffset = 0.3f;
            characterController.minMoveDistance = 0f;

            moveAction = new InputAction("VR Move", InputActionType.Value);
            moveAction.AddBinding("<XRController>{LeftHand}/primary2DAxis");
            moveAction.AddCompositeBinding("2DVector")
                .With("Up", "<Keyboard>/w")
                .With("Down", "<Keyboard>/s")
                .With("Left", "<Keyboard>/a")
                .With("Right", "<Keyboard>/d");
            moveAction.Enable();

            jumpAction = new InputAction("VR Jump", InputActionType.Button);
            jumpAction.AddBinding("<XRController>{RightHand}/primaryButton");
            jumpAction.AddBinding("<Keyboard>/space");
            jumpAction.Enable();

            Transform leftHand = CreateTrackedHand(xrOrigin.transform, "Left Hand", "<XRController>{LeftHand}");
            Transform rightHand = CreateTrackedHand(xrOrigin.transform, "Right Hand", "<XRController>{RightHand}");
            AttachGunToHand(rightHand);
            CreateMenu(leftHand, rightHand);
        }
        else
        {
            AttachGunToHand(null);
            CreateMenu(null, null);
        }

        PlayerHealth playerHealth = playerCamera.GetComponent<PlayerHealth>();
        if (playerHealth == null)
        {
            playerHealth = playerCamera.gameObject.AddComponent<PlayerHealth>();
        }

        playerHealth.CreateUi(playerCamera);
        SetupEnemy(playerHealth);
        SpawnTargets(cameraTransform);
        nextAdditionalTargetTime = Time.time + additionalTargetSpawnInterval;
        VRScoreDisplay.Create(cameraTransform);
    }

    private void SetupEnemy(PlayerHealth playerHealth)
    {
        GameObject enemyObject = GameObject.Find("Enemy");
        if (enemyObject == null)
        {
            EnemyTarget existingEnemy = FindFirstObjectByType<EnemyTarget>();
            if (existingEnemy != null)
            {
                existingEnemy.Configure(playerHealth);
            }
            return;
        }

        EnemyTarget enemy = enemyObject.GetComponent<EnemyTarget>();
        if (enemy == null)
        {
            enemy = enemyObject.AddComponent<EnemyTarget>();
        }

        enemy.Configure(playerHealth);
    }

    private void CreateMenu(Transform leftHand, Transform rightHand)
    {
        if (menuCreated || cameraTransform == null)
        {
            return;
        }

        VRMenuController.Create(leftHand, rightHand, cameraTransform, this);
        menuCreated = true;
        Debug.Log("VRGameplayBootstrap created the VR menu.");
    }

    private void Update()
    {
        if (characterController == null || moveAction == null || cameraTransform == null)
        {
            if (cameraTransform != null && !targetsSpawned)
            {
                SpawnTargets(cameraTransform);
                CreateMenu(null, null);
            }

            SpawnAdditionalTargetIfNeeded();
            return;
        }

        Vector2 input = moveAction.ReadValue<Vector2>();
        Vector3 forward = Vector3.ProjectOnPlane(cameraTransform.forward, Vector3.up).normalized;
        Vector3 right = Vector3.ProjectOnPlane(cameraTransform.right, Vector3.up).normalized;
        Vector3 movement = (forward * input.y + right * input.x) * moveSpeed;

        movement.y = 0f;
        characterController.Move(movement * Time.deltaTime);
        Vector3 stableOriginPosition = xrOriginTransform.position;
        stableOriginPosition.y = floorHeight;
        xrOriginTransform.position = stableOriginPosition;
        VRScoreManager.Tick(Time.deltaTime);
        SpawnAdditionalTargetIfNeeded();
    }

    private void SpawnAdditionalTargetIfNeeded()
    {
        if (!targetsSpawned
            || cameraTransform == null
            || Time.time < nextAdditionalTargetTime
            || FindObjectsByType<MovingTarget>(FindObjectsSortMode.None).Length >= maximumActiveTargets)
        {
            return;
        }

        nextAdditionalTargetTime = Time.time + additionalTargetSpawnInterval;
        additionalTargetCount++;

        float progress = Mathf.Clamp01((additionalTargetCount - 1f) / Mathf.Max(1f, maximumActiveTargets - 7f));
        float minimumRadius = Mathf.Lerp(additionalTargetMinimumRadius, 18f, progress);
        float maximumRadius = Mathf.Lerp(additionalTargetMaximumRadius, 26f, progress);
        float[] targetScores = { 50f, 75f, 100f, 150f, 250f };
        int scoreIndex = additionalTargetCount % targetScores.Length;
        int maximumScore = Mathf.RoundToInt(targetScores[scoreIndex]);

        GameObject targetObject = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        targetObject.name = $"Moving Target Extra {additionalTargetCount}";
        targetObject.transform.position = GetRandomTargetPosition(cameraTransform, minimumRadius, maximumRadius);
        targetObject.transform.localScale = Vector3.one * Random.Range(0.24f, 0.7f);

        MovingTarget target = targetObject.AddComponent<MovingTarget>();
        target.Initialize(GetRandomTargetDirection(cameraTransform), Random.Range(0f, 3f));
        target.ConfigureTier(maximumScore, Mathf.Max(10, maximumScore / 5), Color.HSVToRGB(Random.value, 0.85f, 1f));
        Debug.Log($"Spawned additional target {targetObject.name} at radius {minimumRadius:0.0}-{maximumRadius:0.0}.");
    }

    private Transform CreateTrackedHand(Transform parent, string handName, string deviceBinding)
    {
        GameObject handObject = new(handName);
        handObject.transform.SetParent(parent, false);
        VRTrackedHand trackedHand = handObject.AddComponent<VRTrackedHand>();
        trackedHand.Initialize(deviceBinding);
        return handObject.transform;
    }

    private void AttachGunToHand(Transform rightHand)
    {
        Handgun handgun = FindFirstObjectByType<Handgun>();
        if (handgun == null || rightHand == null)
        {
            if (handgun == null)
            {
                Debug.LogWarning("VRGameplayBootstrap could not find a Handgun in the scene.");
            }
            return;
        }

        Transform gunTransform = handgun.transform;
        gunTransform.SetParent(rightHand, false);
        gunTransform.SetLocalPositionAndRotation(new Vector3(0f, -0.08f, 0.12f), Quaternion.Euler(90f, 0f, 0f));
        handgun.SetBulletSpawnLocalPosition(new Vector3(0f, 0f, 0.16f));
        handgun.SetHeld(true);
    }

    private void SpawnTargets(Transform origin)
    {
        if (targetsSpawned || origin == null)
        {
            if (origin == null)
            {
                Debug.LogError("VRGameplayBootstrap cannot spawn targets because the player camera is unavailable.");
            }

            return;
        }

        float[] targetSizes = { 0.7f, 0.58f, 0.46f, 0.34f, 0.24f, 0.7f, 0.46f };
        int[] maximumScores = { 50, 75, 100, 150, 250, 50, 100 };
        Color[] targetColors =
        {
            new(0.95f, 0.12f, 0.08f),
            new(1f, 0.55f, 0.05f),
            new(1f, 0.9f, 0.05f),
            new(0.15f, 0.85f, 0.95f),
            new(0.95f, 0.2f, 0.9f),
            new(0.95f, 0.12f, 0.08f),
            new(1f, 0.9f, 0.05f)
        };

        int createdTargets = 0;
        for (int index = 0; index < targetSizes.Length; index++)
        {
            GameObject targetObject = GameObject.Find($"Moving Target {index + 1}");
            MovingTarget target = targetObject != null ? targetObject.GetComponent<MovingTarget>() : null;
            if (target != null)
            {
                if (!target.gameObject.activeSelf)
                {
                    target.gameObject.SetActive(true);
                }

                target.ResetTarget(
                    GetRandomTargetPosition(origin),
                    GetRandomTargetDirection(origin),
                    index * 0.7f);
                target.ConfigureTier(
                    maximumScores[index],
                    Mathf.Max(10, maximumScores[index] / 5),
                    targetColors[index]);
                createdTargets++;
                continue;
            }

            if (targetObject == null)
            {
                targetObject = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                targetObject.name = $"Moving Target {index + 1}";
                targetObject.transform.position = GetRandomTargetPosition(origin);
                targetObject.transform.localScale = Vector3.one * targetSizes[index];
            }

            target = targetObject.GetComponent<MovingTarget>();
            if (target == null)
            {
                target = targetObject.AddComponent<MovingTarget>();
            }

            target.Initialize(GetRandomTargetDirection(origin), index * 0.7f);
            target.ConfigureTier(maximumScores[index], Mathf.Max(10, maximumScores[index] / 5), targetColors[index]);
            createdTargets++;
        }

        targetsSpawned = createdTargets == targetSizes.Length;
        Debug.Log($"VRGameplayBootstrap restored {createdTargets}/{targetSizes.Length} floating score targets.");
    }

    private static Camera FindPlayerCamera()
    {
        Camera mainCamera = Camera.main;
        return mainCamera != null ? mainCamera : FindFirstObjectByType<Camera>();
    }

    public void RespawnTarget(MovingTarget target)
    {
        if (target == null)
        {
            return;
        }

        Camera playerCamera = FindPlayerCamera();
        if (playerCamera != null)
        {
            cameraTransform = playerCamera.transform;
        }

        Transform spawnOrigin = cameraTransform != null ? cameraTransform : xrOriginTransform;
        if (spawnOrigin == null)
        {
            Debug.LogError("VRGameplayBootstrap cannot respawn a target because no player camera or XR Origin is available.");
            return;
        }

        target.ResetTarget(GetRandomTargetPosition(spawnOrigin), GetRandomTargetDirection(spawnOrigin), Random.Range(0f, 3f));
    }

    public void ResetTargets()
    {
        MovingTarget[] targets = FindObjectsByType<MovingTarget>(FindObjectsSortMode.None);
        foreach (MovingTarget target in targets)
        {
            RespawnTarget(target);
        }
    }

    public void ResetRound()
    {
        VRScoreManager.ResetRound(roundDuration);
        ResetTargets();
    }

    private Vector3 GetRandomTargetPosition(Transform origin)
    {
        return GetRandomTargetPosition(origin, 3.5f, 7.5f);
    }

    private Vector3 GetRandomTargetPosition(Transform origin, float minimumRadius, float maximumRadius)
    {
        float angle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
        float radius = Random.Range(minimumRadius, maximumRadius);
        Vector3 forward = Vector3.ProjectOnPlane(origin.forward, Vector3.up).normalized;
        if (forward.sqrMagnitude < 0.01f)
        {
            forward = Vector3.forward;
        }

        Vector3 right = Vector3.Cross(Vector3.up, forward).normalized;
        Vector3 horizontalOffset = (forward * Mathf.Cos(angle) + right * Mathf.Sin(angle)) * radius;
        float height = Random.Range(-1.1f, 1.1f);
        return origin.position + horizontalOffset + Vector3.up * height;
    }

    private Vector3 GetRandomTargetDirection(Transform origin)
    {
        Vector3 direction = Vector3.ProjectOnPlane(origin.forward, Vector3.up) * Random.Range(-1f, 1f)
            + Vector3.ProjectOnPlane(origin.right, Vector3.up) * Random.Range(-1f, 1f);
        return direction.sqrMagnitude > 0.01f ? direction.normalized : Vector3.forward;
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }

        if (moveAction == null)
        {
            jumpAction?.Dispose();
            return;
        }

        moveAction.Disable();
        moveAction.Dispose();
        jumpAction?.Disable();
        jumpAction?.Dispose();
    }
}

public class VRTrackedHand : MonoBehaviour
{
    private InputAction positionAction;
    private InputAction rotationAction;

    public void Initialize(string deviceBinding)
    {
        positionAction = new InputAction("Hand Position", InputActionType.Value, deviceBinding + "/devicePosition");
        rotationAction = new InputAction("Hand Rotation", InputActionType.Value, deviceBinding + "/deviceRotation");
        positionAction.Enable();
        rotationAction.Enable();
    }

    private void Update()
    {
        if (positionAction == null || rotationAction == null)
        {
            return;
        }

        transform.SetLocalPositionAndRotation(positionAction.ReadValue<Vector3>(), rotationAction.ReadValue<Quaternion>());
    }

    private void OnDestroy()
    {
        positionAction?.Dispose();
        rotationAction?.Dispose();
    }
}
