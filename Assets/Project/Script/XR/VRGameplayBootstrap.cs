using Unity.XR.CoreUtils;
using UnityEngine;
using UnityEngine.InputSystem;

public class VRGameplayBootstrap : MonoBehaviour
{
    public static VRGameplayBootstrap Instance { get; private set; }

    [SerializeField] private float moveSpeed = 2f;
    [SerializeField] private float jumpSpeed = 5.5f;
    [SerializeField] private float roundDuration = 120f;

    private CharacterController characterController;
    private InputAction moveAction;
    private InputAction jumpAction;
    private Transform cameraTransform;
    private Transform xrOriginTransform;
    private float verticalVelocity;
    private float floorHeight;

    private void Start()
    {
        Instance = this;

        XROrigin xrOrigin = GetComponentInParent<XROrigin>();
        if (xrOrigin == null)
        {
            Debug.LogError("VRGameplayBootstrap requires an XR Origin parent.");
            return;
        }

        xrOriginTransform = xrOrigin.transform;
        CreateEnvironment(xrOriginTransform);
        VRScoreManager.ResetRound(roundDuration);
        floorHeight = xrOriginTransform.position.y;
        cameraTransform = xrOrigin.Camera != null ? xrOrigin.Camera.transform : transform;
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
        SpawnTargets(xrOrigin.transform);
        VRScoreDisplay.Create(cameraTransform);
        VRMenuController.Create(leftHand, rightHand, cameraTransform, this);

        leftHand.localScale = Vector3.one;
        rightHand.localScale = Vector3.one;
    }

    private void Update()
    {
        if (characterController == null || moveAction == null || cameraTransform == null)
        {
            return;
        }

        Vector2 input = moveAction.ReadValue<Vector2>();
        Vector3 forward = Vector3.ProjectOnPlane(cameraTransform.forward, Vector3.up).normalized;
        Vector3 right = Vector3.ProjectOnPlane(cameraTransform.right, Vector3.up).normalized;
        Vector3 movement = (forward * input.y + right * input.x) * moveSpeed;

        if (jumpAction != null && jumpAction.WasPressedThisFrame() && characterController.isGrounded)
        {
            verticalVelocity = jumpSpeed;
        }

        if (characterController.isGrounded && verticalVelocity <= 0f)
        {
            verticalVelocity = -2f;
        }
        else
        {
            verticalVelocity += Physics.gravity.y * Time.deltaTime;
        }

        movement.y = verticalVelocity;
        characterController.Move(movement * Time.deltaTime);
        VRScoreManager.Tick(Time.deltaTime);

        if (xrOriginTransform.position.y < floorHeight - 1f)
        {
            Vector3 recoveredPosition = xrOriginTransform.position;
            recoveredPosition.y = floorHeight;
            xrOriginTransform.position = recoveredPosition;
            verticalVelocity = 0f;
        }
    }

    private Transform CreateTrackedHand(Transform parent, string handName, string deviceBinding)
    {
        GameObject handObject = new GameObject(handName);
        handObject.transform.SetParent(parent, false);
        VRTrackedHand trackedHand = handObject.AddComponent<VRTrackedHand>();
        trackedHand.Initialize(deviceBinding);
        return handObject.transform;
    }

    private void AttachGunToHand(Transform rightHand)
    {
        Handgun handgun = FindFirstObjectByType<Handgun>();
        if (handgun == null)
        {
            Debug.LogWarning("VRGameplayBootstrap could not find a Handgun in the scene.");
            return;
        }

        Transform gunTransform = handgun.transform;
        gunTransform.SetParent(rightHand, false);
        gunTransform.localPosition = new Vector3(0f, -0.08f, 0.12f);
        gunTransform.localRotation = Quaternion.Euler(90f, 0f, 0f);
        handgun.SetBulletSpawnLocalPosition(new Vector3(0f, 0f, 0.16f));
        handgun.SetHeld(true);
    }

    private void SpawnTargets(Transform origin)
    {
        float[] targetSizes = { 0.7f, 0.58f, 0.46f, 0.34f, 0.24f, 0.7f, 0.46f };
        int[] maximumScores = { 50, 75, 100, 150, 250, 50, 100 };
        Color[] targetColors =
        {
            new Color(0.95f, 0.12f, 0.08f),
            new Color(1f, 0.55f, 0.05f),
            new Color(1f, 0.9f, 0.05f),
            new Color(0.15f, 0.85f, 0.95f),
            new Color(0.95f, 0.2f, 0.9f),
            new Color(0.95f, 0.12f, 0.08f),
            new Color(1f, 0.9f, 0.05f)
        };

        for (int index = 0; index < targetSizes.Length; index++)
        {
            GameObject targetObject = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            targetObject.name = $"Moving Target {index + 1}";
            targetObject.transform.position = GetRandomTargetPosition(origin);
            targetObject.transform.localScale = Vector3.one * targetSizes[index];

            MovingTarget target = targetObject.AddComponent<MovingTarget>();
            target.Initialize(GetRandomTargetDirection(origin), index * 0.7f);
            target.ConfigureTier(maximumScores[index], Mathf.Max(10, maximumScores[index] / 5), targetColors[index]);
        }
    }

