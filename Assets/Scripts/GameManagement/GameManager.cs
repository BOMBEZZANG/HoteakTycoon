// Assets/Scripts/GameManager.cs
// 게임의 전반적인 상태와 점수를 관리하는 클래스

using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class GameManager : MonoBehaviour
{
    public enum GameState
    {
        Menu,           // 메인 메뉴
        Playing,        // 게임 중
        Paused,         // 일시정지
        GameOver        // 게임 오버
    }
    
    [Header("게임 상태")]
    public GameState currentState = GameState.Playing;
    
    [Header("점수 시스템")]
    public int currentScore = 0;
    public int highScore = 0;
    public int targetScore = 1000;         // 목표 점수
    
    [Header("시간 관리")]
    public float gameTime = 0f;            // 게임 경과 시간
    public float timeLimit = 300f;         // 시간 제한 (5분)
    public bool hasTimeLimit = false;      // 시간 제한 여부
    
    [Header("UI 연결")]
    public TextMeshProUGUI scoreText;      // 점수 텍스트
    public TextMeshProUGUI highScoreText;  // 최고 점수 텍스트
    public TextMeshProUGUI timeText;       // 시간 텍스트
    public Slider progressSlider;          // 진행도 슬라이더
    public GameObject gameOverPanel;       // 게임 오버 패널
    public GameObject pausePanel;          // 일시정지 패널
    
    [Header("손님 통계 UI")]
    public TextMeshProUGUI totalCustomersText;     // 총 손님 수
    public TextMeshProUGUI satisfiedCustomersText; // 만족한 손님 수
    public TextMeshProUGUI satisfactionRateText;   // 만족도 비율
    
    // 싱글톤
    public static GameManager Instance { get; private set; }
    
    // 이벤트
    public System.Action<int> OnScoreChanged;
    public System.Action<GameState> OnGameStateChanged;
    public System.Action OnGameOver;
    
    void Awake()
    {
        // 싱글톤 설정
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }
        
        InitializeGame();
    }
    
    void Start()
    {
        LoadHighScore();
        UpdateUI();
        StartGame();
    }
    
    void Update()
    {
        UpdateGameTime();
        HandleInput();
    }
    
    /// <summary>
    /// 게임 초기화
    /// </summary>
    void InitializeGame()
    {
        currentScore = 0;
        gameTime = 0f;
        
        // UI 초기화
        if (gameOverPanel != null) gameOverPanel.SetActive(false);
        if (pausePanel != null) pausePanel.SetActive(false);
        
        Debug.Log("🎮 GameManager 초기화 완료!");
    }
    
    /// <summary>
    /// 게임 시작
    /// </summary>
    public void StartGame()
    {
        ChangeGameState(GameState.Playing);
        
        // CustomerSpawner 시작
        if (CustomerSpawner.Instance != null)
        {
            CustomerSpawner.Instance.StartSpawning();
        }
        
        Debug.Log("🎬 게임 시작!");
    }
    
    /// <summary>
    /// 게임 일시정지
    /// </summary>
    public void PauseGame()
    {
        if (currentState == GameState.Playing)
        {
            ChangeGameState(GameState.Paused);
            Time.timeScale = 0f;
            
            if (pausePanel != null) pausePanel.SetActive(true);
            
            Debug.Log("⏸️ 게임 일시정지");
        }
    }
    
    /// <summary>
    /// 게임 재개
    /// </summary>
    public void ResumeGame()
    {
        if (currentState == GameState.Paused)
        {
            ChangeGameState(GameState.Playing);
            Time.timeScale = 1f;
            
            if (pausePanel != null) pausePanel.SetActive(false);
            
            Debug.Log("▶️ 게임 재개");
        }
    }
    
    /// <summary>
    /// 게임 종료
    /// </summary>
    public void GameOver()
    {
        if (currentState == GameState.GameOver) return;
        
        ChangeGameState(GameState.GameOver);
        
        // CustomerSpawner 중지
        if (CustomerSpawner.Instance != null)
        {
            CustomerSpawner.Instance.StopSpawning();
        }
        
        // 최고 점수 업데이트
        if (currentScore > highScore)
        {
            highScore = currentScore;
            SaveHighScore();
            Debug.Log("🎉 신기록 달성!");
        }
        
        // 게임 오버 UI 표시
        if (gameOverPanel != null) gameOverPanel.SetActive(true);
        
        Debug.Log($"💀 게임 오버! 최종 점수: {currentScore}점");
        
        OnGameOver?.Invoke();
    }
    
    /// <summary>
    /// 게임 재시작
    /// </summary>
    public void RestartGame()
    {
        // 모든 손님 제거
        if (CustomerSpawner.Instance != null)
        {
            CustomerSpawner.Instance.ClearAllCustomers();
            CustomerSpawner.Instance.ResetStatistics();
        }
        
        // 게임 상태 초기화
        currentScore = 0;
        gameTime = 0f;
        Time.timeScale = 1f;
        
        // UI 초기화
        if (gameOverPanel != null) gameOverPanel.SetActive(false);
        if (pausePanel != null) pausePanel.SetActive(false);
        
        UpdateUI();
        StartGame();
        
        Debug.Log("🔄 게임 재시작!");
    }
    
    /// <summary>
    /// 게임 상태 변경
    /// </summary>
    void ChangeGameState(GameState newState)
    {
        GameState oldState = currentState;
        currentState = newState;
        
        Debug.Log($"🎮 게임 상태 변경: {oldState} → {newState}");
        
        OnGameStateChanged?.Invoke(newState);
    }
    
    /// <summary>
    /// 점수 추가
    /// </summary>
    public void AddScore(int points)
    {
        currentScore += points;
        
        if (points > 0)
        {
            Debug.Log($"💰 점수 획득: +{points} (총 {currentScore}점)");
        }
        else
        {
            Debug.Log($"💸 점수 감점: {points} (총 {currentScore}점)");
        }
        
        UpdateUI();
        OnScoreChanged?.Invoke(currentScore);
        
        // 목표 점수 달성 확인
        CheckWinCondition();
    }
    
    /// <summary>
    /// 승리 조건 확인
    /// </summary>
    void CheckWinCondition()
    {
        if (currentScore >= targetScore)
        {
            Debug.Log($"🏆 목표 점수 달성! ({targetScore}점)");
            // 여기에 승리 처리 로직 추가 가능
        }
    }
    
    /// <summary>
    /// 게임 시간 업데이트
    /// </summary>
    void UpdateGameTime()
    {
        if (currentState != GameState.Playing) return;
        
        gameTime += Time.deltaTime;
        
        // 시간 제한 확인
        if (hasTimeLimit && gameTime >= timeLimit)
        {
            Debug.Log("⏰ 시간 초과!");
            GameOver();
        }
        
        UpdateUI();
    }
    
    /// <summary>
    /// 입력 처리
    /// </summary>
    void HandleInput()
    {
        // ESC 키로 일시정지/재개
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (currentState == GameState.Playing)
            {
                PauseGame();
            }
            else if (currentState == GameState.Paused)
            {
                ResumeGame();
            }
        }
        
        // R 키로 재시작 (게임 오버 시)
        if (Input.GetKeyDown(KeyCode.R) && currentState == GameState.GameOver)
        {
            RestartGame();
        }
    }
    
    /// <summary>
    /// UI 업데이트
    /// </summary>
    void UpdateUI()
    {
        // 점수 업데이트
        if (scoreText != null)
        {
            scoreText.text = $"점수: {currentScore:N0}";
        }
        
        if (highScoreText != null)
        {
            highScoreText.text = $"최고점: {highScore:N0}";
        }
        
        // 시간 업데이트
        if (timeText != null)
        {
            if (hasTimeLimit)
            {
                float remainingTime = Mathf.Max(0, timeLimit - gameTime);
                int minutes = Mathf.FloorToInt(remainingTime / 60);
                int seconds = Mathf.FloorToInt(remainingTime % 60);
                timeText.text = $"시간: {minutes:00}:{seconds:00}";
                
                // 시간이 부족하면 빨간색으로
                if (remainingTime < 30f)
                {
                    timeText.color = Color.red;
                }
                else
                {
                    timeText.color = Color.white;
                }
            }
            else
            {
                int minutes = Mathf.FloorToInt(gameTime / 60);
                int seconds = Mathf.FloorToInt(gameTime % 60);
                timeText.text = $"시간: {minutes:00}:{seconds:00}";
            }
        }
        
        // 진행도 슬라이더 업데이트
        if (progressSlider != null)
        {
            if (hasTimeLimit)
            {
                progressSlider.value = gameTime / timeLimit;
            }
            else
            {
                progressSlider.value = Mathf.Clamp01((float)currentScore / targetScore);
            }
        }
        
        // 손님 통계 업데이트
        UpdateCustomerStatisticsUI();
    }
    
    /// <summary>
    /// 손님 통계 UI 업데이트
    /// </summary>
    void UpdateCustomerStatisticsUI()
    {
        if (CustomerSpawner.Instance != null)
        {
            var (total, satisfied, angry, satisfactionRate) = CustomerSpawner.Instance.GetStatistics();
            
            if (totalCustomersText != null)
            {
                totalCustomersText.text = $"총 손님: {total}명";
            }
            
            if (satisfiedCustomersText != null)
            {
                satisfiedCustomersText.text = $"만족: {satisfied}명 / 불만: {angry}명";
            }
            
            if (satisfactionRateText != null)
            {
                satisfactionRateText.text = $"만족도: {satisfactionRate:P1}";
                
                // 만족도에 따른 색상 변경
                if (satisfactionRate >= 0.8f)
                {
                    satisfactionRateText.color = Color.green;
                }
                else if (satisfactionRate >= 0.6f)
                {
                    satisfactionRateText.color = Color.yellow;
                }
                else
                {
                    satisfactionRateText.color = Color.red;
                }
            }
        }
    }
    
    /// <summary>
    /// 최고 점수 저장
    /// </summary>
    void SaveHighScore()
    {
        PlayerPrefs.SetInt("HighScore", highScore);
        PlayerPrefs.Save();
    }
    
    /// <summary>
    /// 최고 점수 로드
    /// </summary>
    void LoadHighScore()
    {
        highScore = PlayerPrefs.GetInt("HighScore", 0);
    }
    
    /// <summary>
    /// 현재 점수 반환
    /// </summary>
    public int GetScore()
    {
        return currentScore;
    }
    
    /// <summary>
    /// 현재 게임 상태 반환
    /// </summary>
    public GameState GetGameState()
    {
        return currentState;
    }
    
    /// <summary>
    /// 게임 설정
    /// </summary>
    public void SetGameSettings(float newTimeLimit, int newTargetScore, bool enableTimeLimit)
    {
        timeLimit = newTimeLimit;
        targetScore = newTargetScore;
        hasTimeLimit = enableTimeLimit;
        
        Debug.Log($"⚙️ 게임 설정 변경: 시간제한 {(hasTimeLimit ? timeLimit + "초" : "없음")}, 목표점수 {targetScore}점");
    }
}