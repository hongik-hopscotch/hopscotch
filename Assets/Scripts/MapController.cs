using UnityEngine;
using System.Collections;
using Photon.Pun;
using OnlineMaps;

public class NetworkedMapController : MonoBehaviourPunCallbacks
{
    public Map map;
    private Marker2D myMarker;
    private Marker2D opponentMarker;

    public GeoPoint myLocation;
    public GeoPoint opponentLocation = new GeoPoint(0, 0);

    void Start()
    {
        myLocation = new GeoPoint(0, 0);

        StartCoroutine(GetLocation());

        Input.compass.enabled = true;

        myMarker = Marker2DManager.CreateItem(myLocation);
        myMarker.texture = Resources.Load<Texture2D>("MarkerBlue");

        opponentMarker = Marker2DManager.CreateItem(opponentLocation);
        opponentMarker.texture = Resources.Load<Texture2D>("MarkerRed");

        StartCoroutine(MapInit());
        StartCoroutine(SendLocationRoutine());
    }

    void Update()
    {
        // 내 마커 위치 및 회전 갱신
        myMarker.location = myLocation;

        float heading = Input.compass.trueHeading / 360f;
        myMarker.rotation = Mathf.LerpAngle(myMarker.rotation, heading, Time.deltaTime * 10f);

        // 상대방 마커 갱신
        if (opponentLocation.latitude != 0 && opponentLocation.longitude != 0)
        {
            opponentMarker.location = opponentLocation;
        }
    }

    IEnumerator MapInit()
    {
        while (myLocation.latitude == 0)
            yield return null;

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

            if (timeout < 1 || Input.location.status == LocationServiceStatus.Failed)
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

    public void MoveToMyLocation()
    {
        map.location = myLocation;
    }

    // 상대방에게 내 위치 전송
    IEnumerator SendLocationRoutine()
    {
        while (true)
        {
            if (PhotonNetwork.InRoom && PhotonNetwork.IsConnectedAndReady)
            {
                photonView.RPC("ReceiveOpponentLocation", RpcTarget.Others, myLocation.latitude, myLocation.longitude);
            }
            yield return new WaitForSeconds(0.5f);
        }
    }

    // PunRPC: 다른 플레이어의 네트워크를 통해 호출 가능한 함수로 지정
    // 즉, [PunRPC]가 붙어있어야 다른 클라이언트에서 이 함수를 네트워크 통해 호출할 수 있음

    // 상대방 위치 받아오기
    [PunRPC]
    void ReceiveOpponentLocation(float lat, float lon)
    {
        opponentLocation.latitude = lat;
        opponentLocation.longitude = lon;
    }
}
