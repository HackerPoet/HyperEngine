using System;
using UnityEngine;
using UnityEngine.Assertions;

//Extend Unity's Vector3 to double precision
public struct Vector3D {
    public Vector3D(double _x, double _y, double _z) { x = _x; y = _y; z = _z; }
    public static readonly Vector3D zero = new Vector3D(0.0, 0.0, 0.0);
    public static readonly Vector3D up = new Vector3D(0.0, 1.0, 0.0);

    public double sqrMagnitude {
        get { return x * x + y * y + z * z; }
    }
    public Vector3D normalized {
        get { return this / Math.Sqrt(sqrMagnitude); }
    }

    public static double Dot(Vector3D a, Vector3D b) {
        return a.x * b.x + a.y * b.y + a.z * b.z;
    }
    public static Vector3D Cross(Vector3D a, Vector3D b) {
        return new Vector3D(a.y*b.z - a.z*b.y, a.z*b.x - a.x*b.z, a.x*b.y - a.y*b.x);
    }
    public static Vector3D Project(Vector3D v, Vector3D n) {
        return n * (Dot(v, n) / n.sqrMagnitude);
    }
    public static Vector3D Lerp(Vector3D a, Vector3D b, double t) {
        return a * (1.0 - t) + b * t;
    }
    public static Vector3D operator+(Vector3D a, Vector3D b) {
        return new Vector3D(a.x + b.x, a.y + b.y, a.z + b.z);
    }
    public static Vector3D operator-(Vector3D a, Vector3D b) {
        return new Vector3D(a.x - b.x, a.y - b.y, a.z - b.z);
    }
    public static Vector3D operator-(Vector3D a) {
        return new Vector3D(-a.x, -a.y, -a.z);
    }
    public static Vector3D operator*(Vector3D v, double d) {
        return new Vector3D(v.x * d, v.y * d, v.z * d);
    }
    public static Vector3D operator*(double d, Vector3D v) {
        return new Vector3D(v.x * d, v.y * d, v.z * d);
    }
    public static Vector3D operator/(Vector3D v, double d) {
        return new Vector3D(v.x / d, v.y / d, v.z / d);
    }
    public static explicit operator Vector3(Vector3D v) {
        return new Vector3((float)v.x, (float)v.y, (float)v.z);
    }
    public static explicit operator Vector3D(Vector3 v) {
        return new Vector3D(v.x, v.y, v.z);
    }

    public double x;
    public double y;
    public double z;
}

//Extend Unity's Quaternions to double precision
public struct QuaternionD {
    public QuaternionD(double _x, double _y, double _z, double _w) { x = _x; y = _y; z = _z; w = _w; }
    public static readonly QuaternionD identity = new QuaternionD(0.0, 0.0, 0.0, 1.0);

    public QuaternionD normalized {
        get {
            double invMag = 1.0 / Math.Sqrt(x * x + y * y + z * z + w * w);
            return new QuaternionD(invMag * x, invMag * y, invMag * z, invMag * w);
        }
    }
    public void Normalize() {
        double invMag = 1.0 / Math.Sqrt(x * x + y * y + z * z + w * w);
        x *= invMag; y *= invMag; z *= invMag; w *= invMag;
    }

