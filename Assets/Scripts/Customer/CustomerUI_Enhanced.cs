// Assets/Scripts/Customer/CustomerUI_Enhanced.cs
// 🎭 감정 아이콘 시스템 완전 개선 버전

using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using TMPro;

public class CustomerUI_Enhanced : MonoBehaviour
{
    [Header("UI 컨테이너")]
    public Canvas worldCanvas;                  
    public GameObject uiContainer;
    
    [Header("독립적인 UI 컨테이너")]
    public GameObject orderBubbleContainer;     // 주문 말풍선 전용 컨테이너
    public GameObject emotionIconContainer;     // 감정 아이콘 전용 컨테이너              
    
    [Header("📝 주문 표시 (프로그래밍 방식)")]
    public GameObject orderTextPanel;              // 주문 텍스트 패널 (자동 생성)
    public TextMeshProUGUI orderText;              // 주문 텍스트 (자동 생성)
    public Image textBackground;                   // 텍스트 배경 (자동 생성)              
    
    [Header("⏳ 진행 상태")]
    public Slider waitProgressSlider;           
    public Image progressFillImage;             
    public Color normalProgressColor = Color.green;                    
    public Color warningProgressColor = new Color(1f, 0.5f, 0f, 1f);  
    public Color dangerProgressColor = Color.red;                      
    
    [Header("🎭 감정 아이콘 시스템")]
    [Space(10)]
    [Header("기본 감정 아이콘")]
    public GameObject neutralIcon;              // 😐 평상시
    public GameObject happyIcon;                // 😊 기대/주문
    public GameObject waitingIcon;              // 😌 대기 중
    
    [Header("경고 및 부정적 감정")]
    public GameObject warningIcon;              // ⚠️ 경고 (기존)
    public GameObject worriedIcon;              // 😟 걱정
    public GameObject angryIcon;                // 😡 분노 (기존)
    public GameObject furiousIcon;              // 🤬 격분
    
    [Header("긍정적 감정")]
    public GameObject satisfactionIcon;         // ❤️ 만족 (기존)
    public GameObject heartIcon;                // 💖 사랑
    public GameObject starIcon;                 // ⭐ 별점
    
    [Header("기타 감정")]
    public GameObject confusedIcon;             // 😕 혼란
    public GameObject thinkingIcon;             // 🤔 생각 중
    public GameObject sleepyIcon;               // 😴 지루함
    
    [Header("🎨 아이콘 애니메이션 설정")]
    public Vector3 iconOffset = new Vector3(0.8f, 0.5f, 0);  // 얼굴 옆 오프셋 (우측)
    public float iconScale = 1.0f;                         // 아이콘 크기
    public float pulseSpeed = 2.0f;                        // 맥박 속도
    public float bounceHeight = 0.3f;                      // 바운스 높이
    public float rotationSpeed = 90f;                      // 회전 속도
    
    [Header("📋 주문 텍스트 설정")]
    public Vector3 orderTextOffset = new Vector3(0, 2.0f, 0);   // 머리 위 오프셋
    public Vector2 orderTextSize = new Vector2(3.0f, 1.0f);     // 텍스트 크기 (World Space 단위)
    
    [Header("⚡ 특수 이펙트")]
    public ParticleSystem angryParticles;       // 분노 파티클
    public ParticleSystem loveParticles;        // 사랑 파티클
    public ParticleSystem confusionParticles;   // 혼란 파티클
    
    [Header("🔊 감정 사운드")]
    public AudioClip happySound;                // 기쁨 소리
    public AudioClip angrySound;                // 분노 소리
    public AudioClip satisfiedSound;            // 만족 소리
    public AudioClip confusedSound;             // 혼란 소리
    public AudioClip warningSound;              // 경고 소리
    
    [Header("🔄 아이콘 전환 설정")]
    public float transitionDuration = 0.3f;    // 전환 시간
    public AnimationCurve transitionCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
    
    [Header("📊 피드백 텍스트")]
    public GameObject feedbackTextObject;       
    public TextMeshProUGUI feedbackText;       
    public float feedbackDisplayTime = 2.0f;   
    
    [Header("🎨 텍스트 스타일")]
    public Color orderTextColor = Color.black;             
    public Color completedTextColor = Color.green;         
    public float orderFontSize = 12f;                      // 텍스트 폰트 크기                      
    
    [Header("🐛 디버그")]
    public bool enableUI = true;               
    public bool enableAnimations = true;       // 애니메이션 활성화
    public bool enableSounds = true;           // 사운드 활성화
    public bool enableParticles = true;        // 파티클 활성화
    
    // 내부 상태
    private bool isInitialized = false;
    private Coroutine feedbackCoroutine;
    private Coroutine currentIconAnimation;
    private GameObject currentActiveIcon;
    private List<Customer.OrderItem> currentOrder = new List<Customer.OrderItem>();
    
    // 아이콘 관리
    private Dictionary<string, GameObject> emotionIcons;
    private AudioSource audioSource;
    
    void Awake()
    {
        // 초기화 순서가 중요함: 먼저 감정 시스템, 그 다음 UI
        SetupEmotionIconSystem();
        InitializeUI();
    }
    
    void Start()
    {
        if (worldCanvas != null)
        {
            worldCanvas.worldCamera = Camera.main;
        }
        
        HideAllUI();
    }
    
    void LateUpdate()
    {
        if (enableUI && Camera.main != null)
        {
            // 기존 worldCanvas 회전
            if (worldCanvas != null)
            {
                worldCanvas.transform.LookAt(Camera.main.transform);
                worldCanvas.transform.Rotate(0, 180, 0);
            }

            // 주문 말풍선 컨테이너 회전
            if (orderBubbleContainer != null)
            {
                orderBubbleContainer.transform.LookAt(Camera.main.transform);
                orderBubbleContainer.transform.Rotate(0, 180, 0);
            }

            // 감정 아이콘 컨테이너 회전
            if (emotionIconContainer != null)
            {
                emotionIconContainer.transform.LookAt(Camera.main.transform);
                emotionIconContainer.transform.Rotate(0, 180, 0);
            }
            // Position adjustment disabled - handled in ShowOrderBubble instead
        // Text positioning is now managed when the order is first displayed
        }
    }

