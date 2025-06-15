// Assets/Scripts/Gridle/HotteokOnGriddle.cs
// 🔥 완전한 최종 버전 - 모든 게이지 및 Zone 문제 해결

using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using TMPro;

public class HotteokOnGriddle : MonoBehaviour
{
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
        
        // ✅ 입력 처리 활성화 - 수정된 조건
        HandleInput();
        
        UpdateUI();
        
        // 🔍 게이지 슬라이더 상태 모니터링
        if (enableDebugLogs && (currentState == GriddleState.ReadyToPress || currentState == GriddleState.Pressing_Holding) && pressGaugeSlider != null)
        {
            // 5초마다 게이지 상태 확인
            if (Time.frameCount % 300 == 0)
            {
                bool isActive = pressGaugeSlider.gameObject.activeInHierarchy;
                if (!isActive)
                {
                    Debug.LogWarning($"⚠️ [{gameObject.name}] 게이지가 비활성화됨! 다시 활성화 시도");
                    pressGaugeSlider.gameObject.SetActive(true);
                }
                else
                {
                    Debug.Log($"✅ [{gameObject.name}] 게이지 슬라이더 정상 작동 중 (상태: {currentState}, 값: {pressGaugeSlider.value:F2})");
                }
            }
        }
        
        if (showTimerInfo && enableDebugLogs)
        {
            UpdateDebugInfo();
        }
    }

    /// <summary>
    /// 컴포넌트 초기화
    /// </summary>
    void InitializeComponents()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer == null)
        {
            spriteRenderer = gameObject.AddComponent<SpriteRenderer>();
        }

        // ✅ 콜라이더 설정 개선
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

    /// <summary>
    /// 호떡 상태 초기화
    /// </summary>
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
        
        // 증기 효과 시작
        if (steamEffect != null)
            steamEffect.SetActive(true);
    }

    /// <summary>
    /// 호떡 초기화 (GriddleSlot에서 호출)
    /// </summary>
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

    /// <summary>
    /// 초기 스프라이트 설정
    /// </summary>
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

    /// <summary>
    /// 🔧 게이지 UI 강제 초기화
    /// </summary>
    void ForceInitializeGaugeUI()
    {
        if (pressGaugeSlider == null)
        {
            Debug.LogError($"❌ [{gameObject.name}] pressGaugeSlider가 null입니다! Inspector에서 연결을 확인하세요.");
            return;
        }

        // Canvas 찾기 및 스케일 수정
        Canvas gaugeCanvas = pressGaugeSlider.GetComponentInParent<Canvas>();
        if (gaugeCanvas != null)
        {
            // 🚨 스케일 강제 수정 (0,0,0 -> 1,1,1)
            Transform canvasTransform = gaugeCanvas.transform;
            if (canvasTransform.localScale == Vector3.zero)
            {
                canvasTransform.localScale = Vector3.one;
                Debug.Log($"🔧 [{gameObject.name}] Canvas 스케일 수정됨: {Vector3.zero} → {Vector3.one}");
            }

            // Canvas 설정 최적화
            gaugeCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            gaugeCanvas.sortingOrder = 100; // 다른 UI보다 위에 표시
            
            Debug.Log($"✅ [{gameObject.name}] Canvas 설정: RenderMode={gaugeCanvas.renderMode}, SortingOrder={gaugeCanvas.sortingOrder}");
        }
        else
        {
            Debug.LogError($"❌ [{gameObject.name}] 게이지 슬라이더의 Canvas를 찾을 수 없습니다!");
        }

        // 초기 상태 설정
        pressGaugeSlider.gameObject.SetActive(false);
        pressGaugeSlider.value = 0f;
        
        Debug.Log($"✅ [{gameObject.name}] 게이지 UI 강제 초기화 완료!");
    }

    /// <summary>
    /// ✅ 타이머 업데이트
    /// </summary>
    void UpdateTimer()
    {
        if (currentState == GriddleState.Burnt || currentState == GriddleState.Cooked) return;

        currentTimer += Time.deltaTime;

        // 디버그: 상태 정보 출력 (3초마다)
        if (enableDebugLogs && Time.frameCount % 180 == 0)
        {
            Debug.Log($"[{gameObject.name}] 상태: {currentState}, 타이머: {currentTimer:F1}s / {timeToBecomeReadyToPress:F1}s");
        }

        switch (currentState)
        {
            case GriddleState.Cooking_Unpressed:
                if (currentTimer >= timeToBecomeReadyToPress)
                {
                    if (enableDebugLogs)
                        Debug.Log($"[{gameObject.name}] 🎯 누르기 준비 상태로 전환!");
                    TransitionToReadyToPress();
                }
                break;

            case GriddleState.ReadyToPress:
                if (!isHoldingForPress && currentTimer >= timeToBecomeReadyToPress + timeToBurnIfActionMissed)
                {
                    if (enableDebugLogs)
                        Debug.Log($"[{gameObject.name}] 🔥 시간 초과로 탄 상태로 전환!");
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

    /// <summary>
    /// ✅ 입력 처리 - 수정된 조건
    /// </summary>
    void HandleInput()
    {
        // 🔧 중요 수정: Pressing_Holding 상태도 포함!
        if (currentState == GriddleState.ReadyToPress || currentState == GriddleState.Pressing_Holding)
        {
            HandlePressInput();
        }
        else if (currentState == GriddleState.ReadyToFlip)
        {
            HandleFlipInput();
        }
    }

    /// <summary>
    /// ✅ 누르기 입력 처리 - 게이지 업데이트 포함
    /// </summary>
    void HandlePressInput()
    {
        bool inputStarted = Input.GetMouseButtonDown(0);
        bool inputHeld = Input.GetMouseButton(0);
        bool inputReleased = Input.GetMouseButtonUp(0);

        if (inputStarted && IsMouseOverHotteok() && currentState == GriddleState.ReadyToPress && !isHoldingForPress)
        {
            if (enableDebugLogs)
                Debug.Log($"🖱️ [{gameObject.name}] HandleInput에서 누르기 입력 시작 감지!");
            StartPressing();
        }

        if (isHoldingForPress && currentState == GriddleState.Pressing_Holding)
        {
            if (inputHeld)
            {
                ContinuePressing();
            }
            else if (inputReleased)
            {
                if (enableDebugLogs)
                    Debug.Log($"🖱️ [{gameObject.name}] HandleInput에서 마우스 업 감지!");
                PerformPressAction();
            }
        }
    }

    /// <summary>
    /// 뒤집기 입력 처리
    /// </summary>
    void HandleFlipInput()
    {
        if (Input.GetMouseButtonDown(0) && IsMouseOverHotteok())
        {
            PerformFlipAction();
        }
    }

    /// <summary>
    /// 마우스가 호떡 위에 있는지 확인
    /// </summary>
    bool IsMouseOverHotteok()
    {
        Vector3 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        mousePos.z = 0f;
        float distance = Vector2.Distance(mousePos, transform.position);
        bool isOver = distance <= tapResponseRadius;
        
        return isOver;
    }

    /// <summary>
    /// ✅ OnMouseDown - 중복 방지
    /// </summary>
    void OnMouseDown()
    {
        if (enableDebugLogs)
            Debug.Log($"🖱️ [{gameObject.name}] OnMouseDown 호출됨! 현재 상태: {currentState}");

        if (currentState == GriddleState.ReadyToPress && !isHoldingForPress)
        {
            // HandleInput에서 처리하므로 여기서는 로그만
            if (enableDebugLogs)
                Debug.Log($"🖱️ [{gameObject.name}] OnMouseDown - ReadyToPress 상태");
        }
        else if (currentState == GriddleState.ReadyToFlip)
        {
            Vector3 mousePosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            ShowTapFeedback(mousePosition);
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

    void OnMouseUp()
    {
        if (enableDebugLogs)
            Debug.Log($"🖱️ [{gameObject.name}] OnMouseUp 호출됨! 상태: {currentState}, 누르고 있음: {isHoldingForPress}");
    }

    /// <summary>
    /// ✅ 누르기 시작
    /// </summary>
    void StartPressing()
    {
        if (enableDebugLogs)
            Debug.Log($"🎯 [{gameObject.name}] StartPressing 호출됨!");
        
        isHoldingForPress = true;
        currentHoldTime = 0.0f;
        currentState = GriddleState.Pressing_Holding;

        // 게이지 슬라이더 활성화 확인
        if (pressGaugeSlider != null)
        {
            bool wasActive = pressGaugeSlider.gameObject.activeInHierarchy;
            
            if (!wasActive)
            {
                pressGaugeSlider.gameObject.SetActive(true);
                if (enableDebugLogs)
                    Debug.Log($"🔧 [{gameObject.name}] 게이지 슬라이더 재활성화됨");
            }
            
            pressGaugeSlider.value = 0f;
            
            if (enableDebugLogs)
                Debug.Log($"✅ [{gameObject.name}] 게이지 준비 완료 (활성화: {pressGaugeSlider.gameObject.activeInHierarchy})");
        }
        else
        {
            Debug.LogError($"❌ [{gameObject.name}] pressGaugeSlider가 null입니다!");
        }

        if (enableDebugLogs)
            Debug.Log($"🎯 [{gameObject.name}] 누르기 시작 완료! 상태: {currentState}");
    }

    /// <summary>
    /// ✅ 누르기 지속 - 게이지 업데이트
    /// </summary>
    void ContinuePressing()
    {
        currentHoldTime += Time.deltaTime;
        
        if (pressGaugeSlider != null)
        {
            float gaugeValue = Mathf.Clamp01(currentHoldTime / maxHoldTimeToFillGauge);
            pressGaugeSlider.value = gaugeValue;
            
            // 디버그: 게이지 변화 확인 (0.2초마다)
            if (enableDebugLogs && Time.frameCount % 12 == 0)
            {
                Debug.Log($"🔥 [{gameObject.name}] 게이지 업데이트: {gaugeValue:F2} (홀드 시간: {currentHoldTime:F2}s)");
            }
        }
        else
        {
            if (enableDebugLogs)
                Debug.LogError($"❌ [{gameObject.name}] ContinuePressing에서 pressGaugeSlider가 null!");
        }

        // 게이지가 가득 찬 후에도 계속 누르고 있으면 다시 감소
        if (currentHoldTime > maxHoldTimeToFillGauge * 1.2f)
        {
            float overTime = currentHoldTime - maxHoldTimeToFillGauge;
            if (pressGaugeSlider != null)
            {
                pressGaugeSlider.value = Mathf.Max(0f, 1f - (overTime / maxHoldTimeToFillGauge));
            }
        }
    }

    /// <summary>
    /// ✅ 누르기 액션 수행
    /// </summary>
    void PerformPressAction()
    {
        if (enableDebugLogs)
            Debug.Log($"🎯 [{gameObject.name}] PerformPressAction 호출됨! 홀드 시간: {currentHoldTime:F2}s");

        isHoldingForPress = false;

        float pressQuality = (pressGaugeSlider != null) ? pressGaugeSlider.value : currentHoldTime / maxHoldTimeToFillGauge;
        pressQuality = Mathf.Clamp01(pressQuality);

        PressQualityResult pressResult = PressQualityResult.Miss;
        string resultString = "Miss";
        Color resultColor = Color.red;
        
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
        
        lastPressResult = pressResult;
        
        if (enableDebugLogs)
            Debug.Log($"🎯 [{gameObject.name}] 누르기 결과: {resultString} (게이지: {pressQuality:F2})");
        
        ShowPressResult(resultString, resultColor);
        ApplyPressResultEffects(pressResult);
        UpdateSpriteForPressed();
        HidePressUI();
        TransitionToPressed();
    }

    /// <summary>
    /// 눌린 상태 스프라이트로 변경
    /// </summary>
    void UpdateSpriteForPressed()
    {
        if (spriteRenderer != null)
        {
            if (currentFilling == PreparationUI.FillingType.Sugar && pressedSugarSprite != null)
                spriteRenderer.sprite = pressedSugarSprite;
            else if (currentFilling == PreparationUI.FillingType.Seed && pressedSeedSprite != null)
                spriteRenderer.sprite = pressedSeedSprite;
            else
            {
                if (enableDebugLogs)
                    Debug.LogWarning("눌린 상태에 대한 적절한 스프라이트가 없습니다.");
            }
        }
    }

    /// <summary>
    /// 💎 누르기 결과 효과 적용
    /// </summary>
    void ApplyPressResultEffects(PressQualityResult result)
    {
        // 사운드 재생
        if (pressSound != null)
        {
            AudioSource.PlayClipAtPoint(pressSound, transform.position);
        }

        // 시각적 효과
        if (pressParticleEffect != null)
        {
            GameObject effect = Instantiate(pressParticleEffect, transform.position, Quaternion.identity);
            Destroy(effect, 2f);
        }

        // 탭 효과
        if (tapEffectPrefab != null)
        {
            GameObject effect = Instantiate(tapEffectPrefab, transform.position, Quaternion.identity);
            Destroy(effect, 2f);
        }

        // 요리 시간 조정
        float originalTime = timeToBecomeReadyToFlip;
        
        switch (result)
        {
            case PressQualityResult.Perfect:
                timeToBecomeReadyToFlip *= 0.7f; // 30% 단축
                if (enableDebugLogs)
                    Debug.Log($"PERFECT! 뒤집기까지 시간 30% 단축!");
                break;
                
            case PressQualityResult.Good:
                timeToBecomeReadyToFlip *= 0.85f; // 15% 단축
                if (enableDebugLogs)
                    Debug.Log($"GOOD! 뒤집기까지 시간 15% 단축!");
                break;
                
            case PressQualityResult.Miss:
                if (enableDebugLogs)
                    Debug.Log($"MISS! 기본 쿠킹 시간 유지");
                break;
        }

        // 💎 PointManager 연동
        if (enablePointManagerIntegration && PointManager.Instance != null)
        {
            int earnedPoints = 0;
            
            switch (result)
            {
                case PressQualityResult.Perfect:
                    earnedPoints = PointManager.Instance.ProcessPerfectPress();
                    ShowPointFeedback(earnedPoints, "Perfect!", Color.yellow);
                    break;
                    
                case PressQualityResult.Good:
                    earnedPoints = PointManager.Instance.ProcessGoodPress();
                    ShowPointFeedback(earnedPoints, "Good!", Color.green);
                    break;
                    
                case PressQualityResult.Miss:
                    PointManager.Instance.ProcessMissPress();
                    ShowPointFeedback(0, "Miss!", Color.red);
                    break;
            }
            
            if (enableDebugLogs && earnedPoints > 0)
            {
                Debug.Log($"💎 PointManager: {result} 처리 완료, +{earnedPoints}점 획득");
            }
        }
        else if (enablePointManagerIntegration)
        {
            // PointManager가 없을 때 기존 시스템 사용
            if (GameManager.Instance != null)
            {
                int points = 0;
                switch (result)
                {
                    case PressQualityResult.Perfect:
                        points = 50;
                        break;
                    case PressQualityResult.Good:
                        points = 20;
                        break;
                    case PressQualityResult.Miss:
                        points = 0;
                        break;
                }
                
                if (points > 0)
                {
                    GameManager.Instance.AddScore(points);
                    ShowPointFeedback(points, result.ToString(), Color.white);
                }
            }
        }

        // 호떡 제작 통계
        if (PointManager.Instance != null)
        {
            PointManager.Instance.GetPointData().ProcessHotteokMade();
        }
    }

    /// <summary>
    /// 포인트 피드백 표시
    /// </summary>
    void ShowPointFeedback(int points, string text, Color color)
    {
        if (!showPointFeedback) return;

        if (pointFeedbackPrefab != null)
        {
            GameObject feedback = Instantiate(pointFeedbackPrefab, transform.position + Vector3.up * 0.5f, Quaternion.identity);
            
            // 피드백 텍스트 설정
            TextMeshProUGUI feedbackText = feedback.GetComponentInChildren<TextMeshProUGUI>();
            if (feedbackText != null)
            {
                if (points > 0)
                    feedbackText.text = $"+{points}\n{text}";
                else
                    feedbackText.text = text;
                    
                feedbackText.color = color;
            }
            
            // 자동 제거
            Destroy(feedback, 2f);
        }
    }

    /// <summary>
    /// 뒤집기 액션 수행
    /// </summary>
    void PerformFlipAction()
    {
        if (isFlipping) return;

        PlayFlipSound();
        StartCoroutine(FlipAnimation());
        HideFlipIndicators();
        
        if (enableDebugLogs)
        {
            Debug.Log("호떡 뒤집기!");
        }
    }

    /// <summary>
    /// 뒤집기 애니메이션
    /// </summary>
    IEnumerator FlipAnimation()
    {
        isFlipping = true;
        currentState = GriddleState.Flipping;

        Vector3 originalScale = transform.localScale;
        Vector3 startPosition = transform.position;
        float elapsedTime = 0f;

        // 뒤집기 파티클 효과
        if (flipParticleEffect != null)
        {
            GameObject effect = Instantiate(flipParticleEffect, transform.position, Quaternion.identity);
            Destroy(effect, 2f);
        }

        while (elapsedTime < flipAnimationDuration)
        {
            elapsedTime += Time.deltaTime;
            float progress = elapsedTime / flipAnimationDuration;
            float curveValue = flipCurve.Evaluate(progress);

            // X축 스케일을 이용한 뒤집기 효과
            float scaleX = Mathf.Lerp(1f, -1f, curveValue);
            transform.localScale = new Vector3(scaleX, originalScale.y, originalScale.z);

            // 살짝 위로 튀어오르는 효과
            float jumpHeight = Mathf.Sin(progress * Mathf.PI) * 0.3f;
            transform.position = startPosition + Vector3.up * jumpHeight;

            yield return null;
        }

        transform.localScale = new Vector3(-1f, originalScale.y, originalScale.z);
        transform.position = startPosition;
        
        TransitionToFlippedCooking();
        isFlipping = false;
    }

    /// <summary>
    /// 완성된 호떡을 스택 판매대로 보내기
    /// </summary>
    void SendToStackSalesCounter()
    {
        if (StackSalesCounter.Instance != null)
        {
            if (StackSalesCounter.Instance.CanAddHotteokToStack(currentFilling))
            {
                if (enableDebugLogs)
                {
                    Debug.Log($"🎉 {GetHotteokName(currentFilling)} 완성! 스택 판매대로 전송");
                }
                
                ShowDeliveryGuide();
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
        else
        {
            Debug.LogError("StackSalesCounter.Instance가 null입니다!");
        }
    }

    /// <summary>
    /// 탄 호떡 제거
    /// </summary>
    void RemoveBurntHotteok()
    {
        if (enableDebugLogs)
            Debug.Log("🔥 탄 호떡 제거!");

        if (ownerGriddleSlot != null)
        {
            ownerGriddleSlot.MakeSlotEmpty();
        }

        Destroy(gameObject);
    }

    /// <summary>
    /// UI 업데이트
    /// </summary>
    void UpdateUI()
    {
        // 결과 텍스트 타이머
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

    /// <summary>
    /// 요리 진행도 업데이트
    /// </summary>
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

    /// <summary>
    /// 요리 상태 텍스트 업데이트
    /// </summary>
    void UpdateCookingStateText()
    {
        if (cookingStateText == null) return;

        switch (currentState)
        {
            case GriddleState.Cooking_Unpressed:
                cookingStateText.text = "요리 중...";
                break;
            case GriddleState.ReadyToPress:
                cookingStateText.text = "누르기 준비!";
                break;
            case GriddleState.Pressing_Holding:
                cookingStateText.text = "누르는 중...";
                break;
            case GriddleState.Pressed_Cooking:
                cookingStateText.text = "요리 중...";
                break;
            case GriddleState.ReadyToFlip:
                cookingStateText.text = "뒤집기 준비!";
                break;
            case GriddleState.Flipping:
                cookingStateText.text = "뒤집는 중...";
                break;
            case GriddleState.Flipped_Cooking:
                cookingStateText.text = "마무리 중...";
                break;
            case GriddleState.Cooked:
                cookingStateText.text = "완성!";
                break;
            case GriddleState.Burnt:
                cookingStateText.text = "탔음!";
                break;
        }
    }

    /// <summary>
    /// 디버그 정보 업데이트
    /// </summary>
    void UpdateDebugInfo()
    {
        if (enableDebugLogs && Time.frameCount % 60 == 0) // 1초마다
        {
            Debug.Log($"호떡 디버그: 상태={currentState}, 타이머={currentTimer:F1}s, 필링={currentFilling}");
        }
    }

    // ===== UI 관리 메서드들 =====

    /// <summary>
    /// 🔧 게이지 슬라이더 위치 설정
    /// </summary>
    void SetGaugeSliderPosition()
    {
        if (pressGaugeSlider == null) return;

        Canvas canvas = pressGaugeSlider.GetComponentInParent<Canvas>();
        if (canvas == null || Camera.main == null) return;

        try
        {
            Vector3 worldPosition = transform.position + Vector3.up * 1.5f;
            Vector3 screenPosition = Camera.main.WorldToScreenPoint(worldPosition);
            
            screenPosition.x = Mathf.Clamp(screenPosition.x, 100, Screen.width - 100);
            screenPosition.y = Mathf.Clamp(screenPosition.y, 50, Screen.height - 50);
            
            pressGaugeSlider.transform.position = screenPosition;
            
            if (enableDebugLogs)
            {
                Debug.Log($"🔧 [{gameObject.name}] 게이지 위치 설정: {screenPosition}");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"❌ [{gameObject.name}] 게이지 위치 설정 실패: {e.Message}");
        }
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

            Vector3 worldPos = transform.position + Vector3.up * 0.7f;
            resultTextObject.transform.position = worldPos;
        }
    }

    void HideResultText()
    {
        if (resultTextObject != null)
        {
            resultTextObject.SetActive(false);
        }
    }

    /// <summary>
    /// ✅ Perfect Zone 표시 - 수정됨
    /// </summary>
    void ShowPressZoneIndicators()
    {
        if (perfectZoneIndicator != null)
        {
            perfectZoneIndicator.SetActive(true);
            if (enableDebugLogs)
                Debug.Log($"🟡 [{gameObject.name}] Perfect Zone 표시됨");
                
            // Perfect Zone 위치 설정
            SetZoneIndicatorPosition(perfectZoneIndicator, perfectPressMinThreshold, perfectPressMaxThreshold);
        }
        
        if (goodZoneIndicator != null)
        {
            goodZoneIndicator.SetActive(true);
            if (enableDebugLogs)
                Debug.Log($"🟢 [{gameObject.name}] Good Zone 표시됨");
                
            // Good Zone 위치 설정  
            SetZoneIndicatorPosition(goodZoneIndicator, goodPressMinThreshold, perfectPressMinThreshold);
        }
    }

    /// <summary>
    /// Zone Indicator 위치 설정
    /// </summary>
    void SetZoneIndicatorPosition(GameObject zoneIndicator, float minThreshold, float maxThreshold)
    {
        if (zoneIndicator == null || pressGaugeSlider == null) return;

        RectTransform zoneRect = zoneIndicator.GetComponent<RectTransform>();
        if (zoneRect == null) return;

        // 부모를 게이지 슬라이더로 설정
        zoneRect.SetParent(pressGaugeSlider.transform, false);
        
        // 앵커와 위치 설정
        zoneRect.anchorMin = new Vector2(minThreshold, 0);
        zoneRect.anchorMax = new Vector2(maxThreshold, 1);
        zoneRect.offsetMin = Vector2.zero;
        zoneRect.offsetMax = Vector2.zero;
        
        if (enableDebugLogs)
        {
            Debug.Log($"🎯 Zone 설정: {zoneIndicator.name} ({minThreshold:F2} ~ {maxThreshold:F2})");
        }
    }

    void HidePressUI()
    {
        if (pressGaugeSlider != null)
            pressGaugeSlider.gameObject.SetActive(false);
        if (perfectZoneIndicator != null)
            perfectZoneIndicator.SetActive(false);
        if (goodZoneIndicator != null)
            goodZoneIndicator.SetActive(false);
    }

    void ShowFlipIndicators()
    {
        isFlipIndicatorActive = true;
        
        if (flipIndicatorIcon != null)
            flipIndicatorIcon.SetActive(true);
        if (flipArrowIcon != null)
            flipArrowIcon.SetActive(true);

        if (flipIndicatorCoroutine != null)
            StopCoroutine(flipIndicatorCoroutine);
        flipIndicatorCoroutine = StartCoroutine(BlinkFlipIndicators());

        if (spriteRenderer != null)
        {
            spriteRenderer.color = Color.Lerp(Color.white, readyToFlipColor, 0.3f);
        }
    }

    void HideFlipIndicators()
    {
        isFlipIndicatorActive = false;
        
        if (flipIndicatorCoroutine != null)
        {
            StopCoroutine(flipIndicatorCoroutine);
            flipIndicatorCoroutine = null;
        }
        
        if (flipIndicatorIcon != null)
            flipIndicatorIcon.SetActive(false);
        if (flipArrowIcon != null)
            flipArrowIcon.SetActive(false);
            
        if (spriteRenderer != null)
        {
            spriteRenderer.color = Color.white;
        }
    }

    IEnumerator BlinkFlipIndicators()
    {
        while (isFlipIndicatorActive)
        {
            if (flipIndicatorIcon != null)
                flipIndicatorIcon.SetActive(true);
            if (flipArrowIcon != null)
                flipArrowIcon.SetActive(true);

            yield return new WaitForSeconds(1f / iconBlinkSpeed);

            if (flipIndicatorIcon != null)
                flipIndicatorIcon.SetActive(false);
            if (flipArrowIcon != null)
                flipArrowIcon.SetActive(false);

            yield return new WaitForSeconds(1f / iconBlinkSpeed);
        }
    }

    void HideAllIndicators()
    {
        HidePressUI();
        HideFlipIndicators();
        HideResultText();
    }

    void StartCookingTimer()
    {
        if (cookingTimerUI != null)
            cookingTimerUI.SetActive(true);
    }

    void ShowDeliveryGuide()
    {
        if (enableDebugLogs)
            Debug.Log("🎉 호떡 완성! 탭하여 스택 판매대로 보내세요!");
    }

    void ShowStackFullWarning()
    {
        if (enableDebugLogs)
            Debug.Log($"⚠️ {currentFilling} 스택이 가득찼습니다! 호떡을 손님에게 판매하세요!");
        
        if (spriteRenderer != null)
        {
            StartCoroutine(BlinkWarning());
        }
    }

    IEnumerator BlinkWarning()
    {
        Color originalColor = spriteRenderer.color;
        Color warningColor = Color.red;
        
        for (int i = 0; i < 3; i++)
        {
            spriteRenderer.color = warningColor;
            yield return new WaitForSeconds(0.2f);
            spriteRenderer.color = originalColor;
            yield return new WaitForSeconds(0.2f);
        }
    }

    void ShowTapFeedback(Vector3 position)
    {
        if (tapEffectPrefab != null)
        {
            GameObject effect = Instantiate(tapEffectPrefab, position, Quaternion.identity);
            Destroy(effect, 1f);
        }

        if (tapFeedbackSound != null)
        {
            AudioSource.PlayClipAtPoint(tapFeedbackSound, position);
        }
    }

    // ===== 사운드 메서드들 =====

    void PlayFlipSound()
    {
        if (flipSound != null)
        {
            AudioSource.PlayClipAtPoint(flipSound, transform.position);
        }
    }

    void PlayCookingCompleteSound()
    {
        if (cookingCompleteSound != null)
        {
            AudioSource.PlayClipAtPoint(cookingCompleteSound, transform.position);
        }
    }

    void PlayBurnSound()
    {
        if (burnSound != null)
        {
            AudioSource.PlayClipAtPoint(burnSound, transform.position);
        }
    }

    void PlayReadyToPressSound()
    {
        if (readyToPressSound != null)
        {
            AudioSource.PlayClipAtPoint(readyToPressSound, transform.position);
        }
    }

    void PlayReadyToFlipSound()
    {
        if (readyToFlipSound != null)
        {
            AudioSource.PlayClipAtPoint(readyToFlipSound, transform.position);
        }
    }

    // ===== 상태 전환 메서드들 =====

    void TransitionToReadyToPress()
    {
        if (enableDebugLogs)
            Debug.Log($"🎯 [{gameObject.name}] TransitionToReadyToPress 호출됨!");

        currentState = GriddleState.ReadyToPress;
        
        // 스프라이트 변경
        if (spriteRenderer != null)
        {
            if (currentFilling == PreparationUI.FillingType.Sugar && readyToPressSugarSprite != null)
                spriteRenderer.sprite = readyToPressSugarSprite;
            else if (currentFilling == PreparationUI.FillingType.Seed && readyToPressSeedSprite != null)
                spriteRenderer.sprite = readyToPressSeedSprite;
        }

        // 🔧 게이지 슬라이더 활성화
        if (pressGaugeSlider != null)
        {
            // Canvas 스케일 재확인
            Canvas gaugeCanvas = pressGaugeSlider.GetComponentInParent<Canvas>();
            if (gaugeCanvas != null && gaugeCanvas.transform.localScale == Vector3.zero)
            {
                gaugeCanvas.transform.localScale = Vector3.one;
                Debug.LogWarning($"🔧 [{gameObject.name}] Canvas 스케일 재설정!");
            }

            pressGaugeSlider.gameObject.SetActive(true);
            pressGaugeSlider.value = 0;
            SetGaugeSliderPosition();
            
            if (enableDebugLogs)
            {
                Debug.Log($"🎯 [{gameObject.name}] 게이지 활성화 완료! 위치: {pressGaugeSlider.transform.position}");
            }
        }
        else
        {
            Debug.LogError($"❌ [{gameObject.name}] pressGaugeSlider가 null입니다!");
        }

        ShowPressZoneIndicators();
        PlayReadyToPressSound();
        
        if (enableDebugLogs)
        {
            Debug.Log($"🎯 [{gameObject.name}] 호떡 누르기 준비 완료!");
        }
    }

    void TransitionToPressed()
    {
        currentState = GriddleState.Pressed_Cooking;
        currentTimer = timeToBecomeReadyToPress;
        
        if (enableDebugLogs)
        {
            Debug.Log("호떡 누르기 완료, 요리 중...");
        }
    }

    void TransitionToReadyToFlip()
    {
        currentState = GriddleState.ReadyToFlip;
        ShowFlipIndicators();
        PlayReadyToFlipSound();
        
        if (enableDebugLogs)
        {
            Debug.Log("호떡 뒤집기 준비!");
        }
    }

    void TransitionToFlippedCooking()
    {
        currentState = GriddleState.Flipped_Cooking;
        currentTimer = timeToBecomeReadyToFlip;
        StartCookingTimer();
        
        if (enableDebugLogs)
        {
            Debug.Log("호떡 뒤집기 완료, 마저 요리 중...");
        }
    }

    void TransitionToCooked()
    {
        currentState = GriddleState.Cooked;
        
        // 완성 스프라이트로 변경
        if (spriteRenderer != null)
        {
            if (currentFilling == PreparationUI.FillingType.Sugar && cookedSugarSprite != null)
                spriteRenderer.sprite = cookedSugarSprite;
            else if (currentFilling == PreparationUI.FillingType.Seed && cookedSeedSprite != null)
                spriteRenderer.sprite = cookedSeedSprite;
        }

        // 완성 효과
        if (cookingCompleteEffect != null)
        {
            GameObject effect = Instantiate(cookingCompleteEffect, transform.position, Quaternion.identity);
            Destroy(effect, 3f);
        }

        PlayCookingCompleteSound();
        
        // 증기 효과 중지
        if (steamEffect != null)
            steamEffect.SetActive(false);
        
        if (enableDebugLogs)
        {
            Debug.Log("호떡 요리 완성!");
        }
    }

    void TransitionToBurnt()
    {
        currentState = GriddleState.Burnt;
        
        // 탄 스프라이트로 변경
        if (spriteRenderer != null)
        {
            if (burntSprite != null)
            {
                spriteRenderer.sprite = burntSprite;
            }
            else if (currentFilling == PreparationUI.FillingType.Sugar && burntSugarSprite != null)
            {
                spriteRenderer.sprite = burntSugarSprite;
            }
            else if (currentFilling == PreparationUI.FillingType.Seed && burntSeedSprite != null)
            {
                spriteRenderer.sprite = burntSeedSprite;
            }
        }

        // 탄 효과
        if (burnParticleEffect != null)
        {
            GameObject effect = Instantiate(burnParticleEffect, transform.position, Quaternion.identity);
            Destroy(effect, 5f);
        }

        HideAllIndicators();
        PlayBurnSound();
        StartCoroutine(BurntBlinkEffect());
        
        if (enableDebugLogs)
        {
            Debug.Log("호떡이 탔습니다! 클릭하여 제거하세요.");
        }
    }

    /// <summary>
    /// 탄 호떡 깜빡임 효과
    /// </summary>
    IEnumerator BurntBlinkEffect()
    {
        if (spriteRenderer == null) yield break;

        Color originalColor = spriteRenderer.color;
        Color blinkColor = Color.red;

        while (currentState == GriddleState.Burnt)
        {
            spriteRenderer.color = Color.Lerp(originalColor, blinkColor, 0.7f);
            yield return new WaitForSeconds(0.5f);

            if (currentState != GriddleState.Burnt) break;

            spriteRenderer.color = originalColor;
            yield return new WaitForSeconds(0.5f);
        }

        spriteRenderer.color = originalColor;
    }

    // ===== 공개 접근자 메서드들 =====

    public void SetOwnerGriddleSlot(GriddleSlot slot)
    {
        ownerGriddleSlot = slot;
    }

    public GriddleSlot GetOwnerGriddleSlot()
    {
        return ownerGriddleSlot;
    }

    public bool IsCooked()
    {
        return currentState == GriddleState.Cooked;
    }

    public bool IsBurnt()
    {
        return currentState == GriddleState.Burnt;
    }

    public bool IsReadyForPickup()
    {
        return currentState == GriddleState.Cooked;
    }

    public PressQualityResult GetLastPressResult()
    {
        return lastPressResult;
    }

    public PreparationUI.FillingType GetFillingType()
    {
        return currentFilling;
    }

    public void SetFillingType(PreparationUI.FillingType filling)
    {
        currentFilling = filling;
        SetInitialSprite();
    }

    public GriddleState GetCurrentState()
    {
        return currentState;
    }

    public float GetCookingProgress()
    {
        switch (currentState)
        {
            case GriddleState.Cooking_Unpressed:
                return currentTimer / timeToBecomeReadyToPress;
            case GriddleState.Pressed_Cooking:
                return (currentTimer - timeToBecomeReadyToPress) / (timeToBecomeReadyToFlip - timeToBecomeReadyToPress);
            case GriddleState.Flipped_Cooking:
                return (currentTimer - timeToBecomeReadyToFlip) / (timeToBecomeCooked - timeToBecomeReadyToFlip);
            default:
                return 0f;
        }
    }

    public string GetHotteokName(PreparationUI.FillingType type)
    {
        switch (type)
        {
            case PreparationUI.FillingType.Sugar: return "설탕호떡";
            case PreparationUI.FillingType.Seed: return "씨앗호떡";
            default: return "호떡";
        }
    }

    public void ForceTransitionToState(GriddleState newState)
    {
        currentState = newState;
        if (enableDebugLogs)
        {
            Debug.Log($"강제 상태 전환: {newState}");
        }
    }

    // ===== 디버그 메서드들 =====

    [ContextMenu("🔍 Debug Gauge State")]
    public void DebugGaugeState()
    {
        Debug.Log("=== 게이지 상태 디버그 ===");
        
        if (pressGaugeSlider == null)
        {
            Debug.LogError("❌ pressGaugeSlider가 null입니다!");
            return;
        }

        Debug.Log($"게이지 오브젝트: {pressGaugeSlider.gameObject.name}");
        Debug.Log($"활성화 상태: {pressGaugeSlider.gameObject.activeInHierarchy}");
        Debug.Log($"게이지 값: {pressGaugeSlider.value}");
        Debug.Log($"위치: {pressGaugeSlider.transform.position}");
        Debug.Log($"로컬 스케일: {pressGaugeSlider.transform.localScale}");

        Canvas canvas = pressGaugeSlider.GetComponentInParent<Canvas>();
        if (canvas != null)
        {
            Debug.Log($"Canvas 타입: {canvas.renderMode}");
            Debug.Log($"Canvas 활성화: {canvas.gameObject.activeInHierarchy}");
            Debug.Log($"Canvas 스케일: {canvas.transform.localScale}");
            Debug.Log($"Canvas 정렬 순서: {canvas.sortingOrder}");
        }
        else
        {
            Debug.LogError("❌ Canvas를 찾을 수 없습니다!");
        }

        Debug.Log($"호떡 상태: {currentState}");
        Debug.Log($"누르고 있음: {isHoldingForPress}");
        Debug.Log($"호떡 위치: {transform.position}");
    }

    [ContextMenu("🔧 Force Show Gauge")]
    public void ForceShowGauge()
    {
        Debug.Log($"🔧 [{gameObject.name}] 게이지 강제 표시 시작!");
        
        currentState = GriddleState.ReadyToPress;
        TransitionToReadyToPress();
        
        Debug.Log($"🔧 [{gameObject.name}] 게이지 강제 표시 완료!");
    }

    [ContextMenu("Force Ready To Press")]
    public void ForceReadyToPress()
    {
        Debug.Log($"🔧 [{gameObject.name}] 강제로 누르기 준비 상태로 변경!");
        currentTimer = timeToBecomeReadyToPress;
        TransitionToReadyToPress();
    }

    void OnDestroy()
    {
        if (flipIndicatorCoroutine != null)
        {
            StopCoroutine(flipIndicatorCoroutine);
        }
        
        StopAllCoroutines();
    }
}