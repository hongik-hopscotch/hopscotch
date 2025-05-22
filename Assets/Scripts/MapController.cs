using UnityEngine;
using System.Collections;
using OnlineMaps;
using System.Collections.Generic;
using Mono.Cecil;

public class MapController : MonoBehaviour
{
    public Map map;
    // 현재 플레이어의 위치를 나타내는 marker
    private Marker2D myMarker;
    // 현재 플레이어의 위치 정보
    public GeoPoint myLocation;


    void Start()
    {
        myLocation = new GeoPoint(0, 0);

        /// code for test
        // StartCoroutine(TestLoc());
        //

        // 위치 서비스 활성화
        StartCoroutine(GetLocation());
        
        // 나침반 활성화
        Input.compass.enabled = true;

        // myMarker 생성
        myMarker = Marker2DManager.CreateItem(myLocation);
        myMarker.texture = Resources.Load<Texture2D>("DirectedMarkerBlue");

        // 초기 위치로 이동
        StartCoroutine(MapInit());

        // 건물 하이라이팅 예시
        DrawColor(Buildings.C.GetOutline(), Color.red, 0.5f);
        DrawColor(Buildings.R.GetOutline(), Color.green, 0.5f);
        DrawColor(Buildings.T.GetOutline(), Color.blue, 0.5f);
    }

    // Function: Update()
    // myMarker의 실시간 위치와 방향정보를 프레임 단위로 업데이트 합니다.
    void Update()
    {
        myMarker.location = myLocation;

        float heading = Input.compass.trueHeading / 360f;
        myMarker.rotation = Mathf.LerpAngle(myMarker.rotation, heading, Time.deltaTime * 10f);
    }

    // Coroutine: MapInit()
    // 유효한 위치정보가 업데이트 될 때까지 대기 후, 지도의 시작위치를 설정해주는 함수입니다.
    IEnumerator MapInit()
    {
        while (myLocation.latitude == 0)
        {
            yield return null;
        }
        map.location = myLocation;
    }

    // Coroutine: TestLoc()
    // GPS를 사용할 수 없는 테스트 환경에서, 임의의 초기값을 설정하고 위치를 이동하는 테스트용 함수입니다. 
    IEnumerator TestLoc()
    {
        myLocation.latitude = 37.550882f;
        myLocation.longitude = 126.9258f;
        while (true)
        {
            myLocation.latitude += 0.0001f;
            myLocation.longitude += 0.0001f;
            yield return new WaitForSeconds(0.5f);
        }
    }

    // Coroutine: GetLocation()
    // 위치 서비스를 통해 현재 나의 위치를 전역변수 public GeoPoint myLocation에 업데이트 합니다.
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

    // Function: DrawColor()
    // 건물에 색상 하이라이트를 표시하고, 색상의 투명도를 조절할 수 있습니다.
    // Parameters:
    //     List<GeoPoint> outline: 건물의 외곽선 좌표 리스트
    //     Color color: 적용할 색상
    //     float alpha: color에 대한 투명도 값(0-1)
    void DrawColor(List<GeoPoint> outline, Color color, float alpha)
    {
        Polygon polygon = new Polygon(outline, color, 5, new Color(color.r, color.g, color.b, alpha));
        map.drawingElementManager.Add(polygon);
    }

    // Function: MoveToMyLocaton()
    // 지도의 위치를 현재 나의 위치로 이동합니다.
    public void MoveToMyLocation()
    {
        map.location = myLocation;
    }
}
