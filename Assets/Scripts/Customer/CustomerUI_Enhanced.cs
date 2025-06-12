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
    
    [Header("📝 주문 표시")]
    public GameObject orderBubble;              
    public TextMeshProUGUI orderText;          
    public Image bubbleBackground;              
    
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
    public Vector3 iconOffset = new Vector3(0, 1.2f, 0);  // 머리 위 오프셋
    public float iconScale = 1.0f;                         // 아이콘 크기
    public float pulseSpeed = 2.0f;                        // 맥박 속도
    public float bounceHeight = 0.3f;                      // 바운스 높이
    public float rotationSpeed = 90f;                      // 회전 속도
    
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
    public float orderTextSize = 14f;                      
    
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
        InitializeUI();
        SetupEmotionIconSystem();
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
        if (enableUI && worldCanvas != null && Camera.main != null)
        {
            worldCanvas.transform.LookAt(Camera.main.transform);
            worldCanvas.transform.Rotate(0, 180, 0);
        }
    }
    
    /// <summary>
    /// 🎭 감정 아이콘 시스템 설정
    /// </summary>
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
        
        // 위치 설정
        icon.transform.position = transform.position + iconOffset;
        icon.transform.localScale = Vector3.one * iconScale;
        
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
        if (!enableUI || !emotionIcons.ContainsKey(emotionKey)) 
        {
            Debug.LogWarning($"⚠️ 알 수 없는 감정 키: {emotionKey}");
            return;
        }
        
        GameObject targetIcon = emotionIcons[emotionKey];
        if (targetIcon == null) return;
        
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
        if (worldCanvas == null)
        {
            CreateWorldCanvas();
        }
        
        if (progressFillImage != null)
        {
            progressFillImage.color = normalProgressColor;
        }
        
        if (orderText != null)
        {
            orderText.color = orderTextColor;
            orderText.fontSize = orderTextSize;
        }
        
        isInitialized = true;
    }
    
    void CreateWorldCanvas()
    {
        if (!enableUI) return;
        
        GameObject canvasObj = new GameObject("CustomerUI_Canvas");
        canvasObj.transform.SetParent(transform);
        canvasObj.transform.localPosition = Vector3.up * 1.5f;
        
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
    
    public void ShowOrderBubble(List<Customer.OrderItem> orderItems)
    {
        if (!enableUI || !isInitialized || orderItems == null || orderItems.Count == 0) return;
        
        currentOrder = new List<Customer.OrderItem>(orderItems);
        string orderDisplayText = GenerateOrderDisplayText(orderItems);
        
        Debug.Log($"📋 주문 말풍선 표시: {orderDisplayText}");
        
        if (orderText != null)
        {
            orderText.text = orderDisplayText;
        }
        
        if (orderBubble != null)
        {
            orderBubble.SetActive(true);
            StartCoroutine(BubblePopAnimation(orderBubble));
        }
        
        // 📝 주문 시 기쁨 아이콘 표시
        ShowHappiness();
    }
    
    string GenerateOrderDisplayText(List<Customer.OrderItem> orderItems)
    {
        if (orderItems == null || orderItems.Count == 0) return "주문 없음";
        
        string displayText = "주문:\n";
        
        for (int i = 0; i < orderItems.Count; i++)
        {
            Customer.OrderItem item = orderItems[i];
            string itemName = GetHotteokName(item.fillingType);
            
            if (item.IsCompleted())
            {
                displayText += $"✅ {itemName} {item.quantity}개";
            }
            else
            {
                displayText += $"🔲 {itemName} {item.quantity}개";
            }
            
            if (item.receivedQuantity > 0)
            {
                displayText += $" ({item.receivedQuantity}/{item.quantity})";
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
        
        if (orderBubble != null)
        {
            StartCoroutine(BubblePopAnimation(orderBubble));
        }
        
        // 📝 부분 완료 시 만족 아이콘 표시
        ShowSatisfaction();
    }
    
    public void HideOrderBubble()
    {
        if (orderBubble != null)
        {
            orderBubble.SetActive(false);
        }
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
        
        if (orderBubble != null)
        {
            StartCoroutine(ShakeAnimation(orderBubble, 0.5f));
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
}