    public static QuaternionD Inverse(QuaternionD q) {
        return new QuaternionD(-q.x, -q.y, -q.z, q.w);
    }
    public static QuaternionD Lerp(QuaternionD a, QuaternionD b, double t) {
        double t2 = t * Math.Sign(a.x * b.x + a.y * b.y + a.z * b.z + a.w * b.w);
        double x = a.x * (1.0 - t) + b.x * t2;
        double y = a.y * (1.0 - t) + b.y * t2;
        double z = a.z * (1.0 - t) + b.z * t2;
        double w = a.w * (1.0 - t) + b.w * t2;
        return new QuaternionD(x, y, z, w).normalized;
    }
    public static QuaternionD FromToRotation(Vector3D a, Vector3D b) {
        Vector3D c = Vector3D.Cross(a, b);
        double w = Math.Sqrt(a.sqrMagnitude * b.sqrMagnitude) + Vector3D.Dot(a, b);
        return new QuaternionD(c.x, c.y, c.z, w).normalized;
    }
    public static QuaternionD operator *(QuaternionD q, QuaternionD r) {
        return new QuaternionD(
            r.w*q.x + r.x*q.w - r.y*q.z + r.z*q.y,
            r.w*q.y + r.x*q.z + r.y*q.w - r.z*q.x,
            r.w*q.z - r.x*q.y + r.y*q.x + r.z*q.w,
            r.w*q.w - r.x*q.x - r.y*q.y - r.z*q.z).normalized;
    }
    public static Vector3D operator*(QuaternionD q, Vector3D v) {
        Vector3D r = new Vector3D(q.x, q.y, q.z);
        return v + Vector3D.Cross(2.0 * r, Vector3D.Cross(r, v) + q.w * v);
    }
    public static explicit operator Quaternion(QuaternionD v) {
        return new Quaternion((float)v.x, (float)v.y, (float)v.z, (float)v.w);
    }
    public static explicit operator QuaternionD(Quaternion v) {
        return new QuaternionD(v.x, v.y, v.z, v.w);
    }

    public double x;
    public double y;
    public double z;
    public double w;
}

//HyperMath abbreviated for easier usage
public class HM {
    //The hyperbolic width of a tile
    public static double CELL_WIDTH = 0.0;
    //The location of each vertex in Klein coordinates
    public static float KLEIN_V = 0.0f;
    //Curvature class (-1=Hyperbolic, 0=Euclidean, 1=Spherical)
    public static float K = 0.0f;
    //Number of square tiles that connect at each vertex
    public static int N = 4;

    public static void SetTileType(float n) {
        //Do calculations in double precision because this only needs to be called once
        //and it is very important that these numbers are as accurate as possible.
        //The tiny epsilon is added at the end to hide small gaps between tiles.
        N = (int)n;
        if (n == 4.0f) {
            K = 0.0f;
            KLEIN_V = 1.0f;
            CELL_WIDTH = 2.0;
        } else {
            K = (n < 4.0f ? 1.0f : -1.0f);
            double a = Math.PI / 4;
            double b = Math.PI / n;
            double r = Math.Cos(b) / Math.Sin(a);
            CELL_WIDTH = Math.Sqrt(Math.Abs(r * r - 1.0)) / r;
            KLEIN_V = (float)(CELL_WIDTH + (3e-4 / n));
        }

        //Update the shader _KleinV if running from the main thread, such as in-game.
        //Exceptions are thrown from helper threads like in unit tests or editor scripts
        //but we don't need to use shaders in those cases so just ignore the exception.
        try {
            Shader.SetGlobalFloat("_KleinV", HM.KLEIN_V);
        } catch (UnityException) {}
    }

    //Inverse hyperbolic trig functions (.NET doesn't provide these for some reason) 
    public static double Acosh(double x) {
        return Math.Log(x + Math.Sqrt(x*x - 1));
    }
    public static double Atanh(double x) {
        return 0.5 * Math.Log((1.0 + x) / (1.0 - x));
    }

    //Curvature-dependent tangent
    public static float TanK(float x) {
        if (K > 0.0f) {
            return Mathf.Tan(x);
        } else if (K < 0.0f) {
            return (float)Math.Tanh(x);
        } else {
            return x;
        }
    }
    //Curvature-dependent inverse tangent
    public static float AtanK(float x) {
        if (K > 0.0f) {
            return Mathf.Atan(x);
        } else if (K < 0.0f) {
            return 0.5f * Mathf.Log((1.0f + x)/(1.0f - x));
        } else {
            return x;
        }
    }

