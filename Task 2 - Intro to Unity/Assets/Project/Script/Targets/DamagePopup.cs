using UnityEngine;

public class DamagePopup : MonoBehaviour
{
    private TextMesh textMesh;
    private float lifetime = 0.8f;
    private Vector3 startPosition;

    public static void Create(Vector3 position, int damage, bool critical, Color? color = null)
    {
        GameObject popupObject = new("Damage Number");
        popupObject.transform.position = position;
        DamagePopup popup = popupObject.AddComponent<DamagePopup>();
        popup.textMesh = popupObject.AddComponent<TextMesh>();
        popup.textMesh.text = critical ? $"{damage}!" : damage.ToString();
        popup.textMesh.fontSize = critical ? 64 : 48;
        popup.textMesh.characterSize = 0.025f;
        popup.textMesh.anchor = TextAnchor.MiddleCenter;
        popup.textMesh.alignment = TextAlignment.Center;
        popup.textMesh.color = color ?? (critical ? new Color(1f, 0.85f, 0.1f) : Color.white);
        popup.startPosition = position;
    }

    private void Update()
    {
        lifetime -= Time.deltaTime;
        transform.position = startPosition + Vector3.up * (0.8f - lifetime) * 0.9f;
        if (Camera.main != null)
        {
            transform.rotation = Camera.main.transform.rotation;
        }

        textMesh.color = new Color(textMesh.color.r, textMesh.color.g, textMesh.color.b, Mathf.Clamp01(lifetime * 2f));
        if (lifetime <= 0f)
        {
            Destroy(gameObject);
        }
    }
}