    /// <summary>
    /// 🎭 감정 아이콘 시스템 설정
    /// </summary>
    /// 
    /// 
/// <summary>
/// 말풍선 표시 시 한 번만 위치 조정 (화면 경계 체크)
/// </summary>
void AdjustBubblePositionOnShow()
{
    if (orderBubbleContainer == null || Camera.main == null || orderTextPanel == null) return;

    // 원래 오프셋 위치로 초기화
    orderBubbleContainer.transform.localPosition = orderTextOffset;
    
    // 말풍선 컨테이너의 월드 좌표
    Vector3 bubbleWorldPos = orderBubbleContainer.transform.position;
    
    // 월드 좌표를 화면 좌표로 변환
    Vector3 screenPoint = Camera.main.WorldToScreenPoint(bubbleWorldPos);
    
    // 화면 범위 체크
    if (screenPoint.z < 0) return; // 카메라 뒤에 있으면 무시
    
    // 화면 크기
    float screenWidth = Screen.width;
    float screenHeight = Screen.height;
    
    // 텍스트의 화면상 크기 추정 (새로운 스케일링 고려)
    float bubbleScreenWidth = orderTextSize.x * 100 * 0.01f; // World Space를 화면 픽셀로 변환
    float bubbleScreenHeight = orderTextSize.y * 100 * 0.01f;
    
    // 안전 마진
    float marginX = 30f;
    float marginY = 30f;
    
    // 위치 조정 계산
    Vector3 adjustedOffset = orderTextOffset;
    
    // 좌측 경계 체크
    if (screenPoint.x - bubbleScreenWidth / 2 < marginX)
    {
        // 좌측으로 밀려나면 오른쪽으로 이동
        float adjustment = (marginX + bubbleScreenWidth / 2 - screenPoint.x) / 10f; // 새로운 스케일에 맞춰 조정
        adjustedOffset.x += adjustment;
    }
    // 우측 경계 체크
    else if (screenPoint.x + bubbleScreenWidth / 2 > screenWidth - marginX)
    {
        // 우측으로 밀려나면 왼쪽으로 이동
        float adjustment = (screenPoint.x + bubbleScreenWidth / 2 - (screenWidth - marginX)) / 10f; // 새로운 스케일에 맞춰 조정
        adjustedOffset.x -= adjustment;
    }
    
    // 상단 경계 체크
    if (screenPoint.y + bubbleScreenHeight / 2 > screenHeight - marginY)
    {
        // 상단으로 밀려나면 아래로 이동
        float adjustment = (screenPoint.y + bubbleScreenHeight / 2 - (screenHeight - marginY)) / 10f; // 새로운 스케일에 맞춰 조정
        adjustedOffset.y -= adjustment;
    }
    
    // 조정된 위치 적용
    orderBubbleContainer.transform.localPosition = adjustedOffset;
    
    Debug.Log($"📍 Text position adjusted: Original={orderTextOffset}, Adjusted={adjustedOffset}");
}

void AdjustBubblePosition()
{
    if (orderBubbleContainer == null || Camera.main == null || orderTextPanel == null) return;

    // 말풍선 컨테이너의 월드 좌표 (원래 오프셋 위치 사용)
    Vector3 originalWorldPos = transform.position + orderTextOffset;
    
    // 월드 좌표를 화면 좌표로 변환
    Vector3 screenPoint = Camera.main.WorldToScreenPoint(originalWorldPos);
    
    // 화면 범위 체크 (0 이상, 화면 크기 이하)
    if (screenPoint.z < 0) return; // 카메라 뒤에 있으면 무시
    
    // 화면 크기
    float screenWidth = Screen.width;
    float screenHeight = Screen.height;
    
    // 말풍선의 대략적인 화면상 크기 (간단한 추정)
    float bubbleScreenWidth = 80f; // 더 보수적인 크기 추정
    float bubbleScreenHeight = 40f;
    
    // 안전 마진
    float marginX = 20f;
    float marginY = 20f;
    
    // 경계 체크 및 조정
    Vector3 adjustedWorldPos = originalWorldPos;
    bool needsAdjustment = false;
    
    // 좌측 경계 체크
    if (screenPoint.x - bubbleScreenWidth / 2 < marginX)
    {
        Vector3 targetScreen = new Vector3(marginX + bubbleScreenWidth / 2, screenPoint.y, screenPoint.z);
        Vector3 targetWorld = Camera.main.ScreenToWorldPoint(targetScreen);
        adjustedWorldPos.x = targetWorld.x;
        needsAdjustment = true;
    }
    // 우측 경계 체크
    else if (screenPoint.x + bubbleScreenWidth / 2 > screenWidth - marginX)
    {
        Vector3 targetScreen = new Vector3(screenWidth - marginX - bubbleScreenWidth / 2, screenPoint.y, screenPoint.z);
        Vector3 targetWorld = Camera.main.ScreenToWorldPoint(targetScreen);
        adjustedWorldPos.x = targetWorld.x;
        needsAdjustment = true;
    }
    
    // 상단 경계 체크
    if (screenPoint.y + bubbleScreenHeight / 2 > screenHeight - marginY)
    {
        Vector3 targetScreen = new Vector3(screenPoint.x, screenHeight - marginY - bubbleScreenHeight / 2, screenPoint.z);
        Vector3 targetWorld = Camera.main.ScreenToWorldPoint(targetScreen);
        adjustedWorldPos.y = targetWorld.y;
        needsAdjustment = true;
    }
    
    // 위치 적용
    if (needsAdjustment)
    {
        orderBubbleContainer.transform.position = adjustedWorldPos;
    }
    else
    {
        // 조정이 필요없으면 원래 오프셋 위치 유지
        orderBubbleContainer.transform.localPosition = orderTextOffset;
    }
}

