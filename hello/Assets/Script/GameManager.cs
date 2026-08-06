using System;
using UnityEngine;
using UnityEngine.SceneManagement;

public enum GameState
{
    Title,
    Playing,
    EscapeReady,
    Won,
    Lost
}

public class GameManager : MonoBehaviour
{
    private const string TitleSceneName = "TitleScene";
    private const string PlaySceneName = "PlayScene";

    private static GameManager instance;
    private CharacterManager characterManager;

    public static GameManager Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindAnyObjectByType<GameManager>();
            }

            if (instance == null)
            {
                GameObject managerObject = new GameObject("GameManager");
                instance = managerObject.AddComponent<GameManager>();
            }

            return instance;
        }
    }

    public static GameManager Current => instance;

    public GameState State { get; private set; } = GameState.Title;
    public bool IsGameOver => State is GameState.Won or GameState.Lost;
    public bool IsEscapeReady => State == GameState.EscapeReady;

    public event Action<GameState> StateChanged;

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Debug.LogWarning("중복된 GameManager가 발견되어 파괴되었습니다.", this);
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += HandleSceneLoaded;
    }

    private void Start()
    {
        ConfigureForScene(SceneManager.GetActiveScene());
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= HandleSceneLoaded;
        DetachCharacterManager();
    }

    private void OnDestroy()
    {
        if (instance == this)
        {
            instance = null;
        }
    }

    public void AttachCharacterManager(CharacterManager manager)
    {
        if (characterManager == manager)
        {
            return;
        }

        DetachCharacterManager();
        characterManager = manager;

        if (characterManager != null)
        {
            characterManager.CharacterDied += HandleCharacterDied;
        }
    }

    public bool RequestEscape(CharacterEntity entity)
    {
        if (State != GameState.EscapeReady ||
            entity == null ||
            entity.Role != CharacterRole.Player ||
            !entity.IsAlive)
        {
            return false;
        }

        FinishGame(GameState.Won);
        return true;
    }

    public void RestartGame()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(PlaySceneName);
    }

    public void ReturnToTitle()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(TitleSceneName);
    }

    private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        ConfigureForScene(scene);
    }

    private void ConfigureForScene(Scene scene)
    {
        Time.timeScale = 1f;
        DetachCharacterManager();

        if (scene.name == PlaySceneName)
        {
            AttachCharacterManager(FindAnyObjectByType<CharacterManager>());
            ChangeState(GameState.Playing, true);
            EscapeZone.EnsureForScene();
            return;
        }

        ChangeState(GameState.Title, true);
    }

    private void HandleCharacterDied(CharacterEntity entity)
    {
        if (entity == null || IsGameOver)
        {
            return;
        }

        switch (entity.Role)
        {
            case CharacterRole.Player:
                FinishGame(GameState.Lost);
                break;

            case CharacterRole.Boss when State == GameState.Playing:
                ChangeState(GameState.EscapeReady);
                Debug.Log("보스를 처치했습니다. 탈출 지점으로 이동하세요.", this);
                break;
        }
    }

    private void FinishGame(GameState result)
    {
        ChangeState(result);
        Time.timeScale = 0f;
        Debug.Log(result == GameState.Won ? "게임 승리" : "게임 패배", this);
    }

    private void ChangeState(GameState nextState, bool forceNotify = false)
    {
        if (!forceNotify && State == nextState)
        {
            return;
        }

        State = nextState;
        StateChanged?.Invoke(State);
    }

    private void DetachCharacterManager()
    {
        if (characterManager != null)
        {
            characterManager.CharacterDied -= HandleCharacterDied;
            characterManager = null;
        }
    }
}
