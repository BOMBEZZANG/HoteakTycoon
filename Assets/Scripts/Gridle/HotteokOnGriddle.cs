// Assets/Scripts/Gridle/HotteokOnGriddle.cs
// 🔥 완전한 최종 버전 - 자동 뒤집힘 문제 해결

using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using TMPro;

public class HotteokOnGriddle : MonoBehaviour
{
    // 액션 완료 후 중복 입력을 방지하기 위한 플래그
    private bool actionJustCompleted = false;

    public enum GriddleState
    {
        Cooking_Unpressed,
        ReadyToPress,
        Pressing_Holding,
        Pressed_Cooking,
        ReadyToFlip,
        Flipping,
        Flipped_Cooking,
        Cooked,
        Burnt
    }

    public enum PressQualityResult
    {
        Miss,
        Good,
        Perfect
    }

    [Header("상태 및 속성")]
    public PreparationUI.FillingType currentFilling;
    public GriddleState currentState = GriddleState.Cooking_Unpressed;
    private PressQualityResult lastPressResult = PressQualityResult.Miss;

    // 자신을 생성한 그리들 슬롯을 직접 참조
    private GriddleSlot ownerGriddleSlot;

    [Header("시간 설정")]
    public float timeToBecomeReadyToPress = 4.0f;
    public float timeToBecomeReadyToFlip = 5.0f;
    public float timeToBecomeCooked = 5.0f;
    public float timeToBurnIfActionMissed = 5.0f;

    private float currentTimer = 0.0f;
    private SpriteRenderer spriteRenderer;
    private Sprite initialUnpressedSprite;

    [Header("홀드 앤 릴리즈 누르기 설정")]
    public Slider pressGaugeSlider;
    public float maxHoldTimeToFillGauge = 1.5f;
    public float perfectPressMinThreshold = 0.8f;
    public float perfectPressMaxThreshold = 1.0f;
    public float goodPressMinThreshold = 0.5f;
    private float currentHoldTime = 0.0f;
    private bool isHoldingForPress = false;
    
    [Header("UI 및 효과")]
    public GameObject perfectZoneIndicator;
    public GameObject goodZoneIndicator;
    public GameObject resultTextObject;
    public Text resultText;
    public TextMeshProUGUI resultTextTMP;
    public float resultTextDisplayTime = 1.5f;
    private float resultTextTimer = 0f;

    [Header("요리 진행 UI")]
    public GameObject cookingTimerUI;
    public Slider cookingProgressSlider;
    public TextMeshProUGUI cookingStateText;

    [Header("뒤집기 시각적 신호")]
    public GameObject flipIndicatorIcon;
    public GameObject flipArrowIcon;
    public float iconBlinkSpeed = 2.0f;
    public Color readyToFlipColor = Color.yellow;
    private bool isFlipIndicatorActive = false;
    private Coroutine flipIndicatorCoroutine;

    [Header("탭 입력 설정")]
    public float tapResponseRadius = 1.5f;
    public AudioClip tapFeedbackSound;
    public GameObject tapEffectPrefab;

    [Header("뒤집기 애니메이션")]
    public float flipAnimationDuration = 0.5f;
    public AnimationCurve flipCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
    private bool isFlipping = false;

    [Header("스프라이트")]
    public Sprite unPressedSugarSprite;
    public Sprite pressedSugarSprite;
    public Sprite cookedSugarSprite;
    public Sprite burntSugarSprite;
    public Sprite unPressedSeedSprite;
    public Sprite pressedSeedSprite;
    public Sprite cookedSeedSprite;
    public Sprite burntSeedSprite;
    public Sprite readyToPressSugarSprite;
    public Sprite readyToPressSeedSprite;
    public Sprite burntSprite;

    [Header("사운드")]
    public AudioClip pressSound;
    public AudioClip flipSound;
    public AudioClip cookingCompleteSound;
    public AudioClip burnSound;
    public AudioClip readyToPressSound;
    public AudioClip readyToFlipSound;

    [Header("파티클 효과")]
    public GameObject pressParticleEffect;
    public GameObject flipParticleEffect;
    public GameObject cookingCompleteEffect;
    public GameObject burnParticleEffect;
    public GameObject steamEffect;

    [Header("💎 PointManager 연동 설정")]
    public bool enablePointManagerIntegration = true;
    public bool showPointFeedback = true;
    public GameObject pointFeedbackPrefab;

