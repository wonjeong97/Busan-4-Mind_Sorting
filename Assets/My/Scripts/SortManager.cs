using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Wonjeong.Utils;

public class SortManager : MonoBehaviour
{
    public static SortManager Instance { get; private set; }

    [SerializeField] private GameObject page2;
    [SerializeField] private GameObject page3;
    [SerializeField] private GameObject page4;
    [SerializeField] private GameObject pageLast;

    [Header("Audio Settings")]
    [SerializeField] private AudioSource audioSource;

    [SerializeField] private AudioClip shutterSound;
    [SerializeField] private AudioClip completeSound;

    [Header("Sub Monitor Pages")]
    [SerializeField] private GameObject[] subPages;

    [Header("Sub Monitor Navigation")]
    [SerializeField] private Button subBackButton;

    [SerializeField] private Button subToTitleButton;

    [Header("Result Texts")]
    [SerializeField] private Text subResultText;

    [SerializeField] private Text mainResultText;

    [Header("Effect Settings")]
    [SerializeField] private CanvasGroup flashCanvasGroup;

    [Header("Sub Page 3 Settings")]
    [SerializeField] private Text subPage3Text;

    [SerializeField] private Button subPage3NextButton;

    [Header("Webcam Settings")]
    [SerializeField] private RawImage webcamDisplay;

    [Header("Result Settings")]
    [SerializeField] private RawImage[] resultImages;

    private WebCamTexture webcamTexture;
    private Texture2D capturedPhoto;
    private int currentSubIdx;
    private Coroutine flashCoroutine;
    private Coroutine buttonShowCoroutine;

    private List<Texture2D> loadedResultTextures;

    /// <summary>
    /// 싱글톤 초기화 및 버튼 리스너 연결, 씬 진입 시 이전 캡처 사진들을 미리 로드함.
    /// </summary>
    private void Awake()
    {
        if (Instance)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        loadedResultTextures = new List<Texture2D>();

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

        // 씬 진입 시점에 미리 디스크에서 사진을 읽어와 Page_Last의 RawImage들에 세팅해둠
        LoadAndDisplaySavedPhotos();
    }

    /// <summary>
    /// 씬이 파괴될 때 웹캠 하드웨어 점유를 해제하고, 메모리에 남아있는 모든 텍스처를 제거함.
    /// </summary>
    private void OnDestroy()
    {
        // 1. 웹캠이 켜진 상태로 씬을 이탈했을 경우 카메라 하드웨어 점유를 강제로 해제함
        if (webcamTexture)
        {
            if (webcamTexture.isPlaying)
            {
                webcamTexture.Stop();
            }
            Destroy(webcamTexture);
            webcamTexture = null;
        }

        // 2. 캡처본 및 결과 화면 텍스처 메모리 해제 (기존 로직)
        ClearCapturedPhoto();
        ClearLoadedResultTextures();
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

        if (audioSource && completeSound)
        {
            audioSource.PlayOneShot(completeSound);
        }

        if (flashCoroutine != null)
        {
            StopCoroutine(flashCoroutine);
        }

        flashCoroutine = StartCoroutine(FlashAndMoveToNextPage());
    }

