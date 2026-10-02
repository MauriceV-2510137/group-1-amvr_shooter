using Unity.XR.CoreUtils;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.XR;

public class QuestCameraSetup : MonoBehaviour
{
    private void Awake()
    {
        Camera camera = GetComponent<Camera>();
        if (camera == null)
        {
            return;
        }

        Transform cameraTransform = camera.transform;
        Vector3 cameraPosition = cameraTransform.position;
        Quaternion cameraRotation = cameraTransform.rotation;

        XROrigin xrOrigin = camera.GetComponentInParent<XROrigin>();
        if (xrOrigin == null)
        {
            GameObject originObject = new GameObject("XR Origin");
            originObject.transform.SetPositionAndRotation(cameraPosition, cameraRotation);
            cameraTransform.SetParent(originObject.transform, true);
            xrOrigin = originObject.AddComponent<XROrigin>();
        }

        xrOrigin.Camera = camera;

        TrackedPoseDriver poseDriver = camera.GetComponent<TrackedPoseDriver>();
        if (poseDriver == null)
        {
            poseDriver = camera.gameObject.AddComponent<TrackedPoseDriver>();
        }

        poseDriver.trackingType = TrackedPoseDriver.TrackingType.RotationAndPosition;
        poseDriver.updateType = TrackedPoseDriver.UpdateType.BeforeRender;
        poseDriver.positionInput = new InputActionProperty(
            new InputAction("Quest Head Position", InputActionType.Value, "<XRHMD>/centerEyePosition"));
        poseDriver.rotationInput = new InputActionProperty(
            new InputAction("Quest Head Rotation", InputActionType.Value, "<XRHMD>/centerEyeRotation"));
        poseDriver.positionInput.action.Enable();
        poseDriver.rotationInput.action.Enable();
    }
}
