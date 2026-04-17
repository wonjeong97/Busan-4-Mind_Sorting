using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class VirtualKey : MonoBehaviour
{
    [Tooltip("이 버튼이 입력할 한글 자모음 (예: ㄱ, ㅏ)")]
    public string character;

    private Button button;

    /// <summary>
    /// 버튼 컴포넌트를 반환함.
    /// </summary>
    public Button GetButton()
    {
        if (!button)
        {
            button = GetComponent<Button>();
        }
        return button;
    }
}