using UnityEngine;
using System.Collections;
using Photon.Pun;
using Photon.Realtime;

public class LoadingManager : MonoBehaviourPunCallbacks
{
    [SerializeField]
    private float loadingTime = 3f;

    void Start()
    {
        Debug.Log($"LoadingManager 시작 - 마스터 클라이언트: {PhotonNetwork.IsMasterClient}, 연결 상태: {PhotonNetwork.IsConnected}");
        StartCoroutine(LoadGameScene());
    }

    IEnumerator LoadGameScene()
    {
        Debug.Log("로딩 시작...");
        yield return new WaitForSeconds(loadingTime);
        Debug.Log($"로딩 완료 - 마스터 클라이언트: {PhotonNetwork.IsMasterClient}");
        
        // 모든 클라이언트가 씬을 로드하도록 수정
        if (PhotonNetwork.IsConnected)
        {
            Debug.Log("HopscotchMap 씬으로 전환 시도");
            PhotonNetwork.LoadLevel("HopscotchMap");
        }
        else
        {
            Debug.LogError("Photon 서버에 연결되어 있지 않습니다!");
        }
    }

    public override void OnDisconnected(DisconnectCause cause)
    {
        Debug.LogError($"서버 연결 끊김: {cause}");
    }
} 