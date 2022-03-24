using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Assertions;

[DisallowMultipleComponent]
public class EuclidObject : MonoBehaviour {
    private static readonly int hyperRotID = Shader.PropertyToID("_HyperRot");
    private static readonly int hyperMapRotID = Shader.PropertyToID("_HyperMapRot");
    private static readonly int tanKHeightID = Shader.PropertyToID("_TanKHeight");

    private MaterialPropertyBlock propBlock = null;

    void Awake() {
        if (propBlock == null) { propBlock = new MaterialPropertyBlock(); }
        Renderer renderer = GetComponent<Renderer>();
        renderer.allowOcclusionWhenDynamic = false;
        renderer.GetPropertyBlock(propBlock);
        propBlock.SetFloat(tanKHeightID, 0.0f);
        propBlock.SetMatrix(hyperRotID, Matrix4x4.identity);
        propBlock.SetMatrix(hyperMapRotID, Matrix4x4.identity);
        renderer.SetPropertyBlock(propBlock);
    }
}