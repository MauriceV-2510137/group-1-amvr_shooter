using UnityEngine;
using UnityEngine.InputSystem;

public class VRGunInteraction : MonoBehaviour
{
    [SerializeField] private float pickupDistance = 0.8f;
    [SerializeField] private float gripPressThreshold = 0.6f;
    [SerializeField] private float gripReleaseThreshold = 0.4f;

    private readonly bool[] gripWasPressed = new bool[2];
    private readonly InputAction[] gripActions = new InputAction[2];
    private Transform[] hands;
    private Handgun handgun;
    private Rigidbody body;
    private Collider gunCollider;
    private int holdingHand = -1;

    public void Initialize(Transform leftHand, Transform rightHand)
    {
        hands = new[] { leftHand, rightHand };
        handgun = GetComponent<Handgun>();
        body = GetComponent<Rigidbody>();
        if (body == null)
        {
            body = gameObject.AddComponent<Rigidbody>();
        }

        body.useGravity = false;
        body.isKinematic = true;
        AddColliderIfNeeded();
        gunCollider = GetComponent<Collider>();

        CreateGripAction(0, "<XRController>{LeftHand}/grip", "<Keyboard>/q");
        CreateGripAction(1, "<XRController>{RightHand}/grip", "<Keyboard>/e");

        holdingHand = FindInitialHoldingHand();
        if (holdingHand >= 0)
        {
            gripWasPressed[holdingHand] = true;
        }
    }

    private void Update()
    {
        if (hands == null || handgun == null)
        {
            return;
        }

        for (int index = 0; index < gripActions.Length; index++)
        {
            bool pressed = ReadGrip(index);
            if (pressed && !gripWasPressed[index])
            {
                TryGrab(index);
            }
            else if (!pressed && gripWasPressed[index] && holdingHand == index)
            {
                Drop();
            }

            gripWasPressed[index] = pressed;
        }
    }

    private void TryGrab(int handIndex)
    {
        if (hands[handIndex] == null || !IsWithinPickupRange(hands[handIndex]))
        {
            return;
        }

        AttachToHand(handIndex);
    }

    private bool IsWithinPickupRange(Transform hand)
    {
        if (gunCollider != null)
        {
            return Vector3.Distance(gunCollider.ClosestPoint(hand.position), hand.position) <= pickupDistance;
        }

        return Vector3.Distance(transform.position, hand.position) <= pickupDistance;
    }

    private void AttachToHand(int handIndex)
    {
        Transform hand = hands[handIndex];
        holdingHand = handIndex;
        body.isKinematic = true;
        body.useGravity = false;
        transform.SetParent(hand, false);
        transform.SetLocalPositionAndRotation(new Vector3(0f, -0.16f, 0.12f), Quaternion.Euler(90f, 0f, 0f));
        handgun.SetHeld(true);
    }

    private void Drop()
    {
        transform.SetParent(null, true);
        holdingHand = -1;
        body.isKinematic = false;
        body.useGravity = true;
        handgun.SetHeld(false);
    }

    private bool ReadGrip(int handIndex)
    {
        float value = gripActions[handIndex].ReadValue<float>();
        bool wasPressed = gripWasPressed[handIndex];
        return wasPressed
            ? value >= gripReleaseThreshold
            : value >= gripPressThreshold;
    }

    private void CreateGripAction(int handIndex, string controllerBinding, string keyboardBinding)
    {
        InputAction action = new($"Gun Grip {handIndex}", InputActionType.Value);
        action.AddBinding(controllerBinding);
        action.AddBinding(keyboardBinding);
        action.Enable();
        gripActions[handIndex] = action;
    }

    private int FindInitialHoldingHand()
    {
        for (int index = 0; index < hands.Length; index++)
        {
            if (hands[index] != null && transform.IsChildOf(hands[index]))
            {
                return index;
            }
        }

        return -1;
    }

    private void AddColliderIfNeeded()
    {
        if (GetComponent<Collider>() != null)
        {
            return;
        }

        BoxCollider collider = gameObject.AddComponent<BoxCollider>();
        Bounds bounds = new(Vector3.zero, Vector3.one);
        Renderer[] renderers = GetComponentsInChildren<Renderer>();
        if (renderers.Length > 0)
        {
            bounds = renderers[0].bounds;
            for (int index = 1; index < renderers.Length; index++)
            {
                bounds.Encapsulate(renderers[index].bounds);
            }

            collider.center = transform.InverseTransformPoint(bounds.center);
            collider.size = transform.InverseTransformVector(bounds.size);
        }
    }

    private void OnDestroy()
    {
        for (int index = 0; index < gripActions.Length; index++)
        {
            gripActions[index]?.Dispose();
        }
    }
}
