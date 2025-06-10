// Assets/Scripts/CustomerManager.cs
// 임시 손님 관리 시스템 (테스트용)

using UnityEngine;
using UnityEngine.UI;

public class CustomerManager : MonoBehaviour
{
    [Header("손님 UI 설정")]
    public Button customer1Button;
    public Button customer2Button;
    public Button customer3Button;
    
    [Header("주문 표시 UI")]
    public Text customer1OrderText;
    public Text customer2OrderText;
    public Text customer3OrderText;
    
    [Header("손님 상태")]
    public PreparationUI.FillingType customer1WantedType = PreparationUI.FillingType.Sugar;
    public PreparationUI.FillingType customer2WantedType = PreparationUI.FillingType.Seed;
    public PreparationUI.FillingType customer3WantedType = PreparationUI.FillingType.Sugar;
    
    [Header("피드백")]
    public AudioClip orderCompleteSound;
    public GameObject orderCompleteEffect;
    
    void Start()
    {
        // 손님 버튼 클릭 이벤트 연결
        if (customer1Button != null)
        {
            customer1Button.onClick.AddListener(() => OnCustomerClicked(1, customer1WantedType));
        }
        
        if (customer2Button != null)
        {
            customer2Button.onClick.AddListener(() => OnCustomerClicked(2, customer2WantedType));
        }
        
        if (customer3Button != null)
        {
            customer3Button.onClick.AddListener(() => OnCustomerClicked(3, customer3WantedType));
        }
        
        // 주문 텍스트 업데이트
        UpdateOrderTexts();
        
        Debug.Log("손님 관리자 초기화 완료!");
    }
    
    /// <summary>
    /// 주문 텍스트 업데이트
    /// </summary>
    void UpdateOrderTexts()
    {
        if (customer1OrderText != null)
        {
            customer1OrderText.text = "주문: " + GetKoreanName(customer1WantedType);
        }
        
        if (customer2OrderText != null)
        {
            customer2OrderText.text = "주문: " + GetKoreanName(customer2WantedType);
        }
        
        if (customer3OrderText != null)
        {
            customer3OrderText.text = "주문: " + GetKoreanName(customer3WantedType);
        }
    }
    
    /// <summary>
    /// 호떡 타입의 한국어 이름 반환
    /// </summary>
    string GetKoreanName(PreparationUI.FillingType fillingType)
    {
        switch (fillingType)
        {
            case PreparationUI.FillingType.Sugar:
                return "설탕 호떡";
            case PreparationUI.FillingType.Seed:
                return "씨앗 호떡";
            default:
                return "알 수 없음";
        }
    }
    
    /// <summary>
    /// 손님이 클릭되었을 때
    /// </summary>
    void OnCustomerClicked(int customerNumber, PreparationUI.FillingType wantedType)
    {
        Debug.Log("손님 " + customerNumber + " 클릭됨! 주문: " + GetKoreanName(wantedType));
        
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
            ShowNoSelectionFeedback(customerNumber);
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
        if (selectedType == wantedType)
        {
            // 주문 성공!
            CompleteOrder(customerNumber, selectedType);
        }
        else
        {
            // 주문 실패
            RejectOrder(customerNumber, selectedType, wantedType);
        }
    }
    
    /// <summary>
    /// 주문 완료 처리
    /// </summary>
    void CompleteOrder(int customerNumber, PreparationUI.FillingType deliveredType)
    {
        Debug.Log("🎉 손님 " + customerNumber + "의 주문 완료! " + GetKoreanName(deliveredType) + " 전달");
        
        // 선택된 호떡을 손님에게 전달
        if (StackSalesCounter.Instance.DeliverSelectedHotteokToCustomer())
        {
            // 성공 피드백
            ShowOrderCompleteFeedback(customerNumber);
            
            // 새로운 주문 생성 (랜덤)
            GenerateNewOrder(customerNumber);
        }
    }
    
