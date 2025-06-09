// HotteokOnGriddle.cs
using UnityEngine;
using UnityEngine.UI; // Slider와 Text를 사용하기 위해 추가
using TMPro; // TextMeshPro를 사용하려면 추가 (없으면 Text 컴포넌트 사용)

public class HotteokOnGriddle : MonoBehaviour
{
    public enum GriddleState
    {
        Cooking_Unpressed,      // 1. 초기 익는 중
        ReadyToPress,         // 2. 누르기 대기 (게이지 나타남)
        Pressing_Holding,     // 2a. (선택적 상태) 현재 누르고 있는 중 (게이지 차오름) - 현재 Update에서 isHoldingForPress로 관리
        Pressed_Cooking,      // 3. 눌린 후 익는 중
        ReadyToFlip,          // 4. 뒤집기 대기
        Flipped_Cooking,      // 5. 뒤집힌 후 익는 중
        Cooked,               // 6. 완성
        Burnt                 // 7. 탐
    }

    // 판정 결과 열거형
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

    [Header("시간 설정 (Inspector에서 조절)")]
    public float timeToBecomeReadyToPress = 4.0f;    // 눌리기 대기까지
    public float timeToBecomeReadyToFlip = 5.0f;     // 눌린 후 뒤집기 대기까지
    public float timeToBurnIfActionMissed = 5.0f;    // ReadyToPress 또는 ReadyToFlip 상태에서 너무 오래 방치 시 타는 시간

    private float currentTimer = 0.0f;
    private SpriteRenderer spriteRenderer;

    [Header("홀드 앤 릴리즈 누르기 설정")]
    public Slider pressGaugeSlider;            // 누르기 게이지 (Inspector에서 연결)
    public float maxHoldTimeToFillGauge = 1.5f; // 게이지가 0에서 1까지 차는데 걸리는 시간
    public float perfectPressMinThreshold = 0.8f; // Perfect 판정 최소값 (게이지 값 기준, 0.0 ~ 1.0)
    public float perfectPressMaxThreshold = 1.0f; // Perfect 판정 최대값
    public float goodPressMinThreshold = 0.5f;    // Good 판정 최소값
    private float currentHoldTime = 0.0f;
    private bool isHoldingForPress = false;

    [Header("판정 영역 표시")]
    public GameObject perfectZoneIndicator;    // PERFECT 영역 표시 오브젝트
    public GameObject goodZoneIndicator;       // GOOD 영역 표시 오브젝트

    [Header("판정 결과 UI")]
    public GameObject resultTextObject;        // 판정 결과 텍스트를 표시할 UI 오브젝트
    public Text resultText;                   // 일반 Text 컴포넌트 (또는 TextMeshProUGUI를 사용)
    public float resultTextDisplayTime = 1.5f; // 결과 텍스트 표시 시간
    private float resultTextTimer = 0f;

    [Header("상태별 스프라이트 (Inspector에서 연결)")]
    public Sprite initialUnpressedSprite;
    public Sprite readyToPressSugarSprite;
    public Sprite readyToPressSeedSprite;
    public Sprite pressedSugarSprite;
    public Sprite pressedSeedSprite;
    public Sprite burntSprite;  // 탄 호떡 스프라이트 추가

    void Awake()
    {
        // 맨 첫 줄에 이 로그를 추가해보세요. Awake가 호출되는지 자체를 보기 위함입니다.
        Debug.Log("--- HotteokOnGriddle Awake() START for " + gameObject.name + " ---");

        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer == null) Debug.LogError("HotteokOnGriddle(" + gameObject.name + "): SpriteRenderer가 없습니다!");

        if (pressGaugeSlider != null)
        {
            Canvas parentCanvas = pressGaugeSlider.GetComponentInParent<Canvas>();
            if (parentCanvas != null)
            {
                Debug.Log("Hotteok [" + gameObject.name + "/Awake] Found parent canvas: " + parentCanvas.name + ", RenderMode: " + parentCanvas.renderMode + ", Initial WorldCamera: " + parentCanvas.worldCamera?.name);

                if (parentCanvas.renderMode == RenderMode.WorldSpace)
                {
                    if (Camera.main != null)
                    {
                        parentCanvas.worldCamera = Camera.main;
                        Debug.Log("Hotteok [" + gameObject.name + "/Awake] Assigned Camera.main (" + Camera.main.name + ") to " + parentCanvas.name + ". New WorldCamera: " + parentCanvas.worldCamera?.name);
                    }
                    else
                    {
                        Debug.LogError("Hotteok [" + gameObject.name + "/Awake] Camera.main is NULL. Cannot assign Event Camera!");
                    }
                }
            }
            else
            {
                Debug.LogError("Hotteok [" + gameObject.name + "/Awake] Could not find parent Canvas for pressGaugeSlider!");
            }

            pressGaugeSlider.gameObject.SetActive(false);
            pressGaugeSlider.minValue = 0;
            pressGaugeSlider.maxValue = 1;
        }
        else
        {
            Debug.LogWarning("Hotteok [" + gameObject.name + "/Awake] pressGaugeSlider is NULL in Awake!");
        }

