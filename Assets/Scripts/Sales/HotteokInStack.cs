// Assets/Scripts/HotteokInStack.cs
// 🔧 호떡 위치 이동 문제 완전 해결 버전 (모든 애니메이션 차단)

using UnityEngine;
using System.Collections;

public class HotteokInStack : MonoBehaviour
{
    [Header("스택 호떡 상태")]
    public PreparationUI.FillingType fillingType;
    public int stackIndex; // 스택에서의 위치 (0이 가장 아래)
    
    [Header("시각적 피드백")]
    public Color normalColor = Color.white;
    public Color hoverColor = new Color(1f, 1f, 1f, 0.8f); // 마우스 오버 시
    public AudioClip selectSound;               // 선택 소리
    public GameObject clickEffect;              // 클릭 이펙트
    
    [Header("🚨 모든 효과 완전 차단")]
    public bool enableHoverEffects = false;     // 마우스 오버 효과 (강제 비활성화)
    public bool enableClickAnimations = false;  // 클릭 애니메이션 (강제 비활성화)
    public bool enableScaleEffects = false;     // 크기 변화 효과 (강제 비활성화)
    public bool enablePositionEffects = false;  // 위치 변경 효과 (강제 비활성화)
    public bool enablePositionDebug = false;    // 위치 변경 감지 (강제 비활성화)
    
    [Header("🔒 위치 완전 고정")]
    public bool LOCK_POSITION_COMPLETELY = true; // 위치 완전 고정
    
    private StackSalesCounter parentCounter;
    private SpriteRenderer spriteRenderer;
    private bool isSelected = false;
    private bool isHovering = false;
    
    // 🔒 위치 고정용
    private Vector3 lockedPosition;
    private bool positionLocked = false;
    
