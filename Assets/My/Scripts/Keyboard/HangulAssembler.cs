using System.Text;

public class HangulAssembler
{
    private const int HangulBase = 0xAC00;
    private const int ChoBase = 21 * 28;
    private const int JungBase = 28;

    private readonly string choTable = "ㄱㄲㄴㄷㄸㄹㅁㅂㅃㅅㅆㅇㅈㅉㅊㅋㅌㅍㅎ";
    private readonly string jungTable = "ㅏㅐㅑㅒㅓㅔㅕㅖㅗㅘㅙㅚㅛㅜㅝㅞㅟㅠㅡㅢㅣ";
    private readonly string jongTable = " ㄱㄲㄳㄴㄵㄶㄷㄹㄺㄻㄼㄽㄾㄿㅀㅁㅂㅄㅅㅆㅇㅈㅊㅋㅌㅍㅎ";

    private StringBuilder fullText;
    
    public int maxLength = 15;

    public HangulAssembler()
    {
        fullText = new StringBuilder();
    }

    /// <summary>
    /// 입력된 자모의 상태를 분석하여 유니코드 공식에 따라 한글을 조합함.
    /// </summary>
    public string AddChar(string inputChar)
    {
        if (string.IsNullOrEmpty(inputChar)) return fullText.ToString();

        char input = inputChar[0];
        int inputChoIdx = choTable.IndexOf(input);
        int inputJungIdx = jungTable.IndexOf(input);
        int inputJongIdx = jongTable.IndexOf(input);

        // 1. 첫 글자 입력 시
        if (fullText.Length == 0)
        {
            fullText.Append(input);
            return fullText.ToString();
        }

        char lastChar = fullText[fullText.Length - 1];
        int lastChoIdx = choTable.IndexOf(lastChar);

        // 2. 단독 초성 + 중성 결합 (글자 수가 늘어나지 않으므로 제한 없음)
        if (lastChoIdx >= 0 && inputJungIdx >= 0)
        {
            char combined = (char)(HangulBase + (lastChoIdx * ChoBase) + (inputJungIdx * JungBase));
            fullText[fullText.Length - 1] = combined;
            return fullText.ToString();
        }

        // 3. 이미 조합된 한글 + 새로운 자모 입력
        if (lastChar >= HangulBase && lastChar <= 0xD7A3)
        {
            int code = lastChar - HangulBase;
            int cho = code / ChoBase;
            int jung = (code % ChoBase) / JungBase;
            int jong = code % JungBase;

            // 3-1. 받침 결합 (글자 수가 늘어나지 않으므로 제한 없음)
            if (jong == 0 && inputJongIdx > 0)
            {
                char combined = (char)(lastChar + inputJongIdx);
                fullText[fullText.Length - 1] = combined;
                return fullText.ToString();
            }

            // 3-2. 종성 분리 후 새로운 모음과 결합 (글자 수가 늘어나므로 검사 필요)
            if (jong > 0 && inputJungIdx >= 0)
            {
                if (fullText.Length >= maxLength) return fullText.ToString(); // 초과 방지

                char jongChar = jongTable[jong];
                int newChoIdx = choTable.IndexOf(jongChar);

                if (newChoIdx >= 0)
                {
                    fullText[fullText.Length - 1] = (char)(HangulBase + (cho * ChoBase) + (jung * JungBase));
                    char newChar = (char)(HangulBase + (newChoIdx * ChoBase) + (inputJungIdx * JungBase));
                    fullText.Append(newChar);
                    return fullText.ToString();
                }
            }
        }

        // 4. 완전히 새로운 글자로 이어 붙임 (글자 수가 늘어나므로 검사 필요)
        if (fullText.Length >= maxLength) return fullText.ToString(); // 초과 방지

        fullText.Append(input);
        return fullText.ToString();
    }

    /// <summary>
    /// 공백을 추가함.
    /// </summary>
    public string AddSpace()
    {
        // 띄어쓰기도 한 글자로 취급하므로 검사 필요
        if (fullText.Length >= maxLength) return fullText.ToString();
        
        fullText.Append(" ");
        return fullText.ToString();
    }

    /// <summary>
    /// 마지막 글자를 제거함.
    /// </summary>
    public string RemoveChar()
    {
        if (fullText.Length > 0)
        {
            // 백스페이스 시 자모 단위 분리(홍 -> 호)가 아닌 통글자 삭제로 동작함
            fullText.Remove(fullText.Length - 1, 1);
        }
        return fullText.ToString();
    }
    
    /// <summary>
    /// 조합 중인 모든 문자열 데이터를 초기화함.
    /// </summary>
    public void Clear()
    {
        if (fullText != null)
        {
            fullText.Clear();
        }
    }
}