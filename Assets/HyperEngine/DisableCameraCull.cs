using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DisableCameraCull : MonoBehaviour
{
    public static readonly Matrix4x4 hugeBounds = Matrix4x4.Ortho(-100.0f, 100.0f, -100.0f, 100.0f, -100.0f, 100.0f);
    void Awake() {
        GetComponent<Camera>().cullingMatrix = hugeBounds;
    }
}