    void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        
        // 🚨 모든 애니메이션 효과 강제 비활성화
        ForceDisableAllEffects();
    }
    
    void Start()
    {
        // 🔒 현재 위치를 고정 위치로 설정
        LockCurrentPosition();
    }
    
    void Update()
    {
        // 🔒 위치 완전 고정 시스템
        if (LOCK_POSITION_COMPLETELY && positionLocked)
        {
            EnforceLockedPosition();
        }
    }
    
    /// <summary>
    /// 🚨 모든 애니메이션 효과 강제 비활성화
    /// </summary>
    void ForceDisableAllEffects()
    {
        enableHoverEffects = false;
        enableClickAnimations = false;
        enableScaleEffects = false;
        enablePositionEffects = false;
        enablePositionDebug = false;
        
        Debug.Log($"🚨 [{gameObject.name}] 모든 애니메이션 효과 강제 비활성화됨");
    }
    
    /// <summary>
    /// 🔒 현재 위치 고정
    /// </summary>
    void LockCurrentPosition()
    {
        if (LOCK_POSITION_COMPLETELY)
        {
            lockedPosition = transform.position;
            positionLocked = true;
            Debug.Log($"🔒 [{gameObject.name}] 위치 고정됨: {lockedPosition}");
        }
    }
    
    /// <summary>
    /// 🔒 고정된 위치 강제 적용
    /// </summary>
    void EnforceLockedPosition()
    {
        if (transform.position != lockedPosition)
        {
            Debug.LogWarning($"🚨 [{gameObject.name}] 위치 변경 감지 및 강제 복원: {transform.position} → {lockedPosition}");
            transform.position = lockedPosition;
        }
    }
    
    /// <summary>
    /// 스택 호떡으로 초기화
    /// </summary>
    public void Initialize(PreparationUI.FillingType type, StackSalesCounter counter, int index)
    {
        fillingType = type;
        parentCounter = counter;
        stackIndex = index;
        isSelected = false;
        
        // 🚨 모든 애니메이션 효과 강제 비활성화
        ForceDisableAllEffects();
        
        // 색상 초기화
        if (spriteRenderer != null)
        {
            spriteRenderer.color = normalColor;
        }
        
        // 🔒 위치 고정
        LockCurrentPosition();
        
        Debug.Log($"✅ {fillingType} 호떡이 스택 [{index}] 위치에서 안전하게 초기화됨");
    }
    
    /// <summary>
    /// 스택에서의 인덱스 업데이트 (위치는 변경하지 않음)
    /// </summary>
    public void UpdateStackIndex(int newIndex)
    {
        stackIndex = newIndex;
        Debug.Log($"📋 {fillingType} 호떡의 스택 인덱스가 {newIndex}로 업데이트됨 (위치는 고정 유지)");
    }
    
    /// <summary>
    /// 🔒 새로운 위치 설정 및 고정
    /// </summary>
    public void SetAndLockPosition(Vector3 newPosition)
    {
        transform.position = newPosition;
        lockedPosition = newPosition;
        positionLocked = true;
        Debug.Log($"🔒 [{gameObject.name}] 새로운 위치로 고정됨: {lockedPosition}");
    }
    
    void OnMouseDown()
    {
        // 🔒 위치 고정 확인
        Vector3 originalPosition = transform.position;
        
        // 🔧 호떡 선택만 수행 (자동 배달 방지)
        SelectThisHotteokOnly();
        
        // 🔧 클릭 소리만 (애니메이션 없음)
        if (selectSound != null)
        {
            AudioSource.PlayClipAtPoint(selectSound, transform.position);
        }
        
        // 🔧 간단한 이펙트만 (위치 변경 없음)
        if (clickEffect != null)
        {
            GameObject effect = Instantiate(clickEffect, transform.position, Quaternion.identity);
            Destroy(effect, 1f);
        }
        
        // 🔒 위치가 변경되었다면 강제로 복원
        if (transform.position != originalPosition)
        {
            Debug.LogWarning($"🚨 [{gameObject.name}] 클릭 중 위치 변경 감지! 강제 복원");
            transform.position = originalPosition;
            lockedPosition = originalPosition;
        }
    }
    
    void OnMouseEnter()
    {
        // 🚨 모든 마우스 오버 효과 완전 차단
        if (!enableHoverEffects) return;
        
        Vector3 originalPosition = transform.position;
        
        if (!isSelected && !isHovering)
        {
            isHovering = true;
            
            // 색상 변경만 (크기/위치 변경 절대 없음)
            if (spriteRenderer != null)
            {
                spriteRenderer.color = hoverColor;
            }
        }
        
        // 🔒 위치 변경 방지
        if (transform.position != originalPosition)
        {
            transform.position = originalPosition;
            lockedPosition = originalPosition;
        }
    }
    
    void OnMouseExit()
    {
        // 🚨 모든 마우스 오버 효과 완전 차단
        if (!enableHoverEffects) return;
        
        Vector3 originalPosition = transform.position;
        
        if (!isSelected && isHovering)
        {
            isHovering = false;
            
            // 원래 색상으로 복원 (크기/위치 변경 절대 없음)
            if (spriteRenderer != null)
            {
                spriteRenderer.color = normalColor;
            }
        }
        
        // 🔒 위치 변경 방지
        if (transform.position != originalPosition)
        {
            transform.position = originalPosition;
            lockedPosition = originalPosition;
        }
    }
    
    /// <summary>
    /// 🔧 이 호떡을 선택만 수행 (자동 배달 방지 강화)
    /// </summary>
    void SelectThisHotteokOnly()
    {
        Debug.Log($"🔵 HotteokInStack.SelectThisHotteokOnly() 호출됨: {fillingType}");
        
        if (parentCounter != null)
        {
            // 판매대에 현재 호떡을 선택하도록 요청 (선택만, 배달 안함)
            parentCounter.SelectHotteok(gameObject);
            isSelected = true;
            
            Debug.Log($"✅ {fillingType} 호떡이 선택만 됨! (배달 안됨)");
            Debug.Log($"👆 이제 손님을 직접 클릭해야 배달됩니다!");
            
            // 🔧 자동 배달 절대 방지 확인
            Debug.Log($"🚫 자동 배달 방지: 이 메서드는 선택만 하고 절대 배달하지 않습니다!");
        }
        else
        {
            Debug.LogError($"❌ {fillingType} 호떡: parentCounter가 null입니다!");
        }
    }
    
    /// <summary>
    /// 🔧 이 호떡을 선택 (위치 변경 절대 금지) - 레거시 호환용
    /// </summary>
    void SelectThisHotteok()
    {
        SelectThisHotteokOnly();
    }
    
    /// <summary>
    /// 선택 해제 (외부에서 호출)
    /// </summary>
    public void Deselect()
    {
        Vector3 originalPosition = transform.position;
        
        isSelected = false;
        
        if (spriteRenderer != null)
        {
            spriteRenderer.color = isHovering ? hoverColor : normalColor;
        }
        
        // 🔒 위치 변경 방지
        if (transform.position != originalPosition)
        {
            transform.position = originalPosition;
            lockedPosition = originalPosition;
        }
    }
    
    /// <summary>
    /// 현재 선택 상태 확인
    /// </summary>
    public bool IsSelected()
    {
        return isSelected;
    }
    
    /// <summary>
    /// 호떡이 스택의 맨 위에 있는지 확인
    /// </summary>
    public bool IsTopOfStack()
    {
        if (parentCounter == null) return false;
        
        int stackCount = parentCounter.GetHotteokCount(fillingType);
        return stackIndex == stackCount - 1; // 맨 위는 가장 높은 인덱스
    }
    
    /// <summary>
    /// 손님에게 전달되었을 때 호출
    /// </summary>
    public void OnDeliveredToCustomer()
    {
        Debug.Log($"📦 {fillingType} 호떡이 손님에게 전달됨!");
        
        // 🚨 전달 애니메이션 완전 차단 - 간단한 로그만
        Debug.Log($"🎉 {fillingType} 호떡 전달 성공!");
    }
    
    /// <summary>
    /// 🔧 디버그 정보 출력
    /// </summary>
    [ContextMenu("Debug Hotteok Info")]
    public void PrintDebugInfo()
    {
        Debug.Log("=== HotteokInStack Debug Info ===");
        Debug.Log($"호떡 타입: {fillingType}");
        Debug.Log($"스택 인덱스: {stackIndex}");
        Debug.Log($"선택 상태: {isSelected}");
        Debug.Log($"마우스 오버 상태: {isHovering}");
        Debug.Log($"현재 위치: {transform.position}");
        Debug.Log($"고정 위치: {lockedPosition}");
        Debug.Log($"위치 고정 상태: {positionLocked}");
        Debug.Log($"현재 크기: {transform.localScale}");
        Debug.Log($"현재 색상: {(spriteRenderer != null ? spriteRenderer.color.ToString() : "null")}");
        Debug.Log($"스택 맨 위 여부: {IsTopOfStack()}");
        
        Debug.Log("=== 효과 설정 상태 (모두 비활성화됨) ===");
        Debug.Log($"Hover Effects: {enableHoverEffects}");
        Debug.Log($"Click Animations: {enableClickAnimations}");
        Debug.Log($"Scale Effects: {enableScaleEffects}");
        Debug.Log($"Position Effects: {enablePositionEffects}");
        Debug.Log($"Position Debug: {enablePositionDebug}");
        Debug.Log($"🔒 위치 완전 고정: {LOCK_POSITION_COMPLETELY}");
    }
    
    /// <summary>
    /// 🔧 완전 안전 모드로 재설정
    /// </summary>
    [ContextMenu("Force Ultra Safe Mode")]
    public void ForceUltraSafeMode()
    {
        ForceDisableAllEffects();
        LOCK_POSITION_COMPLETELY = true;
        LockCurrentPosition();
        
        Debug.Log($"🔒 [{gameObject.name}] 완전 안전 모드 강제 활성화!");
    }
}