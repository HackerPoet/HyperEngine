using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;

public class Player : MonoBehaviour {
    public const float HEAD_BOB_FREQ = 12.0f;
    public const float HEAD_BOB_MAX = 0.008f;
    public const float CAP_RADIUS = 0.55f; //Relative to height
    public const float GRAVITY = -4.0f; //Relative to height
    public const float MIN_WALK_SLOPE = 0.8f; //sin of the angle between normal and horizontal
    public const float COYOTE_TIME = 0.2f; //Seconds off the ground before jumping is not allowed
    public const float SENSITIVITY_LOOK = 1.2f;
    public const float MIN_SHAKE_RUMBLE = 0.001f;
    public const float VR_TURN_SNAP_ANGLE = 30.0f;
    public static readonly float[] LAG_LOOK = new float[4] { 0.001f, 0.025f, 0.05f, 0.1f }; //Half-life in seconds
    public static readonly float[] FOV_SCALE = new float[3] { 1.0f, 1.3f, 1.6f };

    public static int FOV_PREF = 0;           //FOV preference              (0=normal, 1=wide, 2=ultra-wide)
    public static int CAM_SMOOTHING_PREF = 2; //Camera smoothing preference (0=off, 1=low, 2=med, 3=high)
    public static bool ENABLE_HEAD_BOB = true;
    public static Quaternion vrControllerRot = Quaternion.identity;

    [System.NonSerialized] public float LOCKED_MAX_Y = 10.0f; //Degrees
    [System.NonSerialized] public float LOCKED_MAX_X = 10.0f; //Degrees
    [System.NonSerialized] public float LAG_MOVE = 0.05f;
    [System.NonSerialized] public Map map;
    public float height = 0.1f;
    public float walkingSpeed = 2.0f; //Relative to height
    public float jumpSpeed = 2.2f;    //Relative to height
    public Vector3 startHyper = new Vector3(0, 0, 0);
    public GameObject mapCam;
    public GameObject mainCamera;
    public bool headCenter = false;
    public bool freeFly = false;
    public bool ignoreColliders = false;
    public static Quaternion focusRot = Quaternion.identity;

    private List<MonoBehaviour> preCollisionCallbacks = new List<MonoBehaviour>();
    private float rotationX; //Degrees
    public float rotationY { get; private set; } //Degrees
    private float lockedRotationX; //Degrees
    private float lockedRotationY; //Degrees
    private float smoothRotationX; //Degrees
    private float smoothRotationY; //Degrees
    private float targetRotationX; //Degrees
    private float targetRotationY; //Degrees
    private Quaternion targetQuaternion = Quaternion.identity; //Free-fly only
    [System.NonSerialized] public Vector3 velocity;
    [System.NonSerialized] public float shakeStrength = 0.0f;
    private float prevShakeStrength = 0.0f;
    private float headBobState;
    private float timeSinceGrounded;
    [System.NonSerialized] public Quaternion xyQuaternion = Quaternion.identity;
    private Lockable lockedObj = null;
    private Vector3 forwardBias;
    private Vector3 flatForwardBias;
    private bool isLocked;
    private bool firstFrame = true;
    private float projCur;
    private float projTransition;
    private float projNew;
    private static readonly int projInterpID = Shader.PropertyToID("_Proj");
    [System.NonSerialized] public float projInterp;
    [System.NonSerialized] public Vector3 inputDelta;
    [System.NonSerialized] public Vector3 outputDelta;
    [System.NonSerialized] public bool isPushing = false;
    [System.NonSerialized] public bool onPlatform = false;
    [System.NonSerialized] public bool overrideCamHeight = false;
    [System.NonSerialized] public bool disableSmoothLook = false;
    public Vector3 inputDeltaXZ { get { return new Vector3(inputDelta.x, 0.0f, inputDelta.z); } }
    [System.NonSerialized] public float baseFOV = 50.0f;
    private bool vrSnapAngle = false;

    private void Awake() {
        //Initialize world position and rotation
        baseFOV = mainCamera.GetComponent<Camera>().fieldOfView;
        HyperObject.worldGV = new GyroVector(-startHyper);
        Debug.Assert(transform.localScale == Vector3.one);

        //Disable the player's head offset in VR since it will be added to the view matrix
        Vector3 origCamPos = transform.localPosition;
        if (UnityEngine.XR.XRSettings.enabled) {
            transform.localPosition = Vector3.zero;
        }
        if (headCenter) {
            transform.localPosition -= origCamPos;
        }

        //World builders should have earlier execution than player, so HM statics
        //should already be setup by this point.
        height *= HM.KLEIN_V / 0.5774f;

        //Find map in the scene
        map = FindObjectOfType<Map>(true);

        //Update the field of view based on preferences
        UpdateFOV();
    }

