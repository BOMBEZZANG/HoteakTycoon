// Assets/Scripts/Customer/CustomerAnimator.cs
// 손님의 스프라이트 애니메이션과 표정 변화를 관리하는 클래스 (StackOverflow 수정)

using UnityEngine;
using System.Collections;

public class CustomerAnimator : MonoBehaviour
{
    [Header("표정 스프라이트")]
    public Sprite neutralSprite;        // 평상시 😐
    public Sprite happySprite;          // 기대/주문 😊
    public Sprite waitingSprite;        // 대기 중 😌
    public Sprite worriedSprite;        // 걱정/경고 😟
    public Sprite angrySprite;          // 화남 😠
    public Sprite satisfiedSprite;      // 만족 😄
    public Sprite confusedSprite;       // 혼란 😕
    
    [Header("애니메이션 설정")]
    public float expressionChangeSpeed = 0.5f;     // 표정 변화 속도
    public float bounceDuration = 0.3f;            // 바운스 지속시간
    public float bounceHeight = 0.2f;              // 바운스 높이
    public float shakeDuration = 0.5f;             // 흔들기 지속시간
    public float shakeIntensity = 0.1f;            // 흔들기 강도
    public float nodDuration = 0.6f;               // 고개 끄덕이기 시간
    public float nodAngle = 15f;                   // 고개 끄덕이기 각도
    
    [Header("걷기 애니메이션")]
    public float walkBobSpeed = 5f;                // 걸을 때 상하 움직임 속도
    public float walkBobAmount = 0.05f;            // 걸을 때 상하 움직임 크기
    
    [Header("🐛 디버그")]
    public bool enableAnimations = true;           // 애니메이션 활성화 여부
    
    // 컴포넌트
    private SpriteRenderer spriteRenderer;
    private Vector3 originalPosition;
    private Vector3 originalScale;
    private Quaternion originalRotation;
    
    // 상태
    private bool isWalking = false;
    private Coroutine currentAnimation;
    private Coroutine walkAnimation;
    private Coroutine idleAnimation;               // 🔧 아이들 애니메이션 코루틴 분리
    
