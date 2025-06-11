// Assets/Scripts/Customer/CustomerUI.cs
// 손님의 말풍선, 주문 표시 등 UI를 관리하는 클래스 (다중 주문 지원)

using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using TMPro;

public class CustomerUI : MonoBehaviour
{
    [Header("UI 컨테이너")]
    public Canvas worldCanvas;                  // World Space Canvas
    public GameObject uiContainer;              // 모든 UI의 부모
    
    [Header("📝 주문 표시 (간단 버전)")]
    public GameObject orderBubble;              // 주문 말풍선 전체
    public TextMeshProUGUI orderText;          // 주문 텍스트 (간단 표시)
    public Image bubbleBackground;              // 말풍선 배경
    
    [Header("진행 상태")]
    public Slider waitProgressSlider;           // 대기 진행 바
    public Image progressFillImage;             // 진행 바 채우기 이미지
    public Color normalProgressColor = Color.green;                    // 평상시 색상
    public Color warningProgressColor = new Color(1f, 0.5f, 0f, 1f);  // 경고 시 색상 (주황색)
    public Color dangerProgressColor = Color.red;                      // 위험 시 색상
    
    [Header("감정 아이콘")]
    public GameObject warningIcon;              // 화남 아이콘 (♨️ 💢)
    public GameObject satisfactionIcon;         // 만족 아이콘 (❤️ 😊)
    public GameObject angryIcon;                // 분노 아이콘 (💥 🤬)
    
    [Header("피드백 텍스트")]
    public GameObject feedbackTextObject;       // 피드백 텍스트 컨테이너
    public TextMeshProUGUI feedbackText;       // 피드백 텍스트
    public float feedbackDisplayTime = 2.0f;   // 피드백 표시 시간
    
    [Header("🎨 주문 표시 스타일")]
    public Color orderTextColor = Color.black;             // 주문 텍스트 색상
    public Color completedTextColor = Color.green;         // 완료된 항목 색상
    public float orderTextSize = 14f;                      // 주문 텍스트 크기
    
    [Header("애니메이션 설정")]
    public float bubblePopDuration = 0.3f;     // 말풍선 팝업 시간
    public float iconPulseDuration = 0.5f;     // 아이콘 맥박 시간
    public AnimationCurve popCurve = new AnimationCurve(
        new Keyframe(0f, 0f, 0f, 0f),
        new Keyframe(0.5f, 1.1f, 2f, 2f),
        new Keyframe(1f, 1f, 0f, 0f)
    ); // EaseOutBack 효과를 흉내낸 커브
    
    [Header("🐛 디버그")]
    public bool enableUI = true;               // UI 활성화 여부
    
    // 내부 상태
    private bool isInitialized = false;
    private Coroutine feedbackCoroutine;
    private Coroutine warningPulseCoroutine;
    private List<Customer.OrderItem> currentOrder = new List<Customer.OrderItem>();
    
    void Awake()
    {
        InitializeUI();
    }
    
    void Start()
    {
        // 카메라를 향하도록 설정
        if (worldCanvas != null)
        {
            worldCanvas.worldCamera = Camera.main;
        }
        
        HideAllUI();
    }
    
    void LateUpdate()
    {
        // UI가 항상 카메라를 향하도록
        if (enableUI && worldCanvas != null && Camera.main != null)
        {
            worldCanvas.transform.LookAt(Camera.main.transform);
            worldCanvas.transform.Rotate(0, 180, 0); // 뒤집힌 상태 보정
        }
    }
    
    /// <summary>
    /// UI 초기화
    /// </summary>
    void InitializeUI()
    {
        // Canvas 자동 생성 (없을 경우)
        if (worldCanvas == null)
        {
            CreateWorldCanvas();
        }
        
        // 진행 바 색상 초기화
        if (progressFillImage != null)
        {
            progressFillImage.color = normalProgressColor;
        }
        
        // 주문 텍스트 초기화
        if (orderText != null)
        {
            orderText.color = orderTextColor;
            orderText.fontSize = orderTextSize;
        }
        
        isInitialized = true;
    }
    
    /// <summary>
    /// World Space Canvas 생성
    /// </summary>
    void CreateWorldCanvas()
    {
        if (!enableUI) return;
        
        GameObject canvasObj = new GameObject("CustomerUI_Canvas");
        canvasObj.transform.SetParent(transform);
        canvasObj.transform.localPosition = Vector3.up * 1.5f; // 손님 머리 위
        
        worldCanvas = canvasObj.AddComponent<Canvas>();
        worldCanvas.renderMode = RenderMode.WorldSpace;
        worldCanvas.worldCamera = Camera.main;
        
        CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        
        // RectTransform 설정
        RectTransform canvasRect = canvasObj.GetComponent<RectTransform>();
        canvasRect.sizeDelta = new Vector2(300, 200); // 더 큰 크기로 설정
        canvasRect.localScale = Vector3.one * 0.01f;  // 월드 스케일 조정
        
        // UI 컨테이너 설정
        if (uiContainer == null)
        {
            uiContainer = canvasObj;
        }
        
        Debug.Log("📋 CustomerUI Canvas 자동 생성됨");
    }
    
