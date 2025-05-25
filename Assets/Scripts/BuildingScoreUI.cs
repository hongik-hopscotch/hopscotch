using UnityEngine;
using TMPro;
using Photon.Pun;

public class BuildingScoreUI : MonoBehaviourPunCallbacks
{
    [SerializeField] private TextMeshProUGUI scoreText;
    private MapController mapController;

    private int myBuildingCount = 0;
    private int enemyBuildingCount = 0;

    void Start()
    {
        mapController = FindObjectOfType<MapController>();
        if (mapController == null)
        {
            Debug.LogError("MapController를 찾을 수 없습니다!");
            return;
        }

        UpdateScoreUI();
    }

    public void UpdateScore(int myCount, int enemyCount)
    {
        myBuildingCount = myCount;
        enemyBuildingCount = enemyCount;
        UpdateScoreUI();
    }

    private void UpdateScoreUI()
    {
        if (scoreText != null)
        {
            scoreText.text = $"내 건물: {myBuildingCount} | 상대방 건물: {enemyBuildingCount}";
        }
    }
} 