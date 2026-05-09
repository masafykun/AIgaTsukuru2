using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody))]
public class PlayerController : MonoBehaviour
{
    [Header("Movement")]
    public float driveForce = 25f;
    public float turnSpeed  = 160f;
    public float maxSpeed   = 14f;

    [HideInInspector] public bool isLocalPlayer;

    private Rigidbody rb;
    private NetPlayerState latestRemoteState;
    private float sendTimer;
    const float SEND_RATE = 0.05f; // 20Hz

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
    }

    void FixedUpdate()
    {
        if (isLocalPlayer)
        {
            float v = 0f, h = 0f;

            // Keyboard
            var kb = Keyboard.current;
            if (kb != null)
            {
                v = (kb.wKey.isPressed ? 1f : 0f) - (kb.sKey.isPressed ? 1f : 0f);
                h = (kb.dKey.isPressed ? 1f : 0f) - (kb.aKey.isPressed ? 1f : 0f);
            }

            // Touch joystick (adds to keyboard, clamped)
            var tc = TouchInput.Instance;
            if (tc != null && tc.Direction.sqrMagnitude > 0.01f)
            {
                v = Mathf.Clamp(v + tc.Direction.y, -1f, 1f);
                h = Mathf.Clamp(h + tc.Direction.x, -1f, 1f);
            }

            transform.Rotate(Vector3.up, h * turnSpeed * Time.fixedDeltaTime);
            if (Mathf.Abs(v) > 0.01f && rb.linearVelocity.magnitude < maxSpeed)
                rb.AddForce(transform.forward * v * driveForce, ForceMode.Force);
        }
        else if (latestRemoteState != null)
        {
            // rb.MovePosition keeps kinematic body in physics simulation → can push the ball
            var targetPos = new Vector3(latestRemoteState.x, latestRemoteState.y, latestRemoteState.z);
            var targetRot = Quaternion.Euler(0, latestRemoteState.ry, 0);
            rb.MovePosition(Vector3.Lerp(rb.position, targetPos, 18f * Time.fixedDeltaTime));
            rb.MoveRotation(Quaternion.Slerp(rb.rotation, targetRot, 15f * Time.fixedDeltaTime));
        }
    }

    void Update()
    {
        if (!isLocalPlayer) return;
        sendTimer += Time.deltaTime;
        if (sendTimer >= SEND_RATE) { sendTimer = 0; SendState(); }
    }

    public void SetRemoteState(NetPlayerState s) => latestRemoteState = s;

    public void ResetToSpawn(Vector3 pos, Quaternion rot)
    {
        transform.SetPositionAndRotation(pos, rot);
        rb.linearVelocity  = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        latestRemoteState  = null;
    }

    void SendState()
    {
        var p = transform.position;
        var v = rb.linearVelocity;
        var msg = new NetPlayerState
        {
            x = p.x, y = p.y, z = p.z,
            ry = transform.eulerAngles.y,
            vx = v.x, vy = v.y, vz = v.z
        };
        GameNetworkManager.Instance?.Send(JsonUtility.ToJson(msg));
    }
}
