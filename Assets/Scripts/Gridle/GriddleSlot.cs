// GriddleSlot.cs - 콜라이더 관리 및 상태 동기화 문제 해결 최종 버전

using UnityEngine;

public class GriddleSlot : MonoBehaviour
{
    [Header("연결 필수")]
    public PreparationUI preparationUILogic;
    public GameObject hotteokPrefabToSpawn;

    [Header("철판 위 호떡 초기 스프라이트")]
    public Sprite unpressedSugarSprite;
    public Sprite unpressedSeedSprite;

    private bool isOccupied = false;
    private GameObject currentHotteokOnSlot = null;
    private Collider2D slotCollider; // 콜라이더 참조 변수

    void Start()
    {
        // ✅ 콜라이더 컴포넌트를 미리 찾아둡니다.
        slotCollider = GetComponent<Collider2D>();
        if (slotCollider == null)
        {
            Debug.LogError($"[{gameObject.name}] Collider2D가 없습니다!");
        }
        else
        {
            Debug.Log($"[{gameObject.name}] 슬롯 콜라이더 초기화 완료: {slotCollider.GetType().Name}");
            // 시작할 때 콜라이더를 확실히 활성화
            slotCollider.enabled = true;
        }

        // 필수 연결 확인
        if (preparationUILogic == null) Debug.LogError($"[{gameObject.name}] PreparationUILogic이 연결되지 않았습니다!");
        if (hotteokPrefabToSpawn == null) Debug.LogError($"[{gameObject.name}] HotteokPrefabToSpawn이 연결되지 않았습니다!");
        if (unpressedSugarSprite == null) Debug.LogError($"[{gameObject.name}] UnpressedSugarSprite가 연결되지 않았습니다!");
        if (unpressedSeedSprite == null) Debug.LogError($"[{gameObject.name}] UnpressedSeedSprite가 연결되지 않았습니다!");
        
        // 주기적 상태 확인 시작
        InvokeRepeating(nameof(PeriodicStateCheck), 2.0f, 5.0f);
    }
    
    /// <summary>
    /// 주기적으로 슬롯 상태를 확인하고 문제가 있으면 수정
    /// </summary>
    void PeriodicStateCheck()
    {
        // isOccupied 플래그와 실제 호떡 존재 여부가 다를 경우
        if (isOccupied && currentHotteokOnSlot == null)
        {
            Debug.LogWarning($"[{gameObject.name}] 주기적 확인: 점유 상태이지만 호떡이 없음! 상태를 강제 수정합니다.");
            MakeSlotEmpty();
        }
        // 호떡은 없는데 콜라이더가 비활성화된 경우
        else if (!isOccupied && currentHotteokOnSlot == null && slotCollider != null && !slotCollider.enabled)
        {
            Debug.LogWarning($"[{gameObject.name}] 주기적 확인: 빈 슬롯이지만 콜라이더가 비활성화됨! 강제로 활성화합니다.");
            slotCollider.enabled = true;
        }
    }

    void Update()
    {
        // 백업 클릭 감지 시스템 - OnMouseDown이 작동하지 않을 때를 위한 대안
        if (Input.GetMouseButtonDown(0))
        {
            Vector3 mouseWorldPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            mouseWorldPos.z = 0f;
            
            // 마우스 위치가 이 슬롯의 콜라이더 범위 내에 있는지 확인
            if (slotCollider != null && slotCollider.bounds.Contains(mouseWorldPos))
            {
                HandleSlotClick();
            }
        }
    }

    void OnMouseDown()
    {
        HandleSlotClick();
    }

    void HandleSlotClick()
    {
        Debug.Log($"[{gameObject.name}] 슬롯 클릭 처리! 현재 점유 상태: {isOccupied}, 콜라이더 활성화: {slotCollider != null && slotCollider.enabled}");
        
        // 콜라이더 상태 강제 확인 및 수정
        ValidateColliderState();
        
        // --- ✨ 핵심 수정 부분 시작 ✨ ---
        // 만약 점유 상태(isOccupied=true)인데 실제 호떡(currentHotteokOnSlot)이 없다면,
        // 상태가 꼬인 것이므로 강제로 슬롯을 비웁니다.
        if (isOccupied && currentHotteokOnSlot == null)
        {
            Debug.LogWarning($"[{gameObject.name}] 상태 불일치 발견! (점유 상태지만 호떡 없음). 강제로 슬롯을 비웁니다.");
            MakeSlotEmpty();
        }
        // --- ✨ 핵심 수정 부분 끝 ✨ ---

        if (isOccupied)
        {
            if(currentHotteokOnSlot != null)
            {
                Debug.Log($"[{gameObject.name}] 이미 호떡이 있습니다. 종류: {currentHotteokOnSlot.GetComponent<HotteokOnGriddle>().currentFilling}");
            }
            return;
        }

        // 호떡을 올릴 준비가 되었는지 확인
        if (preparationUILogic != null && preparationUILogic.IsHotteokReadyForGriddle())
        {
            PlaceHotteok();
        }
        else
        {
            Debug.Log($"[{gameObject.name}] 비어있지만, 준비대에 준비된 호떡이 없습니다.");
        }
    }

