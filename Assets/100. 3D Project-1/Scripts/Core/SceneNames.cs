namespace YoureRight
{
    // Build Settings에 등록된 씬 이름 입니다. 씬 파일 이름을 바꾸면 여기도 같이 바꿔야 합니다.
    public static class SceneNames
    {
        public const string Menu = "00. Menu";
        public const string Result = "90. Result";

        // 플레이 순서대로 나열합니다. (튜토리얼 + 메인 3개)
        public static readonly string[] Stages =
        {
            "10. Stage Tutorial",
            "11. Stage 1",
            "12. Stage 2",
            "13. Stage 3",
        };
    }
}
