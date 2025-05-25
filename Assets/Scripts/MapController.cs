using UnityEngine;
using System.Collections;
using OnlineMaps;
using System.Collections.Generic;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using Photon.Pun;
using Photon.Realtime;
using TMPro;

public class MapController : MonoBehaviourPunCallbacks, IPunObservable
{
    [Header("Map Settings")]
    public Map map;
    [SerializeField] private Camera mapCamera;
    private Marker2D myMarker;
    private Dictionary<string, Marker2D> otherPlayerMarkers = new Dictionary<string, Marker2D>();
    public GeoPoint myLocation;
    private Vector3 networkPosition;
    private Quaternion networkRotation;
    
    [Header("Building Capture Settings")]
    [Tooltip("건물 점령 가능 범위 (약 10미터)")]
    [SerializeField] private float captureRange = 0.0001f;
    [Tooltip("점령 버튼")]
    [SerializeField] private Button captureButton;
    [Tooltip("점령 진행도 표시 바")]
    [SerializeField] private Image captureProgressBar;
    [Tooltip("건물 이름 텍스트")]
    [SerializeField] private TextMeshProUGUI buildingInfoText;
    [Tooltip("점령 점수 UI")]
    [SerializeField] private BuildingScoreUI buildingScoreUI;

    private Building currentBuilding;
    private bool isCapturing;
    private Dictionary<Building, DrawingElement> buildingVisuals;
    private const string BUILDING_CAPTURE_EVENT = "BuildingCaptured";

    void Awake()
    {
        // GameObject를 root로 만들기
        // if (transform.parent != null)
        // {
        //     transform.SetParent(null);
        // }
        // // DontDestroyOnLoad(gameObject);

        if (photonView == null)
        {
            Debug.LogError("PhotonView가 없습니다. MapController에 PhotonView 컴포넌트를 추가해주세요!");
            return;
        }

        // 컴포넌트 참조 확인
        if (map == null)
        {
            Debug.LogError("Map 컴포넌트가 할당되지 않았습니다!");
            return;
        }

        // 카메라 참조 확인
        if (mapCamera == null)
        {
            mapCamera = Camera.main;
            if (mapCamera == null)
            {
                Debug.LogError("카메라를 찾을 수 없습니다!");
                return;
            }
        }
    }

    void Start()
    {
        Debug.Log($"MapController 시작 - 마스터 클라이언트: {PhotonNetwork.IsMasterClient}, 연결 상태: {PhotonNetwork.IsConnected}, 플레이어: {PhotonNetwork.LocalPlayer.NickName}");

        myLocation = new GeoPoint(0, 0);
        StartCoroutine(GetLocation());
        StartCoroutine(ShareLocationRoutine());  // 위치 공유 코루틴 시작

        Input.compass.enabled = true;
        
        // 마커 초기화
        InitializeMarkers();
        
        StartCoroutine(MapInit());

        buildingVisuals = new Dictionary<Building, DrawingElement>();
        InitializeAllBuildings();
        InitializeUI();

        Debug.Log("MapController 초기화 완료");
    }

    private void InitializeMarkers()
    {
        // 내 마커 초기화
        myMarker = Marker2DManager.CreateItem(myLocation);
        Sprite blueMarkerSprite = Resources.Load<Sprite>("MarkerBlue");
        if (blueMarkerSprite != null)
        {
            myMarker.texture = blueMarkerSprite.texture;
            Debug.Log("내 마커 생성 완료");
        }
        else
        {
            Debug.LogError("MarkerBlue 스프라이트를 찾을 수 없습니다!");
        }

        // 다른 플레이어 마커 초기화
        foreach (Player player in PhotonNetwork.PlayerList)
        {
            if (player.ActorNumber != PhotonNetwork.LocalPlayer.ActorNumber)
            {
                UpdateOtherPlayerMarker(player.NickName, myLocation.latitude, myLocation.longitude);
                Debug.Log($"다른 플레이어 마커 초기화: {player.NickName}");
            }
        }
    }

