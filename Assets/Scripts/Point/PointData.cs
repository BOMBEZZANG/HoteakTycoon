// Assets/Scripts/Point/PointData.cs
// 💎 포인트 관련 데이터 구조체 - 완전한 10% 보너스 시스템

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
    public int totalMissPresses = 0;               // 총 Miss 횟수
    public int consecutivePerfectCount = 0;        // 연속 Perfect 횟수
    public int maxConsecutivePerfect = 0;          // 최대 연속 Perfect 기록
    
    [Header("😊 만족 통계")]
    public int totalSatisfiedCustomers = 0;        // 총 만족한 손님 수
    public int totalAngryCustomers = 0;            // 총 화난 손님 수
    public int consecutiveSatisfiedCount = 0;      // 연속 만족 손님 수
    public int maxConsecutiveSatisfied = 0;        // 최대 연속 만족 기록
    
    [Header("📊 일별 통계")]
    public int totalDaysPlayed = 0;                // 총 플레이한 날 수
    public int bestDayScore = 0;                   // 최고 하루 점수
    public int averageDailyScore = 0;              // 평균 일일 점수
    
    [Header("🏆 성취 통계")]
    public int totalGoalsAchieved = 0;             // 총 목표 달성 횟수
    public int longestPerfectStreak = 0;           // 역대 최장 Perfect 연속
    public int longestSatisfactionStreak = 0;      // 역대 최장 만족 연속
    public int totalBonusPointsEarned = 0;         // 총 보너스 포인트 획득량
    
    [Header("⚡ 효율성 통계")]
    public float averagePressAccuracy = 0f;        // 평균 누르기 정확도
    public float averageCustomerSatisfactionRate = 0f; // 평균 손님 만족도
    public int totalHotteoksMade = 0;              // 총 제작한 호떡 수
    public int totalHotteoksSold = 0;              // 총 판매한 호떡 수
    
    /// <summary>
    /// 하루 시작 시 오늘 포인트 초기화
    /// </summary>
    public void StartNewDay()
    {
        todaysPoints = 0;
        consecutivePerfectCount = 0;
        consecutiveSatisfiedCount = 0;
        totalDaysPlayed++;
        
        UnityEngine.Debug.Log($"💎 새로운 하루 시작! (총 {totalDaysPlayed}일째)");
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
        
        // 최고 하루 점수 업데이트
        if (todaysPoints > bestDayScore)
        {
            bestDayScore = todaysPoints;
        }
        
        // 누적 포인트에 오늘 포인트 추가
        totalLifetimePoints += todaysPoints;
        
        // 현재 포인트에 오늘 포인트 추가 (누적)
        currentPoints += todaysPoints;
        
        // 평균 일일 점수 계산
        if (totalDaysPlayed > 0)
        {
            averageDailyScore = totalLifetimePoints / totalDaysPlayed;
        }
        
        // 평균 누르기 정확도 계산
        UpdatePressAccuracy();
        
        UnityEngine.Debug.Log($"💎 하루 종료! 오늘: {todaysPoints}점, 총합: {currentPoints}점, 평균: {averageDailyScore}점");
    }
    
    /// <summary>
    /// Perfect 누르기 처리 - 10% 보너스 시스템
    /// </summary>
    public int ProcessPerfectPress(int basePoints, float bonusPercentage = 10f)
    {
        consecutivePerfectCount++;
        totalPerfectPresses++;
        
        // 연속 Perfect 기록 업데이트
        if (consecutivePerfectCount > maxConsecutivePerfect)
        {
            maxConsecutivePerfect = consecutivePerfectCount;
        }
        
        // 역대 최장 기록 업데이트
        if (consecutivePerfectCount > longestPerfectStreak)
        {
            longestPerfectStreak = consecutivePerfectCount;
        }
        
        // 연속 보너스 계산 (10% 추가)
        int totalPoints = basePoints;
        int bonusPoints = 0;
        
        if (consecutivePerfectCount > 1)
        {
            float bonusMultiplier = 1f + ((consecutivePerfectCount - 1) * bonusPercentage / 100f);
            totalPoints = Mathf.RoundToInt(basePoints * bonusMultiplier);
            bonusPoints = totalPoints - basePoints;
            totalBonusPointsEarned += bonusPoints;
        }
        
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
        totalMissPresses++;
        consecutivePerfectCount = 0;
    }
    
    /// <summary>
    /// 손님 만족 처리 - 10% 보너스 시스템
    /// </summary>
    public int ProcessCustomerSatisfaction(int basePoints, float bonusPercentage = 10f)
    {
        consecutiveSatisfiedCount++;
        totalSatisfiedCustomers++;
        
        // 연속 만족 기록 업데이트
        if (consecutiveSatisfiedCount > maxConsecutiveSatisfied)
        {
            maxConsecutiveSatisfied = consecutiveSatisfiedCount;
        }
        
        // 역대 최장 기록 업데이트
        if (consecutiveSatisfiedCount > longestSatisfactionStreak)
        {
            longestSatisfactionStreak = consecutiveSatisfiedCount;
        }
        
        // 연속 만족 보너스 계산 (10% 추가)
        int totalPoints = basePoints;
        int bonusPoints = 0;
        
        if (consecutiveSatisfiedCount > 1)
        {
            float bonusMultiplier = 1f + ((consecutiveSatisfiedCount - 1) * bonusPercentage / 100f);
            totalPoints = Mathf.RoundToInt(basePoints * bonusMultiplier);
            bonusPoints = totalPoints - basePoints;
            totalBonusPointsEarned += bonusPoints;
        }
        
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
        totalAngryCustomers++;
    }
    
    /// <summary>
    /// 목표 달성 처리
    /// </summary>
    public void ProcessGoalAchievement()
    {
        totalGoalsAchieved++;
        UnityEngine.Debug.Log($"🏆 목표 달성! (총 {totalGoalsAchieved}회)");
    }
    
    /// <summary>
    /// 호떡 제작 처리
    /// </summary>
    public void ProcessHotteokMade()
    {
        totalHotteoksMade++;
    }
    
    /// <summary>
    /// 호떡 판매 처리
    /// </summary>
    public void ProcessHotteokSold()
    {
        totalHotteoksSold++;
    }
    
    /// <summary>
    /// 평균 누르기 정확도 업데이트
    /// </summary>
    void UpdatePressAccuracy()
    {
        int totalPresses = totalPerfectPresses + totalGoodPresses + totalMissPresses;
        if (totalPresses > 0)
        {
            averagePressAccuracy = (float)(totalPerfectPresses + totalGoodPresses) / totalPresses;
        }
    }
    
    /// <summary>
    /// 평균 손님 만족도 업데이트
    /// </summary>
    public void UpdateCustomerSatisfactionRate()
    {
        int totalCustomers = totalSatisfiedCustomers + totalAngryCustomers;
        if (totalCustomers > 0)
        {
            averageCustomerSatisfactionRate = (float)totalSatisfiedCustomers / totalCustomers;
        }
    }
    
    /// <summary>
    /// PlayerPrefs에 저장
    /// </summary>
    public void SaveToPlayerPrefs()
    {
        // 기본 포인트 정보
        PlayerPrefs.SetInt("Point_Current", currentPoints);
        PlayerPrefs.SetInt("Point_TotalLifetime", totalLifetimePoints);
        PlayerPrefs.SetInt("Point_HighestDaily", highestDailyPoints);
        
        // 누르기 통계
        PlayerPrefs.SetInt("Point_TotalPerfect", totalPerfectPresses);
        PlayerPrefs.SetInt("Point_TotalGood", totalGoodPresses);
        PlayerPrefs.SetInt("Point_TotalMiss", totalMissPresses);
        PlayerPrefs.SetInt("Point_MaxPerfectStreak", maxConsecutivePerfect);
        
        // 만족 통계
        PlayerPrefs.SetInt("Point_TotalSatisfied", totalSatisfiedCustomers);
        PlayerPrefs.SetInt("Point_TotalAngry", totalAngryCustomers);
        PlayerPrefs.SetInt("Point_MaxSatisfiedStreak", maxConsecutiveSatisfied);
        
        // 일별 통계
        PlayerPrefs.SetInt("Point_TotalDays", totalDaysPlayed);
        PlayerPrefs.SetInt("Point_BestDay", bestDayScore);
        PlayerPrefs.SetInt("Point_AverageDaily", averageDailyScore);
        
        // 성취 통계
        PlayerPrefs.SetInt("Point_TotalGoals", totalGoalsAchieved);
        PlayerPrefs.SetInt("Point_LongestPerfect", longestPerfectStreak);
        PlayerPrefs.SetInt("Point_LongestSatisfaction", longestSatisfactionStreak);
        PlayerPrefs.SetInt("Point_TotalBonus", totalBonusPointsEarned);
        
        // 효율성 통계
        PlayerPrefs.SetFloat("Point_PressAccuracy", averagePressAccuracy);
        PlayerPrefs.SetFloat("Point_CustomerSatisfactionRate", averageCustomerSatisfactionRate);
        PlayerPrefs.SetInt("Point_HotteoksMade", totalHotteoksMade);
        PlayerPrefs.SetInt("Point_HotteoksSold", totalHotteoksSold);
        
        PlayerPrefs.Save();
        
        UnityEngine.Debug.Log("💾 포인트 데이터 저장 완료");
    }
    
    /// <summary>
    /// PlayerPrefs에서 로드
    /// </summary>
    public void LoadFromPlayerPrefs()
    {
        // 기본 포인트 정보
        currentPoints = PlayerPrefs.GetInt("Point_Current", 0);
        totalLifetimePoints = PlayerPrefs.GetInt("Point_TotalLifetime", 0);
        highestDailyPoints = PlayerPrefs.GetInt("Point_HighestDaily", 0);
        
        // 누르기 통계
        totalPerfectPresses = PlayerPrefs.GetInt("Point_TotalPerfect", 0);
        totalGoodPresses = PlayerPrefs.GetInt("Point_TotalGood", 0);
        totalMissPresses = PlayerPrefs.GetInt("Point_TotalMiss", 0);
        maxConsecutivePerfect = PlayerPrefs.GetInt("Point_MaxPerfectStreak", 0);
        
        // 만족 통계
        totalSatisfiedCustomers = PlayerPrefs.GetInt("Point_TotalSatisfied", 0);
        totalAngryCustomers = PlayerPrefs.GetInt("Point_TotalAngry", 0);
        maxConsecutiveSatisfied = PlayerPrefs.GetInt("Point_MaxSatisfiedStreak", 0);
        
        // 일별 통계
        totalDaysPlayed = PlayerPrefs.GetInt("Point_TotalDays", 0);
        bestDayScore = PlayerPrefs.GetInt("Point_BestDay", 0);
        averageDailyScore = PlayerPrefs.GetInt("Point_AverageDaily", 0);
        
        // 성취 통계
        totalGoalsAchieved = PlayerPrefs.GetInt("Point_TotalGoals", 0);
        longestPerfectStreak = PlayerPrefs.GetInt("Point_LongestPerfect", 0);
        longestSatisfactionStreak = PlayerPrefs.GetInt("Point_LongestSatisfaction", 0);
        totalBonusPointsEarned = PlayerPrefs.GetInt("Point_TotalBonus", 0);
        
        // 효율성 통계
        averagePressAccuracy = PlayerPrefs.GetFloat("Point_PressAccuracy", 0f);
        averageCustomerSatisfactionRate = PlayerPrefs.GetFloat("Point_CustomerSatisfactionRate", 0f);
        totalHotteoksMade = PlayerPrefs.GetInt("Point_HotteoksMade", 0);
        totalHotteoksSold = PlayerPrefs.GetInt("Point_HotteoksSold", 0);
        
        // 오늘 포인트와 연속 기록은 항상 0으로 시작
        todaysPoints = 0;
        consecutivePerfectCount = 0;
        consecutiveSatisfiedCount = 0;
        
        UnityEngine.Debug.Log($"📁 포인트 데이터 로드 완료 - 총 포인트: {currentPoints}, 플레이 일수: {totalDaysPlayed}");
    }
    
    /// <summary>
    /// 모든 데이터 초기화 (리셋)
    /// </summary>
    public void ResetAllData()
    {
        currentPoints = 0;
        todaysPoints = 0;
        totalLifetimePoints = 0;
        highestDailyPoints = 0;
        
        totalPerfectPresses = 0;
        totalGoodPresses = 0;
        totalMissPresses = 0;
        consecutivePerfectCount = 0;
        maxConsecutivePerfect = 0;
        
        totalSatisfiedCustomers = 0;
        totalAngryCustomers = 0;
        consecutiveSatisfiedCount = 0;
        maxConsecutiveSatisfied = 0;
        
        totalDaysPlayed = 0;
        bestDayScore = 0;
        averageDailyScore = 0;
        
        totalGoalsAchieved = 0;
        longestPerfectStreak = 0;
        longestSatisfactionStreak = 0;
        totalBonusPointsEarned = 0;
        
        averagePressAccuracy = 0f;
        averageCustomerSatisfactionRate = 0f;
        totalHotteoksMade = 0;
        totalHotteoksSold = 0;
        
        UnityEngine.Debug.Log("🔄 모든 포인트 데이터 초기화 완료");
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
            if (consecutivePerfectCount > 1)
            {
                int bonusPercentage = (consecutivePerfectCount - 1) * 10;
                status += $" (+{bonusPercentage}%)";
            }
        }
        
        if (consecutiveSatisfiedCount > 0)
        {
            if (!string.IsNullOrEmpty(status)) status += " | ";
            status += $"😊 만족 {consecutiveSatisfiedCount}연속";
            if (consecutiveSatisfiedCount > 1)
            {
                int bonusPercentage = (consecutiveSatisfiedCount - 1) * 10;
                status += $" (+{bonusPercentage}%)";
            }
        }
        
        return string.IsNullOrEmpty(status) ? "연속 기록 없음" : status;
    }
    
    /// <summary>
    /// 상세 통계 텍스트 반환
    /// </summary>
    public string GetDetailedStatsText()
    {
        string stats = "=== 상세 통계 ===\n";
        
        // 포인트 정보
        stats += $"💎 총 포인트: {currentPoints:N0}\n";
        stats += $"📅 오늘 포인트: {todaysPoints:N0}\n";
        stats += $"📈 누적 포인트: {totalLifetimePoints:N0}\n";
        stats += $"🏆 최고 일일: {highestDailyPoints:N0}\n";
        stats += $"📊 평균 일일: {averageDailyScore:N0}\n\n";
        
        // 누르기 통계
        stats += $"🎯 Perfect: {totalPerfectPresses}회\n";
        stats += $"👍 Good: {totalGoodPresses}회\n";
        stats += $"❌ Miss: {totalMissPresses}회\n";
        stats += $"🔥 최장 Perfect: {maxConsecutivePerfect}연속\n";
        stats += $"📊 누르기 정확도: {averagePressAccuracy:P1}\n\n";
        
        // 손님 통계
        stats += $"😊 만족 손님: {totalSatisfiedCustomers}명\n";
        stats += $"😡 화난 손님: {totalAngryCustomers}명\n";
        stats += $"🌟 최장 만족: {maxConsecutiveSatisfied}연속\n";
        stats += $"📊 만족도: {averageCustomerSatisfactionRate:P1}\n\n";
        
        // 성취 통계
        stats += $"🏆 목표 달성: {totalGoalsAchieved}회\n";
        stats += $"💰 보너스 포인트: {totalBonusPointsEarned:N0}\n";
        stats += $"🍯 제작 호떡: {totalHotteoksMade}개\n";
        stats += $"💵 판매 호떡: {totalHotteoksSold}개\n";
        stats += $"📅 플레이 일수: {totalDaysPlayed}일\n";
        
        return stats;
    }
    
    /// <summary>
    /// 효율성 등급 반환
    /// </summary>
    public string GetEfficiencyGrade()
    {
        float overallScore = 0f;
        int factors = 0;
        
        // 누르기 정확도 (40% 가중치)
        if (averagePressAccuracy > 0)
        {
            overallScore += averagePressAccuracy * 0.4f;
            factors++;
        }
        
        // 손님 만족도 (40% 가중치)
        if (averageCustomerSatisfactionRate > 0)
        {
            overallScore += averageCustomerSatisfactionRate * 0.4f;
            factors++;
        }
        
        // 호떡 판매 효율성 (20% 가중치)
        if (totalHotteoksMade > 0)
        {
            float salesEfficiency = (float)totalHotteoksSold / totalHotteoksMade;
            overallScore += salesEfficiency * 0.2f;
            factors++;
        }
        
        if (factors == 0) return "평가 불가";
        
        // 등급 계산
        if (overallScore >= 0.9f) return "S (완벽)";
        else if (overallScore >= 0.8f) return "A (우수)";
        else if (overallScore >= 0.7f) return "B (양호)";
        else if (overallScore >= 0.6f) return "C (보통)";
        else if (overallScore >= 0.5f) return "D (개선 필요)";
        else return "F (많은 연습 필요)";
    }
    
    /// <summary>
    /// 성취도 체크
    /// </summary>
    public bool[] GetAchievements()
    {
        bool[] achievements = new bool[20];
        
        achievements[0] = totalPerfectPresses >= 100;          // Perfect 100회
        achievements[1] = totalGoodPresses >= 200;             // Good 200회
        achievements[2] = maxConsecutivePerfect >= 10;         // Perfect 10연속
        achievements[3] = totalSatisfiedCustomers >= 50;       // 손님 50명 만족
        achievements[4] = consecutiveSatisfiedCount >= 5;      // 손님 5연속 만족
        achievements[5] = totalDaysPlayed >= 7;                // 7일 플레이
        achievements[6] = highestDailyPoints >= 2000;          // 하루 2000점
        achievements[7] = totalLifetimePoints >= 10000;        // 누적 10000점
        achievements[8] = totalGoalsAchieved >= 5;             // 목표 5회 달성
        achievements[9] = averagePressAccuracy >= 0.8f;        // 정확도 80%
        achievements[10] = averageCustomerSatisfactionRate >= 0.9f; // 만족도 90%
        achievements[11] = totalHotteoksSold >= 100;           // 호떡 100개 판매
        achievements[12] = totalBonusPointsEarned >= 1000;     // 보너스 1000점
        achievements[13] = longestPerfectStreak >= 15;         // 역대 최장 Perfect 15연속
        achievements[14] = longestSatisfactionStreak >= 10;    // 역대 최장 만족 10연속
        achievements[15] = bestDayScore >= 3000;               // 최고 하루 3000점
        achievements[16] = totalDaysPlayed >= 30;              // 30일 플레이
        achievements[17] = currentPoints >= 50000;             // 총 포인트 50000점
        achievements[18] = totalSatisfiedCustomers >= 200;     // 손님 200명 만족
        achievements[19] = GetEfficiencyGrade().StartsWith("S"); // S등급 달성
        
        return achievements;
    }
    
    /// <summary>
    /// 디버그용 정보 출력
    /// </summary>
    public void PrintDebugInfo()
    {
        UnityEngine.Debug.Log(GetDetailedStatsText());
        UnityEngine.Debug.Log($"🎯 현재 연속 기록: {GetStreakStatusText()}");
        UnityEngine.Debug.Log($"📊 효율성 등급: {GetEfficiencyGrade()}");
        
        // 성취도 체크
        bool[] achievements = GetAchievements();
        int achievedCount = 0;
        for (int i = 0; i < achievements.Length; i++)
        {
            if (achievements[i]) achievedCount++;
        }
        UnityEngine.Debug.Log($"🏆 성취도: {achievedCount}/{achievements.Length} 달성");
    }
    
    /// <summary>
    /// JSON 형태로 데이터 내보내기
    /// </summary>
    public string ExportToJson()
    {
        return JsonUtility.ToJson(this, true);
    }
    
    /// <summary>
    /// JSON에서 데이터 가져오기
    /// </summary>
    public void ImportFromJson(string json)
    {
        try
        {
            JsonUtility.FromJsonOverwrite(json, this);
            UnityEngine.Debug.Log("📁 JSON 데이터 가져오기 완료");
        }
        catch (System.Exception e)
        {
            UnityEngine.Debug.LogError($"❌ JSON 데이터 가져오기 실패: {e.Message}");
        }
    }
}