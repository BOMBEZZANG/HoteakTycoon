// Assets/Scripts/Point/PointManager.cs
// 💎 포인트 시스템 중앙 관리 매니저 - 완전히 새로운 전체 코드

using UnityEngine;
using System;
using System.Collections;

public class PointManager : MonoBehaviour
{
    [Header("💎 포인트 기본 설정")]
    public int goodPressPoints = 20;                // Good 누르기 기본 점수
    public int perfectPressPoints = 50;             // Perfect 누르기 기본 점수
    public int customerSatisfactionPoints = 100;    // 손님 만족 기본 점수
    
    [Header("🔥 연속 보너스 설정")]
    public float consecutiveBonus = 10f;            // 연속 보너스 비율 (10%)
    public bool enableConsecutiveBonus = true;      // 연속 보너스 활성화
    public int maxConsecutiveBonus = 500;           // 최대 연속 보너스 (500%)
    
    [Header("💎 포인트 데이터")]
    public PointData pointData;                     // 포인트 데이터
    
    [Header("🔊 사운드 효과")]
    public AudioClip perfectSound;                  // Perfect 사운드
    public AudioClip goodSound;                     // Good 사운드
    public AudioClip streakSound;                   // 연속 보너스 사운드
    public AudioClip satisfactionSound;             // 손님 만족 사운드
    public AudioClip goalAchievedSound;             // 목표 달성 사운드
    public AudioClip levelUpSound;                  // 레벨업 사운드
    
    [Header("🎉 시각적 효과")]
    public GameObject perfectEffect;                // Perfect 효과
    public GameObject streakEffect;                 // 연속 효과
    public GameObject satisfactionEffect;           // 만족 효과
    public GameObject goalAchievedEffect;           // 목표 달성 효과
    public GameObject levelUpEffect;                // 레벨업 효과
    
    [Header("📱 UI 연결")]
    public GameObject pointPopupPrefab;             // 포인트 팝업 프리팹
    public Transform uiCanvas;                      // UI 캔버스
    public UnityEngine.UI.Text currentPointsText;  // 현재 포인트 텍스트
    public UnityEngine.UI.Text todaysPointsText;   // 오늘 포인트 텍스트
    public UnityEngine.UI.Text streakStatusText;   // 연속 상태 텍스트
    
    [Header("⚙️ 시스템 설정")]
    public bool autoSaveEnabled = true;             // 자동 저장 활성화
    public float autoSaveInterval = 30f;            // 자동 저장 간격 (초)
    public bool showDetailedLogs = false;           // 상세 로그 표시
    public bool enableParticleEffects = true;       // 파티클 효과 활성화
    public bool enableScreenShake = true;           // 화면 흔들림 효과
    
    [Header("🏆 목표 및 레벨 시스템")]
    public int[] levelThresholds = { 100, 300, 600, 1000, 1500, 2500, 4000, 6000, 9000, 15000 };
    public int currentLevel = 1;
    public bool enableLevelSystem = true;
    
    [Header("🎮 게임플레이 개선")]
    public float perfectTimeWindow = 0.2f;          // Perfect 타이밍 윈도우
    public float comboCooldownTime = 3f;            // 콤보 쿨다운 시간
    public bool enableComboCooldown = true;         // 콤보 쿨다운 활성화
    
    [Header("🐛 디버그")]
    public bool enableDebugLogs = true;             // 디버그 로그 활성화
    public bool showStreakInfo = true;              // 연속 정보 표시
    public bool enableTestMode = false;             // 테스트 모드
    public KeyCode testPerfectKey = KeyCode.P;      // 테스트 Perfect 키
    public KeyCode testGoodKey = KeyCode.G;         // 테스트 Good 키
    public KeyCode testSatisfactionKey = KeyCode.S; // 테스트 만족 키
    
    // 싱글톤 인스턴스
    public static PointManager Instance { get; private set; }
    
    // 이벤트들
    public System.Action<int> OnPointsChanged;          // 포인트 변경 이벤트
    public System.Action<int> OnPerfectPress;           // Perfect 누르기 이벤트
    public System.Action<int> OnGoodPress;              // Good 누르기 이벤트
    public System.Action<int> OnCustomerSatisfaction;   // 손님 만족 이벤트
    public System.Action<int> OnStreakBonus;            // 연속 보너스 이벤트
    public System.Action<string> OnStreakUpdate;        // 연속 기록 업데이트 이벤트
    public System.Action<int> OnLevelUp;                // 레벨업 이벤트
    public System.Action<int> OnGoalAchieved;           // 목표 달성 이벤트
    
