using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class VRMenuController : MonoBehaviour
{
    private VRGameplayBootstrap gameplay;
    private Transform cameraTransform;
    private Transform rightHandTransform;
    private Handgun handgun;
    private CanvasGroup canvasGroup;
    private InputAction menuToggleAction;
    private InputAction resetScoreAction;
    private InputAction restartLevelAction;

    public static VRMenuController Create(Transform leftHandTransform, Transform rightHandTransform, Transform headsetTransform, VRGameplayBootstrap gameplayBootstrap)
    {
        GameObject menuObject = new("VR Menu");
        menuObject.transform.SetParent(leftHandTransform, false);
        menuObject.transform.SetLocalPositionAndRotation(new Vector3(0.2f, 0.1f, 0.25f), Quaternion.identity);
        menuObject.transform.localScale = Vector3.one * 0.00095f; // ???

        VRMenuController menu = menuObject.AddComponent<VRMenuController>();
        menu.gameplay = gameplayBootstrap;
        menu.rightHandTransform = rightHandTransform;
        menu.handgun = FindFirstObjectByType<Handgun>();
        menu.cameraTransform = headsetTransform;
        menu.BuildMenu();
        menu.ConfigureInput();
        return menu;
    }

    private void LateUpdate()
    {
        UpdateAimedButton();

        if (cameraTransform == null)
        {
            return;
        }

        Vector3 directionToHeadset = cameraTransform.position - transform.position;
        if (directionToHeadset.sqrMagnitude > 0.001f)
        {
            transform.rotation = Quaternion.LookRotation(-directionToHeadset.normalized, Vector3.up);
        }
    }

    private void UpdateAimedButton()
    {
        VRShootableButton aimedButton = null;
        if (canvasGroup != null && canvasGroup.alpha > 0.1f && rightHandTransform != null)
        {
            Ray ray;
            if (handgun != null && handgun.TryGetMuzzleRay(out Ray muzzleRay))
            {
                ray = muzzleRay;
            }
            else
            {
                ray = new Ray(rightHandTransform.position, rightHandTransform.forward);
            }
            RaycastHit[] hits = Physics.RaycastAll(ray, 10f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Collide);
            System.Array.Sort(hits, (first, second) => first.distance.CompareTo(second.distance));

            foreach (RaycastHit hit in hits)
            {
                aimedButton = hit.collider.GetComponentInParent<VRShootableButton>();
                if (aimedButton != null)
                {
                    break;
                }
            }
        }

        VRShootableButton[] buttons = FindObjectsByType<VRShootableButton>(FindObjectsSortMode.None);
        foreach (VRShootableButton button in buttons)
        {
            button.SetAimedAt(button == aimedButton);
        }
    }

    private void BuildMenu()
    {
        Canvas canvas = gameObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvas.sortingOrder = 20;
        gameObject.AddComponent<CanvasScaler>();
        canvasGroup = gameObject.AddComponent<CanvasGroup>();
        canvasGroup.alpha = 0.8f;

        GameObject panelObject = new GameObject("Menu Panel");
        panelObject.transform.SetParent(transform, false);
        RectTransform panelRect = panelObject.AddComponent<RectTransform>();
        panelRect.sizeDelta = new Vector2(650f, 520f);

        Image panelImage = panelObject.AddComponent<Image>();
        panelImage.color = new Color(0.015f, 0.03f, 0.06f, 0.46f);

        VerticalLayoutGroup layout = panelObject.AddComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(70, 70, 65, 65);
        layout.spacing = 22f;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = true;

        CreateLabel(panelObject.transform, "ARENA CONTROL", 40, Color.cyan);
        CreateLabel(panelObject.transform, "SHOOT A BUTTON TO ACTIVATE", 19, new Color(0.75f, 0.9f, 0.95f));
        CreateButton(panelObject.transform, "RESET SCORE", ResetScore);
        CreateButton(panelObject.transform, "RESTART LEVEL", RestartLevel);
    }

    private void ConfigureInput()
    {
        menuToggleAction = CreateAction("Menu Toggle", "<XRController>{LeftHand}/menuButton", "<Keyboard>/m");
        resetScoreAction = CreateAction("Reset Score", "<XRController>{RightHand}/secondaryButton", "<Keyboard>/r");
        restartLevelAction = CreateAction("Restart Level", "<XRController>{RightHand}/thumbstickClicked", "<Keyboard>/l");

        menuToggleAction.performed += _ => ToggleMenu();
        resetScoreAction.performed += _ => ResetScore();
        restartLevelAction.performed += _ => RestartLevel();

        menuToggleAction.Enable();
        resetScoreAction.Enable();
        restartLevelAction.Enable();
    }

    private InputAction CreateAction(string actionName, params string[] bindings)
    {
        InputAction action = new(actionName, InputActionType.Button);
        foreach (string binding in bindings)
        {
            action.AddBinding(binding);
        }
        return action;
    }

    private void CreateLabel(Transform parent, string label, int fontSize, Color color)
    {
        GameObject labelObject = new(label);
        labelObject.transform.SetParent(parent, false);
        Text text = labelObject.AddComponent<Text>();
        text.text = label;
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.fontSize = fontSize;
        text.alignment = TextAnchor.MiddleCenter;
        text.color = color;
        LayoutElement layoutElement = labelObject.AddComponent<LayoutElement>();
        layoutElement.minHeight = 70f;
    }

    private VRShootableButton CreateButton(Transform parent, string label, UnityEngine.Events.UnityAction action)
    {
        GameObject buttonObject = new(label);
        buttonObject.transform.SetParent(parent, false);
        Image image = buttonObject.AddComponent<Image>();
        image.color = new Color(0.04f, 0.18f, 0.25f, 0.72f);

        Button button = buttonObject.AddComponent<Button>();
        button.targetGraphic = image;
        button.onClick.AddListener(action);

        VRShootableButton shootableButton = buttonObject.AddComponent<VRShootableButton>();
        shootableButton.Initialize(button, image);

        BoxCollider hitbox = buttonObject.AddComponent<BoxCollider>();
        hitbox.isTrigger = true;
        hitbox.size = new Vector3(510f, 75f, 20f);

        Rigidbody physicsBody = buttonObject.AddComponent<Rigidbody>();
        physicsBody.isKinematic = true;
        physicsBody.useGravity = false;
        physicsBody.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;

        Text text = new GameObject("Label").AddComponent<Text>();
        text.transform.SetParent(buttonObject.transform, false);
        text.text = label;
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.fontSize = 30;
        text.alignment = TextAnchor.MiddleCenter;
        text.color = Color.white;
        RectTransform textRect = text.rectTransform;
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;

        LayoutElement layoutElement = buttonObject.AddComponent<LayoutElement>();
        layoutElement.minHeight = 75f;
        return shootableButton;
    }

    private void ToggleMenu()
    {
        bool menuVisible = canvasGroup.alpha <= 0.1f;
        canvasGroup.alpha = menuVisible ? 0.8f : 0f;
        canvasGroup.interactable = menuVisible;
        canvasGroup.blocksRaycasts = canvasGroup.interactable;
        SetButtonsEnabled(menuVisible);
    }

    private void SetButtonsEnabled(bool enabled)
    {
        VRShootableButton[] buttons = GetComponentsInChildren<VRShootableButton>(true);
        foreach (VRShootableButton button in buttons)
        {
            button.SetInteractionEnabled(enabled);
        }
    }

    private void ResetScore()
    {
        VRScoreManager.ResetScore();
    }

    private void RestartLevel()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    private void OnDestroy()
    {
        DisposeAction(menuToggleAction);
        DisposeAction(resetScoreAction);
        DisposeAction(restartLevelAction);
    }

    private void DisposeAction(InputAction action)
    {
        if (action == null)
        {
            return;
        }

        action.Disable();
        action.Dispose();
    }
}

public class VRShootableButton : MonoBehaviour
{
    private Button button;
    private Image image;
    private Color defaultColor;
    private Color highlightedColor;

    public void Initialize(Button targetButton, Image targetImage)
    {
        button = targetButton;
        image = targetImage;
        defaultColor = image.color;
        highlightedColor = new Color(0.15f, 0.65f, 0.95f, 0.95f);
    }

    public void Activate()
    {
        if (button == null)
        {
            return;
        }

        button.onClick.Invoke();
        StartCoroutine(FlashButton());
    }

    public void SetAimedAt(bool aimedAt)
    {
        if (image != null)
        {
            image.color = aimedAt ? highlightedColor : defaultColor;
        }
    }

    public void SetInteractionEnabled(bool enabled)
    {
        if (TryGetComponent<Collider>(out var collider))
        {
            collider.enabled = enabled;
        }

        if (!enabled)
        {
            SetAimedAt(false);
        }
    }

    private IEnumerator FlashButton()
    {
        image.color = Color.white;
        yield return new WaitForSeconds(0.12f);
        image.color = defaultColor;
    }
}
