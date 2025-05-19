// GriddleSlot.cs
using UnityEngine;

public class GriddleSlot : MonoBehaviour
{
    [Header("연결 필수")]
    public PreparationUI preparationUILogic; // UIManager 등 PreparationUI 스크립트를 가진 오브젝트
    public GameObject hotteokPrefabToSpawn;  // 철판에 생성될 호떡 프리팹

    [Header("철판 위 호떡 초기 스프라이트")]
    public Sprite unpressedSugarSprite;
    public Sprite unpressedSeedSprite;
    // public Sprite unpressedNoneSprite; // 속 없는 호떡이 가능하다면

    private bool isOccupied = false;
    private GameObject currentHotteokOnSlot = null; // 현재 이 슬롯에 있는 호떡 오브젝트

    void Start()
    {
        if (preparationUILogic == null) Debug.LogError(gameObject.name + ": PreparationUILogic이 연결되지 않았습니다!");
        if (hotteokPrefabToSpawn == null) Debug.LogError(gameObject.name + ": HotteokPrefabToSpawn이 연결되지 않았습니다!");
        if (unpressedSugarSprite == null) Debug.LogError(gameObject.name + ": UnpressedSugarSprite가 연결되지 않았습니다!");
        if (unpressedSeedSprite == null) Debug.LogError(gameObject.name + ": UnpressedSeedSprite가 연결되지 않았습니다!");
    }

    void OnMouseDown() // 이 GameObject에 Collider2D가 있어야 작동합니다.
    {
        Debug.Log(gameObject.name + " 클릭됨. 현재 점유 상태: " + isOccupied);

        if (isOccupied)
        {
            // 이미 호떡이 있는 슬롯을 클릭한 경우 (나중에 이 호떡을 누르거나 뒤집는 로직으로 연결)
            // 지금은 아무것도 안 함. 또는 현재 호떡의 상태를 로그로 보여줄 수 있음.
            if(currentHotteokOnSlot != null)
            {
                 Debug.Log(gameObject.name + "에는 이미 호떡이 있습니다. 종류: " + currentHotteokOnSlot.GetComponent<HotteokOnGriddle>().currentFilling);
            }
            return;
        }

        // 슬롯이 비어있다면, 준비대에서 호떡을 가져올 수 있는지 확인
        if (preparationUILogic != null && preparationUILogic.IsHotteokReadyForGriddle())
        {
            PreparationUI.FillingType fillingToPlace = preparationUILogic.GetPreparedFillingType();
            Sprite initialSpriteToUse = GetInitialSpriteForFilling(fillingToPlace);

            if (hotteokPrefabToSpawn != null && initialSpriteToUse != null)
            {
                // 새 호떡 오브젝트를 이 슬롯의 위치에 생성
                currentHotteokOnSlot = Instantiate(hotteokPrefabToSpawn, transform.position, Quaternion.identity);
                // 부모를 이 슬롯으로 설정하면 정리하기 편함 (선택사항)
                // currentHotteokOnSlot.transform.SetParent(transform);

                HotteokOnGriddle hotteokScript = currentHotteokOnSlot.GetComponent<HotteokOnGriddle>();
                if (hotteokScript != null)
                {
                    hotteokScript.Initialize(fillingToPlace, initialSpriteToUse);
                }

                isOccupied = true;
                preparationUILogic.OnHotteokPlacedOnGriddle(); // 준비대 비우기 및 UI 초기화 요청

                Debug.Log(fillingToPlace.ToString() + " 속 호떡이 " + gameObject.name + "에 놓였습니다.");
            }
        }
        else
        {
            Debug.Log(gameObject.name + "은(는) 비어있지만, 준비대에 준비된 호떡이 없습니다.");
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
            // case PreparationUI.FillingType.None:
            // return unpressedNoneSprite;
            default:
                Debug.LogError("알 수 없는 속 타입 또는 None 타입에 대한 초기 스프라이트가 없습니다: " + fillingType);
                return null;
        }
    }

    // 나중에 호떡이 완성되거나 타서 사라질 때 호출될 함수
    public void MakeSlotEmpty()
    {
        if (currentHotteokOnSlot != null)
        {
            Destroy(currentHotteokOnSlot); // 슬롯 위의 호떡 오브젝트 제거
            currentHotteokOnSlot = null;
        }
        isOccupied = false;
        Debug.Log(gameObject.name + " 슬롯이 비워졌습니다.");
    }
}