    /// <summary>
    /// 주문 거부 처리
    /// </summary>
    void RejectOrder(int customerNumber, PreparationUI.FillingType deliveredType, PreparationUI.FillingType wantedType)
    {
        Debug.Log("❌ 손님 " + customerNumber + "이 주문을 거부함! " + 
                  "전달: " + GetKoreanName(deliveredType) + ", 주문: " + GetKoreanName(wantedType));
        
        // 거부 피드백
        ShowOrderRejectFeedback(customerNumber);
        
        // 호떡 선택 해제 (다시 선택할 수 있도록)
        StackSalesCounter.Instance.DeselectHotteok();
    }
    
    /// <summary>
    /// 호떡 선택 안함 피드백
    /// </summary>
    void ShowNoSelectionFeedback(int customerNumber)
    {
        Debug.Log("💭 손님 " + customerNumber + ": 호떡을 먼저 선택해주세요!");
        
        // UI 피드백 (말풍선 등)
        Button customerButton = GetCustomerButton(customerNumber);
        if (customerButton != null)
        {
            StartCoroutine(BlinkButton(customerButton, Color.gray));
        }
    }
    
    /// <summary>
    /// 주문 완료 피드백
    /// </summary>
    void ShowOrderCompleteFeedback(int customerNumber)
    {
        // 성공 소리
        if (orderCompleteSound != null)
        {
            AudioSource.PlayClipAtPoint(orderCompleteSound, transform.position);
        }
        
        // 성공 이펙트
        if (orderCompleteEffect != null)
        {
            Button customerButton = GetCustomerButton(customerNumber);
            if (customerButton != null)
            {
                GameObject effect = Instantiate(orderCompleteEffect, customerButton.transform.position, Quaternion.identity);
                Destroy(effect, 2f);
            }
        }
        
        // 버튼 깜빡임 (초록색)
        Button button = GetCustomerButton(customerNumber);
        if (button != null)
        {
            StartCoroutine(BlinkButton(button, Color.green));
        }
        
        Debug.Log("💚 손님 " + customerNumber + " 만족!");
    }
    
    /// <summary>
    /// 주문 거부 피드백
    /// </summary>
    void ShowOrderRejectFeedback(int customerNumber)
    {
        // 버튼 깜빡임 (빨간색)
        Button customerButton = GetCustomerButton(customerNumber);
        if (customerButton != null)
        {
            StartCoroutine(BlinkButton(customerButton, Color.red));
        }
        
        Debug.Log("💔 손님 " + customerNumber + " 불만족...");
    }
    
    /// <summary>
    /// 새로운 주문 생성 (랜덤)
    /// </summary>
    void GenerateNewOrder(int customerNumber)
    {
        // 랜덤으로 새 주문 생성
        PreparationUI.FillingType newOrder = (Random.Range(0, 2) == 0) ? 
            PreparationUI.FillingType.Sugar : PreparationUI.FillingType.Seed;
        
        switch (customerNumber)
        {
            case 1:
                customer1WantedType = newOrder;
                break;
            case 2:
                customer2WantedType = newOrder;
                break;
            case 3:
                customer3WantedType = newOrder;
                break;
        }
        
        // 주문 텍스트 업데이트
        UpdateOrderTexts();
        
        Debug.Log("손님 " + customerNumber + "의 새 주문: " + GetKoreanName(newOrder));
    }
    
    /// <summary>
    /// 손님 버튼 가져오기
    /// </summary>
    Button GetCustomerButton(int customerNumber)
    {
        switch (customerNumber)
        {
            case 1: return customer1Button;
            case 2: return customer2Button;
            case 3: return customer3Button;
            default: return null;
        }
    }
    
    /// <summary>
    /// 버튼 깜빡임 효과
    /// </summary>
    System.Collections.IEnumerator BlinkButton(Button button, Color blinkColor)
    {
        if (button == null) yield break;
        
        Image buttonImage = button.GetComponent<Image>();
        if (buttonImage == null) yield break;
        
        Color originalColor = buttonImage.color;
        
        // 3번 깜빡임
        for (int i = 0; i < 3; i++)
        {
            buttonImage.color = blinkColor;
            yield return new WaitForSeconds(0.2f);
            buttonImage.color = originalColor;
            yield return new WaitForSeconds(0.2f);
        }
    }
}