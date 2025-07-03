// Assets/Scripts/StackSalesCounter.cs
// 🔧 호떡 위치 이동 문제 완전 해결 버전

using UnityEngine;
using System.Collections.Generic;
using System.Collections;

public class StackSalesCounter : MonoBehaviour
{
    [Header("판매대 설정")]
    public int maxStackHeight = 3;

    [Header("UI 판매대 슬롯 설정")]
    public RectTransform sugarStackSlot;
    public RectTransform seedStackSlot;

    [Header("스택 설정")]
    public float stackOffset = 0.3f;
    public float stackScale = 0.9f;

    [Header("애니메이션 설정")]
    public float flyDuration = 1.0f;
    public AnimationCurve flyCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
    public float flyHeight = 3.0f;
    public float stackDropDuration = 0.3f;

    [Header("UI 피드백")]
    public AudioClip hotteokLandSound;
    public GameObject hotteokLandEffect;
    public AudioClip stackFullWarningSound;

    [Header("🚨 호떡 위치 완전 고정 시스템")]
    public Color selectedColor = Color.yellow;
    public bool COMPLETELY_DISABLE_POSITION_CHANGES = true;  // 🚨 위치 변경 완전 차단
    public bool enableDebugLogs = true;

    // 내부 상태
    private GameObject selectedHotteok = null;
    private Dictionary<PreparationUI.FillingType, List<GameObject>> hotteokStacks;
    private Dictionary<PreparationUI.FillingType, RectTransform> stackSlotsByType;

    // 싱글톤
    public static StackSalesCounter Instance { get; private set; }

