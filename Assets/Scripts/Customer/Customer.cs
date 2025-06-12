// Assets/Scripts/Customer/Customer.cs
// 🎭 감정 아이콘 시스템 완전 통합 버전 - 생략 없는 전체 코드

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
    public float warningThreshold = 0.25f;     // 경고 시작 비율 (25%)
    public float exitDuration = 1.5f;          // 나가는 시간
    
    [Header("이동 설정")]
    public Vector3 enterStartPosition;         // 입장 시작 위치
    public Vector3 counterPosition;            // 카운터 위치
    public Vector3 exitEndPosition;            // 퇴장 끝 위치
    public float walkSpeed = 2.0f;             // 걷기 속도
    public float angryWalkSpeed = 4.0f;        // 화났을 때 걷기 속도
    
    [Header("점수 및 보상")]
    public int satisfactionRewardPerItem = 50; // 항목당 만족 점수
    public int angryPenalty = -50;             // 화남 시 감점
    public int bonusForCompleteOrder = 50;     // 전체 주문 완료 보너스
    
    [Header("🎭 감정 아이콘 시스템 설정")]
    public bool useEnhancedEmotions = true;    // 향상된 감정 시스템 사용 여부
    public bool enableEmotionSounds = true;    // 감정 사운드 활성화
    public bool enableEmotionDebug = false;    // 감정 디버그 로그
    public bool useAnimatorEmotions = false;   // 얼굴 스프라이트 시스템

    
    // 내부 상태
    private CustomerState currentState = CustomerState.Entering;
    private float currentWaitTime = 0f;
    private bool hasReceivedCompleteOrder = false;
    private bool isInitialized = false;
    private CustomerUI customerUI;                    // 기존 UI 시스템
    private CustomerUI_Enhanced enhancedUI;          // 🎭 새로운 감정 아이콘 시스템
    private CustomerAnimator customerAnimator;
    private CustomerSpawner parentSpawner;
    
    // 🎭 감정 상태 추적
    private string lastEmotionShown = "";
    private float lastEmotionTime = 0f;
    private bool isShowingWarningEmotion = false;
    private bool hasShownBoredEmotion = false;
    private bool hasShownAngryEmotion = false;
    
    // 컴포넌트
    private SpriteRenderer spriteRenderer;
    private Collider2D customerCollider;
    
    void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        customerCollider = GetComponent<Collider2D>();
        customerUI = GetComponent<CustomerUI>();
        customerAnimator = GetComponent<CustomerAnimator>();
        
        // 🎭 향상된 UI 시스템 초기화
        enhancedUI = GetComponent<CustomerUI_Enhanced>();
        if (enhancedUI == null && useEnhancedEmotions)
        {
            Debug.LogWarning($"⚠️ {gameObject.name}: CustomerUI_Enhanced 컴포넌트가 없습니다! 기본 UI만 사용됩니다.");
            useEnhancedEmotions = false;
        }
        
        // 초기 설정
        if (customerCollider != null)
        {
            customerCollider.enabled = false; // 들어올 때는 클릭 불가
        }
        
        DebugEmotion($"👤 Customer Awake 완료: {gameObject.name}");
    }
    
    /// <summary>
    /// 🔧 손님 초기화 (CustomerSpawner에서 호출)
    /// </summary>
    public void InitializeCustomer(int id, string name, CustomerSpawner spawner)
    {
        customerID = id;
        customerName = name;
        parentSpawner = spawner;
        
        // 🎨 스프라이트 선택 및 표시
        SelectAndShowRandomSprite();
        
        // 📝 랜덤 주문 생성
        GenerateRandomOrder();
        
        // 🎭 감정 시스템 초기화
        InitializeEmotionSystem();
        
        isInitialized = true;
        
        DebugEmotion($"✅ {customerName} 완전 초기화 완료!");
    }
    
    /// <summary>
    /// 🎭 감정 시스템 초기화
    /// </summary>
    void InitializeEmotionSystem()
    {
        lastEmotionShown = "";
        lastEmotionTime = 0f;
        isShowingWarningEmotion = false;
        hasShownBoredEmotion = false;
        hasShownAngryEmotion = false;
        
        if (useEnhancedEmotions && enhancedUI != null)
        {
            // 감정 시스템 설정 동기화
            enhancedUI.enableSounds = enableEmotionSounds;
            enhancedUI.enableUI = true;
            enhancedUI.enableAnimations = true;
            
            DebugEmotion("🎭 감정 시스템 초기화 완료");
        }
    }
    
    /// <summary>
    /// 🎨 스프라이트 선택 및 표시
    /// </summary>
    void SelectAndShowRandomSprite()
    {
        if (spriteRenderer == null)
        {
            Debug.LogError($"❌ {customerName}: SpriteRenderer가 null입니다!");
            return;
        }
        
        if (customerSprites == null || customerSprites.Length == 0)
        {
            Debug.LogWarning($"⚠️ {customerName}: customerSprites 배열이 비어있습니다! Inspector에서 손님 이미지를 설정해주세요.");
            
            // 🔧 기본 스프라이트로 표시 (디버그용 - 흰색 사각형)
            spriteRenderer.sprite = null;
            spriteRenderer.color = Color.red; // 빨간색으로 표시해서 보이게 함
            spriteRenderer.enabled = true;
            Debug.LogWarning($"🔧 {customerName}: 기본 스프라이트로 표시됨 (빨간색 사각형)");
            return;
        }
        
        // selectedSpriteIndex가 -1이면 랜덤 선택
        if (selectedSpriteIndex == -1)
        {
            selectedSpriteIndex = Random.Range(0, customerSprites.Length);
        }
        
        // 인덱스 범위 확인
        if (selectedSpriteIndex >= 0 && selectedSpriteIndex < customerSprites.Length)
        {
            Sprite selectedSprite = customerSprites[selectedSpriteIndex];
            
            if (selectedSprite != null)
            {
                spriteRenderer.sprite = selectedSprite;
                spriteRenderer.color = Color.white; // 정상 색상
                spriteRenderer.enabled = true;
                DebugEmotion($"🎨 {customerName}: 스프라이트 [{selectedSpriteIndex}] '{selectedSprite.name}' 적용됨");
            }
            else
            {
                Debug.LogError($"❌ {customerName}: customerSprites[{selectedSpriteIndex}]가 null입니다!");
                
                // 🔧 null 스프라이트라도 렌더러는 활성화
                spriteRenderer.sprite = null;
                spriteRenderer.color = Color.yellow; // 노란색으로 표시
                spriteRenderer.enabled = true;
                Debug.LogWarning($"🔧 {customerName}: null 스프라이트 - 노란색으로 표시됨");
            }
        }
        else
        {
            Debug.LogError($"❌ {customerName}: 잘못된 스프라이트 인덱스 {selectedSpriteIndex} (배열 크기: {customerSprites.Length})");
            
            // 🔧 인덱스 오류 시 첫 번째 스프라이트 사용
            if (customerSprites.Length > 0)
            {
                selectedSpriteIndex = 0;
                spriteRenderer.sprite = customerSprites[0];
                spriteRenderer.color = Color.white;
                spriteRenderer.enabled = true;
                Debug.LogWarning($"🔧 {customerName}: 첫 번째 스프라이트로 복구됨");
            }
        }
        
        // 🔍 최종 상태 확인
        DebugEmotion($"🔍 {customerName} 렌더러 최종 상태: enabled={spriteRenderer.enabled}, sprite={spriteRenderer.sprite?.name ?? "null"}, color={spriteRenderer.color}");
    }
    
    /// <summary>
    /// 📝 랜덤 주문 생성
    /// </summary>
    void GenerateRandomOrder()
    {
        orderItems.Clear();
        
        // 총 주문 개수 랜덤 결정
        int totalQuantity = Random.Range(minTotalQuantity, maxTotalQuantity + 1);
        
        // 사용 가능한 호떡 타입
        PreparationUI.FillingType[] availableTypes = {
            PreparationUI.FillingType.Sugar,
            PreparationUI.FillingType.Seed
        };
        
        // 랜덤하게 주문 생성
        int remainingQuantity = totalQuantity;
        
        while (remainingQuantity > 0)
        {
            // 랜덤 타입 선택
            PreparationUI.FillingType randomType = availableTypes[Random.Range(0, availableTypes.Length)];
            
            // 이미 해당 타입이 주문에 있는지 확인
            OrderItem existingItem = orderItems.Find(item => item.fillingType == randomType);
            
            if (existingItem != null)
            {
                // 기존 항목에 추가 (최대 3개까지)
                int addQuantity = Mathf.Min(Random.Range(1, remainingQuantity + 1), 3 - existingItem.quantity);
                existingItem.quantity += addQuantity;
                remainingQuantity -= addQuantity;
            }
            else
            {
                // 새로운 항목 추가
                int quantity = Mathf.Min(Random.Range(1, remainingQuantity + 1), remainingQuantity);
                orderItems.Add(new OrderItem(randomType, quantity));
                remainingQuantity -= quantity;
            }
            
            // 무한 루프 방지
            if (orderItems.Count >= availableTypes.Length)
            {
                // 마지막 항목에 남은 수량 모두 추가
                if (remainingQuantity > 0 && orderItems.Count > 0)
                {
                    orderItems[orderItems.Count - 1].quantity += remainingQuantity;
                    remainingQuantity = 0;
                }
                break;
            }
        }
        
        // 디버그 출력
        DebugEmotion($"📝 {customerName} 주문 생성: {GetOrderSummary()}");
    }
    
    void Start()
    {
        // 🔧 초기화가 완료된 후에만 여정 시작
        if (isInitialized)
        {
            StartCustomerJourney();
        }
        else
        {
            Debug.LogWarning($"⚠️ {gameObject.name} 초기화가 완료되지 않아 여정을 시작할 수 없습니다!");
        }
    }
    
    void Update()
    {
        UpdateCustomerState();
        
        // 🎛️ 개발자 모드 키보드 입력 (에디터에서만)
        if (Application.isEditor && enableEmotionDebug)
        {
            if (Input.GetKeyDown(KeyCode.Alpha1)) ShowHappiness();
            if (Input.GetKeyDown(KeyCode.Alpha2)) ShowAnger();
            if (Input.GetKeyDown(KeyCode.Alpha3)) ShowConfusion();
            if (Input.GetKeyDown(KeyCode.Alpha4)) ShowLove();
            if (Input.GetKeyDown(KeyCode.Alpha5)) ShowWarning();
            if (Input.GetKeyDown(KeyCode.Alpha6)) ShowStars();
            if (Input.GetKeyDown(KeyCode.Alpha7)) TestEmotionSequence();
        }
    }
    
    /// <summary>
    /// 손님의 여정 시작
    /// </summary>
    void StartCustomerJourney()
    {
        if (enterStartPosition == Vector3.zero)
        {
            Debug.LogError($"❌ {customerName}의 입장 위치가 설정되지 않았습니다!");
            return;
        }
        
        DebugEmotion($"👤 {customerName} (스프라이트 {selectedSpriteIndex}) 입장! 주문: {GetOrderSummary()}");
        DebugEmotion($"   현재 위치: {transform.position}");
        
        // 입장 애니메이션 시작
        ChangeState(CustomerState.Entering);
        StartCoroutine(EnterRestaurant());
    }
    
    /// <summary>
    /// 상태에 따른 업데이트 처리
    /// </summary>
    void UpdateCustomerState()
    {
        switch (currentState)
        {
            case CustomerState.Waiting:
                UpdateWaitingState();
                break;
                
            case CustomerState.Warning:
                UpdateWarningState();
                break;
        }
    }
    
    /// <summary>
    /// 🎭 대기 상태 업데이트 (감정 아이콘 포함)
    /// </summary>
    void UpdateWaitingState()
    {
        currentWaitTime += Time.deltaTime;
        float waitProgress = currentWaitTime / maxWaitTime;
        
        // UI 진행 바 업데이트
        if (customerUI != null)
        {
            customerUI.UpdateWaitProgress(waitProgress);
        }
        
        // 🎭 감정 아이콘 업데이트 (Enhanced UI)
        if (useEnhancedEmotions && enhancedUI != null)
        {
            enhancedUI.UpdateWaitProgress(waitProgress);
            
            // 대기 시간에 따른 감정 변화
            if (waitProgress > 0.6f && waitProgress < 0.65f && !hasShownBoredEmotion) // 60% 시점에서 한 번만
            {
                ShowEmotion("sleepy", 1.5f, "지루함 표시");
                hasShownBoredEmotion = true;
            }
        }
        
        // 경고 상태로 전환
        if (waitProgress >= warningThreshold && currentState == CustomerState.Waiting)
        {
            ChangeState(CustomerState.Warning);
        }
    }
    
    /// <summary>
    /// 🎭 경고 상태 업데이트 (감정 아이콘 포함)
    /// </summary>
    void UpdateWarningState()
    {
        currentWaitTime += Time.deltaTime;
        float waitProgress = currentWaitTime / maxWaitTime;
        
        // UI 진행 바 업데이트
        if (customerUI != null)
        {
            customerUI.UpdateWaitProgress(waitProgress);
        }
        
        // 🎭 감정 아이콘 업데이트 (Enhanced UI)
        if (useEnhancedEmotions && enhancedUI != null)
        {
            enhancedUI.UpdateWaitProgress(waitProgress);
            
            // 경고 단계에서 점점 화남 표시
            if (waitProgress > 0.9f && waitProgress < 0.95f && !hasShownAngryEmotion) // 90% 시점에서 한 번만
            {
                ShowEmotion("angry", 2f, "분노 전환");
                hasShownAngryEmotion = true;
            }
        }
        
        // 화내며 떠나기
        if (waitProgress >= 1.0f)
        {
            LeaveAngry();
        }
    }
    
    /// <summary>
    /// 🎭 상태 변경 (감정 아이콘 시스템 통합)
    /// </summary>
