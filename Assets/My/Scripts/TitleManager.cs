using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class TitleManager : MonoBehaviour
{
    [SerializeField]
    private Button startButton;

    /// <summary>
    /// 버튼 컴포넌트 유효성 검사 및 클릭 이벤트를 바인딩함.
    /// </summary>
    private void Awake()
    {
        if (startButton)
        {
            startButton.onClick.AddListener(OnStartButtonClicked);
        }
        else
        {
            // 할당되지 않았을 경우 추적을 위해 로그를 남김
            Debug.LogError("TitleManager: startButton 필드가 비어 있습니다.");
        }
    }

    /// <summary>
    /// 관리 클래스에 정의된 가이드 씬 이름으로 이동함.
    /// </summary>
    private void OnStartButtonClicked()
    {
        SceneManager.LoadScene(SceneName.Guide);
    }
}