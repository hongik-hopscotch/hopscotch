using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;
using System;
using System.Collections;
using UnityEngine.Rendering.Universal.Internal;


public class LobbyManager : MonoBehaviour
{
    public Button createRoomButton;
    public Button joinRoomButton;
    // Join Room 시에 들어오는 input value
    public TMP_InputField joinCodeInputField;

    // debug.log results
    public TextMeshProUGUI debugText;

    public TextMeshProUGUI countdownText;

    // generate Room Code
    public TextMeshProUGUI roomCodeText;

    private string currentRoomCode;
    private float roomLifetime = 180f;
    private float timeRemaining;
    private bool isRoomActive = false;


    void Start()
    {
        createRoomButton.onClick.AddListener(OnCreateRoomButtonClick);
        joinRoomButton.onClick.AddListener(OnJoinRoomButtonClick);

        debugText.text = "Player entered";
        countdownText.text = "";
        roomCodeText.text = "";

        // 처음에는 숨김
        joinCodeInputField.gameObject.SetActive(false);
    }



    // Update is called once per frame
    void Update()
    {
        if (isRoomActive)
        {
            timeRemaining -= Time.deltaTime;

            if (timeRemaining > 0)
            {
                TimeSpan t = TimeSpan.FromSeconds(timeRemaining);
                countdownText.text = $"Remaining time: {t.Minutes:D2}:{t.Seconds:D2}";
            }
            else
            {
                countdownText.text = "Room expired";
                debugText.text = "The room code has expired";
                isRoomActive = false;
                currentRoomCode = null;
            }
        }
    }

    void OnCreateRoomButtonClick()
    {
        currentRoomCode = GenerateRoomCode();
        debugText.text = $"Room code : {currentRoomCode} (3mins)";
        roomCodeText.text = $"room code: {currentRoomCode}";

        Debug.Log($"Room Created Code: {currentRoomCode}");
        timeRemaining = roomLifetime;
        isRoomActive = true;
        joinCodeInputField.gameObject.SetActive(false);
    }

    void OnJoinRoomButtonClick()
    {

        // 입력창이 꺼져 있으면 염
        if (!joinCodeInputField.gameObject.activeSelf)
        {
            joinCodeInputField.gameObject.SetActive(true);
            debugText.text = "enter the code";
            return;
        }
        string inputCode = joinCodeInputField.text.ToUpper().Trim();

        if (string.IsNullOrEmpty(inputCode))
        {
            debugText.text = "Enter code";
            return;
        }

        if (inputCode == currentRoomCode && isRoomActive)
        {
            debugText.text = $"{inputCode} game start";
            Debug.Log("Join Success");
        }
        else
        {
            debugText.text = "code not correct";
            Debug.Log("Join Failed");
        }


    }

    string GenerateRoomCode()
    {
        const string chars = "QWERTYUIOPASDFGHJKLZXCVBNM0987654321";
        System.Random rand = new System.Random();
        char[] code = new char[6];
        for (int i = 0; i < code.Length; i++)
        {
            code[i] = chars[rand.Next(chars.Length)];
        }
        return new string(code);
    }
}
