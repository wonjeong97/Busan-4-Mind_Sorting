using UnityEngine;
using UnityEngine.UI;

public class KeyboardManager : MonoBehaviour
{ 
    public static KeyboardManager Instance { get; private set; }
    
    [SerializeField] private InputField targetInputField;

    [Header("Special Buttons")]
    [SerializeField] private Button backspaceButton;
    [SerializeField] private Button spaceButton;
    
    [Header("External UI References")]
    [SerializeField] private Button completeButton;

    private HangulAssembler assembler;

    /// <summary>
    /// 싱글톤 인스턴스를 할당하고 초기 설정을 수행함.
    /// </summary>
    private void Awake()
    {
        if (Instance)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        assembler = new HangulAssembler();
        
        if (targetInputField)
        {
            targetInputField.characterLimit = 15;
        }
        
        BindSpecialButtons();
        BindVirtualKeys();
    }
    
    /// <summary>
    /// 비활성화된 하위 오브젝트까지 포함하여 VirtualKey를 모두 찾아 이벤트를 등록함.
    /// </summary>
    private void BindVirtualKeys()
    {
        // true 파라미터를 넘겨 꺼져있는 하위 오브젝트에서도 컴포넌트를 찾음
        VirtualKey[] keys = GetComponentsInChildren<VirtualKey>(true);

        foreach (VirtualKey key in keys)
        {
            if (key)
            {
                Button btn = key.GetButton();
                string charCode = key.character;

                if (btn)
                {
                    btn.onClick.AddListener(() => OnKeyClick(charCode));
                }
                else
                {
                    // 버튼 컴포넌트가 같은 위치에 없을 경우 경고를 띄움
                    Debug.LogWarning($"VirtualKey '{charCode}' 오브젝트에 Button 컴포넌트가 없습니다!");
                }
            }
        }
    }

    private void BindSpecialButtons()
    {
        if (backspaceButton) backspaceButton.onClick.AddListener(OnBackspaceClick);
        if (spaceButton) spaceButton.onClick.AddListener(OnSpaceClick);
        if (completeButton) completeButton.onClick.AddListener(OnCompleteClick);
    }

    private void OnKeyClick(string charCode)
    {
        if (!targetInputField)
        {
            Debug.LogError("인풋 필드가 연결되지 않았습니다!");
            return;
        }

        targetInputField.text = assembler.AddChar(charCode);
    }

    /// <summary>
    /// 마지막 글자를 지우거나 조합 중인 자모를 제거함.
    /// </summary>
    private void OnBackspaceClick()
    {
        if (targetInputField)
        {
            targetInputField.text = assembler.RemoveChar();
        }
    }

    /// <summary>
    /// 공백을 추가함.
    /// </summary>
    private void OnSpaceClick()
    {
        if (targetInputField)
        {
            targetInputField.text = assembler.AddSpace();
        }
    }

    /// <summary>
    /// 완성 버튼 클릭 시 현재 인풋 필드의 텍스트를 SortManager로 전달하여 화면 전환 및 텍스트 출력을 지시함.
    /// </summary>
    private void OnCompleteClick()
    {
        if (!targetInputField) return;

        // 플래시 효과 중 연타 방지: 클릭 즉시 비활성화하고 ClearInput() 호출 시 복구함
        if (completeButton)
        {
            ColorBlock cb = completeButton.colors;
            cb.disabledColor = cb.normalColor;
            completeButton.colors = cb;
            completeButton.interactable = false;
        }

        string finalResult = targetInputField.text;

        // 암시적 불리언 변환을 통한 싱글톤 유효성 체크
        if (SortManager.Instance)
        {
            SortManager.Instance.OnInputCompleted(finalResult);
        }
        else
        {
            Debug.LogError("KeyboardManager: SortManager 인스턴스가 존재하지 않습니다.");
        }
    }
    
    /// <summary>
    /// 인풋 필드의 텍스트와 한글 조합 데이터를 모두 비움.
    /// </summary>
    public void ClearInput()
    {
        if (targetInputField)
        {
            targetInputField.text = string.Empty;
        }

        if (assembler != null)
        {
            assembler.Clear();
        }

        // 뒤로가기로 page0 복귀 시 완성 버튼을 다시 활성화함
        if (completeButton) completeButton.interactable = true;
    }
}