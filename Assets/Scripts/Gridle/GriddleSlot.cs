// GriddleSlot.cs - 콜라이더 관리 개선 버전

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
        }

        // 필수 연결 확인
        if (preparationUILogic == null) Debug.LogError($"[{gameObject.name}] PreparationUILogic이 연결되지 않았습니다!");
        if (hotteokPrefabToSpawn == null) Debug.LogError($"[{gameObject.name}] HotteokPrefabToSpawn이 연결되지 않았습니다!");
        if (unpressedSugarSprite == null) Debug.LogError($"[{gameObject.name}] UnpressedSugarSprite가 연결되지 않았습니다!");
        if (unpressedSeedSprite == null) Debug.LogError($"[{gameObject.name}] UnpressedSeedSprite가 연결되지 않았습니다!");
    }

    void OnMouseDown()
    {
        Debug.Log($"[{gameObject.name}] 슬롯 클릭됨! 현재 점유 상태: {isOccupied}");

        if (isOccupied)
        {
            if(currentHotteokOnSlot != null)
            {
                Debug.Log($"[{gameObject.name}] 이미 호떡이 있습니다. 종류: {currentHotteokOnSlot.GetComponent<HotteokOnGriddle>().currentFilling}");
            }
            return;
        }

        if (preparationUILogic != null && preparationUILogic.IsHotteokReadyForGriddle())
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
                    Debug.Log($"[{gameObject.name}] 호떡 초기화 완료");
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

                // ✅ 호떡의 콜라이더 상태 확인
                Collider2D hotteokCollider = currentHotteokOnSlot.GetComponent<Collider2D>();
                if (hotteokCollider != null)
                {
                    Debug.Log($"[{gameObject.name}] 호떡 콜라이더 상태 - 활성화: {hotteokCollider.enabled}, 타입: {hotteokCollider.GetType().Name}");
                }
                else
                {
                    Debug.LogError($"[{gameObject.name}] 생성된 호떡에 콜라이더가 없습니다!");
                }
            }
        }
        else
        {
            Debug.Log($"[{gameObject.name}] 비어있지만, 준비대에 준비된 호떡이 없습니다.");
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
        Debug.Log($"[{gameObject.name}] MakeSlotEmpty 호출됨");
        
        // 슬롯 상태 리셋
        currentHotteokOnSlot = null;
        isOccupied = false;
        
        // ✅ 슬롯이 비었으므로 다시 클릭을 받을 수 있도록 콜라이더를 켬
        if (slotCollider != null) 
        {
            slotCollider.enabled = true;
            Debug.Log($"[{gameObject.name}] 슬롯 콜라이더 재활성화됨");
        }

        Debug.Log($"[{gameObject.name}] 슬롯이 비워졌습니다.");
    }
}