    /// <summary>
    /// 플래시 연출을 진행한 뒤 캡처를 수행하고 2페이지로 이동함.
    /// </summary>
    private IEnumerator FlashAndMoveToNextPage()
    {
        if (flashCanvasGroup)
        {
            float duration = 0.5f;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                flashCanvasGroup.alpha = Mathf.Lerp(0f, 1f, elapsed / duration);
                yield return null;
            }

            if (audioSource && shutterSound)
            {
                audioSource.PlayOneShot(shutterSound);
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

        CaptureWebcam();

        // 캡처 완료 후 서브 페이지 2(인덱스 1)로 전환하며, SetSubPage 내부 로직에 의해 웹캠이 꺼짐
        SetSubPage(1);

        yield return new WaitForSeconds(3f);

        SetSubPage(2);

        flashCoroutine = null;
    }

    /// <summary>
    /// 뒤로가기 클릭 시 연출 중단 및 페이지 이동을 처리함.
    /// </summary>
    /// <summary>
    /// 버튼을 즉시 비활성화하고 delay 초 후 다시 활성화함.
    /// </summary>
    private IEnumerator DebounceButton(Button button, float delay = 1f)
    {
        ColorBlock cb = button.colors;
        cb.disabledColor = cb.normalColor;
        button.colors = cb;
        button.interactable = false;
        yield return new WaitForSeconds(delay);

        if (button) button.interactable = true;
    }

    private void OnSubBackClicked()
    {
        StartCoroutine(DebounceButton(subBackButton));
        StopWebcam();

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
    /// 처음으로 버튼 클릭 시 원본 캡처 데이터를 메모리에서 해제하고 타이틀 씬으로 이동함.
    /// (로컬 저장은 PrepareSubPage3에서 이미 완료됨)
    /// </summary>
    private void OnSubToTitleClicked()
    {
        StartCoroutine(DebounceButton(subToTitleButton));

        ClearCapturedPhoto();

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
    /// 인덱스에 따라 서브 페이지를 전환하며, 1페이지(인덱스 0)일 때만 웹캠을 활성화함.
    /// </summary>
    private void SetSubPage(int index)
    {
        if (subPages == null || index < 0 || index >= subPages.Length) return;

        currentSubIdx = index;

        for (int i = 0; i < subPages.Length; i++)
        {
            if (subPages[i]) subPages[i].SetActive(i == index);
        }

        // 페이지 1 진입 시 웹캠 켜기, 이탈 시 끄기
        if (index == 0)
        {
            PlayWebcam();
        }
        else
        {
            StopWebcam();
        }

        if (index == 2)
        {
            PrepareSubPage3();
        }

        if (subBackButton)
        {
            subBackButton.gameObject.SetActive(index != 3);
        }
    }

    /// <summary>
    /// 서브 페이지 3 진입 시 지연 노출 코루틴을 실행하고, 10초 대기 시간 동안 
    /// 현재 사진을 저장한 뒤 최신 상태의 결과 이미지를 화면에 갱신함.
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

        // 1. 현재 세션의 사진을 디스크에 저장함 (오래된 파일 삭제 포함)
        SavePhotoToLocal();

        // 2. 저장이 완료된 직후 디스크를 다시 읽어와 Page_Last에 매핑함 (현재 세션 포함)
        LoadAndDisplaySavedPhotos();
    }

    /// <summary>
    /// 웹캠 텍스처를 초기화하고 재생함.
    /// </summary>
    private void PlayWebcam()
    {
        if (!webcamTexture)
        {
            webcamTexture = new WebCamTexture();
            if (webcamDisplay)
            {
                webcamDisplay.texture = webcamTexture;
            }
            else
            {
                Debug.LogWarning("SortManager: 웹캠 화면을 띄울 RawImage(webcamDisplay)가 할당되지 않았습니다.");
            }
        }

        if (!webcamTexture.isPlaying)
        {
            webcamTexture.Play();
        }
    }

    /// <summary>
    /// 웹캠 재생을 정지하여 카메라 점유를 해제함.
    /// </summary>
    private void StopWebcam()
    {
        if (webcamTexture)
        {
            if (webcamTexture.isPlaying)
            {
                webcamTexture.Stop();
            }
        }
    }

    /// <summary>
    /// 현재 송출 중인 웹캠 화면의 픽셀 데이터를 추출하고, 설정에 따라 픽셀을 반전시킨 후 Texture2D로 복사함.
    /// 사진 촬영 시 오디오 소스와 클립이 할당되어 있다면 셔터 효과음을 재생함.
    /// </summary>
    private void CaptureWebcam()
    {
        if (!webcamTexture) return;

        if (!webcamTexture.isPlaying)
        {
            Debug.LogWarning("SortManager: 웹캠이 실행 중이 아니어서 캡처할 수 없습니다.");
            return;
        }

        ClearCapturedPhoto();

        int sourceWidth = webcamTexture.width;
        int sourceHeight = webcamTexture.height;

        Rect uvRect = webcamDisplay.uvRect;

        int startX = Mathf.FloorToInt(uvRect.x * sourceWidth);
        int startY = Mathf.FloorToInt(uvRect.y * sourceHeight);
        int cropWidth = Mathf.FloorToInt(uvRect.width * sourceWidth);
        int cropHeight = Mathf.FloorToInt(uvRect.height * sourceHeight);

        startX = Mathf.Clamp(startX, 0, sourceWidth);
        startY = Mathf.Clamp(startY, 0, sourceHeight);
        cropWidth = Mathf.Clamp(cropWidth, 1, sourceWidth - startX);
        cropHeight = Mathf.Clamp(cropHeight, 1, sourceHeight - startY);

        capturedPhoto = new Texture2D(cropWidth, cropHeight);
        Color[] pixels = webcamTexture.GetPixels(startX, startY, cropWidth, cropHeight);

        if (GameManager.Instance && GameManager.Instance.WebcamConfig != null)
        {
            bool flipX = GameManager.Instance.WebcamConfig.FlipX;
            bool flipY = GameManager.Instance.WebcamConfig.FlipY;

            if (flipX || flipY)
            {
                pixels = FlipPixelArray(pixels, cropWidth, cropHeight, flipX, flipY);
            }
        }

        capturedPhoto.SetPixels(pixels);
        capturedPhoto.Apply();
    }

    /// <summary>
    /// Color 배열의 좌표를 수학적으로 재계산하여 좌우 또는 상하로 완벽하게 뒤집어진 새로운 배열을 반환함.
    /// </summary>
    private Color[] FlipPixelArray(Color[] original, int width, int height, bool flipX, bool flipY)
    {
        Color[] flipped = new Color[original.Length];

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                // 반전 여부에 따라 목적지 X, Y 좌표를 반대로 계산함
                int destX = flipX ? (width - 1 - x) : x;
                int destY = flipY ? (height - 1 - y) : y;

                // 1차원 배열 인덱스 공식 (y * width + x)
                flipped[destY * width + destX] = original[y * width + x];
            }
        }

        return flipped;
    }

