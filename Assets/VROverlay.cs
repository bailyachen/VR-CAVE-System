using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class VROverlay : MonoBehaviour
{
    public RectTransform RectTransform;
    public GameObject horizontalRuler;
    public GameObject verticalRuler;
    public GameObject spacialRuler;
    public GameObject mirror;
    public GameObject leftAnchor;
    public GameObject rightAnchor;
    public GameObject converganceObject;
    public VRPointerCounter pointerCounter;
    public Text eyeDistanceText;
    public Text horizontalRulerDistanceText;
    public Text verticalRulerDistanceText;
    public Text fovHText;
    public Text fovVText;
    public Text fovDText;
    public Text spacialRulerDistanceText;
    public Text spacialResolutionText;
    public Text positionStdText;
    public Text orientationStdText;
    public Text pointerCounterText;
    public Text pointerCounterDistanceText;
    public Text timerText;
    public Text accuracyText;
    public Text converganceObjectDistanceText;

    // Calculate FOV
    public float RulerWidth { get; set; } = 1;
    public float RulerHeight { get; set; } = 1;
    public float HorizontalRulerDistance { get; set; } = 1;
    public float VerticalRulerDistance { get; set; } = 1;
    public float FovHorizontal { get; set; } = 1;
    public float FovVertical { get; set; } = 1;
    public float FovDiagonal { get; set; } = 1;

    // Calculate Spacial Resolution
    public float LineSpacing { get; set; } = 0.1f;
    public float SpacialRulerDistance { get; set; } = 1;
    public float SpacialResolution { get; set; } = 1;

    public float PointerCounterDistance { get; set; } = 1;
    public float ConverganceObjectDistance { get; set; } = 1;

    public void Update()
    {
        CalculateFov();
        CalculateSpacialResolution();
        horizontalRuler.transform.localPosition = new Vector3(0, 0, HorizontalRulerDistance);
        verticalRuler.transform.localPosition = new Vector3(0, 0, VerticalRulerDistance);
        spacialRuler.transform.localPosition = new Vector3(0, 0, SpacialRulerDistance);
        pointerCounter.transform.position = new Vector3(0, 1, PointerCounterDistance);
        converganceObject.transform.localPosition = new Vector3(0, 0, ConverganceObjectDistance);
        horizontalRulerDistanceText.text = "Horizontal Ruler Distance: " + HorizontalRulerDistance + " Meters";
        verticalRulerDistanceText.text = "Vertical Ruler Distance: " + VerticalRulerDistance + " Meters";
        fovHText.text = "FOV Horizontal: " + FovHorizontal + " Degrees";
        fovVText.text = "FOV Vertical:   " + FovVertical + " Degrees";
        fovDText.text = "FOV Diagonal:   " + FovDiagonal + " Degrees";
        spacialRulerDistanceText.text = "Vertical Ruler Distance: " + SpacialRulerDistance + " Meters";
        spacialResolutionText.text = "Spacial Resolution: " + SpacialResolution + " Pixels per Degree";
        pointerCounterText.text = "Click Count: " + pointerCounter.timesClicked;
        pointerCounterDistanceText.text = "Object Distance: " + PointerCounterDistance + " Meters";
        converganceObjectDistanceText.text = "Convergance Object Distance: " + ConverganceObjectDistance + " Meters";

    }
    
    public void CalculateFov()
    {
        FovHorizontal = 2 * Mathf.Atan2(RulerWidth / 2f, HorizontalRulerDistance) * Mathf.Rad2Deg;
        float FOVVerticalRadians = 2 * Mathf.Atan2(RulerHeight / 2f, VerticalRulerDistance);
        FovVertical = FOVVerticalRadians * Mathf.Rad2Deg;
        float proportionalRulerHeight = 2 * Mathf.Tan(FOVVerticalRadians / 2f) * HorizontalRulerDistance;
        float diagonalLength = Mathf.Sqrt(Mathf.Pow(RulerWidth, 2) + Mathf.Pow(proportionalRulerHeight, 2));
        FovDiagonal = 2 * Mathf.Atan2(diagonalLength / 2f, HorizontalRulerDistance) * Mathf.Rad2Deg;
    }

    public void CalculateSpacialResolution()
    {
        SpacialResolution = SpacialRulerDistance * Mathf.Tan(Mathf.PI / 180f) / LineSpacing;
    }

    public void CalculateRightControllerPrecision()
    {
        StopAllCoroutines();
        StartCoroutine(CalculateLeftControllerPrecisionRoutine(rightAnchor));
    }

    public void CalculateLeftControllerPrecision()
    {
        StopAllCoroutines();
        StartCoroutine(CalculateLeftControllerPrecisionRoutine(leftAnchor));
    }

    public IEnumerator CalculateLeftControllerPrecisionRoutine(GameObject anchor)
    {
        positionStdText.text = "";
        orientationStdText.text = "";
        List<Vector3> positions = new List<Vector3>();
        List<Quaternion> orientations = new List<Quaternion>();
        float counter = 0;
        while (counter < 5)
        {
            positionStdText.text = "" + anchor.transform.position;
            orientationStdText.text = "" + anchor.transform.rotation;
            positions.Add(leftAnchor.transform.position);
            orientations.Add(leftAnchor.transform.rotation);
            counter += Time.deltaTime;
            yield return new WaitForEndOfFrame();
        }
        // Calculate position and orientation means
        Vector3 positionMean = Vector3.zero;
        float orientationX = 0;
        float orientationY = 0;
        float orientationZ = 0;
        float orientationW = 0;
        foreach (Vector3 position in positions)
        {
            positionMean += position;
        }
        positionMean /= positions.Count;
        foreach (Quaternion quaternion in orientations)
        {
            orientationX += quaternion.x;
            orientationY += quaternion.y;
            orientationZ += quaternion.z;
            orientationW += quaternion.w;
        }
        orientationX /= orientations.Count;
        orientationY /= orientations.Count;
        orientationZ /= orientations.Count;
        orientationW /= orientations.Count;
        Quaternion orientationMean = new Quaternion(orientationX, orientationY, orientationZ, orientationW);

        // Calculate standard deviations
        float sigmaPosition = 0;
        foreach (Vector3 position in positions)
        {
            sigmaPosition += Vector3.SqrMagnitude(position - positionMean);
        }
        sigmaPosition /= positions.Count;
        sigmaPosition = Mathf.Sqrt(sigmaPosition);
        float sigmaX = 0;
        float sigmaY = 0;
        float sigmaZ = 0;
        float sigmaW = 0;
        foreach (Quaternion quaternion in orientations)
        {
            sigmaX += Mathf.Pow(quaternion.x - orientationMean.x, 2);
            sigmaY += Mathf.Pow(quaternion.y - orientationMean.y, 2);
            sigmaZ += Mathf.Pow(quaternion.z - orientationMean.z, 2);
            sigmaW += Mathf.Pow(quaternion.w - orientationMean.w, 2);
        }
        sigmaX /= orientations.Count;
        sigmaY /= orientations.Count;
        sigmaZ /= orientations.Count;
        sigmaW /= orientations.Count;
        sigmaX = Mathf.Sqrt(sigmaX);
        sigmaY = Mathf.Sqrt(sigmaY);
        sigmaZ = Mathf.Sqrt(sigmaZ);
        sigmaW = Mathf.Sqrt(sigmaW);
        float sigmaOrientation = (sigmaX + sigmaY + sigmaZ + sigmaW) / 4f;
        positionStdText.text = "Pos Std Dev: " + sigmaPosition;
        orientationStdText.text = "Orientation Std Dev: " + sigmaOrientation;

    }

    public void CalculatePointerPrecision()
    {
        StopAllCoroutines();
        StartCoroutine(CalculatePointerPrecisionRoutine());
    }


    public IEnumerator CalculatePointerPrecisionRoutine()
    {
        pointerCounter.timesClicked = 0;
        float counter = 20;
        pointerCounter.counting = true;
        while (counter > 0)
        {
            timerText.text = "Time Remaining: " + Mathf.RoundToInt(counter);
            counter -= Time.deltaTime;
            yield return new WaitForEndOfFrame();
        }
        pointerCounter.counting = false;
        accuracyText.text = "Accuracy: " + pointerCounter.timesClicked / 20f * 100f + " %";
    }
    

    public void ToggleHorizontalRuler(bool toggle)
    {
        horizontalRuler.gameObject.SetActive(toggle);
    }

    public void ToggleVerticalRuler(bool toggle)
    {
        verticalRuler.gameObject.SetActive(toggle);
    }

    public void ToggleSpacialRuler(bool toggle)
    {
        spacialRuler.gameObject.SetActive(toggle);
    }

    public void TogglePointerCounter(bool toggle)
    {
        pointerCounter.gameObject.SetActive(toggle);
    }

    public void ToggleConverganceObject(bool toggle)
    {
        converganceObject.gameObject.SetActive(toggle);
    }

    public void ToggleMirror(bool toggle)
    {
        mirror.gameObject.SetActive(toggle);
    }
}