    public void RespawnTarget(MovingTarget target)
    {
        target.ResetTarget(GetRandomTargetPosition(xrOriginTransform), GetRandomTargetDirection(xrOriginTransform), Random.Range(0f, 3f));
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
        Vector3 position;
        do
        {
            float forwardOffset = Random.Range(-8f, 8f);
            float rightOffset = Random.Range(-8f, 8f);
            float height = Random.Range(1.1f, 3.8f);
            position = origin.position + origin.forward * forwardOffset + origin.right * rightOffset + Vector3.up * height;
        }
        while (Vector3.ProjectOnPlane(position - origin.position, Vector3.up).sqrMagnitude < 6.25f);

        return position;
    }

    private Vector3 GetRandomTargetDirection(Transform origin)
    {
        Vector3 direction = origin.forward * Random.Range(-1f, 1f) + origin.right * Random.Range(-1f, 1f);
        return direction.sqrMagnitude > 0.01f ? direction.normalized : origin.forward;
    }

    private void CreateEnvironment(Transform origin)
    {
        Vector3 center = origin.position;
        Vector3 forward = origin.forward;
        Vector3 right = origin.right;

        Material floorMaterial = CreateEnvironmentMaterial("Arena Floor", new Color(0.035f, 0.055f, 0.08f), 0.65f, 0.2f);
        Material wallMaterial = CreateEnvironmentMaterial("Arena Wall", new Color(0.06f, 0.09f, 0.14f), 0.45f, 0.1f);
        Material accentMaterial = CreateEnvironmentMaterial("Arena Accent", new Color(0.02f, 0.65f, 0.85f), 0.35f, 0.5f);
        Material propMaterial = CreateEnvironmentMaterial("Arena Props", new Color(0.18f, 0.22f, 0.28f), 0.75f, 0.25f);
        Material warmAccentMaterial = CreateEnvironmentMaterial("Warm Accent", new Color(0.8f, 0.22f, 0.04f), 0.25f, 0.4f);

        CreateEnvironmentPrimitive(PrimitiveType.Cube, "Main Floor", center + Vector3.down * 0.08f, new Vector3(24f, 0.16f, 24f), floorMaterial);
        CreateEnvironmentPrimitive(PrimitiveType.Cube, "Back Wall", center + forward * 10f + Vector3.up * 2.5f, new Vector3(0.25f, 5f, 20f), wallMaterial);
        CreateEnvironmentPrimitive(PrimitiveType.Cube, "Left Wall", center - right * 10f + Vector3.up * 2.5f, new Vector3(20f, 5f, 0.25f), wallMaterial);
        CreateEnvironmentPrimitive(PrimitiveType.Cube, "Right Wall", center + right * 10f + Vector3.up * 2.5f, new Vector3(20f, 5f, 0.25f), wallMaterial);

        for (int index = -2; index <= 2; index++)
        {
            Vector3 columnPosition = center + forward * 9.8f + right * (index * 4.5f) + Vector3.up * 2.5f;
            CreateEnvironmentPrimitive(PrimitiveType.Cube, $"Backstop Accent {index}", columnPosition, new Vector3(0.08f, 4.3f, 0.08f), accentMaterial);
        }

        CreateEnvironmentPrimitive(PrimitiveType.Cube, "Left Accent Rail", center - right * 9.8f + Vector3.up * 0.04f, new Vector3(20f, 0.08f, 0.08f), accentMaterial);
        CreateEnvironmentPrimitive(PrimitiveType.Cube, "Right Accent Rail", center + right * 9.8f + Vector3.up * 0.04f, new Vector3(20f, 0.08f, 0.08f), accentMaterial);

        CreateEnvironmentPrimitive(PrimitiveType.Cylinder, "Left Arena Pillar", center + forward * 2f - right * 8f + Vector3.up * 2f, new Vector3(0.7f, 2f, 0.7f), propMaterial);
        CreateEnvironmentPrimitive(PrimitiveType.Cylinder, "Right Arena Pillar", center + forward * 2f + right * 8f + Vector3.up * 2f, new Vector3(0.7f, 2f, 0.7f), propMaterial);
        CreateEnvironmentPrimitive(PrimitiveType.Cube, "Left Cover Crate", center + forward * 3f - right * 4f + Vector3.up * 0.6f, new Vector3(1.2f, 1.2f, 1.2f), propMaterial);
        CreateEnvironmentPrimitive(PrimitiveType.Cube, "Right Cover Crate", center + forward * 5f + right * 4f + Vector3.up * 0.6f, new Vector3(1.2f, 1.2f, 1.2f), propMaterial);
        CreateEnvironmentPrimitive(PrimitiveType.Cube, "Target Plinth", center + forward * 8.5f + Vector3.up * 0.35f, new Vector3(10f, 0.7f, 1.2f), warmAccentMaterial);

        CreateParkourCourse(center, forward, right, propMaterial, accentMaterial, warmAccentMaterial);

        CreateArenaLight("Left Arena Light", center - right * 8f + Vector3.up * 4f, new Color(0.05f, 0.55f, 1f));
        CreateArenaLight("Right Arena Light", center + right * 8f + Vector3.up * 4f, new Color(1f, 0.2f, 0.04f));
    }

