using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using UnityEngine.XR;

/// <summary>
/// Attach this script to an empty gameObject. Create a secondary camera 
/// gameObject for offscreen rendering (not your main camera) and connect it 
/// with this script. Offscreen camera should have a texture object attached to it.  
/// OffscreenCamera texture object is used for rendering (please see camera properties). 
/// </summary> 
public class OffscreenRendering : MonoBehaviour
{
	#region public members 	
	/// <summary> 	
	/// The desired number of screenshots per second. 	
	/// </summary> 	
	[Tooltip("Number of screenshots per second.")]
	public int ScreenshotsPerSecond = 19;
	/// <summary> 	
	/// Camera used to render screen to texture. Offscreen camera 	
	/// with desired target texture size should be attached here, 	
	/// not the main camera. 	
	/// </summary> 	
	[Tooltip("The left camera that is used for off-screen rendering.")]
	public Camera OffscreenCameraLeft;
	public LineRenderer vALeft;
	public LineRenderer vBLeft;
	public LineRenderer vCLeft;

	[Tooltip("The right camera that is used for off-screen rendering.")]
	public Camera OffscreenCameraRight;
	public LineRenderer vARight;
	public LineRenderer vBRight;
	public LineRenderer vCRight;

	[Tooltip("The left eye camera that is used for off-screen rendering.")]
	public Camera OffscreenEyeLeft;

	[Tooltip("The right eye camera that is used for off-screen rendering.")]
	public Camera OffscreenEyeRight;

	public Renderer groundPlane;
	public Renderer leftPlane;
	public Renderer rightPlane;

	public GameObject sphere;

	public Material bottomPlaneMaterial;
	public Material leftPlaneMaterial;
	public Material rightPlaneMaterial;

	public RenderTexture leftPlaneLeftTexture;
	public RenderTexture rightPlaneLeftTexture;
	public RenderTexture bottomPlaneLeftTexture;
	public RenderTexture leftPlaneRightTexture;
	public RenderTexture rightPlaneRightTexture;
	public RenderTexture bottomPlaneRightTexture;

	private bool controller = false;
	private bool replaceProj = false;
	private bool freeze = false;
	private bool debug = false;
	private int FrameCounter;

    #endregion
    /// <summary> 	
    /// Keep track of saved frames. 	
    /// counter is added as postifx to file names. 	
    /// </summary> 	private int FrameCounter = 0;  	

    // Use this for initialization 	
    void Start()
	{
		StartCoroutine("CaptureAndSaveFrames");
	}

	void Update()
	{
		if (OVRInput.Get(OVRInput.Button.PrimaryIndexTrigger) || OVRInput.Get(OVRInput.Button.SecondaryIndexTrigger))
		{
			controller = true;
		} else controller = false;
		if (OVRInput.Get(OVRInput.Button.One))
		{
			debug = true;
		} else debug = false;
		if (OVRInput.GetDown(OVRInput.Button.Two))
		{
			freeze = !freeze;
		}
		if (OVRInput.GetDown(OVRInput.Button.PrimaryThumbstick) || OVRInput.GetDown(OVRInput.Button.SecondaryThumbstick))
        {
			Debug.Log("D");
			replaceProj = !replaceProj;
        }
	}

	enum PlaneType
    {
		Right, Left, Ground
    }

	Renderer GetLeftPlane(out Vector3 pA, out Vector3 pB, out Vector3 pC, out Vector3 n)
	{
		Vector3 lMin = leftPlane.bounds.min;
		Vector3 lMax = leftPlane.bounds.max;
		pA = lMin;
		pB = new Vector3(lMax.x, lMin.y, lMax.z);
		pC = new Vector3(lMin.x, lMax.y, lMin.z);
		n = -leftPlane.transform.up;
		return leftPlane;
	}

	Renderer GetRightPlane(out Vector3 pA, out Vector3 pB, out Vector3 pC, out Vector3 n)
	{
		Vector3 rMin = rightPlane.bounds.min;
		Vector3 rMax = rightPlane.bounds.max;
		pA = rMin;
		pB = new Vector3(rMax.x, rMin.y, rMin.z);
		pC = new Vector3(rMin.x, rMax.y, rMax.z);
		n = -rightPlane.transform.up;
		return rightPlane;
	}

	Renderer GetGroundPlane(out Vector3 pA, out Vector3 pB, out Vector3 pC, out Vector3 n)
    {
		Vector3 gMin = groundPlane.bounds.min;
		Vector3 gMax = groundPlane.bounds.max;
		pA = gMin;
		pB = new Vector3(gMax.x, gMax.y, gMin.z);
		pC = new Vector3(gMin.x, gMin.y, gMax.z);
		n = -groundPlane.transform.up;
		return groundPlane;
	}

