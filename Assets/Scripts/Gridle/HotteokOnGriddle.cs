// Assets/Scripts/Gridle/HotteokOnGriddle.cs
// 개선된 뒤집기 기능이 포함된 호떡 철판 스크립트

using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using TMPro;

public class HotteokOnGriddle : MonoBehaviour
{
    public enum GriddleState
    {
        Cooking_Unpressed,      // 1. 초기 익는 중
        ReadyToPress,           // 2. 누르기 대기
        Pressing_Holding,       // (내부 처리용)
        Pressed_Cooking,        // 3. 눌린 후 익는 중
        ReadyToFlip,            // 4. 뒤집기 대기
        Flipping,               // 4a. 뒤집히는 중 (애니메이션)
        Flipped_Cooking,        // 5. 뒤집힌 후 익는 중
        Cooked,                 // 6. 완성
        Burnt                   // 7. 탐
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

    [Header("시간 설정")]
    public float timeToBecomeReadyToPress = 4.0f;
    public float timeToBecomeReadyToFlip = 5.0f;
    public float timeToBecomeCooked = 5.0f;
    public float timeToBurnIfActionMissed = 5.0f;

    private float currentTimer = 0.0f;
    private SpriteRenderer spriteRenderer;

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
    public float resultTextDisplayTime = 1.5f;
    private float resultTextTimer = 0f;

    [Header("====== 1단계: 뒤집기 시각적 신호 ======")]
    public GameObject flipIndicatorIcon;        // 뒤집기 아이콘
    public GameObject flipArrowIcon;           // 화살표 아이콘 (추가)
    public float iconBlinkSpeed = 2.0f;        // 깜빡임 속도
    public Color readyToFlipColor = Color.yellow; // 뒤집기 준비 색상
    private bool isFlipIndicatorActive = false;
    private Coroutine flipIndicatorCoroutine;

    [Header("====== 2단계: 탭 입력 설정 ======")]
    public float tapResponseRadius = 1.5f;     // 탭 감지 반경 확대
    public AudioClip tapFeedbackSound;         // 탭 피드백 사운드
    public GameObject tapEffectPrefab;         // 탭 이펙트

    [Header("====== 3단계: 뒤집기 애니메이션 ======")]
    public float flipAnimationDuration = 0.5f; // 애니메이션 지속시간
    public AnimationCurve flipCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f); // 애니메이션 커브
    public Vector3 flipRotationAxis = Vector3.forward; // 회전축
    public float flipHeight = 0.3f;           // 뒤집을 때 높이
    
    [Header("====== 4단계: 후속 타이머 UI ======")]
    public Slider cookingProgressSlider;       // 요리 진행도 슬라이더
    public GameObject cookingTimerUI;          // 타이머 UI
    public TextMeshProUGUI cookingTimeText;    // 남은 시간 텍스트
    public Color almostDoneColor = new Color(1f, 0.5f, 0f, 1f); // 거의 완성 색상 (주황색)

    [Header("상태별 스프라이트")]
    public Sprite initialUnpressedSprite;
    public Sprite readyToPressSugarSprite;
    public Sprite readyToPressSeedSprite;
    public Sprite pressedSugarSprite;
    public Sprite pressedSeedSprite;
    public Sprite flippedSugarSprite;
    public Sprite flippedSeedSprite;
    public Sprite cookedSugarSprite;
    public Sprite cookedSeedSprite;
    public Sprite burntSprite;

