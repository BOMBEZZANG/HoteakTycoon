// Assets/Scripts/Gridle/HotteokOnGriddle.cs
// 🔥 탄 호떡 제거 기능 추가 버전

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
    public AnimationCurve flipCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
    public Vector3 flipRotationAxis = Vector3.forward;
    public float flipHeight = 0.3f;
    
    [Header("후속 타이머 UI")]
    public Slider cookingProgressSlider;
    public GameObject cookingTimerUI;
    public TextMeshProUGUI cookingTimeText;
    public Color almostDoneColor = new Color(1f, 0.5f, 0f, 1f);

    [Header("🔥 탄 호떡 처리 설정")]
    public AudioClip burntRemovalSound;         // 탄 호떡 제거 소리
    public GameObject burntRemovalEffect;       // 탄 호떡 제거 이펙트
    public float burntRemovalAnimationTime = 0.5f; // 제거 애니메이션 시간

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
    
    /// <summary>
    /// Initialize 함수가 GriddleSlot 참조를 받도록 수정
    /// </summary>
    public void Initialize(PreparationUI.FillingType fillingType, Sprite startingSprite, GriddleSlot owner)
    {
        currentFilling = fillingType;
        initialUnpressedSprite = startingSprite;
        ownerGriddleSlot = owner; // 참조 저장
        
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
                        pressGaugeSlider.value = Mathf.Clamp01(currentHoldTime / maxHoldTimeToFillGauge);
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

    private void UpdateCookingTimer()
    {
        if (cookingProgressSlider != null)
        {
            float progress = currentTimer / timeToBecomeCooked;
            cookingProgressSlider.value = progress;

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
        }
        else if (currentState == GriddleState.ReadyToFlip)
        {
            Vector3 mousePosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            ShowTapFeedback(mousePosition);
            
            StartCoroutine(ImprovedFlipHotteok());
        }
        else if (currentState == GriddleState.Cooked)
        {
            SendToStackSalesCounter();
        }
        else if (currentState == GriddleState.Burnt)
        {
            // 🔥 탄 호떡 클릭 시 제거
            RemoveBurntHotteok();
        }
    }

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

            Vector3 currentRotation = Vector3.Lerp(startRotation, endRotation, curveValue);
            transform.eulerAngles = currentRotation;

            float heightOffset = Mathf.Sin(normalizedTime * Mathf.PI) * flipHeight;
            transform.position = startPosition + Vector3.up * heightOffset;

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

        transform.position = startPosition;
        transform.eulerAngles = endRotation;

        ChangeState(GriddleState.Flipped_Cooking);
    }
    
    public void ChangeState(GriddleState newState)
    {
        GriddleState oldState = currentState;
        currentState = newState;
        currentTimer = 0f;

        if (oldState == GriddleState.ReadyToPress || oldState == GriddleState.Pressing_Holding)
        {
            if (pressGaugeSlider != null) pressGaugeSlider.gameObject.SetActive(false);
            isHoldingForPress = false;
        }
        if (oldState == GriddleState.ReadyToFlip)
        {
            StopFlipIndicator();
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
                
                if (pressGaugeSlider != null)
                {
                    pressGaugeSlider.gameObject.SetActive(true);
                    pressGaugeSlider.value = 0;

                    // UI 위치 계산 단순화 (ScreenSpace-Overlay 캔버스 기준)
                    Vector3 hotteokScreenPosition = Camera.main.WorldToScreenPoint(transform.position);
                    pressGaugeSlider.transform.position = hotteokScreenPosition + new Vector3(0, 80, 0);

                    if (perfectZoneIndicator != null) perfectZoneIndicator.SetActive(true);
                    if (goodZoneIndicator != null) goodZoneIndicator.SetActive(true);
                }
                else
                {
                    Debug.LogError("pressGaugeSlider가 NULL입니다! Inspector에서 연결을 확인하세요.");
                }
                break;
            
            case GriddleState.Pressed_Cooking:
                break;

            case GriddleState.ReadyToFlip:
                StartFlipIndicator();
                break;

            case GriddleState.Flipping:
                break;

            case GriddleState.Flipped_Cooking:
                StartCookingTimer();
                break;

            case GriddleState.Cooked:
                CompleteCooking();
                break;

            case GriddleState.Burnt:
                HandleBurnt();
                break;
        }
    }

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

        if (flipIndicatorCoroutine != null)
            StopCoroutine(flipIndicatorCoroutine);
        flipIndicatorCoroutine = StartCoroutine(FlipIndicatorBlink());

        if (spriteRenderer != null)
        {
            spriteRenderer.color = Color.Lerp(Color.white, readyToFlipColor, 0.3f);
        }

        Debug.Log("뒤집기 준비 완료! 탭하여 뒤집으세요!");
    }

    private IEnumerator FlipIndicatorBlink()
    {
        while (isFlipIndicatorActive)
        {
            if (flipIndicatorIcon != null)
            {
                flipIndicatorIcon.SetActive(!flipIndicatorIcon.activeInHierarchy);
            }
            
            if (flipArrowIcon != null)
            {
                flipArrowIcon.transform.Rotate(0, 0, 180 * Time.deltaTime * iconBlinkSpeed);
            }

            yield return new WaitForSeconds(1f / iconBlinkSpeed);
        }
    }

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

        if (spriteRenderer != null)
        {
            spriteRenderer.color = Color.white;
        }
    }

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
        
        if (cookingTimerUI != null) cookingTimerUI.SetActive(false);
        if (cookingProgressSlider != null) cookingProgressSlider.gameObject.SetActive(false);

        if (currentFilling == PreparationUI.FillingType.Sugar)
            spriteRenderer.sprite = cookedSugarSprite;
        else if (currentFilling == PreparationUI.FillingType.Seed)
            spriteRenderer.sprite = cookedSeedSprite;

        ShowCompletionEffect();
        ShowDeliveryGuide();
    }

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

        if (!StackSalesCounter.Instance.CanAddHotteokToStack(currentFilling))
        {
            Debug.Log(currentFilling + " 스택이 가득참! 호떡을 보낼 수 없습니다.");
            ShowStackFullWarning();
            return;
        }

        Debug.Log("완성된 " + currentFilling + " 호떡을 스택 판매대로 전달!");

        StackSalesCounter.Instance.AddHotteokToStack(gameObject, currentFilling);

        // 저장해둔 참조를 사용하여 슬롯을 비움
        if (ownerGriddleSlot != null)
        {
            ownerGriddleSlot.MakeSlotEmpty();
        }
        else
        {
            Debug.LogError("ownerGriddleSlot이 지정되지 않았습니다!");
        }
    }

    /// <summary>
    /// 🔥 탄 호떡 제거 처리
    /// </summary>
    private void RemoveBurntHotteok()
    {
        Debug.Log("🔥 탄 호떡을 제거합니다!");

        // 제거 이펙트 표시
        ShowBurntRemovalEffects();

        // 제거 애니메이션 시작
        StartCoroutine(BurntRemovalAnimation());
    }

    /// <summary>
    /// 🔥 탄 호떡 제거 이펙트
    /// </summary>
    private void ShowBurntRemovalEffects()
    {
        // 제거 소리
        if (burntRemovalSound != null)
        {
            AudioSource.PlayClipAtPoint(burntRemovalSound, transform.position);
        }

        // 제거 이펙트
        if (burntRemovalEffect != null)
        {
            GameObject effect = Instantiate(burntRemovalEffect, transform.position, Quaternion.identity);
            Destroy(effect, 2f);
        }
    }

    /// <summary>
    /// 🔥 탄 호떡 제거 애니메이션
    /// </summary>
    IEnumerator BurntRemovalAnimation()
    {
        Vector3 startPosition = transform.position;
        Vector3 startScale = transform.localScale;
        Color startColor = spriteRenderer.color;

        float elapsedTime = 0f;

        while (elapsedTime < burntRemovalAnimationTime)
        {
            elapsedTime += Time.deltaTime;
            float t = elapsedTime / burntRemovalAnimationTime;

            // 점점 작아지면서 투명해짐
            transform.localScale = Vector3.Lerp(startScale, Vector3.zero, t);
            
            // 색상 페이드 아웃
            Color currentColor = Color.Lerp(startColor, Color.clear, t);
            spriteRenderer.color = currentColor;

            // 살짝 위로 올라가는 효과
            transform.position = startPosition + Vector3.up * (t * 0.5f);

            yield return null;
        }

        // 완전히 제거
        CompleteBurntRemoval();
    }

    /// <summary>
    /// 🔥 탄 호떡 제거 완료
    /// </summary>
    private void CompleteBurntRemoval()
    {
        Debug.Log("🔥 탄 호떡 제거 완료!");

        // 그리들 슬롯 비우기
        if (ownerGriddleSlot != null)
        {
            ownerGriddleSlot.MakeSlotEmpty();
        }
        else
        {
            Debug.LogError("ownerGriddleSlot이 지정되지 않았습니다!");
        }

        // 호떡 오브젝트 제거
        Destroy(gameObject);
    }

    private void ShowDeliveryGuide()
    {
        Debug.Log("🎉 호떡 완성! 탭하여 스택 판매대로 보내세요!");
    }

    private void ShowStackFullWarning()
    {
        Debug.Log("⚠️ " + currentFilling + " 스택이 가득찼습니다! 호떡을 손님에게 판매하세요!");
        
        if (spriteRenderer != null)
        {
            StartCoroutine(BlinkWarning());
        }
    }

    private IEnumerator BlinkWarning()
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

    private void HandleBurnt()
    {
        Debug.Log("🔥 타버렸습니다... 클릭하여 제거하세요!");
        
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

        // 🔥 탄 호떡임을 시각적으로 표시 (깜빡임 효과)
        StartCoroutine(BurntBlinkEffect());
    }

    /// <summary>
    /// 🔥 탄 호떡 깜빡임 효과 (클릭 가능함을 알림)
    /// </summary>
    IEnumerator BurntBlinkEffect()
    {
        if (spriteRenderer == null) yield break;

        Color originalColor = spriteRenderer.color;
        Color blinkColor = Color.red;

        while (currentState == GriddleState.Burnt)
        {
            // 빨간색으로 깜빡임
            spriteRenderer.color = Color.Lerp(originalColor, blinkColor, 0.7f);
            yield return new WaitForSeconds(0.5f);

            if (currentState != GriddleState.Burnt) break;

            spriteRenderer.color = originalColor;
            yield return new WaitForSeconds(0.5f);
        }

        spriteRenderer.color = originalColor;
    }

    private void ShowCompletionEffect()
    {
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