    void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        
        // 초기 값 저장
        originalPosition = transform.localPosition;
        originalScale = transform.localScale;
        originalRotation = transform.localRotation;
    }
    
    void Start()
    {
        // 기본 표정 설정
        if (enableAnimations)
        {
            ChangeExpression(neutralSprite);
        }
    }
    
    /// <summary>
    /// 표정 변경
    /// </summary>
    public void ChangeExpression(Sprite newExpression, bool smooth = true)
    {
        if (!enableAnimations || spriteRenderer == null || newExpression == null) return;
        
        if (smooth)
        {
            StartCoroutine(SmoothExpressionChange(newExpression));
        }
        else
        {
            spriteRenderer.sprite = newExpression;
        }
    }
    
    /// <summary>
    /// 부드러운 표정 변화
    /// </summary>
    IEnumerator SmoothExpressionChange(Sprite newExpression)
    {
        if (!enableAnimations) yield break;
        
        // 페이드 아웃
        Color originalColor = spriteRenderer.color;
        float elapsedTime = 0f;
        
        while (elapsedTime < expressionChangeSpeed * 0.5f)
        {
            elapsedTime += Time.deltaTime;
            float alpha = 1f - (elapsedTime / (expressionChangeSpeed * 0.5f));
            spriteRenderer.color = new Color(originalColor.r, originalColor.g, originalColor.b, alpha);
            yield return null;
        }
        
        // 스프라이트 교체
        spriteRenderer.sprite = newExpression;
        
        // 페이드 인
        elapsedTime = 0f;
        while (elapsedTime < expressionChangeSpeed * 0.5f)
        {
            elapsedTime += Time.deltaTime;
            float alpha = elapsedTime / (expressionChangeSpeed * 0.5f);
            spriteRenderer.color = new Color(originalColor.r, originalColor.g, originalColor.b, alpha);
            yield return null;
        }
        
        spriteRenderer.color = originalColor;
    }
    
    /// <summary>
    /// 주문하기 애니메이션
    /// </summary>
    public void PlayOrderingAnimation()
    {
        if (!enableAnimations) return;
        
        ChangeExpression(happySprite);
        PlayBounceAnimation();
    }
    
    /// <summary>
    /// 대기 애니메이션
    /// </summary>
    public void PlayWaitingAnimation()
    {
        if (!enableAnimations) return;
        
        ChangeExpression(waitingSprite);
        StartIdleAnimation();
    }
    
    /// <summary>
    /// 경고 애니메이션
    /// </summary>
    public void PlayWarningAnimation()
    {
        if (!enableAnimations) return;
        
        ChangeExpression(worriedSprite);
        PlayShakeAnimation();
    }
    
    /// <summary>
    /// 만족 애니메이션
    /// </summary>
    public void PlaySatisfiedAnimation()
    {
        if (!enableAnimations) return;
        
        ChangeExpression(satisfiedSprite);
        PlayNodAnimation();
    }
    
    /// <summary>
    /// 화남 애니메이션
    /// </summary>
    public void PlayAngryAnimation()
    {
        if (!enableAnimations) return;
        
        ChangeExpression(angrySprite);
        PlayAngryShakeAnimation();
    }
    
    /// <summary>
    /// 거부 애니메이션
    /// </summary>
    public void PlayRejectAnimation()
    {
        if (!enableAnimations) return;
        
        ChangeExpression(angrySprite);
        PlayShakeAnimation();
    }
    
    /// <summary>
    /// 혼란 애니메이션
    /// </summary>
    public void PlayConfusedAnimation()
    {
        if (!enableAnimations) return;
        
        ChangeExpression(confusedSprite);
        PlayTiltAnimation();
    }
    
    /// <summary>
    /// 걷기 시작
    /// </summary>
    public void StartWalking()
    {
        if (!enableAnimations || isWalking) return;
        
        isWalking = true;
        if (walkAnimation != null)
        {
            StopCoroutine(walkAnimation);
        }
        walkAnimation = StartCoroutine(WalkingAnimation());
    }
    
    /// <summary>
    /// 걷기 중지
    /// </summary>
    public void StopWalking()
    {
        isWalking = false;
        if (walkAnimation != null)
        {
            StopCoroutine(walkAnimation);
            walkAnimation = null;
        }
        
        // 원래 위치로 복귀
        transform.localPosition = originalPosition;
    }
    
    /// <summary>
    /// 바운스 애니메이션
    /// </summary>
    void PlayBounceAnimation()
    {
        if (!enableAnimations) return;
        
        if (currentAnimation != null)
        {
            StopCoroutine(currentAnimation);
        }
        currentAnimation = StartCoroutine(BounceAnimation());
    }
    
    /// <summary>
    /// 흔들기 애니메이션
    /// </summary>
    void PlayShakeAnimation()
    {
        if (!enableAnimations) return;
        
        if (currentAnimation != null)
        {
            StopCoroutine(currentAnimation);
        }
        currentAnimation = StartCoroutine(ShakeAnimation());
    }
    
    /// <summary>
    /// 화남 흔들기 애니메이션 (더 강함)
    /// </summary>
    void PlayAngryShakeAnimation()
    {
        if (!enableAnimations) return;
        
        if (currentAnimation != null)
        {
            StopCoroutine(currentAnimation);
        }
        currentAnimation = StartCoroutine(AngryShakeAnimation());
    }
    
    /// <summary>
    /// 고개 끄덕이기 애니메이션
    /// </summary>
    void PlayNodAnimation()
    {
        if (!enableAnimations) return;
        
        if (currentAnimation != null)
        {
            StopCoroutine(currentAnimation);
        }
        currentAnimation = StartCoroutine(NodAnimation());
    }
    
    /// <summary>
    /// 기울이기 애니메이션 (혼란 시)
    /// </summary>
    void PlayTiltAnimation()
    {
        if (!enableAnimations) return;
        
        if (currentAnimation != null)
        {
            StopCoroutine(currentAnimation);
        }
        currentAnimation = StartCoroutine(TiltAnimation());
    }
    
    /// <summary>
    /// 🔧 대기 중 아이들 애니메이션 (수정됨)
    /// </summary>
    void StartIdleAnimation()
    {
        if (!enableAnimations) return;
        
        // 기존 아이들 애니메이션 중지
        if (idleAnimation != null)
        {
            StopCoroutine(idleAnimation);
        }
        
        // 새로운 아이들 애니메이션 시작
        idleAnimation = StartCoroutine(IdleAnimation());
    }
    
    /// <summary>
    /// 걷기 애니메이션 구현
    /// </summary>
    IEnumerator WalkingAnimation()
    {
        while (isWalking && enableAnimations)
        {
            float bobOffset = Mathf.Sin(Time.time * walkBobSpeed) * walkBobAmount;
            transform.localPosition = originalPosition + Vector3.up * bobOffset;
            yield return null;
        }
    }
    
    /// <summary>
    /// 바운스 애니메이션 구현
    /// </summary>
    IEnumerator BounceAnimation()
    {
        Vector3 startPos = transform.localPosition;
        float elapsedTime = 0f;
        
        while (elapsedTime < bounceDuration && enableAnimations)
        {
            elapsedTime += Time.deltaTime;
            float t = elapsedTime / bounceDuration;
            
            // 포물선 모양의 바운스
            float bounceValue = Mathf.Sin(t * Mathf.PI) * bounceHeight;
            transform.localPosition = startPos + Vector3.up * bounceValue;
            
            yield return null;
        }
        
        transform.localPosition = startPos;
        currentAnimation = null;
    }
    
    /// <summary>
    /// 흔들기 애니메이션 구현
    /// </summary>
    IEnumerator ShakeAnimation()
    {
        Vector3 startPos = transform.localPosition;
        float elapsedTime = 0f;
        
        while (elapsedTime < shakeDuration && enableAnimations)
        {
            elapsedTime += Time.deltaTime;
            float t = elapsedTime / shakeDuration;
            
            // 좌우로 흔들기
            float shakeValue = Mathf.Sin(t * 20f) * shakeIntensity * (1 - t);
            transform.localPosition = startPos + Vector3.right * shakeValue;
            
            yield return null;
        }
        
        transform.localPosition = startPos;
        currentAnimation = null;
    }
    
    /// <summary>
    /// 화남 흔들기 애니메이션 구현
    /// </summary>
    IEnumerator AngryShakeAnimation()
    {
        Vector3 startPos = transform.localPosition;
        float elapsedTime = 0f;
        float angryShakeDuration = shakeDuration * 1.5f;
        float angryShakeIntensity = shakeIntensity * 2f;
        
        while (elapsedTime < angryShakeDuration && enableAnimations)
        {
            elapsedTime += Time.deltaTime;
            float t = elapsedTime / angryShakeDuration;
            
            // 더 강하고 불규칙한 흔들기
            float shakeX = (Random.Range(-1f, 1f)) * angryShakeIntensity * (1 - t);
            float shakeY = (Random.Range(-1f, 1f)) * angryShakeIntensity * 0.5f * (1 - t);
            transform.localPosition = startPos + new Vector3(shakeX, shakeY, 0);
            
            yield return null;
        }
        
        transform.localPosition = startPos;
        currentAnimation = null;
    }
    
    /// <summary>
    /// 고개 끄덕이기 애니메이션 구현
    /// </summary>
    IEnumerator NodAnimation()
    {
        Quaternion startRotation = transform.localRotation;
        float elapsedTime = 0f;
        
        // 2번 끄덕이기
        for (int i = 0; i < 2 && enableAnimations; i++)
        {
            // 아래로
            elapsedTime = 0f;
            while (elapsedTime < nodDuration * 0.25f && enableAnimations)
            {
                elapsedTime += Time.deltaTime;
                float t = elapsedTime / (nodDuration * 0.25f);
                float angle = Mathf.Lerp(0, nodAngle, t);
                transform.localRotation = startRotation * Quaternion.Euler(angle, 0, 0);
                yield return null;
            }
            
            // 위로
            elapsedTime = 0f;
            while (elapsedTime < nodDuration * 0.25f && enableAnimations)
            {
                elapsedTime += Time.deltaTime;
                float t = elapsedTime / (nodDuration * 0.25f);
                float angle = Mathf.Lerp(nodAngle, 0, t);
                transform.localRotation = startRotation * Quaternion.Euler(angle, 0, 0);
                yield return null;
            }
        }
        
        transform.localRotation = startRotation;
        currentAnimation = null;
    }
    
    /// <summary>
    /// 기울이기 애니메이션 구현 (혼란)
    /// </summary>
    IEnumerator TiltAnimation()
    {
        Quaternion startRotation = transform.localRotation;
        float tiltAngle = 20f;
        float tiltDuration = 0.8f;
        float elapsedTime = 0f;
        
        // 오른쪽으로 기울이기
        while (elapsedTime < tiltDuration * 0.5f && enableAnimations)
        {
            elapsedTime += Time.deltaTime;
            float t = elapsedTime / (tiltDuration * 0.5f);
            float angle = Mathf.Lerp(0, tiltAngle, t);
            transform.localRotation = startRotation * Quaternion.Euler(0, 0, -angle);
            yield return null;
        }
        
        // 원래대로
        elapsedTime = 0f;
        while (elapsedTime < tiltDuration * 0.5f && enableAnimations)
        {
            elapsedTime += Time.deltaTime;
            float t = elapsedTime / (tiltDuration * 0.5f);
            float angle = Mathf.Lerp(tiltAngle, 0, t);
            transform.localRotation = startRotation * Quaternion.Euler(0, 0, -angle);
            yield return null;
        }
        
        transform.localRotation = startRotation;
        currentAnimation = null;
    }
    
    /// <summary>
    /// 🔧 대기 중 아이들 애니메이션 구현 (수정됨 - StackOverflow 방지)
    /// </summary>
    IEnumerator IdleAnimation()
    {
        Vector3 startPos = transform.localPosition;
        float idleSpeed = 1f;
        float idleAmount = 0.02f;
        
        // 🔧 무한 루프 조건 수정: 간단하고 안전한 방식
        while (enableAnimations && gameObject != null && gameObject.activeInHierarchy)
        {
            float idleOffset = Mathf.Sin(Time.time * idleSpeed) * idleAmount;
            transform.localPosition = startPos + Vector3.up * idleOffset;
            yield return null;
        }
        
        // 정리
        idleAnimation = null;
    }
    
    /// <summary>
    /// 모든 애니메이션 중지 및 초기화
    /// </summary>
    public void ResetToDefault()
    {
        // 모든 코루틴 중지
        if (currentAnimation != null)
        {
            StopCoroutine(currentAnimation);
            currentAnimation = null;
        }
        
        if (idleAnimation != null)
        {
            StopCoroutine(idleAnimation);
            idleAnimation = null;
        }
        
        StopWalking();
        
        // 위치 및 회전 초기화
        transform.localPosition = originalPosition;
        transform.localScale = originalScale;
        transform.localRotation = originalRotation;
        
        // 기본 표정으로 복귀
        if (enableAnimations)
        {
            ChangeExpression(neutralSprite, false);
        }
    }
    
    /// <summary>
    /// 현재 애니메이션 중인지 확인
    /// </summary>
    public bool IsAnimating()
    {
        return currentAnimation != null || walkAnimation != null || idleAnimation != null;
    }
    
    /// <summary>
    /// 애니메이션 활성화/비활성화
    /// </summary>
    public void SetAnimationsEnabled(bool enabled)
    {
        enableAnimations = enabled;
        
        if (!enabled)
        {
            ResetToDefault();
        }
    }
}