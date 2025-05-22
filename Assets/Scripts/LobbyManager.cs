


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
    public Button createRoomButton;
    public Button joinRoomButton;
    public Button exitRoomButton;

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


    void Start()
    {
        createRoomButton.onClick.AddListener(OnCreateRoomButtonClick);
        joinRoomButton.onClick.AddListener(OnJoinRoomButtonClick);
        exitRoomButton.onClick.AddListener(OnExitRoomButtonClick);

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
        if (joinCodeInputField.gameObject.activeSelf && PhotonNetwork.IsConnectedAndReady)
        {
            string inputCode = joinCodeInputField.text.ToUpper().Trim();

            // 유효한 6자리 코드일 경우만 검사
            if (inputCode.Length == 6 && inputCode != lastTriedJoinCode)
            {
                lastTriedJoinCode = inputCode;
                PhotonNetwork.JoinRoom(inputCode);
                debugText.text = $"Trying to join {inputCode}";
                Debug.Log($"Auto join attempt: {inputCode}");
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
        debugText.text = "Connected to Master. Joining lobby...";
        PhotonNetwork.JoinLobby();
    }

    public override void OnJoinedLobby()
    {
        debugText.text = "Lobby joined. You can now create or join rooms.";
        createRoomButton.interactable = true;
    }

    public override void OnCreateRoomFailed(short returnCode, string message)
    {
        debugText.text = $" Room creation failed: {message}";
    }

    public override void OnJoinedRoom()
    {

        debugText.text = $" Joined room: {PhotonNetwork.CurrentRoom.Name}";
    }

    public override void OnJoinRoomFailed(short returnCode, string message)
    {
        debugText.text = $" Failed to join room: {message}";
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
            // PhotonNetwork.LoadLevel("Loading");

            // 맵 테스트 빌드
            PhotonNetwork.LoadLevel("HopscotchMap");
        }
    }

    public override void OnLeftRoom()
    {
        debugText.text = "leave a room";
        roomCodeText.text = "";
        countdownText.text = "";
        isCountdownRunning = false;

        // 로비로 돌아가기
        PhotonNetwork.JoinLobby();
    }


}

