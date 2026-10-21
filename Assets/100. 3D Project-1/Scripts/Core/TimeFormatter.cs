using UnityEngine;

namespace YoureRight
{
    // 초 단위 시간을 "분:초.소수점 두 자리"(00:00.00) 형식의 문자열로 바꿔줍니다.
    // HUD 스톱워치와 결과 화면에서 같이 사용합니다.
    public static class TimeFormatter
    {
        public static string Format(float seconds)
        {
            // 소수점 아래를 반올림하면 59.999초가 "00:60.00"처럼 보일 수 있어서
            // 1/100초 단위 정수로 내림한 뒤 나눕니다.
            int totalHundredths = Mathf.FloorToInt(Mathf.Max(0f, seconds) * 100f);
            int minutes = totalHundredths / 6000;
            int secs = totalHundredths / 100 % 60;
            int hundredths = totalHundredths % 100;

            return $"{minutes:00}:{secs:00}.{hundredths:00}";
        }
    }
}
