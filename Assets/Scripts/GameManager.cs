using UnityEngine;
using UnityEngine.SceneManagement;

public enum GameState
{
    Lobby,
    Loading,
    Playing,
    GameOver
}

public class GameManager : MonoBehaviour
{
    public static GameManager Instance
    {
        get; private set;
    }

    public GameState CurrentState
    {
        get; private set;
    }

    private void Awake()
    {
        // SingleTon
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        // 씬이 바뀌어도 유지되도록록
        DontDestroyOnLoad(gameObject);
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        SetGameState(GameState.Lobby);   
    }

    public void SetGameState(GameState state)
    {
        CurrentState = state;
        Debug.Log($"이제 {state} 씬으로 넘어갈 예정이다.");

        switch (state)
        {
            case GameState.Lobby:
                break;
            case GameState.Loading:
                // SceneManager.LoadScene("LoadingScene"); 
                SceneManager.LoadScene("HopscotchMap"); // 테스트 용
                break;
            case GameState.Playing:
                SceneManager.LoadScene("PlayingScene");
                break;
            case GameState.GameOver:
                SceneManager.LoadScene("GameOverScene");
                break;
        }
    }

    // 일단은 시작할 때 클릭 형식으로 했습니다.

    public void OnPlayButtonClicked()
    {
        SetGameState(GameState.Loading);
        
    }


    // Update is called once per frame
    void Update()
    {
        
    }
}