    void Awake()
    {
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
        hotteokStacks = new Dictionary<PreparationUI.FillingType, List<GameObject>>
        {
            { PreparationUI.FillingType.Sugar, new List<GameObject>() },
            { PreparationUI.FillingType.Seed, new List<GameObject>() }
        };

        stackSlotsByType = new Dictionary<PreparationUI.FillingType, RectTransform>
        {
            { PreparationUI.FillingType.Sugar, sugarStackSlot },
            { PreparationUI.FillingType.Seed, seedStackSlot }
        };

        ValidateSlots();
        DebugLog("스택 판매대 초기화 완료!");
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
            DebugLog("✅ 모든 스택 슬롯이 올바르게 연결됨!");
        }
    }

    public bool CanAddHotteokToStack(PreparationUI.FillingType fillingType)
    {
        if (!hotteokStacks.ContainsKey(fillingType))
        {
            Debug.LogError("지원하지 않는 호떡 타입: " + fillingType);
            return false;
        }

        int currentCount = hotteokStacks[fillingType].Count;
        bool canAdd = currentCount < maxStackHeight;

        DebugLog(fillingType + " 스택 추가 가능 여부: " + canAdd + " (현재: " + currentCount + "/" + maxStackHeight + ")");
        return canAdd;
    }

    public void AddHotteokToStack(GameObject hotteokObject, PreparationUI.FillingType fillingType)
    {
        DebugLog($"🔵 AddHotteokToStack 호출됨: {hotteokObject.name}");

        if (!CanAddHotteokToStack(fillingType))
        {
            DebugLog("스택이 가득 참! " + fillingType + " 호떡을 더 이상 추가할 수 없습니다.");
            ShowStackFullFeedback(fillingType);
            return;
        }

        RectTransform targetSlot = stackSlotsByType[fillingType];
        if (targetSlot == null)
        {
            Debug.LogError("사용 가능한 슬롯을 찾을 수 없습니다!");
            return;
        }

        DebugLog(fillingType + " 호떡을 스택으로 이동 시작!");
        StartCoroutine(FlyHotteokToStack(hotteokObject, targetSlot, fillingType));
    }

    Vector3 GetStackPosition(RectTransform baseSlot, int stackIndex)
    {
        Vector3 basePosition = GetWorldPositionFromUISlot(baseSlot);
        Vector3 stackPosition = basePosition + Vector3.up * (stackOffset * stackIndex);
        return stackPosition;
    }

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

    IEnumerator FlyHotteokToStack(GameObject hotteokObject, RectTransform targetSlot, PreparationUI.FillingType fillingType)
    {
        Vector3 startPosition = hotteokObject.transform.position;
        int stackIndex = hotteokStacks[fillingType].Count;
        Vector3 endPosition = GetStackPosition(targetSlot, stackIndex);

        DebugLog("스택 이동: " + startPosition + " → " + endPosition + " (스택 높이: " + stackIndex + ")");

        float elapsedTime = 0f;

        // 🚨 HotteokOnGriddle 스크립트 완전 제거 (비활성화가 아닌 제거)
        HotteokOnGriddle hotteokScript = hotteokObject.GetComponent<HotteokOnGriddle>();
        if (hotteokScript != null)
        {
            DebugLog("🗑️ HotteokOnGriddle 스크립트 완전 제거");
            DestroyImmediate(hotteokScript);
        }

        // Collider 비활성화
        Collider2D collider = hotteokObject.GetComponent<Collider2D>();
        if (collider != null)
        {
            collider.enabled = false;
        }

        // 날아가는 애니메이션
        while (elapsedTime < flyDuration)
        {
            elapsedTime += Time.deltaTime;
            float normalizedTime = elapsedTime / flyDuration;
            float curveValue = flyCurve.Evaluate(normalizedTime);

            Vector3 currentPosition = Vector3.Lerp(startPosition, endPosition, curveValue);
            float heightOffset = Mathf.Sin(normalizedTime * Mathf.PI) * flyHeight;
            currentPosition.y += heightOffset;

            hotteokObject.transform.position = currentPosition;
            hotteokObject.transform.Rotate(0, 0, 180 * Time.deltaTime);

            yield return null;
        }

        // 최종 위치 및 크기 설정
        hotteokObject.transform.position = endPosition;
        hotteokObject.transform.rotation = Quaternion.identity;

        float scaleMultiplier = Mathf.Pow(stackScale, stackIndex);
        hotteokObject.transform.localScale = Vector3.one * scaleMultiplier;

        ShowLandingEffects(endPosition);
        CompleteHotteokPlacement(hotteokObject, fillingType, stackIndex);
    }

    void CompleteHotteokPlacement(GameObject hotteokObject, PreparationUI.FillingType fillingType, int stackIndex)
    {
        // 스택에 호떡 등록
        hotteokStacks[fillingType].Add(hotteokObject);

        // 🚨 HotteokInStack 스크립트 추가 및 안전 설정
        HotteokInStack stackScript = hotteokObject.GetComponent<HotteokInStack>();
        if (stackScript == null)
        {
            stackScript = hotteokObject.AddComponent<HotteokInStack>();
        }

        // 🚨 모든 애니메이션 효과 강제 비활성화
        stackScript.enableHoverEffects = false;
        stackScript.enableClickAnimations = false;
        stackScript.enableScaleEffects = false;
        stackScript.enablePositionEffects = false;
        stackScript.enablePositionDebug = false;

        stackScript.Initialize(fillingType, this, stackIndex);

        // Collider 다시 활성화
        Collider2D collider = hotteokObject.GetComponent<Collider2D>();
        if (collider != null)
        {
            collider.enabled = true;
        }

        DebugLog($"✅ {fillingType} 호떡이 스택에 안전하게 배치됨! 현재 높이: {hotteokStacks[fillingType].Count}");
    }

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

    void ShowStackFullFeedback(PreparationUI.FillingType fillingType)
    {
        DebugLog("⚠️ " + fillingType + " 스택이 가득참! 호떡을 손님에게 판매하세요!");

        if (stackFullWarningSound != null)
        {
            AudioSource.PlayClipAtPoint(stackFullWarningSound, transform.position);
        }

        StartCoroutine(BlinkStackWarning(fillingType));
    }

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

    // ========== 🚨 완전히 안전한 선택/해제 시스템 ==========

    /// <summary>
    /// 🚨 호떡 선택 (위치 변경 절대 금지) - 자동 배달 방지 강화
    /// </summary>
    public void SelectHotteok(GameObject hotteokToSelect)
    {
        DebugLog($"🟡 호떡 선택 요청: {hotteokToSelect?.name}");

        if (hotteokToSelect == null) return;

        // 🚨 위치 변경 완전 차단
        if (COMPLETELY_DISABLE_POSITION_CHANGES)
        {
            Vector3 originalPosition = hotteokToSelect.transform.position;

            // 이전 선택 해제
            if (selectedHotteok != null && selectedHotteok != hotteokToSelect)
            {
                DeselectHotteok();
            }

            // 새로운 호떡 선택
            selectedHotteok = hotteokToSelect;

            // 색상만 변경
            SpriteRenderer sr = selectedHotteok.GetComponent<SpriteRenderer>();
            if (sr != null)
            {
                sr.color = selectedColor;
            }

            // 🚨 위치가 변경되었다면 강제로 복원
            if (hotteokToSelect.transform.position != originalPosition)
            {
                Debug.LogWarning("🚨 선택 중 위치 변경 감지! 강제 복원");
                hotteokToSelect.transform.position = originalPosition;
            }

            // 🔧 선택만 완료, 배달은 절대 자동으로 하지 않음
            DebugLog($"✅ 호떡 선택 완료: {selectedHotteok.name} (선택만 됨, 배달 안됨)");
            DebugLog($"👆 이제 손님을 직접 클릭해야 배달됩니다!");
        }
    }

    /// <summary>
    /// 🚨 호떡 선택 해제 (위치 변경 절대 금지)
    /// </summary>
    public void DeselectHotteok()
    {
        if (selectedHotteok == null) return;

        Vector3 originalPosition = selectedHotteok.transform.position;

        // 색상 복원
        SpriteRenderer sr = selectedHotteok.GetComponent<SpriteRenderer>();
        if (sr != null)
        {
            HotteokInStack stackScript = selectedHotteok.GetComponent<HotteokInStack>();
            sr.color = (stackScript != null) ? stackScript.normalColor : Color.white;
        }

        // HotteokInStack 스크립트에 알림
        HotteokInStack deselectedScript = selectedHotteok.GetComponent<HotteokInStack>();
        if (deselectedScript != null)
        {
            deselectedScript.Deselect();
        }

        // 🚨 위치가 변경되었다면 강제로 복원
        if (selectedHotteok.transform.position != originalPosition)
        {
            Debug.LogWarning("🚨 선택 해제 중 위치 변경 감지! 강제 복원");
            selectedHotteok.transform.position = originalPosition;
        }

        DebugLog($"✅ 호떡 선택 해제 완료: {selectedHotteok.name}");
        selectedHotteok = null;
    }

    public bool DeliverSelectedHotteokToCustomer()
    {
        // 🚨 호출 추적 로그 추가
        Debug.Log("🚨 DeliverSelectedHotteokToCustomer() 호출됨!");
        Debug.Log($"🚨 호출 스택: {System.Environment.StackTrace}");
        
        if (selectedHotteok == null)
        {
            DebugLog("❌ 선택된 호떡이 없습니다!");
            return false;
        }

        HotteokInStack stackScript = selectedHotteok.GetComponent<HotteokInStack>();
        if (stackScript == null)
        {
            Debug.LogError("❌ HotteokInStack 스크립트를 찾을 수 없습니다!");
            return false;
        }

        PreparationUI.FillingType fillingType = stackScript.fillingType;
        DebugLog($"📦 호떡 전달 시작: {fillingType}");

        List<GameObject> stack = hotteokStacks[fillingType];
        if (stack.Contains(selectedHotteok))
        {
            StartCoroutine(SafeDeliveryAnimation(selectedHotteok, fillingType));
            return true;
        }
        else
        {
            Debug.LogError($"❌ 선택된 호떡이 {fillingType} 스택에 없습니다!");
            return false;
        }
    }

    IEnumerator SafeDeliveryAnimation(GameObject hotteokObject, PreparationUI.FillingType fillingType)
    {
        if (hotteokObject == null) yield break;

        // 즉시 스택에서 제거
        List<GameObject> stack = hotteokStacks[fillingType];
        stack.Remove(hotteokObject);

        DebugLog($"✅ {fillingType} 호떡이 스택에서 제거됨! 남은 스택 높이: {stack.Count}");

        // 클릭 불가능하게
        Collider2D collider = hotteokObject.GetComponent<Collider2D>();
        if (collider != null)
        {
            collider.enabled = false;
        }

        // 선택 해제
        DeselectHotteok();

        // 간단한 페이드 아웃만
        yield return StartCoroutine(SimpleDeliveryFadeOut(hotteokObject));

        // 호떡 오브젝트 제거
        if (hotteokObject != null)
        {
            DebugLog($"🗑️ 호떡 오브젝트 제거: {hotteokObject.name}");
            Destroy(hotteokObject);
        }
    }

    IEnumerator SimpleDeliveryFadeOut(GameObject hotteokObject)
    {
        if (hotteokObject == null) yield break;

        SpriteRenderer sr = hotteokObject.GetComponent<SpriteRenderer>();
        if (sr == null) yield break;

        Color originalColor = sr.color;
        float duration = 0.3f;
        float elapsedTime = 0f;

        while (elapsedTime < duration && hotteokObject != null)
        {
            elapsedTime += Time.deltaTime;
            float t = elapsedTime / duration;

            Color color = originalColor;
            color.a = 1 - t;
            sr.color = color;

            yield return null;
        }
    }

    public GameObject GetSelectedHotteok()
    {
        if (selectedHotteok != null && selectedHotteok.activeInHierarchy)
        {
            return selectedHotteok;
        }

        if (selectedHotteok != null)
        {
            Debug.LogWarning("⚠️ 선택된 호떡이 유효하지 않음 - 자동 해제");
            DeselectHotteok();
        }

        return null;
    }

    public int GetHotteokCount(PreparationUI.FillingType fillingType)
    {
        if (!hotteokStacks.ContainsKey(fillingType)) return 0;
        return hotteokStacks[fillingType].Count;
    }

    void DebugLog(string message)
    {
        if (enableDebugLogs)
        {
            Debug.Log($"[StackSalesCounter] {message}");
        }
    }

    [ContextMenu("Debug Stack Status")]
    public void LogStackStatus()
    {
        Debug.Log("=== StackSalesCounter Debug Info ===");
        Debug.Log($"🚨 위치 변경 완전 차단: {COMPLETELY_DISABLE_POSITION_CHANGES}");

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
}