    /// <summary>
    /// 캡처된 사진을 PNG로 저장하고, 파일이 6개를 초과하면 오래된 파일부터 삭제함.
    /// (밀리초 단위 저장 및 파일명 기반 정렬로 안정성 강화)
    /// </summary>
    private void SavePhotoToLocal()
    {
        if (!capturedPhoto) return;

        byte[] bytes = capturedPhoto.EncodeToPNG();

        string directoryPath = Path.Combine(Application.dataPath, "CapturedPhotos");

        if (!Directory.Exists(directoryPath))
        {
            Directory.CreateDirectory(directoryPath);
        }

        // 1. 충돌 방지를 위해 _fff(밀리초 3자리)를 추가하여 절대 중복되지 않도록 변경함
        string fileName = string.Format("Photo_{0}.png", System.DateTime.Now.ToString("yyyyMMdd_HHmmss_fff"));
        string filePath = Path.Combine(directoryPath, fileName);

        try
        {
            File.WriteAllBytes(filePath, bytes);
            Debug.LogFormat("SortManager: 캡처된 사진이 저장되었습니다. 경로: {0}", filePath);
        }
        catch (System.Exception e)
        {
            Debug.LogErrorFormat("SortManager: 사진 저장 중 오류 발생 (디스크 락 의심): {0}", e.Message);
            return;
        }

        DirectoryInfo dirInfo = new DirectoryInfo(directoryPath);
        FileInfo[] files = dirInfo.GetFiles("*.png");

        if (files.Length > 6)
        {
            // 2. OS의 CreationTime 대신 절대적인 '파일명(날짜시간 문자열)' 기준으로 오름차순(오래된 순) 정렬함
            System.Array.Sort(files, (FileInfo a, FileInfo b) => a.Name.CompareTo(b.Name));

            int deleteCount = files.Length - 6;
            for (int i = 0; i < deleteCount; i++)
            {
                try
                {
                    files[i].Delete();
                    Debug.LogFormat("SortManager: 오래된 사진 삭제 완료. 파일명: {0}", files[i].Name);
                }
                catch (System.Exception e)
                {
                    Debug.LogErrorFormat("SortManager: 사진 삭제 실패: {0}", e.Message);
                }
            }
        }
    }

