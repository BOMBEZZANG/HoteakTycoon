// Assets/Scripts/Gold/GoldData.cs
// 💰 골드 관련 데이터 구조체

using System;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class GoldData
{
    [Header("💰 골드 정보")]
    public int currentGold = 0;                    // 현재 보유 골드
    public int todaysEarnings = 0;                 // 오늘 수익
    public int totalLifetimeEarnings = 0;          // 누적 총 수익
    public int highestDailyEarnings = 0;           // 최고 일일 수익
    
    [Header("📊 판매 통계")]
    public int totalHotteoksSold = 0;              // 총 판매된 호떡 수
    public int sugarHotteoksSold = 0;              // 판매된 설탕 호떡 수
    public int seedHotteoksSold = 0;               // 판매된 씨앗 호떡 수
    
    /// <summary>
    /// 하루 시작 시 오늘 수익 초기화
    /// </summary>
    public void StartNewDay()
    {
        todaysEarnings = 0;
    }
    
    /// <summary>
    /// 하루 종료 시 통계 업데이트
    /// </summary>
    public void EndDay()
    {
        // 최고 일일 수익 업데이트
        if (todaysEarnings > highestDailyEarnings)
        {
            highestDailyEarnings = todaysEarnings;
        }
        
        // 누적 수익에 오늘 수익 추가
        totalLifetimeEarnings += todaysEarnings;
        
        // 현재 골드에 오늘 수익 추가 (누적)
        currentGold += todaysEarnings;
    }
    
    /// <summary>
    /// 호떡 판매 처리
    /// </summary>
    public void ProcessSale(PreparationUI.FillingType hotteokType, int price)
    {
        // 수익 추가
        todaysEarnings += price;
        
        // 판매 통계 업데이트
        totalHotteoksSold++;
        
        switch (hotteokType)
        {
            case PreparationUI.FillingType.Sugar:
                sugarHotteoksSold++;
                break;
            case PreparationUI.FillingType.Seed:
                seedHotteoksSold++;
                break;
        }
    }
    
    /// <summary>
    /// PlayerPrefs에 저장
    /// </summary>
    public void SaveToPlayerPrefs()
    {
        PlayerPrefs.SetInt("Gold_Current", currentGold);
        PlayerPrefs.SetInt("Gold_TotalLifetime", totalLifetimeEarnings);
        PlayerPrefs.SetInt("Gold_HighestDaily", highestDailyEarnings);
        PlayerPrefs.SetInt("Gold_TotalSold", totalHotteoksSold);
        PlayerPrefs.SetInt("Gold_SugarSold", sugarHotteoksSold);
        PlayerPrefs.SetInt("Gold_SeedSold", seedHotteoksSold);
        PlayerPrefs.Save();
    }
    
    /// <summary>
    /// PlayerPrefs에서 로드
    /// </summary>
    public void LoadFromPlayerPrefs()
    {
        currentGold = PlayerPrefs.GetInt("Gold_Current", 0);
        totalLifetimeEarnings = PlayerPrefs.GetInt("Gold_TotalLifetime", 0);
        highestDailyEarnings = PlayerPrefs.GetInt("Gold_HighestDaily", 0);
        totalHotteoksSold = PlayerPrefs.GetInt("Gold_TotalSold", 0);
        sugarHotteoksSold = PlayerPrefs.GetInt("Gold_SugarSold", 0);
        seedHotteoksSold = PlayerPrefs.GetInt("Gold_SeedSold", 0);
        
        // 오늘 수익은 항상 0으로 시작
        todaysEarnings = 0;
    }
    
    /// <summary>
    /// 디버그용 정보 출력
    /// </summary>
    public void PrintDebugInfo()
    {
        UnityEngine.Debug.Log("=== 골드 데이터 정보 ===");
        UnityEngine.Debug.Log($"현재 골드: {currentGold:N0}원");
        UnityEngine.Debug.Log($"오늘 수익: {todaysEarnings:N0}원");
        UnityEngine.Debug.Log($"누적 수익: {totalLifetimeEarnings:N0}원");
        UnityEngine.Debug.Log($"최고 일일 수익: {highestDailyEarnings:N0}원");
        UnityEngine.Debug.Log($"총 판매량: {totalHotteoksSold}개");
        UnityEngine.Debug.Log($"설탕 호떡: {sugarHotteoksSold}개, 씨앗 호떡: {seedHotteoksSold}개");
    }
}