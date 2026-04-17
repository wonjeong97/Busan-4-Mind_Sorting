using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class SortManager : MonoBehaviour
{   
    public static SortManager Instance { get; private set; }
    
    [SerializeField] private GameObject page2;
    [SerializeField] private GameObject page3;
    [SerializeField] private GameObject page4;
    [SerializeField] private GameObject pageLast;
    
    [Header("Sub Monitor Pages")]
    [SerializeField] private GameObject[] subPages;

    [Header("Sub Monitor Navigation")]
    [SerializeField] private Button subBackButton;
    [SerializeField] private Button subToTitleButton; // 처음으로 버튼 필드 추가
    
    [Header("Result Texts")]
    [SerializeField] private Text subResultText; 
    [SerializeField] private Text mainResultText; 
    
    [Header("Effect Settings")]
    [SerializeField] private CanvasGroup flashCanvasGroup;
    
    [Header("Sub Page 3 Settings")]
    [SerializeField] private Text subPage3Text;
    [SerializeField] private Button subPage3NextButton;
    
    private int currentSubIdx;
    private Coroutine flashCoroutine;
    private Coroutine buttonShowCoroutine;
    
    /// <summary>
    /// 싱글톤 초기화 및 버튼 리스너 연결, 초기 씬 구성을 수행함.
    /// </summary>
    private void Awake()
    {   
        if (Instance)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        
        // 버튼 리스너 바인딩
        if (subBackButton)
        {
            subBackButton.onClick.AddListener(OnSubBackClicked);
        }

        if (subToTitleButton)
        {
            subToTitleButton.onClick.AddListener(OnSubToTitleClicked);
        }

        if (subPage3NextButton)
        {
            subPage3NextButton.onClick.AddListener(OnSubPage3NextClicked);
        }
        
        InitializeSortScene(GameData.selectedBranchIndex);
    }

    /// <summary>
    /// 메인 화면의 분기 상태와 서브 화면의 페이지를 초기화함.
    /// </summary>
    private void InitializeSortScene(int branchIndex)
    {
        if (page2) page2.SetActive(branchIndex == 0);
        if (page3) page3.SetActive(branchIndex == 1);
        if (page4) page4.SetActive(branchIndex == 2);
        
        if (pageLast) pageLast.SetActive(false);
        
        InitializeSubPages();
    }
    
    /// <summary>
    /// 서브 페이지를 1페이지(인덱스 0)로 활성화함.
    /// </summary>
    private void InitializeSubPages()
    {
        if (subPages == null) return;
        SetSubPage(0);
    }
    
    /// <summary>
    /// 입력 완료 시 텍스트 가공 및 플래시 연출을 시작함.
    /// </summary>
    public void OnInputCompleted(string inputText)
    {
        if (subResultText) subResultText.text = string.Format("<{0}>", inputText);
        if (mainResultText) mainResultText.text = inputText;

        if (subPage3Text)
        {
            subPage3Text.text = string.Format("<color=#E66D7A><{0}></color>로 분류했군요.\n정말 멋져요!", inputText);
        }

        if (flashCoroutine != null)
        {
            StopCoroutine(flashCoroutine);
        }

        flashCoroutine = StartCoroutine(FlashAndMoveToNextPage());
    }
    
    /// <summary>
    /// 플래시 연출 후 3초 대기하여 다음 페이지로 전환함.
    /// </summary>
    private IEnumerator FlashAndMoveToNextPage()
    {
        SetSubPage(1);

        if (flashCanvasGroup)
        {
            float duration = 0.2f;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                flashCanvasGroup.alpha = Mathf.Lerp(0f, 1f, elapsed / duration);
                yield return null;
            }

            elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                flashCanvasGroup.alpha = Mathf.Lerp(1f, 0f, elapsed / duration);
                yield return null;
            }
            
            flashCanvasGroup.alpha = 0f;
        }

        yield return new WaitForSeconds(3f);
        SetSubPage(2);
        flashCoroutine = null; 
    }
    
    /// <summary>
    /// 뒤로가기 클릭 시 연출 중단 및 페이지 이동을 처리함.
    /// </summary>
    private void OnSubBackClicked()
    {
        if (buttonShowCoroutine != null)
        {
            StopCoroutine(buttonShowCoroutine);
            buttonShowCoroutine = null;
        }

        if (flashCoroutine != null)
        {
            StopCoroutine(flashCoroutine);
            flashCoroutine = null;

            if (flashCanvasGroup)
            {
                flashCanvasGroup.alpha = 0f;
            }
        }

        if (currentSubIdx == 0)
        {
            if (GameManager.Instance)
            {
                GameManager.Instance.GoToGuide();
            }
            else
            {
                SceneManager.LoadScene(SceneName.Guide);    
            }
            return;
        }

        if (currentSubIdx == 2)
        {
            ResetInputsAndTexts();
            SetSubPage(0);
            return;
        }

        if (currentSubIdx == 1)
        {
            ResetInputsAndTexts();
        }

        SetSubPage(currentSubIdx - 1);
    }

    /// <summary>
    /// 처음으로 버튼 클릭 시 타이틀 씬으로 이동함.
    /// </summary>
    private void OnSubToTitleClicked()
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
    
    /// <summary>
    /// 인풋 필드와 결과 텍스트를 초기화함.
    /// </summary>
    private void ResetInputsAndTexts()
    {
        if (subResultText) subResultText.text = string.Empty;
        if (mainResultText) mainResultText.text = string.Empty;

        if (KeyboardManager.Instance)
        {
            KeyboardManager.Instance.ClearInput();
        }
    }

    /// <summary>
    /// 서브 페이지를 전환하고 페이지 인덱스별 특수 로직을 실행함.
    /// </summary>
    private void SetSubPage(int index)
    {
        if (subPages == null || index < 0 || index >= subPages.Length) return;

        currentSubIdx = index;

        for (int i = 0; i < subPages.Length; i++)
        {
            if (subPages[i]) subPages[i].SetActive(i == index);
        }

        if (index == 2)
        {
            PrepareSubPage3();
        }

        // 페이지 4(index 3)일 때 뒤로가기 버튼 숨김
        if (subBackButton)
        {
            subBackButton.gameObject.SetActive(index != 3);
        }
    }

    /// <summary>
    /// 서브 페이지 3 진입 시 10초 후 다음 버튼이 나타나도록 설정함.
    /// </summary>
    private void PrepareSubPage3()
    {
        if (buttonShowCoroutine != null)
        {
            StopCoroutine(buttonShowCoroutine);
        }

        if (subPage3NextButton)
        {
            subPage3NextButton.gameObject.SetActive(false);
            buttonShowCoroutine = StartCoroutine(ShowButtonAfterDelay(10f));
        }
    }

    /// <summary>
    /// 지정된 지연 시간 후 버튼을 활성화함.
    /// </summary>
    private IEnumerator ShowButtonAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        
        if (subPage3NextButton)
        {
            subPage3NextButton.gameObject.SetActive(true);
        }
        
        buttonShowCoroutine = null;
    }

    /// <summary>
    /// 다음 버튼 클릭 시 최종 결과 페이지들을 활성화함.
    /// </summary>
    private void OnSubPage3NextClicked()
    {
        SetSubPage(3);

        if (page2) page2.SetActive(false);
        if (page3) page3.SetActive(false);
        if (page4) page4.SetActive(false);
        
        if (pageLast) pageLast.SetActive(true);
    }
}