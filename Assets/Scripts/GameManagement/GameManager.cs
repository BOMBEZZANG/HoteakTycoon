// Assets/Scripts/GameManager.cs
// 🌅 하루 시간 시스템이 추가된 게임 매니저

using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

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
    
    [Header("손님 통계 UI")]
    public TextMeshProUGUI totalCustomersText;     // 총 손님 수
    public TextMeshProUGUI satisfiedCustomersText; // 만족한 손님 수
    public TextMeshProUGUI satisfactionRateText;   // 만족도 비율
    
    // 싱글톤
    public static GameManager Instance { get; private set; }
    
    // 🌅 하루 시간 관련 내부 변수
    private TimeOfDay previousTimeOfDay = TimeOfDay.Dawn;
    
    // 이벤트
    public System.Action<int> OnScoreChanged;
    public System.Action<GameState> OnGameStateChanged;
    public System.Action OnGameOver;
    public System.Action<TimeOfDay> OnTimeOfDayChanged;    // 🌅 시간대 변경 이벤트
    public System.Action OnDayEnded;                       // 🌅 하루 종료 이벤트
    
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
    
    // 💰 골드 시스템 확인 (새로 추가)
    if (GoldManager.Instance == null)
    {
        Debug.LogWarning("⚠️ GoldManager가 씬에 없습니다! 골드 기능을 사용하려면 GoldManager를 추가하세요.");
    }
    
    UpdateUI();
    StartDay();
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
        currentTimeOfDay = TimeOfDay.Dawn;
        previousTimeOfDay = TimeOfDay.Dawn;
        dayGoalAchieved = false;
        
        // 🌅 하루 시스템 설정
        timeLimit = dayDurationInRealSeconds;
        hasTimeLimit = true;
        
        // UI 초기화
        if (gameOverPanel != null) gameOverPanel.SetActive(false);
        if (pausePanel != null) pausePanel.SetActive(false);
        if (dayEndPanel != null) dayEndPanel.SetActive(false);
        
        Debug.Log("🎮 GameManager 초기화 완료! 하루 길이: " + dayDurationInRealSeconds + "초");
    }
    
    /// <summary>
    /// 🌅 하루 시작
    /// </summary>