/// <summary>
/// 🎭 상태 변경 (CustomerUI_Enhanced 전용 버전)
/// </summary>
void ChangeState(CustomerState newState)
{
    CustomerState oldState = currentState;
    currentState = newState;
    
    DebugEmotion($"👤 {customerName} 상태 변경: {oldState} → {newState}");
    
    // CustomerUI_Enhanced가 없으면 기본 동작만
    if (!useEnhancedEmotions || enhancedUI == null)
    {
        DebugEmotion("⚠️ CustomerUI_Enhanced가 비활성화되어 기본 동작만 실행됩니다.");
        HandleBasicStateChange(newState);
        return;
    }
    
    // 🎭 CustomerUI_Enhanced 기반 상태 처리
    switch (newState)
    {
        case CustomerState.Entering:
            // 🎭 입장 시 중성 표정
            ShowEmotion("neutral", 1f, "입장");
            break;
            
        case CustomerState.Ordering:
            // 🎭 주문 시 기쁨 + 주문 말풍선
            enhancedUI.ShowOrderBubble(orderItems);
            ShowEmotion("happy", 1.5f, "주문 표시");
            break;
            
        case CustomerState.Waiting:
            // 🎭 대기 시 평온한 표정
            if (customerCollider != null)
            {
                customerCollider.enabled = true; // 클릭 가능하게
            }
            ShowEmotion("waiting", -1f, "대기 시작"); // 무한 표시
            break;
            
        case CustomerState.Warning:
            // 🎭 경고 시 경고 아이콘
            ShowEmotion("warning", -1f, "경고 상태"); // 경고 아이콘 (무한 표시)
            isShowingWarningEmotion = true;
            CustomerSpawner.Instance?.PlayWarningSound();
            break;
            
        case CustomerState.Satisfied:
            // 🎭 만족 시 감정 시퀀스
            if (customerCollider != null)
            {
                customerCollider.enabled = false; // 클릭 불가
            }
            
            // 🎭 만족 감정 시퀀스: 별점 → 사랑 → 만족
            string[] emotions = {"star", "heart", "satisfaction"};
            float[] durations = {1f, 1f, 1.5f};
            enhancedUI.ShowEmotionSequence(emotions, durations);
            enhancedUI.HideOrderBubble();
            
            DebugEmotion("🎭 만족 감정 시퀀스 시작");
            CustomerSpawner.Instance?.PlaySatisfactionSound();
            break;
            
        case CustomerState.Angry:
            // 🎭 분노 시 격분 표정
            if (customerCollider != null)
            {
                customerCollider.enabled = false; // 클릭 불가
            }
            
            ShowEmotion("furious", 3f, "격분 퇴장"); // 격분 아이콘
            CustomerSpawner.Instance?.PlayAngrySound();
            break;
            
        case CustomerState.Exiting:
            // 🎭 퇴장 시 모든 UI 숨기기
            enhancedUI.HideAllUI();
            isShowingWarningEmotion = false;
            break;
    }
}

