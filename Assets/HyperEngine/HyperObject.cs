using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Assertions;

[DisallowMultipleComponent]
public class HyperObject : MonoBehaviour {
    private static readonly int hyperRotID = Shader.PropertyToID("_HyperRot");
    private static readonly int hyperMapRotID = Shader.PropertyToID("_HyperMapRot");
    private static readonly int tanKHeightID = Shader.PropertyToID("_TanKHeight");
    private static readonly int camHeightID = Shader.PropertyToID("_CamHeight");

    private Renderer[] hyperRenderers = null;
    private WarpCollider[] warpColliders = null;
    private MaterialPropertyBlock propBlock = null;

    private static GyroVector _worldGV = GyroVector.identity;
    private static GyroVectorD _worldGVD = GyroVectorD.identity;
    public static GyroVector worldGV {
        get => _worldGV;
        set { _worldGV = value; _worldGVD = (GyroVectorD)value; }
    }
    public static GyroVectorD worldGVD {
        get => _worldGVD;
        set { _worldGVD = value; _worldGV = (GyroVector)value; }
    }
    private static GyroVector _shakeGV = GyroVector.identity;
    private static GyroVectorD _shakeGVD = GyroVectorD.identity;
    public static GyroVectorD shakeGVD {
        set { _shakeGVD = value; _shakeGV = (GyroVector)value; }
    }
    public static bool isShaking = false;

    public static Vector3 worldLook = Vector3.forward;
    public const float DRAW_DIST_SQ_FORWARD = 0.9975f;
    public const float DRAW_DIST_SQ_BACKWARD = 0.99f;
    public const float COLLIDER_DIST_SQ_UPDATE = 0.99f;
    public static float drawDistSqForward = DRAW_DIST_SQ_FORWARD;
    public static float drawDistSqBackward = DRAW_DIST_SQ_BACKWARD;
    public static float colliderDistSqUpdate = COLLIDER_DIST_SQ_UPDATE;
    public static bool updateColliders = true;

    private static float _camHeight = 0;
    public static float camHeight {
        get => _camHeight;
        set {
            Debug.Assert(!float.IsNaN(value));
            _camHeight = value;
            Shader.SetGlobalFloat(camHeightID, _camHeight);
        }
    }

    public GyroVector localGV = GyroVector.identity;
    [System.NonSerialized] public GyroVectorD localGVD;
    private bool hasComposedGV = false;
    private GyroVector _composedGV = GyroVector.identity;
    public GyroVector composedGV {
        get {
            if (!hasComposedGV) { UpdateComposedGV(); }
            return _composedGV;
        }
    }
    private GyroVector mapGV = GyroVector.identity;
    public bool useTanKHeight = true;
    public bool allowCulling = true;
    public bool highPrecision = false;

    void Awake() {
        if (highPrecision) { localGVD = (GyroVectorD)localGV; }
        if (propBlock == null) { propBlock = new MaterialPropertyBlock(); }
    }

    void Start() {
        //Make sure all objects created during awake are available before getting components.
        hyperRenderers = null;
        warpColliders = null;
        AddChildObject(gameObject);

        //Always disable culling with spherical geometry or large tile sizes.
        if (HM.K >= 0.0f || HM.N > 99) {
            allowCulling = false;
        }
    }

    public void AddChildObject(GameObject obj) {
        //This could be called before Awake, so make sure we have a valid propBlock.
        if (propBlock == null) { propBlock = new MaterialPropertyBlock(); }

        //Gather all the renderers and colliders.
        Renderer[] newHyperRenderers = obj.GetComponentsInChildren<Renderer>(true);
        WarpCollider[] newWarpColliders = obj.GetComponentsInChildren<WarpCollider>(true);
        foreach (Renderer hyperRenderer in newHyperRenderers) {
            hyperRenderer.allowOcclusionWhenDynamic = false;
            hyperRenderer.GetPropertyBlock(propBlock);
            propBlock.SetFloat(tanKHeightID, useTanKHeight ? 1.0f : 0.0f);
            hyperRenderer.SetPropertyBlock(propBlock);
        }

        //Merge the arrays.
        if (hyperRenderers == null) {
            hyperRenderers = newHyperRenderers;
            warpColliders = newWarpColliders;
        } else {
            Array.Resize(ref hyperRenderers, hyperRenderers.Length + newHyperRenderers.Length);
            Array.Resize(ref warpColliders, warpColliders.Length + newWarpColliders.Length);
            Array.Copy(newHyperRenderers, 0, hyperRenderers, hyperRenderers.Length - newHyperRenderers.Length, newHyperRenderers.Length);
            Array.Copy(newWarpColliders, 0, warpColliders, warpColliders.Length - newWarpColliders.Length, newWarpColliders.Length);
        }
    }

