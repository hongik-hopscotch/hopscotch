using UnityEngine;
using System.Collections;
using Photon.Pun;

public class LoadingManager : MonoBehaviourPunCallbacks
{
    [SerializeField]
    private float loadingTime = 3f;

    void Start()
    {
        if (PhotonNetwork.IsMasterClient)
        {
            StartCoroutine(LoadGameScene());
        }
    }

    IEnumerator LoadGameScene()
    {
        yield return new WaitForSeconds(loadingTime);
        PhotonNetwork.LoadLevel("HopscotchMap");
    }
} 