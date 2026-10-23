using UnityEngine;

public class EnemyProjectile : MonoBehaviour
{
    private PlayerHealth playerHealth;
    private float damage;
    private bool hit;

    public void Initialize(PlayerHealth target, float lifetime, float projectileDamage)
    {
        playerHealth = target;
        damage = projectileDamage;
        Destroy(gameObject, lifetime);
    }

    private void Update()
    {
        if (hit || playerHealth == null)
        {
            return;
        }

        if (Vector3.Distance(transform.position, playerHealth.transform.position) <= 0.28f)
        {
            hit = true;
            playerHealth.TakeDamage(damage);
            DamagePopup.Create(
                playerHealth.transform.position + Vector3.up * 0.25f,
                Mathf.RoundToInt(damage),
                false,
                Color.red);
            Destroy(gameObject);
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (hit)
        {
            return;
        }

        PlayerHealth hitPlayer = collision.collider.GetComponentInParent<PlayerHealth>();
        if (hitPlayer != null && hitPlayer == playerHealth)
        {
            hit = true;
            hitPlayer.TakeDamage(damage);
            DamagePopup.Create(
                hitPlayer.transform.position + Vector3.up * 0.25f,
                Mathf.RoundToInt(damage),
                false,
                Color.red);
        }

        Destroy(gameObject);
    }
}