    //3D Möbius addition (non-commutative, non-associative)
    //NOTE: This is much more numerically stable than the one in Ungar's paper.
    public static Vector3 MobiusAdd(Vector3 a, Vector3 b) {
        Vector3 c = K * Vector3.Cross(a, b);
        float d = 1.0f - K * Vector3.Dot(a, b);
        Vector3 t = a + b;
        return (t*d + Vector3.Cross(c, t)) / (d*d + c.sqrMagnitude);
    }
    public static Vector3D MobiusAdd(Vector3D a, Vector3D b) {
        Vector3D c = K * Vector3D.Cross(a, b);
        double d = 1.0 - K * Vector3D.Dot(a, b);
        Vector3D t = a + b;
        return (t * d + Vector3D.Cross(c, t)) / (d * d + c.sqrMagnitude);
    }

    //3D Möbius gyration
    public static Quaternion MobiusGyr(Vector3 a, Vector3 b) {
        //We're actually doing this operation:
        //  Quaternion.AngleAxis(180.0f, MobiusAdd(a, b)) * Quaternion.AngleAxis(180.0f, a + b);
        //But the precision is better (and faster) by doing the way below:
        Vector3 c = K*Vector3.Cross(a, b);
        float d = 1.0f - K*Vector3.Dot(a, b);
        Quaternion q = new Quaternion(c.x, c.y, c.z, d);
        q.Normalize();
        return q;
    }

    //Optimization to combine Möbius addition and gyration operations.
    //Equivalent to sum = MobiusAdd(a,b); gyr = MobiusGyr(b,a);
    public static void MobiusAddGyrUnnorm(Vector3 a, Vector3 b, out Vector3 sum, out Quaternion gyr) {
        Vector3 c = K * Vector3.Cross(a, b);
        float d = 1.0f - K * Vector3.Dot(a, b);
        Vector3 t = a + b;
        sum = (t * d + Vector3.Cross(c, t)) / (d * d + c.sqrMagnitude);
        gyr = new Quaternion(-c.x, -c.y, -c.z, d);
    }
    public static void MobiusAddGyr(Vector3 a, Vector3 b, out Vector3 sum, out Quaternion gyr) {
        MobiusAddGyrUnnorm(a, b, out sum, out gyr);
        gyr.Normalize();
    }
    public static void MobiusAddGyr(Vector3D a, Vector3D b, out Vector3D sum, out QuaternionD gyr) {
        Vector3D c = K * Vector3D.Cross(a, b);
        double d = 1.0 - K * Vector3D.Dot(a, b);
        Vector3D t = a + b;
        sum = (t * d + Vector3D.Cross(c, t)) / (d * d + c.sqrMagnitude);
        gyr = new QuaternionD(-c.x, -c.y, -c.z, d);
        gyr.Normalize();
    }

    //3D Möbius distance squared
    //Equivalent to MobiusAdd(a, -b).sqrMagnitude
    public static float MobiusDistSq(Vector3 a, Vector3 b) {
        float a2 = a.sqrMagnitude;
        float b2 = b.sqrMagnitude;
        float ab = 2.0f * Vector3.Dot(a, b);
        return (a2 - ab + b2) / (1.0f + K * (ab + K * a2 * b2));
    }
    public static double MobiusDistSq(Vector3D a, Vector3D b) {
        double a2 = a.sqrMagnitude;
        double b2 = b.sqrMagnitude;
        double ab = 2.0 * Vector3D.Dot(a, b);
        return (a2 - ab + b2) / (1.0 + K * (ab + K * a2 * b2));
    }

