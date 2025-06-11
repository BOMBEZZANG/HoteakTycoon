// Assets/Scripts/StackSalesCounter.cs
// 상단 판매대 스택 관리 시스템 (완전한 수정 버전)

using UnityEngine;
using System.Collections.Generic;
using System.Collections;

public class StackSalesCounter : MonoBehaviour
{
    [Header("판매대 설정")]
    public int maxStackHeight = 3; // 각 종류별 최대 스택 높이
    
    [Header("UI 판매대 슬롯 설정 (종류별 1개씩)")]
    public RectTransform sugarStackSlot;     // 설탕 호떡 스택 슬롯
    public RectTransform seedStackSlot;      // 씨앗 호떡 스택 슬롯
    
    [Header("스택 설정")]
    public float stackOffset = 0.3f;         // 스택 간격 (Y축)
    public float stackScale = 0.9f;          // 위로 갈수록 작아지는 비율
    
    [Header("애니메이션 설정")]
    public float flyDuration = 1.0f;                    // 날아가는 시간
    public AnimationCurve flyCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f); // 애니메이션 커브
    public float flyHeight = 3.0f;                      // 날아갈 때 최대 높이
    public float stackDropDuration = 0.3f;              // 스택 재정렬 시간
    
    [Header("UI 피드백")]
    public AudioClip hotteokLandSound;                  // 호떡이 착지할 때 사운드
    public GameObject hotteokLandEffect;                // 착지 이펙트 프리팹
    public AudioClip stackFullWarningSound;             // 스택 가득참 경고음
    
    [Header("선택 시스템")]
    private GameObject selectedHotteok = null;           // 현재 선택된 호떡
    public Color selectedColor = Color.yellow;           // 선택된 호떡 색상
    public GameObject selectionIndicator;                // 선택 표시 UI (화살표 등)
    
    // 내부 데이터 관리 (스택 방식)
    private Dictionary<PreparationUI.FillingType, List<GameObject>> hotteokStacks;
    private Dictionary<PreparationUI.FillingType, RectTransform> stackSlotsByType;
    
    // 싱글톤 패턴
    public static StackSalesCounter Instance { get; private set; }
    
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
        
        InitializeStackSalesCounter();
    }
    
    void InitializeStackSalesCounter()
    {
        // 스택 딕셔너리 초기화
        hotteokStacks = new Dictionary<PreparationUI.FillingType, List<GameObject>>
        {
            { PreparationUI.FillingType.Sugar, new List<GameObject>() },
            { PreparationUI.FillingType.Seed, new List<GameObject>() }
        };
        
        // 슬롯 딕셔너리 초기화
        stackSlotsByType = new Dictionary<PreparationUI.FillingType, RectTransform>
        {
            { PreparationUI.FillingType.Sugar, sugarStackSlot },
            { PreparationUI.FillingType.Seed, seedStackSlot }
        };
        
        // 슬롯 연결 확인
        ValidateSlots();
        
        // 선택 표시 UI 비활성화
        if (selectionIndicator != null)
        {
            selectionIndicator.SetActive(false);
        }
        
        Debug.Log("스택 판매대 초기화 완료!");
    }
    
    void ValidateSlots()
    {
        string missingSlots = "";
        
        if (sugarStackSlot == null) missingSlots += "sugarStackSlot ";
        if (seedStackSlot == null) missingSlots += "seedStackSlot ";
        
        if (!string.IsNullOrEmpty(missingSlots))
        {
            Debug.LogError("판매대 슬롯이 연결되지 않음: " + missingSlots);
        }
        else
        {
            Debug.Log("✅ 모든 스택 슬롯이 올바르게 연결됨!");
        }
    }
    
    /// <summary>
    /// 완성된 호떡을 스택에 추가할 수 있는지 확인
    /// </summary>
    public bool CanAddHotteokToStack(PreparationUI.FillingType fillingType)
    {
        if (!hotteokStacks.ContainsKey(fillingType))
        {
            Debug.LogError("지원하지 않는 호떡 타입: " + fillingType);
            return false;
        }
        
        int currentCount = hotteokStacks[fillingType].Count;
        bool canAdd = currentCount < maxStackHeight;
        
        Debug.Log(fillingType + " 스택 추가 가능 여부: " + canAdd + " (현재: " + currentCount + "/" + maxStackHeight + ")");
        return canAdd;
    }
    
    /// <summary>
    /// 완성된 호떡을 스택으로 이동시키는 메인 함수
    /// </summary>
    public void AddHotteokToStack(GameObject hotteokObject, PreparationUI.FillingType fillingType)
    {
        if (!CanAddHotteokToStack(fillingType))
        {
            Debug.Log("스택이 가득 참! " + fillingType + " 호떡을 더 이상 추가할 수 없습니다.");
            ShowStackFullFeedback(fillingType);
            return;
        }
        
        // 목적지 슬롯 및 위치 계산
        RectTransform targetSlot = stackSlotsByType[fillingType];
        if (targetSlot == null)
        {
            Debug.LogError("사용 가능한 슬롯을 찾을 수 없습니다!");
            return;
        }
        
        Debug.Log(fillingType + " 호떡을 스택으로 이동 시작!");
        
        // 호떡을 스택으로 날려보내는 애니메이션 시작
        StartCoroutine(FlyHotteokToStack(hotteokObject, targetSlot, fillingType));
    }
    
    /// <summary>
    /// 스택 위치 계산 (Y축 오프셋 적용)
    /// </summary>
    Vector3 GetStackPosition(RectTransform baseSlot, int stackIndex)
    {
        Vector3 basePosition = GetWorldPositionFromUISlot(baseSlot);
        
        // Y축으로 스택 오프셋 적용
        Vector3 stackPosition = basePosition + Vector3.up * (stackOffset * stackIndex);
        
        return stackPosition;
    }
    
    /// <summary>
    /// UI 슬롯 위치를 월드 좌표로 변환
    /// </summary>
    Vector3 GetWorldPositionFromUISlot(RectTransform uiSlot)
    {
        if (uiSlot == null) return Vector3.zero;
        
        Canvas canvas = uiSlot.GetComponentInParent<Canvas>();
        if (canvas == null) return uiSlot.position;
        
        if (canvas.renderMode == RenderMode.ScreenSpaceOverlay)
        {
            Vector3 screenPoint = RectTransformUtility.WorldToScreenPoint(null, uiSlot.position);
            Vector3 worldPoint = Camera.main.ScreenToWorldPoint(new Vector3(screenPoint.x, screenPoint.y, Camera.main.nearClipPlane + 5f));
            return worldPoint;
        }
        else if (canvas.renderMode == RenderMode.WorldSpace)
        {
            return uiSlot.position;
        }
        else // Screen Space Camera
        {
            Vector3 screenPoint = RectTransformUtility.WorldToScreenPoint(canvas.worldCamera, uiSlot.position);
            Vector3 worldPoint = canvas.worldCamera.ScreenToWorldPoint(new Vector3(screenPoint.x, screenPoint.y, canvas.worldCamera.nearClipPlane + 5f));
            return worldPoint;
        }
    }
    
    /// <summary>
    /// 호떡을 스택으로 날려보내는 애니메이션
    /// </summary>
    IEnumerator FlyHotteokToStack(GameObject hotteokObject, RectTransform targetSlot, PreparationUI.FillingType fillingType)
    {
        Vector3 startPosition = hotteokObject.transform.position;
        
        // 스택에서의 위치 계산
        int stackIndex = hotteokStacks[fillingType].Count; // 현재 스택 높이
        Vector3 endPosition = GetStackPosition(targetSlot, stackIndex);
        
        Debug.Log("스택 이동: " + startPosition + " → " + endPosition + " (스택 높이: " + stackIndex + ")");
        
        float elapsedTime = 0f;
        
        // 호떡의 상태를 비활성화하여 게임플레이 로직과 분리
        HotteokOnGriddle hotteokScript = hotteokObject.GetComponent<HotteokOnGriddle>();
        if (hotteokScript != null)
        {
            hotteokScript.enabled = false;
        }
        
        // Collider 비활성화 (클릭 방지)
        Collider2D collider = hotteokObject.GetComponent<Collider2D>();
        if (collider != null)
        {
            collider.enabled = false;
        }
        
        while (elapsedTime < flyDuration)
        {
            elapsedTime += Time.deltaTime;
            float normalizedTime = elapsedTime / flyDuration;
            float curveValue = flyCurve.Evaluate(normalizedTime);
            
            // 포물선 경로 계산
            Vector3 currentPosition = Vector3.Lerp(startPosition, endPosition, curveValue);
            
            // 높이 추가 (포물선 효과)
            float heightOffset = Mathf.Sin(normalizedTime * Mathf.PI) * flyHeight;
            currentPosition.y += heightOffset;
            
            hotteokObject.transform.position = currentPosition;
            
            // 회전 효과
            hotteokObject.transform.Rotate(0, 0, 180 * Time.deltaTime);
            
            yield return null;
        }
        
        // 최종 위치 설정
        hotteokObject.transform.position = endPosition;
        hotteokObject.transform.rotation = Quaternion.identity;
        
        // 스택에 따른 크기 조정 (위로 갈수록 작아짐)
        float scaleMultiplier = Mathf.Pow(stackScale, stackIndex);
        hotteokObject.transform.localScale = Vector3.one * scaleMultiplier;
        
        // 착지 효과
        ShowLandingEffects(endPosition);
        
        // 스택에 호떡 등록
        hotteokStacks[fillingType].Add(hotteokObject);
        
        // 스택 전용 스크립트 추가
        HotteokInStack stackScript = hotteokObject.GetComponent<HotteokInStack>();
        if (stackScript == null)
        {
            stackScript = hotteokObject.AddComponent<HotteokInStack>();
        }
        stackScript.Initialize(fillingType, this, stackIndex);
        
        // Collider 다시 활성화 (선택 가능하도록)
        if (collider != null)
        {
            collider.enabled = true;
        }
        
        Debug.Log(fillingType + " 호떡이 스택에 도착! 현재 높이: " + hotteokStacks[fillingType].Count);
    }
    
    /// <summary>
    /// 착지 효과 표시
    /// </summary>
    void ShowLandingEffects(Vector3 position)
    {
        if (hotteokLandSound != null)
        {
            AudioSource.PlayClipAtPoint(hotteokLandSound, position);
        }
        
        if (hotteokLandEffect != null)
        {
            GameObject effect = Instantiate(hotteokLandEffect, position, Quaternion.identity);
            Destroy(effect, 2f);
        }
    }
    
    /// <summary>
    /// 스택 가득함 피드백
    /// </summary>
    void ShowStackFullFeedback(PreparationUI.FillingType fillingType)
    {
        Debug.Log("⚠️ " + fillingType + " 스택이 가득참! 호떡을 손님에게 판매하세요!");
        
        if (stackFullWarningSound != null)
        {
            AudioSource.PlayClipAtPoint(stackFullWarningSound, transform.position);
        }
        
        // 스택 깜빡임 효과
        StartCoroutine(BlinkStackWarning(fillingType));
    }
    
    /// <summary>
    /// 스택 경고 깜빡임 효과
    /// </summary>
    IEnumerator BlinkStackWarning(PreparationUI.FillingType fillingType)
    {
        List<GameObject> stack = hotteokStacks[fillingType];
        
        for (int i = 0; i < 3; i++)
        {
            foreach (GameObject hotteok in stack)
            {
                if (hotteok != null)
                {
                    SpriteRenderer sr = hotteok.GetComponent<SpriteRenderer>();
                    if (sr != null) sr.color = Color.red;
                }
            }
            yield return new WaitForSeconds(0.2f);
            
            foreach (GameObject hotteok in stack)
            {
                if (hotteok != null)
                {
                    SpriteRenderer sr = hotteok.GetComponent<SpriteRenderer>();
                    if (sr != null) sr.color = Color.white;
                }
            }
            yield return new WaitForSeconds(0.2f);
        }
    }
    
    /// <summary>
    /// 🔧 호떡 선택 (탭했을 때) - 수정된 버전
    /// </summary>
    public void SelectHotteok(GameObject hotteokObject)
    {
        // 기존 선택 해제
        if (selectedHotteok != null)
        {
            DeselectHotteok();
        }
        
        selectedHotteok = hotteokObject;
        
        // 🔧 선택 표시: 색상 변경만 (위치 변경 금지)
        SpriteRenderer sr = selectedHotteok.GetComponent<SpriteRenderer>();
        if (sr != null)
        {
            sr.color = selectedColor;
        }
        
        // 🔧 선택 표시 UI 활성화 (위치는 호떡 위에 고정)
        if (selectionIndicator != null)
        {
            selectionIndicator.SetActive(true);
            Vector3 indicatorPosition = hotteokObject.transform.position + Vector3.up * 0.5f;
            selectionIndicator.transform.position = indicatorPosition;
            
            // 🔧 indicator의 위치만 변경하고 호떡 자체는 움직이지 않음
            Debug.Log($"🎯 선택 표시 위치: {indicatorPosition}");
        }
        
        HotteokInStack hotteokScript = hotteokObject.GetComponent<HotteokInStack>();
        if (hotteokScript != null)
        {
            Debug.Log($"✅ 호떡 선택됨: {hotteokScript.fillingType}");
        }
    }
    
    /// <summary>
    /// 🔧 호떡 선택 해제 - 수정된 버전
    /// </summary>
    public void DeselectHotteok()
    {
        if (selectedHotteok != null)
        {
            // 🔧 색상만 원래대로 복원 (위치는 건드리지 않음)
            SpriteRenderer sr = selectedHotteok.GetComponent<SpriteRenderer>();
            if (sr != null)
            {
                sr.color = Color.white;
            }
            
            Debug.Log($"🔄 호떡 선택 해제됨");
            selectedHotteok = null;
        }
        
        if (selectionIndicator != null)
        {
            selectionIndicator.SetActive(false);
        }
    }
    
    /// <summary>
    /// 🔧 선택된 호떡을 손님에게 전달 - 수정된 버전
    /// </summary>
    public bool DeliverSelectedHotteokToCustomer()
    {
        if (selectedHotteok == null)
        {
            Debug.Log("❌ 선택된 호떡이 없습니다!");
            return false;
        }
        
        HotteokInStack stackScript = selectedHotteok.GetComponent<HotteokInStack>();
        if (stackScript == null)
        {
            Debug.LogError("❌ HotteokInStack 스크립트를 찾을 수 없습니다!");
            return false;
        }
        
        PreparationUI.FillingType fillingType = stackScript.fillingType;
        
        Debug.Log($"📦 호떡 전달 시작: {fillingType}");
        
        // 🔧 스택에서 호떡 제거 (LIFO: 맨 위부터)
        List<GameObject> stack = hotteokStacks[fillingType];
        if (stack.Contains(selectedHotteok))
        {
            // 🔧 전달 애니메이션 시작 (선택적)
            StartCoroutine(DeliveryAnimation(selectedHotteok, fillingType));
            
            return true;
        }
        else
        {
            Debug.LogError($"❌ 선택된 호떡이 {fillingType} 스택에 없습니다!");
            return false;
        }
    }
    
    /// <summary>
    /// 🆕 호떡 전달 애니메이션 (스택에서 제거 포함)
    /// </summary>
    IEnumerator DeliveryAnimation(GameObject hotteokObject, PreparationUI.FillingType fillingType)
    {
        // 🔧 즉시 스택에서 제거 (게임 로직상)
        List<GameObject> stack = hotteokStacks[fillingType];
        stack.Remove(hotteokObject);
        
        Debug.Log($"✅ {fillingType} 호떡이 스택에서 제거됨! 남은 스택 높이: {stack.Count}");
        
        // 🔧 전달 중에는 클릭 불가능하게
        Collider2D collider = hotteokObject.GetComponent<Collider2D>();
        if (collider != null)
        {
            collider.enabled = false;
        }
        
        // 🔧 선택 해제
        DeselectHotteok();
        
        // 🔧 간단한 전달 애니메이션 (페이드 아웃)
        SpriteRenderer sr = hotteokObject.GetComponent<SpriteRenderer>();
        if (sr != null)
        {
            Color originalColor = sr.color;
            float duration = 0.5f;
            float elapsedTime = 0f;
            
            while (elapsedTime < duration)
            {
                elapsedTime += Time.deltaTime;
                float alpha = 1f - (elapsedTime / duration);
                sr.color = new Color(originalColor.r, originalColor.g, originalColor.b, alpha);
                yield return null;
            }
        }
        
        // 🔧 호떡 오브젝트 완전 제거
        Debug.Log($"🗑️ 호떡 오브젝트 제거: {hotteokObject.name}");
        Destroy(hotteokObject);
        
        // 🔧 남은 호떡들의 인덱스 업데이트 및 재정렬
        StartCoroutine(ReorganizeStack(fillingType));
    }
    
    /// <summary>
    /// 스택 재정렬 (호떡 제거 후) - 기존 메서드 개선
    /// </summary>
    IEnumerator ReorganizeStack(PreparationUI.FillingType fillingType)
    {
        List<GameObject> stack = hotteokStacks[fillingType];
        RectTransform baseSlot = stackSlotsByType[fillingType];
        
        if (stack.Count == 0)
        {
            Debug.Log($"📦 {fillingType} 스택이 비어있음 - 재정렬 불필요");
            yield break;
        }
        
        Debug.Log($"🔄 {fillingType} 스택 재정렬 시작 (남은 호떡: {stack.Count}개)");
        
        // 각 호떡을 새로운 위치로 이동
        for (int i = 0; i < stack.Count; i++)
        {
            GameObject hotteok = stack[i];
            if (hotteok == null) continue;
            
            Vector3 newPosition = GetStackPosition(baseSlot, i);
            Vector3 startPosition = hotteok.transform.position;
            
            // 크기도 재조정
            float newScale = Mathf.Pow(stackScale, i);
            Vector3 startScale = hotteok.transform.localScale;
            Vector3 targetScale = Vector3.one * newScale;
            
            // 부드러운 이동 애니메이션
            float elapsedTime = 0f;
            while (elapsedTime < stackDropDuration)
            {
                elapsedTime += Time.deltaTime;
                float t = elapsedTime / stackDropDuration;
                
                hotteok.transform.position = Vector3.Lerp(startPosition, newPosition, t);
                hotteok.transform.localScale = Vector3.Lerp(startScale, targetScale, t);
                yield return null;
            }
            
            hotteok.transform.position = newPosition;
            hotteok.transform.localScale = targetScale;
            
            // 스택 인덱스 업데이트
            HotteokInStack stackScript = hotteok.GetComponent<HotteokInStack>();
            if (stackScript != null)
            {
                stackScript.UpdateStackIndex(i);
            }
        }
        
        Debug.Log($"✅ {fillingType} 스택 재정렬 완료");
    }
    
    /// <summary>
    /// 🔧 현재 선택된 호떡 반환 (수정된 버전)
    /// </summary>
    public GameObject GetSelectedHotteok()
    {
        if (selectedHotteok != null)
        {
            // 🔧 선택된 호떡이 여전히 유효한지 확인
            HotteokInStack stackScript = selectedHotteok.GetComponent<HotteokInStack>();
            if (stackScript != null)
            {
                return selectedHotteok;
            }
            else
            {
                // 🔧 유효하지 않은 선택 해제
                Debug.LogWarning("⚠️ 선택된 호떡이 유효하지 않음 - 자동 해제");
                DeselectHotteok();
                return null;
            }
        }
        return null;
    }
    
    /// <summary>
    /// 현재 스택 상태 정보 반환 (디버깅용)
    /// </summary>
    [ContextMenu("Debug Stack Status")]
    public void LogStackStatus()
    {
        Debug.Log("=== StackSalesCounter Debug Info ===");
        
        foreach (var kvp in hotteokStacks)
        {
            PreparationUI.FillingType type = kvp.Key;
            List<GameObject> stack = kvp.Value;
            
            Debug.Log($"{type} 스택: {stack.Count}/{maxStackHeight}");
            
            for (int i = 0; i < stack.Count; i++)
            {
                if (stack[i] != null)
                {
                    Vector3 pos = stack[i].transform.position;
                    bool isSelected = (stack[i] == selectedHotteok);
                    Debug.Log($"  [{i}] {stack[i].name} at {pos} {(isSelected ? "(선택됨)" : "")}");
                }
                else
                {
                    Debug.Log($"  [{i}] NULL");
                }
            }
        }
        
        if (selectedHotteok != null)
        {
            Debug.Log($"현재 선택: {selectedHotteok.name}");
        }
        else
        {
            Debug.Log("현재 선택: 없음");
        }
    }
    
    /// <summary>
    /// 특정 타입의 호떡 개수 반환
    /// </summary>
    public int GetHotteokCount(PreparationUI.FillingType fillingType)
    {
        if (!hotteokStacks.ContainsKey(fillingType)) return 0;
        return hotteokStacks[fillingType].Count;
    }
}