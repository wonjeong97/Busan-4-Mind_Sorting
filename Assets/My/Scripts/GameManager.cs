using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using Wonjeong.Reporter;
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
        if (systemCanvas != null)
            DontDestroyOnLoad(systemCanvas);
        TimestampLogHandler.Attach();
    }
    
    private void Start()
    {
        Cursor.visible = false;
        if (reporter && reporter.show) reporter.show = false;
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