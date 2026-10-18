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
        Destroy(gameObject, maxFlightTime);
    }

    private void OnCollisionEnter(Collision collision)
    {
        EnemyTarget enemy = collision.collider.GetComponentInParent<EnemyTarget>();
        if (enemy != null && collision.contactCount > 0)
        {
            enemy.TakeDamage(10f, collision.collider);
        }

        MovingTarget target = collision.collider.GetComponentInParent<MovingTarget>();
        if (target != null && collision.contactCount > 0)
        {
            target.RegisterHit(collision.GetContact(0).point);
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
