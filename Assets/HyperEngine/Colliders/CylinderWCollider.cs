using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class CylinderWCollider : WCollider {
    //Parameters
    public Vector3 a, b;
    public float r;
    public bool capped;
    //Cache
    public Vector3 wc, wd;
    public float wr, wh;

    public CylinderWCollider(bool useTanKHeight, Vector3 _a, Vector3 _b, float _r, bool _capped) {
        a = HM.UnitToPoincare(_a, useTanKHeight);
        b = HM.UnitToPoincare(_b, useTanKHeight);
        r = HM.UnitToPoincareScale((_a + _b)*0.5f, _r, useTanKHeight);
        capped = _capped;
    }

    public override void UpdateHyperbolic(GyroVector gv) {
        MakeHyperbolic(gv, out wc, out wd, out wr, out wh);
    }

    public override Vector3 ClosestPoint(Vector3 p) {
        return ClosestPoint(p, wc, wd, wr, wh, capped);
    }

    public override Vector3 ClosestPoint(Vector3 p, GyroVector gv) {
        MakeHyperbolic(gv, out Vector3 _wc, out Vector3 _wd, out float _wr, out float _wh);
        return ClosestPoint(p, _wc, _wd, _wr, _wh, capped);
    }

    private void MakeHyperbolic(GyroVector gv, out Vector3 _wc, out Vector3 _wd, out float _wr, out float _wh) {
        //Transform original vertices into hyperbolic ones
        Vector3 wa = gv * a;
        Vector3 wb = gv * b;
        _wc = (wa + wb) * 0.5f;
        _wd = (wb - wa) * 0.5f;
        _wr = r * HM.PoincareScaleFactor(_wc);

        //Use unit height for better optimization
        _wh = _wd.magnitude;
        _wd /= _wh;
    }

    private static Vector3 ClosestPoint(Vector3 p, Vector3 wc, Vector3 wd, float wr, float wh, bool capped) {
        //Find unit distance along line
        Vector3 pc = p - wc;
        float lp = Vector3.Dot(pc, wd);
        //Handle easier capped case separately
        if (capped) {
            //Project point to cylinder line segment
            lp = Mathf.Clamp(lp, -wh, wh);
            pc -= lp * wd;
            //Closest point is on the line
            return wc + lp * wd + pc.normalized * wr;
        } else {
            //Project point to cylinder line
            pc -= lp * wd;
            //If inside the cylinder, closest point is on the line
            if (lp >= -wh && lp <= wh) {
                return wc + lp * wd + pc.normalized * wr;
            }
            //Clamp the line projection now
            lp = Mathf.Clamp(lp, -wh, wh);
            //Find distance squared from line
            float r2 = pc.sqrMagnitude;
            //Return closest point on the disk
            if (r2 <= wr * wr) {
                return wc + lp * wd + pc;
            } else {
                pc *= wr / Mathf.Sqrt(r2);
                return wc + lp * wd + pc;
            }
        }
    }

#if UNITY_EDITOR
    public override void Draw() {
        if (Camera.current.name == "SceneCamera") {
            DrawWireCylinder(wc + wd * wh, wc - wd * wh, wr, false);
            if (capped) {
                Gizmos.DrawWireSphere(wc + wd * wh, wr);
                Gizmos.DrawWireSphere(wc - wd * wh, wr);
            }
        } else {
            Vector3 ch = new Vector3(0.0f, -HyperObject.camHeight, 0.0f);
            Vector3 p1 = HM.MobiusAdd(ch, wc + wd * wh);
            Vector3 p2 = HM.MobiusAdd(ch, wc - wd * wh);
            DrawWireCylinder(p1, p2, wr, true);
            if (capped) {
                Gizmos.DrawWireSphere(p1, wr);
                Gizmos.DrawWireSphere(p2, wr);
            }
        }
    }

    public static void DrawWireCylinder(Vector3 pos, Vector3 pos2, float radius, bool adjustCam) {
        Vector3 forward = pos2 - pos;
        if (float.IsNaN(forward.sqrMagnitude) || forward.sqrMagnitude < 1e-8) { return; }
        Quaternion rot = Quaternion.LookRotation(forward);
        float length = forward.magnitude;
        if (adjustCam) { pos += Camera.main.transform.position; }
        Matrix4x4 angleMatrix = Matrix4x4.TRS(pos, rot, Handles.matrix.lossyScale);
        Handles.color = Gizmos.color;
        using (new Handles.DrawingScope(angleMatrix)) {
            Handles.DrawWireDisc(Vector3.zero, Vector3.forward, radius);
            Handles.DrawWireDisc(new Vector3(0f, 0f, length), Vector3.forward, radius);
            Handles.DrawLine(new Vector3(radius, 0f, 0f), new Vector3(radius, 0f, length));
            Handles.DrawLine(new Vector3(-radius, 0f, 0f), new Vector3(-radius, 0f, length));
            Handles.DrawLine(new Vector3(0f, radius, 0f), new Vector3(0f, radius, length));
            Handles.DrawLine(new Vector3(0f, -radius, 0f), new Vector3(0f, -radius, length));
        }
    }
#endif
}