    /// <summary>
    /// 📝 주문 말풍선 표시 (다중 주문 지원)
    /// </summary>
    public void ShowOrderBubble(List<Customer.OrderItem> orderItems)
    {
        if (!enableUI || !isInitialized || orderItems == null || orderItems.Count == 0) return;
        
        currentOrder = new List<Customer.OrderItem>(orderItems);
        
        // 주문 텍스트 생성
        string orderDisplayText = GenerateOrderDisplayText(orderItems);
        
        Debug.Log($"📋 주문 말풍선 표시: {orderDisplayText}");
        
        // 주문 텍스트 설정
        if (orderText != null)
        {
            orderText.text = orderDisplayText;
        }
        
        // 말풍선 활성화 및 애니메이션
        if (orderBubble != null)
        {
            orderBubble.SetActive(true);
            StartCoroutine(BubblePopAnimation(orderBubble));
        }
    }
    
    /// <summary>
    /// 📝 주문 표시 텍스트 생성
    /// </summary>
    string GenerateOrderDisplayText(List<Customer.OrderItem> orderItems)
    {
        if (orderItems == null || orderItems.Count == 0) return "주문 없음";
        
        string displayText = "주문:\n";
        
        for (int i = 0; i < orderItems.Count; i++)
        {
            Customer.OrderItem item = orderItems[i];
            string itemName = GetHotteokName(item.fillingType);
            
            // 완료 상태에 따른 표시
            if (item.IsCompleted())
            {
                displayText += $"✅ {itemName} {item.quantity}개";
            }
            else
            {
                displayText += $"🔲 {itemName} {item.quantity}개";
            }
            
            // 진행 상황 표시
            if (item.receivedQuantity > 0)
            {
                displayText += $" ({item.receivedQuantity}/{item.quantity})";
            }
            
            if (i < orderItems.Count - 1)
            {
                displayText += "\n";
            }
        }
        
        return displayText;
    }
    
    /// <summary>
    /// 📝 주문 진행 상황 업데이트
    /// </summary>
    public void UpdateOrderProgress(List<Customer.OrderItem> orderItems)
    {
        if (!enableUI || orderItems == null) return;
        
        currentOrder = new List<Customer.OrderItem>(orderItems);
        
        // 주문 텍스트 업데이트
        string updatedText = GenerateOrderDisplayText(orderItems);
        
        if (orderText != null)
        {
            orderText.text = updatedText;
        }
        
        Debug.Log($"📋 주문 진행 상황 업데이트: {updatedText}");
    }
    
    /// <summary>
    /// 📝 부분 완료 피드백
    /// </summary>
    public void ShowPartialCompletionFeedback(string message)
    {
        if (!enableUI) return;
        
        ShowFeedbackText(message, Color.green);
        
        // 살짝 바운스 효과
        if (orderBubble != null)
        {
            StartCoroutine(BubblePopAnimation(orderBubble));
        }
    }
    
    /// <summary>
    /// 주문 말풍선 숨기기
    /// </summary>
    public void HideOrderBubble()
    {
        if (orderBubble != null)
        {
            orderBubble.SetActive(false);
        }
    }
    
    /// <summary>
    /// 대기 진행도 업데이트
    /// </summary>
    public void UpdateWaitProgress(float progress)
    {
        if (!enableUI) return;
        
        if (waitProgressSlider != null)
        {
            waitProgressSlider.value = progress;
        }
        
        // 진행도에 따른 색상 변경
        if (progressFillImage != null)
        {
            Color targetColor;
            
            if (progress < 0.25f)
            {
                targetColor = normalProgressColor;
            }
            else if (progress < 0.75f)
            {
                targetColor = warningProgressColor;
            }
            else
            {
                targetColor = dangerProgressColor;
            }
            
            progressFillImage.color = Color.Lerp(progressFillImage.color, targetColor, Time.deltaTime * 3f);
        }
        
        // 진행 바 표시/숨기기
        if (waitProgressSlider != null)
        {
            waitProgressSlider.gameObject.SetActive(progress > 0);
        }
    }
    
