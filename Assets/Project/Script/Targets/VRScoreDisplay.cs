using UnityEngine;
using UnityEngine.UI;

public class VRScoreDisplay : MonoBehaviour
{
    private Text scoreText;

    public static VRScoreDisplay Create(Transform cameraTransform)
    {
        GameObject canvasObject = new GameObject("VR Score Display");
        canvasObject.transform.SetParent(cameraTransform, false);
        canvasObject.transform.localPosition = new Vector3(0f, -0.25f, 1f);
        canvasObject.transform.localRotation = Quaternion.identity;
        canvasObject.transform.localScale = Vector3.one * 0.0015f;

        Canvas canvas = canvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvasObject.AddComponent<CanvasScaler>();

        GameObject textObject = new GameObject("Score Text");
        textObject.transform.SetParent(canvasObject.transform, false);
        RectTransform rectTransform = textObject.AddComponent<RectTransform>();
        rectTransform.sizeDelta = new Vector2(500f, 150f);

        Text text = textObject.AddComponent<Text>();
        text.alignment = TextAnchor.MiddleCenter;
        text.fontSize = 42;
        text.color = Color.white;
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        VRScoreDisplay display = canvasObject.AddComponent<VRScoreDisplay>();
        display.scoreText = text;
        display.Refresh();
        return display;
    }

    private void Update()
    {
        Refresh();
    }

    private void Refresh()
    {
        if (scoreText != null)
        {
            int seconds = Mathf.CeilToInt(VRScoreManager.TimeRemaining);
            scoreText.text = $"SCORE {VRScoreManager.Score}\nHIGH {VRScoreManager.HighScore}\nTIME {seconds / 60:00}:{seconds % 60:00}";
        }
    }
}
