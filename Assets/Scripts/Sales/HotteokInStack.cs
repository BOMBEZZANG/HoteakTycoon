// Assets/Scripts/HotteokInStack.cs
// 스택에 있는 호떡의 클릭 선택 동작을 관리하는 스크립트

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
    
    private StackSalesCounter parentCounter;
    private SpriteRenderer spriteRenderer;
    private bool isSelected = false;
    private bool isHovering = false;
    
    void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
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
        
        // 색상 초기화
        if (spriteRenderer != null)
        {
            spriteRenderer.color = normalColor;
        }
        
        Debug.Log(fillingType + " 호떡이 스택 [" + index + "] 위치에서 초기화됨");
    }
    
    /// <summary>
    /// 스택에서의 인덱스 업데이트 (다른 호떡이 제거되었을 때)
    /// </summary>
    public void UpdateStackIndex(int newIndex)
    {
        stackIndex = newIndex;
        Debug.Log(fillingType + " 호떡의 스택 인덱스가 " + newIndex + "로 업데이트됨");
    }
    
    void OnMouseDown()
    {
        // 호떡 선택
        SelectThisHotteok();
        
        // 클릭 이펙트
        ShowClickEffect();
        
        // 선택 소리
        if (selectSound != null)
        {
            AudioSource.PlayClipAtPoint(selectSound, transform.position);
        }
    }
    
    void OnMouseEnter()
    {
        if (!isSelected && !isHovering)
        {
            isHovering = true;
            
            // 마우스 오버 시 색상 변경
            if (spriteRenderer != null)
            {
                spriteRenderer.color = hoverColor;
            }
            
            // 살짝 크게 만들기
            StartCoroutine(ScaleHoverEffect(1.05f));
        }
    }
    
    void OnMouseExit()
    {
        if (!isSelected && isHovering)
        {
            isHovering = false;
            
            // 원래 색상으로 복원
            if (spriteRenderer != null)
            {
                spriteRenderer.color = normalColor;
            }
            
            // 원래 크기로 복원
            StartCoroutine(ScaleHoverEffect(1.0f));
        }
    }
    
    /// <summary>
    /// 이 호떡을 선택
    /// </summary>
    void SelectThisHotteok()
    {
        if (parentCounter != null)
        {
            // 판매대에 현재 호떡을 선택하도록 요청
            parentCounter.SelectHotteok(gameObject);
            isSelected = true;
            
            Debug.Log(fillingType + " 호떡이 선택됨! 손님을 클릭하여 전달하세요.");
        }
    }
    
    /// <summary>
    /// 선택 해제 (외부에서 호출)
    /// </summary>
    public void Deselect()
    {
        isSelected = false;
        
        if (spriteRenderer != null)
        {
            spriteRenderer.color = isHovering ? hoverColor : normalColor;
        }
    }
    
    /// <summary>
    /// 클릭 이펙트 표시
    /// </summary>
    void ShowClickEffect()
    {
        if (clickEffect != null)
        {
            GameObject effect = Instantiate(clickEffect, transform.position, Quaternion.identity);
            Destroy(effect, 1f);
        }
        
        // 간단한 펄스 애니메이션
        StartCoroutine(ClickPulseAnimation());
    }
    
    /// <summary>
    /// 클릭 시 펄스 애니메이션
    /// </summary>
    IEnumerator ClickPulseAnimation()
    {
        Vector3 originalScale = transform.localScale;
        Vector3 bigScale = originalScale * 1.2f;
        
        // 크게 되었다가 작아지는 애니메이션
        float duration = 0.15f;
        float elapsedTime = 0f;
        
        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            float t = elapsedTime / duration;
            
            float scaleMultiplier = 1 + 0.2f * Mathf.Sin(t * Mathf.PI);
            transform.localScale = originalScale * scaleMultiplier;
            
            yield return null;
        }
        
        transform.localScale = originalScale;
    }
    
    /// <summary>
    /// 마우스 오버 시 크기 변화 애니메이션
    /// </summary>
    IEnumerator ScaleHoverEffect(float targetScale)
    {
        Vector3 startScale = transform.localScale;
        
        // 현재 스택 위치에 맞는 기본 크기 계산
        float baseScale = Mathf.Pow(parentCounter.stackScale, stackIndex);
        Vector3 baseScaleVector = Vector3.one * baseScale;
        Vector3 targetScaleVector = baseScaleVector * targetScale;
        
        float duration = 0.1f;
        float elapsedTime = 0f;
        
        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            float t = elapsedTime / duration;
            
            transform.localScale = Vector3.Lerp(startScale, targetScaleVector, t);
            yield return null;
        }
        
        transform.localScale = targetScaleVector;
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
    /// 손님에게 전달되었을 때 호출 (나중에 Customer 구현 시 사용)
    /// </summary>
    public void OnDeliveredToCustomer()
    {
        Debug.Log(fillingType + " 호떡이 손님에게 전달됨!");
        
        // 전달 성공 이펙트
        ShowDeliveryEffect();
        
        // 오브젝트는 StackSalesCounter에서 제거됨
    }
    
    /// <summary>
    /// 전달 성공 이펙트
    /// </summary>
    void ShowDeliveryEffect()
    {
        Debug.Log("🎉 " + fillingType + " 호떡 전달 성공!");
        
        // 성공 애니메이션
        StartCoroutine(DeliverySuccessAnimation());
    }
    
    /// <summary>
    /// 전달 성공 애니메이션
    /// </summary>
    IEnumerator DeliverySuccessAnimation()
    {
        Vector3 originalScale = transform.localScale;
        Vector3 originalPosition = transform.position;
        
        float duration = 0.5f;
        float elapsedTime = 0f;
        
        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            float t = elapsedTime / duration;
            
            // 점점 크게 되면서 위로 올라가고 투명해짐
            float scaleMultiplier = 1 + t * 2;
            transform.localScale = originalScale * scaleMultiplier;
            transform.position = originalPosition + Vector3.up * t * 2;
            
            if (spriteRenderer != null)
            {
                Color color = spriteRenderer.color;
                color.a = 1 - t;
                spriteRenderer.color = color;
            }
            
            yield return null;
        }
    }
}