    void Start() {
        //Initialize identity rotations
        smoothRotationX = targetRotationX = rotationX = 0.0f;
        smoothRotationY = targetRotationY = rotationY = 0.0f;

        //Other initialization
        headBobState = 0.0f;
        velocity = Vector3.zero;
        projNew = projCur;
        projTransition = 0.0f;
        timeSinceGrounded = 100.0f;
    }

#if UNITY_EDITOR
    private void OnDrawGizmos() {
        if (Camera.current.name == "SceneCamera") {
            Gizmos.color = Color.blue;
            Gizmos.DrawWireSphere(BodyCollisionPoint(), CollisionRadius());
            if (!headCenter) {
                Gizmos.DrawWireSphere(HeadCollisionPoint(), CollisionRadius());
            }
        }
    }
#endif

    public Vector3 GetForwardBias(bool lookVertical) {
        return (lookVertical ? forwardBias : flatForwardBias);
    }

    public Vector3 HeadCollisionPoint() {
        if (headCenter) {
            return Vector3.zero;
        } else {
            return new Vector3(0.0f, height, 0.0f);
        }
    }

    public Vector3 BodyCollisionPoint() {
        if (headCenter) {
            return Vector3.zero;
        } else {
            return new Vector3(0.0f, CollisionRadius(), 0.0f);
        }
    }

    public float CollisionRadius() {
        return height * CAP_RADIUS;
    }

    public float ScaleFactor() {
        return height / HM.KLEIN_V;
    }