    public override void OnPlayerEnteredRoom(Player newPlayer)
    {
        Debug.Log($"플레이어 입장 감지: {newPlayer.NickName}");
        if (newPlayer.ActorNumber != PhotonNetwork.LocalPlayer.ActorNumber)
        {
            UpdateOtherPlayerMarker(newPlayer.NickName, myLocation.latitude, myLocation.longitude);
        }
    }

    private void OnEnable()
    {
        Debug.Log("MapController 활성화");
        if (PhotonNetwork.InRoom)
        {
            foreach (Player player in PhotonNetwork.PlayerList)
            {
                if (player.ActorNumber != PhotonNetwork.LocalPlayer.ActorNumber)
                {
                    UpdateOtherPlayerMarker(player.NickName, myLocation.latitude, myLocation.longitude);
                    Debug.Log($"기존 플레이어 마커 복원: {player.NickName}");
                }
            }
        }
    }

    private void OnDisable()
    {
        Debug.Log("MapController 비활성화");
        // 마커 정리
        if (myMarker != null)
        {
            Marker2DManager.RemoveItem(myMarker);
        }
        foreach (var marker in otherPlayerMarkers.Values)
        {
            Marker2DManager.RemoveItem(marker);
        }
        otherPlayerMarkers.Clear();
    }

    void Update()
    {
        myMarker.location = myLocation;

        float heading = Input.compass.trueHeading / 360f;
        myMarker.rotation = Mathf.LerpAngle(myMarker.rotation, heading, Time.deltaTime * 10f);

        CheckNearbyBuildings();
        UpdateCapture();
        UpdateBuildingInfo();
    }

    void UpdateBuildingInfo()
    {
        if (buildingInfoText != null && currentBuilding != null)
        {
            string ownerText = currentBuilding.GetOwner() == BuildingOwner.None ? 
                "없음" : currentBuilding.GetOwnerNickname();
            
            buildingInfoText.text = $"건물: {currentBuilding.GetName()}\n소유자: {ownerText}";
            buildingInfoText.gameObject.SetActive(true);
        }
        else if (buildingInfoText != null)
        {
            buildingInfoText.gameObject.SetActive(false);
        }
    }

    void CheckNearbyBuildings()
    {
        Building nearestBuilding = null;
        
        foreach (var building in buildingVisuals.Keys)
        {
            if (building.IsInRange(myLocation, captureRange))
            {
                nearestBuilding = building;
                Debug.Log($"플레이어가 {building.GetName()} 범위 안에 있습니다. 위치: {myLocation.latitude}, {myLocation.longitude}");
                break;
            }
        }

        if (nearestBuilding != currentBuilding)
        {
            if (currentBuilding != null)
            {
                Debug.Log($"{currentBuilding.GetName()} 범위에서 벗어났습니다.");
                currentBuilding.ResetCapture();
                UpdateBuildingVisual(currentBuilding);
            }

            currentBuilding = nearestBuilding;
            isCapturing = false;

            if (captureButton != null)
            {
                captureButton.gameObject.SetActive(nearestBuilding != null);
                if (nearestBuilding != null)
                {
                    Debug.Log($"{nearestBuilding.GetName()} 점령 가능 상태");
                }
            }
            if (captureProgressBar != null)
            {
                captureProgressBar.gameObject.SetActive(false);
            }
        }
    }

    void UpdateCapture()
    {
        if (currentBuilding == null || !isCapturing) return;

        try
        {
            if (captureProgressBar != null)
            {
                captureProgressBar.gameObject.SetActive(true);
                captureProgressBar.fillAmount = currentBuilding.GetCaptureProgress();
            }

            if (currentBuilding.UpdateCapture(Time.deltaTime, isCapturing, PhotonNetwork.LocalPlayer.NickName))
            {
                Debug.Log($"건물 점령 완료: {currentBuilding.GetName()}, 점령자: {PhotonNetwork.LocalPlayer.NickName}");
                if (photonView != null)
                {
                    photonView.RPC("OnBuildingCaptured", RpcTarget.All, currentBuilding.GetName(), PhotonNetwork.LocalPlayer.NickName);
                }
                else
                {
                    Debug.LogWarning("PhotonView가 없습니다. 싱글플레이어 모드로 실행됩니다.");
                    OnBuildingCaptured(currentBuilding.GetName(), "Player");
                }
                
                UpdateBuildingVisual(currentBuilding);
                isCapturing = false;
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"UpdateCapture 에러: {e.Message}");
            isCapturing = false;
            if (captureProgressBar != null)
            {
                captureProgressBar.gameObject.SetActive(false);
            }
        }
    }

