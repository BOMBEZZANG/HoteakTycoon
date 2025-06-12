// Assets/Scripts/Gold/GoldUI.cs
// 💰 골드 UI 관리자

using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class GoldUI : MonoBehaviour
{
    [Header("💰 골드 표시 UI")]
    public TextMeshProUGUI currentGoldText;        // 현재 골드 표시
    public TextMeshProUGUI todaysEarningsText;     // 오늘 수익 표시
    public GameObject goldPanel;                   // 골드 패널 (전체 컨테이너)
    
    [Header("💵 판매 팝업 UI")]
    public GameObject salePopupPanel;              // 판매 팝업 패널
    public TextMeshProUGUI salePopupText;          // 판매 팝업 텍스트
    public float salePopupDuration = 2.0f;         // 팝업 표시 시간
    
    [Header("✨ 애니메이션 설정")]
    public float goldCountAnimationDuration = 1.0f; // 골드 카운트 애니메이션 시간
    public AnimationCurve goldCountCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
    public Color salePopupColor = Color.green;     // 판매 팝업 색상
    
    [Header("🔊 UI 사운드")]
    public AudioClip goldUpdateSound;              // 골드 업데이트 소리
    public AudioClip salePopupSound;               // 판매 팝업 소리
    
    [Header("🐛 디버그")]
    public bool enableDebugLogs = true;
    
    // 내부 변수
    private int displayedGold = 0;                 // 현재 표시되고 있는 골드
    private int displayedEarnings = 0;             // 현재 표시되고 있는 오늘 수익
    private Coroutine goldAnimationCoroutine;      // 골드 애니메이션 코루틴
    private Coroutine earningsAnimationCoroutine;  // 수익 애니메이션 코루틴
    private Coroutine salePopupCoroutine;          // 판매 팝업 코루틴
    
    void Start()
    {
        InitializeGoldUI();
        SubscribeToGoldEvents();
    }
    
    void OnDestroy()
    {
        UnsubscribeFromGoldEvents();
    }
    
    /// <summary>
    /// 골드 UI 초기화
    /// </summary>
    void InitializeGoldUI()
    {
        // 초기 UI 설정
        if (salePopupPanel != null)
        {
            salePopupPanel.SetActive(false);
        }
        
        // GoldManager가 있으면 초기 값 설정
        if (GoldManager.Instance != null)
        {
            displayedGold = GoldManager.Instance.GetCurrentGold();
            displayedEarnings = GoldManager.Instance.GetTodaysEarnings();
            
            UpdateGoldText(displayedGold, false);
            UpdateEarningsText(displayedEarnings, false);
        }
        else
        {
            UpdateGoldText(0, false);
            UpdateEarningsText(0, false);
        }
        
        DebugLog("💰 골드 UI 초기화 완료");
    }
    
    /// <summary>
    /// 골드 매니저 이벤트 구독
    /// </summary>
    void SubscribeToGoldEvents()
    {
        if (GoldManager.Instance != null)
        {
            GoldManager.Instance.OnGoldChanged += OnGoldChanged;
            GoldManager.Instance.OnTodaysEarningsChanged += OnTodaysEarningsChanged;
            GoldManager.Instance.OnSaleCompleted += OnSaleCompleted;
        }
    }
    
    /// <summary>
    /// 골드 매니저 이벤트 구독 해제
    /// </summary>
    void UnsubscribeFromGoldEvents()
    {
        if (GoldManager.Instance != null)
        {
            GoldManager.Instance.OnGoldChanged -= OnGoldChanged;
            GoldManager.Instance.OnTodaysEarningsChanged -= OnTodaysEarningsChanged;
            GoldManager.Instance.OnSaleCompleted -= OnSaleCompleted;
        }
    }
    
    /// <summary>
    /// 골드 변경 이벤트 처리
    /// </summary>
    void OnGoldChanged(int newGold)
    {
        DebugLog($"💰 골드 변경: {displayedGold:N0} → {newGold:N0}");
        
        if (goldAnimationCoroutine != null)
        {
            StopCoroutine(goldAnimationCoroutine);
        }
        
        goldAnimationCoroutine = StartCoroutine(AnimateGoldCount(displayedGold, newGold));
    }
    
    /// <summary>
    /// 오늘 수익 변경 이벤트 처리
    /// </summary>
    void OnTodaysEarningsChanged(int newEarnings)
    {
        DebugLog($"📈 오늘 수익 변경: {displayedEarnings:N0} → {newEarnings:N0}");
        
        if (earningsAnimationCoroutine != null)
        {
            StopCoroutine(earningsAnimationCoroutine);
        }
        
        earningsAnimationCoroutine = StartCoroutine(AnimateEarningsCount(displayedEarnings, newEarnings));
    }
    
    /// <summary>
    /// 판매 완료 이벤트 처리
    /// </summary>
    void OnSaleCompleted(int earnedAmount, PreparationUI.FillingType hotteokType)
    {
        DebugLog($"💵 판매 완료: {GetHotteokName(hotteokType)} +{earnedAmount:N0}원");
        
        ShowSalePopup(earnedAmount, hotteokType);
        
        // 판매 완료 소리
        if (salePopupSound != null)
        {
            AudioSource.PlayClipAtPoint(salePopupSound, transform.position, 0.8f);
        }
    }
    
    /// <summary>
    /// 골드 카운트 애니메이션
    /// </summary>
    IEnumerator AnimateGoldCount(int fromGold, int toGold)
    {
        float elapsedTime = 0f;
        
        while (elapsedTime < goldCountAnimationDuration)
        {
            elapsedTime += Time.deltaTime;
            float t = elapsedTime / goldCountAnimationDuration;
            float curveValue = goldCountCurve.Evaluate(t);
            
            int animatedGold = Mathf.RoundToInt(Mathf.Lerp(fromGold, toGold, curveValue));
            UpdateGoldText(animatedGold, false);
            
            yield return null;
        }
        
        // 최종 값 설정
        displayedGold = toGold;
        UpdateGoldText(toGold, false);
        
        // 골드 업데이트 소리
        if (goldUpdateSound != null)
        {
            AudioSource.PlayClipAtPoint(goldUpdateSound, transform.position, 0.6f);
        }
        
        goldAnimationCoroutine = null;
    }
    
    /// <summary>
    /// 수익 카운트 애니메이션
    /// </summary>
    IEnumerator AnimateEarningsCount(int fromEarnings, int toEarnings)
    {
        float elapsedTime = 0f;
        float duration = goldCountAnimationDuration * 0.7f; // 수익은 좀 더 빠르게
        
        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            float t = elapsedTime / duration;
            float curveValue = goldCountCurve.Evaluate(t);
            
            int animatedEarnings = Mathf.RoundToInt(Mathf.Lerp(fromEarnings, toEarnings, curveValue));
            UpdateEarningsText(animatedEarnings, false);
            
            yield return null;
        }
        
        // 최종 값 설정
        displayedEarnings = toEarnings;
        UpdateEarningsText(toEarnings, false);
        
        earningsAnimationCoroutine = null;
    }
    
    /// <summary>
    /// 골드 텍스트 업데이트
    /// </summary>
    void UpdateGoldText(int gold, bool immediate = true)
    {
        if (currentGoldText != null)
        {
            currentGoldText.text = $"💰 {gold:N0}원";
        }
        
        if (immediate)
        {
            displayedGold = gold;
        }
    }
    
    /// <summary>
    /// 오늘 수익 텍스트 업데이트
    /// </summary>
    void UpdateEarningsText(int earnings, bool immediate = true)
    {
        if (todaysEarningsText != null)
        {
            todaysEarningsText.text = $"📈 오늘: +{earnings:N0}원";
        }
        
        if (immediate)
        {
            displayedEarnings = earnings;
        }
    }
    
    /// <summary>
    /// 판매 팝업 표시
    /// </summary>
    void ShowSalePopup(int earnedAmount, PreparationUI.FillingType hotteokType)
    {
        if (salePopupPanel == null || salePopupText == null) return;
        
        // 기존 팝업이 있으면 중지
        if (salePopupCoroutine != null)
        {
            StopCoroutine(salePopupCoroutine);
        }
        
        // 팝업 텍스트 설정
        string hotteokName = GetHotteokName(hotteokType);
        salePopupText.text = $"✨ {hotteokName} 판매!\n+{earnedAmount:N0}원";
        salePopupText.color = salePopupColor;
        
        // 팝업 애니메이션 시작
        salePopupCoroutine = StartCoroutine(SalePopupAnimation());
    }
    
    /// <summary>
    /// 판매 팝업 애니메이션
    /// </summary>
    IEnumerator SalePopupAnimation()
    {
        if (salePopupPanel == null) yield break;
        
        // 팝업 활성화
        salePopupPanel.SetActive(true);
        
        // 스케일 애니메이션 (작게 시작해서 크게)
        Vector3 originalScale = salePopupPanel.transform.localScale;
        salePopupPanel.transform.localScale = Vector3.zero;
        
        float popInDuration = 0.3f;
        float elapsedTime = 0f;
        
        // 팝인 애니메이션
        while (elapsedTime < popInDuration)
        {
            elapsedTime += Time.deltaTime;
            float t = elapsedTime / popInDuration;
            float scale = Mathf.Lerp(0, 1.1f, t); // 약간 크게 만들었다가
            salePopupPanel.transform.localScale = originalScale * scale;
            yield return null;
        }
        
        // 원래 크기로 조정
        elapsedTime = 0f;
        float bounceBackDuration = 0.2f;
        
        while (elapsedTime < bounceBackDuration)
        {
            elapsedTime += Time.deltaTime;
            float t = elapsedTime / bounceBackDuration;
            float scale = Mathf.Lerp(1.1f, 1f, t); // 원래 크기로
            salePopupPanel.transform.localScale = originalScale * scale;
            yield return null;
        }
        
        salePopupPanel.transform.localScale = originalScale;
        
        // 표시 시간 대기
        yield return new WaitForSeconds(salePopupDuration - popInDuration - bounceBackDuration);
        
        // 페이드 아웃
        CanvasGroup canvasGroup = salePopupPanel.GetComponent<CanvasGroup>();
        if (canvasGroup == null)
        {
            canvasGroup = salePopupPanel.AddComponent<CanvasGroup>();
        }
        
        float fadeOutDuration = 0.5f;
        elapsedTime = 0f;
        
        while (elapsedTime < fadeOutDuration)
        {
            elapsedTime += Time.deltaTime;
            float t = elapsedTime / fadeOutDuration;
            canvasGroup.alpha = 1f - t;
            yield return null;
        }
        
        // 팝업 비활성화
        salePopupPanel.SetActive(false);
        canvasGroup.alpha = 1f; // 다음 사용을 위해 복원
        
        salePopupCoroutine = null;
    }
    
    /// <summary>
    /// 호떡 타입에 따른 이름 반환
    /// </summary>
    string GetHotteokName(PreparationUI.FillingType hotteokType)
    {
        switch (hotteokType)
        {
            case PreparationUI.FillingType.Sugar:
                return "설탕 호떡";
            case PreparationUI.FillingType.Seed:
                return "씨앗 호떡";
            default:
                return "호떡";
        }
    }
    
    /// <summary>
    /// 디버그 로그 출력
    /// </summary>
    void DebugLog(string message)
    {
        if (enableDebugLogs)
        {
            Debug.Log($"[GoldUI] {message}");
        }
    }
    
    /// <summary>
    /// 골드 UI 강제 업데이트 (개발용)
    /// </summary>
    [ContextMenu("Force Update Gold UI")]
    public void ForceUpdateGoldUI()
    {
        if (GoldManager.Instance != null)
        {
            UpdateGoldText(GoldManager.Instance.GetCurrentGold(), true);
            UpdateEarningsText(GoldManager.Instance.GetTodaysEarnings(), true);
            DebugLog("🔄 골드 UI 강제 업데이트 완료");
        }
    }
}