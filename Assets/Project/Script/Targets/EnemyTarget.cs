using UnityEngine;
using UnityEngine.AI;
using UnityEngine.UI;

public class EnemyTarget : MonoBehaviour
{
    [Header("Health")]
    [SerializeField] private float maxHealth = 100f;
    [SerializeField] private float damagePerShot = 10f;

    [Header("AI")]
    [SerializeField] private float sightRange = 30f;
    [SerializeField] private float attackRange = 2.2f;
    [SerializeField] private float attackCooldown = 1.25f;
    [SerializeField] private float projectileSpeed = 18f;
    [SerializeField] private float projectileLifetime = 3f;
    [SerializeField] private float wanderRadius = 6f;
    [SerializeField] private float wanderInterval = 3f;
    [SerializeField] private float moveSpeed = 1.4f;
    [SerializeField] private int playerLayer = 0;

    private float currentHealth;
    private float nextAttackTime;
    private float nextWanderTime;
    private Vector3 wanderDestination;
    private Transform player;
    private PlayerHealth playerHealth;
    private NavMeshAgent agent;
    private Canvas healthCanvas;
    private Image healthFill;
    private Text healthLabel;
    private bool dead;
    private float groundY;
    private Vector3 patrolCenter;
    private float patrolAngle;
    private float nextPlayerSearchTime;

    public float CurrentHealth => currentHealth;
    public float MaxHealth => maxHealth;

    private void Awake()
    {
        currentHealth = maxHealth;
        agent = GetComponent<NavMeshAgent>();
        if (agent != null)
        {
            agent.enabled = false;
            agent = null;
        }

        Rigidbody body = GetComponent<Rigidbody>();
        if (body != null)
        {
            body.isKinematic = true;
            body.useGravity = false;
        }

        NameAndPrepareBodyParts();
        GroundOnFloor();
        patrolCenter = transform.position;
        patrolAngle = Random.Range(0f, Mathf.PI * 2f);
        FindPlayer();
        CreateHealthBar();
    }

    private void Update()
    {
        if (dead)
        {
            return;
        }

        if ((player == null || playerHealth == null) && Time.time >= nextPlayerSearchTime)
        {
            FindPlayer();
            nextPlayerSearchTime = Time.time + 0.5f;
        }

        if (player == null)
        {
            Wander();
            return;
        }

        float distance = Vector3.Distance(
            new Vector3(transform.position.x, 0f, transform.position.z),
            new Vector3(player.position.x, 0f, player.position.z));
        if (distance <= sightRange)
        {
            ChasePlayer();
            ShootAtPlayer();
        }
        else
        {
            Wander();
        }
    }

    public void Configure(PlayerHealth target)
    {
        playerHealth = target;
        player = target != null ? target.transform : null;
        if (player == null)
        {
            Camera playerCamera = FindPlayerCamera();
            if (playerCamera != null)
            {
                player = playerCamera.transform;
                playerHealth = playerCamera.GetComponent<PlayerHealth>();
                if (playerHealth == null)
                {
                    playerHealth = playerCamera.gameObject.AddComponent<PlayerHealth>();
                }
            }
        }

        Debug.Log($"EnemyTarget configured. Target: {(player != null ? player.name : "none")}");
        patrolCenter = transform.position;
        if (groundY == 0f)
        {
            groundY = transform.position.y;
        }
        if (agent != null && !agent.isOnNavMesh)
        {
            agent.enabled = false;
            agent = null;
        }
    }

    public void TakeDamage(float damage, Collider hitCollider)
    {
        if (dead)
        {
            return;
        }

        float multiplier = IsCriticalBodyPart(hitCollider) ? 2f : 1f;
        int displayedDamage = Mathf.RoundToInt(damage * multiplier);
        currentHealth = Mathf.Max(0f, currentHealth - displayedDamage);
        UpdateHealthBar();
        DamagePopup.Create(transform.position + Vector3.up * 1.9f, displayedDamage, multiplier > 1f);

        if (currentHealth <= 0f)
        {
            dead = true;
            if (agent != null)
            {
                agent.isStopped = true;
            }
            Destroy(gameObject, 0.35f);
        }
    }

    private bool IsCriticalBodyPart(Collider hitCollider)
    {
        if (hitCollider == null)
        {
            return false;
        }

        string partName = hitCollider.transform.name.ToLowerInvariant();
        return partName == "main body" || partName == "eye r" || partName == "eye l";
    }

