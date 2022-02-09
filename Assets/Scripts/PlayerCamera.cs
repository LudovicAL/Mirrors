using UnityEngine;
using UnityEngine.Rendering;

public class PlayerCamera : MonoBehaviour {

	RenderedTextureMirror[] renderedTextureMirrors;

	void Awake() {
		renderedTextureMirrors = FindObjectsOfType<RenderedTextureMirror>();
		RenderPipelineManager.beginCameraRendering += OnBeginFrameRendering;
	}

	void OnBeginFrameRendering(ScriptableRenderContext context, Camera cameras) {
		for (int i = 0; i < renderedTextureMirrors.Length; i++) {
			renderedTextureMirrors[i].Render(context);
		}
	}

	// Remove your callback from the delegate's invocation list
	void OnDestroy() {
		RenderPipelineManager.beginCameraRendering -= OnBeginFrameRendering;
	}
}
