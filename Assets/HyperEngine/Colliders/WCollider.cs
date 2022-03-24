using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class WCollider {
    //public static List<WCollider> AllColliders = new List<WCollider>();
    public static List<Group> AllColliders = new List<Group>();
    public const float GROUND_PAD_RATIO = 1.01f;

    //Class for collision hits
    public class Hit {
        public Vector3 displacement = Vector3.zero;
        public float maxSinYGround = 0.0f;
        public float maxSinY = 0.0f;
        public float minSinY = 0.0f;
        public int ix = 0;
        public int localIx = 0;
        public HyperObject ho = null;
    }

    public class Group {
        public WCollider[] colliders;
        public int ix;
        public bool skip;
        public HyperObject ho;
    }

    //Radius offset if applicable
    public float offset = 0.0f;

    public static int CountColliders() {
        int numColliders = 0;
        foreach (Group group in AllColliders) {
            numColliders += group.colliders.Length;
        }
        return numColliders;
    }

    public static Hit Collide(Vector3 p, float r, bool useCache = true, GyroVector gv = new GyroVector()) {
        return Collide(p, r, 0, AllColliders.Count, useCache, gv);
    }

    public static Hit Collide(Vector3 p, float r, int ix_from, int ix_to, bool useCache = true, GyroVector gv = new GyroVector()) {
        Hit hit = new Hit();
        Vector3 delta = Vector3.zero;
        GyroVector composedGV = GyroVector.identity;
        float padR = r * GROUND_PAD_RATIO;
        for (int i = ix_from; i < ix_to; ++i) {
            Group group = AllColliders[i];
            if (group == null || group.skip) { continue; }
            if (!useCache) {
                composedGV = group.ho.localGV - gv;
            }
            for (int localIx = 0; localIx < group.colliders.Length; ++localIx) {
                WCollider c = group.colliders[localIx];
                if (useCache) {
                    if (HM.K > 0.0f && group.ho.composedGV.vec.sqrMagnitude > 2.0f) { continue; }
                    if (c.DE(p) + c.offset >= padR) { continue; }
                    delta = c.ClosestPoint(p) - p;
                } else {
                    delta = c.ClosestPoint(p, composedGV) - p;
                }
                Debug.Assert(!float.IsNaN(delta.x), "Nan detected for " + c.GetType() + " | " + group.ho.name);
                float deltaMag = delta.magnitude + c.offset;
                if (deltaMag < padR && deltaMag > 0.0f) {
                    float sY = -delta.y / deltaMag;
                    hit.maxSinYGround = Mathf.Max(hit.maxSinYGround, sY);
                    if (deltaMag < r) {
                        hit.displacement -= delta * ((r - deltaMag) / deltaMag);
                        p += hit.displacement;
                        hit.maxSinY = Mathf.Max(hit.maxSinY, sY);
                        hit.minSinY = Mathf.Min(hit.minSinY, sY);
                        hit.ix = i;
                        hit.localIx = localIx;
                        hit.ho = group.ho;
                    }
                }
            }
        }
        return hit;
    }

    public abstract void UpdateHyperbolic(GyroVector gv);
    public abstract Vector3 ClosestPoint(Vector3 p);
    public abstract Vector3 ClosestPoint(Vector3 p, GyroVector gv);
    public virtual float DE(Vector3 p) { return 0.0f; }
#if UNITY_EDITOR
    public abstract void Draw();
#endif
}
