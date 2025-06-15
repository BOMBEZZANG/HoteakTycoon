// Assets/Scripts/Customer/Customer.cs
// 🎭 에러 수정된 PointManager 연동 완전한 손님 시스템 (호환성 개선 버전)

using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class Customer : MonoBehaviour
{
    public enum CustomerState
    {
        Entering,       // 들어오는 중
        Ordering,       // 주문 중 (말풍선 나타남)
        Waiting,        // 대기 중 (평온)
        Warning,        // 경고 중 (화남 아이콘)
        Satisfied,      // 만족하며 떠나는 중
        Angry,          // 화내며 떠나는 중
        Exiting         // 퇴장 중
    }
    
    /// <summary>
    /// 📝 주문 항목 클래스
    /// </summary>
    [System.Serializable]
    public class OrderItem
    {
        public PreparationUI.FillingType fillingType;
        public int quantity;
        public int receivedQuantity;  // 받은 개수
        
        public OrderItem(PreparationUI.FillingType type, int qty)
        {
            fillingType = type;
            quantity = qty;
            receivedQuantity = 0;
        }
        
        public bool IsCompleted()
        {
            return receivedQuantity >= quantity;
        }
        
        public int GetRemainingQuantity()
        {
            return quantity - receivedQuantity;
        }
        
        public float GetCompletionPercentage()
        {
            return quantity > 0 ? (float)receivedQuantity / quantity : 0f;
        }
    }
    
    [Header("손님 기본 정보")]
    public int customerID;
    public string customerName = "손님";
    
    [Header("🎨 손님 스프라이트 설정")]
    public Sprite[] customerSprites;        // 손님 이미지 3개 배열
    public int selectedSpriteIndex = -1;    // 선택된 스프라이트 인덱스 (-1이면 랜덤)
    
    [Header("📝 주문 정보")]
    public List<OrderItem> orderItems = new List<OrderItem>();  // 주문 항목 리스트
    public int maxTotalQuantity = 3;        // 최대 총 주문 개수
    public int minTotalQuantity = 1;        // 최소 총 주문 개수
    
    [Header("타이밍 설정")]
    public float enterDuration = 2.0f;         // 들어오는 시간
    public float orderDisplayDelay = 0.5f;     // 주문 표시 딜레이
    public float maxWaitTime = 20.0f;          // 최대 대기 시간
    public float warningThreshold = 0.25f;     // 경고 시작 비율 (75%에서 경고)
    public float exitDuration = 1.5f;          // 나가는 시간
    
    [Header("이동 설정")]
    public Vector3 enterStartPosition;         // 입장 시작 위치
    public Vector3 counterPosition;            // 카운터 위치
    public Vector3 exitEndPosition;            // 퇴장 끝 위치
    public float walkSpeed = 2.0f;             // 걷기 속도
    public float angryWalkSpeed = 4.0f;        // 화났을 때 걷기 속도
    
    [Header("💎 점수 및 보상 (PointManager 연동)")]
    public int satisfactionRewardPerItem = 50; // 항목당 만족 점수 (기존 시스템용)
    public int angryPenalty = -50;             // 화남 시 감점 (기존 시스템용)
    public int bonusForCompleteOrder = 50;     // 전체 주문 완료 보너스 (기존 시스템용)
    public bool usePointManagerSystem = true;  // PointManager 시스템 사용 여부
    
    [Header("🎭 감정 아이콘 시스템 설정")]
    public bool useEnhancedEmotions = true;    // 향상된 감정 시스템 사용 여부
    public bool enableEmotionSounds = true;    // 감정 사운드 활성화
    public bool enableEmotionDebug = false;    // 감정 디버그 로그
    
    [Header("🎨 감정 아이콘들")]
    public GameObject neutralEmotionIcon;      // 중성 감정 (기본)
    public GameObject happyEmotionIcon;        // 기쁨 😊
    public GameObject satisfactionEmotionIcon; // 만족 😋
    public GameObject confusedEmotionIcon;     // 혼란 😕
    public GameObject warnEmotionIcon;         // 경고 😤
    public GameObject angryEmotionIcon;        // 화남 😡
    public GameObject sadEmotionIcon;          // 슬픔 😢
    public GameObject thinkingEmotionIcon;     // 생각 중 🤔
    public GameObject heartEmotionIcon;        // 사랑 💖
    public GameObject starEmotionIcon;         // 별점 ⭐
    public GameObject furiousEmotionIcon;      // 격분 🤬
    
    [Header("🔊 감정 사운드")]
    public AudioClip happySound;               // 기쁨 소리
    public AudioClip satisfactionSound;        // 만족 소리
    public AudioClip confusedSound;            // 혼란 소리
    public AudioClip warnSound;                // 경고 소리
    public AudioClip angrySound;               // 화남 소리
    public AudioClip sadSound;                 // 슬픔 소리
    public AudioClip heartSound;               // 사랑 소리
    public AudioClip starSound;                // 별점 소리
    
    [Header("🎭 감정 아이콘 설정")]
    public float emotionDisplayDuration = 2.0f; // 감정 표시 시간
    public Vector3 emotionIconOffset = new Vector3(0, 1.5f, 0); // 아이콘 오프셋
    public float emotionIconScale = 1.0f;       // 아이콘 크기
    public float emotionAnimationSpeed = 1.0f;  // 감정 애니메이션 속도
    
    [Header("이동 애니메이션")]
    public bool useMovementAnimation = true;    // 이동 애니메이션 사용
    public AnimationCurve movementCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
    public bool enableWalkingBob = true;        // 걷기 시 위아래 흔들림
    public float walkingBobHeight = 0.1f;       // 흔들림 높이
    public float walkingBobSpeed = 5f;          // 흔들림 속도
    
    [Header("상호작용 설정")]
    public float clickRadius = 2f;              // 클릭 가능 범위
    public LayerMask interactionLayer = -1;     // 상호작용 레이어
    public bool enableClickHighlight = true;    // 클릭 하이라이트 활성화
    public Color highlightColor = Color.yellow; // 하이라이트 색상
    
    [Header("🔊 사운드 효과")]
    public AudioClip enterSound;                // 입장 사운드
    public AudioClip orderSound;                // 주문 사운드
    public AudioClip receiveItemSound;          // 아이템 받는 사운드
    public AudioClip completeOrderSound;        // 주문 완료 사운드
    public AudioClip wrongOrderSound;           // 잘못된 주문 사운드
    public AudioClip exitSatisfiedSound;        // 만족 퇴장 사운드
    public AudioClip exitAngrySound;            // 화남 퇴장 사운드
    
    [Header("🎉 시각적 효과")]
    public GameObject satisfactionEffect;       // 만족 효과
    public GameObject angryEffect;              // 화남 효과
    public GameObject orderCompleteEffect;      // 주문 완료 효과
    public GameObject wrongOrderEffect;         // 잘못된 주문 효과
    public GameObject clickEffect;              // 클릭 효과
    
    [Header("⚙️ 시스템 설정")]
    public bool enableAutoDestroy = true;       // 자동 제거 활성화
    public float autoDestroyDelay = 2f;         // 자동 제거 지연 시간
    public bool saveStatistics = true;          // 통계 저장 여부
    public bool enablePerformanceMode = false;  // 성능 모드 (일부 효과 비활성화)
    
    [Header("🐛 디버그")]
    public bool enableDebugLogs = true;         // 디버그 로그 활성화
    public bool showDebugGizmos = false;        // 디버그 기즈모 표시
    public bool enableTestMode = false;         // 테스트 모드
    public KeyCode testSatisfiedKey = KeyCode.Q; // 테스트 만족 키
    public KeyCode testAngryKey = KeyCode.E;    // 테스트 화남 키
    
    // 내부 상태 변수들
    private CustomerState currentState = CustomerState.Entering;
    private float currentWaitTime = 0f;
    private bool hasReceivedCompleteOrder = false;
    private bool isWarningPhase = false;
    private bool wasAngry = false;
    private int wrongOrderAttempts = 0;
    
    // 컴포넌트 참조
    private SpriteRenderer spriteRenderer;
    private AudioSource audioSource;
    private GameObject currentEmotionIcon;
    private Coroutine emotionCoroutine;
    private Rigidbody2D rb;
    private Collider2D customerCollider;
    private CustomerSpawner parentSpawner;
    
    // UI 컴포넌트 (선택적 - 없어도 동작)
    private CustomerUI_Enhanced enhancedUI;      // 🔧 클래스명 수정
    private CustomerAnimator customerAnimator;   // 애니메이션 컴포넌트
    
    // 감정 시스템 내부 변수
    private string lastEmotionShown = "";
    private float lastEmotionTime = 0f;
    private bool isShowingWarningEmotion = false;
    private Queue<string> emotionQueue = new Queue<string>();
    
    // 이동 시스템 내부 변수
    private bool isMoving = false;
    private Vector3 walkingStartPosition;
    private float walkingTimer = 0f;
    private Vector3 originalScale;
    
    // 상호작용 시스템
    private bool isHighlighted = false;
    private Color originalColor;
    
    void Awake()
    {
        InitializeComponents();
        SetupEventHandlers();
    }

    void Start()
    {
        InitializeCustomer();
        SetupCustomerSprite();
        StartCustomerFlow();
        
        if (enableDebugLogs)
        {
            Debug.Log($"🎭 손님 {customerName} 시작 완료");
        }
    }

    void Update()
    {
        UpdateWaitTime();
        UpdateWalkingAnimation();
        HandleInput();
        HandleTestMode();
        
        // 감정 큐 처리
        ProcessEmotionQueue();
    }
    
    /// <summary>
    /// 컴포넌트 초기화
    /// </summary>
    void InitializeComponents()
    {
        // 기본 컴포넌트들
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer == null)
            spriteRenderer = gameObject.AddComponent<SpriteRenderer>();
            
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
            audioSource = gameObject.AddComponent<AudioSource>();
            
        rb = GetComponent<Rigidbody2D>();
        customerCollider = GetComponent<Collider2D>();
        
        // 콜라이더가 없으면 추가
        if (customerCollider == null)
        {
            CircleCollider2D circleCollider = gameObject.AddComponent<CircleCollider2D>();
            circleCollider.radius = clickRadius;
            circleCollider.isTrigger = true;
            customerCollider = circleCollider;
        }
        
        // UI 컴포넌트 자동 연결 (선택적)
        enhancedUI = GetComponentInChildren<CustomerUI_Enhanced>();
        if (enhancedUI == null)
        {
            if (enableDebugLogs)
                Debug.Log($"⚠️ {gameObject.name}: CustomerUI_Enhanced 컴포넌트가 없습니다. 기본 기능만 사용됩니다.");
            useEnhancedEmotions = false;
        }
        
        customerAnimator = GetComponentInChildren<CustomerAnimator>();
        
        // 원본 색상 및 스케일 저장
        if (spriteRenderer != null)
            originalColor = spriteRenderer.color;
        originalScale = transform.localScale;
    }
    
    /// <summary>
    /// 이벤트 핸들러 설정
    /// </summary>
    void SetupEventHandlers()
    {
        // PointManager 이벤트 연결 (있다면)
        if (PointManager.Instance != null && usePointManagerSystem)
        {
            PointManager.Instance.OnCustomerSatisfaction += OnPointManagerSatisfaction;
            
            if (enableDebugLogs)
            {
                Debug.Log($"💎 {customerName}: PointManager 이벤트 연결 완료");
            }
        }
    }
    
    /// <summary>
    /// 손님 초기화
    /// </summary>
    void InitializeCustomer()
    {
        currentState = CustomerState.Entering;
        currentWaitTime = 0f;
        hasReceivedCompleteOrder = false;
        isWarningPhase = false;
        wasAngry = false;
        wrongOrderAttempts = 0;
        
        // 감정 아이콘 모두 숨김
        HideAllEmotionIcons();
        
        // 초기 상태 설정
        if (customerCollider != null)
            customerCollider.enabled = false; // 처음에는 클릭 불가
        
        if (enableDebugLogs)
        {
            Debug.Log($"🎭 손님 {customerName} 초기화 완료");
        }
    }
    
    /// <summary>
    /// CustomerSpawner에서 호출하는 초기화 메서드
    /// </summary>
    public void InitializeCustomer(int id, string name, CustomerSpawner spawner)
    {
        customerID = id;
        customerName = name;
        parentSpawner = spawner;
        
        // 스프라이트 설정
        SetupCustomerSprite();
        
        if (enableDebugLogs)
        {
            Debug.Log($"🎭 손님 {customerName} (ID: {customerID}) CustomerSpawner 초기화 완료");
        }
    }
    
    /// <summary>
    /// 손님 스프라이트 설정
    /// </summary>
    void SetupCustomerSprite()
    {
        if (customerSprites != null && customerSprites.Length > 0)
        {
            // 선택된 인덱스가 유효하지 않으면 랜덤 선택
            if (selectedSpriteIndex < 0 || selectedSpriteIndex >= customerSprites.Length)
            {
                selectedSpriteIndex = Random.Range(0, customerSprites.Length);
            }
            
            // 스프라이트 적용
            if (spriteRenderer != null && customerSprites[selectedSpriteIndex] != null)
            {
                spriteRenderer.sprite = customerSprites[selectedSpriteIndex];
                originalColor = spriteRenderer.color; // 색상 업데이트
            }
            
            if (enableDebugLogs)
            {
                Debug.Log($"🎨 {customerName} 스프라이트 {selectedSpriteIndex}번 적용");
            }
        }
        else if (enableDebugLogs)
        {
            Debug.LogWarning($"⚠️ {customerName}: customerSprites가 설정되지 않았습니다!");
        }
    }
    
    /// <summary>
    /// 손님 플로우 시작
    /// </summary>
    void StartCustomerFlow()
    {
        StartCoroutine(CustomerLifeCycle());
    }
    
    /// <summary>
    /// 손님 생명주기 코루틴
    /// </summary>
    IEnumerator CustomerLifeCycle()
    {
        // 1. 입장
        yield return StartCoroutine(EnterPhase());
        
        // 2. 주문
        yield return StartCoroutine(OrderPhase());
        
        // 3. 대기 (별도 Update에서 처리)
        ChangeState(CustomerState.Waiting);
        
        // 4. 주문 완료되거나 화날 때까지 대기
        while (currentState == CustomerState.Waiting || currentState == CustomerState.Warning)
        {
            yield return null;
        }
        
        // 5. 퇴장
        yield return StartCoroutine(ExitPhase());
    }
    
    /// <summary>
    /// 입장 단계
    /// </summary>
    IEnumerator EnterPhase()
    {
        ChangeState(CustomerState.Entering);
        
        // 입장 위치 설정
        transform.position = counterPosition + enterStartPosition;
        
        // 입장 사운드
        PlaySound(enterSound);
        
        // 입장 애니메이션
        if (useMovementAnimation)
        {
            yield return StartCoroutine(MoveToPosition(counterPosition, walkSpeed));
        }
        else
        {
            transform.position = counterPosition;
        }
        
        if (enableDebugLogs)
        {
            Debug.Log($"🚶 {customerName} 입장 완료");
        }
    }
    
    /// <summary>
    /// 주문 단계
    /// </summary>
    IEnumerator OrderPhase()
    {
        ChangeState(CustomerState.Ordering);
        
        // 주문 생성
        GenerateOrder();
        
        // 주문 사운드
        PlaySound(orderSound);
        
        // 주문 표시 딜레이
        yield return new WaitForSeconds(orderDisplayDelay);
        
        // 🔧 UI에 주문 표시 (안전한 방식)
        ShowOrderUI();
        
        // 🎭 주문 감정 표시
        ShowEmotion("thinking", 2f, "주문 중");
        
        if (enableDebugLogs)
        {
            Debug.Log($"📝 {customerName} 주문: {GetOrderSummary()}");
        }
    }
    
    /// <summary>
    /// UI에 주문 표시 (호환성 개선)
    /// </summary>
    void ShowOrderUI()
    {
        if (useEnhancedEmotions && enhancedUI != null)
        {
            // CustomerUI_Enhanced 사용
            enhancedUI.ShowOrderBubble(orderItems);
        }
        else
        {
            // 기본 UI 또는 로그로 대체
            if (enableDebugLogs)
            {
                Debug.Log($"📋 {customerName} 주문 표시 (UI 없음): {GetOrderSummary()}");
            }
        }
    }
    
    /// <summary>
    /// 주문 UI 숨기기 (호환성 개선)
    /// </summary>
    void HideOrderUI()
    {
        if (useEnhancedEmotions && enhancedUI != null)
        {
            // CustomerUI_Enhanced 사용
            enhancedUI.HideAllUI();
        }
    }
    
    /// <summary>
    /// 퇴장 단계
    /// </summary>
    IEnumerator ExitPhase()
    {
        ChangeState(CustomerState.Exiting);
        
        // UI 숨김
        HideOrderUI();
        
        // 퇴장 사운드
        if (wasAngry)
            PlaySound(exitAngrySound);
        else
            PlaySound(exitSatisfiedSound);
        
        // 퇴장 위치로 이동
        Vector3 exitTarget = counterPosition + exitEndPosition;
        float speed = wasAngry ? angryWalkSpeed : walkSpeed;
        
        if (useMovementAnimation)
        {
            yield return StartCoroutine(MoveToPosition(exitTarget, speed));
        }
        
        // 자동 제거 지연
        if (enableAutoDestroy && autoDestroyDelay > 0)
        {
            yield return new WaitForSeconds(autoDestroyDelay);
        }
        
        ExitComplete();
    }
    
    /// <summary>
    /// 위치로 이동하는 코루틴
    /// </summary>
    IEnumerator MoveToPosition(Vector3 targetPosition, float speed, System.Action onComplete = null)
    {
        Vector3 startPosition = transform.position;
        float distance = Vector3.Distance(startPosition, targetPosition);
        float duration = distance / speed;
        float elapsedTime = 0f;
        
        isMoving = true;
        walkingStartPosition = startPosition;
        walkingTimer = 0f;
        
        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            walkingTimer += Time.deltaTime;
            
            float progress = elapsedTime / duration;
            float curveValue = movementCurve.Evaluate(progress);
            
            Vector3 currentPos = Vector3.Lerp(startPosition, targetPosition, curveValue);
            
            // 걷기 애니메이션 (위아래 흔들림)
            if (enableWalkingBob)
            {
                float bobOffset = Mathf.Sin(walkingTimer * walkingBobSpeed) * walkingBobHeight;
                currentPos.y += bobOffset;
            }
            
            transform.position = currentPos;
            
            yield return null;
        }
        
        transform.position = targetPosition;
        isMoving = false;
        
        onComplete?.Invoke();
    }
    
    /// <summary>
    /// 걷기 애니메이션 업데이트
    /// </summary>
    void UpdateWalkingAnimation()
    {
        if (!isMoving || !enableWalkingBob) return;
        
        // 이미 MoveToPosition에서 처리됨
    }
    
    /// <summary>
    /// 주문 생성
    /// </summary>
    void GenerateOrder()
    {
        orderItems.Clear();
        
        // 총 주문 개수 결정
        int totalQuantity = Random.Range(minTotalQuantity, maxTotalQuantity + 1);
        
        // 랜덤하게 타입 분배
        PreparationUI.FillingType[] availableTypes = {
            PreparationUI.FillingType.Sugar,
            PreparationUI.FillingType.Seed
        };
        
        Dictionary<PreparationUI.FillingType, int> orderCounts = new Dictionary<PreparationUI.FillingType, int>();
        
        for (int i = 0; i < totalQuantity; i++)
        {
            PreparationUI.FillingType randomType = availableTypes[Random.Range(0, availableTypes.Length)];
            
            if (orderCounts.ContainsKey(randomType))
                orderCounts[randomType]++;
            else
                orderCounts[randomType] = 1;
        }
        
        // OrderItem 생성
        foreach (var kvp in orderCounts)
        {
            orderItems.Add(new OrderItem(kvp.Key, kvp.Value));
        }
        
        if (enableDebugLogs)
        {
            Debug.Log($"📝 주문 생성: 총 {totalQuantity}개 - {GetOrderSummary()}");
        }
    }
    
    /// <summary>
    /// 대기 시간 업데이트
    /// </summary>
    void UpdateWaitTime()
    {
        if (currentState != CustomerState.Waiting && currentState != CustomerState.Warning) return;
        
        currentWaitTime += Time.deltaTime;
        
        // 경고 단계 전환
        float warningTime = maxWaitTime * (1f - warningThreshold);
        if (!isWarningPhase && currentWaitTime >= warningTime)
        {
            StartWarningPhase();
        }
        
        // 화나서 떠나기
        if (currentWaitTime >= maxWaitTime)
        {
            LeaveAngry();
        }
        
        // 🔧 UI 업데이트 (안전한 방식)
        UpdateWaitProgressUI();
    }
    
    /// <summary>
    /// 대기 진행 UI 업데이트 (호환성 개선)
    /// </summary>
    void UpdateWaitProgressUI()
    {
        float waitProgress = currentWaitTime / maxWaitTime;
        
        if (useEnhancedEmotions && enhancedUI != null)
        {
            enhancedUI.UpdateWaitProgress(waitProgress);
        }
        else if (enableDebugLogs && Time.frameCount % 60 == 0) // 1초마다 로그
        {
            Debug.Log($"⏰ {customerName} 대기 진행: {waitProgress:P0}");
        }
    }
    
    /// <summary>
    /// 경고 단계 시작
    /// </summary>
    void StartWarningPhase()
    {
        isWarningPhase = true;
        ChangeState(CustomerState.Warning);
        
        // 🎭 경고 감정 표시
        ShowEmotion("warn", -1f, "대기 시간 초과 경고"); // 무한 표시
        
        // 경고 사운드
        PlaySound(warnSound);
        
        if (enableDebugLogs)
        {
            Debug.Log($"⚠️ {customerName} 경고 단계 진입!");
        }
    }
    
    /// <summary>
    /// 입력 처리
    /// </summary>
    void HandleInput()
    {
        if (currentState != CustomerState.Waiting && currentState != CustomerState.Warning) return;
        
        if (Input.GetMouseButtonDown(0))
        {
            Vector3 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            mousePos.z = 0f;
            
            float distance = Vector2.Distance(mousePos, transform.position);
            if (distance <= clickRadius)
            {
                OnCustomerClicked();
            }
        }
    }
    
    /// <summary>
    /// 테스트 모드 처리
    /// </summary>
    void HandleTestMode()
    {
        if (!enableTestMode) return;
        
        if (Input.GetKeyDown(testSatisfiedKey))
        {
            LeaveSatisfied();
        }
        else if (Input.GetKeyDown(testAngryKey))
        {
            LeaveAngry();
        }
    }
    
    /// <summary>
    /// 손님 클릭 처리
    /// </summary>
    void OnCustomerClicked()
    {
        // 클릭 효과
        ShowClickEffect();
        
        if (!CanReceiveOrder())
        {
            DebugEmotion("아직 주문을 받을 수 없습니다!");
            return;
        }
        
        GameObject selectedHotteok = StackSalesCounter.Instance?.GetSelectedHotteok();
        if (selectedHotteok == null)
        {
            DebugEmotion("선택된 호떡이 없습니다! 먼저 판매대에서 호떡을 선택하세요.");
            ShowNoSelectionFeedback();
            return;
        }
        
        // 선택된 호떡의 타입 확인
        HotteokInStack hotteokScript = selectedHotteok.GetComponent<HotteokInStack>();
        if (hotteokScript == null)
        {
            Debug.LogError("선택된 호떡에 HotteokInStack 스크립트가 없습니다!");
            return;
        }
        
        PreparationUI.FillingType selectedType = hotteokScript.fillingType;
        
        // 주문에 해당 타입이 있고 아직 필요한지 확인
        if (HasOrderedType(selectedType))
        {
            ReceiveHotteok(selectedType);
        }
        else
        {
            ReceiveWrongOrder(selectedType);
        }
    }
    
    /// <summary>
    /// 📝 호떡 수령 처리 (올바른 주문) - PointManager 연동 완료
    /// </summary>
    void ReceiveHotteok(PreparationUI.FillingType receivedType)
    {
        // 해당 타입의 주문 항목 찾기
        OrderItem orderItem = orderItems.Find(item => item.fillingType == receivedType && !item.IsCompleted());
        
        if (orderItem != null)
        {
            orderItem.receivedQuantity++;
            
            if (enableDebugLogs)
            {
                Debug.Log($"✅ {customerName} {GetHotteokName(receivedType)} 1개 수령! " +
                         $"({orderItem.receivedQuantity}/{orderItem.quantity}) | 진행: {GetOrderProgress()}");
            }
            
            // 선택된 호떡을 손님에게 전달
            if (StackSalesCounter.Instance != null && StackSalesCounter.Instance.DeliverSelectedHotteokToCustomer())
            {
                // 💰 골드 지급 처리
                if (GoldManager.Instance != null)
                {
                    GoldManager.Instance.ProcessHotteokSale(receivedType);
                }
                else if (enableDebugLogs)
                {
                    Debug.LogWarning("⚠️ GoldManager가 없어 골드를 지급할 수 없습니다!");
                }
                
                // 아이템 받는 사운드
                PlaySound(receiveItemSound);
                
                // 기존 GameManager 점수 시스템 (PointManager와 별개로 유지)
                if (GameManager.Instance != null)
                {
                    GameManager.Instance.AddScore(satisfactionRewardPerItem);
                }
                
                // 🔧 UI 업데이트 (안전한 방식)
                UpdateOrderProgressUI();
                
                // 전체 주문 완료 확인
                if (IsOrderComplete())
                {
                    CompleteEntireOrder();
                }
                else
                {
                    // 부분 완료 피드백
                    ShowPartialCompletionFeedback(receivedType);
                }
            }
        }
        else
        {
            Debug.LogError("❌ 주문 항목을 찾을 수 없습니다!");
        }
    }
    
    /// <summary>
    /// 주문 진행 UI 업데이트 (호환성 개선)
    /// </summary>
    void UpdateOrderProgressUI()
    {
        if (useEnhancedEmotions && enhancedUI != null)
        {
            enhancedUI.UpdateOrderProgress(orderItems);
        }
    }
    
    /// <summary>
    /// 📝 전체 주문 완료 처리 - PointManager 연동 완료
    /// </summary>
    void CompleteEntireOrder()
    {
        hasReceivedCompleteOrder = true;
        
        // 💎 PointManager 시스템 사용 (우선순위)
        if (usePointManagerSystem && PointManager.Instance != null)
        {
            PointManager.Instance.ProcessCustomerSatisfaction();
            
            if (enableDebugLogs)
            {
                Debug.Log($"💎 {customerName}: PointManager를 통해 손님 만족 처리 완료");
            }
        }
        else
        {
            // 기존 GameManager 시스템 (fallback)
            if (GameManager.Instance != null)
            {
                GameManager.Instance.AddScore(bonusForCompleteOrder);
            }
            
            if (enableDebugLogs)
            {
                Debug.LogWarning($"⚠️ {customerName}: PointManager가 없어 기존 시스템 사용");
            }
        }
        
        // 주문 완료 사운드
        PlaySound(completeOrderSound);
        
        // 주문 완료 효과
        ShowVisualEffect(orderCompleteEffect);
        
        DebugEmotion($"🎉 {customerName} 전체 주문 완료!");
        
        // 만족하며 떠나기
        LeaveSatisfied();
    }
    
    /// <summary>
    /// 🎭 부분 완료 피드백 (감정 아이콘 포함)
    /// </summary>
    void ShowPartialCompletionFeedback(PreparationUI.FillingType receivedType)
    {
        string message = $"{GetHotteokName(receivedType)} 감사해요! 🙂";
        
        // 🎭 만족 아이콘 표시
        ShowEmotion("satisfaction", 1.5f, "부분 완료");
        
        if (useEnhancedEmotions && enhancedUI != null) 
        {
            enhancedUI.ShowPartialCompletionFeedback(message);
        }
        else if (enableDebugLogs)
        {
            Debug.Log($"😊 {customerName}: {message}");
        }
    }
    
    /// <summary>
    /// 🎭 잘못된 주문 수령 (감정 아이콘 포함)
    /// </summary>
    void ReceiveWrongOrder(PreparationUI.FillingType receivedType)
    {
        wrongOrderAttempts++;
        
        DebugEmotion($"❌ {customerName} 잘못된 주문! 받음: {GetHotteokName(receivedType)}, 주문: {GetOrderSummary()} (시도: {wrongOrderAttempts}/3)");
        
        // 호떡 선택 해제 (다시 선택할 수 있도록)
        if (StackSalesCounter.Instance != null)
        {
            StackSalesCounter.Instance.DeselectHotteok();
        }
        
        // 잘못된 주문 사운드
        PlaySound(wrongOrderSound);
        
        // 잘못된 주문 효과
        ShowVisualEffect(wrongOrderEffect);
        
        // 🎭 혼란 아이콘 표시
        ShowEmotion("confused", 2f, "잘못된 주문");
        
        if (useEnhancedEmotions && enhancedUI != null) 
        {
            enhancedUI.ShowWrongOrderFeedback();
        }
        
        if (customerAnimator != null)
        {
            customerAnimator.PlayRejectAnimation();
        }
        
        // 너무 많은 잘못된 시도 시 화내기
        if (wrongOrderAttempts >= 3)
        {
            Debug.Log($"😡 {customerName}: 너무 많은 잘못된 주문 시도로 화남!");
            LeaveAngry();
        }
    }
    
    /// <summary>
    /// 🎭 호떡 선택 안함 피드백 (감정 아이콘 포함)
    /// </summary>
    void ShowNoSelectionFeedback()
    {
        DebugEmotion($"💭 {customerName}: 호떡을 먼저 선택해주세요!");
        
        // 🎭 생각 중 아이콘 표시
        ShowEmotion("thinking", 1.5f, "호떡 선택 요청");
        
        if (useEnhancedEmotions && enhancedUI != null) 
        {
            enhancedUI.ShowNoSelectionFeedback();
        }
    }
    
    /// <summary>
    /// 😊 만족하며 떠나기
    /// </summary>
    public void LeaveSatisfied()
    {
        if (currentState == CustomerState.Satisfied || currentState == CustomerState.Exiting) return;
        
        DebugEmotion($"😊 {customerName} 만족하며 떠남!");
        
        wasAngry = false;
        ChangeState(CustomerState.Satisfied);
        
        // 🎭 만족 감정 시퀀스: 별점 → 사랑 → 만족
        if (useEnhancedEmotions && enhancedUI != null)
        {
            string[] emotions = {"star", "heart", "satisfaction"};
            float[] durations = {1f, 1f, 1.5f};
            enhancedUI.ShowEmotionSequence(emotions, durations);
        }
        else
        {
            ShowEmotion("satisfaction", 2f, "만족");
        }
        
        // 만족 효과
        ShowVisualEffect(satisfactionEffect);
        
        // 🔧 만족한 손님 통계 업데이트 (안전한 방식)
        if (parentSpawner != null)
        {
            // parentSpawner는 OnCustomerExit에서 통계 처리
        }
        
        // 콜라이더 비활성화 (클릭 불가)
        if (customerCollider != null)
        {
            customerCollider.enabled = false;
        }
    }
    
    /// <summary>
    /// 😠 화나서 떠나기 - PointManager 연동 완료
    /// </summary>
    public void LeaveAngry()
    {
        if (currentState == CustomerState.Angry || currentState == CustomerState.Exiting) return;
        
        DebugEmotion($"😡 {customerName} 화내며 떠남! (대기시간: {currentWaitTime:F1}초/{maxWaitTime:F1}초)");
        
        wasAngry = true;
        
        // 💎 PointManager로 손님 불만족 처리
        if (usePointManagerSystem && PointManager.Instance != null)
        {
            PointManager.Instance.ProcessCustomerDissatisfaction();
            
            if (enableDebugLogs)
            {
                Debug.Log($"💎 {customerName}: PointManager를 통해 손님 불만족 처리 완료");
            }
        }
        
        // 기존 GameManager 감점 시스템 유지 (PointManager는 보너스만 관리하므로)
        if (GameManager.Instance != null)
        {
            GameManager.Instance.AddScore(angryPenalty);
        }
        
        ChangeState(CustomerState.Angry);
        
        // 🎭 분노 감정 표시
        ShowEmotion("furious", 2f, "분노");
        
        // 화남 효과
        ShowVisualEffect(angryEffect);
        
        // 콜라이더 비활성화 (클릭 불가)
        if (customerCollider != null)
        {
            customerCollider.enabled = false;
        }
    }
    
    /// <summary>
    /// 퇴장 완료
    /// </summary>
    void ExitComplete()
    {
        // 감정 정리
        HideAllEmotionIcons();
        
        // CustomerSpawner에 퇴장 알림
        if (parentSpawner != null)
        {
            parentSpawner.OnCustomerExit(this, !wasAngry); // 🔧 매개변수 추가
        }
        
        // 이벤트 정리
        CleanupEventHandlers();
        
        if (enableDebugLogs)
        {
            Debug.Log($"👋 {customerName} 퇴장 완료 (만족: {!wasAngry})");
        }
        
        // 객체 제거
        Destroy(gameObject);
    }
    
    /// <summary>
    /// 이벤트 핸들러 정리
    /// </summary>
    void CleanupEventHandlers()
    {
        if (PointManager.Instance != null && usePointManagerSystem)
        {
            PointManager.Instance.OnCustomerSatisfaction -= OnPointManagerSatisfaction;
        }
    }
    
    /// <summary>
    /// PointManager 손님 만족 이벤트 핸들러
    /// </summary>
    void OnPointManagerSatisfaction(int points)
    {
        if (enableDebugLogs)
        {
            Debug.Log($"💎 {customerName}: PointManager에서 {points}점 획득!");
        }
        
        // 추가적인 시각적 피드백 등을 여기서 처리할 수 있음
    }
    
    // ===== 🎭 감정 시스템 =====
    
    /// <summary>
    /// 🎭 감정 표시 메인 메서드
    /// </summary>
    void ShowEmotion(string emotionType, float duration = 2f, string debugReason = "")
    {
        if (!useEnhancedEmotions) return;
        
        // enhancedUI가 있으면 사용, 없으면 기본 아이콘 시스템 사용
        if (enhancedUI != null)
        {
            enhancedUI.ShowEmotionIcon(emotionType, duration, enableEmotionSounds);
        }
        else
        {
            // 기본 감정 아이콘 시스템
            ShowBasicEmotion(emotionType, duration);
        }
        
        if (enableEmotionDebug)
        {
            Debug.Log($"🎭 {customerName} 감정 표시: {emotionType} ({debugReason})");
        }
    }
    
    /// <summary>
    /// 기본 감정 아이콘 시스템 (enhancedUI가 없을 때)
    /// </summary>
    void ShowBasicEmotion(string emotionType, float duration)
    {
        // 중복 감정 방지 (짧은 시간 내)
        if (lastEmotionShown == emotionType && Time.time - lastEmotionTime < 0.5f)
        {
            return;
        }
        
        lastEmotionShown = emotionType;
        lastEmotionTime = Time.time;
        
        // 기존 감정 정리
        if (emotionCoroutine != null)
        {
            StopCoroutine(emotionCoroutine);
        }
        HideAllEmotionIcons();
        
        // 새로운 감정 표시
        GameObject targetIcon = GetEmotionIcon(emotionType);
        if (targetIcon != null)
        {
            if (duration > 0)
            {
                emotionCoroutine = StartCoroutine(ShowEmotionCoroutine(targetIcon, duration));
            }
            else
            {
                // 무한 표시 (경고 등)
                emotionCoroutine = StartCoroutine(ShowEmotionInfinite(targetIcon));
            }
            
            // 사운드 재생
            PlayEmotionSound(emotionType);
        }
    }
    
    /// <summary>
    /// 감정을 큐에 추가
    /// </summary>
    void QueueEmotion(string emotionType)
    {
        emotionQueue.Enqueue(emotionType);
    }
    
    /// <summary>
    /// 감정 큐 처리
    /// </summary>
    void ProcessEmotionQueue()
    {
        if (emotionQueue.Count > 0 && (emotionCoroutine == null || currentEmotionIcon == null))
        {
            string nextEmotion = emotionQueue.Dequeue();
            ShowEmotion(nextEmotion, 1.5f, "큐에서 처리");
        }
    }
    
    /// <summary>
    /// 감정 아이콘 표시 코루틴
    /// </summary>
    IEnumerator ShowEmotionCoroutine(GameObject emotionIcon, float duration)
    {
        if (emotionIcon == null) yield break;
        
        currentEmotionIcon = emotionIcon;
        emotionIcon.SetActive(true);
        
        // 위치 및 크기 설정
        emotionIcon.transform.position = transform.position + emotionIconOffset;
        emotionIcon.transform.localScale = Vector3.one * emotionIconScale;
        
        // 애니메이션 효과
        yield return StartCoroutine(AnimateEmotionIcon(emotionIcon, duration));
        
        emotionIcon.SetActive(false);
        currentEmotionIcon = null;
    }
    
    /// <summary>
    /// 무한 감정 표시 코루틴
    /// </summary>
    IEnumerator ShowEmotionInfinite(GameObject emotionIcon)
    {
        if (emotionIcon == null) yield break;
        
        currentEmotionIcon = emotionIcon;
        emotionIcon.SetActive(true);
        
        // 위치 및 크기 설정
        emotionIcon.transform.position = transform.position + emotionIconOffset;
        emotionIcon.transform.localScale = Vector3.one * emotionIconScale;
        
        // 상태가 변경될 때까지 계속 표시
        while (currentEmotionIcon == emotionIcon && 
               (currentState == CustomerState.Warning || currentState == CustomerState.Waiting))
        {
            // 위치 업데이트 (손님이 움직일 수 있으므로)
            emotionIcon.transform.position = transform.position + emotionIconOffset;
            
            // 깜빡임 효과
            float alpha = 0.7f + 0.3f * Mathf.Sin(Time.time * 3f);
            SetEmotionIconAlpha(emotionIcon, alpha);
            
            yield return null;
        }
        
        emotionIcon.SetActive(false);
        if (currentEmotionIcon == emotionIcon)
            currentEmotionIcon = null;
    }
    
    /// <summary>
    /// 감정 아이콘 애니메이션
    /// </summary>
    IEnumerator AnimateEmotionIcon(GameObject emotionIcon, float duration)
    {
        float elapsedTime = 0f;
        Vector3 originalScale = Vector3.one * emotionIconScale;
        
        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            float progress = elapsedTime / duration;
            
            // 스케일 애니메이션
            float scale = emotionIconScale;
            if (progress < 0.2f)
            {
                // 팝업 효과
                scale *= 1f + (1f - progress / 0.2f) * 0.5f;
            }
            else if (progress > 0.8f)
            {
                // 페이드아웃 효과
                float fadeProgress = (progress - 0.8f) / 0.2f;
                scale *= 1f - fadeProgress * 0.3f;
                SetEmotionIconAlpha(emotionIcon, 1f - fadeProgress);
            }
            
            emotionIcon.transform.localScale = Vector3.one * scale;
            
            // 위치 업데이트
            emotionIcon.transform.position = transform.position + emotionIconOffset;
            
            yield return null;
        }
    }
    
    /// <summary>
    /// 감정 아이콘 알파 설정
    /// </summary>
    void SetEmotionIconAlpha(GameObject emotionIcon, float alpha)
    {
        SpriteRenderer sr = emotionIcon.GetComponent<SpriteRenderer>();
        if (sr != null)
        {
            Color color = sr.color;
            color.a = alpha;
            sr.color = color;
        }
        
        UnityEngine.UI.Image image = emotionIcon.GetComponent<UnityEngine.UI.Image>();
        if (image != null)
        {
            Color color = image.color;
            color.a = alpha;
            image.color = color;
        }
    }
    
    /// <summary>
    /// 감정 타입에 따른 아이콘 반환
    /// </summary>
    GameObject GetEmotionIcon(string emotionType)
    {
        switch (emotionType.ToLower())
        {
            case "happy": return happyEmotionIcon;
            case "satisfaction": return satisfactionEmotionIcon;
            case "confused": return confusedEmotionIcon;
            case "warn": case "warning": return warnEmotionIcon;
            case "angry": return angryEmotionIcon;
            case "furious": return furiousEmotionIcon;
            case "sad": return sadEmotionIcon;
            case "thinking": return thinkingEmotionIcon;
            case "heart": return heartEmotionIcon;
            case "star": return starEmotionIcon;
            case "neutral": case "waiting": return neutralEmotionIcon;
            default: return neutralEmotionIcon;
        }
    }
    
    /// <summary>
    /// 감정 사운드 재생
    /// </summary>
    void PlayEmotionSound(string emotionType)
    {
        if (!enableEmotionSounds || audioSource == null) return;
        
        AudioClip soundToPlay = null;
        
        switch (emotionType.ToLower())
        {
            case "happy": soundToPlay = happySound; break;
            case "satisfaction": soundToPlay = satisfactionSound; break;
            case "confused": soundToPlay = confusedSound; break;
            case "warn": case "warning": soundToPlay = warnSound; break;
            case "angry": case "furious": soundToPlay = angrySound; break;
            case "sad": soundToPlay = sadSound; break;
            case "heart": soundToPlay = heartSound; break;
            case "star": soundToPlay = starSound; break;
        }
        
        if (soundToPlay != null)
        {
            audioSource.PlayOneShot(soundToPlay);
        }
    }
    
    /// <summary>
    /// 모든 감정 아이콘 숨김
    /// </summary>
    void HideAllEmotionIcons()
    {
        GameObject[] allIcons = {
            neutralEmotionIcon, happyEmotionIcon, satisfactionEmotionIcon,
            confusedEmotionIcon, warnEmotionIcon, angryEmotionIcon,
            sadEmotionIcon, thinkingEmotionIcon, heartEmotionIcon,
            starEmotionIcon, furiousEmotionIcon
        };
        
        foreach (GameObject icon in allIcons)
        {
            if (icon != null)
                icon.SetActive(false);
        }
        
        currentEmotionIcon = null;
        isShowingWarningEmotion = false;
    }
    
    // ===== 시각적 효과 및 사운드 =====
    
    /// <summary>
    /// 시각적 효과 표시
    /// </summary>
    void ShowVisualEffect(GameObject effectPrefab)
    {
        if (enablePerformanceMode || effectPrefab == null) return;
        
        GameObject effect = Instantiate(effectPrefab, transform.position, Quaternion.identity);
        Destroy(effect, 3f);
    }
    
    /// <summary>
    /// 클릭 효과 표시
    /// </summary>
    void ShowClickEffect()
    {
        if (clickEffect != null)
        {
            ShowVisualEffect(clickEffect);
        }
        
        // 하이라이트 효과
        if (enableClickHighlight && spriteRenderer != null)
        {
            StartCoroutine(HighlightEffect());
        }
    }
    
    /// <summary>
    /// 하이라이트 효과 코루틴
    /// </summary>
    IEnumerator HighlightEffect()
    {
        Color originalColor = spriteRenderer.color;
        
        for (int i = 0; i < 3; i++)
        {
            spriteRenderer.color = highlightColor;
            yield return new WaitForSeconds(0.1f);
            spriteRenderer.color = originalColor;
            yield return new WaitForSeconds(0.1f);
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
    
    // ===== 상태 관리 =====
    
    /// <summary>
    /// 상태 변경
    /// </summary>
    void ChangeState(CustomerState newState)
    {
        CustomerState oldState = currentState;
        currentState = newState;
        
        // 상태별 특별 처리
        switch (newState)
        {
            case CustomerState.Waiting:
                if (customerCollider != null)
                    customerCollider.enabled = true; // 클릭 가능
                break;
                
            case CustomerState.Satisfied:
            case CustomerState.Angry:
                if (customerCollider != null)
                    customerCollider.enabled = false; // 클릭 불가
                break;
        }
        
        if (enableDebugLogs)
        {
            Debug.Log($"🎭 {customerName} 상태 변경: {oldState} → {newState}");
        }
    }
    
    // ===== 유틸리티 메서드들 =====
    
    /// <summary>
    /// 주문 받을 수 있는지 확인
    /// </summary>
    bool CanReceiveOrder()
    {
        return currentState == CustomerState.Waiting || currentState == CustomerState.Warning;
    }
    
    /// <summary>
    /// 주문한 타입인지 확인
    /// </summary>
    bool HasOrderedType(PreparationUI.FillingType type)
    {
        return orderItems.Find(item => item.fillingType == type && !item.IsCompleted()) != null;
    }
    
    /// <summary>
    /// 주문 완료 여부 확인
    /// </summary>
    bool IsOrderComplete()
    {
        foreach (OrderItem item in orderItems)
        {
            if (!item.IsCompleted())
                return false;
        }
        return true;
    }
    
    /// <summary>
    /// 주문 요약 텍스트
    /// </summary>
    string GetOrderSummary()
    {
        if (orderItems.Count == 0) return "주문 없음";
        
        string summary = "";
        for (int i = 0; i < orderItems.Count; i++)
        {
            OrderItem item = orderItems[i];
            summary += $"{GetHotteokName(item.fillingType)} {item.quantity}개";
            
            if (i < orderItems.Count - 1)
                summary += ", ";
        }
        return summary;
    }
    
    /// <summary>
    /// 주문 진행도 텍스트
    /// </summary>
    string GetOrderProgress()
    {
        if (orderItems.Count == 0) return "주문 없음";
        
        string progress = "";
        for (int i = 0; i < orderItems.Count; i++)
        {
            OrderItem item = orderItems[i];
            progress += $"{GetHotteokName(item.fillingType)} {item.receivedQuantity}/{item.quantity}";
            
            if (i < orderItems.Count - 1)
                progress += ", ";
        }
        return progress;
    }
    
    /// <summary>
    /// 호떡 이름 반환
    /// </summary>
    string GetHotteokName(PreparationUI.FillingType type)
    {
        switch (type)
        {
            case PreparationUI.FillingType.Sugar: return "설탕호떡";
            case PreparationUI.FillingType.Seed: return "씨앗호떡";
            default: return "호떡";
        }
    }
    
    /// <summary>
    /// 디버그 감정 로그
    /// </summary>
    void DebugEmotion(string message)
    {
        if (enableEmotionDebug || enableDebugLogs)
        {
            Debug.Log($"🎭 [{customerName}] {message}");
        }
    }
    
    // ===== 공개 접근자 메서드들 =====
    
    public CustomerState GetCurrentState() => currentState;
    public float GetCurrentWaitTime() => currentWaitTime;
    public float GetMaxWaitTime() => maxWaitTime;
    public float GetWaitProgress() => currentWaitTime / maxWaitTime;
    public bool IsWarningPhase() => isWarningPhase;
    public List<OrderItem> GetOrderItems() => orderItems;
    public bool HasReceivedCompleteOrder() => hasReceivedCompleteOrder;
    public int GetCustomerID() => customerID;
    public string GetCustomerName() => customerName;
    public bool WasAngry() => wasAngry;
    public int GetWrongOrderAttempts() => wrongOrderAttempts;
    
    /// <summary>
    /// CustomerSpawner에서 호출하는 설정 메서드
    /// </summary>
    public void SetSpawner(CustomerSpawner spawner)
    {
        parentSpawner = spawner;
    }
    
    public void SetPositions(Vector3 enterPos, Vector3 counterPos, Vector3 exitPos)
    {
        enterStartPosition = enterPos;
        counterPosition = counterPos;
        exitEndPosition = exitPos;
        
        if (enableDebugLogs)
        {
            Debug.Log($"📍 {customerName} 위치 설정 완료");
        }
    }
    
    public void SetWaitTime(float waitTime)
    {
        maxWaitTime = waitTime;
        
        if (enableDebugLogs)
        {
            Debug.Log($"⏰ {customerName} 대기시간: {maxWaitTime:F1}초");
        }
    }
    
    // ===== 디버그 및 기즈모 =====
    
    void OnDrawGizmosSelected()
    {
        if (!showDebugGizmos) return;
        
        // 클릭 범위 - 🔧 DrawWireSphere 사용
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, clickRadius);
        
        // 이동 경로
        Gizmos.color = Color.blue;
        Vector3 enterPos = counterPosition + enterStartPosition;
        Vector3 exitPos = counterPosition + exitEndPosition;
        
        Gizmos.DrawLine(enterPos, counterPosition);
        Gizmos.DrawLine(counterPosition, exitPos);
        
        // 감정 아이콘 위치
        Gizmos.color = Color.magenta;
        Gizmos.DrawWireCube(transform.position + emotionIconOffset, Vector3.one * 0.5f);
    }
    
    void OnDestroy()
    {
        // 코루틴 정리
        StopAllCoroutines();
        
        // 이벤트 정리
        CleanupEventHandlers();
        
        // 감정 아이콘 정리
        HideAllEmotionIcons();
    }
    
    void OnApplicationPause(bool pauseStatus)
    {
        // 일시정지 시 감정 애니메이션 처리
        if (pauseStatus && emotionCoroutine != null)
        {
            StopCoroutine(emotionCoroutine);
        }
    }
}