// Assets/Scripts/Home/HomeManager.cs
// 홈 씬 전체 관리 매니저

using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class HomeManager : MonoBehaviour
{
    [Header("📱 UI 패널들")]
    public GameObject mainMenuPanel;
    public GameObject statsPanel;
    public GameObject settingsPanel;
    public GameObject creditsPanel;
    
    [Header("🎮 메인 메뉴 버튼들")]
    public Button startGameButton;
    public Button statsButton;
    public Button settingsButton;
    public Button creditsButton;
    public Button quitButton;
    
    [Header("📊 통계 UI")]
    public TextMeshProUGUI totalPointsText;
    public TextMeshProUGUI totalDaysText;
    public TextMeshProUGUI bestDayScoreText;
    public TextMeshProUGUI totalHotteoksText;
    public TextMeshProUGUI perfectStreakText;
    public TextMeshProUGUI satisfactionRateText;
    public Button statsCloseButton;
    
    [Header("⚙️ 설정 UI")]
    public Slider masterVolumeSlider;
    public Slider bgmVolumeSlider;
    public Slider sfxVolumeSlider;
    public Toggle fullscreenToggle;
    public Dropdown resolutionDropdown;
    public Button settingsCloseButton;
    public Button resetDataButton;
    
    [Header("🎭 크레딧 UI")]
    public TextMeshProUGUI creditsText;
    public Button creditsCloseButton;
    
    [Header("🎨 타이틀 애니메이션")]
    public TextMeshProUGUI gameTitle;
    public TextMeshProUGUI gameSubtitle;
    public AnimationCurve titleBounceCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
    public float titleAnimationDuration = 2f;
    
    [Header("✨ 배경 효과")]
    public ParticleSystem backgroundParticles;
    public Image backgroundImage;
    public Color[] backgroundColors = { Color.white, Color.cyan, Color.magenta };
    public float colorChangeSpeed = 2f;
    
    [Header("🔊 오디오")]
    public AudioSource bgmAudioSource;
    public AudioClip homeBGM;
    public AudioClip buttonHoverSound;
    public AudioClip buttonClickSound;
    public AudioClip panelOpenSound;
    public AudioClip panelCloseSound;
    
    [Header("🎭 애니메이션 설정")]
    public float panelFadeSpeed = 3f;
    public float buttonHoverScale = 1.1f;
    public float buttonAnimationSpeed = 5f;
    
    [Header("💾 데이터")]
    public bool showWelcomeMessage = true;
    public string welcomeMessage = "호떡마스터에 오신 것을 환영합니다!";
    
    // 내부 변수들
    private AudioSource sfxAudioSource;
    private bool isInitialized = false;
    private Coroutine backgroundColorCoroutine;
    private Coroutine titleAnimationCoroutine;
    
    // 해상도 설정
    private Resolution[] resolutions;
    
    void Start()
    {
        InitializeHomeManager();
        SetupUI();
        StartBackgroundEffects();
        PlayHomeBGM();
        
        if (showWelcomeMessage)
        {
            ShowWelcomeMessage();
        }
    }
    
    /// <summary>
    /// 홈 매니저 초기화
    /// </summary>
    void InitializeHomeManager()
    {
        // AudioSource 설정
        sfxAudioSource = gameObject.AddComponent<AudioSource>();
        sfxAudioSource.loop = false;
        
        // BGM AudioSource 설정
        if (bgmAudioSource == null)
        {
            bgmAudioSource = gameObject.AddComponent<AudioSource>();
        }
        bgmAudioSource.loop = true;
        bgmAudioSource.volume = 0.5f;
        
        // 해상도 설정
        SetupResolutions();
        
        // 저장된 설정 로드
        LoadSettings();
        
        isInitialized = true;
        Debug.Log("🏠 HomeManager 초기화 완료");
    }
    
    /// <summary>
    /// UI 설정 및 버튼 이벤트 연결
    /// </summary>
    void SetupUI()
    {
        // 초기 패널 상태 설정
        ShowMainMenu();
        
        // 메인 메뉴 버튼 이벤트 연결
        if (startGameButton != null)
        {
            startGameButton.onClick.AddListener(OnStartGameClicked);
            AddButtonHoverEffect(startGameButton);
        }
        
        if (statsButton != null)
        {
            statsButton.onClick.AddListener(OnStatsClicked);
            AddButtonHoverEffect(statsButton);
        }
        
        if (settingsButton != null)
        {
            settingsButton.onClick.AddListener(OnSettingsClicked);
            AddButtonHoverEffect(settingsButton);
        }
        
        if (creditsButton != null)
        {
            creditsButton.onClick.AddListener(OnCreditsClicked);
            AddButtonHoverEffect(creditsButton);
        }
        
        if (quitButton != null)
        {
            quitButton.onClick.AddListener(OnQuitClicked);
            AddButtonHoverEffect(quitButton);
        }
        
        // 패널 닫기 버튼들
        if (statsCloseButton != null)
            statsCloseButton.onClick.AddListener(OnStatsCloseClicked);
            
        if (settingsCloseButton != null)
            settingsCloseButton.onClick.AddListener(OnSettingsCloseClicked);
            
        if (creditsCloseButton != null)
            creditsCloseButton.onClick.AddListener(OnCreditsCloseClicked);
        
        // 설정 UI 이벤트
        SetupSettingsUI();
        
        // 데이터 리셋 버튼
        if (resetDataButton != null)
            resetDataButton.onClick.AddListener(OnResetDataClicked);
    }
    
    /// <summary>
    /// 설정 UI 이벤트 설정
    /// </summary>
    void SetupSettingsUI()
    {
        if (masterVolumeSlider != null)
            masterVolumeSlider.onValueChanged.AddListener(OnMasterVolumeChanged);
            
        if (bgmVolumeSlider != null)
            bgmVolumeSlider.onValueChanged.AddListener(OnBGMVolumeChanged);
            
        if (sfxVolumeSlider != null)
            sfxVolumeSlider.onValueChanged.AddListener(OnSFXVolumeChanged);
            
        if (fullscreenToggle != null)
            fullscreenToggle.onValueChanged.AddListener(OnFullscreenToggled);
            
        if (resolutionDropdown != null)
            resolutionDropdown.onValueChanged.AddListener(OnResolutionChanged);
    }
    
    /// <summary>
    /// 해상도 설정
    /// </summary>
    void SetupResolutions()
    {
        resolutions = Screen.resolutions;
        
        if (resolutionDropdown != null)
        {
            resolutionDropdown.ClearOptions();
            
            System.Collections.Generic.List<string> options = new System.Collections.Generic.List<string>();
            int currentResolutionIndex = 0;
            
            for (int i = 0; i < resolutions.Length; i++)
            {
                string option = resolutions[i].width + " x " + resolutions[i].height;
                options.Add(option);
                
                if (resolutions[i].width == Screen.currentResolution.width &&
                    resolutions[i].height == Screen.currentResolution.height)
                {
                    currentResolutionIndex = i;
                }
            }
            
            resolutionDropdown.AddOptions(options);
            resolutionDropdown.value = currentResolutionIndex;
            resolutionDropdown.RefreshShownValue();
        }
    }
    
    /// <summary>
    /// 배경 효과 시작
    /// </summary>
    void StartBackgroundEffects()
    {
        // 배경 색상 애니메이션
        if (backgroundImage != null)
        {
            backgroundColorCoroutine = StartCoroutine(AnimateBackgroundColor());
        }
        
        // 타이틀 애니메이션
        if (gameTitle != null)
        {
            titleAnimationCoroutine = StartCoroutine(AnimateTitle());
        }
        
        // 파티클 효과
        if (backgroundParticles != null)
        {
            backgroundParticles.Play();
        }
    }
    
    /// <summary>
    /// 배경 색상 애니메이션
    /// </summary>
    IEnumerator AnimateBackgroundColor()
    {
        int colorIndex = 0;
        
        while (true)
        {
            if (backgroundColors.Length > 0)
            {
                Color targetColor = backgroundColors[colorIndex];
                Color startColor = backgroundImage.color;
                
                float elapsedTime = 0f;
                float duration = 1f / colorChangeSpeed;
                
                while (elapsedTime < duration)
                {
                    elapsedTime += Time.deltaTime;
                    float t = elapsedTime / duration;
                    backgroundImage.color = Color.Lerp(startColor, targetColor, t);
                    yield return null;
                }
                
                colorIndex = (colorIndex + 1) % backgroundColors.Length;
                yield return new WaitForSeconds(2f);
            }
            else
            {
                yield return new WaitForSeconds(1f);
            }
        }
    }
    
    /// <summary>
    /// 타이틀 애니메이션
    /// </summary>
    IEnumerator AnimateTitle()
    {
        if (gameTitle == null) yield break;
        
        Vector3 originalScale = gameTitle.transform.localScale;
        
        while (true)
        {
            float elapsedTime = 0f;
            
            while (elapsedTime < titleAnimationDuration)
            {
                elapsedTime += Time.deltaTime;
                float t = elapsedTime / titleAnimationDuration;
                float curveValue = titleBounceCurve.Evaluate(t);
                
                gameTitle.transform.localScale = originalScale * (1f + curveValue * 0.1f);
                yield return null;
            }
            
            yield return new WaitForSeconds(3f);
        }
    }
    
    /// <summary>
    /// 홈 BGM 재생
    /// </summary>
    void PlayHomeBGM()
    {
        if (bgmAudioSource != null && homeBGM != null)
        {
            bgmAudioSource.clip = homeBGM;
            bgmAudioSource.Play();
        }
    }
    
    /// <summary>
    /// 환영 메시지 표시
    /// </summary>
    void ShowWelcomeMessage()
    {
        // 간단한 환영 메시지 (필요시 별도 UI로 구현)
        Debug.Log("🎉 " + welcomeMessage);
    }
    
    /// <summary>
    /// 버튼 호버 효과 추가
    /// </summary>
    void AddButtonHoverEffect(Button button)
    {
        if (button == null) return;
        
        // EventTrigger 컴포넌트 추가
        UnityEngine.EventSystems.EventTrigger trigger = button.gameObject.GetComponent<UnityEngine.EventSystems.EventTrigger>();
        if (trigger == null)
        {
            trigger = button.gameObject.AddComponent<UnityEngine.EventSystems.EventTrigger>();
        }
        
        // 마우스 엔터 이벤트
        UnityEngine.EventSystems.EventTrigger.Entry pointerEnter = new UnityEngine.EventSystems.EventTrigger.Entry();
        pointerEnter.eventID = UnityEngine.EventSystems.EventTriggerType.PointerEnter;
        pointerEnter.callback.AddListener((data) => { OnButtonHover(button, true); });
        trigger.triggers.Add(pointerEnter);
        
        // 마우스 엑시트 이벤트
        UnityEngine.EventSystems.EventTrigger.Entry pointerExit = new UnityEngine.EventSystems.EventTrigger.Entry();
        pointerExit.eventID = UnityEngine.EventSystems.EventTriggerType.PointerExit;
        pointerExit.callback.AddListener((data) => { OnButtonHover(button, false); });
        trigger.triggers.Add(pointerExit);
    }
    
    /// <summary>
    /// 버튼 호버 효과 처리
    /// </summary>
    void OnButtonHover(Button button, bool isHovering)
    {
        if (button == null) return;
        
        Vector3 targetScale = isHovering ? Vector3.one * buttonHoverScale : Vector3.one;
        StartCoroutine(AnimateButtonScale(button.transform, targetScale));
        
        if (isHovering && buttonHoverSound != null)
        {
            PlaySFX(buttonHoverSound);
        }
    }
    
    /// <summary>
    /// 버튼 스케일 애니메이션
    /// </summary>
    IEnumerator AnimateButtonScale(Transform buttonTransform, Vector3 targetScale)
    {
        Vector3 startScale = buttonTransform.localScale;
        float elapsedTime = 0f;
        float duration = 1f / buttonAnimationSpeed;
        
        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            float t = elapsedTime / duration;
            buttonTransform.localScale = Vector3.Lerp(startScale, targetScale, t);
            yield return null;
        }
        
        buttonTransform.localScale = targetScale;
    }
    
    // ===== 버튼 이벤트 핸들러들 =====
    
    /// <summary>
    /// 게임 시작 버튼 클릭
    /// </summary>
    void OnStartGameClicked()
    {
        Debug.Log("🎮 게임 시작 버튼 클릭!");
        PlaySFX(buttonClickSound);
        
        if (SceneTransitionManager.Instance != null)
        {
            SceneTransitionManager.Instance.LoadGameScene();
        }
        else
        {
            Debug.LogError("SceneTransitionManager가 없습니다!");
        }
    }
    
    /// <summary>
    /// 통계 버튼 클릭
    /// </summary>
    void OnStatsClicked()
    {
        Debug.Log("📊 통계 버튼 클릭!");
        PlaySFX(buttonClickSound);
        PlaySFX(panelOpenSound);
        ShowStatsPanel();
    }
    
    /// <summary>
    /// 설정 버튼 클릭
    /// </summary>
    void OnSettingsClicked()
    {
        Debug.Log("⚙️ 설정 버튼 클릭!");
        PlaySFX(buttonClickSound);
        PlaySFX(panelOpenSound);
        ShowSettingsPanel();
    }
    
    /// <summary>
    /// 크레딧 버튼 클릭
    /// </summary>
    void OnCreditsClicked()
    {
        Debug.Log("🎭 크레딧 버튼 클릭!");
        PlaySFX(buttonClickSound);
        PlaySFX(panelOpenSound);
        ShowCreditsPanel();
    }
    
    /// <summary>
    /// 종료 버튼 클릭
    /// </summary>
    void OnQuitClicked()
    {
        Debug.Log("🚪 종료 버튼 클릭!");
        PlaySFX(buttonClickSound);
        
        if (SceneTransitionManager.Instance != null)
        {
            SceneTransitionManager.Instance.QuitGame();
        }
        else
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
    
    /// <summary>
    /// 통계 패널 닫기
    /// </summary>
    void OnStatsCloseClicked()
    {
        PlaySFX(buttonClickSound);
        PlaySFX(panelCloseSound);
        ShowMainMenu();
    }
    
    /// <summary>
    /// 설정 패널 닫기
    /// </summary>
    void OnSettingsCloseClicked()
    {
        PlaySFX(buttonClickSound);
        PlaySFX(panelCloseSound);
        SaveSettings();
        ShowMainMenu();
    }
    
    /// <summary>
    /// 크레딧 패널 닫기
    /// </summary>
    void OnCreditsCloseClicked()
    {
        PlaySFX(buttonClickSound);
        PlaySFX(panelCloseSound);
        ShowMainMenu();
    }
    
    /// <summary>
    /// 데이터 리셋 버튼 클릭
    /// </summary>
    void OnResetDataClicked()
    {
        PlaySFX(buttonClickSound);
        
        // 확인 다이얼로그 (간단히 Debug.Log로 대체)
        Debug.Log("⚠️ 모든 데이터가 초기화됩니다!");
        
        // PointManager 데이터 리셋
        if (PointManager.Instance != null)
        {
            PointManager.Instance.ResetAllStats();
        }
        
        // PlayerPrefs 초기화
        PlayerPrefs.DeleteAll();
        PlayerPrefs.Save();
        
        // 통계 UI 업데이트
        UpdateStatsDisplay();
        
        Debug.Log("🔄 모든 데이터가 초기화되었습니다!");
    }
    
    // ===== 설정 이벤트 핸들러들 =====
    
    void OnMasterVolumeChanged(float value)
    {
        AudioListener.volume = value;
        Debug.Log($"🔊 마스터 볼륨: {value:F2}");
    }
    
    void OnBGMVolumeChanged(float value)
    {
        if (bgmAudioSource != null)
        {
            bgmAudioSource.volume = value;
        }
        Debug.Log($"🎵 BGM 볼륨: {value:F2}");
    }
    
    void OnSFXVolumeChanged(float value)
    {
        if (sfxAudioSource != null)
        {
            sfxAudioSource.volume = value;
        }
        Debug.Log($"🔉 SFX 볼륨: {value:F2}");
    }
    
    void OnFullscreenToggled(bool isFullscreen)
    {
        Screen.fullScreen = isFullscreen;
        Debug.Log($"🖥️ 전체화면: {isFullscreen}");
    }
    
    void OnResolutionChanged(int resolutionIndex)
    {
        if (resolutions != null && resolutionIndex < resolutions.Length)
        {
            Resolution resolution = resolutions[resolutionIndex];
            Screen.SetResolution(resolution.width, resolution.height, Screen.fullScreen);
            Debug.Log($"📺 해상도 변경: {resolution.width}x{resolution.height}");
        }
    }
    
    // ===== 패널 관리 =====
    
    /// <summary>
    /// 메인 메뉴 표시
    /// </summary>
    void ShowMainMenu()
    {
        SetPanelActive(mainMenuPanel, true);
        SetPanelActive(statsPanel, false);
        SetPanelActive(settingsPanel, false);
        SetPanelActive(creditsPanel, false);
    }
    
    /// <summary>
    /// 통계 패널 표시
    /// </summary>
    void ShowStatsPanel()
    {
        SetPanelActive(mainMenuPanel, false);
        SetPanelActive(statsPanel, true);
        SetPanelActive(settingsPanel, false);
        SetPanelActive(creditsPanel, false);
        
        UpdateStatsDisplay();
    }
    
    /// <summary>
    /// 설정 패널 표시
    /// </summary>
    void ShowSettingsPanel()
    {
        SetPanelActive(mainMenuPanel, false);
        SetPanelActive(statsPanel, false);
        SetPanelActive(settingsPanel, true);
        SetPanelActive(creditsPanel, false);
        
        LoadSettingsToUI();
    }
    
    /// <summary>
    /// 크레딧 패널 표시
    /// </summary>
    void ShowCreditsPanel()
    {
        SetPanelActive(mainMenuPanel, false);
        SetPanelActive(statsPanel, false);
        SetPanelActive(settingsPanel, false);
        SetPanelActive(creditsPanel, true);
        
        ShowCreditsInfo();
    }
    
    /// <summary>
    /// 패널 활성화/비활성화
    /// </summary>
    void SetPanelActive(GameObject panel, bool active)
    {
        if (panel != null)
        {
            panel.SetActive(active);
            
            if (active)
            {
                // 패널 페이드 인 애니메이션 (필요시 구현)
                StartCoroutine(FadeInPanel(panel));
            }
        }
    }
    
    /// <summary>
    /// 패널 페이드 인 애니메이션
    /// </summary>
    IEnumerator FadeInPanel(GameObject panel)
    {
        CanvasGroup canvasGroup = panel.GetComponent<CanvasGroup>();
        if (canvasGroup == null)
        {
            canvasGroup = panel.AddComponent<CanvasGroup>();
        }
        
        canvasGroup.alpha = 0f;
        
        while (canvasGroup.alpha < 1f)
        {
            canvasGroup.alpha += Time.deltaTime * panelFadeSpeed;
            yield return null;
        }
        
        canvasGroup.alpha = 1f;
    }
    
    /// <summary>
    /// 통계 화면 업데이트
    /// </summary>
    void UpdateStatsDisplay()
    {
        if (PointManager.Instance != null)
        {
            var pointData = PointManager.Instance.GetPointData();
            
            if (totalPointsText != null)
                totalPointsText.text = $"총 포인트: {pointData.currentPoints:N0}";
                
            if (totalDaysText != null)
                totalDaysText.text = $"플레이 일수: {pointData.totalDaysPlayed}일";
                
            if (bestDayScoreText != null)
                bestDayScoreText.text = $"최고 일일 점수: {pointData.bestDayScore:N0}";
                
            if (totalHotteoksText != null)
                totalHotteoksText.text = $"총 호떡 판매: {pointData.totalHotteoksSold}개";
                
            if (perfectStreakText != null)
                perfectStreakText.text = $"최장 Perfect: {pointData.longestPerfectStreak}연속";
                
            if (satisfactionRateText != null)
                satisfactionRateText.text = $"평균 만족도: {pointData.averageCustomerSatisfactionRate:P1}";
        }
        else
        {
            // PointManager가 없을 때 기본값 표시
            if (totalPointsText != null) totalPointsText.text = "총 포인트: 0";
            if (totalDaysText != null) totalDaysText.text = "플레이 일수: 0일";
            if (bestDayScoreText != null) bestDayScoreText.text = "최고 일일 점수: 0";
            if (totalHotteoksText != null) totalHotteoksText.text = "총 호떡 판매: 0개";
            if (perfectStreakText != null) perfectStreakText.text = "최장 Perfect: 0연속";
            if (satisfactionRateText != null) satisfactionRateText.text = "평균 만족도: 0%";
        }
    }
    
    /// <summary>
    /// 크레딧 정보 표시
    /// </summary>
    void ShowCreditsInfo()
    {
        if (creditsText != null)
        {
            creditsText.text = 
                "🍯 호떡마스터 🍯\n\n" +
                "게임 개발: [개발자 이름]\n" +
                "프로그래밍: Unity C#\n" +
                "UI/UX 디자인: [디자이너 이름]\n" +
                "사운드: [사운드 아티스트]\n" +
                "음악: [작곡가]\n\n" +
                "특별한 감사:\n" +
                "- 모든 플레이어들\n" +
                "- 베타 테스터들\n" +
                "- Unity Community\n\n" +
                "버전: 1.0.0\n" +
                "© 2024 호떡마스터 팀";
        }
    }
    
    /// <summary>
    /// 설정 저장
    /// </summary>
    void SaveSettings()
    {
        if (masterVolumeSlider != null)
            PlayerPrefs.SetFloat("MasterVolume", masterVolumeSlider.value);
            
        if (bgmVolumeSlider != null)
            PlayerPrefs.SetFloat("BGMVolume", bgmVolumeSlider.value);
            
        if (sfxVolumeSlider != null)
            PlayerPrefs.SetFloat("SFXVolume", sfxVolumeSlider.value);
            
        if (fullscreenToggle != null)
            PlayerPrefs.SetInt("Fullscreen", fullscreenToggle.isOn ? 1 : 0);
            
        if (resolutionDropdown != null)
            PlayerPrefs.SetInt("ResolutionIndex", resolutionDropdown.value);
        
        PlayerPrefs.Save();
        Debug.Log("💾 설정이 저장되었습니다!");
    }
    
    /// <summary>
    /// 설정 로드
    /// </summary>
    void LoadSettings()
    {
        float masterVolume = PlayerPrefs.GetFloat("MasterVolume", 1f);
        float bgmVolume = PlayerPrefs.GetFloat("BGMVolume", 0.5f);
        float sfxVolume = PlayerPrefs.GetFloat("SFXVolume", 0.7f);
        bool fullscreen = PlayerPrefs.GetInt("Fullscreen", 1) == 1;
        int resolutionIndex = PlayerPrefs.GetInt("ResolutionIndex", 0);
        
        // 설정 적용
        AudioListener.volume = masterVolume;
        if (bgmAudioSource != null) bgmAudioSource.volume = bgmVolume;
        if (sfxAudioSource != null) sfxAudioSource.volume = sfxVolume;
        Screen.fullScreen = fullscreen;
        
        if (resolutions != null && resolutionIndex < resolutions.Length)
        {
            Resolution resolution = resolutions[resolutionIndex];
            Screen.SetResolution(resolution.width, resolution.height, Screen.fullScreen);
        }
    }
    
    /// <summary>
    /// 설정을 UI에 로드
    /// </summary>
    void LoadSettingsToUI()
    {
        if (masterVolumeSlider != null)
            masterVolumeSlider.value = PlayerPrefs.GetFloat("MasterVolume", 1f);
            
        if (bgmVolumeSlider != null)
            bgmVolumeSlider.value = PlayerPrefs.GetFloat("BGMVolume", 0.5f);
            
        if (sfxVolumeSlider != null)
            sfxVolumeSlider.value = PlayerPrefs.GetFloat("SFXVolume", 0.7f);
            
        if (fullscreenToggle != null)
            fullscreenToggle.isOn = PlayerPrefs.GetInt("Fullscreen", 1) == 1;
            
        if (resolutionDropdown != null)
            resolutionDropdown.value = PlayerPrefs.GetInt("ResolutionIndex", 0);
    }
    
    /// <summary>
    /// SFX 사운드 재생
    /// </summary>
    void PlaySFX(AudioClip clip)
    {
        if (sfxAudioSource != null && clip != null)
        {
            sfxAudioSource.PlayOneShot(clip);
        }
    }
    
    void OnDestroy()
    {
        // 코루틴 정지
        if (backgroundColorCoroutine != null)
            StopCoroutine(backgroundColorCoroutine);
            
        if (titleAnimationCoroutine != null)
            StopCoroutine(titleAnimationCoroutine);
    }
}