    // 내부 변수들
    private AudioSource audioSource;
    private Camera mainCamera;
    private float autoSaveTimer = 0f;
    private float lastActionTime = 0f;
    private bool isInitialized = false;
    
    // 콤보 시스템
    private float lastPerfectTime = 0f;
    private int currentComboCount = 0;
    private bool isInCombo = false;
    
    void Awake()
    {
        // 싱글톤 설정
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            
            // PointData 초기화
            if (pointData == null)
                pointData = new PointData();
                
            InitializePointSystem();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    [Obsolete]
    void Start()
    {
        // 컴포넌트 초기화
        SetupComponents();
        
        // PlayerPrefs에서 데이터 로드
        pointData.LoadFromPlayerPrefs();
        
        // 레벨 계산
        CalculateCurrentLevel();
        
        // UI 업데이트
        UpdateAllUI();
        
        isInitialized = true;
        
        if (enableDebugLogs)
        {
            Debug.Log($"💎 PointManager 초기화 완료! 현재 포인트: {pointData.currentPoints}, 레벨: {currentLevel}");
            
            if (showDetailedLogs)
            {
                PrintDetailedStatus();
            }
        }
    }
    
    void Update()
    {
        if (!isInitialized) return;
        
        // 자동 저장 처리
        HandleAutoSave();
        
        // 테스트 모드
        if (enableTestMode)
        {
            HandleTestInput();
        }
        
        // 콤보 쿨다운 처리
        if (enableComboCooldown)
        {
            HandleComboCooldown();
        }
    }
    
    /// <summary>
    /// 포인트 시스템 초기화
    /// </summary>
    void InitializePointSystem()
    {
        // 하루 시작 시 데이터 초기화
        pointData.StartNewDay();
        
        // 내부 변수 초기화
        autoSaveTimer = 0f;
        lastActionTime = Time.time;
        currentComboCount = 0;
        isInCombo = false;
        
        if (enableDebugLogs)
        {
            Debug.Log("💎 포인트 시스템 초기화 완료");
        }
    }

    /// <summary>
    /// 컴포넌트 설정
    /// </summary>
    [Obsolete]
    void SetupComponents()
    {
        // AudioSource 설정
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
            audioSource = gameObject.AddComponent<AudioSource>();
        
        // 메인 카메라 참조
        if (mainCamera == null)
            mainCamera = Camera.main;
        
        // UI 캔버스 자동 찾기
        if (uiCanvas == null)
        {
            Canvas[] canvases = FindObjectsOfType<Canvas>();
            foreach (Canvas canvas in canvases)
            {
                if (canvas.renderMode == RenderMode.ScreenSpaceOverlay)
                {
                    uiCanvas = canvas.transform;
                    break;
                }
            }
        }
    }
    
    /// <summary>
    /// Perfect 누르기 처리
    /// </summary>
    public int ProcessPerfectPress()
    {
        if (!isInitialized) return 0;
        
        // 콤보 처리
        UpdateComboSystem(true);
        
        // 포인트 계산
        int earnedPoints = pointData.ProcessPerfectPress(perfectPressPoints, consecutiveBonus);
        
        // 레벨 체크
        CheckLevelUp();
        
        // GameManager에 점수 추가
        if (GameManager.Instance != null)
        {
            GameManager.Instance.AddScore(earnedPoints);
        }
        
        // 사운드 재생
        PlaySound(perfectSound);
        
        // 효과 생성
        ShowVisualEffect(perfectEffect, "Perfect!");
        
        // 화면 흔들림
        if (enableScreenShake)
        {
            StartCoroutine(ScreenShake(0.1f, 0.1f));
        }
        
        // 포인트 팝업 표시
        ShowPointPopup(earnedPoints, "PERFECT!", Color.yellow, transform.position);
        
        // 이벤트 발생
        OnPerfectPress?.Invoke(earnedPoints);
        OnPointsChanged?.Invoke(pointData.todaysPoints);
        OnStreakUpdate?.Invoke(pointData.GetStreakStatusText());
        
        // 연속 보너스 이벤트 (2회차부터)
        if (pointData.consecutivePerfectCount >= 2)
        {
            OnStreakBonus?.Invoke(pointData.consecutivePerfectCount);
            PlaySound(streakSound);
            
            if (streakEffect != null)
            {
                ShowVisualEffect(streakEffect, $"{pointData.consecutivePerfectCount}x Streak!");
            }
        }
        
        // UI 업데이트
        UpdateAllUI();
        
        // 마지막 액션 시간 업데이트
        lastActionTime = Time.time;
        
        if (enableDebugLogs)
        {
            Debug.Log($"🔥 Perfect! +{earnedPoints}점 (연속 {pointData.consecutivePerfectCount}회) | 총 {pointData.todaysPoints}점");
            
            if (showDetailedLogs)
            {
                Debug.Log($"💎 상세: 기본 {perfectPressPoints}점, 보너스 {earnedPoints - perfectPressPoints}점, 레벨 {currentLevel}");
            }
        }
        
        return earnedPoints;
    }
    
    /// <summary>
    /// Good 누르기 처리
    /// </summary>
    public int ProcessGoodPress()
    {
        if (!isInitialized) return 0;
        
        // 콤보 중단
        UpdateComboSystem(false);
        
        // 포인트 계산
        int earnedPoints = pointData.ProcessGoodPress(goodPressPoints);
        
        // 레벨 체크
        CheckLevelUp();
        
        // GameManager에 점수 추가
        if (GameManager.Instance != null)
        {
            GameManager.Instance.AddScore(earnedPoints);
        }
        
        // 사운드 재생
        PlaySound(goodSound);
        
        // 포인트 팝업 표시
        ShowPointPopup(earnedPoints, "GOOD!", Color.green, transform.position);
        
        // 이벤트 발생
        OnGoodPress?.Invoke(earnedPoints);
        OnPointsChanged?.Invoke(pointData.todaysPoints);
        OnStreakUpdate?.Invoke(pointData.GetStreakStatusText());
        
        // UI 업데이트
        UpdateAllUI();
        
        // 마지막 액션 시간 업데이트
        lastActionTime = Time.time;
        
        if (enableDebugLogs)
        {
            Debug.Log($"👍 Good! +{earnedPoints}점 (Perfect 연속 기록 초기화) | 총 {pointData.todaysPoints}점");
        }
        
        return earnedPoints;
    }
    
    /// <summary>
    /// Miss 누르기 처리
    /// </summary>
    public void ProcessMissPress()
    {
        if (!isInitialized) return;
        
        // 콤보 중단
        UpdateComboSystem(false);
        
        // 연속 기록 초기화
        pointData.ProcessMissPress();
        
        // 이벤트 발생
        OnStreakUpdate?.Invoke(pointData.GetStreakStatusText());
        
        // UI 업데이트
        UpdateAllUI();
        
        // 마지막 액션 시간 업데이트
        lastActionTime = Time.time;
        
        if (enableDebugLogs)
        {
            Debug.Log("❌ Miss! 연속 기록 초기화");
        }
    }
    
    /// <summary>
    /// 손님 만족 처리
    /// </summary>
    public int ProcessCustomerSatisfaction()
    {
        if (!isInitialized) return 0;
        
        // 포인트 계산
        int earnedPoints = pointData.ProcessCustomerSatisfaction(customerSatisfactionPoints, consecutiveBonus);
        
        // 레벨 체크
        CheckLevelUp();
        
        // GameManager에 점수 추가
        if (GameManager.Instance != null)
        {
            GameManager.Instance.AddScore(earnedPoints);
        }
        
        // 사운드 재생
        PlaySound(satisfactionSound);
        
        // 효과 생성
        ShowVisualEffect(satisfactionEffect, "Customer Satisfied!");
        
        // 포인트 팝업 표시
        ShowPointPopup(earnedPoints, "SATISFIED!", Color.cyan, transform.position);
        
        // 이벤트 발생
        OnCustomerSatisfaction?.Invoke(earnedPoints);
        OnPointsChanged?.Invoke(pointData.todaysPoints);
        OnStreakUpdate?.Invoke(pointData.GetStreakStatusText());
        
        // 연속 보너스 이벤트 (2명째부터)
        if (pointData.consecutiveSatisfiedCount >= 2)
        {
            OnStreakBonus?.Invoke(pointData.consecutiveSatisfiedCount);
            PlaySound(streakSound);
            
            if (streakEffect != null)
            {
                ShowVisualEffect(streakEffect, $"{pointData.consecutiveSatisfiedCount}x Customer Streak!");
            }
        }
        
        // UI 업데이트
        UpdateAllUI();
        
        // 마지막 액션 시간 업데이트
        lastActionTime = Time.time;
        
        if (enableDebugLogs)
        {
            Debug.Log($"😊 손님 만족! +{earnedPoints}점 (연속 {pointData.consecutiveSatisfiedCount}명) | 총 {pointData.todaysPoints}점");
            
            if (showDetailedLogs)
            {
                Debug.Log($"💎 상세: 기본 {customerSatisfactionPoints}점, 보너스 {earnedPoints - customerSatisfactionPoints}점");
            }
        }
        
        return earnedPoints;
    }
    
    /// <summary>
    /// 손님 불만족 처리
    /// </summary>
    public void ProcessCustomerDissatisfaction()
    {
        if (!isInitialized) return;
        
        pointData.ProcessCustomerDissatisfaction();
        
        // 이벤트 발생
        OnStreakUpdate?.Invoke(pointData.GetStreakStatusText());
        
        // UI 업데이트
        UpdateAllUI();
        
        if (enableDebugLogs)
        {
            Debug.Log("😠 손님 불만족! 연속 만족 기록 초기화");
        }
    }
    
    /// <summary>
    /// 콤보 시스템 업데이트
    /// </summary>
    void UpdateComboSystem(bool isSuccess)
    {
        if (!enableComboCooldown) return;
        
        float currentTime = Time.time;
        
        if (isSuccess)
        {
            if (currentTime - lastPerfectTime <= comboCooldownTime)
            {
                currentComboCount++;
                isInCombo = true;
            }
            else
            {
                currentComboCount = 1;
                isInCombo = false;
            }
            
            lastPerfectTime = currentTime;
        }
        else
        {
            currentComboCount = 0;
            isInCombo = false;
        }
    }
    
    /// <summary>
    /// 콤보 쿨다운 처리
    /// </summary>
    void HandleComboCooldown()
    {
        if (isInCombo && Time.time - lastPerfectTime > comboCooldownTime)
        {
            isInCombo = false;
            currentComboCount = 0;
        }
    }
    
    /// <summary>
    /// 레벨업 체크
    /// </summary>
    void CheckLevelUp()
    {
        if (!enableLevelSystem || levelThresholds == null || levelThresholds.Length == 0) return;
        
        int newLevel = CalculateLevel(pointData.todaysPoints);
        
        if (newLevel > currentLevel)
        {
            int oldLevel = currentLevel;
            currentLevel = newLevel;
            
            // 레벨업 이벤트
            OnLevelUp?.Invoke(currentLevel);
            
            // 레벨업 효과
            PlaySound(levelUpSound);
            ShowVisualEffect(levelUpEffect, $"Level {currentLevel}!");
            
            if (enableDebugLogs)
            {
                Debug.Log($"🎉 레벨업! {oldLevel} → {currentLevel}");
            }
        }
    }
    
    /// <summary>
    /// 현재 레벨 계산
    /// </summary>
    void CalculateCurrentLevel()
    {
        if (enableLevelSystem)
        {
            currentLevel = CalculateLevel(pointData.todaysPoints);
        }
    }
    
    /// <summary>
    /// 포인트로 레벨 계산
    /// </summary>
    int CalculateLevel(int points)
    {
        if (levelThresholds == null || levelThresholds.Length == 0) return 1;
        
        for (int i = levelThresholds.Length - 1; i >= 0; i--)
        {
            if (points >= levelThresholds[i])
            {
                return i + 2; // 인덱스 0 = 레벨 2
            }
        }
        
        return 1; // 기본 레벨
    }
    
    /// <summary>
    /// 하루 시작
    /// </summary>
    public void StartNewDay()
    {
        if (!isInitialized) return;
        
        pointData.StartNewDay();
        currentLevel = 1;
        currentComboCount = 0;
        isInCombo = false;
        
        // 이벤트 발생
        OnPointsChanged?.Invoke(pointData.todaysPoints);
        OnStreakUpdate?.Invoke(pointData.GetStreakStatusText());
        
        // UI 업데이트
        UpdateAllUI();
        
        if (enableDebugLogs)
        {
            Debug.Log("🌅 새로운 하루 시작! 포인트 기록 초기화");
        }
    }
    
    /// <summary>
    /// 하루 종료
    /// </summary>
    public void EndDay()
    {
        if (!isInitialized) return;
        
        pointData.EndDay();
        pointData.SaveToPlayerPrefs();
        
        if (enableDebugLogs)
        {
            Debug.Log($"🌙 하루 종료! 오늘 획득: {pointData.todaysPoints}점, 총 포인트: {pointData.currentPoints}점, 최종 레벨: {currentLevel}");
            
            if (showDetailedLogs)
            {
                PrintDailySummary();
            }
        }
    }
    
    /// <summary>
    /// 포인트 팝업 표시
    /// </summary>
    void ShowPointPopup(int points, string text, Color color, Vector3 worldPosition)
    {
        if (pointPopupPrefab == null || uiCanvas == null) return;
        
        GameObject popup = Instantiate(pointPopupPrefab, uiCanvas);
        
        // 위치 설정
        Vector3 screenPos = mainCamera.WorldToScreenPoint(worldPosition);
        popup.transform.position = screenPos + Vector3.up * 50f;
        
        // 텍스트 설정
        UnityEngine.UI.Text popupText = popup.GetComponentInChildren<UnityEngine.UI.Text>();
        if (popupText != null)
        {
            if (points > 0)
                popupText.text = $"+{points}\n{text}";
            else
                popupText.text = text;
                
            popupText.color = color;
        }
        
        // 애니메이션 시작
        StartCoroutine(AnimatePointPopup(popup));
    }
    
    /// <summary>
    /// 포인트 팝업 애니메이션
    /// </summary>
    IEnumerator AnimatePointPopup(GameObject popup)
    {
        Vector3 startPos = popup.transform.position;
        Vector3 endPos = startPos + Vector3.up * 100f;
        
        float duration = 1.5f;
        float elapsedTime = 0f;
        
        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            float progress = elapsedTime / duration;
            
            // 위치 애니메이션
            popup.transform.position = Vector3.Lerp(startPos, endPos, progress);
            
            // 알파 애니메이션
            CanvasGroup canvasGroup = popup.GetComponent<CanvasGroup>();
            if (canvasGroup != null)
            {
                canvasGroup.alpha = 1f - progress;
            }
            
            yield return null;
        }
        
        Destroy(popup);
    }
    
