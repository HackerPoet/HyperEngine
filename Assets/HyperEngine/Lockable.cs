using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Lockable : MonoBehaviour
{
    [System.NonSerialized] public HyperObject ho;
    [System.NonSerialized] public HyperCanvasObject hco;

    public virtual void Awake() {
        ho = GetComponentInParent<HyperObject>();
        hco = GetComponentInParent<HyperCanvasObject>();
        Debug.Assert(ho != null || hco != null);
    }

    public virtual void PlayerUnlook(Player p) {}
    public virtual void PlayerLook(Player p) {}
    public virtual void ObjectLocalTransform(out Vector3 pos, out Vector3 forward) {
        pos = transform.position;
        forward = transform.forward;
    }

    public void ObjectTransform(out Vector3 pos, out Vector3 forward) {
        ObjectLocalTransform(out Vector3 localPos, out Vector3 localForward);
        ho.composedGV.TransformNormal(HM.UnitToPoincare(localPos, ho.useTanKHeight), localForward, out pos, out forward);
    }

    public Vector3 ObjectLocalCenter() {
        ObjectLocalTransform(out Vector3 pos, out Vector3 forward);
        return pos;
    }
    public Vector3 ObjectCenter() {
        ObjectTransform(out Vector3 pos, out Vector3 forward);
        return pos;
    }
}
