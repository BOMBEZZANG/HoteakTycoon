// Assets/Scripts/Customer/Customer.cs
// 개별 손님의 상태 및 행동을 관리하는 핵심 클래스 (다중 주문 시스템)

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
    
    [Header("🎨 랜덤 스프라이트 시스템")]
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
    
    // 내부 상태
    private CustomerState currentState = CustomerState.Entering;
    private float currentWaitTime = 0f;
    private bool hasReceivedCompleteOrder = false;
    private CustomerUI customerUI;
    private CustomerAnimator customerAnimator;
    private CustomerSpawner parentSpawner;
    
    // 컴포넌트
    private SpriteRenderer spriteRenderer;
    private Collider2D customerCollider;
    
    void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        customerCollider = GetComponent<Collider2D>();
        customerUI = GetComponent<CustomerUI>();
        customerAnimator = GetComponent<CustomerAnimator>();
        
        // 초기 설정
        if (customerCollider != null)
        {
            customerCollider.enabled = false; // 들어올 때는 클릭 불가
        }
        
        // 🎨 랜덤 스프라이트 선택
        SelectRandomSprite();
    }
    
    /// <summary>
    /// 🎨 랜덤 스프라이트 선택 및 적용
    /// </summary>
    void SelectRandomSprite()
    {
        if (customerSprites == null || customerSprites.Length == 0)
        {
            Debug.LogWarning("⚠️ customerSprites 배열이 비어있습니다! Inspector에서 손님 이미지를 설정해주세요.");
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
            if (spriteRenderer != null && customerSprites[selectedSpriteIndex] != null)
            {
                spriteRenderer.sprite = customerSprites[selectedSpriteIndex];
                Debug.Log($"🎨 손님 {customerID}: 스프라이트 [{selectedSpriteIndex}] 적용됨");
            }
            else
            {
                Debug.LogError($"❌ 스프라이트 또는 SpriteRenderer가 null입니다! 인덱스: {selectedSpriteIndex}");
            }
        }
        else
        {
            Debug.LogError($"❌ 잘못된 스프라이트 인덱스: {selectedSpriteIndex} (배열 크기: {customerSprites.Length})");
        }
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
        Debug.Log($"📝 {customerName} 주문 생성: {GetOrderSummary()}");
    }
    
    /// <summary>
    /// 📝 주문 요약 텍스트 생성
    /// </summary>
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
    
    /// <summary>
    /// 📝 주문 진행 상황 텍스트 생성
    /// </summary>
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
    
    /// <summary>
    /// 📝 전체 주문이 완료되었는지 확인
    /// </summary>
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
    
    /// <summary>
    /// 📝 특정 타입의 호떡을 주문했는지 확인
    /// </summary>
    public bool HasOrderedType(PreparationUI.FillingType type)
    {
        return orderItems.Find(item => item.fillingType == type && !item.IsCompleted()) != null;
    }
    
    /// <summary>
    /// 📝 특정 타입의 남은 주문 개수 반환
    /// </summary>
    public int GetRemainingQuantity(PreparationUI.FillingType type)
    {
        OrderItem item = orderItems.Find(i => i.fillingType == type);
        return item?.GetRemainingQuantity() ?? 0;
    }
    
    /// <summary>
    /// 🎨 특정 스프라이트로 설정 (외부에서 호출 가능)
    /// </summary>
    public void SetCustomerSprite(int spriteIndex)
    {
        selectedSpriteIndex = spriteIndex;
        SelectRandomSprite();
    }
    
    /// <summary>
    /// 🎨 현재 선택된 스프라이트 인덱스 반환
    /// </summary>
    public int GetSelectedSpriteIndex()
    {
        return selectedSpriteIndex;
    }
    
    void Start()
    {
        StartCustomerJourney();
    }
    
    void Update()
    {
        UpdateCustomerState();
    }
    
    /// <summary>
    /// 손님의 여정 시작
    /// </summary>
    void StartCustomerJourney()
    {
        // 입장 위치에서 시작
        transform.position = enterStartPosition;
        
        // 📝 랜덤 주문 생성
        GenerateRandomOrder();
        
        Debug.Log($"👤 {customerName} (스프라이트 {selectedSpriteIndex}) 입장! 주문: {GetOrderSummary()}");
        
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
    /// 대기 상태 업데이트
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
        
        // 경고 상태로 전환
        if (waitProgress >= warningThreshold && currentState == CustomerState.Waiting)
        {
            ChangeState(CustomerState.Warning);
        }
    }
    
    /// <summary>
    /// 경고 상태 업데이트
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
        
        // 화내며 떠나기
        if (waitProgress >= 1.0f)
        {
            LeaveAngry();
        }
    }
    
    /// <summary>
    /// 상태 변경
    /// </summary>
    void ChangeState(CustomerState newState)
    {
        CustomerState oldState = currentState;
        currentState = newState;
        
        Debug.Log($"👤 {customerName} 상태 변경: {oldState} → {newState}");
        
        // 상태별 처리
        switch (newState)
        {
            case CustomerState.Entering:
                break;
                
            case CustomerState.Ordering:
                if (customerUI != null)
                {
                    customerUI.ShowOrderBubble(orderItems);
                }
                if (customerAnimator != null)
                {
                    customerAnimator.PlayOrderingAnimation();
                }
                break;
                
            case CustomerState.Waiting:
                if (customerCollider != null)
                {
                    customerCollider.enabled = true; // 클릭 가능하게
                }
                if (customerAnimator != null)
                {
                    customerAnimator.PlayWaitingAnimation();
                }
                break;
                
            case CustomerState.Warning:
                if (customerUI != null)
                {
                    customerUI.ShowWarningIcon();
                }
                if (customerAnimator != null)
                {
                    customerAnimator.PlayWarningAnimation();
                }
                CustomerSpawner.Instance?.PlayWarningSound();
                break;
                
            case CustomerState.Satisfied:
                if (customerCollider != null)
                {
                    customerCollider.enabled = false; // 클릭 불가
                }
                if (customerUI != null)
                {
                    customerUI.ShowSatisfactionEffect();
                    customerUI.HideOrderBubble();
                    customerUI.HideWarningIcon();
                }
                if (customerAnimator != null)
                {
                    customerAnimator.PlaySatisfiedAnimation();
                }
                CustomerSpawner.Instance?.PlaySatisfactionSound();
                break;
                
            case CustomerState.Angry:
                if (customerCollider != null)
                {
                    customerCollider.enabled = false; // 클릭 불가
                }
                if (customerUI != null)
                {
                    customerUI.ShowAngryEffect();
                    customerUI.HideOrderBubble();
                }
                if (customerAnimator != null)
                {
                    customerAnimator.PlayAngryAnimation();
                }
                CustomerSpawner.Instance?.PlayAngrySound();
                break;
                
            case CustomerState.Exiting:
                if (customerUI != null)
                {
                    customerUI.HideAllUI();
                }
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
        
        Debug.Log($"👤 {customerName} 클릭됨!");
        
        // 선택된 호떡이 있는지 확인
        if (StackSalesCounter.Instance == null)
        {
            Debug.LogError("StackSalesCounter가 없습니다!");
            return;
        }
        
        GameObject selectedHotteok = StackSalesCounter.Instance.GetSelectedHotteok();
        if (selectedHotteok == null)
        {
            Debug.Log("선택된 호떡이 없습니다! 먼저 판매대에서 호떡을 선택하세요.");
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
                // 점수 추가 (항목당)
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
        
        Debug.Log($"🎉 {customerName} 전체 주문 완료! 보너스 +{bonusForCompleteOrder}점");
        
        // 만족하며 떠나기
        LeaveSatisfied();
    }
    
    /// <summary>
    /// 📝 부분 완료 피드백
    /// </summary>
    void ShowPartialCompletionFeedback(PreparationUI.FillingType receivedType)
    {
        if (customerUI != null)
        {
            string message = $"{GetHotteokName(receivedType)} 감사해요! 🙂";
            customerUI.ShowPartialCompletionFeedback(message);
        }
    }
    
    /// <summary>
    /// 잘못된 주문 수령
    /// </summary>
    void ReceiveWrongOrder(PreparationUI.FillingType receivedType)
    {
        Debug.Log($"❌ {customerName} 잘못된 주문! 받음: {GetHotteokName(receivedType)}, 주문: {GetOrderSummary()}");
        
        // 호떡 선택 해제 (다시 선택할 수 있도록)
        StackSalesCounter.Instance.DeselectHotteok();
        
        // 화남 피드백
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
    /// 호떡 선택 안함 피드백
    /// </summary>
    void ShowNoSelectionFeedback()
    {
        Debug.Log($"💭 {customerName}: 호떡을 먼저 선택해주세요!");
        
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
            Debug.Log($"😡 {customerName} (스프라이트 {selectedSpriteIndex}) 화내며 퇴장함... 미완료 주문: {GetOrderProgress()}");
        }
        else
        {
            Debug.Log($"😊 {customerName} (스프라이트 {selectedSpriteIndex}) 만족하며 퇴장함! 완료된 주문: {GetOrderSummary()}");
        }
        
        // 스포너에게 알림
        if (parentSpawner != null)
        {
            parentSpawner.OnCustomerExit(this, !wasAngry);
        }
        
        // 객체 제거
        Destroy(gameObject);
    }
    
    /// <summary>
    /// 스포너 설정
    /// </summary>
    public void SetSpawner(CustomerSpawner spawner)
    {
        parentSpawner = spawner;
    }
    
    /// <summary>
    /// 위치 설정
    /// </summary>
    public void SetPositions(Vector3 enterPos, Vector3 counterPos, Vector3 exitPos)
    {
        enterStartPosition = enterPos;
        counterPosition = counterPos;
        exitEndPosition = exitPos;
    }
    
    /// <summary>
    /// 호떡 이름 반환
    /// </summary>
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
    
    /// <summary>
    /// 현재 상태 반환
    /// </summary>
    public CustomerState GetCurrentState()
    {
        return currentState;
    }
    
    /// <summary>
    /// 대기 진행도 반환 (0~1)
    /// </summary>
    public float GetWaitProgress()
    {
        return Mathf.Clamp01(currentWaitTime / maxWaitTime);
    }
    
    /// <summary>
    /// 📝 주문 항목 리스트 반환 (UI에서 사용)
    /// </summary>
    public List<OrderItem> GetOrderItems()
    {
        return new List<OrderItem>(orderItems);
    }
}