    //Point conversion between Klein and Poincaré
    public static Vector3 KleinToPoincare(Vector3 p) {
        if (K == 0.0f) { return p; }
        return p / (Mathf.Sqrt(Mathf.Max(0.0f, 1.0f + K * p.sqrMagnitude)) + 1.0f);
    }
    public static Vector3 PoincareToKlein(Vector3 p) {
        if (K == 0.0f) { return p; }
        return p * 2.0f / (1.0f - K * p.sqrMagnitude);
    }
    //Plane normal conversion between Klein and Poincaré
    public static Vector3 KleinToPoincare(Vector3 p, Vector3 n) {
        if (K == 0.0f) { return n.normalized; }
        return ((1.0f + Mathf.Sqrt(1.0f + K * p.sqrMagnitude)) * n + (K * Vector3.Dot(n, p)) * p).normalized;
    }
    public static Vector3 PoincareToKlein(Vector3 p, Vector3 n) {
        if (K == 0.0f) { return n.normalized; }
        return ((1.0f + K * p.sqrMagnitude) * n - (2.0f * K * Vector3.Dot(n, p)) * p).normalized;
    }

    //Conversions between Unit tile coordinates and Klein coordinates
    public static Vector3 UnitToKlein(Vector3 p, bool useTanKHeight) {
        p *= KLEIN_V;
        if (useTanKHeight) {
            p.y = TanK(p.y) * Mathf.Sqrt(1.0f + K * (p.x * p.x + p.z * p.z));
        }
        return p;
    }
    public static Vector3 KleinToUnit(Vector3 p, bool useTanKHeight) {
        if (useTanKHeight) {
            p.y = AtanK(p.y / Mathf.Sqrt(1.0f + K * (p.x * p.x + p.z * p.z)));
        }
        return p / KLEIN_V;
    }

    //Composite conversions
    public static Vector3 UnitToPoincare(Vector3 u, bool useTanKHeight) {
        return KleinToPoincare(UnitToKlein(u, useTanKHeight));
    }
    public static Vector3 PoincareToUnit(Vector3 u, bool useTanKHeight) {
        return KleinToUnit(PoincareToKlein(u), useTanKHeight);
    }

    //Other conversions
    public static float UnitToPoincareScale(Vector3 u, float r, bool useTanKHeight) {
        if (K == 0.0f) { return r; }
        u = UnitToKlein(u, useTanKHeight);
        float p = Mathf.Sqrt(1.0f + K * u.sqrMagnitude);
        return r * KLEIN_V / (p * (p + 1));
    }
    public static float PoincareScaleFactor(Vector3 p) {
        return 1.0f + K * p.sqrMagnitude;
    }

    //Apply a translation to the a hyper-rotation
    public static Vector3 HyperTranslate(float dx, float dz) {
        return HyperTranslate(new Vector3(dx, 0.0f, dz));
    }
    public static Vector3 HyperTranslate(float dx, float dy, float dz) {
        return HyperTranslate(new Vector3(dx, dy, dz));
    }
    public static Vector3 HyperTranslate(Vector3 d) {
        float mag = d.magnitude;
        if (mag < 1e-5f) {
            return Vector3.zero;
        }
        return d * (TanK(mag) / mag);
    }

    //"Distance" metric (not a real distance but ordered)
    public static float PoincareDist(Vector3 a, Vector3 b) {
        return (a - b).sqrMagnitude / ((K + a.sqrMagnitude) * (K + b.sqrMagnitude));
    }

    //Returns the up-vector at a given point
    public static Vector3 UpVector(Vector3 p) {
        float u = 1.0f + K * p.sqrMagnitude;
        float v = -2.0f * K * p.y;
        return (u * Vector3.up + v * p).normalized;
    }
    public static Vector3D UpVector(Vector3D p) {
        double u = 1.0 + K * p.sqrMagnitude;
        double v = -2.0 * K * p.y;
        return (u * Vector3D.up + v * p).normalized;
    }