    private static bool IsBehindView(GyroVector gv) {
        return Vector3.Dot(gv.Point(), worldLook) < 1e-4f;
    }

    public void UpdateComposedGV() {
        if (highPrecision) {
            if (isShaking) {
                _composedGV = (GyroVector)(localGVD + _shakeGVD);
            } else {
                _composedGV = (GyroVector)(localGVD + _worldGVD);
            }
        } else {
            if (isShaking) {
                _composedGV = localGV + _shakeGV;
            } else {
                _composedGV = localGV + _worldGV;
            }
        }
        hasComposedGV = true;
    }

    void LateUpdate() {
        //Calculate the hyper-rotation from the player's point of view.
        UpdateComposedGV();

        //Reset colliders
        foreach (WarpCollider warpCollider in warpColliders) {
            warpCollider.ResetActive();
        }

        //Calculate if the center of the object is really far away.
        float dist2 = _composedGV.vec.sqrMagnitude;

        //Check how far away tiles are and don't draw ones that are too far to be seen.
        if (allowCulling && ((dist2 > drawDistSqForward) || (dist2 > drawDistSqBackward && IsBehindView(_composedGV)))) {
            foreach (Renderer hyperRenderer in hyperRenderers) {
                hyperRenderer.enabled = false;
            }
        } else {
            //Calculate the projected map rotation only when the object is actually rendering.
            if (highPrecision) {
                if (isShaking) {
                    mapGV = (GyroVector)localGVD + _shakeGV.ProjectToPlane();
                } else {
                    mapGV = (GyroVector)localGVD + _worldGV.ProjectToPlane();
                }
            } else {
                if (isShaking) {
                    mapGV = localGV + _shakeGV.ProjectToPlane();
                } else {
                    mapGV = localGV + _worldGV.ProjectToPlane();
                }
            }

            //Update shader properties.
            Matrix4x4 composedMat4 = _composedGV.ToMatrix();
            Matrix4x4 mapMat4 = mapGV.ToMatrix();
            foreach (Renderer hyperRenderer in hyperRenderers) {
                hyperRenderer.enabled = true;
                hyperRenderer.GetPropertyBlock(propBlock);
                propBlock.SetMatrix(hyperRotID, composedMat4);
                propBlock.SetMatrix(hyperMapRotID, mapMat4);
                hyperRenderer.SetPropertyBlock(propBlock);
            }

            //Only update collisions if the object is relatively close by.
            if (updateColliders && (dist2 <= colliderDistSqUpdate || !allowCulling)) {
                foreach (WarpCollider warpCollider in warpColliders) {
                    warpCollider.UpdateMesh(_composedGV);
                }
            }
        }
    }

    //Warp colliders are only updated during LateUpdate.  If updated colliders
    //are needed sooner during Update, then this can be called manually.
    public void UpdateCollisions() {
        GyroVector gv = localGV + _worldGV;
        foreach (WarpCollider warpCollider in warpColliders) {
            warpCollider.UpdateMesh(gv);
        }
    }

    //This should be called if you destroy a renderer in a HyperObject.
    public void UpdateRenderers() {
        List<Renderer> renderersList = new List<Renderer>(hyperRenderers);
        renderersList.RemoveAll(x => x == null);
        hyperRenderers = renderersList.ToArray();
    }

    public Vector3 WorldPositionToHyper(Vector3 p) {
        return composedGV * HM.UnitToPoincare(p, useTanKHeight);
    }
    public Vector3 WorldPositionToLocalHyper(Vector3 p) {
        return localGV * HM.UnitToPoincare(p, useTanKHeight);
    }

    //Absorb the GameObject's local rotation into the Gyrovector's rotation then reset it.
    public void AbsorbLocalRotation() {
        localGV = transform.localRotation + localGV;
        transform.localRotation = Quaternion.identity;
    }
    //Absorb the Gyrovector's rotation into the local GameObject's rotation then reset it.
    public void AbsorbGVRotation() {
        transform.localRotation = localGV.gyr * transform.localRotation;
        localGV = Quaternion.Inverse(localGV.gyr) + localGV;
    }

    public void RemoveWarpCollider(WarpCollider wc) {
        for (int i = 0; i < warpColliders.Length; ++i) {
            if (warpColliders[i] == wc) {
                warpColliders[i] = warpColliders[warpColliders.Length - 1];
                Array.Resize(ref warpColliders, warpColliders.Length - 1);
                break;
            }
        }
    }
}
