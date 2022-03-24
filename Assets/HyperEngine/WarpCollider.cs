using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;

public class WarpCollider : MonoBehaviour {
    [System.Serializable]
    public class Box {
        [DraggablePoint] public Vector3 pos;
        public Vector3 rot;
        public Vector3 size;
        public float offset;
    }
    [System.Serializable]
    public class Sphere {
        [DraggablePoint] public Vector3 center;
        public float radius;
    }
    [System.Serializable]
    public class Cylinder {
        [DraggablePoint] public Vector3 p1;
        [DraggablePoint] public Vector3 p2;
        public float radius;
        public bool capped = false;
    }
    [System.Serializable]
    public class Triangle {
        [DraggablePoint] public Vector3 a;
        [DraggablePoint] public Vector3 b;
        [DraggablePoint] public Vector3 c;
    }
    [System.Serializable]
    public class Plane {
        [DraggablePoint] public Vector3 p;
    }

    public Box[] boundingBoxes = new Box[0];
    public Sphere[] boundingSpheres = new Sphere[0];
    public Cylinder[] boundingCylinders = new Cylinder[0];
    public Triangle[] boundingTriangles = new Triangle[0];
    public Plane[] boundingPlanes = new Plane[0];
    public Mesh[] boundingMeshes = new Mesh[0];
    public bool removeFloorTriangles = true;

    private bool hasStarted = false;
    private HyperObject ho;
    [System.NonSerialized] public int groupIx = -1;
    private static readonly int[] triangles = new int[6 * 6] {
        0, 1, 2, 1, 2, 3,
        0, 2, 4, 2, 4, 6,
        0, 4, 1, 4, 1, 5,
        7, 3, 6, 3, 6, 2,
        7, 5, 3, 5, 3, 1,
        7, 6, 5, 6, 5, 4
    };

    void Start() {
        RegenerateColliders();
    }

    public void WorldReset() {
        hasStarted = false;
        groupIx = -1;
        RegenerateColliders();
    }

    public void RegenerateColliders() {
        //Group index must only be generated once.
        if (!hasStarted) {
            groupIx = WCollider.AllColliders.Count;
            hasStarted = true;
        }
        HyperObject ho = GetComponentInParent<HyperObject>();
        Assert.IsNotNull(ho, "WColliders must be children of a HyperObject");
        List<WCollider> colliders = new List<WCollider>();
        foreach (Box box in boundingBoxes) {
            Vector3[] verts = MakeVertices(box);
            int numTriVerts = (verts.Length > 4 ? triangles.Length : 6);
            for (int i = 0; i < numTriVerts; i += 3) {
                AddCheckFloor(colliders, new TriangleWCollider(ho.useTanKHeight,
                    verts[triangles[i + 0]],
                    verts[triangles[i + 1]],
                    verts[triangles[i + 2]],
                    box.offset));
            }
        }
        foreach (Sphere sphere in boundingSpheres) {
            colliders.Add(new SphereWCollider(ho.useTanKHeight,
                transform.TransformPoint(sphere.center),
                transform.lossyScale.y * sphere.radius));
        }
        foreach (Cylinder cylinder in boundingCylinders) {
            colliders.Add(new CylinderWCollider(ho.useTanKHeight,
                transform.TransformPoint(cylinder.p1),
                transform.TransformPoint(cylinder.p2),
                transform.lossyScale.y * cylinder.radius,
                cylinder.capped));
        }
        foreach (Triangle triangle in boundingTriangles) {
            AddCheckFloor(colliders, new TriangleWCollider(ho.useTanKHeight,
                transform.TransformPoint(triangle.a),
                transform.TransformPoint(triangle.b),
                transform.TransformPoint(triangle.c)));
        }
        foreach (Plane plane in boundingPlanes) {
            colliders.Add(new PlaneWCollider(ho.useTanKHeight,
                transform.TransformPoint(plane.p)));
        }
        foreach (Mesh mesh in boundingMeshes) {
            if (!mesh) continue;
            int[] triangles = mesh.triangles;
            Vector3[] verts = mesh.vertices;
            for (int i = 0; i < triangles.Length; i += 3) {
                AddCheckFloor(colliders, new TriangleWCollider(ho.useTanKHeight,
                    transform.TransformPoint(verts[triangles[i + 0]]),
                    transform.TransformPoint(verts[triangles[i + 1]]),
                    transform.TransformPoint(verts[triangles[i + 2]])));
            }
        }
        WCollider.Group group = new WCollider.Group();
        group.ho = ho;
        group.colliders = colliders.ToArray();
        group.ix = groupIx;
        Debug.Assert(groupIx <= WCollider.AllColliders.Count);
        if (groupIx == WCollider.AllColliders.Count) {
            WCollider.AllColliders.Add(group);
        } else {
            WCollider.AllColliders[groupIx] = group;
        }
        UpdateMesh(ho.localGV + HyperObject.worldGV);
    }

