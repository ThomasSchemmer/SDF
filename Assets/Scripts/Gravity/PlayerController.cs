using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    public float maxVelocity = 10;
    public float maxAcceleration = 10;
    public float maxAirAcceleration = 10f;
    public float maxJumpHeight = 2f;
    public float maxAirJumps = 1f;
    public float maxGroundAngle = 25f;
    public float maxStairAngle = 50;
    public float maxSnapSpeed = 100;
    public Vector2 turnRate = new Vector2(25, 5);
    public Vector2 turnOffset = new Vector2(0, 10);
    public Camera orbitCamera;

    private Vector3 velocity = new Vector3();
    private Vector3 desiredVelocity;
    private Vector2 desiredAngles;
    private Quaternion rotation;
    private Quaternion desiredRotation;
    private float minGroundDotProduct, minStairDotProduct;

    private bool desiredJump;
    private int groundContactCount = 0, steepContactCount;
    private bool IsOnGround => groundContactCount > 0;
    private bool IsOnSteep => steepContactCount > 0;

    private int jumpPhase = 0;
    private Vector3 contactNormal, steepNormal;
    private int stepsSinceLastGround = 0, stepsSinceLastJump = 0;

    //gravity related
    Vector3 upAxis, rightAxis, forwardAxis;
    Vector3 gravity;
    private Transform focus;

    private Rigidbody rb;
    private Vector2 screenSize;
    private int worldLayerMask;

    private void Awake() {
        rb = GetComponent<Rigidbody>();
        rb.useGravity = false;
        desiredRotation = rb.rotation;
        focus = this.transform.GetChild(0);
        minGroundDotProduct = Mathf.Cos(maxGroundAngle * Mathf.Deg2Rad);
        minStairDotProduct = Mathf.Cos(maxStairAngle * Mathf.Deg2Rad);

        screenSize = GetMainGameViewSize();
        worldLayerMask = LayerMask.GetMask("World");
    }

    private void Start() {
        desiredAngles = new Vector2();
    }

    private void FixedUpdate() {
        gravity = CustomGravity.GetGravity(transform.position, out upAxis);
        UpdateState();
        AdjustRotation();
        AdjustVelocity();
        if (desiredJump) {
            desiredJump = false;
            Jump();
        }

        velocity += gravity * Time.deltaTime;
        rb.velocity = velocity;
        rb.rotation = rotation.normalized;
        ClearState();
       
    }

    Vector3 debug = Vector3.zero;
    private void OnDrawGizmos() {
        Gizmos.DrawLine(transform.position, transform.position + debug * 2);
        Color old = Gizmos.color;
        Gizmos.color = Color.red;
        Gizmos.DrawLine(transform.position, transform.position + rightAxis * 2);
        Gizmos.color = Color.blue;
        Gizmos.DrawLine(transform.position, transform.position + forwardAxis * 2);
        Gizmos.color = Color.green;
        Gizmos.DrawLine(transform.position, transform.position + upAxis * 2);
        Gizmos.color = old;
    }

    void Update() {
        rightAxis = ProjectDirectionOnPlane(transform.right, upAxis);
        forwardAxis = ProjectDirectionOnPlane(transform.forward, upAxis);
        CalculateRotation();
            
        Vector3 playerInput = new Vector3();
        playerInput.x = Input.GetAxis("Horizontal");
        playerInput.z = Input.GetAxis("Vertical");
        playerInput = Vector3.ClampMagnitude(playerInput, 1);
        desiredVelocity = playerInput * maxVelocity;
        //map current mouse position into [-1..1] range
        desiredAngles = ((Vector2)Input.mousePosition - screenSize / 2f) / (screenSize / 2f);
        if (Mathf.Abs(desiredAngles.x) < 0.1f)
            desiredAngles.x = 0;
        desiredAngles.x = Mathf.Clamp(desiredAngles.x, -1, 1);
        desiredAngles.y = Mathf.Clamp(desiredAngles.y, -1, 1);
        desiredAngles = desiredAngles * turnRate + turnOffset;
       
        desiredJump |= Input.GetButtonDown("Jump");
    }

    void Jump() {
        Vector3 jumpDirection;
        if (IsOnGround) {
            jumpDirection = (contactNormal+ velocity * 0.2f).normalized;
        }else if (IsOnSteep) {
            jumpDirection = (steepNormal + upAxis).normalized;
            jumpPhase = 0;
        }else if(maxAirJumps > 0 && jumpPhase <= maxAirJumps) {
            jumpPhase = jumpPhase == 0 ? 1 : jumpPhase;
            jumpDirection = (contactNormal + velocity * 0.2f).normalized; ;
        } else {
            return;
        }

        float jumpSpeed = Mathf.Sqrt(2f * gravity.magnitude * maxJumpHeight);
        float alignedSpeed = Vector3.Dot(velocity, contactNormal);  //new Vector3(0, jumpDirection.y, 0)
        if (alignedSpeed > 0f)
            jumpSpeed = Mathf.Max(jumpSpeed - alignedSpeed, 0f);
        velocity = jumpSpeed * jumpDirection;
        jumpPhase++;
        stepsSinceLastJump = 0;
    }

    private void OnCollisionStay(Collision collision) {
        EvaluateCollision(collision);
    }
    private void OnCollisionEnter(Collision collision) {
        EvaluateCollision(collision);
    }


    private void EvaluateCollision(Collision coll) {
        float minDot = GetMinDot(coll.gameObject.layer);
        foreach(var contact in coll.contacts) {
            Vector3 normal = contact.normal;
            float upDot = Vector3.Dot(upAxis, normal);
            if(upDot >= minDot) {
                groundContactCount++;
                contactNormal += normal;
            }else if (upDot > -0.01f) {
                steepContactCount++;
                steepNormal += normal;
            }
        }
    }

    private Vector3 ProjectDirectionOnPlane(Vector3 direction, Vector3 normal) {
        return (direction - normal * Vector3.Dot(direction, normal)).normalized;
    }

    private void AdjustVelocity() {
        Vector3 xAxis = rightAxis;// ProjectOnDirectionPlane(rightAxis, contactNormal);
        Vector3 zAxis = forwardAxis;// ProjectOnDirectionPlane(forwardAxis, contactNormal);
        float currentX = Vector3.Dot(velocity, xAxis);
        float currentZ = Vector3.Dot(velocity, zAxis);

        float acc = IsOnGround ? maxAcceleration : maxAirAcceleration;
        float maxSpeedChange = acc * Time.deltaTime;

        float newx = Mathf.MoveTowards(currentX, desiredVelocity.x, maxSpeedChange);
        float newz = Mathf.MoveTowards(currentZ, desiredVelocity.z, maxSpeedChange);

        velocity += xAxis * (newx - currentX) + zAxis * (newz - currentZ);

    }

    private void AdjustRotation() {
        rotation = desiredRotation;
    }

    private void CalculateRotation() {
        RaycastHit first, second;
        bool raycasted = true;
        if (!Physics.Raycast(transform.position, -upAxis, out first, 1, worldLayerMask))
            raycasted = false;
        if(!Physics.Raycast(focus.position, -upAxis, out second, 1, worldLayerMask)) 
            raycasted = false;

        //Calculate orientation of player by using ground as plane on which mouse x movement rotates players forward vector
        Vector3 alignedForward = (IsOnGround & raycasted) ?
            (second.point - first.point).normalized : transform.forward;
        alignedForward = Quaternion.AngleAxis(desiredAngles.x, upAxis) * alignedForward;
        Quaternion playerLook = Quaternion.LookRotation(alignedForward, upAxis);


        float angle = Quaternion.Angle(rotation, playerLook);
        float smoothAlignment = 5;

        float t = 1f;
        if (angle > 0.01f) {
            t = Mathf.Pow(0.25f, Time.unscaledDeltaTime);
        }
        if (angle > smoothAlignment) {
            t = Mathf.Min(t, smoothAlignment / angle);
        }

        desiredRotation = Quaternion.Lerp(
                                rotation,
                                playerLook,
                                t
                            );
    }


    private bool SnapToGround() {
        if(stepsSinceLastGround > 1 || stepsSinceLastJump <= 10) 
            return false;
        if(!Physics.Raycast(rb.position, -upAxis, out RaycastHit hit, 1)) 
            return false;
        float upDot = Vector3.Dot(upAxis, hit.normal);
        if (upDot < GetMinDot(hit.collider.gameObject.layer))
            return false;
        float speed = velocity.magnitude;
        if (speed > maxSnapSpeed)
            return false;

        groundContactCount = 1;
        contactNormal = hit.normal;
        float dot = Vector3.Dot(velocity, hit.normal);
        if (dot > 0f) {
            velocity = (velocity - hit.normal * dot).normalized * speed;
        }
        return true;
    }

    private bool CheckSteepContacts() {
        if(steepContactCount > 1) {
            steepNormal.Normalize();
            float upDot = Vector3.Dot(upAxis, steepNormal);
            if(upDot >= minGroundDotProduct) {
                groundContactCount = 1;
                contactNormal = steepNormal;
                return true;
            }
        }
        return false;
    }

    private void UpdateState() {
        velocity = rb.velocity;
        rotation = rb.rotation;
        stepsSinceLastGround++;
        stepsSinceLastJump++;
        if (IsOnGround || SnapToGround() || CheckSteepContacts()) {
            jumpPhase = 0;
            stepsSinceLastGround = 0;
            if(stepsSinceLastJump > 1) {
                jumpPhase = 0;
            }
            if(groundContactCount > 1)
                contactNormal.Normalize();
        } else {
            contactNormal = upAxis;
        }
    }

    private void ClearState() {
        groundContactCount = 0;
        contactNormal = Vector3.zero;
        steepContactCount = 0;
        steepNormal = Vector3.zero;
    }

    private float GetMinDot(int layer) {
        return LayerMask.NameToLayer("Stair") == layer ? minStairDotProduct : minGroundDotProduct;
    }

    public Vector2 GetDesiredAngles() {
        return desiredAngles;
    }


    private static Vector2 GetMainGameViewSize() {
        if (Application.isEditor) {
            System.Type T = System.Type.GetType("UnityEditor.GameView,UnityEditor");
            System.Reflection.MethodInfo GetSizeOfMainGameView = T.GetMethod("GetSizeOfMainGameView", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
            System.Object Res = GetSizeOfMainGameView.Invoke(null, null);
            return (Vector2)Res;
        }
        return new Vector2(Screen.width, Screen.height);
    }

}
