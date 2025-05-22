using OnlineMaps;
using UnityEngine;
using System.Collections.Generic;
using Photon.Pun;
using ExitGames.Client.Photon;

public enum BuildingOwner
{
    None,
    Player,
    Enemy
}

[System.Serializable]
public class Building
{
    private string name;
    private List<GeoPoint> outline;
    private BuildingOwner currentOwner;
    private string ownerNickname;  // 점령한 플레이어의 닉네임
    private float captureProgress;
    private const float CAPTURE_TIME = 3.0f; // 점령하는데 걸리는 시간(초)
    
    // 이벤트 코드 정의
    public const byte BuildingCaptureEventCode = 1;
    
    public string GetName() => name;
    public List<GeoPoint> GetOutline() => outline;
    public BuildingOwner GetOwner() => currentOwner;
    public string GetOwnerNickname() => ownerNickname;
    public float GetCaptureProgress() => captureProgress;

    public Building(string name, List<GeoPoint> outline)
    {
        this.name = name;
        this.outline = outline;
        this.currentOwner = BuildingOwner.None;
        this.ownerNickname = "";
        this.captureProgress = 0f;
    }

    public bool IsInRange(GeoPoint playerPosition, float captureRange)
    {
        // 먼저 건물 내부에 있는지 확인
        if (IsPointInPolygon(playerPosition))
        {
            return true;
        }

        // 건물 외부에 있다면 외곽선과의 거리 확인
        foreach (var point in outline)
        {
            float distance = (float)GeoPoint.Distance(point, playerPosition);
            if (distance <= captureRange)
                return true;
        }
        return false;
    }

    // Point in Polygon 알고리즘 (Ray Casting)
    private bool IsPointInPolygon(GeoPoint point)
    {
        bool inside = false;
        int j = outline.Count - 1;

        for (int i = 0; i < outline.Count; i++)
        {
            if (((outline[i].latitude > point.latitude) != (outline[j].latitude > point.latitude)) &&
                (point.longitude < (outline[j].longitude - outline[i].longitude) * (point.latitude - outline[i].latitude) / 
                (outline[j].latitude - outline[i].latitude) + outline[i].longitude))
            {
                inside = !inside;
            }
            j = i;
        }

        return inside;
    }

    public bool UpdateCapture(float deltaTime, bool isCapturing, string playerNickname)
    {
        if (!isCapturing)
        {
            captureProgress = Mathf.Max(0f, captureProgress - (deltaTime / CAPTURE_TIME));
            return false;
        }

        captureProgress = Mathf.Min(1f, captureProgress + (deltaTime / CAPTURE_TIME));
        if (captureProgress >= 1f)
        {
            currentOwner = BuildingOwner.Player;
            ownerNickname = playerNickname;
            return true;
        }
        return false;
    }

    public void OnCaptureEventReceived(BuildingOwner owner, string nickname)
    {
        currentOwner = owner;
        ownerNickname = nickname;
        captureProgress = owner == BuildingOwner.None ? 0f : 1f;
    }

    public void ResetCapture()
    {
        captureProgress = 0f;
    }
}

