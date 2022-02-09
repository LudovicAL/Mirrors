using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class RenderedTextureMirror : MonoBehaviour {

	[Tooltip("Subtracted from the near plane of the mirror")]
	public float clipPlaneOffset = 0.07f;

	[Tooltip("Far clip plane for mirror camera")]
	public float farClipPlane = 1000.0f;

	[Tooltip("What layers will be reflected?")]
	public LayerMask reflectLayers = -1;

	private Camera playerCamera;
	private Camera mirrorCamera;
	private RenderTexture reflectionTexture;
	private MeshRenderer mirrorMeshRenderer;
	private Matrix4x4 reflectionMatrix;

	private void Awake() {
		playerCamera = Camera.main;
		mirrorCamera = GetComponentInChildren<Camera>();
		mirrorCamera.enabled = false;
		mirrorMeshRenderer = GetComponent<MeshRenderer>();
	}

	private void Start() {
		UpdateCameraProperties(playerCamera, mirrorCamera);
		CreateRenderTexture();
	}

	private void UpdateCameraProperties(Camera src, Camera dest) {
		dest.clearFlags = src.clearFlags;
		dest.backgroundColor = src.backgroundColor;
		if (src.clearFlags == CameraClearFlags.Skybox) {
			Skybox sky = src.GetComponent<Skybox>();
			Skybox mysky = dest.GetComponent<Skybox>();
			if (!sky || !sky.material) {
				mysky.enabled = false;
			} else {
				mysky.enabled = true;
				mysky.material = sky.material;
			}
		}
		dest.orthographic = src.orthographic;
		dest.orthographicSize = src.orthographicSize;
		dest.aspect = src.aspect;
		dest.renderingPath = src.renderingPath;
	}

	private void CreateRenderTexture() {
		if (reflectionTexture == null || reflectionTexture.width != Screen.width || reflectionTexture.height != Screen.height) {
			if (reflectionTexture != null) {
				reflectionTexture.Release();
			}
			reflectionTexture = new RenderTexture(Screen.width, Screen.height, 16);
			reflectionTexture.filterMode = FilterMode.Bilinear;
			reflectionTexture.antiAliasing = 1;
			reflectionTexture.name = "MirrorRenderTexture_" + GetInstanceID();
			reflectionTexture.hideFlags = HideFlags.HideAndDontSave;
			reflectionTexture.autoGenerateMips = false;
			reflectionTexture.wrapMode = TextureWrapMode.Clamp;
			mirrorMeshRenderer.material.SetTexture("_MainTex", reflectionTexture);
		}
		if (mirrorCamera.targetTexture != reflectionTexture) {
			mirrorCamera.targetTexture = reflectionTexture;
		}
	}

	public void Render(ScriptableRenderContext context) {
		if (MirrorIsVisible()) {
			Vector3 pos = transform.position;
			Vector3 normal = transform.forward;

			// Reflect camera around reflection plane
			float d = -Vector3.Dot(normal, pos) - clipPlaneOffset;
			Vector4 reflectionPlane = new Vector4(normal.x, normal.y, normal.z, d);
			CalculateReflectionMatrix(ref reflectionPlane);
			Vector3 oldpos = mirrorCamera.transform.position;
			float oldclip = mirrorCamera.farClipPlane;
			Vector3 newpos = reflectionMatrix.MultiplyPoint(oldpos);

			Matrix4x4 worldToCameraMatrix = playerCamera.worldToCameraMatrix;

			worldToCameraMatrix *= reflectionMatrix;
			mirrorCamera.worldToCameraMatrix = worldToCameraMatrix;

			// Clip out background
			Vector4 clipPlane = CameraSpacePlane(ref worldToCameraMatrix, ref pos, ref normal, 1.0f);
			mirrorCamera.projectionMatrix = playerCamera.CalculateObliqueMatrix(clipPlane);
			GL.invertCulling = true;
			mirrorCamera.transform.position = newpos;
			mirrorCamera.farClipPlane = farClipPlane;
			mirrorCamera.cullingMask = ~(1 << 4) & reflectLayers.value;
			UniversalRenderPipeline.RenderSingleCamera(context, mirrorCamera);
			mirrorCamera.transform.position = oldpos;
			mirrorCamera.farClipPlane = oldclip;
			GL.invertCulling = false;
		}
	}

	private bool MirrorIsVisible() {
		return CameraUtility.VisibleFromCamera(mirrorMeshRenderer, playerCamera);
	}

	private void CalculateReflectionMatrix(ref Vector4 plane) {
		// Calculates reflection matrix around the given plane

		reflectionMatrix.m00 = (1F - 2F * plane[0] * plane[0]);
		reflectionMatrix.m01 = (-2F * plane[0] * plane[1]);
		reflectionMatrix.m02 = (-2F * plane[0] * plane[2]);
		reflectionMatrix.m03 = (-2F * plane[3] * plane[0]);

		reflectionMatrix.m10 = (-2F * plane[1] * plane[0]);
		reflectionMatrix.m11 = (1F - 2F * plane[1] * plane[1]);
		reflectionMatrix.m12 = (-2F * plane[1] * plane[2]);
		reflectionMatrix.m13 = (-2F * plane[3] * plane[1]);

		reflectionMatrix.m20 = (-2F * plane[2] * plane[0]);
		reflectionMatrix.m21 = (-2F * plane[2] * plane[1]);
		reflectionMatrix.m22 = (1F - 2F * plane[2] * plane[2]);
		reflectionMatrix.m23 = (-2F * plane[3] * plane[2]);

		reflectionMatrix.m30 = 0F;
		reflectionMatrix.m31 = 0F;
		reflectionMatrix.m32 = 0F;
		reflectionMatrix.m33 = 1F;
	}

	private Vector4 CameraSpacePlane(ref Matrix4x4 worldToCameraMatrix, ref Vector3 pos, ref Vector3 normal, float sideSign) {
		Vector3 offsetPos = pos + normal * clipPlaneOffset;
		Vector3 cpos = worldToCameraMatrix.MultiplyPoint(offsetPos);
		Vector3 cnormal = worldToCameraMatrix.MultiplyVector(normal).normalized * sideSign;
		return new Vector4(cnormal.x, cnormal.y, cnormal.z, -Vector3.Dot(cpos, cnormal));
	}
}
