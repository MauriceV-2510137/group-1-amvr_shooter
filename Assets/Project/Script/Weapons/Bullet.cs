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
    }

    void Start()
    {
        rigidBody.linearVelocity = transform.forward * speed;
        Destroy(gameObject, maxFlightTime);
    }

    private void OnCollisionEnter(Collision collision)
    {
        PlayHitSoundAndDestroy();
    }

    private void PlayHitSoundAndDestroy()
    {
        if (!hitSound)
        {
            Destroy(gameObject);
            return;
        }
        AudioSource.PlayClipAtPoint(hitSound, transform.position);
        Destroy(gameObject);
    }
}
