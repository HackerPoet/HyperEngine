using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Camera))]
public class MapCamera : MonoBehaviour {
    public bool squareAspectRatio = true;
    private Camera cam;
    private Player player;

	void Start() {
        //Get the camera component and update aspect ratio
        cam = GetComponent<Camera>();
        player = FindObjectOfType<Player>();
        if (squareAspectRatio) {
            cam.aspect = 1.0f;
        }

        //Replace the shader with a Euclidean camera axis
        if (HM.K > 0.0f) {
            cam.SetReplacementShader(Shader.Find("Custom/S2xEShader"), "HyperRenderType");
        } else if (HM.K < 0.0f) {
            cam.SetReplacementShader(Shader.Find("Custom/H2xEShader"), "HyperRenderType");
        } else {
            cam.SetReplacementShader(Shader.Find("Custom/E2xEShader"), "HyperRenderType");
        }
    }

    void Update() {
        //This equation keeps the camera zoomed and centered well in all projections
        if (player != null) {
            if (HM.K == 0.0f) {
                cam.orthographicSize = 2.0f;
            } else {
                cam.orthographicSize = 0.6f + 0.5f * (player.projInterp - 1.0f) * (player.projInterp - 1.0f);
            }
        }
    }
}