    /// <summary>
    /// 경고 아이콘 표시
    /// </summary>
    public void ShowWarningIcon()
    {
        if (!enableUI) return;
        
        if (warningIcon != null)
        {
            warningIcon.SetActive(true);
            
            // 맥박 애니메이션 시작
            if (warningPulseCoroutine != null)
            {
                StopCoroutine(warningPulseCoroutine);
            }
            warningPulseCoroutine = StartCoroutine(PulseAnimation(warningIcon));
        }
    }
    
    /// <summary>
    /// 경고 아이콘 숨기기
    /// </summary>
    public void HideWarningIcon()
    {
        if (warningIcon != null)
        {
            warningIcon.SetActive(false);
        }
        
        if (warningPulseCoroutine != null)
        {
            StopCoroutine(warningPulseCoroutine);
            warningPulseCoroutine = null;
        }
    }
    
    /// <summary>
    /// 만족 효과 표시
    /// </summary>
    public void ShowSatisfactionEffect()
    {
        if (!enableUI) return;
        
        if (satisfactionIcon != null)
        {
            satisfactionIcon.SetActive(true);
            StartCoroutine(SatisfactionAnimation());
        }
        
        ShowFeedbackText("고마워요! 🎉", Color.green);
    }
    
    /// <summary>
    /// 화남 효과 표시
    /// </summary>
    public void ShowAngryEffect()
    {
        if (!enableUI) return;
        
        if (angryIcon != null)
        {
            angryIcon.SetActive(true);
            StartCoroutine(AngryAnimation());
        }
        
        ShowFeedbackText("너무 오래 기다렸어요! 💢", Color.red);
    }
    
    /// <summary>
    /// 잘못된 주문 피드백
    /// </summary>
    public void ShowWrongOrderFeedback()
    {
        if (!enableUI) return;
        
        ShowFeedbackText("이건 제가 주문한 게 아니에요! 😕", Color.green);
        
        // 말풍선 흔들기 효과
        if (orderBubble != null)
        {
            StartCoroutine(ShakeAnimation(orderBubble));
        }
    }
    
    /// <summary>
    /// 선택 안함 피드백
    /// </summary>
    public void ShowNoSelectionFeedback()
    {
        if (!enableUI) return;
        
        ShowFeedbackText("호떡을 선택해주세요! 🤔", Color.blue);
    }
    
    /// <summary>
    /// 피드백 텍스트 표시
    /// </summary>
    void ShowFeedbackText(string text, Color color)
    {
        if (!enableUI) return;
        
        if (feedbackText != null)
        {
            feedbackText.text = text;
            feedbackText.color = color;
        }
        
        if (feedbackTextObject != null)
        {
            feedbackTextObject.SetActive(true);
            
            if (feedbackCoroutine != null)
            {
                StopCoroutine(feedbackCoroutine);
            }
            feedbackCoroutine = StartCoroutine(FeedbackTextAnimation());
        }
    }
    
    /// <summary>
    /// 모든 UI 숨기기
    /// </summary>
    public void HideAllUI()
    {
        HideOrderBubble();
        HideWarningIcon();
        
        if (satisfactionIcon != null) satisfactionIcon.SetActive(false);
        if (angryIcon != null) angryIcon.SetActive(false);
        if (feedbackTextObject != null) feedbackTextObject.SetActive(false);
        if (waitProgressSlider != null) waitProgressSlider.gameObject.SetActive(false);
    }
    
    /// <summary>
    /// 호떡 타입에 따른 이름 반환
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
    
    // ============= 애니메이션 코루틴들 =============
    
    /// <summary>
    /// 말풍선 팝업 애니메이션
    /// </summary>
    IEnumerator BubblePopAnimation(GameObject target)
    {
        if (!enableUI || target == null) yield break;
        
        Vector3 originalScale = target.transform.localScale;
        target.transform.localScale = Vector3.zero;
        
        float elapsedTime = 0f;
        
        while (elapsedTime < bubblePopDuration)
        {
            elapsedTime += Time.deltaTime;
            float t = elapsedTime / bubblePopDuration;
            float curveValue = popCurve.Evaluate(t);
            
            target.transform.localScale = originalScale * curveValue;
            yield return null;
        }
        
        target.transform.localScale = originalScale;
    }
    
