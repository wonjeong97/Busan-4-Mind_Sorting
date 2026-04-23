using UnityEngine;
using UnityEngine.UI;
using Wonjeong.Utils;

/// <summary>
/// JSON 파싱을 위한 직렬화 데이터 클래스
/// </summary>
[System.Serializable]
public class WebcamData
{
    public float Left;
    public float Right;
    public float Top;
    public float Bottom;
    
    public bool FlipX;
    public bool FlipY;
}

[RequireComponent(typeof(RawImage))]
public class WebcamManager : MonoBehaviour
{
    [Header("Distance from Center (Pixels)")]
    public float Left;
    public float Right;
    public float Top;
    public float Bottom;
    
    [Header("Flip Settings")]
    public bool FlipX;
    public bool FlipY;

    private RawImage targetRawImage;

    private const float BaseWidth = 1920f;
    private const float BaseHeight = 1080f;

    /// <summary>
    /// 컴포넌트를 캐싱하고, 씬 시작 시 화면에 보이지 않도록 숨긴 후 초기 설정을 적용함.
    /// </summary>
    private void Awake()
    {
        targetRawImage = GetComponent<RawImage>();

        if (targetRawImage)
        {
            // 디버그용이므로 씬 시작 시 기본적으로 비활성화(숨김) 처리함
            targetRawImage.enabled = false;

            ApplyConfigFromManager();
        }
        else
        {
            Debug.LogWarning("WebcamManager: RawImage 컴포넌트를 찾을 수 없습니다.");
        }
    }

    /// <summary>
    /// 매 프레임 키 입력을 확인하여 설정을 리로드(F)하거나 화면 표시 여부를 토글(R)함.
    /// </summary>
    private void Update()
    {
        // F 키: 런타임에 JSON 설정을 다시 불러와 즉시 반영함
        if (Input.GetKeyDown(KeyCode.F))
        {
            if (GameManager.Instance)
            {
                GameManager.Instance.LoadWebcamConfig();
                ApplyConfigFromManager();
                Debug.Log("WebcamManager: JSON 설정을 리로드하여 다시 적용했습니다.");
            }
        }

        // R 키: 디버그용 웹캠 화면(RawImage)의 표시 상태를 켜거나 끔
        if (Input.GetKeyDown(KeyCode.R))
        {
            if (targetRawImage)
            {
                targetRawImage.enabled = !targetRawImage.enabled;
            }
        }
    }

    /// <summary>
    /// GameManager의 메모리에 상주하는 설정 값을 가져와 필드에 동기화하고 크롭을 실행함.
    /// </summary>
    private void ApplyConfigFromManager()
    {
        if (!GameManager.Instance) return;
        if (GameManager.Instance.WebcamConfig == null) return;

        WebcamData config = GameManager.Instance.WebcamConfig;
        
        Left = config.Left;
        Right = config.Right;
        Top = config.Top;
        Bottom = config.Bottom;
        FlipX = config.FlipX;
        FlipY = config.FlipY;

        ApplyCrop();
    }

    /// <summary>
    /// 설정된 픽셀 여백 값을 바탕으로 RawImage의 uvRect와 물리적 크기를 동기화하고 반전(Flip)을 적용함.
    /// </summary>
    public void ApplyCrop()
    {
        if (!targetRawImage) return;

        float centerX = BaseWidth * 0.5f;
        float centerY = BaseHeight * 0.5f;

        float startX = centerX - Left;
        float startY = centerY - Bottom;
        
        float cropWidth = Left + Right;
        float cropHeight = Top + Bottom;

        float normalizedX = startX / BaseWidth;
        float normalizedY = startY / BaseHeight;
        float normalizedWidth = cropWidth / BaseWidth;
        float normalizedHeight = cropHeight / BaseHeight;

        targetRawImage.uvRect = new Rect(normalizedX, normalizedY, normalizedWidth, normalizedHeight);

        RectTransform rectTransform = targetRawImage.rectTransform;
        
        if (rectTransform)
        {
            rectTransform.sizeDelta = new Vector2(cropWidth, cropHeight);
            
            // localScale의 음수 값을 활용하여 UI 상에서 이미지를 시각적으로 뒤집음
            float scaleX = FlipX ? -1f : 1f;
            float scaleY = FlipY ? -1f : 1f;
            rectTransform.localScale = new Vector3(scaleX, scaleY, 1f);
        }
    }
}