    private void AddCheckFloor(List<WCollider> colliders, TriangleWCollider collider) {
        //Skip triangles exactly on or below the floor since the floor check takes care of them
        if (removeFloorTriangles && collider.p1.y <= 0.0f && collider.p2.y <= 0.0f && collider.p3.y <= 0.0f) {
            return;
        }
        colliders.Add(collider);
    }

    public void UpdateMesh(GyroVector gv) {
        //Do not update if the GameObject or script is disabled
        if (!isActiveAndEnabled) {
            ResetActive();
            return;
        }

        //Update this object's colliders
        WCollider.AllColliders[groupIx].skip = false;
        foreach (WCollider collider in WCollider.AllColliders[groupIx].colliders) {
            collider.UpdateHyperbolic(gv);
        }
    }

    public void ResetActive() {
        if (hasStarted) {
            WCollider.AllColliders[groupIx].skip = true;
        }
    }

    public WCollider.Hit Collide(Vector3 p, float r) {
        return WCollider.Collide(p, r, groupIx, groupIx + 1);
    }

#if UNITY_EDITOR
    public void OnDrawGizmos() {
        Gizmos.color = Color.green;
        if (UnityEditor.EditorApplication.isPlaying ||
            UnityEditor.EditorApplication.isPaused) {
            //This is being viewed while the game is playing.
            //So draw the warped colliders.
            if (Camera.current.name == "SceneCamera") {
                Gizmos.matrix = Matrix4x4.identity;
            } else {
                Gizmos.matrix = Matrix4x4.Translate(Camera.main.transform.position);
            }
            if (!WCollider.AllColliders[groupIx].skip) {
                foreach (WCollider collider in WCollider.AllColliders[groupIx].colliders) {
                    collider.Draw();
                }
            }
        } else {
            //This is being viewed in edit mode.
            //Do not draw the warped colliders.
            Gizmos.matrix = Matrix4x4.identity;
            foreach (Box box in boundingBoxes) {
                Vector3[] verts = MakeVertices(box);
                int numTriVerts = (verts.Length > 4 ? triangles.Length : 6);
                for (int i = 0; i < numTriVerts; i += 3) {
                    Vector3 v1 = verts[triangles[i + 0]];
                    Vector3 v2 = verts[triangles[i + 1]];
                    Vector3 v3 = verts[triangles[i + 2]];
                    Gizmos.DrawLine(v1, v2);
                    Gizmos.DrawLine(v2, v3);
                    Gizmos.DrawLine(v3, v1);
                }
            }
            foreach (Sphere sphere in boundingSpheres) {
                Gizmos.DrawWireSphere(
                    transform.TransformPoint(sphere.center),
                    transform.lossyScale.y * sphere.radius);
            }
            foreach (Cylinder cylinder in boundingCylinders) {
                CylinderWCollider.DrawWireCylinder(
                    transform.TransformPoint(cylinder.p1),
                    transform.TransformPoint(cylinder.p2),
                    transform.lossyScale.y * cylinder.radius,
                    false);
                if (cylinder.capped) {
                    Gizmos.DrawWireSphere(
                        transform.TransformPoint(cylinder.p1),
                        transform.lossyScale.y * cylinder.radius);
                    Gizmos.DrawWireSphere(
                        transform.TransformPoint(cylinder.p2),
                        transform.lossyScale.y * cylinder.radius);
                }
            }
            foreach (Triangle triangle in boundingTriangles) {
                Vector3 v1 = transform.TransformPoint(triangle.a);
                Vector3 v2 = transform.TransformPoint(triangle.b);
                Vector3 v3 = transform.TransformPoint(triangle.c);
                Gizmos.DrawLine(v1, v2);
                Gizmos.DrawLine(v2, v3);
                Gizmos.DrawLine(v3, v1);
            }
            foreach (Plane plane in boundingPlanes) {
                Gizmos.DrawWireSphere(
                    transform.TransformPoint(plane.p),
                    0.05f);
            }
            foreach (Mesh mesh in boundingMeshes) {
                if (!mesh) continue;
                int[] triangles = mesh.triangles;
                Vector3[] verts = mesh.vertices;
                for (int i = 0; i < triangles.Length; i += 3) {
                    Vector3 v1 = verts[triangles[i + 0]];
                    Vector3 v2 = verts[triangles[i + 1]];
                    Vector3 v3 = verts[triangles[i + 2]];
                    Gizmos.DrawLine(v1, v2);
                    Gizmos.DrawLine(v2, v3);
                    Gizmos.DrawLine(v3, v1);
                }
            }
        }
    }
#endif

