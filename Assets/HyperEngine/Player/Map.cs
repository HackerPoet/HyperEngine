using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Map : MonoBehaviour {
    public const float LAG_HALF_LIFE = 0.04f; //seconds
    public const float DEGREES_TILT = 50.0f; //degrees
    public const float ANIM_SPEED = 2.0f;

    private Quaternion camRotation = Quaternion.identity;
    private float mapInterp = 0.0f;
    private static readonly int colorID = Shader.PropertyToID("_Color");
    private MaterialPropertyBlock propBlock;
    [System.NonSerialized] public MeshRenderer paperRenderer;
    private HyperObject ho;
    private bool takeMapOut = false;
    private Transform camTransform;
    [System.NonSerialized] public bool isVR = false;

    public GameObject mapCam;
    public GameObject paper;

    void Awake() {
        propBlock = new MaterialPropertyBlock();
        paperRenderer = paper.GetComponent<MeshRenderer>();
        transform.localRotation = Quaternion.AngleAxis(DEGREES_TILT, Vector3.right);
        ho = GetComponent<HyperObject>();
        camTransform = Camera.main.transform;
        Debug.Assert(camTransform != null);
    }

    void Update() {
        //Update interpolation animation
        if (takeMapOut) {
            mapInterp = Mathf.Min(mapInterp + Time.deltaTime * ANIM_SPEED, 1.0f);
        } else {
            mapInterp = Mathf.Max(mapInterp - Time.deltaTime * ANIM_SPEED, 0.0f);
        }

        if (!isVR) {
            //Animate the angle to hold the map when taking out
            float a = 1.0f - mapInterp;
            a *= a / (2 * a * (a - 1) + 1);
            Quaternion pickupAngle = Quaternion.AngleAxis(a * DEGREES_TILT, Vector3.right);

            //Always follow and orient to player
            float smooth_lerp = Mathf.Pow(2.0f, -Time.deltaTime / LAG_HALF_LIFE);
            Vector3D camOffset = new Vector3D(0.0, HyperObject.camHeight, 0.0);
            camRotation = Quaternion.Slerp(camTransform.localRotation, camRotation, smooth_lerp);
            transform.localRotation = camRotation * pickupAngle;
            ho.localGVD = camOffset - HyperObject.worldGVD;
        }

        //Update transparency
        paperRenderer.GetPropertyBlock(propBlock);
        propBlock.SetColor(colorID, new Vector4(1.0f, 1.0f, 1.0f, mapInterp * 0.9f));
        paperRenderer.SetPropertyBlock(propBlock);

        //Disable when the map is put away
        if (mapInterp == 0.0f) {
            gameObject.SetActive(false);
            mapCam.SetActive(false);
        }
    }

    public bool IsMapOut() {
        return takeMapOut;
    }

    public void TakeMapOut(bool takeOut) {
        //Toggle if map is shown
        takeMapOut = takeOut;
        if (takeOut) {
            gameObject.SetActive(true);
            mapCam.SetActive(true);
            camRotation = camTransform.localRotation;
        }
    }
}
