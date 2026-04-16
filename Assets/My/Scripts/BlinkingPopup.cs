using UnityEngine;
using UnityEngine.UI;

public class BlinkingPopup : MonoBehaviour
{
    [SerializeField]
    private CanvasGroup canvasGroup;

    [SerializeField]
    private float blinkSpeed;

    private bool isBlinking;

    /// <summary>
    /// 컴포넌트 유효성을 검증하고 버튼 이벤트를 연결함.
    /// </summary>
    private void Awake()
    {
        if (!canvasGroup)
        {
            Debug.LogError("BlinkingPopup: canvasGroup 할당 누락.");
        }
    }

    /// <summary>
    /// 팝업이 활성화될 때마다 깜빡임 상태를 초기화함.
    /// </summary>
    private void OnEnable()
    {
        isBlinking = true;
    }

    /// <summary>
    /// 플래그 기반으로 프레임마다 CanvasGroup의 투명도를 조절함.
    /// </summary>
    private void Update()
    {
        if (!isBlinking) return;

        // 매 프레임 호출되므로 Managed-Native 오버헤드 우회를 위해 ReferenceEquals 사용
        if (!ReferenceEquals(canvasGroup, null))
        {
            // 예시: Time.time=1.25, blinkSpeed=2 -> 2.5
            // 결과: Mathf.PingPong(2.5, 1f) -> 0.5 (알파값 50%)
            canvasGroup.alpha = Mathf.PingPong(Time.time * blinkSpeed, 1f);
        }
    }
}