    private Vector3[] MakeVertices(Box box) {
        //Extract useful properties
        Vector3 c = box.pos;
        Vector3 s = box.size * 0.5f;
        Quaternion r = Quaternion.Euler(box.rot);

        //Return all vertices of the box
        if (s.x == 0.0f) {
            return new Vector3[4] {
                transform.TransformPoint(c + r * new Vector3(0.0f,  s.y,  s.z)),
                transform.TransformPoint(c + r * new Vector3(0.0f, -s.y,  s.z)),
                transform.TransformPoint(c + r * new Vector3(0.0f,  s.y, -s.z)),
                transform.TransformPoint(c + r * new Vector3(0.0f, -s.y, -s.z)),
            };
        } else if (s.y == 0.0f) {
            return new Vector3[4] {
                transform.TransformPoint(c + r * new Vector3( s.x, 0.0f,  s.z)),
                transform.TransformPoint(c + r * new Vector3(-s.x, 0.0f,  s.z)),
                transform.TransformPoint(c + r * new Vector3( s.x, 0.0f, -s.z)),
                transform.TransformPoint(c + r * new Vector3(-s.x, 0.0f, -s.z)),
            };
        } else if (s.z == 0.0f) {
            return new Vector3[4] {
                transform.TransformPoint(c + r * new Vector3( s.x,  s.y, 0.0f)),
                transform.TransformPoint(c + r * new Vector3(-s.x,  s.y, 0.0f)),
                transform.TransformPoint(c + r * new Vector3( s.x, -s.y, 0.0f)),
                transform.TransformPoint(c + r * new Vector3(-s.x, -s.y, 0.0f)),
            };
        } else {
            return new Vector3[8] {
                transform.TransformPoint(c + r * new Vector3( s.x,  s.y,  s.z)),
                transform.TransformPoint(c + r * new Vector3(-s.x,  s.y,  s.z)),
                transform.TransformPoint(c + r * new Vector3( s.x, -s.y,  s.z)),
                transform.TransformPoint(c + r * new Vector3(-s.x, -s.y,  s.z)),
                transform.TransformPoint(c + r * new Vector3( s.x,  s.y, -s.z)),
                transform.TransformPoint(c + r * new Vector3(-s.x,  s.y, -s.z)),
                transform.TransformPoint(c + r * new Vector3( s.x, -s.y, -s.z)),
                transform.TransformPoint(c + r * new Vector3(-s.x, -s.y, -s.z))
            };
        }
    }
}
