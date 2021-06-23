using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using UnityEngine.XR;

public class P2Utils : MonoBehaviour
{
    public enum RenderingMode { Stereo, Mono, LeftOnly, RightOnly };
    public enum TrackingMode { Normal, Position, Orientation, Disabled };
    public Camera leftEye;
    public Camera rightEye;
    public GameObject leftParent;
    public GameObject rightParent;
    public static P2Utils instance;

    public GameObject rightController;
    public GameObject laggedRightController;
    public GameObject TrackingSpace;

    public Text RenderingText;
    public Text TrackingText;
    public Text iodText;

    public RenderingMode renderingMode;
    public TrackingMode trackingMode;
    Vector3 leftPosStart;
    Vector3 rightPosStart;
    bool rotationLock = false;
    Quaternion lockedRotation = Quaternion.identity;
    float iod;
    public int trackingLag = 0;
    public int renderingLag = 0;
    Vector3[] buffer = new Vector3[100];
    int bufferIndex = 0;
    int frameCount = 0;


    // Start is called before the first frame update
    void Start()
    {
        renderingMode = RenderingMode.Stereo;
        trackingMode = TrackingMode.Normal;
        leftPosStart = leftParent.transform.position;
        rightPosStart = rightParent.transform.position;
        iod = 0.065f;
        if (instance == null)
            instance = this;
    }

    // Update is called once per frame
    void Update()
    {
        if (renderingLag == 0 || frameCount == renderingLag)
        {
            TrackingSpace.gameObject.SetActive(true);
            frameCount = 0;
        }
        else if (frameCount < renderingLag) {
            TrackingSpace.gameObject.SetActive(false);
            frameCount++;
        }
        RenderingText.text = "Rendering Mode (A): " + renderingMode.ToString() + " Rendering Lag: " + renderingLag + " Frames";
        TrackingText.text = "Tracking Mode (B): " + trackingMode.ToString() + " Tracking Lag: " + trackingLag + " Frames";
        iodText.text = "IOD: " + iod + " m";
        if (OVRInput.GetDown(OVRInput.Button.One))
        {
            ToggleRenderingMode();
        }
        if (OVRInput.GetDown(OVRInput.Button.Two)) {
            ToggleTrackingMode();
        }
        if (OVRInput.GetDown(OVRInput.Button.PrimaryIndexTrigger))
        {
            if (trackingLag > 0) trackingLag--;
        }
        if (OVRInput.GetDown(OVRInput.Button.SecondaryIndexTrigger))
        {
            if (trackingLag < 99) trackingLag++;
        }
        if (OVRInput.GetDown(OVRInput.Button.PrimaryHandTrigger))
        {
            if (renderingLag > 0) renderingLag--;
        }
        if (OVRInput.GetDown(OVRInput.Button.SecondaryHandTrigger))
        {
            if (renderingLag < 10) renderingLag++;
        }
        float stick = OVRInput.Get(OVRInput.Axis2D.SecondaryThumbstick, OVRInput.Controller.Touch).x;
        float newIod = iod;
        if (stick > 0) newIod += 0.001f;
        else if (stick < 0) newIod -= 0.001f;
        if (newIod < -0.1f) newIod = -0.1f;
        else if (newIod > 0.3f) newIod = 0.3f;
        setIODDistance(newIod);
        if (renderingMode == RenderingMode.Mono)
        {
            var lv = leftEye.GetStereoViewMatrix(Camera.StereoscopicEye.Left);
            var lp = leftEye.GetStereoProjectionMatrix(Camera.StereoscopicEye.Left);
            rightEye.SetStereoViewMatrix(Camera.StereoscopicEye.Right, lv);
            rightEye.SetStereoProjectionMatrix(Camera.StereoscopicEye.Right, lp);
        }

        
        
        buffer[bufferIndex] = rightController.transform.position; 
        if (bufferIndex - trackingLag >= 0 && bufferIndex - trackingLag < 100 && buffer[bufferIndex - trackingLag] != null)
        {
            laggedRightController.transform.position = buffer[bufferIndex - trackingLag];
        }
        if (bufferIndex + 1 >= 100) bufferIndex = 0;
        else bufferIndex++;
        
        
    }

