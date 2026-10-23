using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class Bullet : MonoBehaviour
{
    [Header("Bullet Config")]
    [SerializeField] private float speed = 50f;
    [SerializeField] private float maxFlightTime = 5f;

    [Header("Audio")]
    [SerializeField] private AudioClip hitSound = null;

    private Rigidbody rigidBody = null;
    private Vector3 previousPosition;
    private bool hitProcessed;

    private void Awake()
    {
        if(!TryGetComponent(out rigidBody))
        {
            Debug.LogWarning("Bullet:: RigidBody not found!");
        }

        if(hitSound == null)
        {
            Debug.LogWarning("Handgun:: hitSound not set!");
        }

        rigidBody.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
    }

    void Start()
    {
        rigidBody.linearVelocity = transform.forward * speed;
        previousPosition = transform.position;
        Destroy(gameObject, maxFlightTime);
    }

    private void Update()
    {
        if (hitProcessed)
        {
            return;
        }

        Vector3 currentPosition = transform.position;
        Vector3 travel = currentPosition - previousPosition;
        float distance = travel.magnitude;
        if (distance > 0f
            && Physics.Raycast(previousPosition, travel / distance, out RaycastHit hit, distance,
                Physics.DefaultRaycastLayers, QueryTriggerInteraction.Collide))
        {
            if (TryHitTarget(hit.collider, hit.point))
            {
                return;
            }
        }

        previousPosition = currentPosition;
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (hitProcessed)
        {
            return;
        }

        Vector3 hitPosition = collision.contactCount > 0
            ? collision.GetContact(0).point
            : transform.position;
        if (TryHitTarget(collision.collider, hitPosition))
        {
            return;
        }

        StartCoroutine(PlayHitSoundAndDestroy());
    }

    private void OnTriggerEnter(Collider other)
    {
        VRShootableButton shootableButton = other.GetComponentInParent<VRShootableButton>();
        if (shootableButton == null) return;
        shootableButton.Activate();

        StartCoroutine(PlayHitSoundAndDestroy());
    }

    private bool TryHitTarget(Collider hitCollider, Vector3 hitPosition)
    {
        EnemyTarget enemy = hitCollider.GetComponentInParent<EnemyTarget>();
        if (enemy != null)
        {
            hitProcessed = true;
            enemy.TakeDamage(10f, hitCollider);
            StartCoroutine(PlayHitSoundAndDestroy());
            return true;
        }

        MovingTarget target = hitCollider.GetComponentInParent<MovingTarget>();
        if (target != null)
        {
            hitProcessed = true;
            target.RegisterHit(hitPosition);
            StartCoroutine(PlayHitSoundAndDestroy());
            return true;
        }

        return false;
    }

    private IEnumerator PlayHitSoundAndDestroy()
    {
        if (!hitSound)
        {
            Destroy(gameObject);
            yield break;
        }

        yield return new WaitForSeconds(0.1f);

        AudioSource.PlayClipAtPoint(hitSound, transform.position, 2f);
        Destroy(gameObject);
    }
}
