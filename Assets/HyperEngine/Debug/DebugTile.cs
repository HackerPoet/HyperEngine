using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DebugTile : MonoBehaviour {
    public GameObject letterPrefab;
    public Texture2D textureL;
    public Texture2D textureR;
    public Texture2D textureU;
    public Texture2D textureD;
    [System.NonSerialized] public string coord;

    Texture2D GetTex(char c) {
        switch (c) {
            case 'L': return textureL;
            case 'R': return textureR;
            case 'U': return textureU;
            case 'D': return textureD;
            default: return null;
        }
    }

    void Start () {
        coord = gameObject.name.Substring(5);
        float offsetX = 0.5f * coord.Length;
        for (int i = 0; i < coord.Length; ++i) {
            Texture2D curTex = GetTex(coord[i]);
            GameObject letterObj = Instantiate(letterPrefab, transform);
            float x = (i - offsetX) * letterObj.transform.localScale.x;
            letterObj.transform.Translate(x, 0.0f, 0.0f);
            letterObj.GetComponent<HyperObject>().localGV = GetComponent<HyperObject>().localGV;
            letterObj.transform.parent = gameObject.transform;
            SetTextures letterST = letterObj.GetComponent<SetTextures>();
            Renderer letterRenderer = letterObj.GetComponent<Renderer>();
            letterST.texture = curTex;
            letterST.UpdateTextures();
        }
    }
}