    private void FindPlayer()
    {
        playerHealth = PlayerHealth.Instance;
        if (playerHealth != null)
        {
            player = playerHealth.transform;
            return;
        }

        Camera mainCamera = FindPlayerCamera();
        if (mainCamera != null)
        {
            playerHealth = mainCamera.GetComponent<PlayerHealth>();
            if (playerHealth == null)
            {
                playerHealth = mainCamera.gameObject.AddComponent<PlayerHealth>();
            }

            player = mainCamera.transform;
        }
    }

    private Camera FindPlayerCamera()
    {
        Camera mainCamera = Camera.main;
        return mainCamera != null ? mainCamera : FindFirstObjectByType<Camera>();
    }

    private void ChasePlayer()
    {
        if (CanUseNavMeshAgent())
        {
            agent.isStopped = false;
            agent.SetDestination(player.position);
            return;
        }

        MoveTowards(player.position);
    }

    private void ShootAtPlayer()
    {
        if (player == null || Time.time < nextAttackTime)
        {
            return;
        }

        if (playerHealth == null)
        {
            Camera targetCamera = FindPlayerCamera();
            if (targetCamera != null)
            {
                playerHealth = targetCamera.GetComponent<PlayerHealth>();
                if (playerHealth == null)
                {
                    playerHealth = targetCamera.gameObject.AddComponent<PlayerHealth>();
                }
            }
        }

        Vector3 aimPoint = player.position + Vector3.up * 1.1f;
        Vector3 direction = aimPoint - (transform.position + Vector3.up * 1.1f);
        if (direction.sqrMagnitude < 0.01f)
        {
            return;
        }

        direction.Normalize();
        Vector3 muzzlePoint = transform.position + Vector3.up * 1.25f + direction * 0.55f;
        GameObject projectile = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        projectile.name = "Enemy Shot";
        projectile.transform.position = muzzlePoint;
        projectile.transform.localScale = Vector3.one * 0.08f;
        projectile.GetComponent<Renderer>().material.color = new Color(1f, 0.12f, 0.08f);

        Rigidbody projectileBody = projectile.AddComponent<Rigidbody>();
        projectileBody.useGravity = false;
        projectileBody.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        projectileBody.linearVelocity = direction * projectileSpeed;
        EnemyProjectile projectileBehaviour = projectile.AddComponent<EnemyProjectile>();
        projectileBehaviour.Initialize(playerHealth, projectileLifetime, damagePerShot);
        Debug.Log($"Enemy shot fired at {player.name} for {damagePerShot} damage.");

        Vector3 lookDirection = player.position - transform.position;
        lookDirection.y = 0f;
        if (lookDirection.sqrMagnitude > 0.01f)
        {
            transform.rotation = Quaternion.LookRotation(lookDirection);
        }

        nextAttackTime = Time.time + attackCooldown;
    }

    private void Wander()
    {
        if (Time.time >= nextWanderTime)
        {
            patrolAngle += Random.Range(0.7f, 1.5f);
            wanderDestination = patrolCenter
                + new Vector3(Mathf.Cos(patrolAngle), 0f, Mathf.Sin(patrolAngle)) * wanderRadius;
            nextWanderTime = Time.time + wanderInterval;
        }

        MoveTowards(wanderDestination);
    }

