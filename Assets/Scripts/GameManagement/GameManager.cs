// Assets/Scripts/GameManagement/GameManager.cs
// 🌅 PointManager 연동이 완료된 완전한 버전

using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using System.Collections;

public class GameManager : MonoBehaviour
{
    public enum GameState
    {
        Menu,           // 메인 메뉴
        Playing,        // 게임 중
        Paused,         // 일시정지
        DayEnded,       // 하루 종료
        GameOver        // 게임 오버
    }
    [Header("🏠 씬 관리")]
public bool returnToHomeOnGameOver = true;      // 게임 오버 시 홈으로 돌아가기
public bool returnToHomeOnDayEnd = true;        // 하루 종료 시 홈으로 돌아가기
public float returnToHomeDelay = 3f;            // 홈으로 돌아가는 지연 시간
public Button returnToHomeButton;               // 홈으로 돌아가기 버튼
public Button restartGameButton;                // 게임 재시작 버튼
    /// <summary>
    /// 🌅 하루 시간대 구분
    /// </summary>
    public enum TimeOfDay
    {
        Dawn,           // 새벽 (06:00-07:00) - 준비시간
        Morning,        // 아침 (07:00-11:00) - 가벼운 장사
        Lunch,          // 점심 (11:00-14:00) - 러시아워
        Afternoon,      // 오후 (14:00-17:00) - 한가한 시간
        Evening,        // 저녁 (17:00-20:00) - 러시아워
        Night,          // 밤 (20:00-21:00) - 마무리
        Closed          // 장사 종료 (21:00~)
    }
    
    [Header("게임 상태")]
    public GameState currentState = GameState.Playing;
    
    [Header("🌅 하루 시간 시스템")]
    public float dayDurationInRealSeconds = 300f;   // 5분 = 300초 = 게임 1일
    public TimeOfDay currentTimeOfDay = TimeOfDay.Dawn;
    public float gameStartHour = 6f;                // 게임 시작 시간 (오전 6시)
    public float gameEndHour = 21f;                 // 게임 종료 시간 (오후 9시)
    
    [Header("점수 시스템")]
    public int currentScore = 0;
    public int highScore = 0;
    public int targetScore = 1000;         // 목표 점수
    
    [Header("시간 관리 (기존)")]
    public float gameTime = 0f;            // 게임 경과 시간 (초)
    public float timeLimit = 300f;         // 시간 제한 (5분) - dayDurationInRealSeconds와 동일
    public bool hasTimeLimit = true;       // 시간 제한 여부 (하루 시스템에서는 항상 true)
    
    [Header("🌅 하루 목표 시스템")]
    public int dailyTargetScore = 1000;    // 일일 목표 점수
    public bool dayGoalAchieved = false;   // 일일 목표 달성 여부
    
    [Header("UI 연결")]
    public TextMeshProUGUI scoreText;      // 점수 텍스트
    public TextMeshProUGUI highScoreText;  // 최고 점수 텍스트
    public TextMeshProUGUI timeText;       // 시간 텍스트
    public Slider progressSlider;          // 진행도 슬라이더
    public GameObject gameOverPanel;       // 게임 오버 패널
    public GameObject pausePanel;          // 일시정지 패널
    
    [Header("🌅 하루 시간 UI")]
    public TextMeshProUGUI currentTimeText;        // 현재 시간 표시 (HH:MM)
    public TextMeshProUGUI timeOfDayText;          // 시간대 표시 (아침, 점심 등)
    public TextMeshProUGUI dailyTargetText;       // 일일 목표 표시
    public Slider dayProgressSlider;              // 하루 진행도 슬라이더
    public GameObject dayEndPanel;                // 하루 종료 패널
    
    [Header("💎 포인트 시스템 UI")]
    public TextMeshProUGUI currentPointsText;     // 현재 포인트 표시
    public TextMeshProUGUI todaysPointsText;      // 오늘 포인트 표시
    public TextMeshProUGUI streakStatusText;      // 연속 기록 상태 표시
    public TextMeshProUGUI pointStreakInfoText;   // 포인트 연속 정보
    
    [Header("손님 통계 UI")]
    public TextMeshProUGUI totalCustomersText;     // 총 손님 수
    public TextMeshProUGUI satisfiedCustomersText; // 만족한 손님 수
    public TextMeshProUGUI satisfactionRateText;   // 만족도 비율
    
