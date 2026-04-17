using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class GuideManager : MonoBehaviour
{
    [Header("Main Display Pages")]
    [SerializeField] private GameObject[] mainPages;
    [SerializeField] private GameObject[] mainStep5Pages;
    [SerializeField] private GameObject mainStep5Pop;

    [Header("Sub Display Pages")]
    [SerializeField] private GameObject[] subPages;

    [Header("Sub Display Buttons")]
    [SerializeField] private Button nextButtonStep1;
    [SerializeField] private Button btn2, btn3, btn4;
    [SerializeField] private Button finishButton;
    [SerializeField] private Button backButton; // 공통 뒤로가기 버튼

    private int currentMainIdx;
    private bool isAtStep5;

    /// <summary>
    /// 버튼 리스너를 초기화하고 첫 페이지를 활성화함.
    /// </summary>
    /// <summary>
    /// 씬이 시작될 때 메인과 서브의 첫 페이지만 남기고 모든 요소를 비활성화함.
    /// </summary>
    private void Awake()
    {
        BindButtons();
        InitializeRoutine();
    }
    
    /// <summary>
    /// 모든 페이지와 팝업의 활성 상태를 초기화함.
    /// </summary>
    public void InitializeRoutine()
    {
        currentMainIdx = 0;
        isAtStep5 = false;

        // 메인 페이지 초기화: 0번 인덱스만 활성화
        for (int i = 0; i < mainPages.Length; i++)
        {
            if (mainPages[i])
            {
                mainPages[i].SetActive(i == 0);
            }
        }

        // 서브 페이지 초기화: 0번 인덱스만 활성화
        for (int i = 0; i < subPages.Length; i++)
        {
            if (subPages[i])
            {
                subPages[i].SetActive(i == 0);
            }
        }

        // 분기 페이지 및 팝업 일괄 비활성화
        ResetStep5();
    }

    /// <summary>
    /// 모든 서브 화면 버튼에 이벤트를 바인딩함.
    /// </summary>
    private void BindButtons()
    {
        if (nextButtonStep1) nextButtonStep1.onClick.AddListener(OnNextClicked);
        if (btn2) btn2.onClick.AddListener(() => OnBranchClicked(0));
        if (btn3) btn3.onClick.AddListener(() => OnBranchClicked(1));
        if (btn4) btn4.onClick.AddListener(() => OnBranchClicked(2));
        if (finishButton) finishButton.onClick.AddListener(OnFinishClicked);
        
        // 뒤로가기 버튼 연결
        if (backButton)
        {
            backButton.onClick.AddListener(OnBackClicked);
        }
        else
        {
            Debug.LogError("GuideManager: backButton 필드가 할당되지 않았습니다.");
        }
    }

    /// <summary>
    /// 현재 상태에 따라 이전 단계로 되돌아가거나 타이틀 씬으로 이동함.
    /// </summary>
    private void OnBackClicked()
    {
        // 1. 분기 선택 완료(Step 5) 상태일 때
        if (isAtStep5)
        {
            ResetStep5();
            currentMainIdx = 3; // 메인 Page 4로 복구
            ShowPage(currentMainIdx);
            ToggleSubPage(1);   // 서브 Page 2로 복구
            isAtStep5 = false;
            return;
        }

        // 2. 일반 페이지 진행 중일 때
        if (currentMainIdx > 0)
        {
            currentMainIdx--;
            ShowPage(currentMainIdx);
            
            // Page 4에서 이전으로 갈 경우 서브 페이지도 1단계로 복구
            if (currentMainIdx < 3)
            {
                ToggleSubPage(0);
            }
        }
        else
        {
            if (GameManager.Instance)
            {
                GameManager.Instance.GoToTitle();
            }
            else
            {
                SceneManager.LoadScene(SceneName.Title);
            }
        }
    }

    /// <summary>
    /// 5단계 관련 특수 페이지들과 팝업 오브젝트를 비활성화함.
    /// </summary>
    private void ResetStep5()
    {
        foreach (GameObject page in mainStep5Pages)
        {
            if (page)
            {
                page.SetActive(false);
            }
        }

        if (mainStep5Pop)
        {
            mainStep5Pop.SetActive(false);
        }
    }

    private void OnNextClicked()
    {
        currentMainIdx++;
        if (currentMainIdx < 3)
        {
            ShowPage(currentMainIdx);
        }
        else if (currentMainIdx == 3)
        {
            ShowPage(currentMainIdx);
            ToggleSubPage(1);
        }
    }

    private void OnBranchClicked(int index)
    {
        foreach (GameObject page in mainPages) if (page) page.SetActive(false);

        if (mainStep5Pages[index]) mainStep5Pages[index].SetActive(true);
        if (mainStep5Pop) mainStep5Pop.SetActive(true);

        isAtStep5 = true;
        ToggleSubPage(2);
        
        GameData.selectedBranchIndex = index;
    }

    private void ShowPage(int index)
    {
        for (int i = 0; i < mainPages.Length; i++)
        {
            if (mainPages[i]) mainPages[i].SetActive(i == index);
        }
    }

    private void ToggleSubPage(int subIdx)
    {
        for (int i = 0; i < subPages.Length; i++)
        {
            if (subPages[i]) subPages[i].SetActive(i == subIdx);
        }
    }

    private void OnFinishClicked()
    {
        if (GameManager.Instance)
        {
            GameManager.Instance.GoToSort();
        }
        else
        {
            SceneManager.LoadScene(SceneName.Sort);    
        }
    }
}