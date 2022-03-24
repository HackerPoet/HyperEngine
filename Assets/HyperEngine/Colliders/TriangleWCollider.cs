using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TriangleWCollider : WCollider {
    //Parameters
    public Vector3 p1, p2, p3;
    //Cache
    public Vector3 a, b, c;
    public Vector3 boundingC;
    public float boundingR;

    public TriangleWCollider(bool useTanKHeight, Vector3 _p1, Vector3 _p2, Vector3 _p3, float _offset = 0.0f) {
        p1 = HM.UnitToPoincare(_p1, useTanKHeight);
        p2 = HM.UnitToPoincare(_p2, useTanKHeight);
        p3 = HM.UnitToPoincare(_p3, useTanKHeight);
        offset = _offset;
    }

    public override void UpdateHyperbolic(GyroVector gv) {
        //Transform original vertices into hyperbolic ones
        Vector3 q1 = gv * p1;
        Vector3 q2 = gv * p2;
        Vector3 q3 = gv * p3;

        //Save into more efficient 'corner' format
        a = q1 - q2;
        b = q3 - q2;
        c = q2;

        //Create simple bounding sphere
        boundingC = (q1 + q2 + q3) / 3.0f;
        float r1 = (q1 - boundingC).sqrMagnitude;
        float r2 = (q2 - boundingC).sqrMagnitude;
        float r3 = (q3 - boundingC).sqrMagnitude;
        boundingR = Mathf.Sqrt(Mathf.Min(Mathf.Min(r1, r2), r3));
    }

    public override float DE(Vector3 p) {
        return (p - boundingC).magnitude - boundingR;
    }

    public override Vector3 ClosestPoint(Vector3 p) {
        return ClosestPoint(p, a, b, c);
    }

    public override Vector3 ClosestPoint(Vector3 p, GyroVector gv) {
        //Transform original vertices into hyperbolic ones
        //Optimization: Slightly more efficient than regular GV-vector multiplications
        //              since we can remove 2 quaternion-vector multiplications.
        Vector3 q1 = HM.MobiusAdd(gv.vec, p1);
        Vector3 q2 = HM.MobiusAdd(gv.vec, p2);
        Vector3 q3 = HM.MobiusAdd(gv.vec, p3);
        return gv.gyr * ClosestPoint(p, q1 - q2, q3 - q2, q2);
    }

    private static Vector3 ClosestPoint(Vector3 p, Vector3 a, Vector3 b, Vector3 c) {
        Vector3 v = c - p;

        float aa = Vector3.Dot(a, a);
        float ab = Vector3.Dot(a, b);
        float bb = Vector3.Dot(b, b);
        float av = Vector3.Dot(a, v);
        float bv = Vector3.Dot(b, v);

        float det = aa*bb - ab*ab;
        float s   = ab*bv - bb*av;
        float t   = ab*av - aa*bv;

        if (s + t < det) {
            if (s < 0.0f) {
                if (t < 0.0f) {
                    if (av < 0.0f) {
                        s = Mathf.Clamp01(-av / aa);
                        t = 0.0f;
                    } else {
                        s = 0.0f;
                        t = Mathf.Clamp01(-bv / bb);
                    }
                } else {
                    s = 0.0f;
                    t = Mathf.Clamp01(-bv / bb);
                }
            } else if (t < 0.0f) {
                s = Mathf.Clamp01(-av / aa);
                t = 0.0f;
            } else {
                float invDet = 1.0f / det;
                s *= invDet;
                t *= invDet;
            }
        } else {
            if (s < 0.0f) {
                float tmp0 = ab + av;
                float tmp1 = bb + bv;
                if (tmp1 > tmp0) {
                    float numer = tmp1 - tmp0;
                    float denom = aa - 2*ab + bb;
                    s = Mathf.Clamp01(numer / denom);
                    t = 1.0f - s;
                } else {
                    t = Mathf.Clamp01(-bv / bb);
                    s = 0.0f;
                }
            } else if (t < 0.0f) {
                if (aa + av > ab + bv) {
                    float numer = bb + bv - ab - av;
                    float denom = aa - 2*ab + bb;
                    s = Mathf.Clamp01(numer / denom);
                    t = 1.0f - s;
                } else {
                    s = Mathf.Clamp01(-bv / bb);
                    t = 0.0f;
                }
            } else {
                float numer = bb + bv - ab - av;
                float denom = aa - 2*ab + bb;
                s = Mathf.Clamp01(numer / denom);
                t = 1.0f - s;
            }
        }

        return c + a*s + b*t;
    }

#if UNITY_EDITOR
    public override void Draw() {
        if (Camera.current.name == "SceneCamera") {
            Gizmos.DrawLine(c, c + a);
            Gizmos.DrawLine(c, c + b);
            Gizmos.DrawLine(c + a, c + b);
        } else {
            Vector3 ch = new Vector3(0.0f, -HyperObject.camHeight, 0.0f);
            Vector3 p1 = HM.MobiusAdd(ch, c);
            Vector3 p2 = HM.MobiusAdd(ch, c + a);
            Vector3 p3 = HM.MobiusAdd(ch, c + b);
            Gizmos.DrawLine(p1, p2);
            Gizmos.DrawLine(p1, p3);
            Gizmos.DrawLine(p2, p3);
        }
    }
#endif
}
