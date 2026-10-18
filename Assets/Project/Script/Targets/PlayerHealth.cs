using UnityEngine;
using UnityEngine.UI;

public class PlayerHealth : MonoBehaviour
{
    public static PlayerHealth Instance { get; private set; }

    [SerializeField] private float maxHealth = 100f;
    private float currentHealth;
    private Image healthFill;
    private Text healthLabel;

    public float CurrentHealth => currentHealth;
    public float MaxHealth => maxHealth;

    private void Awake()
    {
        Instance = this;
        currentHealth = maxHealth;
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    public void CreateUi(Camera playerCamera)
    {
        if (healthFill != null || playerCamera == null)
        {
            return;
        }

        GameObject canvasObject = new("Player HUD");
        canvasObject.transform.SetParent(playerCamera.transform, false);
        Canvas canvas = canvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvas.sortingOrder = 100;
        canvasObject.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        canvasObject.AddComponent<GraphicRaycaster>();

        RectTransform root = canvasObject.GetComponent<RectTransform>();
        root.sizeDelta = new Vector2(700f, 140f);
        root.localPosition = new Vector3(0f, -0.38f, 1.2f);
        root.localRotation = Quaternion.identity;
        root.localScale = Vector3.one * 0.001f;
        root.anchorMin = Vector2.zero;
        root.anchorMax = Vector2.one;
        root.offsetMin = Vector2.zero;
        root.offsetMax = Vector2.zero;
        GameObject panel = CreateImage("Healthbar Panel", root, new Color(0.015f, 0.02f, 0.035f, 0.9f));
        RectTransform panelRect = panel.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.08f, 0.035f);
        panelRect.anchorMax = new Vector2(0.42f, 0.13f);
        panelRect.offsetMin = Vector2.zero;
        panelRect.offsetMax = Vector2.zero;

        GameObject background = CreateImage("Healthbar Background", panelRect, new Color(0.08f, 0.1f, 0.14f, 1f));
        RectTransform backgroundRect = background.GetComponent<RectTransform>();
        backgroundRect.anchorMin = new Vector2(0.05f, 0.25f);
        backgroundRect.anchorMax = new Vector2(0.95f, 0.7f);
        backgroundRect.offsetMin = Vector2.zero;
        backgroundRect.offsetMax = Vector2.zero;

        GameObject fill = CreateImage("Healthbar Fill", backgroundRect, new Color(0.12f, 0.9f, 0.42f, 1f));
        RectTransform fillRect = fill.GetComponent<RectTransform>();
        fillRect.anchorMin = Vector2.zero;
        fillRect.anchorMax = Vector2.one;
        fillRect.offsetMin = Vector2.zero;
        fillRect.offsetMax = Vector2.zero;
        healthFill = fill.GetComponent<Image>();

        healthLabel = CreateText("Health Label", panelRect, Color.white);
        healthLabel.fontSize = 26;
        healthLabel.fontStyle = FontStyle.Bold;
        healthLabel.alignment = TextAnchor.MiddleCenter;
        healthLabel.rectTransform.anchorMin = new Vector2(0.05f, 0.7f);
        healthLabel.rectTransform.anchorMax = new Vector2(0.95f, 1f);
        healthLabel.rectTransform.offsetMin = Vector2.zero;
        healthLabel.rectTransform.offsetMax = Vector2.zero;
        UpdateUi();
    }

    public void TakeDamage(float amount)
    {
        currentHealth = Mathf.Max(0f, currentHealth - amount);
        UpdateUi();
        if (currentHealth <= 0f)
        {
            Debug.Log("Player defeated.");
        }
    }

    private void UpdateUi()
    {
        if (healthFill == null)
        {
            return;
        }

        healthFill.rectTransform.anchorMax = new Vector2(Mathf.Clamp01(currentHealth / maxHealth), 1f);
        healthFill.rectTransform.offsetMax = Vector2.zero;
        healthLabel.text = $"HEALTH  {Mathf.CeilToInt(currentHealth)} / {Mathf.CeilToInt(maxHealth)}";
    }

    private static GameObject CreateImage(string objectName, Transform parent, Color color)
    {
        GameObject imageObject = new(objectName);
        imageObject.transform.SetParent(parent, false);
        imageObject.AddComponent<Image>().color = color;
        return imageObject;
    }

    private static Text CreateText(string objectName, Transform parent, Color color)
    {
        GameObject textObject = new(objectName);
        textObject.transform.SetParent(parent, false);
        Text text = textObject.AddComponent<Text>();
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.color = color;
        return text;
    }
}
