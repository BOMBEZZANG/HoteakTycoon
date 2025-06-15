// Assets/Scripts/Customer/CustomerSpawner.cs
// 🔧 손님 이미지 표시 문제 완전 해결 + 누락 메서드 추가 버전

using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public class CustomerSpawner : MonoBehaviour
{
    [Header("🎯 손님 생성 설정")]
    public GameObject customerPrefab;           // 손님 프리팹
    public int maxCustomers = 3;                // 최대 동시 손님 수 (기본 3명)
    public float minSpawnInterval = 3.0f;       // 최소 스폰 간격
    public float maxSpawnInterval = 8.0f;       // 최대 스폰 간격
    public bool autoSpawn = true;               // 자동 스폰 여부
    
    [Header("📍 카운터 위치 설정 (수동)")]
    public Transform[] counterPositions;        // 카운터 위치들 (3개)
    public Vector3 enterStartOffset = new Vector3(-8f, 0f, 0f);  // 입장 시작 오프셋
    public Vector3 exitEndOffset = new Vector3(8f, 0f, 0f);     // 퇴장 끝 오프셋
    
    [Header("⚡ 난이도 설정")]
    public float baseWaitTime = 20.0f;          // 기본 대기 시간
    public float difficultyIncreaseRate = 0.9f; // 난이도 증가율 (시간 감소)
    public float minWaitTime = 8.0f;            // 최소 대기 시간
    public int difficultyIncreaseInterval = 5;  // 몇 명마다 난이도 증가
    
    [Header("🔊 사운드 효과")]
    public AudioClip customerEnterSound;        // 손님 입장 소리
    public AudioClip satisfactionSound;         // 만족 소리
    public AudioClip warningSound;              // 경고 소리
    public AudioClip angrySound;                // 화남 소리
    public AudioClip doorBellSound;             // 문 벨 소리
    
    [Header("📊 통계 (읽기 전용)")]
    [SerializeField] private int totalCustomersServed = 0;        // 총 서빙한 손님 수
    [SerializeField] private int satisfiedCustomers = 0;          // 만족한 손님 수
    [SerializeField] private int angryCustomers = 0;              // 화난 손님 수
    [SerializeField] private float customerSatisfactionRate = 1.0f; // 만족도 비율
    
    [Header("🐛 디버그")]
    public bool enableDebugLogs = true;         // 디버그 로그 활성화
    public bool showGizmos = true;              // 기즈모 표시
    
    // 내부 관리
    private Customer[] activeCustomers;         // 현재 활성 손님들
    private bool[] counterOccupied;             // 카운터 점유 상태
    private Queue<int> availableCounters;       // 사용 가능한 카운터 큐
    private Coroutine spawnCoroutine;           // 스폰 코루틴
    private int customerIdCounter = 1;          // 손님 ID 카운터
    
    // 싱글톤
    public static CustomerSpawner Instance { get; private set; }
    
    // 이벤트
    public System.Action<Customer> OnCustomerSpawned;
    public System.Action<Customer, bool> OnCustomerLeft; // Customer, wasSatisfied
    
    void Awake()
    {
        // 싱글톤 설정
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }
        
        InitializeSpawner();
    }
    
    void Start()
    {
        if (autoSpawn)
        {
            StartSpawning();
        }
    }
    
    /// <summary>
    /// 스포너 초기화
    /// </summary>
    void InitializeSpawner()
    {
        DebugLog("🚀 CustomerSpawner 초기화 시작...");
        
        // 배열 초기화
        activeCustomers = new Customer[maxCustomers];
        counterOccupied = new bool[maxCustomers];
        availableCounters = new Queue<int>();
        
        // 사용 가능한 카운터 큐 초기화
        for (int i = 0; i < maxCustomers; i++)
        {
            availableCounters.Enqueue(i);
        }
        
        // 카운터 위치 검증
        ValidateCounterPositions();
        
        DebugLog($"✅ CustomerSpawner 초기화 완료! 최대 {maxCustomers}명 동시 수용");
    }
    
    /// <summary>
    /// 카운터 위치 검증
    /// </summary>
    void ValidateCounterPositions()
    {
        // 카운터 위치가 설정되지 않았거나 부족한 경우
        if (counterPositions == null || counterPositions.Length < maxCustomers)
        {
            Debug.LogWarning($"⚠️ 카운터 위치가 부족합니다! 필요: {maxCustomers}개, 현재: {(counterPositions?.Length ?? 0)}개");
            Debug.LogWarning("👉 Inspector에서 Counter Positions를 수동으로 설정해주세요!");
            return;
        }
        
        // 카운터 위치 유효성 검사
        bool allValid = true;
        for (int i = 0; i < maxCustomers; i++)
        {
            if (counterPositions[i] == null)
            {
                Debug.LogError($"❌ 카운터 {i}가 null입니다! Inspector에서 설정해주세요.");
                allValid = false;
            }
            else
            {
                DebugLog($"✅ 카운터 {i}: {counterPositions[i].position}");
            }
        }
        
        if (allValid)
        {
            DebugLog($"✅ 모든 카운터 위치가 올바르게 설정됨!");
        }
    }
    
    /// <summary>
    /// 스폰 시작
    /// </summary>
    public void StartSpawning()
    {
        if (spawnCoroutine == null)
        {
            DebugLog("🎬 손님 자동 스폰 시작!");
            spawnCoroutine = StartCoroutine(SpawnRoutine());
        }
        else
        {
            DebugLog("⚠️ 이미 스폰이 진행 중입니다.");
        }
    }
    
    /// <summary>
    /// 스폰 중지
    /// </summary>
    public void StopSpawning()
    {
        if (spawnCoroutine != null)
        {
            StopCoroutine(spawnCoroutine);
            spawnCoroutine = null;
            DebugLog("⏹️ 손님 자동 스폰 중지!");
        }
    }
    
    /// <summary>
    /// 스폰 루틴
    /// </summary>
    IEnumerator SpawnRoutine()
    {
        while (true)
        {
            // 기본 조건 체크
            if (availableCounters.Count > 0 && customerPrefab != null && 
                counterPositions != null && counterPositions.Length >= maxCustomers)
            {
                DebugLog($"🎯 손님 스폰 시도... (사용 가능한 카운터: {availableCounters.Count})");
                SpawnCustomer();
            }
            else
            {
                // 문제 진단
                if (availableCounters.Count == 0)
                {
                    DebugLog("⏳ 모든 카운터가 점유됨. 다음 스폰까지 대기...");
                }
                if (customerPrefab == null)
                {
                    Debug.LogError("❌ customerPrefab이 null입니다! Inspector에서 설정해주세요.");
                }
                if (counterPositions == null || counterPositions.Length < maxCustomers)
                {
                    Debug.LogError("❌ counterPositions가 부족합니다! Inspector에서 설정해주세요.");
                }
            }
            
            // 다음 스폰까지 대기
            float waitTime = Random.Range(minSpawnInterval, maxSpawnInterval);
            DebugLog($"⏰ 다음 스폰까지 {waitTime:F1}초 대기");
            yield return new WaitForSeconds(waitTime);
        }
    }
    
    /// <summary>
    /// 🔧 손님 생성 (이미지 표시 문제 해결)
    /// </summary>
    public void SpawnCustomer()
    {
        // 기본 조건 재확인
        if (availableCounters.Count == 0)
        {
            DebugLog("⚠️ 사용 가능한 카운터가 없어 손님을 생성할 수 없습니다.");
            return;
        }
        
        if (customerPrefab == null)
        {
            Debug.LogError("❌ customerPrefab이 설정되지 않았습니다!");
            return;
        }
        
        if (counterPositions == null || counterPositions.Length < maxCustomers)
        {
            Debug.LogError("❌ counterPositions가 올바르게 설정되지 않았습니다!");
            return;
        }
        
        // 사용 가능한 카운터 선택
        int counterIndex = availableCounters.Dequeue();
        counterOccupied[counterIndex] = true;
        
        // 카운터 위치 확인
        if (counterPositions[counterIndex] == null)
        {
            Debug.LogError($"❌ 카운터 {counterIndex}의 Transform이 null입니다!");
            
            // 카운터 다시 사용 가능하게 만들기
            counterOccupied[counterIndex] = false;
            availableCounters.Enqueue(counterIndex);
            return;
        }
        
        // 위치 계산
        Vector3 counterPos = counterPositions[counterIndex].position;
        Vector3 enterPos = counterPos + enterStartOffset;
        Vector3 exitPos = counterPos + exitEndOffset;
        
        DebugLog($"📍 카운터 {counterIndex} 사용");
        DebugLog($"   카운터 위치: {counterPos}");
        DebugLog($"   입장 위치: {enterPos}");
        DebugLog($"   퇴장 위치: {exitPos}");
        
        // 🔧 손님 생성 (입장 위치에서 바로 생성)
        GameObject customerObj = Instantiate(customerPrefab, enterPos, Quaternion.identity);
        Customer customer = customerObj.GetComponent<Customer>();
        
        if (customer == null)
        {
            Debug.LogError("❌ customerPrefab에 Customer 컴포넌트가 없습니다!");
            Destroy(customerObj);
            
            // 카운터 다시 사용 가능하게 만들기
            counterOccupied[counterIndex] = false;
            availableCounters.Enqueue(counterIndex);
            return;
        }
        
        // 🔧 Customer 설정 및 초기화
        int customerId = customerIdCounter++;
        string customerName = $"손님 {customerId}";
        
        // 기본 설정
        customer.SetSpawner(this);
        customer.SetPositions(enterPos, counterPos, exitPos);
        
        // 🔧 InitializeCustomer 메서드 호출 (스프라이트 초기화 포함)
        customer.InitializeCustomer(customerId, customerName, this);
        
        // 난이도에 따른 대기 시간 조정
        float adjustedWaitTime = CalculateWaitTime();
        customer.maxWaitTime = adjustedWaitTime;
        
        // 활성 손님 목록에 추가
        activeCustomers[counterIndex] = customer;
        
        // 문 벨 소리
        PlayDoorBellSound();
        
        DebugLog($"👤 {customerName} 생성 완료!");
        DebugLog($"   ID: {customerId}");
        DebugLog($"   위치: {customerObj.transform.position}");
        DebugLog($"   대기시간: {adjustedWaitTime:F1}초");
        
        // 🔍 스프라이트 렌더러 상태 확인
        SpriteRenderer customerSprite = customer.GetComponent<SpriteRenderer>();
        if (customerSprite != null)
        {
            DebugLog($"   스프라이트 렌더러: enabled={customerSprite.enabled}, sprite={customerSprite.sprite?.name ?? "null"}");
        }
        else
        {
            Debug.LogError($"❌ {customerName}: SpriteRenderer를 찾을 수 없습니다!");
        }
        
        // 이벤트 발생
        OnCustomerSpawned?.Invoke(customer);
    }
    
    /// <summary>
    /// 🔘 수동으로 손님 생성 (테스트용)
    /// </summary>
    [ContextMenu("Spawn Customer Manually")]
    public void SpawnCustomerManually()
    {
        DebugLog("🎯 수동으로 손님 생성 시도...");
        SpawnCustomer();
    }
    
    /// <summary>
    /// 난이도에 따른 대기 시간 계산
    /// </summary>
    float CalculateWaitTime()
    {
        int difficultyLevel = totalCustomersServed / difficultyIncreaseInterval;
        float adjustedTime = baseWaitTime * Mathf.Pow(difficultyIncreaseRate, difficultyLevel);
        return Mathf.Max(adjustedTime, minWaitTime);
    }
    
    /// <summary>
    /// 손님 퇴장 처리
    /// </summary>
    public void OnCustomerExit(Customer customer, bool wasSatisfied)
    {
        if (customer == null) return;
        
        // 카운터 해제
        int counterIndex = -1;
        for (int i = 0; i < activeCustomers.Length; i++)
        {
            if (activeCustomers[i] == customer)
            {
                counterIndex = i;
                break;
            }
        }
        
        if (counterIndex >= 0)
        {
            activeCustomers[counterIndex] = null;
            counterOccupied[counterIndex] = false;
            availableCounters.Enqueue(counterIndex);
            
            DebugLog($"🚪 {customer.customerName} 퇴장 완료! 카운터 {counterIndex} 해제 (만족: {wasSatisfied})");
        }
        
        // 통계 업데이트
        UpdateStatistics(wasSatisfied);
        
        // 이벤트 발생
        OnCustomerLeft?.Invoke(customer, wasSatisfied);
    }
    
    /// <summary>
    /// 통계 업데이트
    /// </summary>
    void UpdateStatistics(bool wasSatisfied)
    {
        totalCustomersServed++;
        
        if (wasSatisfied)
        {
            satisfiedCustomers++;
        }
        else
        {
            angryCustomers++;
        }
        
        // 만족도 비율 계산
        customerSatisfactionRate = (float)satisfiedCustomers / totalCustomersServed;
        
        DebugLog($"📊 통계 업데이트: 총 {totalCustomersServed}명, 만족 {satisfiedCustomers}명, 불만 {angryCustomers}명 (만족도: {customerSatisfactionRate:P1})");
    }
    
    /// <summary>
    /// 디버그 로그 출력 (활성화된 경우에만)
    /// </summary>
    void DebugLog(string message)
    {
        if (enableDebugLogs)
        {
            Debug.Log($"[CustomerSpawner] {message}");
        }
    }
    
    // ===== 📊 통계 접근자 메서드들 (GameManager에서 호출됨) =====
    
    /// <summary>
    /// 총 서빙한 손님 수 반환
    /// </summary>
    public int GetTotalCustomersServed()
    {
        return totalCustomersServed;
    }
    
    /// <summary>
    /// 만족한 손님 수 반환
    /// </summary>
    public int GetSatisfiedCustomers()
    {
        return satisfiedCustomers;
    }
    
    /// <summary>
    /// 화난 손님 수 반환
    /// </summary>
    public int GetAngryCustomers()
    {
        return angryCustomers;
    }
    
    /// <summary>
    /// 손님 만족도 비율 반환 (0.0 ~ 1.0)
    /// </summary>
    public float GetCustomerSatisfactionRate()
    {
        return customerSatisfactionRate;
    }
    
    // ===== 🎭 손님 상태 변경 콜백 메서드들 (Customer에서 호출됨) =====
    
    /// <summary>
    /// 손님이 만족하며 떠날 때 호출
    /// </summary>
    public void OnCustomerLeaveSatisfied()
    {
        // 주의: 이 메서드는 OnCustomerExit에서 이미 통계가 업데이트되므로 
        // 중복 업데이트를 방지하기 위해 사운드만 재생
        PlaySatisfactionSound();
        
        if (enableDebugLogs)
        {
            DebugLog($"😊 손님 만족 퇴장 신호 수신!");
        }
    }
    
    /// <summary>
    /// 손님이 화내며 떠날 때 호출
    /// </summary>
    public void OnCustomerLeaveAngry()
    {
        // 주의: 이 메서드는 OnCustomerExit에서 이미 통계가 업데이트되므로 
        // 중복 업데이트를 방지하기 위해 사운드만 재생
        PlayAngrySound();
        
        if (enableDebugLogs)
        {
            DebugLog($"😡 손님 분노 퇴장 신호 수신!");
        }
    }
    
    // ===== 🔊 사운드 효과 =====
    
    public void PlayEnterSound()
    {
        if (customerEnterSound != null)
        {
            AudioSource.PlayClipAtPoint(customerEnterSound, transform.position);
        }
    }
    
    public void PlaySatisfactionSound()
    {
        if (satisfactionSound != null)
        {
            AudioSource.PlayClipAtPoint(satisfactionSound, transform.position);
        }
    }
    
    public void PlayWarningSound()
    {
        if (warningSound != null)
        {
            AudioSource.PlayClipAtPoint(warningSound, transform.position);
        }
    }
    
    public void PlayAngrySound()
    {
        if (angrySound != null)
        {
            AudioSource.PlayClipAtPoint(angrySound, transform.position);
        }
    }
    
    void PlayDoorBellSound()
    {
        if (doorBellSound != null)
        {
            AudioSource.PlayClipAtPoint(doorBellSound, transform.position);
        }
    }
    
    /// <summary>
    /// 모든 손님 제거 (게임 리셋 시)
    /// </summary>
    public void ClearAllCustomers()
    {
        DebugLog("🧹 모든 손님 제거 시작...");
        
        for (int i = 0; i < activeCustomers.Length; i++)
        {
            if (activeCustomers[i] != null)
            {
                Destroy(activeCustomers[i].gameObject);
                activeCustomers[i] = null;
            }
            counterOccupied[i] = false;
        }
        
        // 사용 가능한 카운터 큐 재초기화
        availableCounters.Clear();
        for (int i = 0; i < maxCustomers; i++)
        {
            availableCounters.Enqueue(i);
        }
        
        DebugLog("✅ 모든 손님 제거 완료");
    }
    
    // ===== 기타 유틸리티 함수들 =====
    
    public void SetDifficulty(float newBaseWaitTime, float newSpawnIntervalMin, float newSpawnIntervalMax)
    {
        baseWaitTime = newBaseWaitTime;
        minSpawnInterval = newSpawnIntervalMin;
        maxSpawnInterval = newSpawnIntervalMax;
        
        DebugLog($"🎚️ 난이도 조정: 대기시간 {baseWaitTime}초, 스폰간격 {minSpawnInterval}-{maxSpawnInterval}초");
    }
    
    public int GetActiveCustomerCount()
    {
        return activeCustomers.Count(c => c != null);
    }
    
    public int GetAvailableCounterCount()
    {
        return availableCounters.Count;
    }
    
    public Customer GetCustomerAtPosition(int counterIndex)
    {
        if (counterIndex >= 0 && counterIndex < activeCustomers.Length)
        {
            return activeCustomers[counterIndex];
        }
        return null;
    }
    
    public (int total, int satisfied, int angry, float satisfactionRate) GetStatistics()
    {
        return (totalCustomersServed, satisfiedCustomers, angryCustomers, customerSatisfactionRate);
    }
    
    public void ResetStatistics()
    {
        totalCustomersServed = 0;
        satisfiedCustomers = 0;
        angryCustomers = 0;
        customerSatisfactionRate = 1.0f;
        customerIdCounter = 1;
        
        DebugLog("📊 통계 리셋 완료");
    }
    
    // ===== 📈 추가 통계 및 유틸리티 메서드들 =====
    
    /// <summary>
    /// 현재 활성 손님들의 상태 요약 반환
    /// </summary>
    public string GetActiveCustomersStatus()
    {
        int activeCount = GetActiveCustomerCount();
        if (activeCount == 0)
        {
            return "활성 손님 없음";
        }
        
        string status = $"활성 손님 {activeCount}명: ";
        for (int i = 0; i < activeCustomers.Length; i++)
        {
            if (activeCustomers[i] != null)
            {
                Customer customer = activeCustomers[i];
                status += $"[{customer.customerName}: {customer.GetCurrentState()}] ";
            }
        }
        
        return status;
    }
    
    /// <summary>
    /// 통계 요약 문자열 반환
    /// </summary>
    public string GetStatisticsSummary()
    {
        if (totalCustomersServed == 0)
        {
            return "아직 손님이 없습니다.";
        }
        
        return $"총 {totalCustomersServed}명 서빙 | 만족: {satisfiedCustomers}명 | 불만: {angryCustomers}명 | 만족도: {customerSatisfactionRate:P1}";
    }
    
    /// <summary>
    /// 특정 카운터의 손님 정보 반환
    /// </summary>
    public Customer GetCustomerAtCounter(int counterIndex)
    {
        if (counterIndex >= 0 && counterIndex < maxCustomers)
        {
            return activeCustomers[counterIndex];
        }
        return null;
    }
    
    /// <summary>
    /// 모든 활성 손님 목록 반환
    /// </summary>
    public List<Customer> GetAllActiveCustomers()
    {
        List<Customer> customers = new List<Customer>();
        
        foreach (Customer customer in activeCustomers)
        {
            if (customer != null)
            {
                customers.Add(customer);
            }
        }
        
        return customers;
    }
    
    /// <summary>
    /// 현재 난이도 레벨 반환
    /// </summary>
    public int GetCurrentDifficultyLevel()
    {
        return totalCustomersServed / difficultyIncreaseInterval;
    }
    
    /// <summary>
    /// 현재 계산된 대기 시간 반환
    /// </summary>
    public float GetCurrentWaitTime()
    {
        return CalculateWaitTime();
    }
    
    /// <summary>
    /// 스폰 간격 범위 반환
    /// </summary>
    public (float min, float max) GetSpawnInterval()
    {
        return (minSpawnInterval, maxSpawnInterval);
    }
    
    /// <summary>
    /// 카운터 점유 상태 반환
    /// </summary>
    public bool[] GetCounterOccupiedStatus()
    {
        return (bool[])counterOccupied.Clone();
    }
    
    /// <summary>
    /// 사용 가능한 카운터 인덱스 목록 반환
    /// </summary>
    public List<int> GetAvailableCounterIndices()
    {
        return new List<int>(availableCounters);
    }
    
    // ===== 🛠️ 디버그 및 테스트 메서드들 =====
    
    /// <summary>
    /// 특정 손님을 강제로 만족시키기 (디버그/테스트용)
    /// </summary>
    [ContextMenu("Force Satisfy All Customers")]
    public void ForceSatisfyAllCustomers()
    {
        if (!enableDebugLogs) return;
        
        foreach (Customer customer in activeCustomers)
        {
            if (customer != null && customer.GetCurrentState() == Customer.CustomerState.Waiting)
            {
                customer.LeaveSatisfied();
            }
        }
        
        DebugLog("🧪 모든 활성 손님을 강제로 만족시켰습니다.");
    }
    
    /// <summary>
    /// 특정 손님을 강제로 화나게 하기 (디버그/테스트용)
    /// </summary>
    [ContextMenu("Force Anger All Customers")]
    public void ForceAngerAllCustomers()
    {
        if (!enableDebugLogs) return;
        
        foreach (Customer customer in activeCustomers)
        {
            if (customer != null && customer.GetCurrentState() == Customer.CustomerState.Waiting)
            {
                customer.LeaveAngry();
            }
        }
        
        DebugLog("🧪 모든 활성 손님을 강제로 화나게 했습니다.");
    }
    
    /// <summary>
    /// 손님 스폰 강제 실행 (디버그/테스트용)
    /// </summary>
    [ContextMenu("Force Spawn Customer Now")]
    public void ForceSpawnCustomerNow()
    {
        if (enableDebugLogs)
        {
            DebugLog("🧪 강제 손님 스폰 실행!");
            SpawnCustomer();
        }
    }
    
    /// <summary>
    /// 통계 초기화 및 새 게임 준비
    /// </summary>
    public void PrepareNewGame()
    {
        // 모든 손님 정리
        ClearAllCustomers();
        
        // 통계 리셋
        ResetStatistics();
        
        // 스폰 중지
        StopSpawning();
        
        DebugLog("🎮 새 게임 준비 완료!");
    }
    
    /// <summary>
    /// 디버그 정보 출력
    /// </summary>
    [ContextMenu("Debug Info")]
    public void PrintDebugInfo()
    {
        Debug.Log("=== CustomerSpawner Debug Info ===");
        Debug.Log($"활성 손님 수: {GetActiveCustomerCount()}/{maxCustomers}");
        Debug.Log($"사용 가능한 카운터: {GetAvailableCounterCount()}");
        Debug.Log($"총 서빙: {totalCustomersServed}명");
        Debug.Log($"만족도: {customerSatisfactionRate:P1}");
        Debug.Log($"현재 대기시간: {CalculateWaitTime():F1}초");
        Debug.Log($"스폰 상태: {(spawnCoroutine != null ? "진행 중" : "중지됨")}");
        
        // 카운터 위치 정보
        if (counterPositions != null)
        {
            for (int i = 0; i < counterPositions.Length && i < maxCustomers; i++)
            {
                if (counterPositions[i] != null)
                {
                    Vector3 pos = counterPositions[i].position;
                    bool isOccupied = activeCustomers[i] != null;
                    string customerName = isOccupied ? activeCustomers[i].customerName : "비어있음";
                    Debug.Log($"카운터 {i}: {pos} - {customerName}");
                }
                else
                {
                    Debug.Log($"카운터 {i}: NULL");
                }
            }
        }
        else
        {
            Debug.Log("카운터 위치가 설정되지 않음!");
        }
    }
    
    /// <summary>
    /// 상세 디버그 정보 출력
    /// </summary>
    [ContextMenu("Print Detailed Debug Info")]
    public void PrintDetailedDebugInfo()
    {
        Debug.Log("=== CustomerSpawner 상세 정보 ===");
        Debug.Log($"📊 통계: {GetStatisticsSummary()}");
        Debug.Log($"🎚️ 난이도 레벨: {GetCurrentDifficultyLevel()}");
        Debug.Log($"⏰ 현재 대기시간: {GetCurrentWaitTime():F1}초");
        Debug.Log($"🕐 스폰 간격: {minSpawnInterval:F1}초 ~ {maxSpawnInterval:F1}초");
        Debug.Log($"👥 {GetActiveCustomersStatus()}");
        Debug.Log($"📍 카운터 점유: {string.Join(", ", counterOccupied.Select((occupied, i) => $"{i}:{(occupied ? "점유" : "비움")}"))}");
        Debug.Log($"🔄 스폰 상태: {(spawnCoroutine != null ? "진행 중" : "중지됨")}");
        
        // 활성 손님들의 상세 정보
        for (int i = 0; i < activeCustomers.Length; i++)
        {
            if (activeCustomers[i] != null)
            {
                Customer customer = activeCustomers[i];
                Debug.Log($"   손님 {i}: {customer.customerName} - {customer.GetCurrentState()} - 대기: {customer.GetCurrentWaitTime():F1}/{customer.GetMaxWaitTime():F1}초");
            }
        }
    }
    
    /// <summary>
    /// 기즈모 그리기 (Scene 뷰에서 카운터 위치 확인용)
    /// </summary>
    void OnDrawGizmos()
    {
        if (!showGizmos || counterPositions == null) return;
        
        for (int i = 0; i < counterPositions.Length && i < maxCustomers; i++)
        {
            if (counterPositions[i] == null) continue;
            
            Vector3 pos = counterPositions[i].position;
            
            // 카운터 위치 표시 (파란색 원)
            Gizmos.color = Color.blue;
            Gizmos.DrawWireSphere(pos, 0.7f);
            
            // 입장 위치 표시 (초록색 원)
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(pos + enterStartOffset, 0.4f);
            
            // 퇴장 위치 표시 (빨간색 원)
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(pos + exitEndOffset, 0.4f);
            
            // 연결선 그리기
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(pos + enterStartOffset, pos);
            Gizmos.DrawLine(pos, pos + exitEndOffset);
        }
    }
}