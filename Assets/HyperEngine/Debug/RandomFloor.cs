using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RandomFloor : MonoBehaviour {
    private void Start() {
        string coord = transform.parent.name.Substring(5);
        System.Random rand = new System.Random(coord.GetHashCode());
        Vector3 randVector = new Vector3((float)rand.NextDouble(), (float)rand.NextDouble(), (float)rand.NextDouble());
        randVector = randVector * 0.5f + Vector3.one * 0.5f;
        Color randColor = new Color(randVector.x, randVector.y, randVector.z, 1.0f);
        GetComponent<SetTextures>().UpdateColorOnly(randColor);
    }
}