    /// <summary>
    /// 맥박 애니메이션
    /// </summary>
    IEnumerator PulseAnimation(GameObject target)
    {
        if (!enableUI || target == null) yield break;
        
        Vector3 originalScale = target.transform.localScale;
        
        while (target.activeInHierarchy)
        {
            // 크게
            float elapsedTime = 0f;
            while (elapsedTime < iconPulseDuration * 0.5f)
            {
                elapsedTime += Time.deltaTime;
                float t = elapsedTime / (iconPulseDuration * 0.5f);
                target.transform.localScale = Vector3.Lerp(originalScale, originalScale * 1.3f, t);
                yield return null;
            }
            
            // 작게
            elapsedTime = 0f;
            while (elapsedTime < iconPulseDuration * 0.5f)
            {
                elapsedTime += Time.deltaTime;
                float t = elapsedTime / (iconPulseDuration * 0.5f);
                target.transform.localScale = Vector3.Lerp(originalScale * 1.3f, originalScale, t);
                yield return null;
            }
        }
    }
    
    /// <summary>
    /// 만족 애니메이션
    /// </summary>
    IEnumerator SatisfactionAnimation()
    {
        if (!enableUI || satisfactionIcon == null) yield break;
        
        Vector3 originalPos = satisfactionIcon.transform.localPosition;
        Vector3 originalScale = satisfactionIcon.transform.localScale;
        
        float duration = 1.5f;
        float elapsedTime = 0f;
        
        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            float t = elapsedTime / duration;
            
            // 위로 올라가면서 크게 되었다가 사라짐
            satisfactionIcon.transform.localPosition = originalPos + Vector3.up * t * 50f;
            satisfactionIcon.transform.localScale = originalScale * (1 + t * 0.5f);
            
            // 투명도 변화
            CanvasGroup canvasGroup = satisfactionIcon.GetComponent<CanvasGroup>();
            if (canvasGroup == null)
            {
                canvasGroup = satisfactionIcon.AddComponent<CanvasGroup>();
            }
            canvasGroup.alpha = 1 - t;
            
            yield return null;
        }
        
        satisfactionIcon.SetActive(false);
        
        // 원상복구
        satisfactionIcon.transform.localPosition = originalPos;
        satisfactionIcon.transform.localScale = originalScale;
    }
    
    /// <summary>
    /// 화남 애니메이션
    /// </summary>
    IEnumerator AngryAnimation()
    {
        if (!enableUI || angryIcon == null) yield break;
        
        float duration = 0.8f;
        float elapsedTime = 0f;
        
        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            float t = elapsedTime / duration;
            
            // 좌우로 흔들기
            float shake = Mathf.Sin(t * 20f) * 10f * (1 - t);
            angryIcon.transform.localPosition = Vector3.right * shake;
            
            yield return null;
        }
        
        angryIcon.SetActive(false);
        angryIcon.transform.localPosition = Vector3.zero;
    }
    
    /// <summary>
    /// 흔들기 애니메이션
    /// </summary>
    IEnumerator ShakeAnimation(GameObject target)
    {
        if (!enableUI || target == null) yield break;
        
        Vector3 originalPos = target.transform.localPosition;
        float duration = 0.5f;
        float elapsedTime = 0f;
        
        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            float t = elapsedTime / duration;
            
            float shake = Mathf.Sin(t * 15f) * 5f * (1 - t);
            target.transform.localPosition = originalPos + Vector3.right * shake;
            
            yield return null;
        }
        
        target.transform.localPosition = originalPos;
    }
    
    /// <summary>
    /// 피드백 텍스트 애니메이션
    /// </summary>
    IEnumerator FeedbackTextAnimation()
    {
        if (!enableUI || feedbackTextObject == null) yield break;
        
        // 페이드 인
        CanvasGroup canvasGroup = feedbackTextObject.GetComponent<CanvasGroup>();
        if (canvasGroup == null)
        {
            canvasGroup = feedbackTextObject.AddComponent<CanvasGroup>();
        }
        
        canvasGroup.alpha = 0f;
        float fadeInTime = 0.3f;
        float elapsedTime = 0f;
        
        while (elapsedTime < fadeInTime)
        {
            elapsedTime += Time.deltaTime;
            canvasGroup.alpha = elapsedTime / fadeInTime;
            yield return null;
        }
        
        canvasGroup.alpha = 1f;
        
        // 표시 시간 대기
        yield return new WaitForSeconds(feedbackDisplayTime);
        
        // 페이드 아웃
        float fadeOutTime = 0.3f;
        elapsedTime = 0f;
        
        while (elapsedTime < fadeOutTime)
        {
            elapsedTime += Time.deltaTime;
            canvasGroup.alpha = 1f - (elapsedTime / fadeOutTime);
            yield return null;
        }
        
        canvasGroup.alpha = 0f;
        feedbackTextObject.SetActive(false);
    }
    
    /// <summary>
    /// UI 활성화/비활성화
    /// </summary>
    public void SetUIEnabled(bool enabled)
    {
        enableUI = enabled;
        
        if (!enabled)
        {
            HideAllUI();
        }
    }
}