    [Header("디버그")]
    public bool enableDebugLogs = true;
    public bool showTimerInfo = false;

    void Start()
    {
        InitializeComponents();
        InitializeHotteok();
        SetInitialSprite();
        ForceInitializeGaugeUI();

        if (enableDebugLogs)
        {
            Debug.Log($"[{gameObject.name}] 호떡 생성: {currentFilling} 타입, 초기 상태: {currentState}");
        }
    }

    void Update()
    {
        UpdateTimer();
        HandleInput();
        UpdateUI();
        
        if (enableDebugLogs && (currentState == GriddleState.ReadyToPress || currentState == GriddleState.Pressing_Holding) && pressGaugeSlider != null)
        {
            if (Time.frameCount % 300 == 0)
            {
                bool isActive = pressGaugeSlider.gameObject.activeInHierarchy;
                if (!isActive)
                {
                    Debug.LogWarning($"⚠️ [{gameObject.name}] 게이지가 비활성화됨! 다시 활성화 시도");
                    pressGaugeSlider.gameObject.SetActive(true);
                }
            }
        }
        
        if (showTimerInfo && enableDebugLogs)
        {
            UpdateDebugInfo();
        }
    }

    void InitializeComponents()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer == null)
        {
            spriteRenderer = gameObject.AddComponent<SpriteRenderer>();
        }

        Collider2D existingCollider = GetComponent<Collider2D>();
        if (existingCollider == null)
        {
            CircleCollider2D collider = gameObject.AddComponent<CircleCollider2D>();
            collider.radius = tapResponseRadius;
            collider.isTrigger = false;
            if (enableDebugLogs)
                Debug.Log($"[{gameObject.name}] 새 콜라이더 생성됨 - 반지름: {tapResponseRadius}");
        }
        else
        {
            if (enableDebugLogs)
                Debug.Log($"[{gameObject.name}] 기존 콜라이더 사용됨: {existingCollider.GetType().Name}");
        }
    }

    void InitializeHotteok()
    {
        currentState = GriddleState.Cooking_Unpressed;
        currentTimer = 0.0f;
        isHoldingForPress = false;
        currentHoldTime = 0.0f;
        lastPressResult = PressQualityResult.Miss;
        isFlipping = false;

        if (pressGaugeSlider != null)
        {
            pressGaugeSlider.gameObject.SetActive(false);
            pressGaugeSlider.value = 0f;
        }

        if (cookingTimerUI != null)
            cookingTimerUI.SetActive(false);

        HideAllIndicators();
        
        if (steamEffect != null)
            steamEffect.SetActive(true);
    }

    public void Initialize(PreparationUI.FillingType filling, Sprite initialSprite, GriddleSlot slot)
    {
        currentFilling = filling;
        initialUnpressedSprite = initialSprite;
        ownerGriddleSlot = slot;
        
        SetInitialSprite();
        
        if (enableDebugLogs)
        {
            Debug.Log($"[{gameObject.name}] 호떡 초기화 완료: {filling} 타입");
        }
    }

    void SetInitialSprite()
    {
        if (spriteRenderer != null)
        {
            if (initialUnpressedSprite != null)
            {
                spriteRenderer.sprite = initialUnpressedSprite;
            }
            else if (currentFilling == PreparationUI.FillingType.Sugar && unPressedSugarSprite != null)
            {
                spriteRenderer.sprite = unPressedSugarSprite;
                initialUnpressedSprite = unPressedSugarSprite;
            }
            else if (currentFilling == PreparationUI.FillingType.Seed && unPressedSeedSprite != null)
            {
                spriteRenderer.sprite = unPressedSeedSprite;
                initialUnpressedSprite = unPressedSeedSprite;
            }
        }
    }

    void ForceInitializeGaugeUI()
    {
        if (pressGaugeSlider == null)
        {
            Debug.LogError($"❌ [{gameObject.name}] pressGaugeSlider가 null입니다! Inspector에서 연결을 확인하세요.");
            return;
        }

        Canvas gaugeCanvas = pressGaugeSlider.GetComponentInParent<Canvas>();
        if (gaugeCanvas != null)
        {
            Transform canvasTransform = gaugeCanvas.transform;
            if (canvasTransform.localScale == Vector3.zero)
            {
                canvasTransform.localScale = Vector3.one;
                Debug.Log($"🔧 [{gameObject.name}] Canvas 스케일 수정됨: {Vector3.zero} → {Vector3.one}");
            }

            gaugeCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            gaugeCanvas.sortingOrder = 100;
            
            Debug.Log($"✅ [{gameObject.name}] Canvas 설정: RenderMode={gaugeCanvas.renderMode}, SortingOrder={gaugeCanvas.sortingOrder}");
        }
        else
        {
            Debug.LogError($"❌ [{gameObject.name}] 게이지 슬라이더의 Canvas를 찾을 수 없습니다!");
        }

        pressGaugeSlider.gameObject.SetActive(false);
        pressGaugeSlider.value = 0f;
        
        Debug.Log($"✅ [{gameObject.name}] 게이지 UI 강제 초기화 완료!");
    }

    void UpdateTimer()
    {
        if (currentState == GriddleState.Burnt || currentState == GriddleState.Cooked) return;

        currentTimer += Time.deltaTime;

        if (enableDebugLogs && Time.frameCount % 180 == 0)
        {
            Debug.Log($"[{gameObject.name}] 상태: {currentState}, 타이머: {currentTimer:F1}s / {timeToBecomeReadyToPress:F1}s");
        }

        switch (currentState)
        {
            case GriddleState.Cooking_Unpressed:
                if (currentTimer >= timeToBecomeReadyToPress)
                {
                    TransitionToReadyToPress();
                }
                break;
            case GriddleState.ReadyToPress:
                if (!isHoldingForPress && currentTimer >= timeToBecomeReadyToPress + timeToBurnIfActionMissed)
                {
                    TransitionToBurnt();
                }
                break;
            case GriddleState.Pressed_Cooking:
                if (currentTimer >= timeToBecomeReadyToFlip)
                {
                    TransitionToReadyToFlip();
                }
                break;
            case GriddleState.ReadyToFlip:
                if (currentTimer >= timeToBecomeReadyToFlip + timeToBurnIfActionMissed)
                {
                    TransitionToBurnt();
                }
                break;
            case GriddleState.Flipped_Cooking:
                if (currentTimer >= timeToBecomeCooked)
                {
                    TransitionToCooked();
                }
                break;
        }
    }

    // ================== [수정된 부분 시작] ==================

    /// <summary>
    /// 입력 처리 로직 단일화
    /// </summary>
    void HandleInput()
    {
        // 액션이 방금 완료되었다면, 이번 프레임의 입력은 무시
        if (actionJustCompleted)
        {
            return;
        }

        // 마우스 왼쪽 버튼 클릭 시 (탭)
        if (Input.GetMouseButtonDown(0))
        {
            if (IsMouseOverHotteok())
            {
                if (currentState == GriddleState.ReadyToFlip)
                {
                    PerformFlipAction();
                }
                else if (currentState == GriddleState.Cooked)
                {
                    SendToStackSalesCounter();
                }
                else if (currentState == GriddleState.Burnt)
                {
                    RemoveBurntHotteok();
                }
            }
        }

        // 누르기 동작(홀드 앤 릴리즈) 처리
        if (currentState == GriddleState.ReadyToPress || currentState == GriddleState.Pressing_Holding)
        {
            HandlePressInput();
        }
    }
    
    /// <summary>
    /// OnMouseDown 메서드는 HandleInput으로 통합되었으므로 주석 처리하거나 삭제
    /// </summary>
    // void OnMouseDown() { ... }

    /// <summary>
    /// 누르기(홀드 앤 릴리즈) 입력 처리
    /// </summary>
    void HandlePressInput()
    {
        if (Input.GetMouseButtonDown(0) && IsMouseOverHotteok() && currentState == GriddleState.ReadyToPress && !isHoldingForPress)
        {
            StartPressing();
        }

        if (isHoldingForPress && currentState == GriddleState.Pressing_Holding)
        {
            if (Input.GetMouseButton(0))
            {
                ContinuePressing();
            }
            
            if (Input.GetMouseButtonUp(0))
            {
                PerformPressAction();
            }
        }
    }
    
    /// <summary>
    /// 액션 완료 플래그를 리셋하는 코루틴
    /// </summary>
    IEnumerator ResetActionFlag()
    {
        // 다음 프레임까지 대기
        yield return null;
        actionJustCompleted = false;
    }
    
    // ================== [수정된 부분 끝] ====================

    bool IsMouseOverHotteok()
    {
        // Use collider-based detection instead of distance-based for more precise clicking
        Collider2D hotteokCollider = GetComponent<Collider2D>();
        if (hotteokCollider == null) return false;
        
        Vector3 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        mousePos.z = 0f;
        
        // Check if mouse position is within the hotteok's collider bounds
        return hotteokCollider.bounds.Contains(mousePos);
    }

    void StartPressing()
    {
        isHoldingForPress = true;
        currentHoldTime = 0.0f;
        currentState = GriddleState.Pressing_Holding;

        if (pressGaugeSlider != null)
        {
            if (!pressGaugeSlider.gameObject.activeInHierarchy)
            {
                pressGaugeSlider.gameObject.SetActive(true);
            }
            pressGaugeSlider.value = 0f;
        }
    }

    void ContinuePressing()
    {
        currentHoldTime += Time.deltaTime;
        if (pressGaugeSlider != null)
        {
            float gaugeValue = Mathf.Clamp01(currentHoldTime / maxHoldTimeToFillGauge);
            pressGaugeSlider.value = gaugeValue;

            if (currentHoldTime > maxHoldTimeToFillGauge * 1.2f)
            {
                float overTime = currentHoldTime - maxHoldTimeToFillGauge;
                pressGaugeSlider.value = Mathf.Max(0f, 1f - (overTime / maxHoldTimeToFillGauge));
            }
        }
    }

    void PerformPressAction()
    {
        isHoldingForPress = false;
        
        float pressQuality = (pressGaugeSlider != null) ? pressGaugeSlider.value : 0;
        pressQuality = Mathf.Clamp01(pressQuality);

        PressQualityResult pressResult;
        string resultString;
        Color resultColor;

        if (pressQuality >= perfectPressMinThreshold && pressQuality <= perfectPressMaxThreshold)
        {
            pressResult = PressQualityResult.Perfect;
            resultString = "PERFECT!";
            resultColor = new Color(1f, 0.8f, 0f);
        }
        else if (pressQuality >= goodPressMinThreshold)
        {
            pressResult = PressQualityResult.Good;
            resultString = "GOOD!";
            resultColor = new Color(0f, 0.8f, 0.2f);
        }
        else
        {
            pressResult = PressQualityResult.Miss;
            resultString = "Miss";
            resultColor = Color.red;
        }
        
        lastPressResult = pressResult;
        
        ShowPressResult(resultString, resultColor);
        ApplyPressResultEffects(pressResult);
        UpdateSpriteForPressed();
        HidePressUI();
        TransitionToPressed();
        
        // 액션 완료 플래그 설정
        actionJustCompleted = true;
        StartCoroutine(ResetActionFlag());
    }

    void UpdateSpriteForPressed()
    {
        if (spriteRenderer == null) return;

        if (currentFilling == PreparationUI.FillingType.Sugar)
            spriteRenderer.sprite = pressedSugarSprite;
        else if (currentFilling == PreparationUI.FillingType.Seed)
            spriteRenderer.sprite = pressedSeedSprite;
    }

    void ApplyPressResultEffects(PressQualityResult result)
    {
        if (pressSound != null) AudioSource.PlayClipAtPoint(pressSound, transform.position);
        if (pressParticleEffect != null) Destroy(Instantiate(pressParticleEffect, transform.position, Quaternion.identity), 2f);
        if (tapEffectPrefab != null) Destroy(Instantiate(tapEffectPrefab, transform.position, Quaternion.identity), 2f);

        switch (result)
        {
            case PressQualityResult.Perfect:
                timeToBecomeReadyToFlip *= 0.7f;
                break;
            case PressQualityResult.Good:
                timeToBecomeReadyToFlip *= 0.85f;
                break;
        }

        if (enablePointManagerIntegration && PointManager.Instance != null)
        {
            int earnedPoints = 0;
            string feedbackText = "";

            switch (result)
            {
                case PressQualityResult.Perfect:
                    earnedPoints = PointManager.Instance.ProcessPerfectPress();
                    feedbackText = "Perfect!";
                    break;
                case PressQualityResult.Good:
                    earnedPoints = PointManager.Instance.ProcessGoodPress();
                    feedbackText = "Good!";
                    break;
                case PressQualityResult.Miss:
                    PointManager.Instance.ProcessMissPress();
                    feedbackText = "Miss!";
                    break;
            }
            ShowPointFeedback(earnedPoints, feedbackText, result == PressQualityResult.Perfect ? Color.yellow : result == PressQualityResult.Good ? Color.green : Color.red);
        }
        
        if (PointManager.Instance != null)
        {
            PointManager.Instance.GetPointData().ProcessHotteokMade();
        }
    }
    
    void ShowPointFeedback(int points, string text, Color color)
    {
        if (!showPointFeedback || pointFeedbackPrefab == null) return;

        GameObject feedback = Instantiate(pointFeedbackPrefab, transform.position + Vector3.up * 0.5f, Quaternion.identity);
        TextMeshProUGUI feedbackText = feedback.GetComponentInChildren<TextMeshProUGUI>();
        if (feedbackText != null)
        {
            feedbackText.text = points > 0 ? $"+{points}\n{text}" : text;
            feedbackText.color = color;
        }
        Destroy(feedback, 2f);
    }
    
    void PerformFlipAction()
    {
        if (isFlipping) return;

        PlayFlipSound();
        StartCoroutine(FlipAnimation());
        HideFlipIndicators();
        
        // 액션 완료 플래그 설정
        actionJustCompleted = true;
        StartCoroutine(ResetActionFlag());
    }

    IEnumerator FlipAnimation()
    {
        isFlipping = true;
        currentState = GriddleState.Flipping;

        Vector3 originalScale = transform.localScale;
        Vector3 startPosition = transform.position;
        float elapsedTime = 0f;

        if (flipParticleEffect != null) Destroy(Instantiate(flipParticleEffect, transform.position, Quaternion.identity), 2f);

        while (elapsedTime < flipAnimationDuration)
        {
            elapsedTime += Time.deltaTime;
            float progress = elapsedTime / flipAnimationDuration;
            float curveValue = flipCurve.Evaluate(progress);

            transform.localScale = new Vector3(Mathf.Lerp(1f, -1f, curveValue), originalScale.y, originalScale.z);
            transform.position = startPosition + Vector3.up * (Mathf.Sin(progress * Mathf.PI) * 0.3f);
            yield return null;
        }

        transform.localScale = new Vector3(-1f, originalScale.y, originalScale.z);
        transform.position = startPosition;
        
        TransitionToFlippedCooking();
        isFlipping = false;
    }

    void SendToStackSalesCounter()
    {
        if (StackSalesCounter.Instance != null)
        {
            if (StackSalesCounter.Instance.CanAddHotteokToStack(currentFilling))
            {
                StackSalesCounter.Instance.AddHotteokToStack(gameObject, currentFilling);
                if (ownerGriddleSlot != null)
                {
                    ownerGriddleSlot.MakeSlotEmpty();
                }
            }
            else
            {
                ShowStackFullWarning();
            }
        }
    }

    void RemoveBurntHotteok()
    {
        if (ownerGriddleSlot != null)
        {
            ownerGriddleSlot.MakeSlotEmpty();
        }
        Destroy(gameObject);
    }

    void UpdateUI()
    {
        if (resultTextTimer > 0f)
        {
            resultTextTimer -= Time.deltaTime;
            if (resultTextTimer <= 0f)
            {
                HideResultText();
            }
        }
        UpdateCookingProgress();
        UpdateCookingStateText();
    }

    void UpdateCookingProgress()
    {
        if (cookingProgressSlider == null) return;
        float progress = 0f;
        
        switch (currentState)
        {
            case GriddleState.Cooking_Unpressed:
                progress = currentTimer / timeToBecomeReadyToPress;
                break;
            case GriddleState.Pressed_Cooking:
                progress = (currentTimer - timeToBecomeReadyToPress) / (timeToBecomeReadyToFlip - timeToBecomeReadyToPress);
                break;
            case GriddleState.Flipped_Cooking:
                progress = (currentTimer - timeToBecomeReadyToFlip) / (timeToBecomeCooked - timeToBecomeReadyToFlip);
                break;
            case GriddleState.Cooked:
                progress = 1f;
                break;
        }
        cookingProgressSlider.value = Mathf.Clamp01(progress);
    }

    void UpdateCookingStateText()
    {
        if (cookingStateText == null) return;
        string text = "";
        switch (currentState)
        {
            case GriddleState.ReadyToPress: text = "누르기 준비!"; break;
            case GriddleState.ReadyToFlip: text = "뒤집기 준비!"; break;
            case GriddleState.Cooked: text = "완성!"; break;
            case GriddleState.Burnt: text = "탔음!"; break;
            default: text = "요리 중..."; break;
        }
        cookingStateText.text = text;
    }

    void UpdateDebugInfo()
    {
        if (enableDebugLogs && Time.frameCount % 60 == 0)
        {
            Debug.Log($"호떡 디버그: 상태={currentState}, 타이머={currentTimer:F1}s, 필링={currentFilling}");
        }
    }

    void SetGaugeSliderPosition()
    {
        if (pressGaugeSlider == null || Camera.main == null) return;
        Vector3 worldPosition = transform.position + Vector3.up * 1.5f;
        pressGaugeSlider.transform.position = Camera.main.WorldToScreenPoint(worldPosition);
    }

    void ShowPressResult(string result, Color color)
    {
        if (resultTextObject != null)
        {
            resultTextObject.SetActive(true);
            resultTextTimer = resultTextDisplayTime;

            if (resultText != null)
            {
                resultText.text = result;
                resultText.color = color;
            }
            if (resultTextTMP != null)
            {
                resultTextTMP.text = result;
                resultTextTMP.color = color;
            }
            resultTextObject.transform.position = transform.position + Vector3.up * 0.7f;
        }
    }

    void HideResultText()
    {
        if (resultTextObject != null) resultTextObject.SetActive(false);
    }

    void ShowPressZoneIndicators()
    {
        if (perfectZoneIndicator != null)
        {
            perfectZoneIndicator.SetActive(true);
            SetZoneIndicatorPosition(perfectZoneIndicator, perfectPressMinThreshold, perfectPressMaxThreshold);
        }
        if (goodZoneIndicator != null)
        {
            goodZoneIndicator.SetActive(true);
            SetZoneIndicatorPosition(goodZoneIndicator, goodPressMinThreshold, perfectPressMinThreshold);
        }
    }

    void SetZoneIndicatorPosition(GameObject zoneIndicator, float minThreshold, float maxThreshold)
    {
        if (zoneIndicator == null || pressGaugeSlider == null) return;
        RectTransform zoneRect = zoneIndicator.GetComponent<RectTransform>();
        if (zoneRect == null) return;

        zoneRect.SetParent(pressGaugeSlider.transform, false);
        zoneRect.anchorMin = new Vector2(minThreshold, 0);
        zoneRect.anchorMax = new Vector2(maxThreshold, 1);
        zoneRect.offsetMin = Vector2.zero;
        zoneRect.offsetMax = Vector2.zero;
    }

    void HidePressUI()
    {
        if (pressGaugeSlider != null) pressGaugeSlider.gameObject.SetActive(false);
        if (perfectZoneIndicator != null) perfectZoneIndicator.SetActive(false);
        if (goodZoneIndicator != null) goodZoneIndicator.SetActive(false);
    }

    void ShowFlipIndicators()
    {
        isFlipIndicatorActive = true;
        if (flipIndicatorIcon != null) flipIndicatorIcon.SetActive(true);
        if (flipArrowIcon != null) flipArrowIcon.SetActive(true);

        if (flipIndicatorCoroutine != null) StopCoroutine(flipIndicatorCoroutine);
        flipIndicatorCoroutine = StartCoroutine(BlinkFlipIndicators());

        if (spriteRenderer != null) spriteRenderer.color = Color.Lerp(Color.white, readyToFlipColor, 0.3f);
    }

    void HideFlipIndicators()
    {
        isFlipIndicatorActive = false;
        if (flipIndicatorCoroutine != null) StopCoroutine(flipIndicatorCoroutine);
        
        if (flipIndicatorIcon != null) flipIndicatorIcon.SetActive(false);
        if (flipArrowIcon != null) flipArrowIcon.SetActive(false);
            
        if (spriteRenderer != null) spriteRenderer.color = Color.white;
    }

    IEnumerator BlinkFlipIndicators()
    {
        while (isFlipIndicatorActive)
        {
            if (flipIndicatorIcon != null) flipIndicatorIcon.SetActive(true);
            if (flipArrowIcon != null) flipArrowIcon.SetActive(true);
            yield return new WaitForSeconds(1f / iconBlinkSpeed);

            if (flipIndicatorIcon != null) flipIndicatorIcon.SetActive(false);
            if (flipArrowIcon != null) flipArrowIcon.SetActive(false);
            yield return new WaitForSeconds(1f / iconBlinkSpeed);
        }
    }

    void HideAllIndicators()
    {
        HidePressUI();
        HideFlipIndicators();
        HideResultText();
    }

    void ShowStackFullWarning()
    {
        if (enableDebugLogs) Debug.Log($"⚠️ {currentFilling} 스택이 가득찼습니다!");
        if (spriteRenderer != null) StartCoroutine(BlinkWarning());
    }

    IEnumerator BlinkWarning()
    {
        Color originalColor = spriteRenderer.color;
        for (int i = 0; i < 3; i++)
        {
            spriteRenderer.color = Color.red;
            yield return new WaitForSeconds(0.2f);
            spriteRenderer.color = originalColor;
            yield return new WaitForSeconds(0.2f);
        }
    }

    void PlayFlipSound()
    {
        if (flipSound != null) AudioSource.PlayClipAtPoint(flipSound, transform.position);
    }

    void TransitionToReadyToPress()
    {
        currentState = GriddleState.ReadyToPress;
        if (spriteRenderer != null)
        {
            if (currentFilling == PreparationUI.FillingType.Sugar) spriteRenderer.sprite = readyToPressSugarSprite;
            else if (currentFilling == PreparationUI.FillingType.Seed) spriteRenderer.sprite = readyToPressSeedSprite;
        }

        if (pressGaugeSlider != null)
        {
            pressGaugeSlider.gameObject.SetActive(true);
            pressGaugeSlider.value = 0;
            SetGaugeSliderPosition();
        }

        ShowPressZoneIndicators();
        if (readyToPressSound != null) AudioSource.PlayClipAtPoint(readyToPressSound, transform.position);
    }

    void TransitionToPressed()
    {
        currentState = GriddleState.Pressed_Cooking;
        currentTimer = timeToBecomeReadyToPress;
    }

    void TransitionToReadyToFlip()
    {
        currentState = GriddleState.ReadyToFlip;
        ShowFlipIndicators();
        if (readyToFlipSound != null) AudioSource.PlayClipAtPoint(readyToFlipSound, transform.position);
    }

    void TransitionToFlippedCooking()
    {
        currentState = GriddleState.Flipped_Cooking;
        currentTimer = timeToBecomeReadyToFlip;
    }

    void TransitionToCooked()
    {
        currentState = GriddleState.Cooked;
        
        if (spriteRenderer != null)
        {
            if (currentFilling == PreparationUI.FillingType.Sugar) spriteRenderer.sprite = cookedSugarSprite;
            else if (currentFilling == PreparationUI.FillingType.Seed) spriteRenderer.sprite = cookedSeedSprite;
        }

        if (cookingCompleteEffect != null) Destroy(Instantiate(cookingCompleteEffect, transform.position, Quaternion.identity), 3f);
        if (cookingCompleteSound != null) AudioSource.PlayClipAtPoint(cookingCompleteSound, transform.position);
        if (steamEffect != null) steamEffect.SetActive(false);
    }

    void TransitionToBurnt()
    {
        currentState = GriddleState.Burnt;
        
        if (spriteRenderer != null)
        {
            spriteRenderer.sprite = burntSprite;
        }

        if (burnParticleEffect != null) Destroy(Instantiate(burnParticleEffect, transform.position, Quaternion.identity), 5f);
        HideAllIndicators();
        if (burnSound != null) AudioSource.PlayClipAtPoint(burnSound, transform.position);
        StartCoroutine(BurntBlinkEffect());
    }

    IEnumerator BurntBlinkEffect()
    {
        if (spriteRenderer == null) yield break;
        Color originalColor = spriteRenderer.color;
        while (currentState == GriddleState.Burnt)
        {
            spriteRenderer.color = Color.Lerp(originalColor, Color.red, 0.7f);
            yield return new WaitForSeconds(0.5f);
            if (currentState != GriddleState.Burnt) break;
            spriteRenderer.color = originalColor;
            yield return new WaitForSeconds(0.5f);
        }
        spriteRenderer.color = originalColor;
    }

    public void SetOwnerGriddleSlot(GriddleSlot slot) { ownerGriddleSlot = slot; }
    public GriddleSlot GetOwnerGriddleSlot() { return ownerGriddleSlot; }
    public bool IsCooked() { return currentState == GriddleState.Cooked; }
    public void ForceTransitionToState(GriddleState newState) { currentState = newState; }

    void OnDestroy()
    {
        StopAllCoroutines();
    }
}