    void Update() {
        if (IsLocked()) {
            //Update raw rotations
            Vector2 lookXY = InputManager.GetAxes("Look", false, UnityEngine.XR.XRSettings.enabled);
            if (UnityEngine.XR.XRSettings.enabled) {
                LookVRSnap(lookXY.x);
            } else {
                lockedRotationY += lookXY.y * SENSITIVITY_LOOK;
                lockedRotationX += lookXY.x * SENSITIVITY_LOOK;
            }

            //Clamp raw rotations to valid ranges
            float lockedMinY = Mathf.Max(-LOCKED_MAX_Y, -90.0f - rotationY);
            float lockedMaxY = Mathf.Min(LOCKED_MAX_Y, 90.0f - rotationY);
            lockedRotationY = Mathf.Clamp(lockedRotationY, lockedMinY, lockedMaxY);
            lockedRotationX = Mathf.Clamp(lockedRotationX, -LOCKED_MAX_X, LOCKED_MAX_X);

            //Handle locked-on objects
            if (lockedObj) {
                LookAtImmediate(lockedObj.ObjectCenter());
            }

            //Apply locked look smoothing (time dependent)
            ModPi(ref rotationX, targetRotationX);
            float smooth_look_locked = 0.0f;
            if (!UnityEngine.XR.XRSettings.enabled && !disableSmoothLook) {
                smooth_look_locked = Mathf.Pow(2.0f, -Time.deltaTime / (LAG_LOOK[CAM_SMOOTHING_PREF] * 2.0f));
            }
            rotationX = rotationX * smooth_look_locked + targetRotationX * (1 - smooth_look_locked);
            rotationY = rotationY * smooth_look_locked + targetRotationY * (1 - smooth_look_locked);
        } else {
            //When unlocked, update colliders
            HyperObject.updateColliders = true;

            //Update projection
            if (HM.K == 0.0f) {
                projInterp = 0.0f;
            } else if (projTransition > 0.0f && projCur != projNew) {
                projTransition = Mathf.Max(projTransition - Time.deltaTime, 0.0f);
                float a = Mathf.SmoothStep(0.0f, 1.0f, projTransition);
                projInterp = projCur * a + projNew * (1 - a);
            } else {
                projCur = projNew;
                projInterp = projCur;
                if (InputManager.GetKeyDown("ProjChange") && map && map.IsMapOut()) {
                    projTransition = 1.0f;
                    projNew += 1.0f;
                    if (projNew >= 2.0f) {
                        projNew = -1.0f;
                    }
                }
            }
            Shader.SetGlobalFloat(projInterpID, projInterp);

            //Check if map needs to be toggled
            if (map && InputManager.GetKeyDown("MapToggle")) {
                map.TakeMapOut(!map.IsMapOut());
            }

            //Update if there was any locked rotation
            rotationX += lockedRotationX;
            rotationY += lockedRotationY;
            lockedRotationX = 0.0f;
            lockedRotationY = 0.0f;

            //Update raw rotations
            Vector2 lookXY = InputManager.GetAxes("Look", false, UnityEngine.XR.XRSettings.enabled);
            if (UnityEngine.XR.XRSettings.enabled) {
                LookVRSnap(lookXY.x);
            } else {
                rotationY += lookXY.y * SENSITIVITY_LOOK;
                rotationX += lookXY.x * SENSITIVITY_LOOK;
            }
        }

        //Clamp raw rotations to valid ranges
        rotationY = Mathf.Clamp(rotationY, -90.0f, 90.0f);
        while (rotationX > 180.0f) { rotationX -= 360.0f; }
        while (rotationX < -180.0f) { rotationX += 360.0f; }

        //Apply smoothing (time dependent)
        ModPi(ref smoothRotationX, rotationX + lockedRotationX);
        float clampedRotationY = Mathf.Clamp(rotationY + lockedRotationY, -90.0f, 90.0f);
        float smooth_look = Mathf.Pow(2.0f, -Time.deltaTime / LAG_LOOK[CAM_SMOOTHING_PREF]);
        smoothRotationX = smoothRotationX * smooth_look + (rotationX + lockedRotationX) * (1 - smooth_look);
        smoothRotationY = smoothRotationY * smooth_look + clampedRotationY * (1 - smooth_look);

        //Get the rotation you will be at next as a Quaternion
        Quaternion yQuaternion;
        Quaternion xQuaternion;
        if (freeFly) {
            if (IsLocked()) {
                yQuaternion = Quaternion.AngleAxis(smoothRotationY, Vector3.left);
                xQuaternion = Quaternion.AngleAxis(smoothRotationX, Vector3.up);
                xyQuaternion = Quaternion.Slerp(targetQuaternion, xyQuaternion, smooth_look);
                focusRot = xyQuaternion * xQuaternion * yQuaternion;
            } else {
                yQuaternion = Quaternion.AngleAxis(smoothRotationY * Time.deltaTime * 100.0f, Vector3.left);
                xQuaternion = Quaternion.AngleAxis(smoothRotationX * Time.deltaTime * 100.0f, Vector3.up);
                xyQuaternion *= xQuaternion * yQuaternion;
                rotationX = 0.0f;
                rotationY = 0.0f;
                focusRot = xyQuaternion;
            }
            xQuaternion = xyQuaternion;
        } else {
            yQuaternion = Quaternion.AngleAxis(smoothRotationY, Vector3.left);
            xQuaternion = Quaternion.AngleAxis(smoothRotationX, Vector3.up);
            xyQuaternion = xQuaternion * yQuaternion;
            focusRot = xyQuaternion;
        }
        if (!UnityEngine.XR.XRSettings.enabled) {
            mainCamera.transform.localRotation = focusRot;
        }

        //Always work to dampen movement, even when locked.
        float smooth_move = Mathf.Pow(2.0f, -Time.deltaTime / LAG_MOVE);
        if (freeFly) {
            velocity *= smooth_move;
        } else {
            velocity.x *= smooth_move;
            velocity.z *= smooth_move;
        }

        Vector3 displacement = Vector3.zero;
        if (!IsLocked()) {
            //Update how long the player has been on the ground
            timeSinceGrounded += Time.deltaTime;

            //Get keyboard or joystick input
            Vector2 dxy = InputManager.GetAxes("Move");
            inputDelta = Vector3.ClampMagnitude(new Vector3(dxy.x, 0.0f, dxy.y), 1.0f);

            //Orient to camera direction or controller direction in VR
            if (UnityEngine.XR.XRSettings.enabled) {
                inputDelta = vrControllerRot * inputDelta;
            } else {
                if (freeFly) {
                    inputDelta = xyQuaternion * inputDelta;
                } else {
                    inputDelta = xQuaternion * inputDelta;
                }
            }

            inputDelta *= walkingSpeed * height;
            velocity += inputDelta * (1 - smooth_move);
            if (!freeFly) {
                velocity.y += GRAVITY * height * Time.deltaTime;
            }
            Vector3 velocityXZ = new Vector3(velocity.x, 0.0f, velocity.z);
            inputDelta = HM.HyperTranslate(velocity * Time.deltaTime);

            //Allow other objects to resolve their own collisions first and/or
            //possibly alter player's input velocity.
            onPlatform = false;
            for (int i = 0; i < preCollisionCallbacks.Count; ++i) {
                preCollisionCallbacks[i].SendMessage("OnPreCollide", this);
            }

            //Do collisions manually since dynamic meshes don't behave well with Unity physics.
            var hit = IteratedCollide(inputDelta, CollisionRadius(), ignoreColliders ? 0 : 2);
            displacement = hit.displacement * 0.9f;
            Debug.Assert(!float.IsNaN(displacement.x));

            //Stop momentum in perpendicular directions
            Vector3 collisionShift = hit.displacement - inputDelta;
            if (!isPushing && Vector3.Dot(velocityXZ, collisionShift) < -float.Epsilon && hit.maxSinY < 0.5f) {
                Vector3 projVel = Vector3.Project(velocityXZ, collisionShift);
                velocity.x -= projVel.x;
                velocity.z -= projVel.z;
            }
            isPushing = false;

            if (freeFly) {
                //Ignore gravity, just apply the displacement directly
                outputDelta = displacement;
                GyroVectorD gv = HyperObject.worldGVD;
                gv -= (Vector3D)outputDelta;
                HyperObject.worldGVD = gv;
            } else {
                //Check if the player is grounded
                bool isGrounded = (HyperObject.worldGVD.vec.y >= 0.0 || hit.maxSinYGround >= MIN_WALK_SLOPE);
                if (isGrounded) {
                    timeSinceGrounded = 0.0f;
                    //Cancel y velocity if on ground
                    velocity.y = Mathf.Max(velocity.y, 0.0f);
                    if (!onPlatform) {
                        displacement.y = Mathf.Max(0.0f, displacement.y);
                    }
                }
                bool isRecentGrounded = (timeSinceGrounded < COYOTE_TIME);

                //Jump if allowable
                if (velocity.y <= 0.0f && isRecentGrounded && InputManager.GetKeyDown("Jump") && !IsLocked() && walkingSpeed > 0.0f) {
                    velocity.y = jumpSpeed * height;
                    isGrounded = false;
                }

                //Cancel y velocity if hitting head
                if (hit.minSinY < -0.1f && velocity.y > 0.0f) {
                    velocity.y = 0.0f;
                }

                //Map that world displacement to a hyperbolic one (in high precision since this only happens once per frame)
                outputDelta = displacement;
                GyroVectorD gv = HyperObject.worldGVD;
                gv -= (Vector3D)outputDelta;
                gv.vec.y = Math.Min(gv.vec.y, 0.0);
                gv.AlignUpVector();
                HyperObject.worldGVD = gv;
            }
        }

        //Apply head bobbing
        float headDelta = 0.0f;
        if (!UnityEngine.XR.XRSettings.enabled && ENABLE_HEAD_BOB) {
            float displacementTime = height * walkingSpeed * Time.deltaTime;
            if (displacementTime > 0.0f) {
                float normalized_speed = displacement.magnitude / displacementTime;
                float maxBobOffset = height * HEAD_BOB_MAX * normalized_speed;
                headDelta = maxBobOffset * Mathf.Sin(headBobState * HEAD_BOB_FREQ);
                if (normalized_speed > 0.25f && !freeFly) {
                    headBobState += Mathf.Min(1.0f, (normalized_speed - 0.25f) * 10.0f) * Time.deltaTime;
                } else {
                    headBobState = 0.0f;
                }
            }
        }

        //Rotate the held map
        if (mapCam && map) {
            Vector3 camRight = mainCamera.transform.right;
            float mapAng = Mathf.Rad2Deg * Mathf.Atan2(-camRight.z, camRight.x);
            mapCam.transform.localRotation = Quaternion.Euler(0.0f, mapAng, 0.0f);
        }

        //Update the Camera height for the skyline
        if (!headCenter && !overrideCamHeight) {
            HyperObject.camHeight = HM.TanK(height + headDelta);
        }

        //Update world look vector
        HyperObject.worldLook = mainCamera.transform.forward;

        //Update forward bias point for interactions
        Vector3 biasVector = new Vector3(0.0f, 0.0f, CollisionRadius());
        if (UnityEngine.XR.XRSettings.enabled) {
            Quaternion mainCamRot = mainCamera.transform.rotation;
            flatForwardBias = Quaternion.Euler(0.0f, mainCamRot.eulerAngles.y, 0.0f) * biasVector;
            forwardBias = mainCamRot * biasVector;
        } else {
            flatForwardBias = xQuaternion * biasVector;
            forwardBias = xyQuaternion * biasVector;
        }
    }