    private void MoveTowards(Vector3 destination)
    {
        Vector3 direction = destination - transform.position;
        direction.y = 0f;
        if (direction.sqrMagnitude < 0.01f)
        {
            return;
        }

        Vector3 nextPosition = transform.position + direction.normalized * moveSpeed * Time.deltaTime;
        transform.position = nextPosition;
        Quaternion targetRotation = Quaternion.LookRotation(direction.normalized, Vector3.up);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, 8f * Time.deltaTime);
    }

    private bool CanUseNavMeshAgent()
    {
        return false;
    }

    private void GroundOnFloor()
    {
        Bounds bodyBounds = new(transform.position, Vector3.zero);
        Collider[] colliders = GetComponentsInChildren<Collider>();
        foreach (Collider bodyCollider in colliders)
        {
            bodyBounds.Encapsulate(bodyCollider.bounds);
        }

        float floorY = 0f;
        bool foundFloor = false;
        RaycastHit[] hits = Physics.RaycastAll(bodyBounds.center + Vector3.up * 10f, Vector3.down, 30f);
        foreach (RaycastHit hit in hits)
        {
            if (!hit.transform.IsChildOf(transform) && hit.transform != transform)
            {
                if (!foundFloor || hit.point.y < floorY)
                {
                    floorY = hit.point.y;
                    foundFloor = true;
                }
            }
        }

        groundY = foundFloor
            ? floorY + (transform.position.y - bodyBounds.min.y)
            : transform.position.y - bodyBounds.min.y;
        Vector3 groundedPosition = transform.position;
        groundedPosition.y = groundY;
        transform.position = groundedPosition;
    }

    private void NameAndPrepareBodyParts()
    {
        string[] partNames = { "Main body", "Leg R", "Leg L", "Arm R", "Arm L", "Pants", "Shoe R", "Shoe L", "Eye R", "Eye L" };
        for (int index = 0; index < transform.childCount && index < partNames.Length; index++)
        {
            Transform part = transform.GetChild(index);
            part.name = partNames[index];
            if (part.GetComponent<Collider>() == null)
            {
                part.gameObject.AddComponent<BoxCollider>();
            }
        }
    }

    private void CreateHealthBar()
    {
        GameObject canvasObject = new("Enemy Healthbar");
        canvasObject.transform.SetParent(transform, false);
        Bounds bodyBounds = new(transform.position, Vector3.zero);
        foreach (Renderer bodyRenderer in GetComponentsInChildren<Renderer>())
        {
            bodyBounds.Encapsulate(bodyRenderer.bounds);
        }

        canvasObject.transform.localPosition = new Vector3(
            0f,
            bodyBounds.max.y - transform.position.y + 0.25f,
            0f);
        healthCanvas = canvasObject.AddComponent<Canvas>();
        healthCanvas.renderMode = RenderMode.WorldSpace;
        healthCanvas.sortingOrder = 20;
        canvasObject.AddComponent<CanvasScaler>().dynamicPixelsPerUnit = 20f;
        canvasObject.AddComponent<GraphicRaycaster>();

        RectTransform root = canvasObject.GetComponent<RectTransform>();
        root.sizeDelta = new Vector2(220f, 30f);
        root.localScale = Vector3.one * 0.01f;

        GameObject background = CreateUiImage("Background", root, new Color(0.04f, 0.04f, 0.06f, 0.95f));
        background.GetComponent<RectTransform>().sizeDelta = root.sizeDelta;
        GameObject fill = CreateUiImage("Fill", root, new Color(0.95f, 0.12f, 0.18f, 1f));
        RectTransform fillRect = fill.GetComponent<RectTransform>();
        fillRect.anchorMin = new Vector2(0f, 0f);
        fillRect.anchorMax = new Vector2(0f, 1f);
        fillRect.pivot = new Vector2(0f, 0.5f);
        fillRect.sizeDelta = new Vector2(root.sizeDelta.x, 0f);
        healthFill = fill.GetComponent<Image>();
        healthLabel = CreateUiText("Label", root, Color.white);
        healthLabel.alignment = TextAnchor.MiddleCenter;
        healthLabel.fontSize = 22;
        UpdateHealthBar();
    }

    private GameObject CreateUiImage(string objectName, Transform parent, Color color)
    {
        GameObject imageObject = new(objectName);
        imageObject.transform.SetParent(parent, false);
        Image image = imageObject.AddComponent<Image>();
        image.color = color;
        return imageObject;
    }

    private Text CreateUiText(string objectName, Transform parent, Color color)
    {
        GameObject textObject = new(objectName);
        textObject.transform.SetParent(parent, false);
        Text text = textObject.AddComponent<Text>();
        text.color = color;
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.rectTransform.anchorMin = Vector2.zero;
        text.rectTransform.anchorMax = Vector2.one;
        text.rectTransform.offsetMin = Vector2.zero;
        text.rectTransform.offsetMax = Vector2.zero;
        return text;
    }

    private void LateUpdate()
    {
        if (healthCanvas != null && Camera.main != null)
        {
            healthCanvas.transform.rotation = Camera.main.transform.rotation;
        }
    }

    private void UpdateHealthBar()
    {
        if (healthFill == null)
        {
            return;
        }

        float normalizedHealth = Mathf.Clamp01(currentHealth / maxHealth);
        healthFill.rectTransform.anchorMax = new Vector2(normalizedHealth, 1f);
        healthFill.rectTransform.sizeDelta = Vector2.zero;
        if (healthLabel != null)
        {
            healthLabel.text = $"{Mathf.CeilToInt(currentHealth)} / {Mathf.CeilToInt(maxHealth)}";
        }
    }
}