/// <summary>
/// 🔧 기본 상태 변경 (CustomerUI_Enhanced 없을 때)
/// </summary>
void HandleBasicStateChange(CustomerState newState)
{
    switch (newState)
    {
        case CustomerState.Ordering:
            // 기존 UI 시스템 사용
            if (customerUI != null)
            {
                customerUI.ShowOrderBubble(orderItems);
            }
            break;
            
        case CustomerState.Waiting:
            if (customerCollider != null)
            {
                customerCollider.enabled = true;
            }
            break;
            
        case CustomerState.Warning:
            if (customerUI != null)
            {
                customerUI.ShowWarningIcon();
            }
            CustomerSpawner.Instance?.PlayWarningSound();
            break;
            
        case CustomerState.Satisfied:
            if (customerCollider != null)
            {
                customerCollider.enabled = false;
            }
            if (customerUI != null)
            {
                customerUI.ShowSatisfactionEffect();
                customerUI.HideOrderBubble();
                customerUI.HideWarningIcon();
            }
            CustomerSpawner.Instance?.PlaySatisfactionSound();
            break;
            
        case CustomerState.Angry:
            if (customerCollider != null)
            {
                customerCollider.enabled = false;
            }
            if (customerUI != null)
            {
                customerUI.ShowAngryEffect();
                customerUI.HideOrderBubble();
            }
            CustomerSpawner.Instance?.PlayAngrySound();
            break;
            
        case CustomerState.Exiting:
            if (customerUI != null)
            {
                customerUI.HideAllUI();
            }
            isShowingWarningEmotion = false;
            break;
    }
}
    
    /// <summary>
    /// 가게로 들어오는 애니메이션
    /// </summary>
    IEnumerator EnterRestaurant()
    {
        // 입장 효과음
        CustomerSpawner.Instance?.PlayEnterSound();
        
        Vector3 startPos = enterStartPosition;
        Vector3 endPos = counterPosition;
        
        float elapsedTime = 0f;
        
        while (elapsedTime < enterDuration)
        {
            elapsedTime += Time.deltaTime;
            float t = elapsedTime / enterDuration;
            
            // 부드러운 이동
            transform.position = Vector3.Lerp(startPos, endPos, t);
            
            yield return null;
        }
        
        transform.position = endPos;
        
        // 잠시 대기 후 주문 표시
        yield return new WaitForSeconds(orderDisplayDelay);
        
        ChangeState(CustomerState.Ordering);
        
        // 주문 표시 후 대기 시작
        yield return new WaitForSeconds(0.5f);
        ChangeState(CustomerState.Waiting);
    }
    
    /// <summary>
    /// 손님 클릭 처리
    /// </summary>
    void OnMouseDown()
    {
        if (currentState != CustomerState.Waiting && currentState != CustomerState.Warning)
        {
            return;
        }
        
        DebugEmotion($"👤 {customerName} 클릭됨!");
        
        // 선택된 호떡이 있는지 확인
        if (StackSalesCounter.Instance == null)
        {
            Debug.LogError("StackSalesCounter가 없습니다!");
            return;
        }
        
        GameObject selectedHotteok = StackSalesCounter.Instance.GetSelectedHotteok();
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
    /// 📝 호떡 수령 처리 (올바른 주문)
    /// </summary>
    // Customer.cs에서 수정할 부분
// ReceiveHotteok 메서드에 골드 지급 로직 추가

/// <summary>
/// 📝 호떡 수령 처리 (올바른 주문) - 골드 시스템 연동
/// </summary>
void ReceiveHotteok(PreparationUI.FillingType receivedType)
{
    // 해당 타입의 주문 항목 찾기
    OrderItem orderItem = orderItems.Find(item => item.fillingType == receivedType && !item.IsCompleted());
    
    if (orderItem != null)
    {
        orderItem.receivedQuantity++;
        
        Debug.Log($"✅ {customerName} {GetHotteokName(receivedType)} 1개 수령! " +
                 $"({orderItem.receivedQuantity}/{orderItem.quantity}) | 진행: {GetOrderProgress()}");
        
        // 선택된 호떡을 손님에게 전달
        if (StackSalesCounter.Instance.DeliverSelectedHotteokToCustomer())
        {
            // 💰 골드 지급 처리 (새로 추가된 부분)
            if (GoldManager.Instance != null)
            {
                GoldManager.Instance.ProcessHotteokSale(receivedType);
            }
            else
            {
                Debug.LogWarning("⚠️ GoldManager가 없어 골드를 지급할 수 없습니다!");
            }
            
            // 점수 추가 (기존 시스템 유지)
            GameManager.Instance?.AddScore(satisfactionRewardPerItem);
            
            // UI 업데이트
            if (customerUI != null)
            {
                customerUI.UpdateOrderProgress(orderItems);
            }
            
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
    /// 📝 전체 주문 완료 처리
    /// </summary>
    void CompleteEntireOrder()
    {
        hasReceivedCompleteOrder = true;
        
        // 보너스 점수
        GameManager.Instance?.AddScore(bonusForCompleteOrder);
        
        DebugEmotion($"🎉 {customerName} 전체 주문 완료! 보너스 +{bonusForCompleteOrder}점");
        
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
        
        if (customerUI != null)
        {
            customerUI.ShowPartialCompletionFeedback(message);
        }
    }
    
    /// <summary>
    /// 🎭 잘못된 주문 수령 (감정 아이콘 포함)
    /// </summary>
    void ReceiveWrongOrder(PreparationUI.FillingType receivedType)
    {
        DebugEmotion($"❌ {customerName} 잘못된 주문! 받음: {GetHotteokName(receivedType)}, 주문: {GetOrderSummary()}");
        
        // 호떡 선택 해제 (다시 선택할 수 있도록)
        StackSalesCounter.Instance.DeselectHotteok();
        
        // 🎭 혼란 아이콘 표시
        ShowEmotion("confused", 2f, "잘못된 주문");
        
        if (useEnhancedEmotions && enhancedUI != null) 
        {
            enhancedUI.ShowWrongOrderFeedback();
        }
        
        if (customerUI != null)
        {
            customerUI.ShowWrongOrderFeedback();
        }
        if (customerAnimator != null)
        {
            customerAnimator.PlayRejectAnimation();
        }
    }
    
    /// <summary>
    /// 🎭 호떡 선택 안함 피드백 (감정 아이콘 포함)
    /// </summary>
    void ShowNoSelectionFeedback()
    {
        DebugEmotion($"💭 {customerName}: 호떡을 먼저 선택해주세요!");
        
        // 🎭 생각 아이콘 표시
        ShowEmotion("thinking", 1.5f, "호떡 선택 안함");
        
        if (useEnhancedEmotions && enhancedUI != null) 
        {
            enhancedUI.ShowNoSelectionFeedback();
        }
        
        if (customerUI != null)
        {
            customerUI.ShowNoSelectionFeedback();
        }
        if (customerAnimator != null)
        {
            customerAnimator.PlayConfusedAnimation();
        }
    }
    
    /// <summary>
    /// 만족하며 떠나기
    /// </summary>
    public void LeaveSatisfied()
    {
        ChangeState(CustomerState.Satisfied);
        StartCoroutine(ExitRestaurant(false));
    }
    
    /// <summary>
    /// 화내며 떠나기
    /// </summary>
    public void LeaveAngry()
    {
        ChangeState(CustomerState.Angry);
        
        // 감점
        GameManager.Instance?.AddScore(angryPenalty);
        
        StartCoroutine(ExitRestaurant(true));
    }
    
    /// <summary>
    /// 가게에서 나가는 애니메이션
    /// </summary>
    IEnumerator ExitRestaurant(bool isAngry)
    {
        ChangeState(CustomerState.Exiting);
        
        Vector3 startPos = transform.position;
        Vector3 endPos = exitEndPosition;
        
        float currentExitDuration = exitDuration;
        float currentSpeed = walkSpeed;
        
        if (isAngry)
        {
            currentExitDuration *= 0.6f; // 더 빨리 나감
            currentSpeed = angryWalkSpeed;
        }
        
        float elapsedTime = 0f;
        
        while (elapsedTime < currentExitDuration)
        {
            elapsedTime += Time.deltaTime;
            float t = elapsedTime / currentExitDuration;
            
            // 화난 경우 더 빠른 속도
            if (isAngry)
            {
                t = Mathf.Pow(t, 0.7f); // 가속도 적용
            }
            
            transform.position = Vector3.Lerp(startPos, endPos, t);
            
            yield return null;
        }
        
        // 완전히 퇴장 완료
        OnExitComplete(isAngry);
    }
    
    /// <summary>
    /// 퇴장 완료 처리
    /// </summary>
    void OnExitComplete(bool wasAngry)
    {
        if (wasAngry)
        {
            DebugEmotion($"😡 {customerName} (스프라이트 {selectedSpriteIndex}) 화내며 퇴장함... 미완료 주문: {GetOrderProgress()}");
        }
        else
        {
            DebugEmotion($"😊 {customerName} (스프라이트 {selectedSpriteIndex}) 만족하며 퇴장함! 완료된 주문: {GetOrderSummary()}");
        }
        
        // 스포너에게 알림
        if (parentSpawner != null)
        {
            parentSpawner.OnCustomerExit(this, !wasAngry);
        }
        
        // 객체 제거
        Destroy(gameObject);
    }
    
    // ============= 🎭 감정 아이콘 시스템 핵심 함수들 =============
    
    /// <summary>
    /// 🎭 감정 표시 (통합 함수)
    /// </summary>
    void ShowEmotion(string emotionKey, float duration = 2f, string context = "")
    {
        if (!useEnhancedEmotions || enhancedUI == null) return;
        
        // 중복 감정 방지
        if (lastEmotionShown == emotionKey && Time.time - lastEmotionTime < 1f)
        {
            return;
        }
        
        lastEmotionShown = emotionKey;
        lastEmotionTime = Time.time;
        
        // 감정 아이콘 표시
        enhancedUI.ShowEmotionIcon(emotionKey, duration, enableEmotionSounds);
        
        DebugEmotion($"🎭 감정 표시: {emotionKey} (지속: {duration}초) - {context}");
    }
    
    /// <summary>
    /// 🎭 감정 디버그 로그
    /// </summary>
    void DebugEmotion(string message)
    {
        if (enableEmotionDebug)
        {
            Debug.Log($"[{customerName}] {message}");
        }
    }
    
    // ============= 🛠️ 감정 시스템 편의 함수들 =============
    
    /// <summary>
    /// 🎭 빠른 감정 표시 함수들
    /// </summary>
    public void ShowHappiness() => ShowEmotion("happy", 1.5f, "수동 호출");
    public void ShowSatisfaction() => ShowEmotion("satisfaction", 2f, "수동 호출");
    public void ShowAnger() => ShowEmotion("angry", 2f, "수동 호출");
    public void ShowFury() => ShowEmotion("furious", 3f, "수동 호출");
    public void ShowWarning() => ShowEmotion("warning", -1f, "수동 호출");
    public void ShowConfusion() => ShowEmotion("confused", 1.5f, "수동 호출");
    public void ShowThinking() => ShowEmotion("thinking", 2f, "수동 호출");
    public void ShowLove() => ShowEmotion("heart", 1.5f, "수동 호출");
    public void ShowStars() => ShowEmotion("star", 2f, "수동 호출");
    
    /// <summary>
    /// 🎭 감정 시스템 설정 함수들
    /// </summary>
    public void EnableEmotionSystem(bool enable)
    {
        useEnhancedEmotions = enable;
        if (enhancedUI != null)
        {
            enhancedUI.SetUIEnabled(enable);
        }
        DebugEmotion($"🎭 감정 시스템 {(enable ? "활성화" : "비활성화")}");
    }
    
    public void EnableEmotionSounds(bool enable)
    {
        enableEmotionSounds = enable;
        if (enhancedUI != null)
        {
            enhancedUI.enableSounds = enable;
        }
        DebugEmotion($"🔊 감정 사운드 {(enable ? "활성화" : "비활성화")}");
    }
    
    public void EnableEmotionDebug(bool enable)
    {
        enableEmotionDebug = enable;
        DebugEmotion($"🐛 감정 디버그 {(enable ? "활성화" : "비활성화")}");
    }
    
    // ===== 기존 유틸리티 함수들 (변경 없음) =====
    
    public void SetSpawner(CustomerSpawner spawner)
    {
        parentSpawner = spawner;
    }
    
    public void SetPositions(Vector3 enterPos, Vector3 counterPos, Vector3 exitPos)
    {
        enterStartPosition = enterPos;
        counterPosition = counterPos;
        exitEndPosition = exitPos;
        
        DebugEmotion($"📍 {customerName} 위치 설정됨:");
        DebugEmotion($"   입장: {enterStartPosition}");
        DebugEmotion($"   카운터: {counterPosition}");
        DebugEmotion($"   퇴장: {exitEndPosition}");
    }
    
    public string GetOrderSummary()
    {
        if (orderItems.Count == 0) return "주문 없음";
        
        string summary = "";
        for (int i = 0; i < orderItems.Count; i++)
        {
            OrderItem item = orderItems[i];
            string itemName = GetHotteokName(item.fillingType);
            summary += $"{itemName} {item.quantity}개";
            
            if (i < orderItems.Count - 1)
            {
                summary += ", ";
            }
        }
        return summary;
    }
    
    public string GetOrderProgress()
    {
        if (orderItems.Count == 0) return "주문 없음";
        
        string progress = "";
        for (int i = 0; i < orderItems.Count; i++)
        {
            OrderItem item = orderItems[i];
            string itemName = GetHotteokName(item.fillingType);
            progress += $"{itemName} {item.receivedQuantity}/{item.quantity}";
            
            if (i < orderItems.Count - 1)
            {
                progress += ", ";
            }
        }
        return progress;
    }
    
    public bool IsOrderComplete()
    {
        if (orderItems.Count == 0) return false;
        
        foreach (OrderItem item in orderItems)
        {
            if (!item.IsCompleted())
            {
                return false;
            }
        }
        return true;
    }
    
    public bool HasOrderedType(PreparationUI.FillingType type)
    {
        return orderItems.Find(item => item.fillingType == type && !item.IsCompleted()) != null;
    }
    
    public int GetRemainingQuantity(PreparationUI.FillingType type)
    {
        OrderItem item = orderItems.Find(i => i.fillingType == type);
        return item?.GetRemainingQuantity() ?? 0;
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
    
    public CustomerState GetCurrentState()
    {
        return currentState;
    }
    
    public float GetWaitProgress()
    {
        return Mathf.Clamp01(currentWaitTime / maxWaitTime);
    }
    
    public List<OrderItem> GetOrderItems()
    {
        return new List<OrderItem>(orderItems);
    }
    
    // ============= 🛠️ 디버그 및 테스트 함수들 =============
    
    /// <summary>
    /// 🎭 에디터 테스트 함수들 (Context Menu)
    /// </summary>
    [ContextMenu("🎭 Test Happy Emotion")]
    public void TestHappyEmotion() => ShowHappiness();
    
    [ContextMenu("🎭 Test Angry Emotion")]
    public void TestAngryEmotion() => ShowAnger();
    
    [ContextMenu("🎭 Test Confusion Emotion")]
    public void TestConfusionEmotion() => ShowConfusion();
    
    [ContextMenu("🎭 Test Love Emotion")]
    public void TestLoveEmotion() => ShowLove();
    
    [ContextMenu("🎭 Test Star Emotion")]
    public void TestStarEmotion() => ShowStars();
    
    [ContextMenu("🎭 Test Emotion Sequence")]
    public void TestEmotionSequence()
    {
        if (useEnhancedEmotions && enhancedUI != null)
        {
            string[] emotions = {"happy", "thinking", "satisfaction", "heart"};
            float[] durations = {1f, 1f, 1f, 2f};
            enhancedUI.ShowEmotionSequence(emotions, durations);
            DebugEmotion("🎭 감정 시퀀스 테스트 실행");
        }
    }
    
    [ContextMenu("🐛 Print Debug Info")]
    public void PrintDebugInfo()
    {
        Debug.Log("=== Customer Debug Info ===");
        Debug.Log($"👤 손님: {customerName} (ID: {customerID})");
        Debug.Log($"🎭 감정 시스템: {(useEnhancedEmotions ? "활성화" : "비활성화")}");
        Debug.Log($"🔊 감정 사운드: {(enableEmotionSounds ? "활성화" : "비활성화")}");
        Debug.Log($"🐛 감정 디버그: {(enableEmotionDebug ? "활성화" : "비활성화")}");
        Debug.Log($"📊 현재 상태: {currentState}");
        Debug.Log($"⏰ 대기 시간: {currentWaitTime:F1}초 / {maxWaitTime}초");
        Debug.Log($"📝 주문: {GetOrderSummary()}");
        Debug.Log($"📈 진행: {GetOrderProgress()}");
        Debug.Log($"🎭 마지막 감정: {lastEmotionShown} ({Time.time - lastEmotionTime:F1}초 전)");
        Debug.Log($"⚠️ 경고 표시 중: {isShowingWarningEmotion}");
        Debug.Log($"💤 지루함 표시됨: {hasShownBoredEmotion}");
        Debug.Log($"😡 분노 표시됨: {hasShownAngryEmotion}");
        
        // Enhanced UI 상태
        if (enhancedUI != null)
        {
            Debug.Log($"🎭 Enhanced UI: 활성화됨");
        }
        else
        {
            Debug.Log($"❌ Enhanced UI: 없음");
        }
    }
}