    void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer == null) Debug.LogError("SpriteRenderer가 없습니다!");

        // UI 초기화
        if (pressGaugeSlider != null) pressGaugeSlider.gameObject.SetActive(false);
        if (resultTextObject != null) resultTextObject.SetActive(false);
        if (flipIndicatorIcon != null) flipIndicatorIcon.SetActive(false);
        if (flipArrowIcon != null) flipArrowIcon.SetActive(false);
        if (cookingTimerUI != null) cookingTimerUI.SetActive(false);
        if (cookingProgressSlider != null) cookingProgressSlider.gameObject.SetActive(false);
    }
    
    public void Initialize(PreparationUI.FillingType fillingType, Sprite startingSprite)
    {
        currentFilling = fillingType;
        initialUnpressedSprite = startingSprite;
        if (spriteRenderer != null) spriteRenderer.sprite = initialUnpressedSprite;
        
        SetupJudgmentZones();
        if (resultTextObject != null) resultTextObject.SetActive(false);
        
        ChangeState(GriddleState.Cooking_Unpressed);
        Debug.Log(currentFilling.ToString() + " 속 호떡(" + gameObject.name + ")이 철판에 놓임. 초기 상태: " + currentState);
    }
    
    private void SetupJudgmentZones()
    {
        if (pressGaugeSlider == null)
        {
            Debug.LogError("SetupJudgmentZones: pressGaugeSlider is NULL. Cannot setup zones.");
            return;
        }

        RectTransform parentRect = pressGaugeSlider.transform.Find("Background")?.GetComponent<RectTransform>();
        if (parentRect == null)
        {
            Debug.LogError("SetupJudgmentZones: Slider's 'Background' child RectTransform not found. Cannot setup zones.");
            return;
        }

        float parentWidth = parentRect.rect.width;
        float parentHeight = parentRect.rect.height;

        if (goodZoneIndicator != null)
        {
            RectTransform goodRect = goodZoneIndicator.GetComponent<RectTransform>();
            Image goodImage = goodZoneIndicator.GetComponent<Image>();

            if (goodRect != null)
            {
                goodRect.anchorMin = new Vector2(goodPressMinThreshold, 0);
                goodRect.anchorMax = new Vector2(perfectPressMinThreshold, 1);
                goodRect.pivot = new Vector2(0.5f, 0.5f); 
                goodRect.offsetMin = Vector2.zero;
                goodRect.offsetMax = Vector2.zero; 
                
                if (goodImage != null)
                {
                    Color goodColor = goodImage.color;
                    goodColor.a = 0.9f;
                    goodImage.color = goodColor;
                    goodImage.raycastTarget = false;
                }
                goodZoneIndicator.SetActive(false);
            }
        }

        if (perfectZoneIndicator != null)
        {
            RectTransform perfectRect = perfectZoneIndicator.GetComponent<RectTransform>();
            Image perfectImage = perfectZoneIndicator.GetComponent<Image>();

            if (perfectRect != null)
            {
                perfectRect.anchorMin = new Vector2(perfectPressMinThreshold, 0);
                perfectRect.anchorMax = new Vector2(perfectPressMaxThreshold, 1);
                perfectRect.pivot = new Vector2(0.5f, 0.5f);
                perfectRect.offsetMin = Vector2.zero;
                perfectRect.offsetMax = Vector2.zero;

                if (perfectImage != null)
                {
                    Color perfectColor = perfectImage.color;
                    perfectColor.a = 0.9f;
                    perfectImage.color = perfectColor;
                    perfectImage.raycastTarget = false;
                }
                perfectZoneIndicator.SetActive(false);
            }
        }
    }

    void Update()
    {
        currentTimer += Time.deltaTime;

        switch (currentState)
        {
            case GriddleState.Cooking_Unpressed:
                if (currentTimer >= timeToBecomeReadyToPress)
                {
                    ChangeState(GriddleState.ReadyToPress);
                }
                break;

            case GriddleState.ReadyToPress:
                if (isHoldingForPress)
                {
                    currentHoldTime += Time.deltaTime;
                    if (pressGaugeSlider != null)
                    {
                        float newValue = Mathf.Clamp01(currentHoldTime / maxHoldTimeToFillGauge);
                        pressGaugeSlider.value = newValue;
                        
                        // 게이지 업데이트 디버깅 (첫 몇 번만)
                        if (Time.frameCount % 30 == 0) // 30프레임마다 로그
                        {
                            Debug.Log("게이지 업데이트: " + newValue + ", 슬라이더 활성상태: " + pressGaugeSlider.gameObject.activeInHierarchy);
                        }
                    }
                    if (currentHoldTime >= maxHoldTimeToFillGauge)
                    {
                        PerformPressAction();
                    }
                }
                else if (currentTimer >= timeToBurnIfActionMissed)
                {
                    ChangeState(GriddleState.Burnt);
                }
                break;

            case GriddleState.Pressed_Cooking:
                if (currentTimer >= timeToBecomeReadyToFlip)
                {
                    ChangeState(GriddleState.ReadyToFlip);
                }
                break;

            case GriddleState.ReadyToFlip:
                if (currentTimer >= timeToBurnIfActionMissed)
                {
                    ChangeState(GriddleState.Burnt);
                }
                break;

            case GriddleState.Flipped_Cooking:
                // 4단계: 후속 타이머 UI 업데이트
                UpdateCookingTimer();
                if (currentTimer >= timeToBecomeCooked)
                {
                    ChangeState(GriddleState.Cooked);
                }
                break;

            case GriddleState.Cooked:
                break;

            case GriddleState.Flipping:
            case GriddleState.Burnt:
                break;
        }

        // 결과 텍스트 타이머 업데이트
        if (resultTextObject != null && resultTextObject.activeInHierarchy)
        {
            resultTextTimer += Time.deltaTime;
            if (resultTextTimer >= resultTextDisplayTime)
            {
                resultTextObject.SetActive(false);
                resultTextTimer = 0f;
            }
        }
    }

    // ====== 4단계: 후속 타이머 UI 업데이트 ======
    private void UpdateCookingTimer()
    {
        if (cookingProgressSlider != null)
        {
            float progress = currentTimer / timeToBecomeCooked;
            cookingProgressSlider.value = progress;

            // 거의 완성되면 색상 변경
            if (progress > 0.8f)
            {
                Image fillImage = cookingProgressSlider.fillRect.GetComponent<Image>();
                if (fillImage != null)
                {
                    fillImage.color = Color.Lerp(Color.green, almostDoneColor, (progress - 0.8f) / 0.2f);
                }
            }
        }

        if (cookingTimeText != null)
        {
            float remainingTime = timeToBecomeCooked - currentTimer;
            cookingTimeText.text = Mathf.Ceil(remainingTime).ToString() + "s";
        }
    }
    
    void OnMouseDown()
    {
        if (currentState == GriddleState.ReadyToPress && !isHoldingForPress)
        {
            isHoldingForPress = true;
            currentHoldTime = 0f;
            Debug.Log("호떡(" + gameObject.name + ") 누르기 시작!");
            
            // UI 상태 재확인
            if (pressGaugeSlider != null)
            {
                Debug.Log("누르기 시작 시 슬라이더 상태: 활성=" + pressGaugeSlider.gameObject.activeInHierarchy + 
                         ", 값=" + pressGaugeSlider.value + ", 위치=" + pressGaugeSlider.transform.position);
            }
        }
        else if (currentState == GriddleState.ReadyToFlip)
        {
            // 2단계: 탭 피드백 효과 (선택사항)
            Vector3 mousePosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            ShowTapFeedback(mousePosition);
            
            StartCoroutine(ImprovedFlipHotteok());
        }
        else if (currentState == GriddleState.Cooked)
        {
            // 🆕 완성된 호떡을 탭했을 때 스택 판매대로 전달
            SendToStackSalesCounter();
        }
    }

    // 2단계: 탭 피드백 효과
    private void ShowTapFeedback(Vector3 position)
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

    void OnMouseUp()
    {
        if (currentState == GriddleState.ReadyToPress && isHoldingForPress)
        {
            PerformPressAction();
        }
    }

    // ====== 3단계: 개선된 뒤집기 애니메이션 ======
    IEnumerator ImprovedFlipHotteok()
    {
        ChangeState(GriddleState.Flipping);

        Vector3 startPosition = transform.position;
        Vector3 startRotation = transform.eulerAngles;
        Vector3 endRotation = startRotation + new Vector3(0, 180, 0);
        
        bool spriteChanged = false;
        float elapsedTime = 0f;

        while (elapsedTime < flipAnimationDuration)
        {
            elapsedTime += Time.deltaTime;
            float normalizedTime = elapsedTime / flipAnimationDuration;
            float curveValue = flipCurve.Evaluate(normalizedTime);

            // 회전 애니메이션
            Vector3 currentRotation = Vector3.Lerp(startRotation, endRotation, curveValue);
            transform.eulerAngles = currentRotation;

            // 높이 애니메이션 (포물선)
            float heightOffset = Mathf.Sin(normalizedTime * Mathf.PI) * flipHeight;
            transform.position = startPosition + Vector3.up * heightOffset;

            // 중간 지점에서 스프라이트 변경
            if (normalizedTime >= 0.5f && !spriteChanged)
            {
                spriteChanged = true;
                if (currentFilling == PreparationUI.FillingType.Sugar)
                    spriteRenderer.sprite = flippedSugarSprite;
                else if (currentFilling == PreparationUI.FillingType.Seed)
                    spriteRenderer.sprite = flippedSeedSprite;
            }

            yield return null;
        }

        // 최종 위치 및 회전 설정
        transform.position = startPosition;
        transform.eulerAngles = endRotation;

        ChangeState(GriddleState.Flipped_Cooking);
    }
    
    public void ChangeState(GriddleState newState)
    {
        GriddleState oldState = currentState;
        currentState = newState;
        currentTimer = 0f;

        // 이전 상태 정리
        if (oldState == GriddleState.ReadyToPress || oldState == GriddleState.Pressing_Holding)
        {
            if (pressGaugeSlider != null) pressGaugeSlider.gameObject.SetActive(false);
            isHoldingForPress = false;
        }
        if (oldState == GriddleState.ReadyToFlip)
        {
            StopFlipIndicator(); // 1단계: 뒤집기 표시 중지
        }

        Debug.Log("호떡 상태 변경: " + oldState + " -> " + newState);

        switch (newState)
        {
            case GriddleState.Cooking_Unpressed:
                if (spriteRenderer != null) spriteRenderer.sprite = initialUnpressedSprite;
                break;

            case GriddleState.ReadyToPress:
                if (spriteRenderer != null)
                {
                    if (currentFilling == PreparationUI.FillingType.Sugar && readyToPressSugarSprite != null)
                        spriteRenderer.sprite = readyToPressSugarSprite;
                    else if (currentFilling == PreparationUI.FillingType.Seed && readyToPressSeedSprite != null)
                        spriteRenderer.sprite = readyToPressSeedSprite;
                    else spriteRenderer.sprite = initialUnpressedSprite;
                }

                currentHoldTime = 0f;
                
                // UI 디버깅 로그 추가
                Debug.Log("=== 누르기 UI 활성화 시도 ===");
                if (pressGaugeSlider != null)
                {
                    pressGaugeSlider.value = 0;
                    pressGaugeSlider.gameObject.SetActive(true);
                    
                    // 더 자세한 UI 상태 디버깅
                    Debug.Log("pressGaugeSlider 활성화됨. 위치: " + pressGaugeSlider.transform.position);
                    
                    Canvas canvas = pressGaugeSlider.GetComponentInParent<Canvas>();
                    Debug.Log("pressGaugeSlider Canvas: " + (canvas != null ? canvas.name + " (" + canvas.renderMode + ")" : "NULL"));
                    Debug.Log("pressGaugeSlider 활성 상태: " + pressGaugeSlider.gameObject.activeInHierarchy);
                    Debug.Log("pressGaugeSlider Scale: " + pressGaugeSlider.transform.localScale);
                    
                    // 부모 오브젝트들 확인
                    Transform parent = pressGaugeSlider.transform.parent;
                    while (parent != null)
                    {
                        Debug.Log("부모: " + parent.name + " 활성상태: " + parent.gameObject.activeInHierarchy);
                        parent = parent.parent;
                    }
                    
                    // RectTransform 정보
                    RectTransform rectTrans = pressGaugeSlider.GetComponent<RectTransform>();
                    if (rectTrans != null)
                    {
                        Debug.Log("RectTransform - anchoredPosition: " + rectTrans.anchoredPosition + 
                                 ", sizeDelta: " + rectTrans.sizeDelta + 
                                 ", anchorMin: " + rectTrans.anchorMin + 
                                 ", anchorMax: " + rectTrans.anchorMax);
                    }
                    
                    // 강제로 호떡 위에 위치시키기 (WorldSpace Canvas 지원)
                    if (canvas != null)
                    {
                        if (canvas.renderMode == RenderMode.ScreenSpaceOverlay)
                        {
                            Debug.Log(">>> 테스트: Screen Space UI를 화면 중앙으로 이동");
                            Vector3 hotteokScreenPos = Camera.main.WorldToScreenPoint(transform.position);
                            pressGaugeSlider.transform.position = new Vector3(hotteokScreenPos.x, hotteokScreenPos.y + 100, 0);
                        }
                        else if (canvas.renderMode == RenderMode.WorldSpace)
                        {
                            Debug.Log(">>> 테스트: WorldSpace UI를 호떡 위로 이동");
                            // WorldSpace Canvas의 경우 월드 좌표로 위치 설정
                            Vector3 hotteokWorldPos = transform.position + Vector3.up * 2.0f; // 호떡 위 2유닛
                            pressGaugeSlider.transform.position = hotteokWorldPos;
                            
                            // 카메라를 향하도록 회전 설정
                            if (Camera.main != null)
                            {
                                pressGaugeSlider.transform.LookAt(Camera.main.transform);
                                pressGaugeSlider.transform.Rotate(0, 180, 0); // 뒤집힌 상태 보정
                            }
                            
                            // 크기 조정 (WorldSpace에서는 작게 보일 수 있음)
                            pressGaugeSlider.transform.localScale = Vector3.one * 0.01f; // 크기 조정
                        }
                        Debug.Log("새로운 위치: " + pressGaugeSlider.transform.position);
                    }
                    
                    if (perfectZoneIndicator != null) 
                    {
                        perfectZoneIndicator.SetActive(true);
                        Debug.Log("perfectZoneIndicator 활성화됨");
                    }
                    if (goodZoneIndicator != null) 
                    {
                        goodZoneIndicator.SetActive(true);
                        Debug.Log("goodZoneIndicator 활성화됨");
                    }
                }
                else
                {
                    Debug.LogError("pressGaugeSlider가 NULL입니다! Inspector에서 연결을 확인하세요.");
                }
                break;
            
            case GriddleState.Pressed_Cooking:
                break;

            case GriddleState.ReadyToFlip:
                StartFlipIndicator(); // 1단계: 뒤집기 표시 시작
                break;

            case GriddleState.Flipping:
                break;

            case GriddleState.Flipped_Cooking:
                StartCookingTimer(); // 4단계: 후속 타이머 시작
                break;

            case GriddleState.Cooked:
                CompleteCooking(); // 완성 처리
                break;

            case GriddleState.Burnt:
                HandleBurnt(); // 탄 상태 처리
                break;
        }
    }

    // ====== 1단계: 뒤집기 시각적 신호 시작 ======
    private void StartFlipIndicator()
    {
        isFlipIndicatorActive = true;
        
        if (flipIndicatorIcon != null)
        {
            flipIndicatorIcon.SetActive(true);
        }
        
        if (flipArrowIcon != null)
        {
            flipArrowIcon.SetActive(true);
        }

        // 깜빡임 효과 시작
        if (flipIndicatorCoroutine != null)
            StopCoroutine(flipIndicatorCoroutine);
        flipIndicatorCoroutine = StartCoroutine(FlipIndicatorBlink());

        // 호떡 색상 변경으로 준비 상태 표시
        if (spriteRenderer != null)
        {
            spriteRenderer.color = Color.Lerp(Color.white, readyToFlipColor, 0.3f);
        }

        Debug.Log("뒤집기 준비 완료! 탭하여 뒤집으세요!");
    }

    // 1단계: 깜빡임 효과 코루틴
    private IEnumerator FlipIndicatorBlink()
    {
        while (isFlipIndicatorActive)
        {
            // 아이콘 깜빡임
            if (flipIndicatorIcon != null)
            {
                flipIndicatorIcon.SetActive(!flipIndicatorIcon.activeInHierarchy);
            }
            
            // 화살표 회전 애니메이션
            if (flipArrowIcon != null)
            {
                flipArrowIcon.transform.Rotate(0, 0, 180 * Time.deltaTime * iconBlinkSpeed);
            }

            yield return new WaitForSeconds(1f / iconBlinkSpeed);
        }
    }

    // 1단계: 뒤집기 표시 중지
    private void StopFlipIndicator()
    {
        isFlipIndicatorActive = false;
        
        if (flipIndicatorCoroutine != null)
        {
            StopCoroutine(flipIndicatorCoroutine);
            flipIndicatorCoroutine = null;
        }

        if (flipIndicatorIcon != null) flipIndicatorIcon.SetActive(false);
        if (flipArrowIcon != null) flipArrowIcon.SetActive(false);

        // 색상 원복
        if (spriteRenderer != null)
        {
            spriteRenderer.color = Color.white;
        }
    }

    // ====== 4단계: 후속 타이머 시작 ======
    private void StartCookingTimer()
    {
        if (cookingTimerUI != null)
        {
            cookingTimerUI.SetActive(true);
        }

        if (cookingProgressSlider != null)
        {
            cookingProgressSlider.gameObject.SetActive(true);
            cookingProgressSlider.value = 0f;
            
            // 초기 색상 설정
            Image fillImage = cookingProgressSlider.fillRect.GetComponent<Image>();
            if (fillImage != null)
            {
                fillImage.color = Color.green;
            }
        }

        Debug.Log("뒤집기 완료! 이제 " + timeToBecomeCooked + "초 후에 완성됩니다.");
    }

    private void CompleteCooking()
    {
        Debug.Log("호떡 완성!");
        
        // 타이머 UI 숨기기
        if (cookingTimerUI != null) cookingTimerUI.SetActive(false);
        if (cookingProgressSlider != null) cookingProgressSlider.gameObject.SetActive(false);

        // 완성 스프라이트 설정
        if (currentFilling == PreparationUI.FillingType.Sugar)
            spriteRenderer.sprite = cookedSugarSprite;
        else if (currentFilling == PreparationUI.FillingType.Seed)
            spriteRenderer.sprite = cookedSeedSprite;

        // 완성 효과
        ShowCompletionEffect();
        
        // 판매대로 전달 안내 표시
        ShowDeliveryGuide();
    }

    /// <summary>
    /// 🆕 완성된 호떡을 스택 판매대로 전달
    /// </summary>
    private void SendToStackSalesCounter()
    {
        if (currentState != GriddleState.Cooked)
        {
            Debug.Log("완성되지 않은 호떡은 스택 판매대로 보낼 수 없습니다!");
            return;
        }

        if (StackSalesCounter.Instance == null)
        {
            Debug.LogError("StackSalesCounter가 씬에 없습니다! 스택 판매대를 설정해주세요.");
            return;
        }

        // 스택 판매대에 추가 가능한지 확인
        if (!StackSalesCounter.Instance.CanAddHotteokToStack(currentFilling))
        {
            Debug.Log(currentFilling + " 스택이 가득참! 호떡을 보낼 수 없습니다.");
            ShowStackFullWarning();
            return;
        }

        Debug.Log("완성된 " + currentFilling + " 호떡을 스택 판매대로 전달!");

        // 철판 슬롯 비우기 (GriddleSlot에서 관리)
        GriddleSlot parentSlot = GetComponentInParent<GriddleSlot>();
        if (parentSlot == null)
        {
            // 부모에서 찾지 못했다면 다른 방법으로 찾기
            Transform current = transform.parent;
            while (current != null && parentSlot == null)
            {
                parentSlot = current.GetComponent<GriddleSlot>();
                current = current.parent;
            }
        }

        // 스택 판매대로 호떡 전달
        StackSalesCounter.Instance.AddHotteokToStack(gameObject, currentFilling);

        // 철판 슬롯을 비움 (MakeSlotEmpty 호출)
        if (parentSlot != null)
        {
            parentSlot.MakeSlotEmpty();
        }
        else
        {
            Debug.LogWarning("GriddleSlot을 찾을 수 없어서 수동으로 슬롯을 정리합니다.");
            // GriddleSlot을 찾지 못한 경우를 대비한 대안
            // 이 경우 StackSalesCounter에서 호떡을 가져간 후 이 오브젝트는 비활성화됨
        }
    }

    /// <summary>
    /// 🆕 스택 판매대로 전달 안내 표시
    /// </summary>
    private void ShowDeliveryGuide()
    {
        Debug.Log("🎉 호떡 완성! 탭하여 스택 판매대로 보내세요!");
        
        // 완성된 호떡 위에 안내 텍스트나 아이콘 표시 (선택사항)
        // 예: "TAP TO STACK" 텍스트나 위쪽 화살표 아이콘
    }

    /// <summary>
    /// 🆕 스택 가득참 경고 표시
    /// </summary>
    private void ShowStackFullWarning()
    {
        Debug.Log("⚠️ " + currentFilling + " 스택이 가득찼습니다! 호떡을 손님에게 판매하세요!");
        
        // UI 경고 표시 (빨간색 깜빡임 등)
        if (spriteRenderer != null)
        {
            StartCoroutine(BlinkWarning());
        }
    }

    /// <summary>
    /// 🆕 경고 깜빡임 효과
    /// </summary>
    private IEnumerator BlinkWarning()
    {
        Color originalColor = spriteRenderer.color;
        Color warningColor = Color.red;
        
        for (int i = 0; i < 3; i++) // 3번 깜빡임
        {
            spriteRenderer.color = warningColor;
            yield return new WaitForSeconds(0.2f);
            spriteRenderer.color = originalColor;
            yield return new WaitForSeconds(0.2f);
        }
    }

    private void HandleBurnt()
    {
        Debug.Log("타버렸습니다... ㅠㅠ");
        
        // 모든 UI 숨기기
        if (pressGaugeSlider != null) pressGaugeSlider.gameObject.SetActive(false);
        if (cookingTimerUI != null) cookingTimerUI.SetActive(false);
        if (cookingProgressSlider != null) cookingProgressSlider.gameObject.SetActive(false);
        StopFlipIndicator();

        // 탄 스프라이트 설정
        if (spriteRenderer != null && burntSprite != null)
        {
            spriteRenderer.sprite = burntSprite;
        }
    }

    private void ShowCompletionEffect()
    {
        // 완성 효과 구현 (파티클, 사운드 등)
        Debug.Log("🎉 호떡 완성 축하 효과!");
    }

    void PerformPressAction()
    {
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
        Debug.Log("호떡(" + gameObject.name + ") 누르기 결과: " + resultString + " (게이지: " + pressQuality.ToString("F2") + ")");
        
        ShowPressResult(resultString, resultColor);
        ApplyPressResultEffects(pressResult);

        if (spriteRenderer != null)
        {
            if (currentFilling == PreparationUI.FillingType.Sugar && pressedSugarSprite != null)
                spriteRenderer.sprite = pressedSugarSprite;
            else if (currentFilling == PreparationUI.FillingType.Seed && pressedSeedSprite != null)
                spriteRenderer.sprite = pressedSeedSprite;
            else Debug.LogWarning("눌린 상태에 대한 적절한 스프라이트가 없습니다.");
        }
        ChangeState(GriddleState.Pressed_Cooking);
    }

    private void ShowPressResult(string result, Color color)
    {
        if (resultTextObject != null && resultText != null)
        {
            resultText.text = result;
            resultText.color = color;
            resultTextObject.SetActive(true);
            resultTextTimer = 0f;
            
            RectTransform resultRect = resultTextObject.GetComponent<RectTransform>();
            if (resultRect != null)
            {
                Canvas canvas = resultTextObject.GetComponentInParent<Canvas>();
                if (canvas != null && canvas.renderMode == RenderMode.WorldSpace)
                {
                    resultRect.position = new Vector3(
                        transform.position.x,
                        transform.position.y + 0.7f,
                        transform.position.z - 0.1f
                    );
                }
            }
            
            Animation anim = resultTextObject.GetComponent<Animation>();
            if (anim != null)
            {
                anim.Stop();
                anim.Play();
            }
        }
    }

    private void ApplyPressResultEffects(PressQualityResult result)
    {
        float originalTime = timeToBecomeReadyToFlip;
        
        switch (result)
        {
            case PressQualityResult.Perfect:
                timeToBecomeReadyToFlip *= 0.7f;
                Debug.Log("PERFECT! 뒤집기까지 시간 30% 단축! (" + originalTime + "초 -> " + timeToBecomeReadyToFlip + "초)");
                break;
                
            case PressQualityResult.Good:
                timeToBecomeReadyToFlip *= 0.85f;
                Debug.Log("GOOD! 뒤집기까지 시간 15% 단축! (" + originalTime + "초 -> " + timeToBecomeReadyToFlip + "초)");
                break;
                
            case PressQualityResult.Miss:
                Debug.Log("MISS! 기본 쿠킹 시간 유지: " + timeToBecomeReadyToFlip + "초");
                break;
        }
    }
}