    //Swing-Twist decomposition of a quaternion
    public static Quaternion SwingTwist(Quaternion q, Vector3 d) {
        Vector3 ra = new Vector3(q.x, q.y, q.z);
        Vector3 p = Vector3.Project(ra, d);
        return (new Quaternion(p.x, p.y, p.z, q.w)).normalized;
    }
    public static QuaternionD SwingTwist(QuaternionD q, Vector3D d) {
        Vector3D ra = new Vector3D(q.x, q.y, q.z);
        Vector3D p = Vector3D.Project(ra, d);
        return (new QuaternionD(p.x, p.y, p.z, q.w)).normalized;
    }

    //Project the Poincaré point onto the XZ plane
    public static Vector3 ProjectToPlane(Vector3 p) {
        float m = K * p.sqrMagnitude;
        float d = 1.0f + m;
        float s = 2.0f / (1.0f - m + Mathf.Sqrt(d * d - 4.0f * K * p.y * p.y));
        return new Vector3(p.x * s, 0.0f, p.z * s);
    }
    public static Vector3D ProjectToPlane(Vector3D p) {
        double m = K * p.sqrMagnitude;
        double d = 1.0 + m;
        double s = 2.0 / (1.0 - m + Math.Sqrt(d * d - 4.0 * K * p.y * p.y));
        return new Vector3D(p.x * s, 0.0, p.z * s);
    }
}

//Data structure to hold a full Möbius transform
[System.Serializable]
public struct GyroVector
{
    //Identity element
    public static readonly GyroVector identity = new GyroVector(Vector3.zero);

    //Members
    public Vector3 vec;     //This is the hyperbolic offset vector or position
    public Quaternion gyr;  //This is the post-rotation as a result of holonomy

    //Constructors
    public GyroVector(float x, float y, float z) { vec = new Vector3(x,y,z); gyr = Quaternion.identity; }
    public GyroVector(Vector3 _vec) { vec = _vec; gyr = Quaternion.identity; }
    public GyroVector(Quaternion _gyr) { vec = Vector3.zero; gyr = _gyr.normalized; }
    public GyroVector(Vector3 _vec, Quaternion _gyr) { vec = _vec; gyr = _gyr.normalized; }

    //Compose the GyroVector with a Möbius Translation
    public static GyroVector operator+(GyroVector gv, Vector3 delta) {
        HM.MobiusAddGyrUnnorm(gv.vec, Quaternion.Inverse(gv.gyr) * delta, out Vector3 newVec, out Quaternion newGyr);
        return new GyroVector(newVec, gv.gyr * newGyr);
    }
    public static GyroVector operator+(Vector3 delta, GyroVector gv) {
        HM.MobiusAddGyrUnnorm(delta, gv.vec, out Vector3 newVec, out Quaternion newGyr);
        return new GyroVector(newVec, gv.gyr * newGyr);
    }
    public static GyroVector operator+(GyroVector gv, Quaternion rot) {
        return new GyroVector(gv.vec, rot * gv.gyr);
    }
    public static GyroVector operator+(Quaternion rot, GyroVector gv2) {
        return new GyroVector(Quaternion.Inverse(rot) * gv2.vec, gv2.gyr * rot);
    }
    public static GyroVector operator+(GyroVector gv1, GyroVector gv2) {
        HM.MobiusAddGyrUnnorm(gv1.vec, Quaternion.Inverse(gv1.gyr) * gv2.vec, out Vector3 newVec, out Quaternion newGyr);
        return new GyroVector(newVec, gv2.gyr * gv1.gyr * newGyr);
    }

    //Inverse GyroVector
    public static GyroVector operator-(GyroVector gv) {
        return new GyroVector(-(gv.gyr * gv.vec), Quaternion.Inverse(gv.gyr));
    }

    //Inverse composition
    public static GyroVector operator-(GyroVector gv, Vector3 delta) {
        return gv + (-delta);
    }
    public static GyroVector operator-(Vector3 delta, GyroVector gv) {
        return delta + (-gv);
    }
    public static GyroVector operator-(GyroVector gv, Quaternion rot) {
        return gv + Quaternion.Inverse(rot);
    }
    public static GyroVector operator-(Quaternion rot, GyroVector gv) {
        return rot + (-gv);
    }
    public static GyroVector operator-(GyroVector gv1, GyroVector gv2) {
        return gv1 + (-gv2);
    }

