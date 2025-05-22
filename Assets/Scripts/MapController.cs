using UnityEngine;
using System.Collections;
using OnlineMaps;
using System.Collections.Generic;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using Photon.Pun;
using Photon.Realtime;
using TMPro;

public class MapController : MonoBehaviourPunCallbacks
{
    [Header("Map Settings")]
    public Map map;
    private Marker2D myMarker;
    public GeoPoint myLocation;
    
    [Header("Building Capture Settings")]
    [Tooltip("건물 점령 가능 범위 (약 10미터)")]
    [SerializeField] private float captureRange = 0.0001f;
    [Tooltip("점령 버튼")]
    [SerializeField] private Button captureButton;
    [Tooltip("점령 진행도 표시 바")]
    [SerializeField] private Image captureProgressBar;
    [Tooltip("건물 이름 텍스트")]
    [SerializeField] private TextMeshProUGUI buildingInfoText;

    private Building currentBuilding;
    private bool isCapturing;
    private Dictionary<Building, DrawingElement> buildingVisuals;
    private const string BUILDING_CAPTURE_EVENT = "BuildingCaptured";

    void Start()
    {
        myLocation = new GeoPoint(0, 0);

        // real
        StartCoroutine(GetLocation());

        Input.compass.enabled = true;
        myMarker = Marker2DManager.CreateItem(myLocation);
        myMarker.texture = Resources.Load<Texture2D>("MarkerBlue");

        StartCoroutine(MapInit());

        // 모든 건물 초기화
        buildingVisuals = new Dictionary<Building, DrawingElement>();
        InitializeAllBuildings();

        // UI 초기화
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

        // Photon 연결 확인
        if (!PhotonNetwork.IsConnected)
        {
            Debug.Log("Photon 서버에 연결되어 있지 않습니다. 싱글플레이어 모드로 실행됩니다.");
        }
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
        foreach (var building in buildingVisuals.Keys)
        {
            if (building.GetName() == buildingName)
            {
                building.OnCaptureEventReceived(BuildingOwner.Player, capturedByNickname);
                UpdateBuildingVisual(building);
                break;
            }
        }
    }

    void UpdateBuildingVisual(Building building)
    {
        Color color = building.GetOwner() switch
        {
            BuildingOwner.None => Color.gray,
            BuildingOwner.Player => building.GetOwnerNickname() == PhotonNetwork.NickName ? Color.blue : Color.red,
            BuildingOwner.Enemy => Color.red,
            _ => Color.gray
        };
        
        DrawBuilding(building, color);
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
        InitializeBuilding(Buildings.A, Color.red);
        // B동
        InitializeBuilding(Buildings.B, Color.red);
        // C동
        InitializeBuilding(Buildings.C, Color.red);
        // D동
        InitializeBuilding(Buildings.D, Color.red);
        // 제2기숙사
        InitializeBuilding(Buildings.Dorm, Color.red);
        // E동
        InitializeBuilding(Buildings.E, Color.red);
        // 학생회관, 중앙도서관
        InitializeBuilding(Buildings.GH, Color.red);
        // 과학관
        InitializeBuilding(Buildings.I, Color.red);
        // 제3공학관
        InitializeBuilding(Buildings.J, Color.red);
        // 제1공학관
        InitializeBuilding(Buildings.K, Color.red);
        // 제2공학관
        InitializeBuilding(Buildings.P, Color.red);
        // 정보통신센터
        InitializeBuilding(Buildings.Q, Color.red);
        // 홍문관
        InitializeBuilding(Buildings.R, Color.red);
        // 강당
        InitializeBuilding(Buildings.S, Color.red);
        // 제4공학관
        InitializeBuilding(Buildings.T, Color.red);
        // 미술종합강의동
        InitializeBuilding(Buildings.U, Color.red);
        // 제1강의동
        InitializeBuilding(Buildings.Z1, Color.red);
        // 이천득관
        InitializeBuilding(Buildings.Z2, Color.red);
        // 제3강의동
        InitializeBuilding(Buildings.Z3, Color.red);
        // 제4강의동
        InitializeBuilding(Buildings.Z4, Color.red);
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
    }

    DrawingElement DrawColor(List<GeoPoint> outline, Color color, float alpha)
    {
        color.a = alpha;
        DrawingElement element = new DrawingElement();
        element.points = outline;
        element.color = color;
        element.visible = true;
        element.Fill = true;
        map.drawingElementManager.Add(element);
        return element;
    }
    public void MoveToMyLocation()
    {
        map.location = myLocation;
    }
}