    /// <summary>
    /// 디스크에 저장된 이전 사진들을 읽어와 파일명 기준 내림차순(최신 순)으로 정렬한 뒤 UI에 매핑함.
    /// </summary>
    private void LoadAndDisplaySavedPhotos()
    {
        if (resultImages == null) return;

        ClearLoadedResultTextures();

        string directoryPath = Path.Combine(Application.dataPath, "CapturedPhotos");
        if (!Directory.Exists(directoryPath)) return;

        DirectoryInfo dirInfo = new DirectoryInfo(directoryPath);
        FileInfo[] files = dirInfo.GetFiles("*.png");

        // 3. 파일명을 기준으로 내림차순 정렬하여 최신 파일이 인덱스 0번(첫 번째)으로 오도록 보장함
        System.Array.Sort(files, (FileInfo a, FileInfo b) => b.Name.CompareTo(a.Name));

        for (int i = 0; i < resultImages.Length; i++)
        {
            if (resultImages[i])
            {
                if (i < files.Length)
                {
                    // 4. 백신 프로그램의 파일 검사 등으로 인한 I/O 충돌을 방어하기 위한 Try-Catch
                    try
                    {
                        byte[] fileData = File.ReadAllBytes(files[i].FullName);
                        Texture2D tex = new Texture2D(2, 2);

                        if (tex.LoadImage(fileData))
                        {
                            resultImages[i].texture = tex;
                            resultImages[i].gameObject.SetActive(true);
                            loadedResultTextures.Add(tex);
                        }
                        else
                        {
                            Destroy(tex);
                            resultImages[i].gameObject.SetActive(false);
                        }
                    }
                    catch (System.Exception e)
                    {
                        Debug.LogWarningFormat("SortManager: 사진 로드 실패 (파일 락 발생 가능성): {0}", e.Message);
                        resultImages[i].gameObject.SetActive(false);
                    }
                }
                else
                {
                    resultImages[i].gameObject.SetActive(false);
                }
            }
        }
    }

    /// <summary>
    /// 웹캠에서 캡처하여 대기 중인 원본 텍스처를 파괴함.
    /// </summary>
    private void ClearCapturedPhoto()
    {
        if (capturedPhoto)
        {
            Destroy(capturedPhoto);
        }
    }

    /// <summary>
    /// 디스크에서 불러와 결과 화면에 매핑했던 텍스처들의 메모리를 일괄 해제함.
    /// </summary>
    private void ClearLoadedResultTextures()
    {
        if (loadedResultTextures != null)
        {
            for (int i = 0; i < loadedResultTextures.Count; i++)
            {
                Texture2D tex = loadedResultTextures[i];
                if (tex)
                {
                    Destroy(tex);
                }
            }

            loadedResultTextures.Clear();
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
        StartCoroutine(DebounceButton(subPage3NextButton));

        if (audioSource && completeSound)
        {
            audioSource.PlayOneShot(completeSound);
        }

        SetSubPage(3);

        if (page2) page2.SetActive(false);
        if (page3) page3.SetActive(false);
        if (page4) page4.SetActive(false);

        if (pageLast && mainResultText)
        {
            pageLast.SetActive(true);
            mainResultText.gameObject.SetActive(false);
        }
    }
}