	void SetProjection(Camera eye, PlaneType type)
	{
		Renderer plane = rightPlane;
		Vector3 pA = Vector3.zero;
		Vector3 pB = Vector3.zero;
		Vector3 pC = Vector3.zero;
		Vector3 n = Vector3.up;
        switch (type)
        {
            case PlaneType.Left:
				plane = GetLeftPlane(out pA, out pB, out pC, out n);
                break;
            case PlaneType.Ground:
				plane = GetGroundPlane(out pA, out pB, out pC, out n);
                break;
			case PlaneType.Right:
			default:
				plane = GetRightPlane(out pA, out pB, out pC, out n);
				break;
		}

		Instantiate(sphere, pA, Quaternion.identity);
		Instantiate(sphere, pB, Quaternion.identity);
		Instantiate(sphere, pC, Quaternion.identity);

		Vector3 vA = pA - eye.transform.position;
		Vector3 vB = pB - eye.transform.position;
		Vector3 vC = pC - eye.transform.position;

		Vector3 vR = Vector3.Normalize(pB - pA);//-plane.transform.right;//
		Vector3 vU = Vector3.Normalize(pC - pA);//-plane.transform.forward;//
		Vector3 vN = Vector3.Normalize(Vector3.Cross(vU, vR));//-plane.transform.up;// // should be vU cross vR
		float d = -Vector3.Dot(vN, vA);
		float l = Vector3.Dot(vR, vA) * eye.nearClipPlane / d;
		float r = Vector3.Dot(vR, vB) * eye.nearClipPlane / d;
		float t = Vector3.Dot(vU, vC) * eye.nearClipPlane / d;
		float b = Vector3.Dot(vU, vA) * eye.nearClipPlane / d;
		// Projection Matrix P
		Matrix4x4 P = Matrix4x4.Frustum(l, r, b, t, eye.nearClipPlane, eye.farClipPlane);
		// Rotation Matrix M^T
		Matrix4x4 mT = new Matrix4x4(new Vector4(vR.x, vU.x, vN.x), new Vector4(vR.y, vU.y, vN.y), new Vector4(vR.z, vU.z, vN.z), new Vector4(0, 0, 0, 1));
		mT = mT.transpose;
		// Translation Matrix T
		Matrix4x4 T = new Matrix4x4(new Vector4(1, 0), new Vector4(0, 1), new Vector4(0, 0, 1), new Vector4(-eye.transform.position.x, -eye.transform.position.y, -eye.transform.position.z, 1));
		// new Projection Matrix P'
		Matrix4x4 pPrime = P * mT * T;
		//eye.projectionMatrix = pPrime;
		eye.projectionMatrix = P;
		eye.transform.rotation = Quaternion.LookRotation(-vN, vU);
		if (controller && debug)
		{
			vALeft.enabled = true;
			vARight.enabled = true;
			vBLeft.enabled = true;
			vBRight.enabled = true;
			vCLeft.enabled = true;
			vCRight.enabled = true;
			if (eye == OffscreenCameraLeft)
			{
				vALeft.SetPosition(1, vA);
				vBLeft.SetPosition(1, vB);
				vCLeft.SetPosition(1, vC);
				//Debug.DrawRay(eye.transform.position + eye.transform.forward * eye.nearClipPlane, vA, Color.green, 1);
				//Debug.DrawRay(eye.transform.position + eye.transform.forward * eye.nearClipPlane, vB, Color.green, 1);
				//Debug.DrawRay(eye.transform.position + eye.transform.forward * eye.nearClipPlane, vC, Color.green, 1);
			}
			else if (eye == OffscreenCameraRight)
			{
				vARight.SetPosition(1, vA);
				vBRight.SetPosition(1, vB);
				vCRight.SetPosition(1, vC);
				//Debug.DrawRay(eye.transform.position + eye.transform.forward * eye.nearClipPlane, vA, Color.red, 1);
				//Debug.DrawRay(eye.transform.position + eye.transform.forward * eye.nearClipPlane, vB, Color.red, 1);
				//Debug.DrawRay(eye.transform.position + eye.transform.forward * eye.nearClipPlane, vC, Color.red, 1);
			}
		}
		else
        {
			vALeft.enabled = false;
			vARight.enabled = false;
			vBLeft.enabled = false;
			vBRight.enabled = false;
			vCLeft.enabled = false;
			vCRight.enabled = false;
        }
		
		//Debug.DrawRay(plane.transform.position, vU, Color.green, 1);
		//Debug.DrawRay(plane.transform.position, vR, Color.red, 1);
		//Debug.DrawRay(plane.transform.position, vN, Color.blue, 1);
	}

