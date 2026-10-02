using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Collider))]
public class MovingTarget : MonoBehaviour
{
    [SerializeField] private int maximumScore = 100;
    [SerializeField] private int minimumScore = 10;
    [SerializeField] private float moveAmplitude = 1.25f;
    [SerializeField] private float moveSpeed = 1.5f;

    private Vector3 startPosition;
    private Vector3 moveDirection;
    private float moveOffset;
    private Vector3 targetScale;
    private Renderer targetRenderer;
    private Collider targetCollider;
    private bool isHit;

    private void Awake()
    {
        targetScale = transform.localScale;
        targetRenderer = GetComponent<Renderer>();
        targetCollider = GetComponent<Collider>();
    }

    public void Initialize(Vector3 direction, float offset)
    {
        startPosition = transform.position;
        moveDirection = direction.normalized;
        moveOffset = offset;
    }

    public void ConfigureTier(int maximumTierScore, int minimumTierScore, Color tierColor)
    {
        maximumScore = maximumTierScore;
        minimumScore = minimumTierScore;
        if (targetRenderer != null)
        {
            targetRenderer.material.color = tierColor;
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
        Debug.Log($"Target hit. Aim score: {score}. Total score: {VRScoreManager.Score}");

        isHit = true;
        targetRenderer.enabled = false;
        targetCollider.enabled = false;
        CreateExplosion();
        StartCoroutine(RespawnAfterHit());
    }

    public void ResetTarget(Vector3 position, Vector3 direction, float offset)
    {
        transform.position = position;
        transform.localScale = targetScale;
        startPosition = position;
        moveDirection = direction.normalized;
        moveOffset = offset;
        targetRenderer.enabled = true;
        targetCollider.enabled = true;
        isHit = false;
    }

    private IEnumerator RespawnAfterHit()
    {
        yield return new WaitForSeconds(0.4f);

        if (VRGameplayBootstrap.Instance == null)
        {
            Destroy(gameObject);
            yield break;
        }

        VRGameplayBootstrap.Instance.RespawnTarget(this);
    }

    private void CreateExplosion()
    {
        GameObject explosionObject = new GameObject("Target Hit Explosion");
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