    [Header("🔊 게임 사운드")]
    public AudioClip gameStartSound;              // 게임 시작 사운드
    public AudioClip dayEndSound;                 // 하루 종료 사운드
    public AudioClip gameOverSound;               // 게임 오버 사운드
    public AudioClip goalAchievedSound;           // 목표 달성 사운드
    public AudioClip timeWarningSound;            // 시간 경고 사운드
    
    [Header("🎉 목표 달성 효과")]
    public GameObject goalAchievedEffect;         // 목표 달성 효과
    public float goalEffectDuration = 3f;         // 효과 지속 시간
    
    [Header("⚙️ 게임 설정")]
    public bool autoSaveEnabled = true;           // 자동 저장 활성화
    public float autoSaveInterval = 30f;          // 자동 저장 간격 (초)
    public bool showDebugInfo = false;            // 디버그 정보 표시
    
    [Header("🐛 디버그")]
    public bool enableDebugLogs = true;           // 디버그 로그 활성화
    public bool enableTimeSkip = false;           // 시간 스킵 활성화 (디버그용)
    public KeyCode timeSkipKey = KeyCode.T;       // 시간 스킵 키
    
    // 싱글톤
    public static GameManager Instance { get; private set; }
    
    // 🌅 하루 시간 관련 내부 변수
    private TimeOfDay previousTimeOfDay = TimeOfDay.Dawn;
    private float autoSaveTimer = 0f;
    private bool isGameStarted = false;
    private AudioSource audioSource;
    
    // 이벤트
    public System.Action<int> OnScoreChanged;
    public System.Action<GameState> OnGameStateChanged;
    public System.Action OnGameOver;
    public System.Action<TimeOfDay> OnTimeOfDayChanged;    // 🌅 시간대 변경 이벤트
    public System.Action OnDayEnded;                       // 🌅 하루 종료 이벤트
    public System.Action OnDayStarted;                     // 🌅 하루 시작 이벤트
    public System.Action OnGoalAchieved;                   // 🌅 목표 달성 이벤트
    
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
        // 컴포넌트 초기화
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
            audioSource = gameObject.AddComponent<AudioSource>();
        
        LoadHighScore();
        
        // 💰 골드 시스템 확인
        if (GoldManager.Instance == null)
        {
            Debug.LogWarning("⚠️ GoldManager가 씬에 없습니다! 골드 기능을 사용하려면 GoldManager를 추가하세요.");
        }
        
        // 💎 포인트 시스템 확인
        if (PointManager.Instance == null)
        {
            Debug.LogWarning("⚠️ PointManager가 씬에 없습니다! 포인트 기능을 사용하려면 PointManager를 추가하세요.");
        }
        else
        {
            // PointManager 이벤트 연결
            SetupPointManagerEvents();
        }
        SetupReturnToHomeButton();

        UpdateUI();
        StartDay();
        
        if (enableDebugLogs)
        {
            Debug.Log("🎮 GameManager 시작 완료!");
        }
    }
    void SetupReturnToHomeButton()
    {
        if (returnToHomeButton != null)
        {
            returnToHomeButton.onClick.AddListener(ReturnToHome);

            // 초기에는 비활성화 (게임 오버나 하루 종료 시에만 활성화)
            returnToHomeButton.gameObject.SetActive(false);
        }

        if (restartGameButton != null)
        {
            restartGameButton.onClick.AddListener(RestartGame);
        }
    }

public void ReturnToHome()
{
    Debug.Log("🏠 홈 씬으로 돌아가기");
    
    // 💎 포인트 시스템 강제 저장
    if (PointManager.Instance != null)
    {
        PointManager.Instance.SaveData();
    }
    
    // 최고 기록 저장
    PlayerPrefs.SetInt("HighScore", highScore);
    PlayerPrefs.Save();
    
    // CustomerSpawner 정지
    if (CustomerSpawner.Instance != null)
    {
        CustomerSpawner.Instance.StopSpawning();
        CustomerSpawner.Instance.ClearAllCustomers();
    }
    
    // 씬 전환
    if (SceneTransitionManager.Instance != null)
    {
        SceneTransitionManager.Instance.LoadHomeScene();
    }
    else
    {
        Debug.LogError("SceneTransitionManager가 없습니다!");
        // fallback: 직접 씬 로드
        UnityEngine.SceneManagement.SceneManager.LoadScene("HomeScene");
    }
}

