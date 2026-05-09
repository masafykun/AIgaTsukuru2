using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class BallSync : MonoBehaviour
{
    private Rigidbody rb;
    private bool isHost;
    private NetBallState latestState;
    private float sendTimer;
    const float SEND_RATE = 0.05f; // 20Hz

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    void Start()
    {
        var net = GameNetworkManager.Instance;
        if (net == null) return;
        net.OnAssigned += id =>
        {
            isHost = (id == 1);
            if (!isHost)
                GetComponent<Collider>().isTrigger = true; // guest: visual only, host handles physics
        };
        net.OnBallState += s => latestState = s;
    }

    void FixedUpdate()
    {
        if (!isHost && latestState != null)
        {
            // Guest: kinematic sync toward host's ball position
            var targetPos = new Vector3(latestState.x, latestState.y, latestState.z);
            rb.MovePosition(Vector3.Lerp(rb.position, targetPos, 18f * Time.fixedDeltaTime));
            rb.linearVelocity = new Vector3(latestState.vx, latestState.vy, latestState.vz);
        }
    }

    void Update()
    {
        if (!isHost) return;

        sendTimer += Time.deltaTime;
        if (sendTimer < SEND_RATE) return;
        sendTimer = 0;

        var p = transform.position;
        var v = rb.linearVelocity;
        var msg = new NetBallState { x = p.x, y = p.y, z = p.z, vx = v.x, vy = v.y, vz = v.z };
        GameNetworkManager.Instance?.Send(JsonUtility.ToJson(msg));
    }

    public void ResetBall(Vector3 pos)
    {
        transform.position = pos;
        rb.linearVelocity  = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        latestState        = null;
    }
}
