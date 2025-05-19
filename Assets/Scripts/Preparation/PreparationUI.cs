// PreparationUI.cs
using UnityEngine;
using UnityEngine.UI;

public class PreparationUI : MonoBehaviour
{
    public enum FillingType
    {
        None,
        Sugar,
        Seed
    }

    [Header("UI Elements - Dough")]
    public Button doughIconButton;
    public Image prepSlot_ContentImage;

    [Header("UI Elements - Fillings")]
    public Button sugarFillingButton;
    public Button seedFillingButton;

    [Header("Sprites - Dough & Preparation Slot")]
    public Sprite rawDoughSprite;

    [Header("Sprites - Filled Dough")]
    public Sprite rawDoughWithSugarSprite;
    public Sprite rawDoughWithSeedSprite;

    private bool isRawDoughOnPrepSlot = false;
    private FillingType currentFillingType = FillingType.None;

    void Start()
    {
        if (doughIconButton != null) doughIconButton.onClick.AddListener(OnDoughIconButtonClicked);
        else Debug.LogError("DoughIconButton이 할당되지 않았습니다!");

        if (sugarFillingButton != null) sugarFillingButton.onClick.AddListener(OnSugarFillingButtonClicked);
        else Debug.LogError("SugarFillingButton이 할당되지 않았습니다!");

        if (seedFillingButton != null) seedFillingButton.onClick.AddListener(OnSeedFillingButtonClicked);
        else Debug.LogError("SeedFillingButton이 할당되지 않았습니다!");

        InitializePreparationSlotAndUI();
    }

    void InitializePreparationSlotAndUI()
    {
        if (prepSlot_ContentImage != null)
        {
            prepSlot_ContentImage.sprite = null;
            Color c = prepSlot_ContentImage.color;
            c.a = 0f;
            prepSlot_ContentImage.color = c;
        }
        isRawDoughOnPrepSlot = false;
        currentFillingType = FillingType.None;

        if (doughIconButton != null) doughIconButton.interactable = true;
        UpdateFillingButtonsInteractable(); // 속 재료 버튼 초기 비활성화 (doughIconButton 상태에 따라 결정될 수도 있음)

        Debug.Log("준비대 및 관련 UI가 초기화되었습니다.");
    }

    void OnDoughIconButtonClicked()
    {
        Debug.Log("반죽 아이콘 버튼 클릭됨!");
        if (isRawDoughOnPrepSlot || currentFillingType != FillingType.None)
        {
            Debug.Log("이미 준비대에 작업 중인 호떡이 있습니다.");
            return;
        }

        if (prepSlot_ContentImage != null && rawDoughSprite != null)
        {
            prepSlot_ContentImage.sprite = rawDoughSprite;
            Color c = prepSlot_ContentImage.color;
            c.a = 1f;
            prepSlot_ContentImage.color = c;

            isRawDoughOnPrepSlot = true;
            currentFillingType = FillingType.None; // 중요: 아직 속은 안 채워짐
            Debug.Log("준비대에 생지 호떡이 올라감.");

            UpdateFillingButtonsInteractable();
            if (doughIconButton != null) doughIconButton.interactable = false;
        }
        else Debug.LogError("PrepSlot_ContentImage 또는 RawDoughSprite가 할당되지 않았습니다!");
    }

    void OnSugarFillingButtonClicked()
    {
        Debug.Log("설탕 속 버튼 클릭됨!");
        AddFilling(FillingType.Sugar, rawDoughWithSugarSprite);
    }

    void OnSeedFillingButtonClicked()
    {
        Debug.Log("씨앗 속 버튼 클릭됨!");
        AddFilling(FillingType.Seed, rawDoughWithSeedSprite);
    }

    void AddFilling(FillingType fillingToAdd, Sprite filledDoughSprite)
    {
        if (isRawDoughOnPrepSlot && currentFillingType == FillingType.None)
        {
            if (prepSlot_ContentImage != null && filledDoughSprite != null)
            {
                prepSlot_ContentImage.sprite = filledDoughSprite;
                currentFillingType = fillingToAdd;
                // isRawDoughOnPrepSlot은 여전히 true (생지 -> 속채워진 생지로 변경)
                Debug.Log(fillingToAdd.ToString() + " 속이 추가됨. 이제 철판에 올릴 준비 완료.");
                UpdateFillingButtonsInteractable(); // 모든 속 버튼 비활성화
            }
            else Debug.LogError("PrepSlot_ContentImage 또는 채워진 호떡 스프라이트가 할당되지 않았습니다!");
        }
        else Debug.Log("속을 추가할 수 없는 상태입니다. (준비대에 생지가 없거나 이미 속이 채워져 있음)");
    }

    void UpdateFillingButtonsInteractable()
    {
        bool canAddFilling = isRawDoughOnPrepSlot && currentFillingType == FillingType.None;
        if (sugarFillingButton != null) sugarFillingButton.interactable = canAddFilling;
        if (seedFillingButton != null) seedFillingButton.interactable = canAddFilling;
    }

    // GriddleSlot에서 호출할 함수들
    public bool IsHotteokReadyForGriddle()
    {
        // 생지가 올라와 있고, 속도 채워져 있어야 철판으로 옮길 준비 완료
        return isRawDoughOnPrepSlot && currentFillingType != FillingType.None;
    }

    public FillingType GetPreparedFillingType()
    {
        return currentFillingType;
    }

    // 철판에 호떡을 성공적으로 옮겼을 때 GriddleSlot에서 호출할 함수
    public void OnHotteokPlacedOnGriddle()
    {
        InitializePreparationSlotAndUI(); // 준비대 초기화 및 UI 상태 원복
        // doughIconButton은 InitializePreparationSlotAndUI 내부에서 활성화됨
        Debug.Log("호떡이 철판으로 옮겨져 준비대가 비워지고 UI가 초기화됨.");
    }
}