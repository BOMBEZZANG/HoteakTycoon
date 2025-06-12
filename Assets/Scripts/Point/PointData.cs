// Assets/Scripts/Gold/PointData.cs
// 💎 포인트 관련 데이터 구조체

using System;
using UnityEngine;

[System.Serializable]
public class PointData
{
    [Header("💎 포인트 정보")]
    public int currentPoints = 0;                  // 현재 보유 포인트
    public int todaysPoints = 0;                   // 오늘 획득 포인트
    public int totalLifetimePoints = 0;            // 누적 총 포인트
    public int highestDailyPoints = 0;             // 최고 일일 포인트
    
    [Header("🎯 누르기 통계")]
    public int totalPerfectPresses = 0;            // 총 Perfect 횟수
    public int totalGoodPresses = 0;               // 총 Good 횟수
    public int consecutivePerfectCount = 0;        // 연속 Perfect 횟수
    public int maxConsecutivePerfect = 0;          // 최대 연속 Perfect 기록
    
    [Header("😊 만족 통계")]
    public int totalSatisfiedCustomers = 0;        // 총 만족한 손님 수
    public int consecutiveSatisfiedCount = 0;      // 연속 만족 손님 수
    public int maxConsecutiveSatisfied = 0;        // 최대 연속 만족 기록
    
    /// <summary>
    /// 하루 시작 시 오늘 포인트 초기화
    /// </summary>
    public void StartNewDay()
    {
        todaysPoints = 0;
        consecutivePerfectCount = 0;
        consecutiveSatisfiedCount = 0;
    }
    
    /// <summary>
    /// 하루 종료 시 통계 업데이트
    /// </summary>
    public void EndDay()
    {
        // 최고 일일 포인트 업데이트
        if (todaysPoints > highestDailyPoints)
        {
            highestDailyPoints = todaysPoints;
        }
        
        // 누적 포인트에 오늘 포인트 추가
        totalLifetimePoints += todaysPoints;
        
        // 현재 포인트에 오늘 포인트 추가 (누적)
        currentPoints += todaysPoints;
    }
    
    /// <summary>
    /// Perfect 누르기 처리
    /// </summary>
    public int ProcessPerfectPress(int basePoints)
    {
        consecutivePerfectCount++;
        totalPerfectPresses++;
        
        // 연속 Perfect 기록 업데이트
        if (consecutivePerfectCount > maxConsecutivePerfect)
        {
            maxConsecutivePerfect = consecutivePerfectCount;
        }
        
        // 연속 보너스 계산
        int bonusMultiplier = GetConsecutivePerfectMultiplier();
        int totalPoints = basePoints * bonusMultiplier;
        
        // 포인트 추가
        todaysPoints += totalPoints;
        
        return totalPoints;
    }
    
    /// <summary>
    /// Good 누르기 처리
    /// </summary>
    public int ProcessGoodPress(int basePoints)
    {
        totalGoodPresses++;
        
        // Perfect 연속 기록 초기화
        consecutivePerfectCount = 0;
        
        // 포인트 추가
        todaysPoints += basePoints;
        
        return basePoints;
    }
    
    /// <summary>
    /// Miss 누르기 처리 (포인트 없음, 연속 기록 초기화)
    /// </summary>
    public void ProcessMissPress()
    {
        consecutivePerfectCount = 0;
    }
    
    /// <summary>
    /// 손님 만족 처리
    /// </summary>
    public int ProcessCustomerSatisfaction(int basePoints)
    {
        consecutiveSatisfiedCount++;
        totalSatisfiedCustomers++;
        
        // 연속 만족 기록 업데이트
        if (consecutiveSatisfiedCount > maxConsecutiveSatisfied)
        {
            maxConsecutiveSatisfied = consecutiveSatisfiedCount;
        }
        
        // 연속 만족 보너스 계산
        int bonusMultiplier = GetConsecutiveSatisfiedMultiplier();
        int totalPoints = basePoints * bonusMultiplier;
        
        // 포인트 추가
        todaysPoints += totalPoints;
        
        return totalPoints;
    }
    
    /// <summary>
    /// 손님 불만족 처리 (연속 기록 초기화)
    /// </summary>
    public void ProcessCustomerDissatisfaction()
    {
        consecutiveSatisfiedCount = 0;
    }
    
