// Assets/Scripts/SceneManagement/SceneTransitionManager.cs
// 씬 전환 관리 매니저

using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class SceneTransitionManager : MonoBehaviour
{
    [Header("씬 이름 설정")]
    public string homeSceneName = "HomeScene";
    public string gameSceneName = "GameScene";
    
    [Header("전환 효과")]
    public GameObject transitionCanvas;
    public CanvasGroup transitionCanvasGroup;
    public float fadeInDuration = 1f;
    public float fadeOutDuration = 1f;
    
    [Header("로딩 화면")]
    public GameObject loadingPanel;
    public UnityEngine.UI.Slider loadingProgressBar;
    public TMPro.TextMeshProUGUI loadingText;
    
    [Header("사운드")]
    public AudioClip buttonClickSound;
    public AudioClip sceneTransitionSound;
    
    // 싱글톤
    public static SceneTransitionManager Instance { get; private set; }
    
    private AudioSource audioSource;
    private bool isTransitioning = false;
    
    void Awake()
    {
        // 싱글톤 설정 및 씬 전환 시 파괴되지 않도록 설정
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            InitializeTransitionManager();
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    void InitializeTransitionManager()
    {
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
            audioSource = gameObject.AddComponent<AudioSource>();
        
        // 전환 캔버스 초기 설정
        if (transitionCanvas != null)
        {
            transitionCanvas.SetActive(false);
        }
        
        if (transitionCanvasGroup != null)
        {
            transitionCanvasGroup.alpha = 0f;
        }
        
        Debug.Log("🎬 SceneTransitionManager 초기화 완료");
    }
    
    /// <summary>
    /// 게임 씬으로 전환
    /// </summary>
    public void LoadGameScene()
    {
        if (isTransitioning) return;
        
        Debug.Log("🎮 게임 씬으로 전환 시작");
        PlayButtonSound();
        StartCoroutine(TransitionToScene(gameSceneName));
    }
    
    /// <summary>
    /// 홈 씬으로 전환
    /// </summary>
    public void LoadHomeScene()
    {
        if (isTransitioning) return;
        
        Debug.Log("🏠 홈 씬으로 전환 시작");
        PlayButtonSound();
        StartCoroutine(TransitionToScene(homeSceneName));
    }
    
    /// <summary>
    /// 씬 전환 코루틴
    /// </summary>
    IEnumerator TransitionToScene(string sceneName)
    {
        isTransitioning = true;
        
        // 전환 사운드 재생
        PlayTransitionSound();
        
        // 페이드 아웃
        yield return StartCoroutine(FadeOut());
        
        // 로딩 화면 표시
        ShowLoadingScreen();
        
        // 비동기 씬 로딩
        yield return StartCoroutine(LoadSceneAsync(sceneName));
        
        // 페이드 인
        yield return StartCoroutine(FadeIn());
        
        // 로딩 화면 숨기기
        HideLoadingScreen();
        
        isTransitioning = false;
        
        Debug.Log($"✅ {sceneName} 씬 전환 완료");
    }
    
    /// <summary>
    /// 비동기 씬 로딩
    /// </summary>
    IEnumerator LoadSceneAsync(string sceneName)
    {
        AsyncOperation asyncOperation = SceneManager.LoadSceneAsync(sceneName);
        asyncOperation.allowSceneActivation = false;
        
        while (!asyncOperation.isDone)
        {
            // 로딩 진행률 업데이트
            float progress = Mathf.Clamp01(asyncOperation.progress / 0.9f);
            UpdateLoadingProgress(progress);
            
            // 로딩이 90% 완료되면 씬 활성화
            if (asyncOperation.progress >= 0.9f)
            {
                UpdateLoadingProgress(1f);
                yield return new WaitForSeconds(0.5f); // 최소 로딩 시간
                asyncOperation.allowSceneActivation = true;
            }
            
            yield return null;
        }
    }
    
    /// <summary>
    /// 페이드 아웃 효과
    /// </summary>
    IEnumerator FadeOut()
    {
        if (transitionCanvas != null)
        {
            transitionCanvas.SetActive(true);
        }
        
        if (transitionCanvasGroup != null)
        {
            float elapsedTime = 0f;
            
            while (elapsedTime < fadeOutDuration)
            {
                elapsedTime += Time.unscaledDeltaTime;
                transitionCanvasGroup.alpha = Mathf.Lerp(0f, 1f, elapsedTime / fadeOutDuration);
                yield return null;
            }
            
            transitionCanvasGroup.alpha = 1f;
        }
    }
    
    /// <summary>
    /// 페이드 인 효과
    /// </summary>
    IEnumerator FadeIn()
    {
        if (transitionCanvasGroup != null)
        {
            float elapsedTime = 0f;
            
            while (elapsedTime < fadeInDuration)
            {
                elapsedTime += Time.unscaledDeltaTime;
                transitionCanvasGroup.alpha = Mathf.Lerp(1f, 0f, elapsedTime / fadeInDuration);
                yield return null;
            }
            
            transitionCanvasGroup.alpha = 0f;
        }
        
        if (transitionCanvas != null)
        {
            transitionCanvas.SetActive(false);
        }
    }
    
    /// <summary>
    /// 로딩 화면 표시
    /// </summary>
    void ShowLoadingScreen()
    {
        if (loadingPanel != null)
        {
            loadingPanel.SetActive(true);
        }
        
        if (loadingText != null)
        {
            loadingText.text = "로딩 중...";
        }
        
        UpdateLoadingProgress(0f);
    }
    
    /// <summary>
    /// 로딩 화면 숨기기
    /// </summary>
    void HideLoadingScreen()
    {
        if (loadingPanel != null)
        {
            loadingPanel.SetActive(false);
        }
    }
    
    /// <summary>
    /// 로딩 진행률 업데이트
    /// </summary>
    void UpdateLoadingProgress(float progress)
    {
        if (loadingProgressBar != null)
        {
            loadingProgressBar.value = progress;
        }
        
        if (loadingText != null)
        {
            loadingText.text = $"로딩 중... {Mathf.RoundToInt(progress * 100)}%";
        }
    }
    
    /// <summary>
    /// 버튼 클릭 사운드 재생
    /// </summary>
    void PlayButtonSound()
    {
        if (audioSource != null && buttonClickSound != null)
        {
            audioSource.PlayOneShot(buttonClickSound);
        }
    }
    
    /// <summary>
    /// 씬 전환 사운드 재생
    /// </summary>
    void PlayTransitionSound()
    {
        if (audioSource != null && sceneTransitionSound != null)
        {
            audioSource.PlayOneShot(sceneTransitionSound);
        }
    }
    
    /// <summary>
    /// 현재 씬이 홈 씬인지 확인
    /// </summary>
    public bool IsHomeScene()
    {
        return SceneManager.GetActiveScene().name == homeSceneName;
    }
    
    /// <summary>
    /// 현재 씬이 게임 씬인지 확인
    /// </summary>
    public bool IsGameScene()
    {
        return SceneManager.GetActiveScene().name == gameSceneName;
    }
    
    /// <summary>
    /// 게임 종료
    /// </summary>
    public void QuitGame()
    {
        Debug.Log("🚪 게임 종료");
        PlayButtonSound();
        
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
    
    /// <summary>
    /// 즉시 씬 전환 (전환 효과 없이)
    /// </summary>
    public void LoadSceneImmediate(string sceneName)
    {
        if (isTransitioning) return;
        
        Debug.Log($"⚡ {sceneName} 씬으로 즉시 전환");
        SceneManager.LoadScene(sceneName);
    }
}