    //Apply the full GyroVector to a point
    public static Vector3 operator*(GyroVector gv, Vector3 pt) {
        return gv.gyr * HM.MobiusAdd(gv.vec, pt);
    }
    public Vector3 Point() {
        return gyr * vec;
    }

    //Apply the full GyroVector to a point AND the normal at that point
    public void TransformNormal(Vector3 pt, Vector3 n, out Vector3 newPt, out Vector3 newN) {
        HM.MobiusAddGyr(vec, pt, out Vector3 v, out Quaternion q);
        newPt = gyr * v;
        newN = gyr * (Quaternion.Inverse(q) * n);
    }

    //Aligns the rotation of the gyrovector so that up is up
    public void AlignUpVector() {
        //TODO: I'm sure all this math could simplify eventually...
        Vector3 newAxis = HM.UpVector(vec);
        Quaternion newBasis = Quaternion.FromToRotation(newAxis, Vector3.up);
        Quaternion twist = HM.SwingTwist(gyr, newAxis);
        gyr = newBasis * twist;
    }

    //Projects the Gyrovector to the ground plane
    public GyroVector ProjectToPlane() {
        //Remove the y-component from the Klein projection and any out-of-plane rotation
        return new GyroVector(HM.ProjectToPlane(vec), new Quaternion(0.0f, gyr.y, 0.0f, gyr.w));
    }

    //Convert to a matrix so the shader can read it
    public Matrix4x4 ToMatrix() {
        return Matrix4x4.TRS(vec, gyr, Vector3.one);
    }

    //Spherical linear interpolation
    public static GyroVector Slerp(GyroVector a, GyroVector b, float t) {
        return new GyroVector(Vector3.LerpUnclamped(a.vec, b.vec, t), Quaternion.SlerpUnclamped(a.gyr, b.gyr, t));
    }
    public static GyroVector SlerpReverse(GyroVector a, GyroVector b, float t) {
        return Slerp(identity, b - a, t) + a;
    }

    //Human readable form
    public override string ToString() {
        return "(" + ((double)vec.x).ToString("F9") + ", " +
               ((double)vec.y).ToString("F9") + ", " +
               ((double)vec.z).ToString("F9") + ") [" +
               ((double)gyr.x).ToString("F9") + ", " +
               ((double)gyr.y).ToString("F9") + ", " +
               ((double)gyr.z).ToString("F9") + ", " +
               ((double)gyr.w).ToString("F9") + "]";
    }
}

//Data structure to hold a full Möbius transform (double precision)
public struct GyroVectorD {
    //Identity element
    public static readonly GyroVectorD identity = new GyroVectorD(Vector3D.zero);

    //Members
    public Vector3D vec;     //This is the hyperbolic offset vector or position
    public QuaternionD gyr;  //This is the post-rotation as a result of holonomy

    //Constructors
    public GyroVectorD(double x, double y, double z) { vec = new Vector3D(x, y, z); gyr = QuaternionD.identity; }
    public GyroVectorD(Vector3D _vec) { vec = _vec; gyr = QuaternionD.identity; }
    public GyroVectorD(QuaternionD _gyr) { vec = Vector3D.zero; gyr = _gyr.normalized; }
    public GyroVectorD(Vector3D _vec, QuaternionD _gyr) { vec = _vec; gyr = _gyr.normalized; }

    //Convert to regular GyroVector
    public static explicit operator GyroVector(GyroVectorD gv) {
        return new GyroVector((Vector3)gv.vec, (Quaternion)gv.gyr);
    }
    public static explicit operator GyroVectorD(GyroVector gv) {
        return new GyroVectorD((Vector3D)gv.vec, (QuaternionD)gv.gyr);
    }