    /// <summary>
    /// 연속 Perfect 배수 계산
    /// </summary>
    int GetConsecutivePerfectMultiplier()
    {
        if (consecutivePerfectCount >= 10) return 5;      // 10연속: 5배
        else if (consecutivePerfectCount >= 7) return 4;  // 7연속: 4배
        else if (consecutivePerfectCount >= 5) return 3;  // 5연속: 3배
        else if (consecutivePerfectCount >= 3) return 2;  // 3연속: 2배
        else return 1;                                    // 기본: 1배
    }
    
    /// <summary>
    /// 연속 만족 배수 계산
    /// </summary>
    int GetConsecutiveSatisfiedMultiplier()
    {
        if (consecutiveSatisfiedCount >= 10) return 3;    // 10연속: 3배
        else if (consecutiveSatisfiedCount >= 5) return 2; // 5연속: 2배
        else return 1;                                    // 기본: 1배
    }
    
    /// <summary>
    /// PlayerPrefs에 저장
    /// </summary>
    public void SaveToPlayerPrefs()
    {
        PlayerPrefs.SetInt("Point_Current", currentPoints);
        PlayerPrefs.SetInt("Point_TotalLifetime", totalLifetimePoints);
        PlayerPrefs.SetInt("Point_HighestDaily", highestDailyPoints);
        PlayerPrefs.SetInt("Point_TotalPerfect", totalPerfectPresses);
        PlayerPrefs.SetInt("Point_TotalGood", totalGoodPresses);
        PlayerPrefs.SetInt("Point_MaxPerfectStreak", maxConsecutivePerfect);
        PlayerPrefs.SetInt("Point_TotalSatisfied", totalSatisfiedCustomers);
        PlayerPrefs.SetInt("Point_MaxSatisfiedStreak", maxConsecutiveSatisfied);
        PlayerPrefs.Save();
    }
    
    /// <summary>
    /// PlayerPrefs에서 로드
    /// </summary>
    public void LoadFromPlayerPrefs()
    {
        currentPoints = PlayerPrefs.GetInt("Point_Current", 0);
        totalLifetimePoints = PlayerPrefs.GetInt("Point_TotalLifetime", 0);
        highestDailyPoints = PlayerPrefs.GetInt("Point_HighestDaily", 0);
        totalPerfectPresses = PlayerPrefs.GetInt("Point_TotalPerfect", 0);
        totalGoodPresses = PlayerPrefs.GetInt("Point_TotalGood", 0);
        maxConsecutivePerfect = PlayerPrefs.GetInt("Point_MaxPerfectStreak", 0);
        totalSatisfiedCustomers = PlayerPrefs.GetInt("Point_TotalSatisfied", 0);
        maxConsecutiveSatisfied = PlayerPrefs.GetInt("Point_MaxSatisfiedStreak", 0);
        
        // 오늘 포인트와 연속 기록은 항상 0으로 시작
        todaysPoints = 0;
        consecutivePerfectCount = 0;
        consecutiveSatisfiedCount = 0;
    }
    
    /// <summary>
    /// 연속 기록 상태 텍스트 반환
    /// </summary>
    public string GetStreakStatusText()
    {
        string status = "";
        
        if (consecutivePerfectCount > 0)
        {
            status += $"🔥 Perfect {consecutivePerfectCount}연속";
        }
        
        if (consecutiveSatisfiedCount > 0)
        {
            if (!string.IsNullOrEmpty(status)) status += " | ";
            status += $"😊 만족 {consecutiveSatisfiedCount}연속";
        }
        
        return string.IsNullOrEmpty(status) ? "연속 기록 없음" : status;
    }
    
    /// <summary>
    /// 디버그용 정보 출력
    /// </summary>
    public void PrintDebugInfo()
    {
        UnityEngine.Debug.Log("=== 포인트 데이터 정보 ===");
        UnityEngine.Debug.Log($"현재 포인트: {currentPoints:N0}점");
        UnityEngine.Debug.Log($"오늘 포인트: {todaysPoints:N0}점");
        UnityEngine.Debug.Log($"누적 포인트: {totalLifetimePoints:N0}점");
        UnityEngine.Debug.Log($"최고 일일 포인트: {highestDailyPoints:N0}점");
        UnityEngine.Debug.Log($"Perfect 누르기: {totalPerfectPresses}회 (연속: {consecutivePerfectCount}회, 최대: {maxConsecutivePerfect}회)");
        UnityEngine.Debug.Log($"Good 누르기: {totalGoodPresses}회");
        UnityEngine.Debug.Log($"만족 손님: {totalSatisfiedCustomers}명 (연속: {consecutiveSatisfiedCount}명, 최대: {maxConsecutiveSatisfied}명)");
        UnityEngine.Debug.Log($"현재 연속 기록: {GetStreakStatusText()}");
    }
}