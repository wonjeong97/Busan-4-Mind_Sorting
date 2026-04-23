using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Wonjeong.Reporter;
using Wonjeong.UI;
using Wonjeong.Utils;

/// <summary>
/// 프로젝트의 전역 데이터 보관을 위한 정적 클래스.
/// </summary>
public static class GameData
{
    // 0: page2, 1: page3, 2: page4
    public static int selectedBranchIndex;
}

/// <summary>
/// 게임의 전체적인 상태와 씬 전환 로직을 관리하는 매니저 클래스.
/// </summary>
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }
    
    [SerializeField] private Reporter reporter;
    [SerializeField] private GameObject systemCanvas;
    
    [Header("UI Audio")]
    [SerializeField] private AudioSource uiAudioSource;
    [SerializeField] private AudioClip defaultClickSound;
    [SerializeField] private string mainBgmKey;
    
    [Header("Idle Timer Settings")]
    [SerializeField] private float maxIdleTime = 60f; // 최대 대기 시간 (초)
    
    private float currentIdleTime;
    public WebcamData WebcamConfig { get; private set; }

    /// <summary>
    /// 싱글톤 인스턴스를 설정하고 씬 전환 시 파괴되지 않도록 보호함.
    /// </summary>
    private void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
        
        LoadWebcamConfig();
        
        if (systemCanvas != null)
            DontDestroyOnLoad(systemCanvas);
        TimestampLogHandler.Attach();
    }
    
    /// <summary>
    /// 마우스 커서 숨김 처리, 다중 디스플레이 활성화 및 기본 BGM 재생을 수행함.
    /// </summary>
    private void Start()
    {
        Cursor.visible = false;
        
        if (reporter)
        {
            if (reporter.show) reporter.show = false;
        }
        
        if (Display.displays.Length > 1)
        {
            Display.displays[1].Activate();
        }

        // 추가된 로직: 씬 시작 시 SoundManager를 통해 BGM을 재생함
        if (SoundManager.Instance)
        {
            if (!string.IsNullOrEmpty(mainBgmKey))
            {
                SoundManager.Instance.PlayBGM(mainBgmKey);
            }
            else
            {
                Debug.LogWarning("GameManager: mainBgmKey가 인스펙터에 할당되지 않았습니다.");
            }
        }
    }
    
    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.D) && reporter)
        {
            reporter.showGameManagerControl = !reporter.showGameManagerControl;
            if (reporter.show) reporter.show = false;
        }
        else if (Input.GetKeyDown(KeyCode.M)) 
        {
            Cursor.visible = !Cursor.visible;
        }
        
        CheckIdleTime();
    }
    
    /// <summary>
    /// JSON 파일로부터 웹캠 설정을 로드하여 메모리에 캐싱함.
    /// </summary>
    public void LoadWebcamConfig()
    {
        WebcamData data = JsonLoader.Load<WebcamData>("Webcam.json");

        if (data != null)
        {
            WebcamConfig = data;
        }
        else
        {
            // 파일이 없을 경우 기본값으로 생성 및 캐싱
            WebcamConfig = new WebcamData();
            WebcamConfig.Left = 0f;
            WebcamConfig.Right = 0f;
            WebcamConfig.Top = 0f;
            WebcamConfig.Bottom = 0f;
            WebcamConfig.FlipX = false;
            WebcamConfig.FlipY = false;
            
            JsonLoader.Save<WebcamData>(WebcamConfig, "Webcam.json");
            Debug.LogWarning("GameManager: Webcam.json이 없어 기본값으로 생성했습니다.");
        }
    }
    
    /// <summary>
    /// 화면 터치, 마우스 클릭, 키보드 입력 등 어떠한 입력이라도 감지되면 타이머를 초기화하고, 
    /// 지정된 시간을 초과하면 타이틀 씬으로 강제 복귀시킴.
    /// </summary>
    private void CheckIdleTime()
    {
        // 타이틀 씬에서는 미입력 타이머가 작동할 필요가 없으므로 제외함
        if (SceneManager.GetActiveScene().name == SceneName.Title)
        {
            currentIdleTime = 0f;
            return;
        }

        // Input.anyKey는 터치와 마우스 클릭을 모두 포함하여 감지함
        if (Input.anyKey || Input.GetAxis("Mouse X") != 0 || Input.GetAxis("Mouse Y") != 0)
        {
            currentIdleTime = 0f;
        }
        else
        {
            currentIdleTime += Time.deltaTime;

            if (currentIdleTime >= maxIdleTime)
            {
                currentIdleTime = 0f;
                Debug.Log("GameManager: 장시간 입력이 없어 타이틀 씬으로 자동 복귀합니다.");
                GoToTitle();
            }
        }
    }

    /// <summary>
    /// 인덱스에 따라 분기 데이터를 저장하고 정렬 씬으로 전환함.
    /// </summary>
    /// <param name="branchIndex">사용자가 선택한 페이지 인덱스</param>
    public void StartSortScene(int branchIndex)
    {
        GameData.selectedBranchIndex = branchIndex;
        LoadScene(SceneName.Sort);
    }

    /// <summary>
    /// 가이드 씬으로 전환함.
    /// </summary>
    public void GoToGuide()
    {
        LoadScene(SceneName.Guide);
    }

    /// <summary>
    /// 타이틀 씬으로 전환함.
    /// </summary>
    public void GoToTitle()
    {
        LoadScene(SceneName.Title);
    }
    
    public void GoToSort()
    {
        LoadScene(SceneName.Sort);
    }

    /// <summary>
    /// 문자열 이름을 기반으로 실제 씬 로드를 수행함.
    /// </summary>
    /// <param name="sceneName">이동할 씬의 등록된 이름</param>
    private void LoadScene(string sceneName)
    {
        if (string.IsNullOrEmpty(sceneName))
        {
            Debug.LogError("GameManager: 로드하려는 씬 이름이 비어 있습니다.");
            return;
        }
        
        SceneManager.LoadScene(sceneName);
    }
    
    /// <summary>
    /// 기본 UI 클릭 효과음을 재생합니다.
    /// </summary>
    public void PlayUIClickSound()
    {
        if (!uiAudioSource)
        {
            Debug.LogWarning("GameManager에 uiAudioSource가 할당되지 않았습니다.");
            return;
        }

        if (!defaultClickSound)
        {
            Debug.LogWarning("GameManager에 defaultClickSound가 할당되지 않았습니다.");
            return;
        }

        uiAudioSource.PlayOneShot(defaultClickSound);
    }
}