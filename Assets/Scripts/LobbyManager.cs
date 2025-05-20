



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
    // Join Room 시에 들어오는 input value
    public TMP_InputField joinCodeInputField;

    // debug.log results
    public TextMeshProUGUI debugText;

    public TextMeshProUGUI countdownText;

    // generate Room Code
    public TextMeshProUGUI roomCodeText;

    private string currentRoomCode;
    private float roomLifetime = 180f;
    private float timeRemaining;
    private bool isRoomActive = false;


    void Start()
    {
        createRoomButton.onClick.AddListener(OnCreateRoomButtonClick);
        joinRoomButton.onClick.AddListener(OnJoinRoomButtonClick);

        debugText.text = "Player entered";
        countdownText.text = "";
        roomCodeText.text = "";

        // 처음에는 숨김
        joinCodeInputField.gameObject.SetActive(false);


        // Photon 멀티서버 연결 - App ID 기반 자동 연결
        createRoomButton.interactable = false;
        PhotonNetwork.ConnectUsingSettings();
    }



    // Update is called once per frame
    void Update()
    {
        if (isRoomActive)
        {
            timeRemaining -= Time.deltaTime;

            if (timeRemaining > 0)
            {
                TimeSpan t = TimeSpan.FromSeconds(timeRemaining);
                countdownText.text = $"Remaining time: {t.Minutes:D2}:{t.Seconds:D2}";
            }
            else
            {
                countdownText.text = "Room expired";
                debugText.text = "The room code has expired";
                isRoomActive = false;
                currentRoomCode = null;
            }
        }
    }

    void OnCreateRoomButtonClick()
    {
        // Photon 서버에 이미 접속해 있는 경우 서버 생성 막기
        if (PhotonNetwork.InRoom)
    {
            debugText.text = "Already connecting to the server.";
        Debug.Log("Already connecting to the server.");
        return;
    }

        currentRoomCode = GenerateRoomCode();
        debugText.text = $"Room code : {currentRoomCode} (3mins)";
        roomCodeText.text = $"room code: {currentRoomCode}";

        Debug.Log($"Room Created Code: {currentRoomCode}");
        timeRemaining = roomLifetime;
        isRoomActive = true;
        joinCodeInputField.gameObject.SetActive(false);


        // Photon 멀티 서버 생성
        RoomOptions options = new RoomOptions { MaxPlayers = 2 };
        PhotonNetwork.CreateRoom(currentRoomCode, options);
        Debug.Log("Create a Server");


    }

    void OnJoinRoomButtonClick()
    {

        // 입력창이 꺼져 있으면 염
        if (!joinCodeInputField.gameObject.activeSelf)
        {
            joinCodeInputField.gameObject.SetActive(true);
            debugText.text = "enter the code";
            return;
        }
        string inputCode = joinCodeInputField.text.ToUpper().Trim();

        if (string.IsNullOrEmpty(inputCode))
        {
            debugText.text = "Enter code";
            return;
        }

        if (inputCode == currentRoomCode && isRoomActive)
        {
            debugText.text = $"{inputCode} game start";
            Debug.Log("Join Success");
        }
        else
        {
            debugText.text = "code not correct";
            Debug.Log("Join Failed");
        }

                if (PhotonNetwork.InRoom)
    {
        debugText.text = "이미 방에 입장 중입니다. 나간 뒤 다시 시도하세요.";
        Debug.Log("⚠️ 이미 방 안에 있음. 방 생성 차단.");
        return;
    }

        // Photon 멀티 서버에 참여 
        PhotonNetwork.JoinRoom(inputCode);
        debugText.text = $"Trying to join {inputCode}";

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

    // 콜백함수
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
        // 예: 게임 씬으로 전환하고 싶다면 여기서 SceneManager.LoadScene() 호출 가능
        // SceneManager.LoadScene("GameScene");
    }

    public override void OnJoinRoomFailed(short returnCode, string message)
    {
        debugText.text = $" Failed to join room: {message}";
    }
    
}