    private void LateUpdate() {
        //Update controller rumble
        float shake = (Time.deltaTime > 0.0f ? shakeStrength : 0.0f);
        if (shake > 0.0f) {
            HyperObject.shakeGVD = HyperObject.worldGVD + (Vector3D)(UnityEngine.Random.insideUnitSphere * shake);
            if (shake >= MIN_SHAKE_RUMBLE && prevShakeStrength < MIN_SHAKE_RUMBLE) {
                InputManager.Rumble(true);
            }
        }
        if (shake < MIN_SHAKE_RUMBLE && prevShakeStrength >= MIN_SHAKE_RUMBLE) {
            InputManager.Rumble(false);
        }
        prevShakeStrength = shake;
        HyperObject.isShaking = (shake > 0.0f);
    }

    public bool IsLocked() {
        return isLocked;
    }

    public bool IsGrounded() {
        return timeSinceGrounded == 0.0f;
    }

    public void LookAt(Lockable lockable, bool lookBack=true) {
        if (UnityEngine.XR.XRSettings.enabled && lockable != null && lockable != lockedObj) {
            LookAtImmediate(lockable.ObjectCenter());
            SnapVRToTarget(60.0f);
        }
        lockedObj = lockable;
        if (lockedObj && lookBack) {
            lockedObj.PlayerLook(this);
        }
    }