    [PunRPC]
    void OnBuildingCaptured(string buildingName, string capturedByNickname)
    {
        Debug.Log($"건물 점령 이벤트 수신: {buildingName}, 점령자: {capturedByNickname}");
        foreach (var building in buildingVisuals.Keys)
        {
            if (building.GetName() == buildingName)
            {
                building.OnCaptureEventReceived(BuildingOwner.Player, capturedByNickname);
                UpdateBuildingVisual(building);
                Debug.Log($"건물 {buildingName}의 소유자가 {capturedByNickname}로 변경됨");
                break;
            }
        }
    }

    void UpdateBuildingVisual(Building building)
    {
        Color color;
        if (building.GetOwner() == BuildingOwner.None)
        {
            color = Color.gray;
            Debug.Log($"건물 {building.GetName()} - 소유자 없음, 회색으로 설정");
        }
        else
        {
            string ownerNickname = building.GetOwnerNickname();
            if (ownerNickname == PhotonNetwork.LocalPlayer.NickName)
            {
                color = Color.blue;  // 내 건물
                Debug.Log($"건물 {building.GetName()} - 내 소유, 파란색으로 설정");
            }
            else
            {
                color = Color.red;   // 상대방 건물
                Debug.Log($"건물 {building.GetName()} - {ownerNickname} 소유, 빨간색으로 설정");
            }
        }
        
        DrawBuilding(building, color);
        UpdateBuildingScore();  // 건물 상태가 변경될 때마다 점수 업데이트
    }

    private void UpdateBuildingScore()
    {
        if (buildingScoreUI == null) return;

        int myCount = 0;
        int enemyCount = 0;

        foreach (var building in buildingVisuals.Keys)
        {
            if (building.GetOwner() == BuildingOwner.Player)
            {
                if (building.GetOwnerNickname() == PhotonNetwork.LocalPlayer.NickName)
                {
                    myCount++;
                }
                else
                {
                    enemyCount++;
                }
            }
        }

        buildingScoreUI.UpdateScore(myCount, enemyCount);
    }

    void DrawBuilding(Building building, Color color)
    {
        if (building == null || map == null || map.drawingElementManager == null) return;

        try
        {
            if (buildingVisuals.ContainsKey(building))
            {
                map.drawingElementManager.Remove(buildingVisuals[building]);
            }

            var polygon = new Polygon(building.GetOutline(), color, 5, new Color(color.r, color.g, color.b, 0.5f));
            buildingVisuals[building] = polygon;
            map.drawingElementManager.Add(polygon);
        }
        catch (System.Exception e)
        {
            Debug.LogError($"DrawBuilding 에러: {e.Message}\n건물: {building?.GetName() ?? "null"}");
        }
    }

    void InitializeAllBuildings()
    {
        // A동
        InitializeBuilding(Buildings.A, Color.gray);
        // B동
        InitializeBuilding(Buildings.B, Color.gray);
        // C동
        InitializeBuilding(Buildings.C, Color.gray);
        // D동
        InitializeBuilding(Buildings.D, Color.gray);
        // 제2기숙사
        InitializeBuilding(Buildings.Dorm, Color.gray);
        // E동
        InitializeBuilding(Buildings.E, Color.gray);
        // 학생회관, 중앙도서관
        InitializeBuilding(Buildings.GH, Color.gray);
        // 과학관
        InitializeBuilding(Buildings.I, Color.gray);
        // 제3공학관
        InitializeBuilding(Buildings.J, Color.gray);
        // 제1공학관
        InitializeBuilding(Buildings.K, Color.gray);
        // 제2공학관
        InitializeBuilding(Buildings.P, Color.gray);
        // 정보통신센터
        InitializeBuilding(Buildings.Q, Color.gray);
        // 홍문관
        InitializeBuilding(Buildings.R, Color.gray);
        // 강당
        InitializeBuilding(Buildings.S, Color.gray);
        // 제4공학관
        InitializeBuilding(Buildings.T, Color.gray);
        // 미술종합강의동
        InitializeBuilding(Buildings.U, Color.gray);
        // 제1강의동
        InitializeBuilding(Buildings.Z1, Color.gray);
        // 이천득관
        InitializeBuilding(Buildings.Z2, Color.gray);
        // 제3강의동
        InitializeBuilding(Buildings.Z3, Color.gray);
        // 제4강의동
        InitializeBuilding(Buildings.Z4, Color.gray);
        // 체육관
        InitializeBuilding(Buildings.M, Color.gray);
        // 와우관
        InitializeBuilding(Buildings.L, Color.gray);
        // 미술학관
        InitializeBuilding(Buildings.F, Color.gray);
        // 문헌관
        InitializeBuilding(Buildings.MH, Color.gray);

    }