    /// <summary>
    /// 콜라이더 상태를 검증하고 필요시 수정
    /// </summary>
    void ValidateColliderState()
    {
        // 콜라이더가 없으면 다시 찾기
        if (slotCollider == null)
        {
            slotCollider = GetComponent<Collider2D>();
            Debug.LogWarning($"[{gameObject.name}] 콜라이더가 null이었음. 다시 찾았음: {slotCollider != null}");
        }

        // 빈 슬롯인데 콜라이더가 비활성화되어 있으면 활성화
        if (!isOccupied && currentHotteokOnSlot == null && slotCollider != null && !slotCollider.enabled)
        {
            slotCollider.enabled = true;
            Debug.LogWarning($"[{gameObject.name}] 빈 슬롯이지만 콜라이더가 비활성화됨! 활성화했습니다.");
        }
    }

    void PlaceHotteok()
    {
        PreparationUI.FillingType fillingToPlace = preparationUILogic.GetPreparedFillingType();
        Sprite initialSpriteToUse = GetInitialSpriteForFilling(fillingToPlace);

        if (hotteokPrefabToSpawn != null && initialSpriteToUse != null)
        {
            // ✅ 호떡 생성
            currentHotteokOnSlot = Instantiate(hotteokPrefabToSpawn, transform.position, Quaternion.identity);
            Debug.Log($"[{gameObject.name}] 호떡 생성됨: {currentHotteokOnSlot.name}");

            HotteokOnGriddle hotteokScript = currentHotteokOnSlot.GetComponent<HotteokOnGriddle>();
            if (hotteokScript != null)
            {
                // 호떡 초기화
                hotteokScript.Initialize(fillingToPlace, initialSpriteToUse, this);
            }
            else
            {
                Debug.LogError($"[{gameObject.name}] HotteokOnGriddle 컴포넌트를 찾을 수 없습니다!");
            }

            isOccupied = true;
            preparationUILogic.OnHotteokPlacedOnGriddle();

            // ✅ 자기 자신의 콜라이더를 꺼서, 위에 생성된 호떡이 클릭될 수 있도록 함
            if (slotCollider != null) 
            {
                slotCollider.enabled = false;
                Debug.Log($"[{gameObject.name}] 슬롯 콜라이더 비활성화됨 (호떡 클릭 가능하도록)");
            }

            Debug.Log($"[{gameObject.name}] {fillingToPlace} 속 호떡이 놓였습니다.");
        }
    }

    Sprite GetInitialSpriteForFilling(PreparationUI.FillingType fillingType)
    {
        switch (fillingType)
        {
            case PreparationUI.FillingType.Sugar:
                return unpressedSugarSprite;
            case PreparationUI.FillingType.Seed:
                return unpressedSeedSprite;
            default:
                Debug.LogError($"[{gameObject.name}] 알 수 없는 속 타입: {fillingType}");
                return null;
        }
    }

    public void MakeSlotEmpty()
    {
        Debug.Log($"[{gameObject.name}] MakeSlotEmpty 호출됨 - 이전 상태: isOccupied={isOccupied}");
        
        // 슬롯 상태 리셋
        currentHotteokOnSlot = null;
        isOccupied = false;
        
        // ✅ 슬롯이 비었으므로 다시 클릭을 받을 수 있도록 콜라이더를 켬
        if (slotCollider != null) 
        {
            slotCollider.enabled = true;
            Debug.Log($"[{gameObject.name}] 슬롯 콜라이더 재활성화됨 - 활성화 상태: {slotCollider.enabled}");
        }
        else
        {
            Debug.LogError($"[{gameObject.name}] slotCollider가 null입니다! 콜라이더를 다시 찾습니다.");
            slotCollider = GetComponent<Collider2D>();
            if (slotCollider != null)
            {
                slotCollider.enabled = true;
                Debug.Log($"[{gameObject.name}] 콜라이더를 다시 찾아서 활성화했습니다.");
            }
        }

        Debug.Log($"[{gameObject.name}] 슬롯이 비워졌습니다. 최종 상태: isOccupied={isOccupied}, 콜라이더 활성화={slotCollider != null && slotCollider.enabled}");
    }