    //Compose the GyroVectorD with a Möbius Translation
    public static GyroVectorD operator +(GyroVectorD gv, Vector3D delta) {
        HM.MobiusAddGyr(gv.vec, QuaternionD.Inverse(gv.gyr) * delta, out Vector3D newVec, out QuaternionD newGyr);
        return new GyroVectorD(newVec, gv.gyr * newGyr);
    }
    public static GyroVectorD operator +(Vector3D delta, GyroVectorD gv) {
        HM.MobiusAddGyr(delta, gv.vec, out Vector3D newVec, out QuaternionD newGyr);
        return new GyroVectorD(newVec, gv.gyr * newGyr);
    }
    public static GyroVectorD operator +(GyroVectorD gv, QuaternionD rot) {
        return new GyroVectorD(gv.vec, rot * gv.gyr);
    }
    public static GyroVectorD operator +(QuaternionD rot, GyroVectorD gv2) {
        return new GyroVectorD(QuaternionD.Inverse(rot) * gv2.vec, gv2.gyr * rot);
    }
    public static GyroVectorD operator +(GyroVectorD gv1, GyroVectorD gv2) {
        HM.MobiusAddGyr(gv1.vec, QuaternionD.Inverse(gv1.gyr) * gv2.vec, out Vector3D newVec, out QuaternionD newGyr);
        return new GyroVectorD(newVec, gv2.gyr * gv1.gyr * newGyr);
    }

    //Inverse GyroVectorD
    public static GyroVectorD operator -(GyroVectorD gv) {
        return new GyroVectorD(-(gv.gyr * gv.vec), QuaternionD.Inverse(gv.gyr));
    }

    //Inverse composition
    public static GyroVectorD operator -(GyroVectorD gv, Vector3D delta) {
        return gv + (-delta);
    }
    public static GyroVectorD operator -(Vector3D delta, GyroVectorD gv) {
        return delta + (-gv);
    }
    public static GyroVectorD operator -(GyroVectorD gv, QuaternionD rot) {
        return gv + QuaternionD.Inverse(rot);
    }
    public static GyroVectorD operator -(QuaternionD rot, GyroVectorD gv) {
        return rot + (-gv);
    }
    public static GyroVectorD operator -(GyroVectorD gv1, GyroVectorD gv2) {
        return gv1 + (-gv2);
    }

    //Apply the full GyroVectorD to a point
    public static Vector3D operator *(GyroVectorD gv, Vector3D pt) {
        return gv.gyr * HM.MobiusAdd(gv.vec, pt);
    }
    public Vector3D Point() {
        return gyr * vec;
    }

    //Aligns the rotation of the gyrovector so that up is up
    public void AlignUpVector() {
        //TODO: I'm sure all this math could simplify eventually...
        Vector3D newAxis = HM.UpVector(vec);
        QuaternionD newBasis = QuaternionD.FromToRotation(newAxis, Vector3D.up);
        QuaternionD twist = HM.SwingTwist(gyr, newAxis);
        gyr = newBasis * twist;
    }

    //Spherical linear interpolation
    public static GyroVectorD Slerp(GyroVectorD a, GyroVectorD b, float t) {
        return new GyroVectorD(Vector3D.Lerp(a.vec, b.vec, t), QuaternionD.Lerp(a.gyr, b.gyr, t));
    }
    public static GyroVectorD SlerpReverse(GyroVectorD a, GyroVectorD b, float t) {
        return Slerp(identity, b - a, t) + a;
    }

    //Human readable form
    public override string ToString() {
        return "(" + (vec.x).ToString("F9") + ", " +
               (vec.y).ToString("F9") + ", " +
               (vec.z).ToString("F9") + ") [" +
               (gyr.x).ToString("F9") + ", " +
               (gyr.y).ToString("F9") + ", " +
               (gyr.z).ToString("F9") + ", " +
               (gyr.w).ToString("F9") + "]";
    }
}