    void InitializeBuilding(Building building, Color color)
    {
        DrawingElement element = DrawColor(building.GetOutline(), color, 0.5f);
        buildingVisuals.Add(building, element);
    }

    void StartCapture()
    {
        if (currentBuilding != null)
        {
            isCapturing = true;
        }
    }

    void StopCapture()
    {
        isCapturing = false;
        if (currentBuilding != null)
        {
            currentBuilding.ResetCapture();
            if (captureProgressBar != null)
            {
                captureProgressBar.fillAmount = 0f;
                captureProgressBar.gameObject.SetActive(false);
            }
        }
    }

    IEnumerator MapInit()
    {
        while (myLocation.latitude == 0)
        {
            yield return null;
        }
        map.location = myLocation;
    }

    IEnumerator GetLocation()
    {
#if UNITY_EDITOR
        // 에디터에서는 테스트 위치 사용 (홍익대학교 중앙도서관 위치)
        myLocation = new GeoPoint(126.926285, 37.551571);
        while (true)
        {
            yield return new WaitForSeconds(0.5f);
        }
#else
        while (true)
        {
            if (!Input.location.isEnabledByUser)
            {
                Debug.Log("위치 서비스가 꺼져있습니다.");
                yield return new WaitForSeconds(5);
                continue;
            }

            Input.location.Start();

            int timeout = 20;
            while (Input.location.status == LocationServiceStatus.Initializing && timeout > 0)
            {
                yield return new WaitForSeconds(1);
                timeout--;
            }
            if (timeout < 1)
            {
                Debug.Log("Timeout");
                yield return new WaitForSeconds(5);
                continue;
            }

            if (Input.location.status == LocationServiceStatus.Failed)
            {
                Debug.Log("위치 정보를 불러올 수 없습니다.");
                yield return new WaitForSeconds(5);
                continue;
            }
            else
            {
                while (Input.location.status == LocationServiceStatus.Running)
                {
                    myLocation.longitude = Input.location.lastData.longitude;
                    myLocation.latitude = Input.location.lastData.latitude;
                    yield return new WaitForSeconds(0.5f);
                }
            }
        }
#endif
    }

    DrawingElement DrawColor(List<GeoPoint> outline, Color color, float alpha)
    {
        Polygon polygon = new Polygon(outline, color, 5, new Color(color.r, color.g, color.b, alpha));
        map.drawingElementManager.Add(polygon);
        return polygon;
    }
    public void MoveToMyLocation()
    {
        map.location = myLocation;
    }

    void InitializeUI()
    {
        if (captureButton != null)
        {
            captureButton.gameObject.SetActive(false);
            EventTrigger trigger = captureButton.gameObject.GetComponent<EventTrigger>();
            if (trigger == null)
            {
                trigger = captureButton.gameObject.AddComponent<EventTrigger>();
            }

            EventTrigger.Entry entryDown = new EventTrigger.Entry();
            entryDown.eventID = EventTriggerType.PointerDown;
            entryDown.callback.AddListener((data) => { StartCapture(); });
            trigger.triggers.Add(entryDown);

            EventTrigger.Entry entryUp = new EventTrigger.Entry();
            entryUp.eventID = EventTriggerType.PointerUp;
            entryUp.callback.AddListener((data) => { StopCapture(); });
            trigger.triggers.Add(entryUp);
        }
        
        if (captureProgressBar != null)
        {
            captureProgressBar.gameObject.SetActive(false);
        }

        if (buildingInfoText != null)
        {
            buildingInfoText.gameObject.SetActive(false);
        }
    }

