using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Tests
{
    public class HMTest {
        //Additional assertions
        static void AssertEqual(Vector3 a, Vector3 b, float tolerance = 1e-4f) {
            Assert.AreEqual(a.x, b.x, tolerance);
            Assert.AreEqual(a.y, b.y, tolerance);
            Assert.AreEqual(a.z, b.z, tolerance);
        }
        static void AssertEqual(Vector3D a, Vector3D b, double tolerance = 1e-7) {
            Assert.AreEqual(a.x, b.x, tolerance);
            Assert.AreEqual(a.y, b.y, tolerance);
            Assert.AreEqual(a.z, b.z, tolerance);
        }
        static void AssertEqual(Quaternion a, Quaternion b, float tolerance = 1e-4f) {
            Assert.AreEqual(a.x, b.x, tolerance);
            Assert.AreEqual(a.y, b.y, tolerance);
            Assert.AreEqual(a.z, b.z, tolerance);
            Assert.AreEqual(a.w, b.w, tolerance);
        }
        static void AssertEqual(QuaternionD a, QuaternionD b, double tolerance = 1e-7) {
            Assert.AreEqual(a.x, b.x, tolerance);
            Assert.AreEqual(a.y, b.y, tolerance);
            Assert.AreEqual(a.z, b.z, tolerance);
            Assert.AreEqual(a.w, b.w, tolerance);
        }
        static void AssertEqual(GyroVector a, GyroVector b, float tolerance = 1e-4f) {
            AssertEqual(a.vec, b.vec, tolerance);
            AssertEqual(a.gyr, b.gyr, tolerance);
        }
        static void AssertEqual(GyroVectorD a, GyroVectorD b, double tolerance = 1e-7) {
            AssertEqual(a.vec, b.vec, tolerance);
            AssertEqual(a.gyr, b.gyr, tolerance);
        }
        static string ToStr(Vector3 x) {
            return "(" + ((double)x.x).ToString("F9") + ", " +
                   ((double)x.y).ToString("F9") + ", " +
                   ((double)x.z).ToString("F9") + ")";
        }
        static string ToStr(Quaternion x) {
            return "[" + ((double)x.x).ToString("F9") + ", " +
                   ((double)x.y).ToString("F9") + ", " +
                   ((double)x.z).ToString("F9") + ", " +
                   ((double)x.w).ToString("F9") + "]";
        }

        [Test]
        public void TestConversions() {
            for (int i = 3; i <= 5; i++) {
                for (int j = 0; j < 2; ++j) {
                    HM.SetTileType(i);
                    bool useTanKHeight = (j == 1);

                    Vector3 u = new Vector3(0.2f, 0.7f, 0.6f);
                    Vector3 n = new Vector3(0.5f, -0.2f, 0.3f);
                    Vector3 k = HM.UnitToKlein(u, useTanKHeight);
                    Vector3 p = HM.KleinToPoincare(k);

                    //Test inverse conversions
                    AssertEqual(k, HM.PoincareToKlein(p));
                    AssertEqual(u, HM.KleinToUnit(k, useTanKHeight));

                    //Test combined transforms
                    AssertEqual(p, HM.UnitToPoincare(u, useTanKHeight));
                    AssertEqual(u, HM.PoincareToUnit(p, useTanKHeight));

                    //Test vector conversions
                    AssertEqual(n.normalized, HM.KleinToPoincare(k, HM.PoincareToKlein(p, n)));
                }
            }
        }

        [Test]
        public void TestTanK() {
            float testVal = 0.876f;
            for (int i = 3; i <= 5; i++) {
                HM.SetTileType(i);
                Assert.AreEqual(0.0f, HM.TanK(0.0f), 1e-5f);
                Assert.AreEqual(0.0f, HM.AtanK(0.0f), 1e-5f);
                Assert.AreEqual(testVal, HM.AtanK(HM.TanK(testVal)), 1e-5f);
            }
        }

        [Test]
        public void TestAddition() {
            GyroVector gv = new GyroVector(new Vector3(0.1f, 0.3f, 0.5f), Quaternion.Euler(20.0f, -40.0f, 30.0f));
            Vector3 a = new Vector3(-0.5f, 0.2f, -0.4f);
            Quaternion q = Quaternion.Euler(90.0f, -10.0f, -40.0f);

            for (int i = 3; i <= 5; i++) {
                HM.SetTileType(i);
                AssertEqual(gv + a, gv + new GyroVector(a));
                AssertEqual(a + gv, new GyroVector(a) + gv);
                AssertEqual(gv + q, gv + new GyroVector(q));
                AssertEqual(q + gv, new GyroVector(q) + gv);
                AssertEqual(gv.Point(), (q + gv).Point());
            }
        }

        [Test]
        public void TestAdditionD() {
            GyroVectorD gv = new GyroVectorD(new Vector3D(0.1, 0.3, 0.5), (QuaternionD)Quaternion.Euler(20.0f, -40.0f, 30.0f));
            Vector3D a = new Vector3D(-0.5, 0.2, -0.4);
            QuaternionD q = (QuaternionD)Quaternion.Euler(90.0f, -10.0f, -40.0f);

            for (int i = 3; i <= 5; i++) {
                HM.SetTileType(i);
                AssertEqual(gv + a, gv + new GyroVectorD(a));
                AssertEqual(a + gv, new GyroVectorD(a) + gv);
                AssertEqual(gv + q, gv + new GyroVectorD(q));
                AssertEqual(q + gv, new GyroVectorD(q) + gv);
            }
        }

        [Test]
        public void TestGyroVectorInverse() {
            GyroVector a = new GyroVector(new Vector3(0.1f, 0.3f, 0.5f), Quaternion.Euler(20.0f, -40.0f, 30.0f));
            GyroVector b = new GyroVector(new Vector3(-0.5f, 0.2f, -0.4f), Quaternion.Euler(90.0f, -10.0f, -40.0f));
            GyroVector c = new GyroVector(new Vector3(-0.2f, 0.0f, 0.6f), Quaternion.Euler(10.0f, -15.0f, 0.0f));
            Vector3 d = new Vector3(0.3f, -0.3f, 0.2f);

            for (int i = 3; i <= 5; i++) {
                HM.SetTileType(i);
                AssertEqual(GyroVector.identity, -GyroVector.identity);
                AssertEqual(GyroVector.identity, a + (-a));
                AssertEqual(GyroVector.identity, a - a);
                AssertEqual(-(a + b), (-b) + (-a));
                AssertEqual(a - b, a + (-b));
                AssertEqual(b, (-a) + (a + b));
                AssertEqual(a - d, a + (-d));
                AssertEqual(a, (a + d) - d);
                AssertEqual(a, (a - d) + d);
                AssertEqual(d, ((d + a) - a).vec);
                AssertEqual(d, ((d - a) + a).vec);
                AssertEqual(Quaternion.identity, ((d + a) - a).gyr);
                AssertEqual(Quaternion.identity, ((d - a) + a).gyr);

                //Test associativity
                AssertEqual((a + b) + c, a + (b + c));
            }
        }

        [Test]
        public void TestGyroVectorInverseD() {
            GyroVectorD a = new GyroVectorD(new Vector3D(0.1, 0.3f, 0.5), (QuaternionD)Quaternion.Euler(20.0f, -40.0f, 30.0f));
            GyroVectorD b = new GyroVectorD(new Vector3D(-0.5, 0.2f, -0.4), (QuaternionD)Quaternion.Euler(90.0f, -10.0f, -40.0f));
            GyroVectorD c = new GyroVectorD(new Vector3D(-0.2, 0.0, 0.6), (QuaternionD)Quaternion.Euler(10.0f, -15.0f, 0.0f));
            Vector3D d = new Vector3D(0.3, -0.3, 0.2);

            for (int i = 3; i <= 5; i++) {
                HM.SetTileType(i);
                AssertEqual(GyroVectorD.identity, -GyroVectorD.identity);
                AssertEqual(GyroVectorD.identity, a + (-a));
                AssertEqual(GyroVectorD.identity, a - a);
                AssertEqual(a - b, a + (-b));
                AssertEqual(b, (-a) + (a + b));
                AssertEqual(a - d, a + (-d));
                AssertEqual(a, (a + d) - d);
                AssertEqual(a, (a - d) + d);
                AssertEqual(d, ((d + a) - a).vec);
                AssertEqual(d, ((d - a) + a).vec);
                AssertEqual(QuaternionD.identity, ((d + a) - a).gyr);
                AssertEqual(QuaternionD.identity, ((d - a) + a).gyr);

                //Test associativity
                AssertEqual((a + b) + c, a + (b + c));
            }
        }

        [Test]
        public void TestGyroVectorMultiply() {
            GyroVector g = new GyroVector(new Vector3(0.1f, 0.3f, 0.5f), Quaternion.Euler(20.0f, -40.0f, 30.0f));
            Vector3 a = new Vector3(0.3f, -0.4f, 0.2f);

            for (int i = 3; i <= 5; i++) {
                HM.SetTileType(i);
                AssertEqual(g * a, (a + g) * Vector3.zero);
                AssertEqual(g * a, (a + g).Point());
            }
        }

        [Test]
        public void TestGyroVectorMultiplyD() {
            GyroVectorD g = new GyroVectorD(new Vector3D(0.1, 0.3, 0.5), (QuaternionD)Quaternion.Euler(20.0f, -40.0f, 30.0f));
            Vector3D a = new Vector3D(0.3, -0.4, 0.2);

            for (int i = 3; i <= 5; i++) {
                HM.SetTileType(i);
                AssertEqual(g * a, (a + g) * Vector3D.zero);
                AssertEqual(g * a, (a + g).Point());
            }
        }

        [Test]
        public void TestMobiusOperations() {
            Vector3 a = new Vector3(-0.2f, 0.5f, -0.1f);
            Vector3 b = new Vector3(0.1f, 0.2f, 0.3f);
            Vector3 c = new Vector3(0.4f, 0.0f, 0.4f);
            Vector3 x = new Vector3(0.7f, 0.1f, 0.2f);
            Quaternion q = Quaternion.Euler(20.0f, 50.0f, 80.0f);

            for (int i = 3; i <= 5; i++) {
                HM.SetTileType(i);

                //Basic Gyration Identities
                AssertEqual(Quaternion.identity, HM.MobiusGyr(Vector3.zero, a));
                AssertEqual(Quaternion.identity, HM.MobiusGyr(a, Vector3.zero));
                AssertEqual(Quaternion.identity, HM.MobiusGyr(a, a));
                AssertEqual(Quaternion.identity, HM.MobiusGyr(a, b) * HM.MobiusGyr(b, a));

                //Basic Addition Identities
                AssertEqual(a, HM.MobiusAdd(a, Vector3.zero));
                AssertEqual(a, HM.MobiusAdd(Vector3.zero, a));

                //Rotation invariance
                AssertEqual(q * HM.MobiusAdd(a, b), HM.MobiusAdd(q * a, q * b));
                AssertEqual(q * HM.MobiusGyr(a, b) * Quaternion.Inverse(q), HM.MobiusGyr(q * a, q * b));

                //Combination Identities
                AssertEqual(HM.MobiusAdd(a, b), HM.MobiusGyr(a, b) * HM.MobiusAdd(b, a));
                AssertEqual(HM.MobiusGyr(a, b), HM.MobiusGyr(HM.MobiusAdd(a, b), b));
                AssertEqual(HM.MobiusGyr(a, b), HM.MobiusGyr(a, HM.MobiusAdd(b, a)));

                //Associativity identities
                AssertEqual(HM.MobiusAdd(a, HM.MobiusAdd(b, x)), HM.MobiusAdd(HM.MobiusAdd(a, b), HM.MobiusGyr(a, b) * x));
                AssertEqual(HM.MobiusAdd(a, HM.MobiusAdd(b, x)), HM.MobiusGyr(a, b) * HM.MobiusAdd(HM.MobiusAdd(b, a), x));
                AssertEqual(HM.MobiusAdd(HM.MobiusAdd(a, b), x), HM.MobiusAdd(a, HM.MobiusAdd(b, HM.MobiusGyr(b, a) * x)));

                //Multi-Composition
                AssertEqual(HM.MobiusAdd(a, HM.MobiusAdd(b, HM.MobiusAdd(c, x))),
                            HM.MobiusAdd(HM.MobiusAdd(a, HM.MobiusAdd(b, c)), HM.MobiusGyr(a, HM.MobiusAdd(b, c)) * HM.MobiusGyr(b, c) * x));
                AssertEqual(HM.MobiusAdd(a, HM.MobiusAdd(b, HM.MobiusAdd(c, x))),
                            HM.MobiusGyr(a, HM.MobiusAdd(b, c)) * HM.MobiusGyr(b, c) * HM.MobiusAdd(HM.MobiusAdd(c, HM.MobiusAdd(b, a)), x));
            }
        }

        [Test]
        public void TestCombinedAddGyr() {
            Vector3 a = new Vector3(-0.2f, 0.5f, -0.1f);
            Vector3 b = new Vector3(0.1f, 0.2f, 0.3f);

            for (int i = 3; i <= 5; i++) {
                HM.SetTileType(i);

                HM.MobiusAddGyr(a, b, out Vector3 sum, out Quaternion gyr);
                AssertEqual(HM.MobiusAdd(a, b), sum);
                AssertEqual(HM.MobiusGyr(b, a), gyr);
            }
        }

        [Test]
        public void TestWalkHyperbolic() {
            HM.SetTileType(5);

            //Start at the origin and walk along tiles in the hyperbolic plane
            GyroVector x = GyroVector.identity;
            x += new Vector3(0.0f, 0.0f, (float)HM.CELL_WIDTH);  //Up
            x += new Vector3((float)HM.CELL_WIDTH, 0.0f, 0.0f);  //Right
            x += new Vector3(0.0f, 0.0f, -(float)HM.CELL_WIDTH); //Down
            x += new Vector3(-(float)HM.CELL_WIDTH, 0.0f, 0.0f); //Left
            x += new Vector3(0.0f, 0.0f, (float)HM.CELL_WIDTH);  //Up

            //We should be back where we started but with a 90 degree rotation
            AssertEqual(Vector3.zero, x.vec);
            AssertEqual(Quaternion.Euler(0.0f, 90.0f, 0.0f), x.gyr);
        }

        [Test]
        public void TestWalkHyperbolicD() {
            HM.SetTileType(5);

            //Start at the origin and walk along tiles in the hyperbolic plane
            GyroVectorD x = GyroVectorD.identity;
            x += new Vector3D(0.0, 0.0, HM.CELL_WIDTH);  //Up
            x += new Vector3D(HM.CELL_WIDTH, 0.0, 0.0);  //Right
            x += new Vector3D(0.0, 0.0, -HM.CELL_WIDTH); //Down
            x += new Vector3D(-HM.CELL_WIDTH, 0.0, 0.0); //Left
            x += new Vector3D(0.0, 0.0, HM.CELL_WIDTH);  //Up

            //We should be back where we started but with a 90 degree rotation
            AssertEqual(Vector3D.zero, x.vec);
            AssertEqual((QuaternionD)Quaternion.Euler(0.0f, 90.0f, 0.0f), x.gyr);
        }

        [Test]
        public void TestWalkEuclidean() {
            HM.SetTileType(4);

            //Start at the origin and walk along tiles in the hyperbolic plane
            GyroVector x = GyroVector.identity;
            x += new Vector3(0.0f, 0.0f, (float)HM.CELL_WIDTH);  //Up
            AssertEqual(new Vector3(0.0f, 0.0f, (float)HM.CELL_WIDTH), x.vec);
            AssertEqual(Quaternion.identity, x.gyr);

            x += new Vector3((float)HM.CELL_WIDTH, 0.0f, 0.0f);  //Right
            AssertEqual(new Vector3((float)HM.CELL_WIDTH, 0.0f, (float)HM.CELL_WIDTH), x.vec);
            AssertEqual(Quaternion.identity, x.gyr);

            x += new Vector3(0.0f, 0.0f, -(float)HM.CELL_WIDTH); //Down
            AssertEqual(new Vector3((float)HM.CELL_WIDTH, 0.0f, 0.0f), x.vec);
            AssertEqual(Quaternion.identity, x.gyr);

            x += new Vector3(-(float)HM.CELL_WIDTH, 0.0f, 0.0f); //Left
            AssertEqual(Vector3.zero, x.vec);
            AssertEqual(Quaternion.identity, x.gyr);
        }

        [Test]
        public void TestWalkSpherical() {
            HM.SetTileType(3);

            //Start at the origin and walk along tiles in the spherical plane
            GyroVector x = GyroVector.identity;
            x += new Vector3(0.0f, 0.0f, (float)HM.CELL_WIDTH);  //Up
            x += new Vector3((float)HM.CELL_WIDTH, 0.0f, 0.0f);  //Right
            x += new Vector3(0.0f, 0.0f, -(float)HM.CELL_WIDTH); //Down

            //We should be back where we started but with a -90 degree rotation
            AssertEqual(Vector3.zero, x.vec);
            AssertEqual(Quaternion.Euler(0.0f, -90.0f, 0.0f), x.gyr);
        }

        [Test]
        public void TestAccuracy() {
            const int ITERS = 5;
            const float DELTA = 0.5f;
            HM.SetTileType(5);

            //Start at the origin and walk along tiles in the hyperbolic plane
            GyroVector x = GyroVector.identity;
            for (int i = 0; i < ITERS; ++i) {
                x += new Vector3(0.0f, 0.0f, DELTA);  //Up
                x += new Vector3(DELTA, 0.0f, 0.0f);  //Right
            }
            for (int i = 0; i < ITERS; ++i) {
                x -= new Vector3(DELTA, 0.0f, 0.0f);  //Left
                x -= new Vector3(0.0f, 0.0f, DELTA);  //Down
            }

            AssertEqual(Vector3.zero, x.vec, 1e-4f);
            AssertEqual(Quaternion.identity, x.gyr, 1e-4f);
        }

        [Test]
        public void TestAccuracyD() {
            const int ITERS = 5;
            const float DELTA = 0.5f;
            HM.SetTileType(5);

            //Start at the origin and walk along tiles in the hyperbolic plane
            GyroVectorD x = GyroVectorD.identity;
            for (int i = 0; i < ITERS; ++i) {
                x += new Vector3D(0.0f, 0.0f, DELTA);  //Up
                x += new Vector3D(DELTA, 0.0f, 0.0f);  //Right
            }
            for (int i = 0; i < ITERS; ++i) {
                x -= new Vector3D(DELTA, 0.0f, 0.0f);  //Left
                x -= new Vector3D(0.0f, 0.0f, DELTA);  //Down
            }

            AssertEqual(Vector3D.zero, x.vec, 1e-10);
            AssertEqual(QuaternionD.identity, x.gyr, 1e-10);
        }

        [Test]
        public void TestLimits() {
            HM.SetTileType(5);
            GyroVector a = new GyroVector(new Vector3(0.9999f, 0.0f, 0.0f));
            GyroVector b = new GyroVector(new Vector3(0.9998f, 0.0f, 0.0f));
            GyroVector x = a - b - a + b;
            AssertEqual(GyroVector.identity, x, 1e-3f);

            //Make sure identity on Mobius addition maintains EXACT original value.
            GyroVector c = new GyroVector(new Vector3(-0.283646286f, 0.0f, -0.956248403f), new Quaternion(0.0f, 0.973488510f, 0.0f, -0.228755936f));
            AssertEqual(c, c + Vector3.zero, 0.0f);
            AssertEqual(c, Vector3.zero + c, 0.0f);
            AssertEqual(c, c + GyroVector.identity, 0.0f);
            AssertEqual(c, GyroVector.identity + c, 0.0f);
        }

        [Test]
        public void TestProjectToPlane() {
            Vector3 a = new Vector3(0.9f, 0.01f, 0.43f);
            Quaternion q = Quaternion.Euler(20.0f, 50.0f, 80.0f);
            GyroVector gv;
            Vector3 k, proj;

            for (int i = 3; i <= 5; i++) {
                HM.SetTileType(i);

                //Basic sanity checks
                AssertEqual(Vector3.zero, new GyroVector(Vector3.zero).ProjectToPlane().Point());
                AssertEqual(Vector3.zero, new GyroVector(Vector3.up * 0.5f).ProjectToPlane().Point());
                AssertEqual(Vector3.zero, new GyroVector(Vector3.down * 0.5f).ProjectToPlane().Point());
                AssertEqual(Vector3.left * 0.5f, new GyroVector(Vector3.left * 0.5f).ProjectToPlane().Point());
                AssertEqual(Vector3.right * 0.5f, new GyroVector(Vector3.right * 0.5f).ProjectToPlane().Point());
                AssertEqual(Vector3.forward * 0.5f, new GyroVector(Vector3.forward * 0.5f).ProjectToPlane().Point());
                AssertEqual(Vector3.back * 0.5f, new GyroVector(Vector3.back * 0.5f).ProjectToPlane().Point());

                //Try projecting an arbitrary point
                gv = new GyroVector(a);
                k = HM.PoincareToKlein(gv.Point());
                k.y = 0.0f;
                proj = HM.KleinToPoincare(k);
                AssertEqual(proj, gv.ProjectToPlane().Point());
            }
        }

        [Test]
        public void TestDerivative() {
            GyroVector gv = new GyroVector(new Vector3(0.7f, 0.1f, 0.2f), Quaternion.Euler(20.0f, 50.0f, 80.0f));
            Vector3 pt = new Vector3(0.2f, -0.5f, 0.3f);
            Vector3 delta = new Vector3(-0.4f, 0.2f, -0.1f).normalized;

            Vector3 newDelta1 = (gv * (pt + delta * 1e-3f) - gv * pt).normalized;
            gv.TransformNormal(pt, delta, out Vector3 newPt, out Vector3 newDelta2);
            AssertEqual(newDelta1, newDelta2, 1e-3f);
        }

        [Test]
        public void TestEuclideanProperties() {
            //Make sure scales stay 1:1 in euclidean geometry
            HM.SetTileType(4);
            Assert.AreEqual(1.0f, HM.KLEIN_V, 1e-5f);
            Assert.AreEqual(2.0f, HM.CELL_WIDTH, 1e-5f);

            //In euclidean geometry, mobius addition should be identical to regular addition
            Vector3 a = new Vector3(1.0f, 2.0f, 3.0f);
            Vector3 b = new Vector3(-2.0f, 3.0f, -1.0f);
            AssertEqual(a + b, HM.MobiusAdd(a, b));
        }

        [Test]
        public void TestDistance() {
            Vector3 a = new Vector3(-0.2f, 0.5f, -0.1f);
            Vector3 b = new Vector3(0.1f, 0.2f, 0.3f);

            for (int i = 3; i <= 5; i++) {
                HM.SetTileType(i);

                float d1 = HM.MobiusAdd(a, -b).sqrMagnitude;
                float d2 = HM.MobiusDistSq(a, b);
                Assert.AreEqual(d1, d2, 1e-5f);
            }
        }

        [Test]
        public void TestUpVector() {
            Quaternion q = Quaternion.Euler(20.0f, 40.0f, 30.0f);
            Vector3 a = new Vector3(-0.3f, -0.6f, 0.2f);

            for (int i = 3; i <= 5; i++) {
                HM.SetTileType(i);

                AssertEqual(Vector3.up, HM.UpVector(Vector3.zero));

                Vector3 up = HM.UpVector(a);
                Assert.AreEqual(1.0f, up.magnitude, 1e-5f);
                Vector3 aPlusUp = HM.MobiusAdd(a, up * 0.8f);
                Vector3 aPlane = HM.ProjectToPlane(a);
                Vector3 aPlusUpPlane = HM.ProjectToPlane(aPlusUp);
                Assert.AreEqual(aPlane.x, aPlusUpPlane.x, 1e-5f);
                Assert.AreEqual(aPlane.z, aPlusUpPlane.z, 1e-5f);

                GyroVector gv = new GyroVector(a, q);
                Vector3 f = gv.gyr * Vector3.forward;
                Vector3 u = gv.gyr * Vector3.up;
                gv.AlignUpVector();
                Vector3 nf = Quaternion.Inverse(gv.gyr) * Vector3.forward;
                Vector3 nu = Quaternion.Inverse(gv.gyr) * Vector3.up;
                AssertEqual(HM.UpVector(gv.vec), nu);
            }
        }
    }
}