public void StartDay()
{
    ChangeGameState(GameState.Playing);
    
    // 시간 초기화
    gameTime = 0f;
    currentTimeOfDay = TimeOfDay.Dawn;
    previousTimeOfDay = TimeOfDay.Dawn;
    dayGoalAchieved = false;
    
    // 💰 골드 시스템에 새로운 하루 시작 알림 (새로 추가)
    if (GoldManager.Instance != null)
    {
        // GoldManager 내부에서 이미 새로운 하루 시작 처리를 하므로 별도 호출 불필요
        Debug.Log("💰 골드 시스템과 연동된 새로운 하루 시작");
    }
    
    // CustomerSpawner 시작
    if (CustomerSpawner.Instance != null)
    {
        CustomerSpawner.Instance.StartSpawning();
    }
    
    Debug.Log("🌅 새로운 하루 시작! 목표: " + dailyTargetScore + "점");
    
    // 시간대 변경 이벤트 발생
    OnTimeOfDayChanged?.Invoke(currentTimeOfDay);
}

    
    /// <summary>
    /// 🌅 하루 종료
    /// </summary>
    public void EndDay()
{
    if (currentState == GameState.DayEnded) return;
    
    ChangeGameState(GameState.DayEnded);
    
    // CustomerSpawner 중지
    if (CustomerSpawner.Instance != null)
    {
        CustomerSpawner.Instance.StopSpawning();
    }
    
    // 목표 달성 확인
    dayGoalAchieved = currentScore >= dailyTargetScore;
    
    // 최고 점수 업데이트
    if (currentScore > highScore)
    {
        highScore = currentScore;
        SaveHighScore();
        Debug.Log("🎉 신기록 달성!");
    }
    
    // 💰 골드 정보 출력 (새로 추가)
    if (GoldManager.Instance != null)
    {
        int todaysEarnings = GoldManager.Instance.GetTodaysEarnings();
        int totalGold = GoldManager.Instance.GetCurrentGold();
        
        Debug.Log($"💰 하루 골드 수익: {todaysEarnings:N0}원");
        Debug.Log($"💰 총 보유 골드: {totalGold:N0}원");
    }
    
    // 하루 종료 UI 표시
    if (dayEndPanel != null) 
    {
        dayEndPanel.SetActive(true);
    }
    
    Debug.Log($"🌙 하루 종료! 최종 점수: {currentScore}점 (목표: {dailyTargetScore}점) - {(dayGoalAchieved ? "성공" : "실패")}");
    
    OnDayEnded?.Invoke();
    
    // 💰 골드 시스템의 OnDayEnded 이벤트가 자동으로 호출되어 골드 누적 처리됨
}
    
    /// <summary>
    /// 🌅 현재 게임 시간을 실제 시간으로 변환 (6시~21시)
    /// </summary>
    public float GetCurrentGameHour()
    {
        float dayProgress = gameTime / dayDurationInRealSeconds;
        float totalGameHours = gameEndHour - gameStartHour; // 15시간 (6시~21시)
        return gameStartHour + (dayProgress * totalGameHours);
    }
    
    /// <summary>
    /// 🌅 현재 시간대 계산
    /// </summary>
    TimeOfDay CalculateTimeOfDay()
    {
        float currentHour = GetCurrentGameHour();
        
        if (currentHour < 7f)           return TimeOfDay.Dawn;        // 06:00-07:00
        else if (currentHour < 11f)     return TimeOfDay.Morning;     // 07:00-11:00
        else if (currentHour < 14f)     return TimeOfDay.Lunch;       // 11:00-14:00
        else if (currentHour < 17f)     return TimeOfDay.Afternoon;   // 14:00-17:00
        else if (currentHour < 20f)     return TimeOfDay.Evening;     // 17:00-20:00
        else if (currentHour < 21f)     return TimeOfDay.Night;       // 20:00-21:00
        else                            return TimeOfDay.Closed;      // 21:00~
    }
    
    /// <summary>
    /// 🌅 시간대별 한국어 이름 반환
    /// </summary>
    public string GetTimeOfDayKoreanName(TimeOfDay timeOfDay)
    {
        switch (timeOfDay)
        {
            case TimeOfDay.Dawn:        return "새벽";
            case TimeOfDay.Morning:     return "아침";
            case TimeOfDay.Lunch:       return "점심";
            case TimeOfDay.Afternoon:   return "오후";
            case TimeOfDay.Evening:     return "저녁";
            case TimeOfDay.Night:       return "밤";
            case TimeOfDay.Closed:      return "마감";
            default:                    return "알수없음";
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
    /// 게임 종료 (기존 시스템 유지)
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
        currentTimeOfDay = TimeOfDay.Dawn;
        dayGoalAchieved = false;
        Time.timeScale = 1f;
        
        // UI 초기화
        if (gameOverPanel != null) gameOverPanel.SetActive(false);
        if (pausePanel != null) pausePanel.SetActive(false);
        if (dayEndPanel != null) dayEndPanel.SetActive(false);
        
        UpdateUI();
        StartDay();
        
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
        
        // 🌅 일일 목표 달성 확인
        CheckDailyGoal();
    }
    
    /// <summary>
    /// 🌅 일일 목표 달성 확인
    /// </summary>
    void CheckDailyGoal()
    {
        if (!dayGoalAchieved && currentScore >= dailyTargetScore)
        {
            dayGoalAchieved = true;
            Debug.Log($"🏆 일일 목표 달성! ({dailyTargetScore}점)");
            // TODO: 목표 달성 축하 효과 추가 (다음 단계)
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
            
            Debug.Log($"🌅 시간대 변경: {GetTimeOfDayKoreanName(previousTimeOfDay)} → {GetTimeOfDayKoreanName(currentTimeOfDay)}");
            
            OnTimeOfDayChanged?.Invoke(currentTimeOfDay);
        }
        
        // 🌅 하루 종료 확인
        if (gameTime >= dayDurationInRealSeconds || currentTimeOfDay == TimeOfDay.Closed)
        {
            Debug.Log("⏰ 하루 시간 종료!");
            EndDay();
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
        
        // R 키로 재시작 (게임 오버 또는 하루 종료 시)
        if (Input.GetKeyDown(KeyCode.R) && (currentState == GameState.GameOver || currentState == GameState.DayEnded))
        {
            RestartGame();
        }
    }
    
    /// <summary>
    /// 🌅 UI 업데이트 (하루 시간 시스템 포함)
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
        
        // 🌅 하루 시간 UI 업데이트
        UpdateDayTimeUI();
        
        // 기존 시간 업데이트 (호환성 유지)
        if (timeText != null)
        {
            float remainingTime = Mathf.Max(0, dayDurationInRealSeconds - gameTime);
            int minutes = Mathf.FloorToInt(remainingTime / 60);
            int seconds = Mathf.FloorToInt(remainingTime % 60);
            timeText.text = $"남은시간: {minutes:00}:{seconds:00}";
            
            // 🌅 시간이 부족하면 빨간색으로
            if (remainingTime < 60f)
            {
                timeText.color = Color.red;
            }
            else
            {
                timeText.color = Color.white;
            }
        }
        
        // 진행도 슬라이더 업데이트
        if (progressSlider != null)
        {
            progressSlider.value = gameTime / dayDurationInRealSeconds;
        }
        
        // 손님 통계 업데이트
        UpdateCustomerStatisticsUI();
    }
    
    /// <summary>
    /// 🌅 하루 시간 관련 UI 업데이트
    /// </summary>
    void UpdateDayTimeUI()
    {
        // 현재 시간 표시 (HH:MM 형식)
        if (currentTimeText != null)
        {
            float currentHour = GetCurrentGameHour();
            int hour = Mathf.FloorToInt(currentHour);
            int minute = Mathf.FloorToInt((currentHour - hour) * 60);
            
            currentTimeText.text = $"{hour:00}:{minute:00}";
        }
        
        // 시간대 표시
        if (timeOfDayText != null)
        {
            string timeOfDayName = GetTimeOfDayKoreanName(currentTimeOfDay);
            timeOfDayText.text = timeOfDayName;
            
            // 🌅 시간대별 색상 설정
            Color timeColor = GetTimeOfDayColor(currentTimeOfDay);
            timeOfDayText.color = timeColor;
        }
        
        // 일일 목표 표시
        if (dailyTargetText != null)
        {
            string goalStatus = dayGoalAchieved ? "달성!" : $"{dailyTargetScore}점";
            dailyTargetText.text = $"목표: {goalStatus}";
            dailyTargetText.color = dayGoalAchieved ? Color.green : Color.white;
        }
        
        // 하루 진행도 슬라이더
        if (dayProgressSlider != null)
        {
            dayProgressSlider.value = gameTime / dayDurationInRealSeconds;
        }
    }
    
    /// <summary>
    /// 🌅 시간대별 색상 반환
    /// </summary>
    Color GetTimeOfDayColor(TimeOfDay timeOfDay)
    {
        switch (timeOfDay)
        {
            case TimeOfDay.Dawn:        return new Color(0.4f, 0.4f, 0.8f, 1f);    // 어두운 파랑
            case TimeOfDay.Morning:     return new Color(1f, 0.8f, 0.4f, 1f);      // 주황
            case TimeOfDay.Lunch:       return new Color(1f, 1f, 0.2f, 1f);        // 밝은 노랑
            case TimeOfDay.Afternoon:   return new Color(0.2f, 0.8f, 1f, 1f);      // 하늘색
            case TimeOfDay.Evening:     return new Color(1f, 0.4f, 0.2f, 1f);      // 주황-빨강
            case TimeOfDay.Night:       return new Color(0.3f, 0.2f, 0.6f, 1f);    // 보라
            case TimeOfDay.Closed:      return Color.gray;                         // 회색
            default:                    return Color.white;
        }
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
    /// 게임 설정 (확장된 버전)
    /// </summary>
    public void SetGameSettings(float newDayDuration, int newDailyTarget)
    {
        dayDurationInRealSeconds = newDayDuration;
        timeLimit = newDayDuration;
        dailyTargetScore = newDailyTarget;
        hasTimeLimit = true;
        
        Debug.Log($"⚙️ 게임 설정 변경: 하루길이 {dayDurationInRealSeconds}초, 일일목표 {dailyTargetScore}점");
    }
}