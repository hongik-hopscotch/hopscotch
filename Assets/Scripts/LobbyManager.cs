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

    private string pendingRoomCode = null;  // 대기 중인 방 코드

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
        // GameObject를 root로 만들기
        if (transform.parent != null)
        {
            transform.SetParent(null);
        }

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

        // 연결 설정
        PhotonNetwork.EnableCloseConnection = false;  // 연결 자동 종료 비활성화

        // 서버 지역 설정 확인
        Debug.Log($"현재 서버 설정 - 지역: {PhotonNetwork.PhotonServerSettings.AppSettings.FixedRegion}, 씬 동기화: {PhotonNetwork.AutomaticallySyncScene}");
        
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
            Debug.Log("앱이 백그라운드로 전환됨");
            if (PhotonNetwork.IsConnected)
            {
                PhotonNetwork.EnableCloseConnection = false;  // 연결 유지
            }
        }
        else
        {
            // 앱이 다시 포그라운드로 올 때
            Debug.Log("앱이 포그라운드로 전환됨");
            if (!PhotonNetwork.IsConnected)
            {
                Debug.Log("재연결 시도");
                PhotonNetwork.ConnectUsingSettings();
            }
            else if (PhotonNetwork.InRoom)
            {
                Debug.Log("방에 재접속 시도");
                PhotonNetwork.ReconnectAndRejoin();
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
        if (isCountdownRunning && PhotonNetwork.IsMasterClient && PhotonNetwork.InRoom && PhotonNetwork.CurrentRoom.PlayerCount == 1)
        {
            timeRemaining -= Time.deltaTime;
            countdownText.text = $"waiting... {Mathf.CeilToInt(timeRemaining)}";

            if (timeRemaining <= 0f)
            {
                countdownText.text = "expired";
                debugText.text = "expired - termination.";
                isCountdownRunning = false;
                PhotonNetwork.LeaveRoom();
            }
        }

        // 보류 중인 방 참가 요청 처리
        if (pendingRoomCode != null && PhotonNetwork.InLobby)
        {
            string roomToJoin = pendingRoomCode;
            pendingRoomCode = null;
            TryJoinRoom(roomToJoin);
        }
    }

    private void TryJoinRoom(string roomCode)
    {
        if (!PhotonNetwork.InLobby)
        {
            Debug.Log($"로비에 없는 상태에서 방 참가 시도. 로비 진입 후 재시도 예정 - 코드: {roomCode}");
            pendingRoomCode = roomCode;
            PhotonNetwork.JoinLobby();
            return;
        }

        Debug.Log($"방 참가 시도: {roomCode}, 서버 지역: {PhotonNetwork.CloudRegion}, 로비 상태: {PhotonNetwork.InLobby}");
        PhotonNetwork.JoinRoom(roomCode);
    }

    void OnCreateRoomButtonClick()
    {
        if (!PhotonNetwork.IsConnectedAndReady)
        {
            debugText.text = "서버 연결 대기 중...";
            Debug.LogWarning("서버 연결 대기 중");
            return;
        }

        if (PhotonNetwork.InRoom)
        {
            debugText.text = "이미 방에 있습니다.";
            Debug.Log("Already in room");
            return;
        }

        currentRoomCode = GenerateRoomCode();
        debugText.text = $"방 코드: {currentRoomCode} (3분)";
        roomCodeText.text = $"방 코드: {currentRoomCode}";

        RoomOptions options = new RoomOptions
        {
            MaxPlayers = 2,
            PublishUserId = true,  // 유저 ID 공개 설정
            BroadcastPropsChangeToAll = true,  // 모든 클라이언트에게 속성 변경 브로드캐스트
            CleanupCacheOnLeave = false  // 플레이어가 나가도 캐시 유지
        };

        Debug.Log($"방 생성 시도 - 코드: {currentRoomCode}, 서버 지역: {PhotonNetwork.CloudRegion}");
        PhotonNetwork.CreateRoom(currentRoomCode, options, null);
    }

    void OnJoinRoomButtonClick()
    {
        if (PhotonNetwork.InRoom)
        {
            debugText.text = "이미 방에 참가한 상태입니다. 나간 후 다시 시도해주세요.";
            Debug.Log("이미 방에 있음. 중복 참가 시도 차단.");
            return;
        }

        if (!PhotonNetwork.IsConnectedAndReady)
        {
            debugText.text = "서버에 연결중입니다. 잠시 후 다시 시도해주세요.";
            Debug.Log("서버 연결 대기 중");
            return;
        }

        if (!joinCodeInputField.gameObject.activeSelf)
        {
            joinCodeInputField.gameObject.SetActive(true);
            debugText.text = "참가 코드를 입력하세요";
            joinCodeInputField.ActivateInputField();
            return;
        }

        string inputCode = joinCodeInputField.text.ToUpper().Trim();

        if (string.IsNullOrEmpty(inputCode))
        {
            debugText.text = "참가 코드를 입력하세요";
            return;
        }

        if (inputCode.Length != 6)
        {
            debugText.text = "참가 코드는 6자리여야 합니다";
            return;
        }

        debugText.text = "방 참가 시도 중...";
        TryJoinRoom(inputCode);
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

        // 닉네임이 설정되어 있지 않은 경우에만 설정
        if (string.IsNullOrEmpty(PhotonNetwork.LocalPlayer.NickName))
        {
            // 고유한 닉네임 생성 (예: Player_1234)
            string uniqueNickname = "Player_" + UnityEngine.Random.Range(1000, 9999).ToString();
            PhotonNetwork.LocalPlayer.NickName = uniqueNickname;
            Debug.Log($"플레이어 닉네임 설정: {uniqueNickname}");
        }

        PhotonNetwork.JoinLobby();
    }

    public override void OnJoinedLobby()
    {
        Debug.Log($"로비 입장 완료 - 지역: {PhotonNetwork.CloudRegion}");
        debugText.text = "로비 입장 완료. 이제 방을 만들거나 참여할 수 있습니다.";
        createRoomButton.interactable = true;

        // 보류 중인 방 참가 요청이 있다면 처리
        if (pendingRoomCode != null)
        {
            TryJoinRoom(pendingRoomCode);
        }
    }

    public override void OnCreatedRoom()
    {
        Debug.Log($"방 생성 성공 - 코드: {currentRoomCode}, 지역: {PhotonNetwork.CloudRegion}, 현재 인원: {PhotonNetwork.CurrentRoom.PlayerCount}");
        isCountdownRunning = true;
        timeRemaining = roomLifetime;
    }

    public override void OnJoinedRoom()
    {
        Debug.Log($"방 참가 성공 - 방 이름: {PhotonNetwork.CurrentRoom.Name}, 플레이어: {PhotonNetwork.LocalPlayer.NickName}, 현재 인원: {PhotonNetwork.CurrentRoom.PlayerCount}, 지역: {PhotonNetwork.CloudRegion}");
        debugText.text = $"방 참가 성공: {PhotonNetwork.CurrentRoom.Name}";

        if (PhotonNetwork.IsMasterClient)
        {
            var props = new ExitGames.Client.Photon.Hashtable();
            props.Add("startTime", PhotonNetwork.Time);
            PhotonNetwork.CurrentRoom.SetCustomProperties(props);
        }

        // 이미 방에 다른 플레이어가 있다면 바로 게임 시작
        if (PhotonNetwork.CurrentRoom.PlayerCount == 2)
        {
            if (PhotonNetwork.IsMasterClient)
            {
                Debug.Log("두 번째 플레이어 입장으로 게임을 시작합니다.");
                PhotonNetwork.LoadLevel("Loading");
            }
        }
    }

    public override void OnJoinRoomFailed(short returnCode, string message)
    {
        string errorMessage = "알 수 없는 오류가 발생했습니다.";
        
        Debug.Log($"방 참가 실패 상세 정보 - 코드: {returnCode}, 메시지: {message}, 서버 지역: {PhotonNetwork.CloudRegion}, 연결 상태: {PhotonNetwork.IsConnected}, 로비 상태: {PhotonNetwork.InLobby}");
        
        switch (returnCode)
        {
            case 32758: // Game does not exist
                errorMessage = $"존재하지 않는 방입니다. 코드를 다시 확인해주세요. (지역: {PhotonNetwork.CloudRegion})";
                break;
            case 32765:
                errorMessage = "방이 가득 찼습니다.";
                break;
            case 32764:
                errorMessage = "이미 종료된 방입니다.";
                break;
            default:
                errorMessage = $"방 참가 실패: {message}";
                break;
        }
        
        debugText.text = errorMessage;
        lastTriedJoinCode = "";

        if (joinCodeInputField != null)
        {
            joinCodeInputField.text = "";
            joinCodeInputField.ActivateInputField();
        }
    }

    public override void OnPlayerEnteredRoom(Player newPlayer)
    {
        Debug.Log($"플레이어 입장: {newPlayer.NickName}, 현재 인원: {PhotonNetwork.CurrentRoom.PlayerCount}");

        // 서버에 두 사람 모두 접속 시
        if (PhotonNetwork.CurrentRoom.PlayerCount == 2)
        {
            countdownText.text = "";
            isCountdownRunning = false;
            debugText.text = "게임을 시작합니다...";

            // 마스터 클라이언트만 씬을 로드하도록 함
            if (PhotonNetwork.IsMasterClient)
            {
                Debug.Log("마스터 클라이언트가 게임 씬을 로드합니다.");
                // 실제 빌드
                PhotonNetwork.LoadLevel("Loading");
                // 맵 테스트 빌드
                // PhotonNetwork.LoadLevel("HopscotchMap");
            }
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

    public override void OnLeftLobby()
    {
        Debug.Log("로비에서 나감");
        createRoomButton.interactable = false;
    }

    public override void OnDisconnected(DisconnectCause cause)
    {
        Debug.LogWarning($"서버 연결 끊김: {cause}");
        
        string disconnectMessage = "서버와 연결이 끊어졌습니다.";
        switch (cause)
        {
            case DisconnectCause.ServerTimeout:
                disconnectMessage = "서버 응답 시간 초과. 다시 연결을 시도합니다.";
                break;
            case DisconnectCause.ClientTimeout:
                disconnectMessage = "클라이언트 응답 시간 초과. 다시 연결을 시도합니다.";
                break;
            case DisconnectCause.DisconnectByServerLogic:
                disconnectMessage = "서버에 의해 연결이 종료되었습니다.";
                break;
            case DisconnectCause.DisconnectByClientLogic:
                disconnectMessage = "클라이언트에 의해 연결이 종료되었습니다.";
                break;
        }
        
        debugText.text = disconnectMessage;
        createRoomButton.interactable = false;
        pendingRoomCode = null;  // 연결이 끊어지면 보류 중인 방 참가 요청 취소

        // 현재 씬이 게임 씬이면 로비로 돌아가기
        if (UnityEngine.SceneManagement.SceneManager.GetActiveScene().name != "Lobby")
        {
            UnityEngine.SceneManagement.SceneManager.LoadScene("Lobby");
        }
        
        if (cause != DisconnectCause.DisconnectByClientLogic)
        {
            Debug.Log("재연결 시도 중...");
            PhotonNetwork.ConnectUsingSettings();
        }
    }

}

