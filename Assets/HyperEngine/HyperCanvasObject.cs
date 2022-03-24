using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Assertions;

public class HyperCanvasObject : MonoBehaviour {
    private Renderer[] hyperRenderers;
    private CanvasRenderer[] hyperCanvasRenderers;
    private MaterialPropertyBlock propBlock;

    private static readonly int hyperRotID = Shader.PropertyToID("_HyperRot");
    private static readonly int hyperTileRotID = Shader.PropertyToID("_HyperTileRot");
    private static readonly int hyperMapRotID = Shader.PropertyToID("_HyperMapRot");
    private static readonly int tanKHeightID = Shader.PropertyToID("_TanKHeight");

    public GyroVector localGV = GyroVector.identity;
    [HideInInspector] public GyroVector composedGV = GyroVector.identity;
    private GyroVector mapGV = GyroVector.identity;
    public bool useTanKHeight = true;

    void Awake() {
        propBlock = new MaterialPropertyBlock();
    }

    void Start() {
        //Make sure all objects created during awake are available before getting components
        hyperRenderers = null;
        hyperCanvasRenderers = null;
        AddChildObject(gameObject);
    }

    public void AddChildObject(GameObject obj) {
        //Disable dynamic occlusion
        Renderer[] newHyperRenderers = obj.GetComponentsInChildren<Renderer>(true);
        CanvasRenderer[] newHyperCanvasRenderers = obj.GetComponentsInChildren<CanvasRenderer>(true);
        foreach (Renderer hyperRenderer in newHyperRenderers) {
            hyperRenderer.allowOcclusionWhenDynamic = false;
        }
        foreach (CanvasRenderer hyperCanvasRenderer in newHyperCanvasRenderers) {
            hyperCanvasRenderer.cull = false;
            hyperCanvasRenderer.cullTransparentMesh = false;
        }

        //Merge the arrays
        if (hyperRenderers == null) {
            hyperRenderers = newHyperRenderers;
            hyperCanvasRenderers = newHyperCanvasRenderers;
        } else {
            Array.Resize(ref hyperRenderers, hyperRenderers.Length + newHyperRenderers.Length);
            Array.Resize(ref hyperCanvasRenderers, hyperCanvasRenderers.Length + newHyperCanvasRenderers.Length);
            Array.Copy(newHyperRenderers, 0, hyperRenderers, hyperRenderers.Length - newHyperRenderers.Length, newHyperRenderers.Length);
            Array.Copy(newHyperCanvasRenderers, 0, hyperCanvasRenderers, hyperCanvasRenderers.Length - newHyperCanvasRenderers.Length, newHyperCanvasRenderers.Length);
        }
    }

    void LateUpdate() {
        //Calculate the hyper-rotation from the player's point of view
        composedGV = localGV + HyperObject.worldGV;
        mapGV = localGV + HyperObject.worldGV.ProjectToPlane();

        //Update shader and canvas material properties
        Matrix4x4 composedMat4 = composedGV.ToMatrix();
        Matrix4x4 localMat4 = localGV.ToMatrix();
        Matrix4x4 mapMat4 = mapGV.ToMatrix();
        foreach (Renderer hyperRenderer in hyperRenderers) {
            hyperRenderer.enabled = true;
            hyperRenderer.GetPropertyBlock(propBlock);
            propBlock.SetMatrix(hyperRotID, composedMat4);
            propBlock.SetMatrix(hyperTileRotID, localMat4);
            propBlock.SetMatrix(hyperMapRotID, mapMat4);
            propBlock.SetFloat(tanKHeightID, useTanKHeight ? 1.0f : 0.0f);
            hyperRenderer.SetPropertyBlock(propBlock);
        }
        foreach (CanvasRenderer hyperCanvasRenderer in hyperCanvasRenderers) {
            Material material = hyperCanvasRenderer.GetMaterial();
            if (!material) { continue; }
            material.SetMatrix(hyperRotID, composedMat4);
            material.SetMatrix(hyperTileRotID, localMat4);
            material.SetMatrix(hyperMapRotID, mapMat4);
            material.SetFloat(tanKHeightID, useTanKHeight ? 1.0f : 0.0f);
        }
    }
}
