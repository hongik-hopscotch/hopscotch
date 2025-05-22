using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;
using System;
using System.Collections;
using UnityEngine.Rendering.Universal.Internal;

using Photon.Pun;
using Photon.Realtime;

public class LobbyManager : MonoBehaviourPunCallbacks
{
    public static LobbyManager Instance { get; private set; }

    public Button createRoomButton;
    public Button joinRoomButton;
    // public Button exitRoomButton;

    // Join Room 시에 들어오는 input value
    public TMP_InputField joinCodeInputField;

    // debug.log results
    public TextMeshProUGUI debugText;
    public TextMeshProUGUI countdownText;

    // generate Room Code
    public TextMeshProUGUI roomCodeText;
    private string currentRoomCode;

    // 로비 내 서버(방)) 제한 시간 설정
    private float roomLifetime = 180f;  // 3분
    private float timeRemaining;
    private bool isCountdownRunning = false;

    // 입장 코드 검사
    private string lastTriedJoinCode = "";

    private void Awake()
    {
        // 싱글톤 패턴 구현
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }

    void Start()
    {
        createRoomButton.onClick.AddListener(OnCreateRoomButtonClick);
        joinRoomButton.onClick.AddListener(OnJoinRoomButtonClick);
        // exitRoomButton.onClick.AddListener(OnExitRoomButtonClick);

        debugText.text = "Player entered";
        countdownText.text = "";
        roomCodeText.text = "";

        // 처음에는 숨김
        joinCodeInputField.gameObject.SetActive(false);

        // Photon 멀티서버 연결 - App ID 기반 자동 연결
        PhotonNetwork.AutomaticallySyncScene = true;
        createRoomButton.interactable = false;
        PhotonNetwork.ConnectUsingSettings();
    }

    public override void OnDisable()
    {
        base.OnDisable();  // 부모 클래스의 OnDisable 호출
        
        // 이벤트 리스너 제거
        if (createRoomButton != null)
            createRoomButton.onClick.RemoveListener(OnCreateRoomButtonClick);
        if (joinRoomButton != null)
            joinRoomButton.onClick.RemoveListener(OnJoinRoomButtonClick);
        // if (exitRoomButton != null)
        //     exitRoomButton.onClick.RemoveListener(OnExitRoomButtonClick);
    }

    void OnDestroy()
    {
        // 리소스 정리
        StopAllCoroutines();
        
        if (PhotonNetwork.IsConnected)
        {
            PhotonNetwork.Disconnect();
        }
    }

    void OnApplicationPause(bool pauseStatus)
    {
        if (pauseStatus)
        {
            // 앱이 백그라운드로 갈 때
            if (PhotonNetwork.IsConnected && !PhotonNetwork.InRoom)
            {
                PhotonNetwork.Disconnect();
            }
        }
        else
        {
            // 앱이 다시 포그라운드로 올 때
            if (!PhotonNetwork.IsConnected && !PhotonNetwork.InRoom)
            {
                PhotonNetwork.ConnectUsingSettings();
            }
        }
    }

    void OnApplicationQuit()
    {
        if (PhotonNetwork.IsConnected)
        {
            PhotonNetwork.Disconnect();
        }
    }

    // Update is called once per frame
    void Update()
    {
        // 서버 제한시간 카운트다운 
        // 상대방 접속시 시간 카운트 자동으로 멈춤
        if (isCountdownRunning && PhotonNetwork.IsMasterClient && PhotonNetwork.InRoom && PhotonNetwork.CurrentRoom.PlayerCount == 1)
        {
            timeRemaining -= Time.deltaTime;
            countdownText.text = $"waiting... {Mathf.CeilToInt(timeRemaining)}";

            // 3분 지날 시 서버 폭파
            if (timeRemaining <= 0f)
            {
                countdownText.text = "expired";
                debugText.text = "expired - termination.";

                isCountdownRunning = false;

                PhotonNetwork.LeaveRoom();
            }
        }

        // JoinCodeInputField 활성화 된 경우 코드 검사
        if (joinCodeInputField.gameObject.activeSelf)
        {
            if (!PhotonNetwork.InLobby)
            {
                debugText.text = "로비 연결 대기 중...";
                return;
            }

            string inputCode = joinCodeInputField.text.ToUpper().Trim();

            // 유효한 6자리 코드일 경우만 검사
            if (inputCode.Length == 6 && inputCode != lastTriedJoinCode)
            {
                lastTriedJoinCode = inputCode;
                Debug.Log($"방 참가 시도: {inputCode}, 연결 상태: {PhotonNetwork.IsConnected}, Ready 상태: {PhotonNetwork.IsConnectedAndReady}, 로비 상태: {PhotonNetwork.InLobby}");
                PhotonNetwork.JoinRoom(inputCode);
                debugText.text = $"방 참가 시도 중: {inputCode}";
            }
        }
    }