    private void LateUpdate()
    {
        if (rotationLock)
        {
            transform.rotation = lockedRotation;
        }
        if (iod != 0.065f)
        {
#pragma warning disable CS0618 // Type or member is obsolete
            var leftPos = UnityEngine.XR.InputTracking.GetLocalPosition(UnityEngine.XR.XRNode.LeftEye);
            var rightPos = UnityEngine.XR.InputTracking.GetLocalPosition(UnityEngine.XR.XRNode.RightEye);
#pragma warning restore CS0618 // Type or member is obsolete
            var direction = Vector3.Normalize(leftPos - rightPos);
            float lefty = leftParent.transform.localPosition.y;
            Vector3 newLeftLocalPosition = leftPosStart + direction * (iod - 0.065f) * 17.5f;
            newLeftLocalPosition.y = lefty;
            float righty = rightParent.transform.localPosition.y;
            Vector3 newRightLocalPosition = rightPosStart - direction * (iod - 0.065f) * 17.5f;
            newRightLocalPosition.y = righty;
            leftParent.transform.localPosition = newLeftLocalPosition;
            rightParent.transform.localPosition = newRightLocalPosition;
        }
    }

    public void ToggleTrackingMode()
    {
        if (trackingMode == TrackingMode.Disabled)
        {
            changeTrackingMode(TrackingMode.Normal);
        }
        else
        {
            TrackingMode newTrackingMode = (TrackingMode)(((int)trackingMode + 1) % ((int)TrackingMode.Disabled + 1));
            changeTrackingMode(newTrackingMode);
        }
    }

    public void changeTrackingMode(TrackingMode newTrackingMode)
    {
        trackingMode = newTrackingMode;
        switch (trackingMode)
        {
            case TrackingMode.Normal:
                OVRManager.instance.usePositionTracking = true;
                rotationLock = false;
                break;
            case TrackingMode.Position:
                OVRManager.instance.usePositionTracking = true;
                lockedRotation = transform.rotation;
                rotationLock = true;
                break;
            case TrackingMode.Orientation:
                OVRManager.instance.usePositionTracking = false;
                rotationLock = false;
                break;
            case TrackingMode.Disabled:
                OVRManager.instance.usePositionTracking = false;
                lockedRotation = transform.rotation;
                rotationLock = true;
                break;
        }
    }

    public void ToggleRenderingMode()
    {
        if (renderingMode == RenderingMode.RightOnly)
        {
            changeRenderingMode(RenderingMode.Stereo);
        }
        else
        {
            RenderingMode newRenderingMode = (RenderingMode)(((int)renderingMode + 1) % ((int)RenderingMode.RightOnly + 1));
            changeRenderingMode(newRenderingMode);
        }
    }

    public void changeRenderingMode(P2Utils.RenderingMode mode)
    {
        renderingMode = mode;
        switch (renderingMode)
        {
        default:
        case RenderingMode.Stereo:
            Shader.SetGlobalInt("_RenderingMode", 0);
            rightEye.ResetStereoViewMatrices();
            rightEye.ResetStereoProjectionMatrices();
            break;
        case RenderingMode.Mono:
            Shader.SetGlobalInt("_RenderingMode", 1);
            break;
        case RenderingMode.LeftOnly:
            Shader.SetGlobalInt("_RenderingMode", 2);
            rightEye.ResetStereoViewMatrices();
            rightEye.ResetStereoProjectionMatrices();
            break;
        case RenderingMode.RightOnly:
            Shader.SetGlobalInt("_RenderingMode", 3);
            rightEye.ResetStereoViewMatrices();
            rightEye.ResetStereoProjectionMatrices();
            break;
        }
    }

    public void disableTracking(bool enabled)
    {
        UnityEngine.XR.XRDevice.DisableAutoXRCameraTracking(leftEye, enabled);
        UnityEngine.XR.XRDevice.DisableAutoXRCameraTracking(rightEye, enabled);
    }

    public void setIODDistance(float distance)
    {
        iod = distance;
        if (iod == 0.065f)
            resetEyeParents();
    }

    void resetEyeParents()
    {
        float lefty = leftParent.transform.localPosition.y;
        float righty = rightParent.transform.localPosition.y;
        leftPosStart.y = lefty;
        rightPosStart.y = righty;
        leftParent.transform.localPosition = leftPosStart;
        rightParent.transform.localPosition = rightPosStart;
    }
}
