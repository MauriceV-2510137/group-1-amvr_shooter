using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Collider))]
[RequireComponent(typeof(Renderer))]
public class MovingTarget : MonoBehaviour
{
    [SerializeField] private int maximumScore = 100;
    [SerializeField] private int minimumScore = 10;
    [SerializeField] private float largestTargetSize = 0.7f;
    [SerializeField] private float smallestTargetSize = 0.24f;
    [SerializeField] private int scoreForLargestTarget = 50;
    [SerializeField] private int scoreForSmallestTarget = 250;
    [SerializeField] private float moveAmplitude = 1.25f;
    [SerializeField] private float moveSpeed = 1.5f;
    [SerializeField] private float lifetime = 8f;

    private Vector3 startPosition;
    private Vector3 moveDirection;
    private float moveOffset;
    private Vector3 targetScale;
    private Renderer targetRenderer;
    private Collider targetCollider;
    private bool isHit;
    private bool isRespawning;
    private Coroutine lifetimeCoroutine;

    private void Awake()
    {
        targetScale = transform.localScale;
        if (!TryGetComponent(out targetRenderer))
        {
            Debug.LogWarning("MovingTarget::didnt find Renderer component!");
        }
        if(!TryGetComponent(out targetCollider))
        {
            Debug.LogWarning("MovingTarget::didnt find Collider component!");
        }
    }

    public void Initialize(Vector3 direction, float offset)
    {
        startPosition = transform.position;
        moveDirection = direction.normalized;
        moveOffset = offset;
        RestartLifetime();
    }

    public void ConfigureAppearance(Color targetColor)
    {
        float sizeProgress = Mathf.InverseLerp(
            largestTargetSize,
            smallestTargetSize,
            transform.localScale.x);
        maximumScore = Mathf.RoundToInt(Mathf.Lerp(
            scoreForLargestTarget,
            scoreForSmallestTarget,
            sizeProgress));
        minimumScore = Mathf.Max(10, maximumScore / 5);

        if (targetRenderer != null)
        {
            targetRenderer.material.color = targetColor;
        }
    }

    private void Update()
    {
        if (isHit)
        {
            return;
        }

        float movement = Mathf.Sin((Time.time + moveOffset) * moveSpeed) * moveAmplitude;
        transform.position = startPosition + moveDirection * movement;
    }

    public void RegisterHit(Vector3 hitPosition)
    {
        if (isHit)
        {
            return;
        }

        float targetRadius = Mathf.Max(transform.localScale.x * 0.5f, 0.01f);
        float accuracy = Mathf.Clamp01(Vector3.Distance(transform.position, hitPosition) / targetRadius);
        int score = Mathf.RoundToInt(Mathf.Lerp(maximumScore, minimumScore, accuracy));
        VRScoreManager.AddScore(score);
        DamagePopup.Create(
            transform.position + Vector3.up * Mathf.Max(transform.localScale.y * 0.75f, 0.35f),
            score,
            false,
            new Color(1f, 0.85f, 0.1f));
        Debug.Log($"Target hit. Aim score: {score}. Total score: {VRScoreManager.Score}");

        isHit = true;
        targetRenderer.enabled = false;
        targetCollider.enabled = false;
        CreateExplosion();
        StartCoroutine(RespawnAfterHit());
    }

    public void ResetTarget(Vector3 position, Vector3 direction, float offset)
    {
        if (lifetimeCoroutine != null)
        {
            StopCoroutine(lifetimeCoroutine);
        }

        transform.position = position;
        transform.localScale = targetScale;
        startPosition = position;
        moveDirection = direction.normalized;
        moveOffset = offset;
        targetRenderer.enabled = true;
        targetCollider.enabled = true;
        isHit = false;
        isRespawning = false;
        RestartLifetime();
    }

    private IEnumerator RespawnAfterHit()
    {
        yield return new WaitForSeconds(0.4f);
        Respawn();
    }

    private IEnumerator HideAfterLifetime()
    {
        yield return new WaitForSeconds(lifetime);

        if (!isHit)
        {
            isHit = true;
            targetRenderer.enabled = false;
            targetCollider.enabled = false;
            Respawn();
        }
    }

    private void RestartLifetime()
    {
        if (lifetimeCoroutine != null)
        {
            StopCoroutine(lifetimeCoroutine);
        }

        lifetimeCoroutine = StartCoroutine(HideAfterLifetime());
    }

    private void Respawn()
    {
        if (isRespawning)
        {
            return;
        }

        isRespawning = true;

        VRGameplayBootstrap bootstrap = VRGameplayBootstrap.Instance;
        if (bootstrap == null)
        {
            bootstrap = FindFirstObjectByType<VRGameplayBootstrap>();
        }

        if (bootstrap == null)
        {
            Debug.LogError($"Cannot respawn target {name}: VRGameplayBootstrap is missing.");
            isRespawning = false;
            return;
        }

        bootstrap.RespawnTarget(this);
    }

    private void CreateExplosion()
    {
        GameObject explosionObject = new("Target Hit Explosion");
        explosionObject.transform.position = transform.position;
        ParticleSystem particleSystem = explosionObject.AddComponent<ParticleSystem>();

        ParticleSystem.MainModule main = particleSystem.main;
        main.duration = 0.25f;
        main.loop = false;
        main.startLifetime = 0.35f;
        main.startSpeed = 2.5f;
        main.startSize = 0.08f;
        main.startColor = new Color(1f, 0.45f, 0.05f, 1f);
        main.maxParticles = 24;

        ParticleSystem.EmissionModule emission = particleSystem.emission;
        emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 18) });

        ParticleSystem.ShapeModule shape = particleSystem.shape;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = 0.05f;

        particleSystem.Play();
        Destroy(explosionObject, 1f);
    }
}