    /// <summary>     
    /// Captures x frames per second.      
    /// </summary>     
    /// <returns>Enumerator object</returns>     
    IEnumerator CaptureAndSaveFrames()
	{
		while (true)
		{
			yield return new WaitForEndOfFrame();
			if (!freeze)
            {
				OffscreenEyeLeft.enabled = true;
				OffscreenEyeRight.enabled = true;
				OffscreenCameraLeft.enabled = true;
				OffscreenCameraRight.enabled = true;

				if (controller)
				{
					rightPlane.material = leftPlaneMaterial;
				}
				else rightPlane.material = rightPlaneMaterial;

				// Remember currently active render texture. 			
				RenderTexture currentRT = RenderTexture.active;

				if (!controller)
				{
					// Set target texture as active render texture. 			
					RenderTexture.active = rightPlaneLeftTexture; //OffscreenEyeLeft.targetTexture;
					if (replaceProj)
					{
						SetProjection(OffscreenEyeLeft, PlaneType.Right);
					}
					else OffscreenEyeLeft.ResetProjectionMatrix();
					// Render to texture
					OffscreenEyeLeft.Render();
					// Read offscreen texture 			
					Texture2D offscreenTextureLeft = new Texture2D(OffscreenEyeLeft.targetTexture.width, OffscreenEyeLeft.targetTexture.height, TextureFormat.RGB24, false);
					offscreenTextureLeft.ReadPixels(new Rect(0, 0, OffscreenEyeLeft.targetTexture.width, OffscreenEyeLeft.targetTexture.height), 0, 0, false);
					offscreenTextureLeft.Apply();

					

					RenderTexture.active = rightPlaneRightTexture; //OffscreenEyeRight.targetTexture;
					if (replaceProj)
					{
						SetProjection(OffscreenEyeRight, PlaneType.Right);
					}
					else OffscreenEyeRight.ResetProjectionMatrix();
					// Render to texture 			
					OffscreenEyeRight.Render();
					// Read offscreen texture 			
					Texture2D offscreenTextureRight = new Texture2D(OffscreenEyeRight.targetTexture.width, OffscreenEyeRight.targetTexture.height, TextureFormat.RGB24, false);
					offscreenTextureRight.ReadPixels(new Rect(0, 0, OffscreenEyeRight.targetTexture.width, OffscreenEyeRight.targetTexture.height), 0, 0, false);
					offscreenTextureRight.Apply();

					// Reset previous render texture. 			
					RenderTexture.active = currentRT;
					++FrameCounter;

					// Encode texture into PNG 			
					//byte[] bytes = offscreenTexture.EncodeToPNG();
					//File.WriteAllBytes(Application.dataPath + "/../capturedframe" + FrameCounter.ToString() + ".png", bytes);

					// Delete textures. 			
					UnityEngine.Object.Destroy(offscreenTextureLeft);
					UnityEngine.Object.Destroy(offscreenTextureRight);
				}
				else
				{
					


					// Set target texture as active render texture. 			
					RenderTexture.active = rightPlaneLeftTexture; //OffscreenEyeLeft.targetTexture;
					if (replaceProj)
					{
						SetProjection(OffscreenCameraLeft, PlaneType.Right);
					}
					else OffscreenCameraLeft.ResetProjectionMatrix();
					// Render to texture 			
					OffscreenCameraLeft.Render();
					// Read offscreen texture 			
					Texture2D offscreenTextureLeft = new Texture2D(OffscreenCameraLeft.targetTexture.width, OffscreenCameraLeft.targetTexture.height, TextureFormat.RGB24, false);
					offscreenTextureLeft.ReadPixels(new Rect(0, 0, OffscreenCameraLeft.targetTexture.width, OffscreenCameraLeft.targetTexture.height), 0, 0, false);
					offscreenTextureLeft.Apply();

					

					RenderTexture.active = rightPlaneLeftTexture; //OffscreenCameraRight.targetTexture;
					if (replaceProj)
					{
						SetProjection(OffscreenCameraRight, PlaneType.Right);
					}
					else OffscreenCameraRight.ResetProjectionMatrix();
					// Render to texture 			
					OffscreenCameraRight.Render();
					// Read offscreen texture 			
					Texture2D offscreenTextureRight = new Texture2D(OffscreenCameraRight.targetTexture.width, OffscreenCameraRight.targetTexture.height, TextureFormat.RGB24, false);
					offscreenTextureRight.ReadPixels(new Rect(0, 0, OffscreenCameraRight.targetTexture.width, OffscreenCameraRight.targetTexture.height), 0, 0, false);
					offscreenTextureRight.Apply();

					// Reset previous render texture. 			
					RenderTexture.active = currentRT;
					++FrameCounter;

					// Encode texture into PNG 			
					//byte[] bytes = offscreenTexture.EncodeToPNG();
					//File.WriteAllBytes(Application.dataPath + "/../capturedframe" + FrameCounter.ToString() + ".png", bytes);

					// Delete textures. 			
					UnityEngine.Object.Destroy(offscreenTextureLeft);
					UnityEngine.Object.Destroy(offscreenTextureRight);
				}
			}
			else
            {
				OffscreenEyeLeft.enabled = false;
				OffscreenEyeRight.enabled = false;
				OffscreenCameraLeft.enabled = false;
				OffscreenCameraRight.enabled = false;
			}
			

			yield return new WaitForSeconds(1.0f / ScreenshotsPerSecond);
		}
	}

	/// <summary>     
	/// Stop image capture.     
	/// </summary>     
	public void StopCapturing()
	{
		StopCoroutine("CaptureAndSaveFrames");
		FrameCounter = 0;
	}

	/// <summary> 	
	/// Resume image capture. 	
	/// </summary> 	
	public void ResumeCapturing()
	{
		StartCoroutine("CaptureAndSaveFrames");
	}
}