/// <summary>
/// 홈으로 돌아가기 버튼 표시
/// </summary>
void ShowReturnToHomeButton()
{
    if (returnToHomeButton != null)
    {
        returnToHomeButton.gameObject.SetActive(true);
    }
}

    /// <summary>
    /// 자동으로 홈으로 돌아가기 (지연 시간 후)
    /// </summary>
    IEnumerator AutoReturnToHome()
    {
        yield return new WaitForSeconds(returnToHomeDelay);

        if (enableDebugLogs)
        {
            Debug.Log($"🏠 {returnToHomeDelay}초 후 자동으로 홈으로 돌아갑니다...");
        }

        ReturnToHome();
    }


    void Update()
    {
        UpdateGameTime();
        HandleInput();
        UpdateAutoSave();
        
        if (showDebugInfo)
        {
            UpdateDebugInfo();
        }
    }
    
    /// <summary>
    /// 게임 초기화
    /// </summary>
    void InitializeGame()
    {
        currentScore = 0;
        gameTime = 0f;
        currentTimeOfDay = TimeOfDay.Dawn;
        previousTimeOfDay = TimeOfDay.Dawn;
        dayGoalAchieved = false;
        isGameStarted = false;
        autoSaveTimer = 0f;
        
        // 🌅 하루 시스템 설정
        timeLimit = dayDurationInRealSeconds;
        hasTimeLimit = true;
        
        // UI 초기화
        if (gameOverPanel != null) gameOverPanel.SetActive(false);
        if (pausePanel != null) pausePanel.SetActive(false);
        if (dayEndPanel != null) dayEndPanel.SetActive(false);
        
        if (enableDebugLogs)
        {
            Debug.Log("🎮 GameManager 초기화 완료! 하루 길이: " + dayDurationInRealSeconds + "초");
        }
    }
    
    /// <summary>
    /// PointManager 이벤트 연결
    /// </summary>
    void SetupPointManagerEvents()
    {
        if (PointManager.Instance != null)
        {
            PointManager.Instance.OnPointsChanged += OnPointsChanged;
            PointManager.Instance.OnPerfectPress += OnPerfectPress;
            PointManager.Instance.OnGoodPress += OnGoodPress;
            PointManager.Instance.OnCustomerSatisfaction += OnCustomerSatisfaction;
            PointManager.Instance.OnStreakBonus += OnStreakBonus;
            PointManager.Instance.OnStreakUpdate += OnStreakUpdate;
            
            if (enableDebugLogs)
            {
                Debug.Log("💎 PointManager 이벤트 연결 완료");
            }
        }
    }
    
    /// <summary>
    /// 🌅 하루 시작 - PointManager 연동
    /// </summary>
    public void StartDay()
    {
        ChangeGameState(GameState.Playing);
        
        // 시간 초기화
        gameTime = 0f;
        currentTimeOfDay = TimeOfDay.Dawn;
        previousTimeOfDay = TimeOfDay.Dawn;
        dayGoalAchieved = false;
        isGameStarted = true;
        
        // 💰 골드 시스템에 새로운 하루 시작 알림
        if (GoldManager.Instance != null)
        {
            if (enableDebugLogs)
            {
                Debug.Log("💰 골드 시스템과 연동된 새로운 하루 시작");
            }
        }
        
        // 💎 포인트 시스템에 새로운 하루 시작 알림
        if (PointManager.Instance != null)
        {
            PointManager.Instance.StartNewDay();
            if (enableDebugLogs)
            {
                Debug.Log("💎 포인트 시스템과 연동된 새로운 하루 시작");
            }
        }
        
        // CustomerSpawner 시작
        if (CustomerSpawner.Instance != null)
        {
            CustomerSpawner.Instance.StartSpawning();
        }
        
        // 사운드 재생
        PlaySound(gameStartSound);
        
        if (enableDebugLogs)
        {
            Debug.Log("🌅 새로운 하루 시작!");
        }
        
        OnDayStarted?.Invoke();
    }
    
    /// <summary>
    /// 🌅 하루 종료 - PointManager 연동
    /// </summary>
    public void EndDay()
{
    ChangeGameState(GameState.DayEnded);
    
    // 💎 포인트 시스템 하루 종료 처리
    if (PointManager.Instance != null)
    {
        PointManager.Instance.EndDay();
        if (enableDebugLogs)
        {
            Debug.Log($"💎 포인트 시스템 하루 종료 - 오늘: {PointManager.Instance.GetTodaysPoints()}점, 총합: {PointManager.Instance.GetCurrentPoints()}점");
        }
    }
    
    // 최고 기록 업데이트
    UpdateHighScore();
    
    // CustomerSpawner 정지
    if (CustomerSpawner.Instance != null)
    {
        CustomerSpawner.Instance.StopSpawning();
    }
    
    // 하루 종료 UI 표시
    if (dayEndPanel != null)
    {
        dayEndPanel.SetActive(true);
        UpdateDayEndUI();
    }
    
    // 🔧 홈으로 돌아가기 버튼 표시
    ShowReturnToHomeButton();
    
    // 사운드 재생
    PlaySound(dayEndSound);
    
    if (enableDebugLogs)
    {
        Debug.Log($"🌙 하루 종료! 최종 점수: {currentScore}점");
    }
    
    OnDayEnded?.Invoke();
    
    // 🔧 자동으로 홈으로 돌아가기 (설정된 경우)
    if (returnToHomeOnDayEnd)
    {
        StartCoroutine(AutoReturnToHome());
    }
}

    
    /// <summary>
    /// 최고 기록 업데이트
    /// </summary>
    void UpdateHighScore()
    {
        int totalScore = currentScore;
        
        // PointManager 점수 포함
        if (PointManager.Instance != null)
        {
            totalScore += PointManager.Instance.GetTodaysPoints();
        }
        
        if (totalScore > highScore)
        {
            highScore = totalScore;
            PlayerPrefs.SetInt("HighScore", highScore);
            
            if (enableDebugLogs)
            {
                Debug.Log($"🏆 신기록! {highScore}점");
            }
        }
    }
    
    /// <summary>
    /// 하루 종료 UI 업데이트
    /// </summary>
    void UpdateDayEndUI()
    {
        // 하루 종료 패널에 통계 정보 표시
        // 이 부분은 DayEndPanel UI 컴포넌트가 있다면 구현
        if (enableDebugLogs)
        {
            Debug.Log("📊 하루 종료 UI 업데이트");
        }
    }
    
    /// <summary>
    /// 게임 오버
    /// </summary>
    public void GameOver()
{
    ChangeGameState(GameState.GameOver);
    
    // 💎 포인트 시스템 강제 저장
    if (PointManager.Instance != null)
    {
        PointManager.Instance.SaveData();
    }
    
    // 최고 기록 업데이트
    UpdateHighScore();
    
    // CustomerSpawner 정지
    if (CustomerSpawner.Instance != null)
    {
        CustomerSpawner.Instance.StopSpawning();
        CustomerSpawner.Instance.ClearAllCustomers();
    }
    
    // 게임 오버 UI 표시
    if (gameOverPanel != null) 
    {
        gameOverPanel.SetActive(true);
    }
    
    // 🔧 홈으로 돌아가기 버튼 표시
    ShowReturnToHomeButton();
    
    // 사운드 재생
    PlaySound(gameOverSound);
    
    if (enableDebugLogs)
    {
        Debug.Log($"💀 게임 오버! 최종 점수: {currentScore}점");
    }
    
    OnGameOver?.Invoke();
    
    // 🔧 자동으로 홈으로 돌아가기 (설정된 경우)
    if (returnToHomeOnGameOver)
    {
        StartCoroutine(AutoReturnToHome());
    }
}
    /// <summary>
    /// 게임 재시작 - PointManager 연동
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
    currentTimeOfDay = TimeOfDay.Dawn;
    dayGoalAchieved = false;
    isGameStarted = false;
    Time.timeScale = 1f;
    
    // 💎 포인트 시스템 재시작
    if (PointManager.Instance != null)
    {
        PointManager.Instance.StartNewDay();
    }
    
    // UI 초기화
    if (gameOverPanel != null) gameOverPanel.SetActive(false);
    if (pausePanel != null) pausePanel.SetActive(false);
    if (dayEndPanel != null) dayEndPanel.SetActive(false);
    
    // 🔧 홈으로 돌아가기 버튼 숨기기
    if (returnToHomeButton != null)
    {
        returnToHomeButton.gameObject.SetActive(false);
    }
    
    UpdateUI();
    StartDay();
    
    if (enableDebugLogs)
    {
        Debug.Log("🔄 게임 재시작!");
    }
}
    /// <summary>
    /// 게임 상태 변경
    /// </summary>
    void ChangeGameState(GameState newState)
    {
        GameState oldState = currentState;
        currentState = newState;
        
        if (enableDebugLogs)
        {
            Debug.Log($"🎮 게임 상태 변경: {oldState} → {newState}");
        }
        
        OnGameStateChanged?.Invoke(newState);
    }
    
    /// <summary>
    /// 점수 추가
    /// </summary>
    public void AddScore(int points)
    {
        currentScore += points;
        
        if (enableDebugLogs)
        {
            if (points > 0)
            {
                Debug.Log($"💰 점수 획득: +{points} (총 {currentScore}점)");
            }
            else
            {
                Debug.Log($"💸 점수 감점: {points} (총 {currentScore}점)");
            }
        }
        
        UpdateUI();
        OnScoreChanged?.Invoke(currentScore);
        
        // 🌅 일일 목표 달성 확인
        CheckDailyGoal();
    }
    
    /// <summary>
    /// 🌅 일일 목표 달성 확인
    /// </summary>
    void CheckDailyGoal()
    {
        if (!dayGoalAchieved)
        {
            int totalScore = currentScore;
            
            // PointManager 점수 포함
            if (PointManager.Instance != null)
            {
                totalScore += PointManager.Instance.GetTodaysPoints();
            }
            
            if (totalScore >= dailyTargetScore)
            {
                dayGoalAchieved = true;
                
                // 목표 달성 효과
                ShowGoalAchievedEffect();
                PlaySound(goalAchievedSound);
                
                if (enableDebugLogs)
                {
                    Debug.Log($"🏆 일일 목표 달성! ({dailyTargetScore}점)");
                }
                
                OnGoalAchieved?.Invoke();
            }
        }
    }
    
    /// <summary>
    /// 목표 달성 효과 표시
    /// </summary>
    void ShowGoalAchievedEffect()
    {
        if (goalAchievedEffect != null)
        {
            GameObject effect = Instantiate(goalAchievedEffect, transform.position, Quaternion.identity);
            Destroy(effect, goalEffectDuration);
        }
    }
    
    /// <summary>
    /// 🌅 게임 시간 업데이트 (시간대 변경 포함)
    /// </summary>
    void UpdateGameTime()
    {
        if (currentState != GameState.Playing) return;
        
        gameTime += Time.deltaTime;
        
        // 🌅 시간대 업데이트
        TimeOfDay newTimeOfDay = CalculateTimeOfDay();
        if (newTimeOfDay != currentTimeOfDay)
        {
            previousTimeOfDay = currentTimeOfDay;
            currentTimeOfDay = newTimeOfDay;
            
            if (enableDebugLogs)
            {
                Debug.Log($"🌅 시간대 변경: {GetTimeOfDayKoreanName(previousTimeOfDay)} → {GetTimeOfDayKoreanName(currentTimeOfDay)}");
            }
            
            OnTimeOfDayChanged?.Invoke(currentTimeOfDay);
        }
        
        // 시간 경고 사운드 (마지막 1분)
        if (gameTime >= dayDurationInRealSeconds - 60f && gameTime <= dayDurationInRealSeconds - 59f)
        {
            PlaySound(timeWarningSound);
        }
        
        // 🌅 하루 종료 확인
        if (gameTime >= dayDurationInRealSeconds || currentTimeOfDay == TimeOfDay.Closed)
        {
            if (enableDebugLogs)
            {
                Debug.Log("⏰ 하루 시간 종료!");
            }
            EndDay();
        }
    }
    
    /// <summary>
    /// 시간대 계산
    /// </summary>
    TimeOfDay CalculateTimeOfDay()
    {
        float gameHours = gameStartHour + (gameTime / dayDurationInRealSeconds) * (gameEndHour - gameStartHour);
        
        if (gameHours < 7f) return TimeOfDay.Dawn;
        else if (gameHours < 11f) return TimeOfDay.Morning;
        else if (gameHours < 14f) return TimeOfDay.Lunch;
        else if (gameHours < 17f) return TimeOfDay.Afternoon;
        else if (gameHours < 20f) return TimeOfDay.Evening;
        else if (gameHours < 21f) return TimeOfDay.Night;
        else return TimeOfDay.Closed;
    }
    
    /// <summary>
    /// 시간대 한국어 이름 반환
    /// </summary>
    string GetTimeOfDayKoreanName(TimeOfDay timeOfDay)
    {
        switch (timeOfDay)
        {
            case TimeOfDay.Dawn: return "새벽";
            case TimeOfDay.Morning: return "아침";
            case TimeOfDay.Lunch: return "점심";
            case TimeOfDay.Afternoon: return "오후";
            case TimeOfDay.Evening: return "저녁";
            case TimeOfDay.Night: return "밤";
            case TimeOfDay.Closed: return "마감";
            default: return "알 수 없음";
        }
    }
    
    /// <summary>
    /// 현재 게임 시간을 HH:MM 형태로 반환
    /// </summary>
    string GetCurrentTimeString()
    {
        float gameHours = gameStartHour + (gameTime / dayDurationInRealSeconds) * (gameEndHour - gameStartHour);
        int hours = Mathf.FloorToInt(gameHours);
        int minutes = Mathf.FloorToInt((gameHours - hours) * 60f);
        
        return $"{hours:D2}:{minutes:D2}";
    }
    
    /// <summary>
    /// 입력 처리
    /// </summary>
    void HandleInput()
    {
        // 일시정지/재개
        if (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Escape))
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
        
        // 🔧 ESC 키 두 번으로 홈으로 돌아가기 (일시정지 상태에서)
        if (Input.GetKeyDown(KeyCode.Escape) && currentState == GameState.Paused)
        {
            // 간단히 즉시 홈으로 이동
            ReturnToHome();
        }
        
        // R 키로 재시작 (게임 오버 또는 하루 종료 시)
        if (Input.GetKeyDown(KeyCode.R) && (currentState == GameState.GameOver || currentState == GameState.DayEnded))
        {
            RestartGame();
        }
        
        // 🔧 H 키로 홈으로 돌아가기 (게임 오버나 하루 종료 시)
        if (Input.GetKeyDown(KeyCode.H) && (currentState == GameState.GameOver || currentState == GameState.DayEnded))
        {
            ReturnToHome();
        }
        
        // 디버그 시간 스킵
        if (enableTimeSkip && Input.GetKeyDown(timeSkipKey))
        {
            gameTime += 60f; // 1분 스킵
            if (enableDebugLogs)
            {
                Debug.Log("⏩ 시간 1분 스킵!");
            }
        }
        
        // 디버그 정보 토글
        if (Input.GetKeyDown(KeyCode.F1))
        {
            showDebugInfo = !showDebugInfo;
        }
        
        // PointManager 디버그 정보
        if (Input.GetKeyDown(KeyCode.F2) && PointManager.Instance != null)
        {
            PointManager.Instance.PrintDebugInfo();
        }
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
            
            if (pausePanel != null)
                pausePanel.SetActive(true);
                
            if (enableDebugLogs)
            {
                Debug.Log("⏸️ 게임 일시정지");
            }
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
            
            if (pausePanel != null)
                pausePanel.SetActive(false);
                
            if (enableDebugLogs)
            {
                Debug.Log("▶️ 게임 재개");
            }
        }
    }
    
    /// <summary>
    /// 자동 저장 업데이트
    /// </summary>
    void UpdateAutoSave()
    {
        if (!autoSaveEnabled) return;
        
        autoSaveTimer += Time.deltaTime;
        
        if (autoSaveTimer >= autoSaveInterval)
        {
            autoSaveTimer = 0f;
            AutoSave();
        }
    }
    
    /// <summary>
    /// 자동 저장
    /// </summary>
    void AutoSave()
    {
        // 포인트 데이터 저장
        if (PointManager.Instance != null)
        {
            PointManager.Instance.SaveData();
        }
        
        // 최고 점수 저장
        PlayerPrefs.SetInt("HighScore", highScore);
        PlayerPrefs.Save();
        
        if (enableDebugLogs)
        {
            Debug.Log("💾 자동 저장 완료");
        }
    }
    
    /// <summary>
    /// 디버그 정보 업데이트
    /// </summary>
    void UpdateDebugInfo()
    {
        // 화면에 디버그 정보 표시 (OnGUI 사용 또는 UI 텍스트 업데이트)
        // 이 부분은 필요에 따라 구현
    }
    
    /// <summary>
    /// UI 업데이트
    /// </summary>
    void UpdateUI()
    {
        // 기본 점수 UI
        if (scoreText != null)
            scoreText.text = $"점수: {currentScore:N0}";
            
        if (highScoreText != null)
            highScoreText.text = $"최고: {highScore:N0}";
        
        // 시간 UI
        if (timeText != null)
        {
            if (hasTimeLimit)
            {
                float remainingTime = Mathf.Max(0, timeLimit - gameTime);
                int minutes = Mathf.FloorToInt(remainingTime / 60f);
                int seconds = Mathf.FloorToInt(remainingTime % 60f);
                timeText.text = $"{minutes:D2}:{seconds:D2}";
            }
            else
            {
                int minutes = Mathf.FloorToInt(gameTime / 60f);
                int seconds = Mathf.FloorToInt(gameTime % 60f);
                timeText.text = $"{minutes:D2}:{seconds:D2}";
            }
        }
        
        // 진행도 슬라이더
        if (progressSlider != null)
        {
            if (hasTimeLimit)
                progressSlider.value = gameTime / timeLimit;
            else
                progressSlider.value = 0f;
        }
        
        // 🌅 하루 시간 UI
        if (currentTimeText != null)
            currentTimeText.text = GetCurrentTimeString();
            
        if (timeOfDayText != null)
            timeOfDayText.text = GetTimeOfDayKoreanName(currentTimeOfDay);
            
        if (dailyTargetText != null)
        {
            int totalScore = currentScore;
            if (PointManager.Instance != null)
                totalScore += PointManager.Instance.GetTodaysPoints();
                
            dailyTargetText.text = $"목표: {totalScore:N0}/{dailyTargetScore:N0}";
            
            if (dayGoalAchieved)
                dailyTargetText.color = Color.green;
            else
                dailyTargetText.color = Color.white;
        }
        
        if (dayProgressSlider != null)
            dayProgressSlider.value = gameTime / dayDurationInRealSeconds;
        
        // 💎 포인트 시스템 UI
        UpdatePointUI();
        
        // 손님 통계 UI
        UpdateCustomerStatsUI();
    }
    
    /// <summary>
    /// 포인트 UI 업데이트
    /// </summary>
    void UpdatePointUI()
    {
        if (PointManager.Instance == null) return;
        
        if (currentPointsText != null)
            currentPointsText.text = $"총 포인트: {PointManager.Instance.GetCurrentPoints():N0}";
            
        if (todaysPointsText != null)
            todaysPointsText.text = $"오늘: {PointManager.Instance.GetTodaysPoints():N0}";
            
        if (streakStatusText != null)
            streakStatusText.text = PointManager.Instance.GetStreakStatus();
            
        if (pointStreakInfoText != null)
        {
            string streakInfo = "";
            int perfectStreak = PointManager.Instance.GetPerfectStreak();
            int satisfactionStreak = PointManager.Instance.GetSatisfactionStreak();
            
            if (perfectStreak > 1 || satisfactionStreak > 1)
            {
                streakInfo = "보너스 활성: ";
                if (perfectStreak > 1)
                    streakInfo += $"Perfect +{(perfectStreak - 1) * 10}%";
                if (satisfactionStreak > 1)
                {
                    if (perfectStreak > 1) streakInfo += ", ";
                    streakInfo += $"만족 +{(satisfactionStreak - 1) * 10}%";
                }
            }
            else
            {
                streakInfo = "연속 보너스 없음";
            }
            
            pointStreakInfoText.text = streakInfo;
        }
    }
    
    /// <summary>
    /// 손님 통계 UI 업데이트
    /// </summary>
    void UpdateCustomerStatsUI()
    {
        if (CustomerSpawner.Instance == null) return;
        
        if (totalCustomersText != null)
            totalCustomersText.text = $"총 손님: {CustomerSpawner.Instance.GetTotalCustomersServed()}명";
            
        if (satisfiedCustomersText != null)
            satisfiedCustomersText.text = $"만족: {CustomerSpawner.Instance.GetSatisfiedCustomers()}명";
            
        if (satisfactionRateText != null)
        {
            float rate = CustomerSpawner.Instance.GetCustomerSatisfactionRate();
            satisfactionRateText.text = $"만족도: {rate:P0}";
            
            if (rate >= 0.8f)
                satisfactionRateText.color = Color.green;
            else if (rate >= 0.6f)
                satisfactionRateText.color = Color.yellow;
            else
                satisfactionRateText.color = Color.red;
        }
    }
    
    /// <summary>
    /// 사운드 재생
    /// </summary>
    void PlaySound(AudioClip clip)
    {
        if (audioSource != null && clip != null)
        {
            audioSource.PlayOneShot(clip);
        }
    }
    
    /// <summary>
    /// 최고 점수 로드
    /// </summary>
    void LoadHighScore()
    {
        highScore = PlayerPrefs.GetInt("HighScore", 0);
    }
    
    // ===== PointManager 이벤트 핸들러들 =====
    
    void OnPointsChanged(int points)
    {
        UpdateUI(); // 포인트 변경 시 UI 업데이트
    }
    
    void OnPerfectPress(int points)
    {
        // Perfect 누르기 특별 효과 (필요시)
        if (enableDebugLogs)
        {
            Debug.Log($"🔥 Perfect 처리 완료: +{points}점");
        }
    }
    
    void OnGoodPress(int points)
    {
        // Good 누르기 효과 (필요시)
        if (enableDebugLogs)
        {
            Debug.Log($"👍 Good 처리 완료: +{points}점");
        }
    }
    
    void OnCustomerSatisfaction(int points)
    {
        // 손님 만족 특별 효과 (필요시)
        if (enableDebugLogs)
        {
            Debug.Log($"😊 손님 만족 처리 완료: +{points}점");
        }
    }
    
    void OnStreakBonus(int streakCount)
    {
        // 연속 보너스 특별 효과
        if (enableDebugLogs)
        {
            Debug.Log($"🔥 연속 보너스 발생! 연속 {streakCount}회");
        }
    }
    
    void OnStreakUpdate(string streakStatus)
    {
        // 연속 상태 업데이트
        UpdateUI(); // UI 즉시 업데이트
    }
    
    // ===== 🌅 공개 접근자 메서드들 =====
    
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
    /// 🌅 현재 시간대 반환
    /// </summary>
    public TimeOfDay GetCurrentTimeOfDay()
    {
        return currentTimeOfDay;
    }
    
    /// <summary>
    /// 🌅 하루 진행도 반환 (0.0 ~ 1.0)
    /// </summary>
    public float GetDayProgress()
    {
        return Mathf.Clamp01(gameTime / dayDurationInRealSeconds);
    }
    
    /// <summary>
    /// 🌅 일일 목표 달성 여부 반환
    /// </summary>
    public bool IsDayGoalAchieved()
    {
        return dayGoalAchieved;
    }
    
    /// <summary>
    /// 현재 게임 시간 반환 (초)
    /// </summary>
    public float GetGameTime()
    {
        return gameTime;
    }
    
    /// <summary>
    /// 게임 시작 여부 반환
    /// </summary>
    public bool IsGameStarted()
    {
        return isGameStarted;
    }
    
    /// <summary>
    /// 게임 설정 (확장된 버전)
    /// </summary>
    public void SetGameSettings(float newDayDuration, int newDailyTarget)
    {
        dayDurationInRealSeconds = newDayDuration;
        timeLimit = newDayDuration;
        dailyTargetScore = newDailyTarget;
        hasTimeLimit = true;
        
        if (enableDebugLogs)
        {
            Debug.Log($"⚙️ 게임 설정 변경: 하루길이 {dayDurationInRealSeconds}초, 일일목표 {dailyTargetScore}점");
        }
    }
    
    void OnDestroy()
    {
        // 이벤트 연결 해제
        if (PointManager.Instance != null)
        {
            PointManager.Instance.OnPointsChanged -= OnPointsChanged;
            PointManager.Instance.OnPerfectPress -= OnPerfectPress;
            PointManager.Instance.OnGoodPress -= OnGoodPress;
            PointManager.Instance.OnCustomerSatisfaction -= OnCustomerSatisfaction;
            PointManager.Instance.OnStreakBonus -= OnStreakBonus;
            PointManager.Instance.OnStreakUpdate -= OnStreakUpdate;
        }
        
        // 자동 저장
        if (autoSaveEnabled)
        {
            AutoSave();
        }
    }
}