    void SetupEmotionIconSystem()
    {
        emotionIcons = new Dictionary<string, GameObject>
        {
            // 기본 감정
            {"neutral", neutralIcon},
            {"happy", happyIcon},
            {"waiting", waitingIcon},
            
            // 경고 및 부정적 감정  
            {"warning", warningIcon},
            {"worried", worriedIcon},
            {"angry", angryIcon},
            {"furious", furiousIcon},
            
            // 긍정적 감정
            {"satisfaction", satisfactionIcon},
            {"heart", heartIcon},
            {"star", starIcon},
            
            // 기타 감정
            {"confused", confusedIcon},
            {"thinking", thinkingIcon},
            {"sleepy", sleepyIcon}
        };
        
        // 모든 아이콘 초기 비활성화
        foreach (var kvp in emotionIcons)
        {
            if (kvp.Value != null)
            {
                kvp.Value.SetActive(false);
                SetupIconTransform(kvp.Value);
            }
        }
        
        // AudioSource 설정
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }
        audioSource.playOnAwake = false;
        audioSource.volume = 0.7f;
    }
    
    /// <summary>
    /// 🎭 아이콘 Transform 설정
    /// </summary>
    void SetupIconTransform(GameObject icon)
    {
        if (icon == null) return;
        
        // 감정 아이콘 컨테이너가 있으면 해당 컨테이너에 부모 설정
        if (emotionIconContainer != null)
        {
            icon.transform.SetParent(emotionIconContainer.transform, false);
            icon.transform.localPosition = Vector3.zero;
            icon.transform.localRotation = Quaternion.identity;
            icon.transform.localScale = Vector3.one * iconScale;
            
            // RectTransform 설정 (UI 요소인 경우)
            RectTransform iconRect = icon.GetComponent<RectTransform>();
            if (iconRect != null)
            {
                iconRect.anchoredPosition = Vector2.zero;
                iconRect.sizeDelta = new Vector2(50, 50); // 아이콘 크기
            }
        }
        else
        {
            // 기본 설정 (감정 아이콘 컨테이너가 없는 경우)
            icon.transform.position = transform.position + iconOffset;
            icon.transform.localScale = Vector3.one * iconScale;
        }
        
        // Canvas Group 추가 (페이드 효과용)
        if (icon.GetComponent<CanvasGroup>() == null)
        {
            icon.AddComponent<CanvasGroup>();
        }
    }
    
    /// <summary>
    /// 🎭 감정 아이콘 표시 (메인 함수)
    /// </summary>
    public void ShowEmotionIcon(string emotionKey, float duration = 2f, bool playSound = true)
    {
        if (!enableUI)
        {
            Debug.LogWarning("⚠️ UI가 비활성화되어 있습니다");
            return;
        }
        
        if (emotionIcons == null || !emotionIcons.ContainsKey(emotionKey)) 
        {
            Debug.LogWarning($"⚠️ 알 수 없는 감정 키 또는 감정 아이콘 시스템 미초기화: {emotionKey}");
            return;
        }
        
        GameObject targetIcon = emotionIcons[emotionKey];
        if (targetIcon == null) 
        {
            Debug.LogWarning($"⚠️ 감정 아이콘이 Inspector에 할당되지 않음: {emotionKey}");
            return;
        }
        
        // 이전 아이콘 숨기기
        HideCurrentIcon();
        
        // 새 아이콘 표시
        currentActiveIcon = targetIcon;
        targetIcon.SetActive(true);
        
        // 애니메이션 시작
        if (enableAnimations)
        {
            StartIconAnimation(emotionKey, targetIcon, duration);
        }
        
        // 사운드 재생
        if (enableSounds && playSound)
        {
            PlayEmotionSound(emotionKey);
        }
        
        // 파티클 효과
        if (enableParticles)
        {
            PlayEmotionParticles(emotionKey);
        }
        
        Debug.Log($"🎭 감정 아이콘 표시: {emotionKey}");
    }
    
    /// <summary>
    /// 🎭 감정별 애니메이션 시작
    /// </summary>
    void StartIconAnimation(string emotionKey, GameObject icon, float duration)
    {
        if (currentIconAnimation != null)
        {
            StopCoroutine(currentIconAnimation);
        }
        
        switch (emotionKey)
        {
            case "happy":
            case "satisfaction":
            case "heart":
                currentIconAnimation = StartCoroutine(BounceAnimation(icon, duration));
                break;
                
            case "angry":
            case "furious":
                currentIconAnimation = StartCoroutine(ShakeAnimation(icon, duration));
                break;
                
            case "warning":
            case "worried":
                currentIconAnimation = StartCoroutine(PulseAnimation(icon, duration));
                break;
                
            case "confused":
            case "thinking":
                currentIconAnimation = StartCoroutine(TiltAnimation(icon, duration));
                break;
                
            case "star":
                currentIconAnimation = StartCoroutine(SpinAnimation(icon, duration));
                break;
                
            default:
                currentIconAnimation = StartCoroutine(SimpleShowAnimation(icon, duration));
                break;
        }
    }
    
    /// <summary>
    /// 🎭 감정별 사운드 재생
    /// </summary>
    void PlayEmotionSound(string emotionKey)
    {
        AudioClip clipToPlay = null;
        
        switch (emotionKey)
        {
            case "happy":
            case "satisfaction":
            case "heart":
            case "star":
                clipToPlay = happySound ?? satisfiedSound;
                break;
                
            case "angry":
            case "furious":
                clipToPlay = angrySound;
                break;
                
            case "confused":
            case "thinking":
                clipToPlay = confusedSound;
                break;
                
            case "warning":
            case "worried":
                clipToPlay = warningSound;
                break;
        }
        
        if (clipToPlay != null && audioSource != null)
        {
            audioSource.PlayOneShot(clipToPlay);
        }
    }
    
    /// <summary>
    /// 🎭 감정별 파티클 효과
    /// </summary>
    void PlayEmotionParticles(string emotionKey)
    {
        ParticleSystem particleToPlay = null;
        
        switch (emotionKey)
        {
            case "angry":
            case "furious":
                particleToPlay = angryParticles;
                break;
                
            case "satisfaction":
            case "heart":
                particleToPlay = loveParticles;
                break;
                
            case "confused":
            case "thinking":
                particleToPlay = confusionParticles;
                break;
        }
        
        if (particleToPlay != null)
        {
            particleToPlay.transform.position = transform.position + iconOffset;
            particleToPlay.Play();
        }
    }
    
    /// <summary>
    /// 현재 아이콘 숨기기
    /// </summary>
    void HideCurrentIcon()
    {
        if (currentActiveIcon != null)
        {
            currentActiveIcon.SetActive(false);
            currentActiveIcon = null;
        }
        
        if (currentIconAnimation != null)
        {
            StopCoroutine(currentIconAnimation);
            currentIconAnimation = null;
        }
    }
    
    // ============= 🎬 애니메이션 코루틴들 =============
    
    /// <summary>
    /// 바운스 애니메이션 (기쁨, 만족)
    /// </summary>
    IEnumerator BounceAnimation(GameObject icon, float duration)
    {
        Vector3 originalPos = icon.transform.position;
        float elapsedTime = 0f;
        
        while (elapsedTime < duration && icon.activeInHierarchy)
        {
            elapsedTime += Time.deltaTime;
            float bounceValue = Mathf.Sin(elapsedTime * pulseSpeed * 2f) * bounceHeight;
            icon.transform.position = originalPos + Vector3.up * Mathf.Abs(bounceValue);
            yield return null;
        }
        
        icon.transform.position = originalPos;
    }
    
    /// <summary>
    /// 흔들기 애니메이션 (분노)
    /// </summary>
    IEnumerator ShakeAnimation(GameObject icon, float duration)
    {
        Vector3 originalPos = icon.transform.position;
        float elapsedTime = 0f;
        
        while (elapsedTime < duration && icon.activeInHierarchy)
        {
            elapsedTime += Time.deltaTime;
            Vector3 randomOffset = new Vector3(
                Random.Range(-0.1f, 0.1f),
                Random.Range(-0.1f, 0.1f),
                0
            );
            icon.transform.position = originalPos + randomOffset;
            yield return new WaitForSeconds(0.05f);
        }
        
        icon.transform.position = originalPos;
    }
    
    /// <summary>
    /// 맥박 애니메이션 (경고, 걱정)
    /// </summary>
    IEnumerator PulseAnimation(GameObject icon, float duration)
    {
        Vector3 originalScale = icon.transform.localScale;
        float elapsedTime = 0f;
        
        while (elapsedTime < duration && icon.activeInHierarchy)
        {
            elapsedTime += Time.deltaTime;
            float pulseValue = 1f + Mathf.Sin(elapsedTime * pulseSpeed) * 0.2f;
            icon.transform.localScale = originalScale * pulseValue;
            yield return null;
        }
        
        icon.transform.localScale = originalScale;
    }
    
    /// <summary>
    /// 기울이기 애니메이션 (혼란, 생각)
    /// </summary>
    IEnumerator TiltAnimation(GameObject icon, float duration)
    {
        Quaternion originalRotation = icon.transform.rotation;
        float elapsedTime = 0f;
        
        while (elapsedTime < duration && icon.activeInHierarchy)
        {
            elapsedTime += Time.deltaTime;
            float tiltAngle = Mathf.Sin(elapsedTime * pulseSpeed) * 15f;
            icon.transform.rotation = originalRotation * Quaternion.Euler(0, 0, tiltAngle);
            yield return null;
        }
        
        icon.transform.rotation = originalRotation;
    }
    
    /// <summary>
    /// 회전 애니메이션 (별점)
    /// </summary>
    IEnumerator SpinAnimation(GameObject icon, float duration)
    {
        float elapsedTime = 0f;
        
        while (elapsedTime < duration && icon.activeInHierarchy)
        {
            elapsedTime += Time.deltaTime;
            icon.transform.Rotate(0, 0, rotationSpeed * Time.deltaTime);
            yield return null;
        }
    }
    
    /// <summary>
    /// 간단한 표시 애니메이션
    /// </summary>
    IEnumerator SimpleShowAnimation(GameObject icon, float duration)
    {
        CanvasGroup canvasGroup = icon.GetComponent<CanvasGroup>();
        if (canvasGroup == null) yield break;
        
        // 페이드 인
        canvasGroup.alpha = 0f;
        float fadeTime = 0.3f;
        float elapsedTime = 0f;
        
        while (elapsedTime < fadeTime)
        {
            elapsedTime += Time.deltaTime;
            canvasGroup.alpha = elapsedTime / fadeTime;
            yield return null;
        }
        
        canvasGroup.alpha = 1f;
        
        // 표시 시간 대기
        yield return new WaitForSeconds(duration - fadeTime * 2);
        
        // 페이드 아웃
        elapsedTime = 0f;
        while (elapsedTime < fadeTime)
        {
            elapsedTime += Time.deltaTime;
            canvasGroup.alpha = 1f - (elapsedTime / fadeTime);
            yield return null;
        }
        
        canvasGroup.alpha = 0f;
        icon.SetActive(false);
    }
    
    // ============= 🎭 편의 함수들 =============
    
    /// <summary>
    /// 빠른 감정 표시 함수들
    /// </summary>
    public void ShowHappiness() => ShowEmotionIcon("happy", 1.5f);
    public void ShowSatisfaction() => ShowEmotionIcon("satisfaction", 2f);
    public void ShowAnger() => ShowEmotionIcon("angry", 2f);
    public void ShowFury() => ShowEmotionIcon("furious", 3f);
    public void ShowWarning() => ShowEmotionIcon("warning", -1f); // 무한 표시
    public void ShowConfusion() => ShowEmotionIcon("confused", 1.5f);
    public void ShowThinking() => ShowEmotionIcon("thinking", 2f);
    public void ShowLove() => ShowEmotionIcon("heart", 1.5f);
    public void ShowStars() => ShowEmotionIcon("star", 2f);
    
    /// <summary>
    /// 감정 조합 표시
    /// </summary>
    public void ShowEmotionSequence(string[] emotions, float[] durations)
    {
        StartCoroutine(PlayEmotionSequence(emotions, durations));
    }
    
    IEnumerator PlayEmotionSequence(string[] emotions, float[] durations)
    {
        for (int i = 0; i < emotions.Length; i++)
        {
            float duration = i < durations.Length ? durations[i] : 1f;
            ShowEmotionIcon(emotions[i], duration, i == 0); // 첫 번째만 사운드
            yield return new WaitForSeconds(duration);
        }
    }
    
    // ============= 기존 UI 함수들 (유지) =============
    
    void InitializeUI()
    {
        // 먼저 감정 아이콘 시스템 초기화
        if (emotionIcons == null)
        {
            SetupEmotionIconSystem();
        }
        
        if (worldCanvas == null)
        {
            CreateWorldCanvas();
        }
        
        // 독립 컨테이너들 생성
        CreateIndependentContainers();
        
        if (progressFillImage != null)
        {
            progressFillImage.color = normalProgressColor;
        }
        
        if (orderText != null)
        {
            orderText.color = orderTextColor;
            orderText.fontSize = orderFontSize;
        }
        
        isInitialized = true;
    }
    
    void CreateWorldCanvas()
    {
        if (!enableUI) return;
        
        GameObject canvasObj = new GameObject("CustomerUI_Canvas");
        canvasObj.transform.SetParent(transform);
        canvasObj.transform.localPosition = Vector3.zero; // 기본 위치
        
        worldCanvas = canvasObj.AddComponent<Canvas>();
        worldCanvas.renderMode = RenderMode.WorldSpace;
        worldCanvas.worldCamera = Camera.main;
        
        CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        
        RectTransform canvasRect = canvasObj.GetComponent<RectTransform>();
        canvasRect.sizeDelta = new Vector2(300, 200);
        canvasRect.localScale = Vector3.one * 0.01f;
        
        if (uiContainer == null)
        {
            uiContainer = canvasObj;
        }
        
        Debug.Log("📋 CustomerUI Canvas 자동 생성됨");
    }
    
    /// <summary>
    /// 독립적인 UI 컨테이너들 생성
    /// </summary>
    void CreateIndependentContainers()
    {
        // 주문 텍스트 컨테이너 생성 (World Space Canvas)
        if (orderBubbleContainer == null)
        {
            orderBubbleContainer = new GameObject("OrderTextContainer");
            orderBubbleContainer.transform.SetParent(transform);
            orderBubbleContainer.transform.localPosition = orderTextOffset;
            
            // Canvas 컴포넌트 추가
            Canvas textCanvas = orderBubbleContainer.AddComponent<Canvas>();
            textCanvas.renderMode = RenderMode.WorldSpace;
            textCanvas.worldCamera = Camera.main;
            textCanvas.sortingOrder = 100; // 다른 UI보다 앞에 표시
            
            // RectTransform 설정
            RectTransform containerRect = orderBubbleContainer.GetComponent<RectTransform>();
            containerRect.sizeDelta = new Vector2(100, 100); // 기본 컨테이너 크기
            containerRect.localScale = Vector3.one * 0.01f; // World Space 스케일링
            
            // 간단한 텍스트 UI 생성
            CreateSimpleOrderText();
        }
        
        // 감정 아이콘 컨테이너 생성 (World Space Canvas)
        if (emotionIconContainer == null)
        {
            emotionIconContainer = new GameObject("EmotionIconContainer");
            emotionIconContainer.transform.SetParent(transform);
            emotionIconContainer.transform.localPosition = iconOffset;
            
            // Canvas 컴포넌트 추가
            Canvas iconCanvas = emotionIconContainer.AddComponent<Canvas>();
            iconCanvas.renderMode = RenderMode.WorldSpace;
            iconCanvas.worldCamera = Camera.main;
            
            // CanvasScaler 추가
            CanvasScaler iconScaler = emotionIconContainer.AddComponent<CanvasScaler>();
            iconScaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            iconScaler.referenceResolution = new Vector2(1920, 1080);
            
            // RectTransform 설정
            RectTransform iconRect = emotionIconContainer.GetComponent<RectTransform>();
            iconRect.sizeDelta = new Vector2(100, 100); // 아이콘 컨테이너 크기
            iconRect.localScale = Vector3.one * 0.01f; // World space scaling
            
            // GraphicRaycaster 추가 (UI 상호작용용)
            emotionIconContainer.AddComponent<GraphicRaycaster>();
        }
        
        Debug.Log("📋 독립 UI 컨테이너들 생성됨 (World Space Canvas)");
        
        // 기존 UI 요소들을 새 컨테이너로 이동
        RefreshUIElementParents();
    }
    
    /// <summary>
    /// 간단한 주문 텍스트 UI 생성 (텍스트만)
    /// </summary>
    void CreateSimpleOrderText()
    {
        if (orderBubbleContainer == null) 
        {
            Debug.LogError("❌ OrderTextContainer is null! Cannot create text UI.");
            return;
        }
        
        Debug.Log("📋 Creating simple order text UI...");
        
        // 텍스트만 생성 (패널 없이)
        orderTextPanel = new GameObject("OrderText");
        orderTextPanel.transform.SetParent(orderBubbleContainer.transform, false);
        
        // RectTransform 설정
        RectTransform textRect = orderTextPanel.AddComponent<RectTransform>();
        textRect.sizeDelta = new Vector2(orderTextSize.x * 100, orderTextSize.y * 100); // World Space를 UI 픽셀로 변환
        textRect.anchoredPosition = Vector2.zero;
        
        // Unity Text 컴포넌트 사용 (간단하고 안정적)
        UnityEngine.UI.Text textComponent = orderTextPanel.AddComponent<UnityEngine.UI.Text>();
        textComponent.text = "주문 대기중...";
        textComponent.fontSize = (int)orderFontSize;
        textComponent.color = Color.white;
        textComponent.alignment = TextAnchor.MiddleCenter;
        textComponent.fontStyle = FontStyle.Bold;
        textComponent.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        
        // 초기에는 비활성화
        orderTextPanel.SetActive(false);
        
        Debug.Log($"📋 간단한 주문 텍스트 UI 생성 완료! Size: {orderTextSize}");
    }
    
    /// <summary>
    /// 둥근 모서리 스프라이트 생성 (기본 UI 스프라이트 사용)
    /// </summary>
    Sprite CreateRoundedRectSprite()
    {
        // Unity 기본 UI 스프라이트 사용
        return Resources.GetBuiltinResource<Sprite>("UI/Skin/UISprite.psd");
    }
    
    /// <summary>
    /// 기존 UI 요소들을 적절한 컨테이너로 재배치
    /// </summary>
    void RefreshUIElementParents()
    {
        // 프로그래밍 방식으로 생성된 말풍선은 이미 적절한 위치에 있으므로 스킵
        // 기존 Inspector에서 할당된 orderBubble이 있다면 무시 (새로운 방식 사용)
        
        // 모든 감정 아이콘들을 새 컨테이너로 이동 (null 체크 추가)
        if (emotionIconContainer != null && emotionIcons != null)
        {
            foreach (var kvp in emotionIcons)
            {
                if (kvp.Value != null)
                {
                    SetupIconTransform(kvp.Value);
                }
            }
        }
        else
        {
            if (emotionIconContainer == null)
                Debug.LogWarning("⚠️ EmotionIconContainer is null during RefreshUIElementParents");
            if (emotionIcons == null)
                Debug.LogWarning("⚠️ EmotionIcons dictionary is null during RefreshUIElementParents");
        }
        
        Debug.Log("📋 기존 UI 요소들이 새 컨테이너로 이동됨");
    }
    
    public void ShowOrderBubble(List<Customer.OrderItem> orderItems)
    {
        Debug.Log($"🔍 ShowOrderText called - enableUI: {enableUI}, isInitialized: {isInitialized}, orderItems count: {orderItems?.Count ?? 0}");
        
        if (!enableUI || !isInitialized || orderItems == null || orderItems.Count == 0) 
        {
            Debug.LogWarning("⚠️ ShowOrderText early return - conditions not met");
            return;
        }
        
        currentOrder = new List<Customer.OrderItem>(orderItems);
        string orderDisplayText = GenerateOrderDisplayText(orderItems);
        
        Debug.Log($"📋 주문 텍스트 표시: {orderDisplayText}");
        Debug.Log($"🔍 orderTextPanel: {(orderTextPanel != null ? "Found" : "NULL")}");
        Debug.Log($"🔍 orderBubbleContainer: {(orderBubbleContainer != null ? "Found" : "NULL")}");
        
        // 텍스트 내용 업데이트
        if (orderTextPanel != null)
        {
            // Unity Text 컴포넌트 찾기 (orderTextPanel 자체가 텍스트 오브젝트)
            UnityEngine.UI.Text textComponent = orderTextPanel.GetComponent<UnityEngine.UI.Text>();
            if (textComponent != null)
            {
                textComponent.text = orderDisplayText;
                Debug.Log($"✅ Order text updated: {orderDisplayText}");
            }
            else
            {
                Debug.LogError("❌ Text component not found in orderTextPanel!");
            }
            
            Debug.Log($"🔍 Activating orderTextPanel at position: {orderTextPanel.transform.position}");
            orderTextPanel.SetActive(true);
            
            // 간단한 페이드인 애니메이션
            StartCoroutine(SimpleTextFadeIn(orderTextPanel));
            
            Debug.Log("✅ OrderTextPanel activated!");
        }
        else
        {
            Debug.LogError("❌ orderTextPanel is NULL! Cannot show text.");
        }
        
        // 📝 주문 시 기쁨 아이콘 표시
        ShowHappiness();
    }
    
    string GenerateOrderDisplayText(List<Customer.OrderItem> orderItems)
    {
        if (orderItems == null || orderItems.Count == 0) return "주문 없음";
        
        string displayText = "";
        
        for (int i = 0; i < orderItems.Count; i++)
        {
            Customer.OrderItem item = orderItems[i];
            string itemName = GetHotteokName(item.fillingType);
            
            // Debug logging to check quantity values
            Debug.Log($"🐛 Order item {i}: {itemName}, quantity={item.quantity}, receivedQuantity={item.receivedQuantity}");
            
            // Ensure quantity is valid (minimum 1)
            int displayQuantity = Mathf.Max(1, item.quantity);
            
            // Create the quantity text separately for debugging
            string quantityText = displayQuantity.ToString();
            Debug.Log($"🐛 Quantity text: '{quantityText}', Length: {quantityText.Length}");
            
            if (item.IsCompleted())
            {
                displayText += "✅ " + itemName + " " + quantityText + "개";
            }
            else
            {
                displayText += "🔲 " + itemName + " " + quantityText + "개";
            }
            
            // Debug the complete line
            string currentLine = (item.IsCompleted() ? "✅ " : "🔲 ") + itemName + " " + quantityText + "개";
            Debug.Log($"🐛 Current line: '{currentLine}'");
            
            if (item.receivedQuantity > 0)
            {
                displayText += " (" + item.receivedQuantity.ToString() + "/" + displayQuantity.ToString() + ")";
            }
            
            if (i < orderItems.Count - 1)
            {
                displayText += "\n";
            }
        }
        
        return displayText;
    }
    
    public void UpdateOrderProgress(List<Customer.OrderItem> orderItems)
    {
        if (!enableUI || orderItems == null) return;
        
        currentOrder = new List<Customer.OrderItem>(orderItems);
        string updatedText = GenerateOrderDisplayText(orderItems);
        
        if (orderText != null)
        {
            orderText.text = updatedText;
        }
        
        Debug.Log($"📋 주문 진행 상황 업데이트: {updatedText}");
    }
    
    public void ShowPartialCompletionFeedback(string message)
    {
        if (!enableUI) return;
        
        ShowFeedbackText(message, Color.green);
        
        if (orderTextPanel != null)
        {
            StartCoroutine(SimpleTextFadeIn(orderTextPanel));
        }
        
        // 📝 부분 완료 시 만족 아이콘 표시
        ShowSatisfaction();
    }
    
    public void HideOrderBubble()
    {
        if (orderTextPanel != null)
        {
            orderTextPanel.SetActive(false);
        }
    }
    
    /// <summary>
    /// 간단한 텍스트 페이드인 애니메이션
    /// </summary>
    IEnumerator SimpleTextFadeIn(GameObject textPanel)
    {
        CanvasGroup canvasGroup = textPanel.GetComponent<CanvasGroup>();
        if (canvasGroup == null)
        {
            canvasGroup = textPanel.AddComponent<CanvasGroup>();
        }
        
        canvasGroup.alpha = 0f;
        float fadeTime = 0.3f;
        float elapsedTime = 0f;
        
        while (elapsedTime < fadeTime)
        {
            elapsedTime += Time.deltaTime;
            canvasGroup.alpha = elapsedTime / fadeTime;
            yield return null;
        }
        
        canvasGroup.alpha = 1f;
    }
    
    public void UpdateWaitProgress(float progress)
    {
        if (!enableUI) return;
        
        if (waitProgressSlider != null)
        {
            waitProgressSlider.value = progress;
        }
        
        if (progressFillImage != null)
        {
            Color targetColor;
            
            if (progress < 0.25f)
            {
                targetColor = normalProgressColor;
            }
            else if (progress < 0.75f)
            {
                targetColor = warningProgressColor;
                // 📝 경고 단계에서 걱정 아이콘 표시
                if (progress > 0.3f && progress < 0.35f) // 한 번만 표시
                {
                    ShowEmotionIcon("worried", 1f);
                }
            }
            else
            {
                targetColor = dangerProgressColor;
                // 📝 위험 단계에서 경고 아이콘 표시
                if (progress > 0.8f && progress < 0.85f) // 한 번만 표시
                {
                    ShowWarning();
                }
            }
            
            progressFillImage.color = Color.Lerp(progressFillImage.color, targetColor, Time.deltaTime * 3f);
        }
        
        if (waitProgressSlider != null)
        {
            waitProgressSlider.gameObject.SetActive(progress > 0);
        }
    }
    
    public void ShowSatisfactionEffect()
    {
        if (!enableUI) return;
        
        ShowLove(); // 사랑 아이콘 표시
        ShowFeedbackText("고마워요! 🎉", Color.green);
    }
    
    public void ShowAngryEffect()
    {
        if (!enableUI) return;
        
        ShowFury(); // 격분 아이콘 표시
        ShowFeedbackText("너무 오래 기다렸어요! 💢", Color.red);
    }
    
    public void ShowWrongOrderFeedback()
    {
        if (!enableUI) return;
        
        ShowConfusion(); // 혼란 아이콘 표시
        ShowFeedbackText("이건 제가 주문한 게 아니에요! 😕", Color.green);
        
        if (orderTextPanel != null)
        {
            StartCoroutine(SimpleTextFadeIn(orderTextPanel));
        }
    }
    
    public void ShowNoSelectionFeedback()
    {
        if (!enableUI) return;
        
        ShowThinking(); // 생각 아이콘 표시
        ShowFeedbackText("호떡을 선택해주세요! 🤔", Color.blue);
    }
    
    void ShowFeedbackText(string text, Color color)
    {
        if (!enableUI) return;
        
        if (feedbackText != null)
        {
            feedbackText.text = text;
            feedbackText.color = color;
        }
        
        if (feedbackTextObject != null)
        {
            feedbackTextObject.SetActive(true);
            
            if (feedbackCoroutine != null)
            {
                StopCoroutine(feedbackCoroutine);
            }
            feedbackCoroutine = StartCoroutine(FeedbackTextAnimation());
        }
    }
    
    public void HideAllUI()
    {
        HideOrderBubble();
        HideCurrentIcon(); // 🎭 모든 감정 아이콘 숨기기
        
        if (feedbackTextObject != null) feedbackTextObject.SetActive(false);
        if (waitProgressSlider != null) waitProgressSlider.gameObject.SetActive(false);
    }
    
    string GetHotteokName(PreparationUI.FillingType type)
    {
        switch (type)
        {
            case PreparationUI.FillingType.Sugar:
                return "설탕 호떡";
            case PreparationUI.FillingType.Seed:
                return "씨앗 호떡";
            default:
                return "알 수 없는 호떡";
        }
    }
    
    // ============= 기존 애니메이션 코루틴들 =============
    
    IEnumerator BubblePopAnimation(GameObject target)
    {
        if (!enableUI || target == null) yield break;
        
        Vector3 originalScale = target.transform.localScale;
        target.transform.localScale = Vector3.zero;
        
        float duration = 0.3f;
        float elapsedTime = 0f;
        
        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            float t = elapsedTime / duration;
            float curveValue = transitionCurve.Evaluate(t);
            
            target.transform.localScale = originalScale * curveValue;
            yield return null;
        }
        
        target.transform.localScale = originalScale;
    }
    
    IEnumerator FeedbackTextAnimation()
    {
        if (!enableUI || feedbackTextObject == null) yield break;
        
        CanvasGroup canvasGroup = feedbackTextObject.GetComponent<CanvasGroup>();
        if (canvasGroup == null)
        {
            canvasGroup = feedbackTextObject.AddComponent<CanvasGroup>();
        }
        
        canvasGroup.alpha = 0f;
        float fadeInTime = 0.3f;
        float elapsedTime = 0f;
        
        while (elapsedTime < fadeInTime)
        {
            elapsedTime += Time.deltaTime;
            canvasGroup.alpha = elapsedTime / fadeInTime;
            yield return null;
        }
        
        canvasGroup.alpha = 1f;
        yield return new WaitForSeconds(feedbackDisplayTime);
        
        float fadeOutTime = 0.3f;
        elapsedTime = 0f;
        
        while (elapsedTime < fadeOutTime)
        {
            elapsedTime += Time.deltaTime;
            canvasGroup.alpha = 1f - (elapsedTime / fadeOutTime);
            yield return null;
        }
        
        canvasGroup.alpha = 0f;
        feedbackTextObject.SetActive(false);
    }
    
    public void SetUIEnabled(bool enabled)
    {
        enableUI = enabled;
        
        if (!enabled)
        {
            HideAllUI();
        }
    }
    
    /// <summary>
    /// 🛠️ 디버그 테스트 함수들
    /// </summary>
    [ContextMenu("Test Happy Icon")]
    public void TestHappyIcon() => ShowHappiness();
    
    [ContextMenu("Test Angry Icon")]
    public void TestAngryIcon() => ShowAnger();
    
    [ContextMenu("Test Confusion Icon")]
    public void TestConfusionIcon() => ShowConfusion();
    
    [ContextMenu("Test Love Icon")]
    public void TestLoveIcon() => ShowLove();
    
    [ContextMenu("Test Emotion Sequence")]
    public void TestEmotionSequence()
    {
        string[] emotions = {"happy", "thinking", "satisfaction", "heart"};
        float[] durations = {1f, 1f, 1f, 2f};
        ShowEmotionSequence(emotions, durations);
    }
    
    /// <summary>
    /// 🎯 런타임 위치 조정 함수들
    /// </summary>
    [ContextMenu("Update Emotion Icon Position")]
    public void UpdateEmotionIconPosition()
    {
        if (emotionIconContainer != null)
        {
            emotionIconContainer.transform.localPosition = iconOffset;
            Debug.Log($"🎭 감정 아이콘 위치 업데이트: {iconOffset}");
        }
    }
    
    [ContextMenu("Update Order Bubble Position")]
    public void UpdateOrderBubblePosition()
    {
        if (orderBubbleContainer != null)
        {
            orderBubbleContainer.transform.localPosition = orderTextOffset;
            Debug.Log($"📋 주문 텍스트 위치 업데이트: {orderTextOffset}");
        }
    }
    
    [ContextMenu("Test Order Bubble")]
    public void TestOrderBubble()
    {
        List<Customer.OrderItem> testOrder = new List<Customer.OrderItem>
        {
            new Customer.OrderItem(PreparationUI.FillingType.Sugar, 2),
            new Customer.OrderItem(PreparationUI.FillingType.Seed, 1)
        };
        ShowOrderBubble(testOrder);
    }
    
    [ContextMenu("Test UI Visibility")]
    public void TestUIVisibility()
    {
        Debug.Log("=== UI 컨테이너 상태 확인 ===");
        Debug.Log($"OrderBubbleContainer: {(orderBubbleContainer != null ? "존재" : "없음")}");
        Debug.Log($"EmotionIconContainer: {(emotionIconContainer != null ? "존재" : "없음")}");
        Debug.Log($"OrderTextPanel: {(orderTextPanel != null ? "존재" : "없음")}");
        Debug.Log($"WorldCanvas: {(worldCanvas != null ? "존재" : "없음")}");
        Debug.Log($"EnableUI: {enableUI}");
        Debug.Log($"IsInitialized: {isInitialized}");
        
        if (orderBubbleContainer != null)
        {
            Debug.Log($"OrderBubbleContainer position: {orderBubbleContainer.transform.position}");
            Debug.Log($"OrderBubbleContainer active: {orderBubbleContainer.activeInHierarchy}");
        }
        
        if (emotionIconContainer != null)
        {
            Debug.Log($"EmotionIconContainer position: {emotionIconContainer.transform.position}");
            Debug.Log($"EmotionIconContainer active: {emotionIconContainer.activeInHierarchy}");
        }
    }
    
    [ContextMenu("Force Refresh UI")]
    public void ForceRefreshUI()
    {
        InitializeUI();
        RefreshUIElementParents();
        Debug.Log("UI 강제 새로고침 완료");
    }
    
    [ContextMenu("Validate Setup")]
    public void ValidateSetup()
    {
        Debug.Log("=== CustomerUI_Enhanced 설정 검증 ===");
        
        // 기본 UI 요소 확인
        Debug.Log($"OrderTextPanel: {(orderTextPanel != null ? "✅ 자동 생성됨 (텍스트만)" : "❌ 생성 필요")}");
        
        // 감정 아이콘 확인
        Debug.Log("--- 감정 아이콘 상태 ---");
        string[] requiredIcons = {"happy", "angry", "satisfaction", "warning", "confused"};
        
        foreach (string iconKey in requiredIcons)
        {
            if (emotionIcons != null && emotionIcons.ContainsKey(iconKey))
            {
                GameObject icon = emotionIcons[iconKey];
                Debug.Log($"{iconKey}: {(icon != null ? "✅ 할당됨" : "❌ 할당 필요")}");
            }
            else
            {
                Debug.Log($"{iconKey}: ❌ 키 없음");
            }
        }
        
        // 필수 설정 확인
        Debug.Log("--- 필수 설정 확인 ---");
        Debug.Log($"EnableUI: {enableUI}");
        Debug.Log($"EnableAnimations: {enableAnimations}");
        Debug.Log($"IconOffset: {iconOffset}");
        Debug.Log($"OrderTextOffset: {orderTextOffset}");
        Debug.Log($"OrderTextSize: {orderTextSize}");
        
        // 권장사항 출력
        Debug.Log("=== 설정 권장사항 ===");
        Debug.Log("1. 새로운 시스템은 자동으로 텍스트 UI를 생성합니다:");
        Debug.Log("   - Order Text (자동 생성, 배경 없음)");
        Debug.Log("   - Unity Text 컴포넌트 (자동 생성)");
        Debug.Log("   - 각종 감정 아이콘 GameObjects (수동 할당)");
        Debug.Log("2. 아이콘 위치는 iconOffset으로 조정 가능합니다.");
        Debug.Log("3. 텍스트 위치는 orderTextOffset으로 조정 가능합니다.");
    }
    
    /// <summary>
    /// 개별 위치 조정 함수들 (Inspector에서 실시간 조정 가능)
    /// </summary>
    public void SetEmotionIconPosition(Vector3 newPosition)
    {
        iconOffset = newPosition;
        UpdateEmotionIconPosition();
    }
    
    public void SetOrderTextPosition(Vector3 newPosition)
    {
        orderTextOffset = newPosition;
        UpdateOrderBubblePosition();
    }
    
    public void SetOrderTextSize(Vector2 newSize)
    {
        orderTextSize = newSize;
        if (orderTextPanel != null)
        {
            RectTransform textRect = orderTextPanel.GetComponent<RectTransform>();
            if (textRect != null)
            {
                textRect.sizeDelta = new Vector2(orderTextSize.x * 100, orderTextSize.y * 100);
            }
        }
    }
}