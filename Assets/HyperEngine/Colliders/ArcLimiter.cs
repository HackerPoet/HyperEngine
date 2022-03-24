using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ArcLimiter : MonoBehaviour
{
    public float arcMin = 0.0f;
    public float arcMax = 0.0f;
    public float arcRadiusSq = 0.97f;

    void Update() {
        GyroVector gv = HyperObject.worldGV;
        float x = gv.vec.x;
        float y = gv.vec.y;
        float z = gv.vec.z;
        float magSq = x * x + y * y + z * z;
        if (magSq > arcRadiusSq) {
            float ang = Mathf.Atan2(-z, -x);
            if (ang >= arcMin && ang <= arcMax) {
                float r = Mathf.Sqrt(arcRadiusSq / magSq);
                gv.vec = new Vector3(x * r, y * r, z * r);
                HyperObject.worldGV = gv;
            }
        }
    }


#if UNITY_EDITOR
    public void OnDrawGizmos() {
        Gizmos.color = Color.blue;
        if ((UnityEditor.EditorApplication.isPlaying || UnityEditor.EditorApplication.isPaused) && Camera.current.name == "SceneCamera") {
            Vector3 p1 = new Vector3(Mathf.Cos(arcMin), 0.0f, Mathf.Sin(arcMin));
            Vector3 p2 = new Vector3(Mathf.Cos(arcMax), 0.0f, Mathf.Sin(arcMax));
            Gizmos.DrawLine(Vector3.zero, p1);
            Gizmos.DrawLine(Vector3.zero, p2);
        }
    }
#endif
}
