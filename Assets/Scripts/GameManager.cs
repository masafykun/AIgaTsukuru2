using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using TMPro;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Players")]
    public PlayerController localPlayer;
    public PlayerController remotePlayer;

    [Header("Ball")]
    public BallSync ball;

    [Header("Camera")]
    public CameraFollow cameraFollow;

    [Header("Spawn Points")]
    public Transform spawnP1;
    public Transform spawnP2;
    public Transform ballSpawn;

    [Header("Goal Effects")]
    public GoalEffect goalEffectP1;
    public GoalEffect goalEffectP2;

    [Header("UI")]
    public TMP_Text scoreText;
    public TMP_Text statusText;
    public GameObject nicknamePanel;
    public TMP_InputField nicknameInput;

    [Header("Nickname Labels")]
    public TMP_Text localNicknameLabel;
    public TMP_Text remoteNicknameLabel;

    [Header("Win Screen")]
    public GameObject winOverlay;
    public TMP_Text   winText;
    public TMP_Text   winScoreText;
    public GameObject rematchButton;
    public TMP_Text   waitText;

    [Header("Effects")]
    public Image screenFlash;

    private int   scoreP1, scoreP2;
    private bool  gameActive;
    private bool  goalProcessed;
    private bool  isHostPlayer;
    private string localNickname = "Player";

    const int WIN_SCORE = 3;

    void Awake()
    {
        if (Instance) { Destroy(gameObject); return; }
        Instance = this;
    }

    void Start()
    {
        SetStatus("");
        if (nicknamePanel) nicknamePanel.SetActive(true);
        if (winOverlay)    winOverlay.SetActive(false);

        var net = GameNetworkManager.Instance;
        net.OnAssigned             += OnAssigned;
        net.OnOpponentJoined       += OnOpponentJoined;
        net.OnPlayerState          += s => remotePlayer.SetRemoteState(s);
        net.OnGoal                 += OnGoalReceived;
        net.OnGameReset            += OnResetReceived;
        net.OnRematch              += OnRematchReceived;
        net.OnOpponentDisconnected += () => SetStatus("Opponent disconnected");
        net.OnNickname             += name => { if (remoteNicknameLabel) remoteNicknameLabel.text = name; };
    }

    public void SubmitNickname()
    {
        var input = nicknameInput ? nicknameInput.text.Trim() : "";
        localNickname = input.Length > 0 ? input : "Player";
        if (localNicknameLabel) localNicknameLabel.text = localNickname;
        if (nicknamePanel) nicknamePanel.SetActive(false);
        SetStatus(GameNetworkManager.Instance.IsReady ? "" : "Connecting...");

        if (GameNetworkManager.Instance.IsReady)
            SendNickname();
    }

    void OnAssigned(int playerId)
    {
        bool host = playerId == 1;
        isHostPlayer = host;

        if (!host)
        {
            (localPlayer, remotePlayer)               = (remotePlayer, localPlayer);
            (localNicknameLabel, remoteNicknameLabel) = (remoteNicknameLabel, localNicknameLabel);
        }

        localPlayer.isLocalPlayer  = true;
        remotePlayer.isLocalPlayer = false;

        var remoteRb = remotePlayer.GetComponent<Rigidbody>();
        remoteRb.isKinematic = true;

        localPlayer.ResetToSpawn(
            host ? spawnP1.position : spawnP2.position,
            host ? spawnP1.rotation : spawnP2.rotation);

        remotePlayer.ResetToSpawn(
            host ? spawnP2.position : spawnP1.position,
            host ? spawnP2.rotation : spawnP1.rotation);

        if (cameraFollow) cameraFollow.target = localPlayer.transform;
        if (!host) ball.GetComponent<Rigidbody>().isKinematic = true;

        SetStatus(host ? "Waiting for opponent..." : "Waiting for host...");
    }

    void OnOpponentJoined()
    {
        gameActive = true;
        UpdateScoreUI();
        SetStatus("");
        SendNickname();
    }

    public void RegisterGoal(int scorer)
    {
        if (!GameNetworkManager.Instance.IsHost) return;
        if (!gameActive || goalProcessed) return;

        goalProcessed = true;
        ApplyGoal(scorer);
        GameNetworkManager.Instance.Send(JsonUtility.ToJson(new NetGoalScored { scorer = scorer }));
        Invoke(nameof(DoReset), 2.5f);
    }

    void OnGoalReceived(int scorer)
    {
        if (GameNetworkManager.Instance.IsHost) return;
        ApplyGoal(scorer);
    }

    void ApplyGoal(int scorer)
    {
        if (scorer == 1) { scoreP1++; goalEffectP1?.Play(); }
        else             { scoreP2++; goalEffectP2?.Play(); }

        UpdateScoreUI();

        var flashColor = scorer == 1
            ? new Color(0.3f, 0.5f, 1f, 0.6f)
            : new Color(1f, 0.3f, 0.3f, 0.6f);
        StartCoroutine(FlashScreen(flashColor));

        if (scoreP1 >= WIN_SCORE || scoreP2 >= WIN_SCORE)
        {
            gameActive = false;
            AudioManager.Instance?.PlayWin();
            ShowWinOverlay(scorer);
        }
        else
        {
            AudioManager.Instance?.PlayGoal();
        }
    }

    void ShowWinOverlay(int scorer)
    {
        SetStatus("");
        if (winOverlay == null) return;
        winOverlay.SetActive(true);

        if (winText)      winText.text      = scorer == 1 ? "★ PLAYER 1 WINS! ★" : "★ PLAYER 2 WINS! ★";
        if (winScoreText) winScoreText.text = $"{scoreP1}  -  {scoreP2}";
        if (rematchButton) rematchButton.SetActive(isHostPlayer);
        if (waitText)      waitText.gameObject.SetActive(!isHostPlayer);
    }

    public void DoRematch()
    {
        if (!isHostPlayer) return;
        scoreP1 = scoreP2 = 0;
        goalProcessed = false;
        gameActive    = true;
        if (winOverlay) winOverlay.SetActive(false);
        UpdateScoreUI();
        SetStatus("");
        localPlayer.ResetToSpawn(spawnP1.position,    spawnP1.rotation);
        remotePlayer.ResetToSpawn(spawnP2.position,   spawnP2.rotation);
        ball.ResetBall(ballSpawn.position);
        GameNetworkManager.Instance.Send(JsonUtility.ToJson(new NetRematch()));
    }

    void OnRematchReceived()
    {
        if (isHostPlayer) return;
        scoreP1 = scoreP2 = 0;
        gameActive = true;
        if (winOverlay) winOverlay.SetActive(false);
        UpdateScoreUI();
        SetStatus("");
        localPlayer.ResetToSpawn(spawnP2.position,  spawnP2.rotation);
        remotePlayer.ResetToSpawn(spawnP1.position, spawnP1.rotation);
        ball.ResetBall(ballSpawn.position);
    }

    void DoReset()
    {
        goalProcessed = false;
        localPlayer.ResetToSpawn(spawnP1.position, spawnP1.rotation);
        remotePlayer.ResetToSpawn(spawnP2.position, spawnP2.rotation);
        ball.ResetBall(ballSpawn.position);
        GameNetworkManager.Instance.Send("{\"type\":\"reset\"}");
    }

    void OnResetReceived()
    {
        if (GameNetworkManager.Instance.IsHost) return;
        localPlayer.ResetToSpawn(spawnP2.position, spawnP2.rotation);
        remotePlayer.ResetToSpawn(spawnP1.position, spawnP1.rotation);
    }

    IEnumerator FlashScreen(Color color)
    {
        if (screenFlash == null) yield break;
        screenFlash.color = color;
        float t = 0;
        while (t < 1f)
        {
            t += Time.deltaTime * 2.5f;
            screenFlash.color = new Color(color.r, color.g, color.b, Mathf.Lerp(color.a, 0f, t));
            yield return null;
        }
        screenFlash.color = Color.clear;
    }

    void SendNickname()
    {
        if (nicknamePanel != null && nicknamePanel.activeSelf) return;
        GameNetworkManager.Instance.Send(JsonUtility.ToJson(new NetNickname { name = localNickname }));
    }

    void UpdateScoreUI()
    {
        if (scoreText) scoreText.text = $"{scoreP1}  -  {scoreP2}";
    }

    void SetStatus(string msg)
    {
        if (statusText) statusText.text = msg;
    }
}
