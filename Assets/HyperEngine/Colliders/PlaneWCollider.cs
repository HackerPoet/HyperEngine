using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlaneWCollider : WCollider {
    //Parameters
    public Vector3 c;
    //Cache
    public Vector3 kc, kn;

    //Normal is assumed to face the center of the tile
    public PlaneWCollider(bool useTanKHeight, Vector3 _c) {
        c = HM.UnitToPoincare(_c, useTanKHeight);
    }

    public override void UpdateHyperbolic(GyroVector gv) {
        MakeHyperbolic(gv, out kc, out kn);
    }

    public override Vector3 ClosestPoint(Vector3 p) {
        return ClosestPoint(p, kc, kn);
    }

    public override Vector3 ClosestPoint(Vector3 p, GyroVector gv) {
        MakeHyperbolic(gv, out Vector3 _kc, out Vector3 _kn);
        return ClosestPoint(p, _kc, _kn);
    }

    private void MakeHyperbolic(GyroVector gv, out Vector3 _kc, out Vector3 _kn) {
        //Transform original vertices into hyperbolic ones
        GyroVector q = c + gv;
        Vector3 wc = q.Point();
        Vector3 wn = q.gyr * c;
        _kc = HM.PoincareToKlein(wc);
        _kn = HM.PoincareToKlein(wc, wn);
    }

    private static Vector3 ClosestPoint(Vector3 p, Vector3 kc, Vector3 kn) {
        Vector3 kp = HM.PoincareToKlein(p);
        kp -= kn * Vector3.Dot(kp - kc, kn);
        return HM.KleinToPoincare(kp);
    }

#if UNITY_EDITOR
    public override void Draw() {
        if (Camera.current.name == "SceneCamera") {
            Vector3 wc = HM.KleinToPoincare(kc);
            Vector3 wn = HM.KleinToPoincare(kc, kn);
            Gizmos.DrawLine(wc, wc + wn * 0.1f);
        } else {
            Vector3 ch = new Vector3(0.0f, -HyperObject.camHeight, 0.0f);
            Vector3 wc = HM.KleinToPoincare(HM.MobiusAdd(ch, kc));
            Vector3 wn = HM.KleinToPoincare(HM.MobiusAdd(ch, kc), kn);
            Gizmos.DrawLine(wc, wc + wn * 0.1f);
        }
    }
#endif
}
