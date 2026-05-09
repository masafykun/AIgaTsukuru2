using System;
using UnityEngine;
using NativeWebSocket;

public class GameNetworkManager : MonoBehaviour
{
    public static GameNetworkManager Instance { get; private set; }

    public string serverUrl = "wss://example.com/soccer-ws";

    private WebSocket ws;

    public int PlayerId { get; private set; } = -1;
    public bool IsHost => PlayerId == 1;
    public bool IsReady { get; private set; }

    public event Action<int>           OnAssigned;
    public event Action                OnOpponentJoined;
    public event Action<NetPlayerState> OnPlayerState;
    public event Action<NetBallState>   OnBallState;
    public event Action<int>           OnGoal;
    public event Action                OnGameReset;
    public event Action                OnOpponentDisconnected;
    public event Action<string>        OnNickname;
    public event Action                OnRematch;

    void Awake()
    {
        if (Instance) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    async void Start()
    {
        ws = new WebSocket(serverUrl);
        ws.OnMessage += HandleMessage;
        ws.OnError   += e    => Debug.LogError("WS Error: " + e);
        ws.OnClose   += code => Debug.Log("WS Closed: " + code);
        await ws.Connect();
    }

    void Update()
    {
#if !UNITY_WEBGL || UNITY_EDITOR
        ws?.DispatchMessageQueue();
#endif
    }

    void HandleMessage(byte[] bytes)
    {
        var json = System.Text.Encoding.UTF8.GetString(bytes);
        var base_ = JsonUtility.FromJson<NetBase>(json);

        switch (base_.type)
        {
            case "assigned":
                var am = JsonUtility.FromJson<NetAssigned>(json);
                PlayerId = am.playerId;
                OnAssigned?.Invoke(PlayerId);
                break;
            case "opponent_joined":
                IsReady = true;
                OnOpponentJoined?.Invoke();
                break;
            case "pstate":
                OnPlayerState?.Invoke(JsonUtility.FromJson<NetPlayerState>(json));
                break;
            case "bstate":
                OnBallState?.Invoke(JsonUtility.FromJson<NetBallState>(json));
                break;
            case "goal":
                OnGoal?.Invoke(JsonUtility.FromJson<NetGoal>(json).scorer);
                break;
            case "reset":
                OnGameReset?.Invoke();
                break;
            case "opponent_disconnected":
                OnOpponentDisconnected?.Invoke();
                break;
            case "nickname":
                OnNickname?.Invoke(JsonUtility.FromJson<NetNickname>(json).name);
                break;
            case "rematch":
                OnRematch?.Invoke();
                break;
        }
    }

    public async void Send(string json)
    {
        if (ws?.State == WebSocketState.Open)
            await ws.SendText(json);
    }

    async void OnDestroy()
    {
        if (ws != null) await ws.Close();
    }
}

// ---- Message structs ----

[Serializable] public class NetBase     { public string type; }
[Serializable] public class NetAssigned { public string type; public int playerId; }
[Serializable] public class NetGoal     { public string type; public int scorer; }
[Serializable] public class NetNickname { public string type = "nickname"; public string name; }

[Serializable]
public class NetPlayerState
{
    public string type = "pstate";
    public float x, y, z, ry, vx, vy, vz;
}

[Serializable]
public class NetBallState
{
    public string type = "bstate";
    public float x, y, z, vx, vy, vz;
}

[Serializable]
public class NetGoalScored
{
    public string type = "goal";
    public int scorer;
}

[Serializable] public class NetRematch { public string type = "rematch"; }