    /// <summary>
    /// 모든 그리들 슬롯의 상태를 확인하는 정적 메서드
    /// </summary>
    [ContextMenu("Check All Griddle Slots")]
    public void CheckAllGriddleSlots()
    {
        GriddleSlot[] allSlots = FindObjectsOfType<GriddleSlot>();
        Debug.Log($"=== 전체 그리들 슬롯 상태 확인 (총 {allSlots.Length}개) ===");
        
        for (int i = 0; i < allSlots.Length; i++)
        {
            GriddleSlot slot = allSlots[i];
            bool hasCollider = slot.slotCollider != null;
            bool colliderEnabled = hasCollider && slot.slotCollider.enabled;
            bool isOccupied = slot.isOccupied;
            bool hasHotteok = slot.currentHotteokOnSlot != null;
            
            string status = "❌ 문제 있음";
            if (!isOccupied && !hasHotteok && colliderEnabled)
            {
                status = "✅ 정상 (빈 슬롯)";
            }
            else if (isOccupied && hasHotteok && !colliderEnabled)
            {
                status = "✅ 정상 (점유됨)";
            }
            
            Debug.Log($"슬롯 {i+1} [{slot.name}]: {status}");
            Debug.Log($"  - 점유됨: {isOccupied}, 호떡있음: {hasHotteok}, 콜라이더활성화: {colliderEnabled}");
            
            // 문제가 있는 슬롯 자동 수정
            if (status.Contains("문제"))
            {
                Debug.LogWarning($"슬롯 {i+1} 문제 감지! 자동 수정 시도...");
                slot.ValidateColliderState();
                if (slot.isOccupied && slot.currentHotteokOnSlot == null)
                {
                    slot.MakeSlotEmpty();
                }
            }
        }
    }

    /// <summary>
    /// 특정 슬롯을 강제로 테스트하는 메서드
    /// </summary>
    [ContextMenu("Test This Slot")]
    public void TestThisSlot()
    {
        Debug.Log($"=== [{gameObject.name}] 슬롯 테스트 시작 ===");
        
        // 현재 상태 출력
        ForceCheckSlotState();
        
        // 마우스 시뮬레이션 (실제 마우스 위치 무시하고 강제 클릭)
        Debug.Log($"[{gameObject.name}] 강제 클릭 시뮬레이션...");
        HandleSlotClick();
        
        Debug.Log($"=== [{gameObject.name}] 슬롯 테스트 완료 ===");
    }

    /// <summary>
    /// 슬롯 상태 강제 확인 - 디버깅용
    /// </summary>
    [ContextMenu("Force Check Slot State")]
    public void ForceCheckSlotState()
    {
        Debug.Log($"=== [{gameObject.name}] 슬롯 상태 강제 확인 ===");
        Debug.Log($"isOccupied: {isOccupied}");
        Debug.Log($"currentHotteokOnSlot: {(currentHotteokOnSlot != null ? currentHotteokOnSlot.name : "null")}");
        Debug.Log($"slotCollider: {(slotCollider != null ? slotCollider.GetType().Name : "null")}");
        Debug.Log($"slotCollider.enabled: {(slotCollider != null ? slotCollider.enabled.ToString() : "N/A")}");
        
        // 실제 자식 오브젝트 확인
        bool hasHotteokChild = false;
        foreach (Transform child in transform)
        {
            if (child.GetComponent<HotteokOnGriddle>() != null)
            {
                hasHotteokChild = true;
                Debug.Log($"발견된 호떡 자식: {child.name}");
            }
        }
        
        // 상태 불일치 수정
        if (!isOccupied && currentHotteokOnSlot == null && !hasHotteokChild)
        {
            // 빈 슬롯이어야 하는 경우
            if (slotCollider != null && !slotCollider.enabled)
            {
                slotCollider.enabled = true;
                Debug.Log($"[{gameObject.name}] 빈 슬롯의 콜라이더를 활성화했습니다.");
            }
        }
        else if (isOccupied && currentHotteokOnSlot != null)
        {
            // 점유된 슬롯인 경우
            Debug.Log($"[{gameObject.name}] 점유된 슬롯입니다. 콜라이더 상태는 적절합니다.");
        }
        else
        {
            // 상태 불일치 발견
            Debug.LogWarning($"[{gameObject.name}] 상태 불일치 발견!");
            Debug.LogWarning($"isOccupied={isOccupied}, currentHotteokOnSlot={currentHotteokOnSlot != null}, hasHotteokChild={hasHotteokChild}");
        }
    }

    void OnDestroy()
    {
        // 주기적 체크 정리
        CancelInvoke(nameof(PeriodicStateCheck));
    }
}