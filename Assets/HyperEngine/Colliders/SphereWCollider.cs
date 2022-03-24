using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SphereWCollider : WCollider {
    //Parameters
    public Vector3 c;
    public float r;
    //Cache
    public Vector3 wc;
    public float wr;

    public SphereWCollider(bool useTanKHeight, Vector3 _c, float _r) {
        c = HM.UnitToPoincare(_c, useTanKHeight);
        r = HM.UnitToPoincareScale(_c, _r, useTanKHeight);
    }

    public override void UpdateHyperbolic(GyroVector gv) {
        MakeHyperbolic(gv, out wc, out wr);
    }

    public override Vector3 ClosestPoint(Vector3 p) {
        return ClosestPoint(p, wc, wr);
    }

    public override Vector3 ClosestPoint(Vector3 p, GyroVector gv) {
        MakeHyperbolic(gv, out Vector3 _wc, out float _wr);
        return ClosestPoint(p, _wc, _wr);
    }

    private void MakeHyperbolic(GyroVector gv, out Vector3 _wc, out float _wr) {
        //Transform original vertices into hyperbolic ones
        _wc = gv * c;
        _wr = r * HM.PoincareScaleFactor(_wc);
    }

    private static Vector3 ClosestPoint(Vector3 p, Vector3 wc, float wr) {
        return wc + (p - wc).normalized * wr;
    }

#if UNITY_EDITOR
    public override void Draw() {
        if (Camera.current.name == "SceneCamera") {
            Gizmos.DrawWireSphere(wc, wr);
        } else {
            Vector3 ch = new Vector3(0.0f, -HyperObject.camHeight, 0.0f);
            Gizmos.DrawWireSphere(HM.MobiusAdd(ch, wc), wr);
        }
    }
#endif
}