        // 결과 텍스트 초기화
        if (resultTextObject != null)
        {
            resultTextObject.SetActive(false);
        }

        Debug.Log("--- HotteokOnGriddle Awake() END for " + gameObject.name + " ---");
    }

    // Initialize method with gauge zone setup
    public void Initialize(PreparationUI.FillingType fillingType, Sprite startingSprite)
    {
        currentFilling = fillingType;
        initialUnpressedSprite = startingSprite;
        if (spriteRenderer != null) spriteRenderer.sprite = initialUnpressedSprite;
        
        // Set up the pressure gauge slider properly for World Space canvas
        if (pressGaugeSlider != null)
        {
            Canvas canvas = pressGaugeSlider.GetComponentInParent<Canvas>();
            if (canvas != null && canvas.renderMode == RenderMode.WorldSpace)
            {
                // Ensure the canvas has a reference to the camera
                canvas.worldCamera = Camera.main;
                
                // Position the canvas/slider in 3D space - adjust these values as needed
                RectTransform canvasRect = canvas.GetComponent<RectTransform>();
                if (canvasRect != null)
                {
                    // Make sure the canvas is positioned correctly relative to the hotteok
                    canvasRect.position = new Vector3(
                        transform.position.x,
                        transform.position.y + 0.5f, // Position above the hotteok
                        transform.position.z - 0.1f  // Slightly in front for visibility
                    );
                    
                    // Scale the canvas appropriately for world space
                    canvasRect.localScale = new Vector3(0.01f, 0.01f, 0.01f); // Adjust as needed
                }
                
                // Set proper sorting layer for the canvas
                canvas.sortingLayerName = "UI"; // Make sure this layer exists
                canvas.sortingOrder = 5; // Adjust as needed
            }
        }
        
        // 판정 영역 설정
        SetupUIHierarchy();
    
        // 판정 영역 위치 및 크기 설정
        SetupJudgmentZones();
        // 결과 텍스트 초기화
        if (resultTextObject != null)
        {
            resultTextObject.SetActive(false);
        }
        
        ChangeState(GriddleState.Cooking_Unpressed); // Start the first state and timer
        Debug.Log(currentFilling.ToString() + " 속 호떡(" + gameObject.name + ")이 철판에 놓임. 초기 상태: " + currentState);
    }

    // 판정 영역 설정 함수
    private void SetupJudgmentZones()
    {
        if (pressGaugeSlider == null)
        {
            Debug.LogError("SetupJudgmentZones: pressGaugeSlider is NULL. Cannot setup zones.");
            return;
        }

        // 슬라이더의 Background RectTransform을 기준으로 삼음
        RectTransform parentRect = pressGaugeSlider.transform.Find("Background")?.GetComponent<RectTransform>();
        if (parentRect == null)
        {
            Debug.LogError("SetupJudgmentZones: Slider's 'Background' child RectTransform not found. Cannot setup zones.");
            return;
        }

        float parentWidth = parentRect.rect.width;
        float parentHeight = parentRect.rect.height; // 판정 영역 높이를 부모(Background)와 동일하게 설정

        // GOOD 존 설정
        if (goodZoneIndicator != null)
        {
            RectTransform goodRect = goodZoneIndicator.GetComponent<RectTransform>();
            Image goodImage = goodZoneIndicator.GetComponent<Image>(); // 색상 및 알파 조절용

            if (goodRect != null)
            {
                // 부모(Background)의 좌측 하단을 (0,0), 우측 상단을 (1,1)로 하는 앵커 설정
                goodRect.anchorMin = new Vector2(goodPressMinThreshold, 0);
                goodRect.anchorMax = new Vector2(perfectPressMinThreshold, 1);
                goodRect.pivot = new Vector2(0.5f, 0.5f); // 중앙 피벗

                // 앵커가 부모의 특정 비율에 맞춰 늘어나므로, offset으로 크기와 위치를 0으로 맞춰줌
                goodRect.offsetMin = Vector2.zero; // anchoredPosition, sizeDelta 대신 사용
                goodRect.offsetMax = Vector2.zero; // anchoredPosition, sizeDelta 대신 사용
                
                if (goodImage != null)
                {
                    Color goodColor = goodImage.color;
                    goodColor.a = 0.9f; // 90% 불투명도
                    goodImage.color = goodColor;
                    goodImage.raycastTarget = false;
                }
                goodZoneIndicator.SetActive(false); // 초기에는 숨김
            }
        }

        // PERFECT 존 설정
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
        Debug.Log("판정 영역 UI 설정 적용됨: GOOD(" + goodPressMinThreshold + "~" + perfectPressMinThreshold + "), PERFECT(" + perfectPressMinThreshold + "~" + perfectPressMaxThreshold + ")");
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
                else
                {
                    if (currentTimer >= timeToBurnIfActionMissed)
                    {
                        Debug.Log("ReadyToPress 상태에서 너무 오래 방치되어 타버렸습니다!");
                        ChangeState(GriddleState.Burnt);
                    }
                }
                
                // 게이지 위치 업데이트 (필요한 경우)
                UpdateGaugePosition();
                break;

            case GriddleState.Pressed_Cooking:
                if (currentTimer >= timeToBecomeReadyToFlip)
                {
                    ChangeState(GriddleState.ReadyToFlip);
                }
                break;

            case GriddleState.Burnt:
                enabled = false;
                break;
        }

        // 결과 텍스트 표시 타이머 관리
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

    // 게이지 위치 업데이트 함수
    private void UpdateGaugePosition()
    {
        if (pressGaugeSlider != null && pressGaugeSlider.gameObject.activeInHierarchy)
        {
            Canvas canvas = pressGaugeSlider.GetComponentInParent<Canvas>();
            if (canvas != null && canvas.renderMode == RenderMode.WorldSpace)
            {
                RectTransform canvasRect = canvas.GetComponent<RectTransform>();
                if (canvasRect != null)
                {
                    canvasRect.position = new Vector3(
                        transform.position.x,
                        transform.position.y + 0.5f,  // 호떡 위에 위치
                        transform.position.z - 0.1f   // 앞쪽에 위치 (보이도록)
                    );
                }
            }
        }
    }
    
    private void SetupUIHierarchy()
    {
        if (pressGaugeSlider == null) return;
        
        // 슬라이더의 배경 Transform 찾기
        Transform sliderBackground = pressGaugeSlider.transform.Find("Background");
        if (sliderBackground == null) return;
        
        // PerfectZone과 GoodZone의 부모를 Background로 설정
        if (perfectZoneIndicator != null)
        {
            perfectZoneIndicator.transform.SetParent(sliderBackground, false);
        }
        
        if (goodZoneIndicator != null)
        {
            goodZoneIndicator.transform.SetParent(sliderBackground, false);
        }
        
        Debug.Log("판정 영역 UI 계층 구조 설정 완료");
    }

    public void ChangeState(GriddleState newState)
    {
        GriddleState oldState = currentState;
        currentState = newState;
        currentTimer = 0f;

        Debug.Log("호떡 (" + gameObject.name + ", " + currentFilling.ToString() + ") 상태 변경: " + oldState + " -> " + newState);

        if (oldState == GriddleState.ReadyToPress || oldState == GriddleState.Pressing_Holding)
        {
            if (pressGaugeSlider != null) pressGaugeSlider.gameObject.SetActive(false);
            isHoldingForPress = false;
        }

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
                if (pressGaugeSlider != null)
                {
                    pressGaugeSlider.value = 0;
                    pressGaugeSlider.gameObject.SetActive(true); // 게이지 보이기
                    Debug.Log("Hotteok [" + gameObject.name + "] GaugeSlider.gameObject.SetActive(true) 호출됨.");

                    // 판정 영역도 표시
                    if (perfectZoneIndicator != null) perfectZoneIndicator.SetActive(true);
                    if (goodZoneIndicator != null) goodZoneIndicator.SetActive(true);
                }
                else
                {
                    Debug.LogError("CRITICAL: Hotteok [" + gameObject.name + "] pressGaugeSlider is NULL when trying to activate it in ReadyToPress state!");
                }
                break;

            case GriddleState.Pressed_Cooking:
                // PerformPressAction에서 스프라이트 변경
                break;

            case GriddleState.ReadyToFlip:
                Debug.Log("이제 뒤집을 시간입니다! (다음 단계에서 구현)");
                break;

            case GriddleState.Burnt:
                Debug.Log("타버렸습니다... ㅠㅠ");
                if (pressGaugeSlider != null) pressGaugeSlider.gameObject.SetActive(false);
                // 탄 스프라이트로 변경
                if (spriteRenderer != null && burntSprite != null)
                {
                    spriteRenderer.sprite = burntSprite;
                }
                break;
        }
    }

    void OnMouseDown()
    {
        if (currentState == GriddleState.ReadyToPress && !isHoldingForPress)
        {
            isHoldingForPress = true;
            currentHoldTime = 0f;
            Debug.Log("호떡(" + gameObject.name + ") 누르기 시작! 홀드 중...");
        }
    }

    void OnMouseUp()
    {
        if (currentState == GriddleState.ReadyToPress && isHoldingForPress)
        {
            PerformPressAction();
        }
    }

    void PerformPressAction()
    {
        isHoldingForPress = false;

        float pressQuality = (pressGaugeSlider != null) ? pressGaugeSlider.value : currentHoldTime / maxHoldTimeToFillGauge;
        pressQuality = Mathf.Clamp01(pressQuality);

        // 판정 결과 결정 (수정된 로직)
        PressQualityResult pressResult = PressQualityResult.Miss;
        string resultString = "Miss";
        Color resultColor = Color.red;
        
        // 판정 로직 수정: perfectPressMinThreshold와 perfectPressMaxThreshold 사이의 값에 대해서만 Perfect 판정
        if (pressQuality >= perfectPressMinThreshold && pressQuality <= perfectPressMaxThreshold)
        {
            pressResult = PressQualityResult.Perfect;
            resultString = "PERFECT!";
            resultColor = new Color(1f, 0.8f, 0f); // 금색
        }
        // 명확하게 goodPressMinThreshold 이상이고 perfectPressMinThreshold 미만일 때만 Good 판정
        else if (pressQuality >= goodPressMinThreshold && pressQuality < perfectPressMinThreshold)
        {
            pressResult = PressQualityResult.Good;
            resultString = "GOOD!";
            resultColor = new Color(0f, 0.8f, 0.2f); // 녹색
        }
        // 그 외 경우는 Miss (이미 설정됨)
        
        // 결과 저장 및 디버그 로그
        lastPressResult = pressResult;
        Debug.Log("호떡(" + gameObject.name + ") 누르기 결과: " + resultString + " (게이지: " + pressQuality.ToString("F2") + ")");
        
        // 결과 텍스트 표시
        ShowPressResult(resultString, resultColor);
        
        // 판정 결과에 따른 추가 효과 적용
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

    // 결과 텍스트 표시 함수
    private void ShowPressResult(string result, Color color)
    {
        if (resultTextObject != null && resultText != null)
        {
            resultText.text = result;
            resultText.color = color;
            resultTextObject.SetActive(true);
            resultTextTimer = 0f;
            
            // 결과 텍스트 위치 조정 (호떡 위에 표시)
            RectTransform resultRect = resultTextObject.GetComponent<RectTransform>();
            if (resultRect != null)
            {
                Canvas canvas = resultTextObject.GetComponentInParent<Canvas>();
                if (canvas != null && canvas.renderMode == RenderMode.WorldSpace)
                {
                    resultRect.position = new Vector3(
                        transform.position.x,
                        transform.position.y + 0.7f,  // 호떡 위에 표시
                        transform.position.z - 0.1f   // 앞쪽에 표시
                    );
                }
            }
            
            // 결과 텍스트에 애니메이션 효과 추가 (선택사항)
            Animation anim = resultTextObject.GetComponent<Animation>();
            if (anim != null)
            {
                anim.Stop();
                anim.Play();
            }
        }
    }

    // 판정 결과에 따른 효과 적용 함수
    private void ApplyPressResultEffects(PressQualityResult result)
    {
        float originalTime = timeToBecomeReadyToFlip;
        
        switch (result)
        {
            case PressQualityResult.Perfect:
                // PERFECT 판정 효과: 30% 시간 단축
                timeToBecomeReadyToFlip *= 0.7f;
                Debug.Log("PERFECT! 뒤집기까지 시간 30% 단축! (" + originalTime + "초 -> " + timeToBecomeReadyToFlip + "초)");
                break;
                
            case PressQualityResult.Good:
                // GOOD 판정 효과: 15% 시간 단축
                timeToBecomeReadyToFlip *= 0.85f;
                Debug.Log("GOOD! 뒤집기까지 시간 15% 단축! (" + originalTime + "초 -> " + timeToBecomeReadyToFlip + "초)");
                break;
                
            case PressQualityResult.Miss:
                // MISS 판정 효과: 추가 효과 없음
                Debug.Log("MISS! 기본 쿠킹 시간 유지: " + timeToBecomeReadyToFlip + "초");
                break;
        }
        
        // GameManager에 점수 추가 (GameManager가 있다면)
        /*
        GameManager gameManager = FindObjectOfType<GameManager>();
        if (gameManager != null)
        {
            int scoreToAdd = 0;
            switch (result)
            {
                case PressQualityResult.Perfect: scoreToAdd = 100; break;
                case PressQualityResult.Good: scoreToAdd = 50; break;
                case PressQualityResult.Miss: scoreToAdd = 10; break;
            }
            gameManager.AddScore(scoreToAdd);
        }
        */
    }
}