class Buildings
{
    public static Building A = new Building(
        "인문사회관A동",
        new List<GeoPoint> {
            new GeoPoint(126.9257621, 37.5502284),
            new GeoPoint(126.9259072, 37.5502259),
            new GeoPoint(126.9258811, 37.5492727),
            new GeoPoint(126.925736, 37.5492752),
            new GeoPoint(126.9257621, 37.5502284)
        }
    );
    public static Building B = new Building(
        "인문사회관B동",
        new List<GeoPoint> {
            new GeoPoint(126.9256192, 37.550085),
            new GeoPoint(126.9262822, 37.5500566),
            new GeoPoint(126.9262764, 37.5499713),
            new GeoPoint(126.9256134, 37.5499996),
            new GeoPoint(126.9256192, 37.550085)
        }
    );
    public static Building C = new Building(
        "인문사회관C동",
        new List<GeoPoint> {
            new GeoPoint(126.9259927, 37.5492214),
            new GeoPoint(126.9256101, 37.5492301),
            new GeoPoint(126.9256061, 37.5491193),
            new GeoPoint(126.9256408, 37.5491185),
            new GeoPoint(126.9256394, 37.5490787),
            new GeoPoint(126.9264966, 37.5490594),
            new GeoPoint(126.926502, 37.5492099),
            new GeoPoint(126.9262229, 37.5492162),
            new GeoPoint(126.9262453, 37.5498392),
            new GeoPoint(126.9260048, 37.5498446),
            new GeoPoint(126.9259962, 37.5496049),
            new GeoPoint(126.9258084, 37.5496091),
            new GeoPoint(126.9258049, 37.5495121),
            new GeoPoint(126.926003, 37.5495076),
            new GeoPoint(126.9259927, 37.5492214)
        }
    );
    public static Building D = new Building(
        "인문사회관D동",
        new List<GeoPoint> {
            new GeoPoint(126.9257163, 37.5490579),
            new GeoPoint(126.9257305, 37.5489371),
            new GeoPoint(126.9262916, 37.5489306),
            new GeoPoint(126.9262916, 37.5490483),
            new GeoPoint(126.9257163, 37.5490579)
        }
    );
    public static Building Dorm = new Building(
        "제2기숙사",
        new List<GeoPoint> {
            new GeoPoint(126.9243483, 37.5497984),
            new GeoPoint(126.9245106, 37.5498126),
            new GeoPoint(126.9245541, 37.5494995),
            new GeoPoint(126.9252409, 37.5495595),
            new GeoPoint(126.9252495, 37.5494982),
            new GeoPoint(126.9252312, 37.5494966),
            new GeoPoint(126.9252411, 37.5494255),
            new GeoPoint(126.9252863, 37.5494081),
            new GeoPoint(126.9253029, 37.5493251),
            new GeoPoint(126.9249363, 37.5493949),
            new GeoPoint(126.9244109, 37.5493489),
            new GeoPoint(126.9243483, 37.5497984)
        }
    );
    public static Building E = new Building(
        "조형관",
        new List<GeoPoint> {
            new GeoPoint(126.9262433, 37.550505),
            new GeoPoint(126.9260293, 37.5505113),
            new GeoPoint(126.9260137, 37.5501806),
            new GeoPoint(126.9261041, 37.5501779),
            new GeoPoint(126.9261019, 37.5501319),
            new GeoPoint(126.9260589, 37.5501332),
            new GeoPoint(126.9260576, 37.5501043),
            new GeoPoint(126.9261356, 37.550102),
            new GeoPoint(126.9261391, 37.5501761),
            new GeoPoint(126.9262277, 37.5501735),
            new GeoPoint(126.9262433, 37.550505)
        }
    );
    public static Building F = new Building(
        "미술학관",
        new List<GeoPoint> {
            new GeoPoint(126.9261269, 37.5508285),
            new GeoPoint(126.9262256, 37.5508248),
            new GeoPoint(126.926223, 37.5507804),
            new GeoPoint(126.9262619, 37.5507789),
            new GeoPoint(126.9262637, 37.5508095),
            new GeoPoint(126.9263712, 37.5508054),
            new GeoPoint(126.9263687, 37.5507636),
            new GeoPoint(126.9263928, 37.5507627),
            new GeoPoint(126.9263944, 37.5507895),
            new GeoPoint(126.9265065, 37.5507854),
            new GeoPoint(126.9265036, 37.5507365),
            new GeoPoint(126.9265908, 37.5507347),
            new GeoPoint(126.9265794, 37.5507429),
            new GeoPoint(126.9265666, 37.5507617),
            new GeoPoint(126.9265625, 37.5507829),
            new GeoPoint(126.9265677, 37.5508038),
            new GeoPoint(126.9265815, 37.5508222),
            new GeoPoint(126.9266023, 37.5508358),
            new GeoPoint(126.9266277, 37.550843),
            new GeoPoint(126.9266547, 37.5508431),
            new GeoPoint(126.9266776, 37.550837),
            new GeoPoint(126.9266971, 37.5508256),
            new GeoPoint(126.9267113, 37.5508102),
            new GeoPoint(126.926719, 37.550792),
            new GeoPoint(126.9267193, 37.5507728),
            new GeoPoint(126.9267123, 37.5507545),
            new GeoPoint(126.9266987, 37.5507387),
            new GeoPoint(126.9266771, 37.5507258),
            new GeoPoint(126.9266513, 37.5507195),
            new GeoPoint(126.9266402, 37.5504086),
            new GeoPoint(126.9265039, 37.5504152),
            new GeoPoint(126.926513, 37.5505682),
            new GeoPoint(126.9261123, 37.5505832),
            new GeoPoint(126.9261269, 37.5508285)
        }
    );
    public static Building GH = new Building(
        "학생회관, 중앙도서관",
        new List<GeoPoint> {
            new GeoPoint(126.9262568, 37.5515714),
            new GeoPoint(126.9263813, 37.551535),
            new GeoPoint(126.9263387, 37.5514434),
            new GeoPoint(126.9265814, 37.5513724),
            new GeoPoint(126.9266267, 37.5514699),
            new GeoPoint(126.9268549, 37.5514031),
            new GeoPoint(126.9266554, 37.5509742),
            new GeoPoint(126.9264556, 37.5510326),
            new GeoPoint(126.9265549, 37.5512461),
            new GeoPoint(126.9262701, 37.5513294),
            new GeoPoint(126.9261055, 37.5509755),
            new GeoPoint(126.9259945, 37.551008),
            new GeoPoint(126.9262568, 37.5515714)
        }
    );
    public static Building I = new Building(
        "과학관",
        new List<GeoPoint> {
            new GeoPoint(126.927243, 37.5516755),
            new GeoPoint(126.9271324, 37.5515491),
            new GeoPoint(126.9270921, 37.5515713),
            new GeoPoint(126.9270308, 37.5515013),
            new GeoPoint(126.9270734, 37.5514779),
            new GeoPoint(126.9270137, 37.5514097),
            new GeoPoint(126.9272143, 37.5512993),
            new GeoPoint(126.9274459, 37.5515639),
            new GeoPoint(126.927243, 37.5516755)
        }
    );
    public static Building J = new Building(
        "제3공학관",
        new List<GeoPoint> {
            new GeoPoint(126.9267332, 37.5517256),
            new GeoPoint(126.9267748, 37.5517701),
            new GeoPoint(126.9270983, 37.5515801),
            new GeoPoint(126.9271742, 37.5516614),
            new GeoPoint(126.9268772, 37.5518358),
            new GeoPoint(126.9268598, 37.5518172),
            new GeoPoint(126.926759, 37.5518764),
            new GeoPoint(126.9267246, 37.5518396),
            new GeoPoint(126.9267386, 37.5518314),
            new GeoPoint(126.9267163, 37.5518075),
            new GeoPoint(126.9266745, 37.551832),
            new GeoPoint(126.9265869, 37.5517382),
            new GeoPoint(126.9266793, 37.551684),
            new GeoPoint(126.9266441, 37.5516462),
            new GeoPoint(126.9269449, 37.5514696),
            new GeoPoint(126.9270244, 37.5515547),
            new GeoPoint(126.9267332, 37.5517256)
        }
    );
    public static Building K = new Building(
        "제1공학관",
        new List<GeoPoint> {
            new GeoPoint(126.9254822, 37.5525616),
            new GeoPoint(126.92541, 37.5524833),
            new GeoPoint(126.9255846, 37.5523821),
            new GeoPoint(126.92557, 37.5523663),
            new GeoPoint(126.926582, 37.5517791),
            new GeoPoint(126.9266585, 37.551862),
            new GeoPoint(126.9265521, 37.5519236),
            new GeoPoint(126.9265628, 37.5519352),
            new GeoPoint(126.9254822, 37.5525616)
        }
    );
    public static Building L = new Building(
        "와우관",
        new List<GeoPoint> {
            new GeoPoint(126.9264546, 37.5518061),
            new GeoPoint(126.9266588, 37.551682),
            new GeoPoint(126.9266278, 37.55165),
            new GeoPoint(126.9266476, 37.551638),
            new GeoPoint(126.9265358, 37.5515224),
            new GeoPoint(126.9263118, 37.5516585),
            new GeoPoint(126.9264546, 37.5518061)
        }
    );
    public static Building M = new Building(
        "체육관",
        new List<GeoPoint> {
            new GeoPoint(126.9244192, 37.5521723),
            new GeoPoint(126.9244022, 37.5521363),
            new GeoPoint(126.924375, 37.5521444),
            new GeoPoint(126.9243622, 37.5521176),
            new GeoPoint(126.9243073, 37.552134),
            new GeoPoint(126.924203, 37.5519143),
            new GeoPoint(126.9242695, 37.5518945),
            new GeoPoint(126.9242099, 37.5517691),
            new GeoPoint(126.9242213, 37.5517657),
            new GeoPoint(126.9242072, 37.5517361),
            new GeoPoint(126.9243307, 37.5516991),
            new GeoPoint(126.9242729, 37.5515778),
            new GeoPoint(126.9243442, 37.5515565),
            new GeoPoint(126.9243186, 37.5515027),
            new GeoPoint(126.9245176, 37.5514432),
            new GeoPoint(126.9245719, 37.5515572),
            new GeoPoint(126.9245603, 37.5515607),
            new GeoPoint(126.9245906, 37.5516245),
            new GeoPoint(126.9246046, 37.5516204),
            new GeoPoint(126.9248109, 37.5520557),
            new GeoPoint(126.9244192, 37.5521723)
        }
    );
    public static Building MH = new Building(
        "문헌관",
        new List<GeoPoint> {
            new GeoPoint(126.9258298, 37.5503345),
            new GeoPoint(126.9259752, 37.5503294),
            new GeoPoint(126.925991, 37.550613),
            new GeoPoint(126.926003, 37.5506126),
            new GeoPoint(126.9260062, 37.5506714),
            new GeoPoint(126.9260163, 37.550671),
            new GeoPoint(126.9260153, 37.5506528),
            new GeoPoint(126.9260561, 37.5506172),
            new GeoPoint(126.9260954, 37.5506454),
            new GeoPoint(126.9260999, 37.5507546),
            new GeoPoint(126.9260639, 37.5507856),
            new GeoPoint(126.9260275, 37.5507591),
            new GeoPoint(126.9260265, 37.5507412),
            new GeoPoint(126.9260185, 37.5507415),
            new GeoPoint(126.9260215, 37.5507952),
            new GeoPoint(126.9260024, 37.5507959),
            new GeoPoint(126.9260104, 37.5509422),
            new GeoPoint(126.9258655, 37.5509473),
            new GeoPoint(126.9258573, 37.5507998),
            new GeoPoint(126.925805, 37.5508016),
            new GeoPoint(126.9257949, 37.5506183),
            new GeoPoint(126.9258454, 37.5506166),
            new GeoPoint(126.9258298, 37.5503345)
        }
    );
    public static Building P = new Building(
        "제2공학관",
        new List<GeoPoint> {
            new GeoPoint(126.9269191, 37.5513205),
            new GeoPoint(126.9270194, 37.5512928),
            new GeoPoint(126.9270102, 37.5512719),
            new GeoPoint(126.9270411, 37.5512633),
            new GeoPoint(126.9270353, 37.5512501),
            new GeoPoint(126.927107, 37.5512304),
            new GeoPoint(126.9269342, 37.5508356),
            new GeoPoint(126.9268544, 37.5508575),
            new GeoPoint(126.9268467, 37.5508399),
            new GeoPoint(126.9267328, 37.5508713),
            new GeoPoint(126.926766, 37.5509472),
            new GeoPoint(126.9267845, 37.5509421),
            new GeoPoint(126.9269196, 37.5512503),
            new GeoPoint(126.9268917, 37.5512579),
            new GeoPoint(126.9269191, 37.5513205)
        }
    );
    public static Building Q = new Building(
        "정보통신센터",
        new List<GeoPoint> {
            new GeoPoint(126.9266383, 37.5509686),
            new GeoPoint(126.926614, 37.5509165),
            new GeoPoint(126.9265798, 37.5509265),
            new GeoPoint(126.9265151, 37.5507874),
            new GeoPoint(126.9261583, 37.5508917),
            new GeoPoint(126.9261921, 37.5509644),
            new GeoPoint(126.9262246, 37.5509549),
            new GeoPoint(126.9262443, 37.5509971),
            new GeoPoint(126.9262001, 37.5510101),
            new GeoPoint(126.9262708, 37.5511621),
            new GeoPoint(126.9264716, 37.5511033),
            new GeoPoint(126.9264364, 37.5510277),
            new GeoPoint(126.9266383, 37.5509686)
        }
    );
    public static Building R = new Building(
        "홍문관",
        new List<GeoPoint> {
            new GeoPoint(126.9243455, 37.5522547),
            new GeoPoint(126.9244031, 37.5523051),
            new GeoPoint(126.924706, 37.552565),
            new GeoPoint(126.9250592, 37.5528496),
            new GeoPoint(126.9252087, 37.552733),
            new GeoPoint(126.9250942, 37.5526408),
            new GeoPoint(126.9252151, 37.5525651),
            new GeoPoint(126.9253039, 37.5526585),
            new GeoPoint(126.9254655, 37.552561),
            new GeoPoint(126.9253712, 37.5524699),
            new GeoPoint(126.9254485, 37.5524203),
            new GeoPoint(126.9253129, 37.5522796),
            new GeoPoint(126.9252301, 37.5523352),
            new GeoPoint(126.9251878, 37.5522939),
            new GeoPoint(126.924914, 37.552484),
            new GeoPoint(126.9249444, 37.5525087),
            new GeoPoint(126.9249353, 37.5525129),
            new GeoPoint(126.9248554, 37.5524485),
            new GeoPoint(126.9245546, 37.5521949),
            new GeoPoint(126.924499, 37.5521484),
            new GeoPoint(126.9243455, 37.5522547)
        }
    );
    public static Building S = new Building(
        "강당",
        new List<GeoPoint> {
            new GeoPoint(126.9250512, 37.5504959),
            new GeoPoint(126.925286, 37.5504937),
            new GeoPoint(126.9252812, 37.5501694),
            new GeoPoint(126.9252808, 37.5501439),
            new GeoPoint(126.925046, 37.5501461),
            new GeoPoint(126.9250463, 37.5501654),
            new GeoPoint(126.9250512, 37.5504959)
        }
    );
    public static Building T = new Building(
        "제4공학관",
        new List<GeoPoint> {
            new GeoPoint(126.924305, 37.5500141),
            new GeoPoint(126.9249373, 37.5500905),
            new GeoPoint(126.9249239, 37.55016),
            new GeoPoint(126.9249096, 37.5502347),
            new GeoPoint(126.9242772, 37.5501583),
            new GeoPoint(126.924305, 37.5500141)
        }
    );
    public static Building U = new Building(
        "미술종합강의동",
        new List<GeoPoint> {
            new GeoPoint(126.9263667, 37.5502073),
            new GeoPoint(126.9264458, 37.5502046),
            new GeoPoint(126.9264511, 37.5503031),
            new GeoPoint(126.9265923, 37.5502983),
            new GeoPoint(126.9265875, 37.5502099),
            new GeoPoint(126.92656, 37.5502108),
            new GeoPoint(126.9265505, 37.550034),
            new GeoPoint(126.9264645, 37.550037),
            new GeoPoint(126.9264582, 37.5499211),
            new GeoPoint(126.9263514, 37.5499247),
            new GeoPoint(126.9263667, 37.5502073)
        }
    );
    public static Building Z1 = new Building(
        "제1강의동",
        new List<GeoPoint> {
            new GeoPoint(126.9254641, 37.5510641),
            new GeoPoint(126.9254335, 37.5501903),
            new GeoPoint(126.9255633, 37.5501875),
            new GeoPoint(126.9255939, 37.5510612),
            new GeoPoint(126.9254641, 37.5510641)
        }
    );
    public static Building Z2 = new Building(
        "이천득관",
        new List<GeoPoint> {
            new GeoPoint(126.9249942, 37.550048),
            new GeoPoint(126.9250179, 37.5499314),
            new GeoPoint(126.925206, 37.5499554),
            new GeoPoint(126.9253286, 37.5493519),
            new GeoPoint(126.9255021, 37.549374),
            new GeoPoint(126.925384, 37.5499548),
            new GeoPoint(126.9253557, 37.5500942),
            new GeoPoint(126.9249942, 37.550048)
        }
    );
    public static Building Z3 = new Building(
        "제3강의동",
        new List<GeoPoint> {
            new GeoPoint(126.9242904, 37.549846),
            new GeoPoint(126.9242712, 37.5499447),
            new GeoPoint(126.9249361, 37.5500261),
            new GeoPoint(126.9249446, 37.5499824),
            new GeoPoint(126.9248997, 37.5499769),
            new GeoPoint(126.9249104, 37.5499219),
            new GeoPoint(126.9242904, 37.549846)
        }
    );
    public static Building Z4 = new Building(
        "제4강의동",
        new List<GeoPoint> {
            new GeoPoint(126.9243958, 37.5509036),
            new GeoPoint(126.924335, 37.5503495),
            new GeoPoint(126.9244791, 37.5503395),
            new GeoPoint(126.9245268, 37.5507743),
            new GeoPoint(126.9251075, 37.5507343),
            new GeoPoint(126.9251206, 37.5508535),
            new GeoPoint(126.9243958, 37.5509036)
        }
    );
}
