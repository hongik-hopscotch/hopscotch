using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;
using System;
using System.Collections;


public class LobbyManager : MonoBehaviour
{

    public TMP_InputField codeInput;
    public Button joinButton;
    public Button createButton;
    // 에러 및 관련 text 생성기
    public TextMeshProUGUI messageText;
    // 생성된 자동 코드
    public TextMeshProUGUI createdCodeText;

    private string currentCode;
    private Coroutine expireCoroutine;

    void Start()
    {
        joinButton.onClick.AddListener(OnJoinClicked);
        createButton.onClick.AddListener(OnCreateClicked);
        createdCodeText.text = "";
        messageText.text = "";
    }

    void OnCreateClicked()
    {
        currentCode = System.Guid.NewGuid().ToString().Substring(0, 6).ToUpper();

        if (RoomManager.CreateRoom(currentCode))
        {
            createdCodeText.text = $"Created code: {currentCode}";
            messageText.text = "Enter in 3 Mins!";

            if (expireCoroutine != null) StopCoroutine(expireCoroutine);
            expireCoroutine = StartCoroutine(CodeExpireCountdown(currentCode));
        }
        else
        {
            messageText.text = "Try again";
        }
    }

    void OnJoinClicked()
    {
        string inputCode = codeInput.text.Trim().ToUpper();

        if (string.IsNullOrEmpty(inputCode))
        {
            messageText.text = "Enter the Code!";
            return;
        }

        if (RoomManager.JoinRoom(inputCode))
        {
            messageText.text = "Game is loading...";
            StartCoroutine(WaitAndStart(inputCode));
        }
        else
        {
            messageText.text = "Wrong or expired code";
        }
    }

    IEnumerator WaitAndStart(string code)
    {
        yield return new WaitForSeconds(1f); // UI 확인용 잠깐 대기

        if (RoomManager.IsRoomReady(code))
        {
            SceneManager.LoadScene("Intro"); // 인트로 화면으로 이동
        }
        else
        {
            messageText.text = "Player is not yet.";
        }
    }

    IEnumerator CodeExpireCountdown(string code)
    {
        yield return new WaitForSeconds(180f);

        if (code == currentCode)
        {
            createdCodeText.text = "Code has expired";
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
