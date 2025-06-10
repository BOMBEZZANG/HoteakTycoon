// Assets/Scripts/Customer/CustomerSpawner.cs
// 손님 생성 및 전체 관리를 담당하는 시스템

using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public class CustomerSpawner : MonoBehaviour
{
    [Header("손님 생성 설정")]
    public GameObject customerPrefab;           // 손님 프리팹
    public int maxCustomers = 3;                // 최대 동시 손님 수
    public float minSpawnInterval = 3.0f;       // 최소 스폰 간격
    public float maxSpawnInterval = 8.0f;       // 최대 스폰 간격
    public bool autoSpawn = true;               // 자동 스폰 여부
    
    [Header("카운터 위치 설정")]
    public Transform[] counterPositions;        // 카운터 위치들 (3개)
    public Vector3 enterStartOffset = new Vector3(-8f, 0f, 0f);  // 입장 시작 오프셋
    public Vector3 exitEndOffset = new Vector3(8f, 0f, 0f);     // 퇴장 끝 오프셋
    
    [Header("난이도 설정")]
    public float baseWaitTime = 20.0f;          // 기본 대기 시간
    public float difficultyIncreaseRate = 0.9f; // 난이도 증가율 (시간 감소)
    public float minWaitTime = 8.0f;            // 최소 대기 시간
    public int difficultyIncreaseInterval = 5;  // 몇 명마다 난이도 증가
    
    [Header("사운드 효과")]
    public AudioClip customerEnterSound;        // 손님 입장 소리
    public AudioClip satisfactionSound;         // 만족 소리
    public AudioClip warningSound;              // 경고 소리
    public AudioClip angrySound;                // 화남 소리
    public AudioClip doorBellSound;             // 문 벨 소리
    
    [Header("통계")]
    public int totalCustomersServed = 0;        // 총 서빙한 손님 수
    public int satisfiedCustomers = 0;          // 만족한 손님 수
    public int angryCustomers = 0;              // 화난 손님 수
    public float customerSatisfactionRate = 1.0f; // 만족도 비율
    
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
        
        Debug.Log($"✅ CustomerSpawner 초기화 완료! 최대 {maxCustomers}명 동시 수용");
    }
    
    /// <summary>
    /// 카운터 위치 검증
    /// </summary>
    void ValidateCounterPositions()
    {
        if (counterPositions == null || counterPositions.Length < maxCustomers)
        {
            Debug.LogError($"❌ 카운터 위치가 부족합니다! 필요: {maxCustomers}개, 현재: {(counterPositions?.Length ?? 0)}개");
            
            // 기본 위치 자동 생성
            CreateDefaultCounterPositions();
        }
        else
        {
            Debug.Log($"✅ {counterPositions.Length}개 카운터 위치 검증 완료");
        }
    }
    
    /// <summary>
    /// 기본 카운터 위치 생성
    /// </summary>
    void CreateDefaultCounterPositions()
    {
        counterPositions = new Transform[maxCustomers];
        
        for (int i = 0; i < maxCustomers; i++)
        {
            GameObject counterPos = new GameObject($"CounterPosition_{i}");
            counterPos.transform.SetParent(transform);
            
            // 적당한 간격으로 배치
            float x = (i - 1) * 2f; // 중앙 기준으로 좌우 배치
            float y = 2f;           // 호떡 철판 위쪽
            counterPos.transform.position = new Vector3(x, y, 0);
            
            counterPositions[i] = counterPos.transform;
        }
        
        Debug.Log("🔧 기본 카운터 위치 자동 생성 완료");
    }
    
    /// <summary>
    /// 스폰 시작
    /// </summary>
    public void StartSpawning()
    {
        if (spawnCoroutine == null)
        {
            spawnCoroutine = StartCoroutine(SpawnRoutine());
            Debug.Log("🎬 손님 스폰 시작!");
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
            Debug.Log("⏹️ 손님 스폰 중지!");
        }
    }
    
    /// <summary>
    /// 스폰 루틴
    /// </summary>
    IEnumerator SpawnRoutine()
    {
        while (true)
        {
            // 사용 가능한 카운터가 있고 손님 프리팹이 있는 경우에만 스폰
            if (availableCounters.Count > 0 && customerPrefab != null)
            {
                SpawnCustomer();
            }
            
            // 다음 스폰까지 대기
            float waitTime = Random.Range(minSpawnInterval, maxSpawnInterval);
            yield return new WaitForSeconds(waitTime);
        }
    }
    
    /// <summary>
    /// 손님 생성
    /// </summary>
    public void SpawnCustomer()
    {
        if (availableCounters.Count == 0)
        {
            Debug.Log("⚠️ 사용 가능한 카운터가 없어 손님을 생성할 수 없습니다.");
            return;
        }
        
        if (customerPrefab == null)
        {
            Debug.LogError("❌ customerPrefab이 설정되지 않았습니다!");
            return;
        }
        
        // 사용 가능한 카운터 선택
        int counterIndex = availableCounters.Dequeue();
        counterOccupied[counterIndex] = true;
        
        // 위치 계산
        Vector3 counterPos = counterPositions[counterIndex].position;
        Vector3 enterPos = counterPos + enterStartOffset;
        Vector3 exitPos = counterPos + exitEndOffset;
        
        // 손님 생성
        GameObject customerObj = Instantiate(customerPrefab, enterPos, Quaternion.identity);
        Customer customer = customerObj.GetComponent<Customer>();
        
        if (customer == null)
        {
            Debug.LogError("❌ customerPrefab에 Customer 컴포넌트가 없습니다!");
            Destroy(customerObj);
            return;
        }
        
        // 손님 설정
        customer.customerID = customerIdCounter++;
        customer.customerName = $"손님 {customer.customerID}";
        customer.SetSpawner(this);
        customer.SetPositions(enterPos, counterPos, exitPos);
        
        // 난이도에 따른 대기 시간 조정
        float adjustedWaitTime = CalculateWaitTime();
        customer.maxWaitTime = adjustedWaitTime;
        
        // 활성 손님 목록에 추가
        activeCustomers[counterIndex] = customer;
        
        // 문 벨 소리
        PlayDoorBellSound();
        
        Debug.Log($"👤 {customer.customerName} 생성됨! 카운터: {counterIndex}, 대기시간: {adjustedWaitTime:F1}초");
        
        // 이벤트 발생
        OnCustomerSpawned?.Invoke(customer);
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
            
            Debug.Log($"🚪 {customer.customerName} 퇴장 완료! 카운터 {counterIndex} 해제");
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
        
        Debug.Log($"📊 통계 업데이트: 총 {totalCustomersServed}명, 만족 {satisfiedCustomers}명, 불만 {angryCustomers}명 (만족도: {customerSatisfactionRate:P1})");
    }
    
    /// <summary>
    /// 사운드 효과 재생
    /// </summary>
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
        
        Debug.Log("🧹 모든 손님 제거 완료");
    }
    
    /// <summary>
    /// 난이도 설정
    /// </summary>
    public void SetDifficulty(float newBaseWaitTime, float newSpawnIntervalMin, float newSpawnIntervalMax)
    {
        baseWaitTime = newBaseWaitTime;
        minSpawnInterval = newSpawnIntervalMin;
        maxSpawnInterval = newSpawnIntervalMax;
        
        Debug.Log($"🎚️ 난이도 조정: 대기시간 {baseWaitTime}초, 스폰간격 {minSpawnInterval}-{maxSpawnInterval}초");
    }
    
    /// <summary>
    /// 현재 활성 손님 수 반환
    /// </summary>
    public int GetActiveCustomerCount()
    {
        return activeCustomers.Count(c => c != null);
    }
    
    /// <summary>
    /// 사용 가능한 카운터 수 반환
    /// </summary>
    public int GetAvailableCounterCount()
    {
        return availableCounters.Count;
    }
    
    /// <summary>
    /// 특정 위치의 손님 반환
    /// </summary>
    public Customer GetCustomerAtPosition(int counterIndex)
    {
        if (counterIndex >= 0 && counterIndex < activeCustomers.Length)
        {
            return activeCustomers[counterIndex];
        }
        return null;
    }
    
    /// <summary>
    /// 현재 통계 반환
    /// </summary>
    public (int total, int satisfied, int angry, float satisfactionRate) GetStatistics()
    {
        return (totalCustomersServed, satisfiedCustomers, angryCustomers, customerSatisfactionRate);
    }
    
    /// <summary>
    /// 통계 리셋
    /// </summary>
    public void ResetStatistics()
    {
        totalCustomersServed = 0;
        satisfiedCustomers = 0;
        angryCustomers = 0;
        customerSatisfactionRate = 1.0f;
        customerIdCounter = 1;
        
        Debug.Log("📊 통계 리셋 완료");
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
        
        for (int i = 0; i < activeCustomers.Length; i++)
        {
            if (activeCustomers[i] != null)
            {
                Debug.Log($"카운터 {i}: {activeCustomers[i].customerName} ({activeCustomers[i].GetCurrentState()})");
            }
        }
    }
}