    private void SnapVRToTarget(float minAngle) {
        Vector3 camForward = mainCamera.transform.forward;
        targetRotationX = Mathf.Clamp(targetRotationX, -179.0f, 179.0f);
        if (freeFly) {
            Vector3 targetForward = targetQuaternion * Vector3.forward;
            targetForward.y = 0.0f;
            if (Vector3.Angle(camForward, targetForward) >= minAngle) {
                transform.localRotation = Quaternion.FromToRotation(camForward, targetForward) * transform.localRotation;
            }
            transform.localRotation = Quaternion.LookRotation(transform.localRotation * Vector3.forward);
        } else {
            Vector3 targetForward = Quaternion.AngleAxis(targetRotationX, Vector3.up) * Vector3.forward;
            camForward.y = 0.0f;
            if (camForward.sqrMagnitude > 1e-6f && targetForward.sqrMagnitude > 1e-6f && Vector3.Angle(camForward, targetForward) >= minAngle) {
                transform.localRotation = Quaternion.FromToRotation(camForward, targetForward) * transform.localRotation;
            }
        }
    }

    public void LookAtImmediate(Vector3 trueDelta, float yAngleBias = 6.0f) {
        //Look at the locked-on object if applicable
        trueDelta = HM.MobiusAdd(-HeadCollisionPoint(), trueDelta);
        if (freeFly) {
            targetQuaternion = Quaternion.FromToRotation(Vector3.forward, trueDelta);
            targetRotationX = 0.0f;
            targetRotationY = 0.0f;
        } else {
            targetRotationX = Mathf.Atan2(trueDelta.x, trueDelta.z) * Mathf.Rad2Deg;
            targetRotationY = Mathf.Rad2Deg * Mathf.Asin(trueDelta.normalized.y) - yAngleBias;
        }

        //Remove all velocity to prevent jolt after event finishes.
        velocity = Vector3.zero;
    }

    public void SnapLockedRotation() {
        rotationX = targetRotationX;
        rotationY = targetRotationY;
        smoothRotationX = targetRotationX;
        smoothRotationY = targetRotationY;
        lockedRotationX = 0.0f;
        lockedRotationY = 0.0f;
    }

    public void Lock() {
        //When locked, don't update colliders
        HyperObject.updateColliders = false;

        isLocked = true;
        LOCKED_MAX_X = 10.0f;
        LOCKED_MAX_Y = 10.0f;
        lockedRotationX = 0.0f;
        lockedRotationY = 0.0f;
        targetRotationX = rotationX;
        targetRotationY = rotationY;
        targetQuaternion = xyQuaternion;
        if (map) {
            map.TakeMapOut(false);
        }
    }