    private void CreateParkourCourse(Vector3 center, Vector3 forward, Vector3 right, Material propMaterial, Material accentMaterial, Material warmAccentMaterial)
    {
        for (int index = 0; index < 9; index++)
        {
            float forwardOffset = -7f + index * 1.8f;
            float sideOffset = index % 2 == 0 ? -6.2f : -4.4f;
            float height = 0.25f + index * 0.16f;
            Material material = index % 3 == 0 ? accentMaterial : propMaterial;
            Vector3 position = center + forward * forwardOffset + right * sideOffset + Vector3.up * height;
            CreateEnvironmentPrimitive(PrimitiveType.Cube, $"Parkour Step {index + 1}", position, new Vector3(1.35f, 0.5f, 1.25f), material);
        }

        for (int index = 0; index < 6; index++)
        {
            float forwardOffset = -4.5f + index * 2.4f;
            float sideOffset = index % 2 == 0 ? 3.8f : 5.8f;
            float height = 0.3f + (index % 3) * 0.28f;
            Vector3 position = center + forward * forwardOffset + right * sideOffset + Vector3.up * height;
            CreateEnvironmentPrimitive(PrimitiveType.Cylinder, $"Parkour Pad {index + 1}", position, new Vector3(0.9f, 0.28f, 0.9f), warmAccentMaterial);
        }

        CreateEnvironmentPrimitive(PrimitiveType.Cube, "Parkour High Landing", center + forward * 5.5f - right * 7f + Vector3.up * 1.05f, new Vector3(2.2f, 0.5f, 1.8f), accentMaterial);
        CreateEnvironmentPrimitive(PrimitiveType.Cube, "Parkour Bridge", center + forward * 7f + right * 5.8f + Vector3.up * 0.85f, new Vector3(2.8f, 0.45f, 1.4f), propMaterial);
        CreateEnvironmentPrimitive(PrimitiveType.Cube, "Parkour Finish", center + forward * 8f - right * 4.8f + Vector3.up * 0.55f, new Vector3(2.6f, 0.6f, 2f), warmAccentMaterial);

        for (int index = 0; index < 7; index++)
        {
            float forwardOffset = -6f + index * 2.1f;
            float sideOffset = index % 2 == 0 ? 7f : 6.2f;
            float height = 0.4f + (index % 3) * 0.25f;
            Vector3 position = center + forward * forwardOffset + right * sideOffset + Vector3.up * height;
            CreateEnvironmentPrimitive(PrimitiveType.Cube, $"Parkour Beam {index + 1}", position, new Vector3(1.8f, 0.35f, 0.55f), accentMaterial);
        }

        for (int index = 0; index < 6; index++)
        {
            float forwardOffset = -3f + index * 2.2f;
            float sideOffset = index % 2 == 0 ? -1.8f : 1.8f;
            float height = 0.55f + (index % 2) * 0.35f;
            Vector3 position = center + forward * forwardOffset + right * sideOffset + Vector3.up * height;
            CreateEnvironmentPrimitive(PrimitiveType.Cube, $"Parkour Center Block {index + 1}", position, new Vector3(1.25f, 0.5f, 1.25f), propMaterial);
        }
    }

    private GameObject CreateEnvironmentPrimitive(PrimitiveType primitiveType, string objectName, Vector3 position, Vector3 scale, Material material)
    {
        GameObject environmentObject = GameObject.CreatePrimitive(primitiveType);
        environmentObject.name = objectName;
        environmentObject.transform.position = position;
        environmentObject.transform.localScale = scale;
        environmentObject.GetComponent<Renderer>().material = material;
        return environmentObject;
    }

    private Material CreateEnvironmentMaterial(string materialName, Color color, float metallic, float smoothness)
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null)
        {
            shader = Shader.Find("Standard");
        }

        Material material = new Material(shader)
        {
            name = materialName,
            color = color
        };

        if (material.HasProperty("_Metallic"))
        {
            material.SetFloat("_Metallic", metallic);
        }
        if (material.HasProperty("_Smoothness"))
        {
            material.SetFloat("_Smoothness", smoothness);
        }

        return material;
    }

    private void CreateArenaLight(string lightName, Vector3 position, Color color)
    {
        GameObject lightObject = new GameObject(lightName);
        lightObject.transform.position = position;
        Light light = lightObject.AddComponent<Light>();
        light.type = LightType.Point;
        light.color = color;
        light.intensity = 7f;
        light.range = 8f;
        light.shadows = LightShadows.Soft;
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

        transform.localPosition = positionAction.ReadValue<Vector3>();
        transform.localRotation = rotationAction.ReadValue<Quaternion>();
    }

    private void OnDestroy()
    {
        positionAction?.Dispose();
        rotationAction?.Dispose();
    }
}
