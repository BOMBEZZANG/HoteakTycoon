// Assets/Scripts/Gold/GoldManager.cs
// 💰 골드 시스템 메인 관리자

using UnityEngine;
using System;

public class GoldManager : MonoBehaviour
{
    [Header("💰 호떡 가격 설정")]
    public int sugarHotteokPrice = 500;        // 설탕 호떡 가격
    public int seedHotteokPrice = 500;         // 씨앗 호떡 가격
    
    [Header("🔊 사운드 효과")]
    public AudioClip goldEarnSound;            // 골드 획득 소리
    public AudioClip saleCompleteSound;        // 판매 완료 소리
    
    [Header("🐛 디버그")]
    public bool enableDebugLogs = true;        // 디버그 로그 활성화
    
    // 골드 데이터
    private GoldData goldData;
    
    // 싱글톤
    public static GoldManager Instance { get; private set; }
    
    // 이벤트
    public Action<int> OnGoldChanged;              // 골드 변경 시
    public Action<int, PreparationUI.FillingType> OnSaleCompleted;  // 판매 완료 시
    public Action<int> OnTodaysEarningsChanged;    // 오늘 수익 변경 시
    
    void Awake()
    {
        // 싱글톤 설정
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            
            InitializeGoldSystem();
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    void Start()
    {
        // GameManager의 하루 시작/종료 이벤트 구독
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnDayEnded += OnDayEnded;
        }
    }
    
    void OnDestroy()
    {
        // 이벤트 구독 해제
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnDayEnded -= OnDayEnded;
        }
    }
    
    /// <summary>
    /// 골드 시스템 초기화
    /// </summary>
    void InitializeGoldSystem()
    {
        goldData = new GoldData();
        goldData.LoadFromPlayerPrefs();
        goldData.StartNewDay(); // 새로운 하루 시작
        
        DebugLog("💰 골드 시스템 초기화 완료!");
        DebugLog($"현재 골드: {goldData.currentGold:N0}원");
        
        // UI 업데이트
        OnGoldChanged?.Invoke(goldData.currentGold);
        OnTodaysEarningsChanged?.Invoke(goldData.todaysEarnings);
    }
    
    /// <summary>
    /// 호떡 판매 처리
    /// </summary>
    public void ProcessHotteokSale(PreparationUI.FillingType hotteokType)
    {
        // 호떡 타입에 따른 가격 결정
        int price = GetHotteokPrice(hotteokType);
        
        // 판매 처리
        goldData.ProcessSale(hotteokType, price);
        
        DebugLog($"💰 {GetHotteokName(hotteokType)} 판매! +{price:N0}원 (오늘 총: {goldData.todaysEarnings:N0}원)");
        
        // 사운드 효과
        PlaySaleSound();
        
        // UI 업데이트 이벤트 발생
        OnTodaysEarningsChanged?.Invoke(goldData.todaysEarnings);
        OnSaleCompleted?.Invoke(price, hotteokType);
        
        // 즉시 저장 (데이터 손실 방지)
        goldData.SaveToPlayerPrefs();
    }
    
    /// <summary>
    /// 호떡 타입에 따른 가격 반환
    /// </summary>
    int GetHotteokPrice(PreparationUI.FillingType hotteokType)
    {
        switch (hotteokType)
        {
            case PreparationUI.FillingType.Sugar:
                return sugarHotteokPrice;
            case PreparationUI.FillingType.Seed:
                return seedHotteokPrice;
            default:
                DebugLog($"❌ 알 수 없는 호떡 타입: {hotteokType}");
                return sugarHotteokPrice; // 기본값
        }
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
                return "알 수 없는 호떡";
        }
    }
    
    /// <summary>
    /// 하루 종료 처리
    /// </summary>
    void OnDayEnded()
    {
        DebugLog($"🌙 하루 종료 - 오늘 수익: {goldData.todaysEarnings:N0}원");
        
        // 하루 종료 처리
        goldData.EndDay();
        
        // 골드 변경 이벤트 발생 (누적된 골드)
        OnGoldChanged?.Invoke(goldData.currentGold);
        
        // 데이터 저장
        goldData.SaveToPlayerPrefs();
        
        DebugLog($"💰 누적 골드: {goldData.currentGold:N0}원 (오늘 +{goldData.todaysEarnings:N0}원)");
        
        // 통계 출력
        if (enableDebugLogs)
        {
            goldData.PrintDebugInfo();
        }
    }
    
    /// <summary>
    /// 판매 완료 소리 재생
    /// </summary>
    void PlaySaleSound()
    {
        if (saleCompleteSound != null)
        {
            AudioSource.PlayClipAtPoint(saleCompleteSound, transform.position);
        }
        
        if (goldEarnSound != null)
        {
            AudioSource.PlayClipAtPoint(goldEarnSound, transform.position, 0.7f);
        }
    }
    
    /// <summary>
    /// 디버그 로그 출력
    /// </summary>
    void DebugLog(string message)
    {
        if (enableDebugLogs)
        {
            Debug.Log($"[GoldManager] {message}");
        }
    }
    
    // ===== 공개 접근자 메서드들 =====
    
    /// <summary>
    /// 현재 골드 반환
    /// </summary>
    public int GetCurrentGold()
    {
        return goldData?.currentGold ?? 0;
    }
    
    /// <summary>
    /// 오늘 수익 반환
    /// </summary>
    public int GetTodaysEarnings()
    {
        return goldData?.todaysEarnings ?? 0;
    }
    
    /// <summary>
    /// 누적 총 수익 반환
    /// </summary>
    public int GetTotalLifetimeEarnings()
    {
        return goldData?.totalLifetimeEarnings ?? 0;
    }
    
    /// <summary>
    /// 최고 일일 수익 반환
    /// </summary>
    public int GetHighestDailyEarnings()
    {
        return goldData?.highestDailyEarnings ?? 0;
    }
    
    /// <summary>
    /// 총 판매량 반환
    /// </summary>
    public int GetTotalHotteoksSold()
    {
        return goldData?.totalHotteoksSold ?? 0;
    }
    
    /// <summary>
    /// 호떡별 판매량 반환
    /// </summary>
    public (int sugar, int seed) GetSalesByType()
    {
        if (goldData == null) return (0, 0);
        return (goldData.sugarHotteoksSold, goldData.seedHotteoksSold);
    }
    
    /// <summary>
    /// 골드 데이터 강제 저장
    /// </summary>
    public void SaveGoldData()
    {
        goldData?.SaveToPlayerPrefs();
        DebugLog("💾 골드 데이터 수동 저장 완료");
    }
    
    /// <summary>
    /// 골드 데이터 리셋 (개발용)
    /// </summary>
    [ContextMenu("Reset Gold Data")]
    public void ResetGoldData()
    {
        if (goldData != null)
        {
            goldData = new GoldData();
            goldData.SaveToPlayerPrefs();
            
            OnGoldChanged?.Invoke(0);
            OnTodaysEarningsChanged?.Invoke(0);
            
            DebugLog("🔄 골드 데이터 리셋 완료");
        }
    }
    
    /// <summary>
    /// 디버그 정보 출력 (개발용)
    /// </summary>
    [ContextMenu("Print Gold Debug Info")]
    public void PrintGoldDebugInfo()
    {
        if (goldData != null)
        {
            goldData.PrintDebugInfo();
        }
        else
        {
            Debug.Log("❌ 골드 데이터가 없습니다!");
        }
    }
}