    /// <summary>
    /// 시각적 효과 표시
    /// </summary>
    void ShowVisualEffect(GameObject effectPrefab, string message)
    {
        if (!enableParticleEffects || effectPrefab == null) return;
        
        GameObject effect = Instantiate(effectPrefab, transform.position, Quaternion.identity);
        
        // 메시지 설정
        UnityEngine.UI.Text effectText = effect.GetComponentInChildren<UnityEngine.UI.Text>();
        if (effectText != null)
        {
            effectText.text = message;
        }
        
        // 자동 제거
        Destroy(effect, 3f);
    }
    
    /// <summary>
    /// 화면 흔들림 효과
    /// </summary>
    IEnumerator ScreenShake(float duration, float intensity)
    {
        if (mainCamera == null) yield break;
        
        Vector3 originalPosition = mainCamera.transform.position;
        
        float elapsedTime = 0f;
        
        while (elapsedTime < duration)
        {
            float x = UnityEngine.Random.Range(-1f, 1f) * intensity;
            float y = UnityEngine.Random.Range(-1f, 1f) * intensity;
            
            mainCamera.transform.position = originalPosition + new Vector3(x, y, 0);
            
            elapsedTime += Time.deltaTime;
            yield return null;
        }
        
        mainCamera.transform.position = originalPosition;
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
    /// 모든 UI 업데이트
    /// </summary>
    void UpdateAllUI()
    {
        if (currentPointsText != null)
            currentPointsText.text = $"Total: {pointData.currentPoints:N0}";
            
        if (todaysPointsText != null)
            todaysPointsText.text = $"Today: {pointData.todaysPoints:N0}";
            
        if (streakStatusText != null)
            streakStatusText.text = pointData.GetStreakStatusText();
    }
    
    /// <summary>
    /// 자동 저장 처리
    /// </summary>
    void HandleAutoSave()
    {
        if (!autoSaveEnabled) return;
        
        autoSaveTimer += Time.deltaTime;
        
        if (autoSaveTimer >= autoSaveInterval)
        {
            autoSaveTimer = 0f;
            SaveData();
        }
    }
    
    /// <summary>
    /// 테스트 입력 처리
    /// </summary>
    void HandleTestInput()
    {
        if (Input.GetKeyDown(testPerfectKey))
        {
            ProcessPerfectPress();
        }
        else if (Input.GetKeyDown(testGoodKey))
        {
            ProcessGoodPress();
        }
        else if (Input.GetKeyDown(testSatisfactionKey))
        {
            ProcessCustomerSatisfaction();
        }
    }
    
    // ===== 공개 접근자 메서드들 =====
    
    /// <summary>
    /// 현재 포인트 반환
    /// </summary>
    public int GetCurrentPoints()
    {
        return pointData.currentPoints;
    }
    
    /// <summary>
    /// 오늘 포인트 반환
    /// </summary>
    public int GetTodaysPoints()
    {
        return pointData.todaysPoints;
    }
    
    /// <summary>
    /// 연속 기록 상태 반환
    /// </summary>
    public string GetStreakStatus()
    {
        return pointData.GetStreakStatusText();
    }
    
    /// <summary>
    /// Perfect 연속 횟수 반환
    /// </summary>
    public int GetPerfectStreak()
    {
        return pointData.consecutivePerfectCount;
    }
    
    /// <summary>
    /// 만족 연속 횟수 반환
    /// </summary>
    public int GetSatisfactionStreak()
    {
        return pointData.consecutiveSatisfiedCount;
    }
    
    /// <summary>
    /// 현재 레벨 반환
    /// </summary>
    public int GetCurrentLevel()
    {
        return currentLevel;
    }
    
    /// <summary>
    /// 다음 레벨까지 필요한 포인트 반환
    /// </summary>
    public int GetPointsToNextLevel()
    {
        if (!enableLevelSystem || levelThresholds == null || currentLevel > levelThresholds.Length)
            return 0;
            
        int nextLevelThreshold = levelThresholds[currentLevel - 1];
        return Mathf.Max(0, nextLevelThreshold - pointData.todaysPoints);
    }
    
    /// <summary>
    /// 콤보 카운트 반환
    /// </summary>
    public int GetComboCount()
    {
        return currentComboCount;
    }
    
    /// <summary>
    /// 콤보 상태 반환
    /// </summary>
    public bool IsInCombo()
    {
        return isInCombo;
    }
    
    /// <summary>
    /// 포인트 데이터 반환
    /// </summary>
    public PointData GetPointData()
    {
        return pointData;
    }
    
    /// <summary>
    /// 포인트 강제 추가 (치트 또는 특별 이벤트용)
    /// </summary>
    public void AddPoints(int points, string reason = "특별 보너스")
    {
        if (!isInitialized) return;
        
        pointData.todaysPoints += points;
        
        if (GameManager.Instance != null)
        {
            GameManager.Instance.AddScore(points);
        }
        
        OnPointsChanged?.Invoke(pointData.todaysPoints);
        UpdateAllUI();
        
        // 포인트 팝업 표시
        ShowPointPopup(points, reason, Color.magenta, transform.position);
        
        if (enableDebugLogs)
        {
            Debug.Log($"💎 포인트 추가: +{points}점 ({reason})");
        }
    }
    
    /// <summary>
    /// 연속 기록 초기화 (치트 또는 특별 상황용)
    /// </summary>
    public void ResetStreaks()
    {
        if (!isInitialized) return;
        
        pointData.consecutivePerfectCount = 0;
        pointData.consecutiveSatisfiedCount = 0;
        currentComboCount = 0;
        isInCombo = false;
        
        OnStreakUpdate?.Invoke(pointData.GetStreakStatusText());
        UpdateAllUI();
        
        if (enableDebugLogs)
        {
            Debug.Log("🔄 모든 연속 기록 초기화");
        }
    }
    
    /// <summary>
    /// 레벨 강제 설정
    /// </summary>
    public void SetLevel(int level)
    {
        if (!enableLevelSystem) return;
        
        int oldLevel = currentLevel;
        currentLevel = Mathf.Max(1, level);
        
        if (currentLevel != oldLevel)
        {
            OnLevelUp?.Invoke(currentLevel);
            
            if (enableDebugLogs)
            {
                Debug.Log($"🎮 레벨 강제 설정: {oldLevel} → {currentLevel}");
            }
        }
    }
    
    /// <summary>
    /// 통계 초기화
    /// </summary>
    public void ResetAllStats()
    {
        if (!isInitialized) return;
        
        pointData.ResetAllData();
        currentLevel = 1;
        currentComboCount = 0;
        isInCombo = false;
        
        OnPointsChanged?.Invoke(pointData.todaysPoints);
        OnStreakUpdate?.Invoke(pointData.GetStreakStatusText());
        UpdateAllUI();
        
        if (enableDebugLogs)
        {
            Debug.Log("🔄 모든 통계 초기화 완료");
        }
    }
    
    /// <summary>
    /// 디버그 정보 출력
    /// </summary>
    public void PrintDebugInfo()
    {
        if (!isInitialized)
        {
            Debug.Log("❌ PointManager가 초기화되지 않았습니다.");
            return;
        }
        
        pointData.PrintDebugInfo();
        
        Debug.Log("=== PointManager 추가 정보 ===");
        Debug.Log($"현재 레벨: {currentLevel}");
        Debug.Log($"콤보 상태: {currentComboCount}회 연속 (활성: {isInCombo})");
        Debug.Log($"다음 레벨까지: {GetPointsToNextLevel()}점");
        Debug.Log($"마지막 액션: {Time.time - lastActionTime:F1}초 전");
    }
    
    /// <summary>
    /// 상세 상태 출력
    /// </summary>
    void PrintDetailedStatus()
    {
        Debug.Log("=== PointManager 상세 상태 ===");
        Debug.Log($"초기화 완료: {isInitialized}");
        Debug.Log($"자동 저장: {autoSaveEnabled} (간격: {autoSaveInterval}초)");
        Debug.Log($"레벨 시스템: {enableLevelSystem}");
        Debug.Log($"콤보 시스템: {enableComboCooldown} (쿨다운: {comboCooldownTime}초)");
        Debug.Log($"연속 보너스: {enableConsecutiveBonus} ({consecutiveBonus}% 증가)");
    }
    
    /// <summary>
    /// 일일 요약 출력
    /// </summary>
    void PrintDailySummary()
    {
        Debug.Log("=== 일일 요약 ===");
        Debug.Log($"📊 오늘 획득 포인트: {pointData.todaysPoints:N0}점");
        Debug.Log($"🏆 최고 레벨: {currentLevel}");
        Debug.Log($"🔥 최고 Perfect 연속: {pointData.maxConsecutivePerfect}회");
        Debug.Log($"😊 최고 만족 연속: {pointData.maxConsecutiveSatisfied}명");
        Debug.Log($"🎯 효율성 등급: {pointData.GetEfficiencyGrade()}");
    }
    
    /// <summary>
    /// 포인트 데이터 저장
    /// </summary>
    public void SaveData()
    {
        if (!isInitialized) return;
        
        pointData.SaveToPlayerPrefs();
        
        if (enableDebugLogs)
        {
            Debug.Log("💾 포인트 데이터 저장 완료");
        }
    }
    
    /// <summary>
    /// 포인트 데이터 로드
    /// </summary>
    public void LoadData()
    {
        if (!isInitialized) return;
        
        pointData.LoadFromPlayerPrefs();
        CalculateCurrentLevel();
        
        OnPointsChanged?.Invoke(pointData.todaysPoints);
        OnStreakUpdate?.Invoke(pointData.GetStreakStatusText());
        UpdateAllUI();
        
        if (enableDebugLogs)
        {
            Debug.Log("📁 포인트 데이터 로드 완료");
        }
    }
    
    void OnDestroy()
    {
        // 자동 저장
        if (autoSaveEnabled && isInitialized)
        {
            SaveData();
        }
        
        // 모든 코루틴 정지
        StopAllCoroutines();
    }
    
    void OnApplicationPause(bool pauseStatus)
    {
        // 앱 일시정지 시 자동 저장
        if (pauseStatus && autoSaveEnabled && isInitialized)
        {
            SaveData();
        }
    }
    
    void OnApplicationFocus(bool hasFocus)
    {
        // 앱 포커스 잃을 때 자동 저장
        if (!hasFocus && autoSaveEnabled && isInitialized)
        {
            SaveData();
        }
    }
}