    public void Unlock() {
        if (lockedObj) {
            lockedObj.PlayerUnlook(this);
            lockedObj = null;
        }
        if (freeFly) {
            Quaternion yQuaternion = Quaternion.AngleAxis(smoothRotationY, Vector3.left);
            Quaternion xQuaternion = Quaternion.AngleAxis(smoothRotationX, Vector3.up);
            xyQuaternion *= xQuaternion * yQuaternion;
            smoothRotationX = 0.0f;
            smoothRotationY = 0.0f;
            lockedRotationX = 0.0f;
            lockedRotationY = 0.0f;
            rotationX = 0.0f;
            rotationY = 0.0f;
        }
        isLocked = false;
    }

    public void SetRotation(float lookX, float lookY) {
        rotationX = lookX;
        rotationY = lookY;
        targetRotationX = rotationX;
        targetRotationY = rotationY;

        //Always snap in VR when doing a manual SetRotation
        if (UnityEngine.XR.XRSettings.enabled) {
            SnapVRToTarget(0.0f);
        }
    }

    public float SmoothRotationX() { return smoothRotationX; }

    //Places the first value in a range that can be interpolated with the second value
    private static void ModPi(ref float a, float b) {
        if (a - b > 180.0f) {
            a -= 360.0f;
        } else if (a - b < -180.0f) {
            a += 360.0f;
        }
    }

    public static float AngleToPlayer(HyperObject ho) {
        Vector3 pos = HM.UnitToPoincare(ho.transform.position, ho.useTanKHeight);
        ho.composedGV.TransformNormal(pos, ho.transform.forward, out Vector3 trueDelta, out Vector3 normal);
        normal.y = 0.0f; trueDelta.y = 0.0f;
        return Vector3.SignedAngle(normal, trueDelta, Vector3.up);
    }

    public WCollider.Hit IteratedCollide(Vector3 inDelta, float r, int iters) {
        Vector3 p1 = BodyCollisionPoint();
        Vector3 p2 = HeadCollisionPoint();
        WCollider.Hit hit = new WCollider.Hit();
        hit.displacement = inDelta;

        //Don't interact with colliders on the very first frame.
        //Colliders need the composedGV updated from LateUpdate first.
        if (firstFrame) {
            firstFrame = false;
            return hit;
        }

        for (int i = 0; i < iters; ++i) {
            //Attempt collisions with body and head then combine results
            WCollider.Hit bodyHit = WCollider.Collide(p1 + hit.displacement, r);
            hit.displacement += bodyHit.displacement;
            hit.maxSinY = Mathf.Max(hit.maxSinY, bodyHit.maxSinY);
            hit.maxSinYGround = Mathf.Max(hit.maxSinYGround, bodyHit.maxSinYGround);

            //If body is centered on the head, then the player is just a big sphere,
            //and we only need one collision check.
            if (headCenter) {
                hit.minSinY = Mathf.Min(hit.minSinY, bodyHit.minSinY);
                //End loop early if there were no collisions
                if (bodyHit.displacement.sqrMagnitude == 0.0f) {
                    break;
                }
            } else {
                //Also attempt collisions with the head and combine the results
                WCollider.Hit headHit = WCollider.Collide(p2 + hit.displacement, r);
                hit.displacement += headHit.displacement;
                hit.minSinY = Mathf.Min(hit.minSinY, headHit.minSinY);
                //End loop early if there were no collisions
                if (bodyHit.displacement.sqrMagnitude == 0.0f && headHit.displacement.sqrMagnitude == 0.0f) {
                    break;
                }
            }
        }
        return hit;
    }

    public void AddPreCollision(MonoBehaviour obj) {
        preCollisionCallbacks.Add(obj);
    }
    public void RemovePreCollision(MonoBehaviour obj) {
        preCollisionCallbacks.Remove(obj);
    }

    public void UpdateFOV() {
        if (!UnityEngine.XR.XRSettings.enabled) {
            mainCamera.GetComponent<Camera>().fieldOfView = baseFOV * FOV_SCALE[FOV_PREF];
        }
    }

    //Use the controller's look d-pad to change looking direction with snapping
    private void LookVRSnap(float angleX) {
        if (!vrSnapAngle && Mathf.Abs(angleX) > 0.5f) {
            float snapAng = Mathf.Sign(angleX) * VR_TURN_SNAP_ANGLE;
            transform.localRotation = Quaternion.Euler(0.0f, snapAng, 0.0f) * transform.localRotation;
            vrSnapAngle = true;
        } else if (vrSnapAngle && Mathf.Abs(angleX) < 0.1f) {
            vrSnapAngle = false;
        }
    }

    //Make sure controller always stops vibrating when player is unloaded
    private void OnDisable() {
        InputManager.Rumble(false);
    }
}
