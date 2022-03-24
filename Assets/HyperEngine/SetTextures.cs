using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;

public class SetTextures : MonoBehaviour {
    private Renderer meshRenderer;
    private MaterialPropertyBlock propBlock;
    private static readonly int textureID = Shader.PropertyToID("_MainTex");
    private static readonly int stID = Shader.PropertyToID("_MainTex_ST");
    private static readonly int aomapID = Shader.PropertyToID("_AOTex");
    private static readonly int boundaryAOID = Shader.PropertyToID("_BoundaryAO");
    private static readonly int ambientID = Shader.PropertyToID("_Ambient");
    private static readonly int colorID = Shader.PropertyToID("_Color");
    private static readonly int noiseID = Shader.PropertyToID("_Noise");
    private static readonly int specularID = Shader.PropertyToID("_Specular");

    public Texture2D texture;
    public Texture2D aomap;
    [Range(0.0f, 1.0f)] public float ambient = 0.6f;
    [Range(0.0f, 1.0f)] public float specular = 0.0f;
    [Range(0.0f, 80.0f)] public float shininess = 0.0f;
    public Color colorize = Color.white;
    public Color noise = Color.clear;
    public float overrideBoundaryAO = -1.0f;

    private static Hashtable dynamicMaterials = new Hashtable();

    private void Awake() {
        meshRenderer = GetComponent<Renderer>();
        Debug.Assert(meshRenderer != null, "SetTextures can only be applied to a material with a renderer");
        Debug.Assert(meshRenderer.sharedMaterial != null, "Invalid material in object " + gameObject.name);

        propBlock = new MaterialPropertyBlock();

        if (texture == null) { texture = Texture2D.whiteTexture; }
        if (aomap == null) { aomap = Texture2D.whiteTexture; }
    }

    private void Start() {
        //World builder needs to update globalFog and globalBounryAO during Awake.
        //So this step must happen later on Start.
        UpdateTextures();
    }

    public string GetMaterialName() {
        string materialName = meshRenderer.sharedMaterial.name;
        int lastIndexOfDS = materialName.LastIndexOf('$');
        if (lastIndexOfDS >= 0) {
            return materialName.Substring(lastIndexOfDS + 1);
        } else {
            return materialName;
        }
    }

    public void UpdateTextures() {
        if (propBlock != null) {
            //Create a unique material for each texture combination
            //This is because GPU instancing doesn't support instanced textures
            string materialKey = texture.name + "$" + aomap.name + "$" + GetMaterialName();
            Material material = dynamicMaterials[materialKey] as Material;
            if (material == null) {
                material = new Material(meshRenderer.sharedMaterial);
                material.name = materialKey;
                material.SetTexture(textureID, texture);
                material.SetTexture(aomapID, aomap);
                dynamicMaterials[materialKey] = material;
            }
            meshRenderer.sharedMaterial = material;

            //Set the rest of the properties through the property blocks
            float boundaryAO = (overrideBoundaryAO >= 0.0f ? overrideBoundaryAO : WorldBuilder.globalBounryAO);
            meshRenderer.GetPropertyBlock(propBlock);
            propBlock.SetFloat(boundaryAOID, boundaryAO);
            propBlock.SetFloat(ambientID, ambient);
            propBlock.SetColor(colorID, colorize);
            propBlock.SetColor(noiseID, noise);
            propBlock.SetVector(specularID, new Vector2(specular, shininess));
            meshRenderer.SetPropertyBlock(propBlock);
        }
    }

    public void UpdateColorOnly(float newAlpha) {
        if (colorize.a != newAlpha) {
            colorize.a = newAlpha;
            UpdateColorOnly();
        }
    }

    public void UpdateColorOnly(Color newColor) {
        if (colorize != newColor) {
            colorize = newColor;
            UpdateColorOnly();
        }
    }

    public void UpdateColorOnly() {
        if (propBlock != null) {
            meshRenderer.GetPropertyBlock(propBlock);
            propBlock.SetColor(colorID, colorize);
            meshRenderer.SetPropertyBlock(propBlock);
        }
    }

    public void SetST(float sx, float sy, float dx, float dy) {
        if (propBlock != null) {
            meshRenderer.GetPropertyBlock(propBlock);
            propBlock.SetVector(stID, new Vector4(sx, sy, dx, dy));
            meshRenderer.SetPropertyBlock(propBlock);
        }
    }
}
