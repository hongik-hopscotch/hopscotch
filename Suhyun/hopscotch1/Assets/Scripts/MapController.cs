using UnityEngine;
using System.Collections;
using OnlineMaps;
using System.Collections.Generic;

public class MapController : MonoBehaviour
{
    public Map map;
    private Marker2D myMarker;
    public GeoPoint myLocation;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        myLocation = new GeoPoint(0, 0);

        // test
       // StartCoroutine(TestLoc());
        //

        // real
        StartCoroutine(GetLocation());
        //

        Input.compass.enabled = true;
        myMarker = Marker2DManager.CreateItem(myLocation);
        myMarker.texture = Resources.Load<Texture2D>("MarkerBlue");

        StartCoroutine(MapInit());

        DrawColor(Buildings.C.GetOutline(), Color.red, 0.5f);
        DrawColor(Buildings.R.GetOutline(), Color.green, 0.5f);
        DrawColor(Buildings.T.GetOutline(), Color.blue, 0.5f);
    }

    // Update is called once per frame
    void Update()
    {
        myMarker.location = myLocation;

        float heading = Input.compass.trueHeading / 360f;
        myMarker.rotation = Mathf.LerpAngle(myMarker.rotation, heading, Time.deltaTime * 10f);
    }

    IEnumerator MapInit()
    {
        while (myLocation.latitude == 0)
        {
            yield return null;
        }
        map.location = myLocation;
    }

    IEnumerator TestLoc()
    {
        while (true)
        {
            myLocation.latitude += 0.0001f;
            myLocation.longitude += 0.0001f;
            yield return new WaitForSeconds(0.5f);
        }
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

    void DrawColor(List<GeoPoint> outline, Color color, float alpha)
    {
        Polygon polygon = new Polygon(outline, color, 5, new Color(color.r, color.g, color.b, alpha));
        map.drawingElementManager.Add(polygon);
    }
    public void MoveToMyLocation()
    {
        map.location = myLocation;
    }
}
