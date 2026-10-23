using UnityEngine;

[RequireComponent (typeof(Rigidbody))]
[RequireComponent (typeof(Collider))]
public class Bucket : MonoBehaviour
{
    [SerializeField] private float bulletImpact = 50f;   

    private Rigidbody rb = null;

    private void Awake()
    {
        TryGetComponent(out rb);        
    }

    private void OnCollisionEnter(Collision collision)
    {
        if(collision.collider.TryGetComponent(out Bullet bullet))
        {
            Vector3 bulltetForward = bullet.gameObject.transform.forward;
            ApplyForce(bulltetForward);
        }
    }

    private void ApplyForce(Vector3 forward)
    {
        if (!rb) return;
        rb.AddForce(forward * bulletImpact);
    }

    [ContextMenu("Apply Force!!")]
    private void DebugTrigger()
    {
        Vector3 randomForward = new (
            Random.Range(-10f, 10f),
            Random.Range(0f, 5f),
            Random.Range(-10f, 10f)
        );
        randomForward = randomForward.normalized;
        ApplyForce(randomForward);
    }
}