    public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
    {
        try
        {
            Debug.Log($"OnPhotonSerializeView - IsWriting: {stream.IsWriting}, IsMasterClient: {PhotonNetwork.IsMasterClient}, LocalPlayer: {PhotonNetwork.LocalPlayer.NickName}");
            
            if (stream.IsWriting)
            {
                // 내 위치 정보를 다른 플레이어에게 보내기
                stream.SendNext(myLocation.latitude);
                stream.SendNext(myLocation.longitude);
                stream.SendNext(PhotonNetwork.LocalPlayer.NickName);
                Debug.Log($"위치 정보 전송: {myLocation.latitude}, {myLocation.longitude}");
            }
            else
            {
                // 다른 플레이어의 위치 정보 받기
                double latitude = (double)stream.ReceiveNext();
                double longitude = (double)stream.ReceiveNext();
                string senderNickname = (string)stream.ReceiveNext();
                
                if (senderNickname != PhotonNetwork.LocalPlayer.NickName)
                {
                    UpdateOtherPlayerMarker(senderNickname, latitude, longitude);
                    Debug.Log($"다른 플레이어 위치 수신: {senderNickname} - {latitude}, {longitude}");
                }
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"OnPhotonSerializeView 에러: {e.Message}");
        }
    }

    private void UpdateOtherPlayerMarker(string playerNickname, double latitude, double longitude)
    {
        try
        {
            if (!otherPlayerMarkers.ContainsKey(playerNickname))
            {
                // 다른 플레이어의 마커 생성
                Marker2D marker = Marker2DManager.CreateItem(new GeoPoint(longitude, latitude));
                Sprite markerSprite = Resources.Load<Sprite>("MarkerRed");
                if (markerSprite != null)
                {
                    marker.texture = markerSprite.texture;
                    Debug.Log($"마커 생성 성공: {playerNickname}");
                }
                else
                {
                    Debug.LogError("MarkerRed 스프라이트를 찾을 수 없습니다!");
                }
                otherPlayerMarkers[playerNickname] = marker;
            }
            else
            {
                // 기존 마커 위치 업데이트
                otherPlayerMarkers[playerNickname].location = new GeoPoint(longitude, latitude);
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"UpdateOtherPlayerMarker 에러: {e.Message}");
        }
    }

    public override void OnPlayerLeftRoom(Player otherPlayer)
    {
        if (otherPlayerMarkers.ContainsKey(otherPlayer.NickName))
        {
            Marker2D marker = otherPlayerMarkers[otherPlayer.NickName];
            Marker2DManager.RemoveItem(marker);
            otherPlayerMarkers.Remove(otherPlayer.NickName);
            Debug.Log($"플레이어 마커 제거: {otherPlayer.NickName}");
        }
    }

    IEnumerator ShareLocationRoutine()
    {
        while (true)
        {
            if (photonView != null && PhotonNetwork.IsConnected)
            {
                Debug.Log($"위치 공유 시도 - IsMasterClient: {PhotonNetwork.IsMasterClient}, Player: {PhotonNetwork.LocalPlayer.NickName}, 위치: {myLocation.latitude}, {myLocation.longitude}");
                photonView.RPC("ShareLocation", RpcTarget.All, myLocation.latitude, myLocation.longitude, PhotonNetwork.LocalPlayer.NickName);
            }
            yield return new WaitForSeconds(1f);  // 1초마다 위치 전송
        }
    }

    [PunRPC]
    void ShareLocation(double latitude, double longitude, string senderNickname)
    {
        Debug.Log($"RPC 수신 - From: {senderNickname}, To: {PhotonNetwork.LocalPlayer.NickName}, IsMaster: {PhotonNetwork.IsMasterClient}, 위치: {latitude}, {longitude}");
        
        if (senderNickname != PhotonNetwork.LocalPlayer.NickName)
        {
            UpdateOtherPlayerMarker(senderNickname, latitude, longitude);
            Debug.Log($"다른 플레이어 마커 업데이트: {senderNickname} - {latitude}, {longitude}");
        }
    }
}
