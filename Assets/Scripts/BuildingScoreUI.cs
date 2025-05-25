using UnityEngine;
using TMPro;
using Photon.Pun;
using UnityEngine.UI;

public class BuildingScoreUI : MonoBehaviourPunCallbacks
{
    [Header("Score UI")]
    [SerializeField] private TextMeshProUGUI scoreText;
    
    [Header("Timer UI")]
    [SerializeField] private TextMeshProUGUI timerText;
    [SerializeField] private float gameDuration = 60f;
    
    [Header("Game Over UI")]
    [SerializeField] private GameObject gameOverPanel;
    [SerializeField] private TextMeshProUGUI resultText;
    [SerializeField] private Button returnToLobbyButton;

    private MapController mapController;
    private float currentTime;
    private bool isGameActive = false;

    private int myBuildingCount = 0;
    private int enemyBuildingCount = 0;

    void Start()
    {
        mapController = FindObjectOfType<MapController>();
        if (mapController == null)
        {
            Debug.LogError("MapController를 찾을 수 없습니다!");
            return;
        }

        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(false);
        }

        if (returnToLobbyButton != null)
        {
            returnToLobbyButton.onClick.AddListener(ReturnToLobby);
        }

        StartGame();
        UpdateScoreUI();
    }

    void StartGame()
    {
        currentTime = gameDuration;
        isGameActive = true;
        if (PhotonNetwork.IsMasterClient)
        {
            photonView.RPC("SyncGameStart", RpcTarget.All);
        }
    }

    [PunRPC]
    void SyncGameStart()
    {
        currentTime = gameDuration;
        isGameActive = true;
    }

    void Update()
    {
        if (!isGameActive) return;

        currentTime = Mathf.Max(0f, currentTime - Time.deltaTime);
        UpdateTimerDisplay();

        if (currentTime <= 0f)
        {
            currentTime = 0f;  // 0으로 고정
            isGameActive = false;
            if (PhotonNetwork.IsMasterClient)
            {
                photonView.RPC("EndGame", RpcTarget.All);
            }
        }
    }

    void UpdateTimerDisplay()
    {
        if (timerText != null)
        {
            if (!isGameActive && currentTime <= 0f)
            {
                timerText.text = "00:00";
            }
            else
            {
                int minutes = Mathf.FloorToInt(currentTime / 60);
                int seconds = Mathf.FloorToInt(currentTime % 60);
                timerText.text = string.Format("{0:00}:{1:00}", minutes, seconds);
            }
        }
    }

    public void UpdateScore(int myCount, int enemyCount)
    {
        myBuildingCount = myCount;
        enemyBuildingCount = enemyCount;
        UpdateScoreUI();
    }

    private void UpdateScoreUI()
    {
        if (scoreText != null)
        {
            scoreText.text = $"내 건물: {myBuildingCount} | 상대방 건물: {enemyBuildingCount}";
        }
    }

    [PunRPC]
    void EndGame()
    {
        isGameActive = false;
        ShowGameOverPanel();
    }

    void ShowGameOverPanel()
    {
        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(true);

            string resultMessage;
            if (myBuildingCount > enemyBuildingCount)
                resultMessage = "승리!";
            else if (myBuildingCount < enemyBuildingCount)
                resultMessage = "패배...";
            else
                resultMessage = "무승부";

            resultText.text = $"게임 종료!\n{resultMessage}\n내 점령: {myBuildingCount}개\n상대방 점령: {enemyBuildingCount}개";
        }
    }

    void ReturnToLobby()
    {
        PhotonNetwork.LeaveRoom();
        GameManager.Instance.SetGameState(GameState.Lobby);
    }

    public override void OnLeftRoom()
    {
        base.OnLeftRoom();
        PhotonNetwork.LoadLevel("Lobby");
    }
} 