// Assets/Scripts/Customer/Customer.cs
// 개별 손님의 상태 및 행동을 관리하는 핵심 클래스

using UnityEngine;
using System.Collections;

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
    
    [Header("손님 기본 정보")]
    public int customerID;
    public string customerName = "손님";
    
    [Header("주문 정보")]
    public PreparationUI.FillingType orderedType;
    public int orderedQuantity = 1; // 주문 개수 (확장 가능)
    
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
    public int satisfactionReward = 100;       // 만족 시 점수
    public int angryPenalty = -50;             // 화남 시 감점
    
    // 내부 상태
    private CustomerState currentState = CustomerState.Entering;
    private float currentWaitTime = 0f;
    private bool hasReceivedOrder = false;
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
        
        // 주문 타입 랜덤 생성 (임시)
        orderedType = (Random.Range(0, 2) == 0) ? 
            PreparationUI.FillingType.Sugar : PreparationUI.FillingType.Seed;
        
        Debug.Log($"👤 {customerName} 입장! 주문: {GetOrderName()}");
        
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
                    customerUI.ShowOrderBubble(orderedType, orderedQuantity);
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
        
        // 주문과 일치하는지 확인
        if (selectedType == orderedType)
        {
            ReceiveCorrectOrder();
        }
        else
        {
            ReceiveWrongOrder(selectedType);
        }
    }
    
    /// <summary>
    /// 올바른 주문 수령
    /// </summary>
    void ReceiveCorrectOrder()
    {
        Debug.Log($"🎉 {customerName} 주문 성공! {GetOrderName()} 전달");
        
        hasReceivedOrder = true;
        
        // 선택된 호떡을 손님에게 전달
        if (StackSalesCounter.Instance.DeliverSelectedHotteokToCustomer())
        {
            // 점수 추가
            GameManager.Instance?.AddScore(satisfactionReward);
            
            // 만족하며 떠나기
            LeaveSatisfied();
        }
    }
    
    /// <summary>
    /// 잘못된 주문 수령
    /// </summary>
    void ReceiveWrongOrder(PreparationUI.FillingType receivedType)
    {
        Debug.Log($"❌ {customerName} 주문 실패! 주문: {GetOrderName()}, 받음: {GetHotteokName(receivedType)}");
        
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
            Debug.Log($"😡 {customerName} 화내며 퇴장함...");
        }
        else
        {
            Debug.Log($"😊 {customerName} 만족하며 퇴장함!");
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
    /// 주문명 반환
    /// </summary>
    string GetOrderName()
    {
        return GetHotteokName(orderedType);
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
}