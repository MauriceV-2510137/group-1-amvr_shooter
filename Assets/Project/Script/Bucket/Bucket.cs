using System;
using UnityEngine;

[RequireComponent (typeof(Rigidbody))]
[RequireComponent (typeof(Collider))]
public class Bucket : MonoBehaviour
{
    [SerializeField]
    private float bulletImpact = 50f;   

    private Rigidbody rigidbody = null;

    private void Awake()
    {
        TryGetComponent(out rigidbody);        
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
        if (!rigidbody) return;
        rigidbody.AddForce(forward * bulletImpact);
    }

    [ContextMenu("Apply Force!!")]
    private void DebugTrigger()
    {
        Vector3 randomForward = new Vector3(
            UnityEngine.Random.Range(-10f, 10f),
            UnityEngine.Random.Range(0f, 5f),
            UnityEngine.Random.Range(-10f, 10f)
        );
        randomForward = randomForward.normalized;
        ApplyForce(randomForward);
    }
}