    void OnCreateRoomButtonClick()
    {
        // 서버와 연결되고 내부 클라이언트 상태가 ready일 경우
        if (!PhotonNetwork.IsConnectedAndReady)
        {
            debugText.text = "server waiting...";
            Debug.LogWarning("Tried to create room before ready.");
            return;
        }

        // Photon 서버에 이미 접속해 있는 경우 서버 생성 막기
        if (PhotonNetwork.InRoom)
        {
            debugText.text = "Already connecting to the server.";
            Debug.Log("Already connecting to the server.");
            return;
        }

        // RoomCode 생성 + 제한시간 설정 
        currentRoomCode = GenerateRoomCode();

        debugText.text = $"Room code : {currentRoomCode} (3mins)";
        roomCodeText.text = $"Room code: {currentRoomCode}";

        // 참여 인원 2명으로 제한
        RoomOptions options = new RoomOptions
        {
            MaxPlayers = 2
        };

        // 생성된 RoomCode 이용해 Photon 멀티 서버 생성
        // 멀티 서버 생성 시 생성자는 자동으로 방(서버)에 입장됨, 경쟁 상대방만 코드 이용해 입장하면 됨
        joinCodeInputField.gameObject.SetActive(false);

        PhotonNetwork.CreateRoom(currentRoomCode, options);
        Debug.Log("Create a Server");

        isCountdownRunning = true;
        timeRemaining = roomLifetime;
    }

    void OnJoinRoomButtonClick()
    {
        // 방에 접속했지만 또 다른 방에 접속하는 경우 제한
        if (PhotonNetwork.InRoom)
        {
            debugText.text = "You are already entering the room. Please leave and try again.";
            Debug.Log("Already in the room. Block room creation.");
            return;
        }

        // 입력창이 꺼져 있으면 염
        if (!joinCodeInputField.gameObject.activeSelf)
        {
            joinCodeInputField.gameObject.SetActive(true);
            debugText.text = "enter the code";

            joinCodeInputField.ActivateInputField();

            return;
        }

        string inputCode = joinCodeInputField.text.ToUpper().Trim();

        if (string.IsNullOrEmpty(inputCode))
        {
            debugText.text = "Enter code";
            return;
        }

        // RoomCode가 생성되어 있고, 이미 방이 생성되어 있는 경우 Photon 멀티 서버에 참여 가능
        if (PhotonNetwork.IsConnectedAndReady)
        {

            PhotonNetwork.JoinRoom(inputCode);
            debugText.text = $"Trying to join {inputCode}";
            Debug.Log("Join Success");

        }
    }
    void OnExitRoomButtonClick()
    {
        if (PhotonNetwork.InRoom)
        {
            debugText.text = "leaving the room...";
            PhotonNetwork.LeaveRoom();
        }
        else
        {
            debugText.text = "you are not in the room.";
        }
    }

    string GenerateRoomCode()
    {
        const string chars = "QWERTYUIOPASDFGHJKLZXCVBNM0987654321";
        System.Random rand = new System.Random();
        char[] code = new char[6];
        for (int i = 0; i < code.Length; i++)
        {
            code[i] = chars[rand.Next(chars.Length)];
        }
        return new string(code);
    }


    // 서버 접속 관련 콜백함수 - MonoBehaviourPunCallbacks 
    public override void OnConnectedToMaster()
    {
        Debug.Log("마스터 서버 연결 완료");
        debugText.text = "서버 연결 완료. 로비 입장 중...";
        PhotonNetwork.JoinLobby();
    }

    public override void OnJoinedLobby()
    {
        Debug.Log("로비 입장 완료");
        debugText.text = "로비 입장 완료. 이제 방을 만들거나 참여할 수 있습니다.";
        createRoomButton.interactable = true;
    }

    public override void OnCreateRoomFailed(short returnCode, string message)
    {
        debugText.text = $" Room creation failed: {message}";
    }

    public override void OnJoinedRoom()
    {
        Debug.Log($"방 참가 성공: {PhotonNetwork.CurrentRoom.Name}");
        debugText.text = $"방 참가 성공: {PhotonNetwork.CurrentRoom.Name}";
    }

    public override void OnJoinRoomFailed(short returnCode, string message)
    {
        debugText.text = $"방 참가 실패: {message} (코드: {returnCode})";
        Debug.LogError($"방 참가 실패 - 코드: {returnCode}, 메시지: {message}");
        lastTriedJoinCode = "";
    }

    public override void OnPlayerEnteredRoom(Player newPlayer)
    {
        Debug.Log("player Entered!");

        // 서버에 두 사람 모두 접속 시
        if (PhotonNetwork.CurrentRoom.PlayerCount == 2)
        {
            countdownText.text = "";
            isCountdownRunning = false;

            debugText.text = "";

            // 실제 빌드
            PhotonNetwork.LoadLevel("Loading");

            // 맵 테스트 빌드
            // PhotonNetwork.LoadLevel("HopscotchMap");
        }
    }

    public override void OnLeftRoom()
    {
        // 리소스 정리
        Resources.UnloadUnusedAssets();
        System.GC.Collect();

        debugText.text = "leave a room";
        roomCodeText.text = "";
        countdownText.text = "";
        isCountdownRunning = false;

        // 로비로 돌아가기
        PhotonNetwork.JoinLobby();
    }

    public override void OnDisconnected(DisconnectCause cause)
    {
        Debug.LogWarning($"서버 연결 끊김: {cause}");
        debugText.text = $"서버 연결이 끊겼습니다: {cause}";
        
        // 재연결 시도
        if (!PhotonNetwork.IsConnected)
        {
            PhotonNetwork.ConnectUsingSettings();
        }
    }

}
