// GriddleSlot.cs
// [최종 수정본] 호떡 생성 시 자신의 참조를 전달하도록 수정

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

    void Start()
    {
        if (preparationUILogic == null) Debug.LogError(gameObject.name + ": PreparationUILogic이 연결되지 않았습니다!");
        if (hotteokPrefabToSpawn == null) Debug.LogError(gameObject.name + ": HotteokPrefabToSpawn이 연결되지 않았습니다!");
        if (unpressedSugarSprite == null) Debug.LogError(gameObject.name + ": UnpressedSugarSprite가 연결되지 않았습니다!");
        if (unpressedSeedSprite == null) Debug.LogError(gameObject.name + ": UnpressedSeedSprite가 연결되지 않았습니다!");
    }

    void OnMouseDown()
    {
        Debug.Log(gameObject.name + " 클릭됨. 현재 점유 상태: " + isOccupied);

        if (isOccupied)
        {
            if(currentHotteokOnSlot != null)
            {
                 Debug.Log(gameObject.name + "에는 이미 호떡이 있습니다. 종류: " + currentHotteokOnSlot.GetComponent<HotteokOnGriddle>().currentFilling);
            }
            return;
        }

        if (preparationUILogic != null && preparationUILogic.IsHotteokReadyForGriddle())
        {
            PreparationUI.FillingType fillingToPlace = preparationUILogic.GetPreparedFillingType();
            Sprite initialSpriteToUse = GetInitialSpriteForFilling(fillingToPlace);

            if (hotteokPrefabToSpawn != null && initialSpriteToUse != null)
            {
                currentHotteokOnSlot = Instantiate(hotteokPrefabToSpawn, transform.position, Quaternion.identity);

                HotteokOnGriddle hotteokScript = currentHotteokOnSlot.GetComponent<HotteokOnGriddle>();
                if (hotteokScript != null)
                {
                    // [개선] Initialize 함수에 세 번째 인자로 this를 추가하여 자신을 알려줌
                    hotteokScript.Initialize(fillingToPlace, initialSpriteToUse, this); 
                }

                isOccupied = true;
                preparationUILogic.OnHotteokPlacedOnGriddle();

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
            default:
                Debug.LogError("알 수 없는 속 타입 또는 None 타입에 대한 초기 스프라이트가 없습니다: " + fillingType);
                return null;
        }
    }

    public void MakeSlotEmpty()
    {
        // 이제 호떡 오브젝트 자체는 StackSalesCounter가 관리하므로,
        // 이 슬롯은 단순히 점유 상태와 참조만 관리합니다.
        currentHotteokOnSlot = null;
        isOccupied = false;
        Debug.Log(gameObject.name + " 슬롯이 비워졌습니다.");
    }
}