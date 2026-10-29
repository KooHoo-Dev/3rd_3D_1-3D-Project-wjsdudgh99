using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace YoureRight
{
    // 2-2 기차역용 스타일라이즈드 소품(벤치, 쓰레기통, LED 안내기 등)을 Cube·Cylinder·Sphere 조각으로 조립해 프리팹으로 만드는 에디터 도구입니다.
    // Tools → You're Right → Build Station Props
    //
    // 기준점은 밑면 가운데(y 0)이고 앞면은 +X(선로 쪽)를 봅니다. 플랫폼 윗면(y 1.2)에 바로 놓으면 됩니다.
    // 예외: 천장에 매다는 소품(Exit Sign Hanging, LED Departure Board, Digital Clock Hanging)은 매다는 점(맨 위)이 기준점이고 아래로 늘어집니다.
    // 앞에서 볼 때 오른쪽이 +Z입니다(글자·숫자는 -Z에서 +Z로 읽힘).
    //
    // 모든 소품은 루트에 Collider 없이 부품마다 MeshCollider를 붙입니다(Default 레이어라 클릭하면 오답).
    // 루트에 상자를 씌우면 상자 겉면에 먼저 맞아서 자판기 버튼처럼 부품 하나를 정답으로 둘 수 없기 때문입니다.
    // 소품 통째로 정답일 때는 맞은 부품의 부모에서 AnswerTarget을 찾으므로 루트에 AnswerTarget을 붙이면 됩니다.
    // 머티리얼은 모두 Emission을 켜고 검정으로 둡니다(정답 반짝임용). LED 글자와 자판기 진열창만 스스로 빛나는 색입니다.
    // 색 변형용 머티리얼(M_Prop_*)도 함께 만들어 두므로, 씬에서 부품 머티리얼만 바꿔 미끼를 만들 수 있습니다.
    // 다시 실행하면 같은 이름의 머티리얼·프리팹을 덮어씁니다(씬에 놓인 인스턴스는 그대로 따라 바뀜).
    public static class StationPropsBuilder
    {
        private const string MenuPath = "Tools/You're Right/Build Station Props";
        private const string Title = "Build Station Props";
        private const string MaterialFolder = "Assets/100. 3D Project-1/Materials/Station";
        private const string PrefabFolder = "Assets/100. 3D Project-1/Prefabs/Station/Props";

        private const float Inlay = 0.01f;   // 글자·숫자 조각의 두께 (앞면에서 튀어나온 만큼)

        private static Mesh cube;
        private static Mesh cylinder;
        private static Mesh sphere;

        [MenuItem(MenuPath)]
        private static void Build()
        {
            if (EditorApplication.isPlaying)
            {
                EditorUtility.DisplayDialog(Title, "Play 모드에서는 만들 수 없습니다.", "OK");
                return;
            }

            if (AssetDatabase.IsValidFolder(PrefabFolder) &&
                EditorUtility.DisplayDialog(Title, "이미 만든 소품이 있습니다. 머티리얼과 프리팹을 덮어쓸까요?\n(프리팹에서 직접 고친 값은 사라집니다)", "덮어쓰기", "취소") == false)
            {
                return;
            }

            EnsureFolder(MaterialFolder);
            EnsureFolder(PrefabFolder);
            cube = Resources.GetBuiltinResource<Mesh>("Cube.fbx");
            cylinder = Resources.GetBuiltinResource<Mesh>("Cylinder.fbx");
            sphere = Resources.GetBuiltinResource<Mesh>("Sphere.fbx");

            // 현재 씬을 더럽히지 않도록 미리보기 씬에서 조립한 뒤 프리팹으로 저장합니다.
            Scene preview = EditorSceneManager.NewPreviewScene();
            try
            {
                var m = new Materials();
                Save(BuildModernBench(m), preview);
                Save(BuildRuralBench(m), preview);
                Save(BuildModernTrashBin(m), preview);
                Save(BuildRuralTrashBin(m), preview);
                Save(BuildLedBoard(m), preview);
                Save(BuildModernNameSign(m), preview);
                Save(BuildRuralNameSign(m), preview);
                Save(BuildVendingMachine(m), preview);
                Save(BuildTicketGate(m), preview);
                Save(BuildDigitalClock(m), preview);
                Save(BuildPlatformMirror(m), preview);
                Save(BuildStopMarker(m), preview);
                Save(BuildExitSign(m), preview);
                Save(BuildFireExtinguisherBox(m), preview);
                Save(BuildPostBox(m), preview);
            }
            finally
            {
                EditorSceneManager.ClosePreviewScene(preview);
            }

            AssetDatabase.SaveAssets();
            Debug.Log($"[StationPropsBuilder] 소품 생성 완료: {PrefabFolder}");
            EditorGUIUtility.PingObject(AssetDatabase.LoadAssetAtPath<Object>(PrefabFolder));
        }

        #region 머티리얼

        // 소품에서 쓰는 단색 머티리얼 모음. 프리팹에 쓰지 않는 색도 변형용으로 함께 만듭니다.
        private class Materials
        {
            // 기본
            public readonly Material Metal = Prop("M_Prop_Metal", "8C949C", 0.4f, 0.3f);
            public readonly Material Dark = Prop("M_Prop_Dark", "2E3236");
            public readonly Material Black = Prop("M_Prop_Black", "1C1E21");
            public readonly Material White = Prop("M_Prop_White", "F2F2EE");
            public readonly Material BodyLight = Prop("M_Prop_Body_Light", "D9DDE1");
            public readonly Material Mirror = Prop("M_Prop_Mirror", "C9E3F0", 0.95f, 0.8f);

            // 색 (부품 색 바꾸기용으로도 씀)
            public readonly Material Red = Prop("M_Prop_Red", "D2372F");
            public readonly Material Orange = Prop("M_Prop_Orange", "F08A3C");
            public readonly Material Yellow = Prop("M_Prop_Yellow", "F2C230");
            public readonly Material Green = Prop("M_Prop_Green", "2E9E5B");
            public readonly Material Teal = Prop("M_Prop_Teal", "2BA59B");
            public readonly Material Blue = Prop("M_Prop_Blue", "2F6FD0");
            public readonly Material Gray = Prop("M_Prop_Gray", "9AA0A6");
            public readonly Material Brown = Prop("M_Prop_Brown", "7A4A2A");

            // 나무·쓰레기통
            public readonly Material Wood = Prop("M_Prop_Wood", "9A6A45");
            public readonly Material WoodLight = Prop("M_Prop_Wood_Light", "C49A6C");
            public readonly Material WoodDark = Prop("M_Prop_Wood_Dark", "6B4329");
            public readonly Material TrashGreen = Prop("M_Prop_Trash_Green", "4E7A4F");
            public readonly Material TrashRust = Prop("M_Prop_Trash_Rust", "B5652E");

            // 스스로 빛나는 색 (LED_Red·LED_Off는 변형용)
            public readonly Material LedOrange = Glow("M_Prop_LED_Orange", "FF8A1F", 1.2f);
            public readonly Material LedGreen = Glow("M_Prop_LED_Green", "3DDC6A", 1.2f);
            public readonly Material LedRed = Glow("M_Prop_LED_Red", "FF3B30", 1.2f);
            public readonly Material LedWhite = Glow("M_Prop_LED_White", "F0F4FF", 1.2f);
            public readonly Material LedBlue = Glow("M_Prop_LED_Blue", "3FA9FF", 1.2f);
            public readonly Material LedOff = Prop("M_Prop_LED_Off", "3A2E24");
            public readonly Material VendingWindow = Glow("M_Prop_Vending_Window", "E8F4FF", 1f);
        }

        // 무광 단색 + Emission 켬(검정)
        private static Material Prop(string name, string hex, float smoothness = 0.2f, float metallic = 0f)
        {
            Material material = LoadOrCreate(name);
            material.SetColor("_BaseColor", Hex(hex));
            material.SetFloat("_Smoothness", smoothness);
            material.SetFloat("_Metallic", metallic);
            SetEmission(material, Color.black);
            EditorUtility.SetDirty(material);
            return material;
        }

        // 스스로 빛나는 색 (Emission = 색 × 세기)
        private static Material Glow(string name, string hex, float intensity)
        {
            Material material = LoadOrCreate(name);
            Color color = Hex(hex);
            material.SetColor("_BaseColor", color);
            material.SetFloat("_Smoothness", 0.5f);
            material.SetFloat("_Metallic", 0f);
            SetEmission(material, color * intensity);
            EditorUtility.SetDirty(material);
            return material;
        }

        // 검정이어도 RealtimeEmissive로 둡니다. EmissiveIsBlack이면 URP가 머티리얼을 저장할 때 _EMISSION을 꺼 버립니다.
        private static void SetEmission(Material material, Color color)
        {
            material.EnableKeyword("_EMISSION");
            material.SetColor("_EmissionColor", color);
            material.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
        }

        private static Material LoadOrCreate(string name)
        {
            string path = $"{MaterialFolder}/{name}.mat";
            Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material != null) return material;

            material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            AssetDatabase.CreateAsset(material, path);
            return material;
        }

        private static Color Hex(string hex)
        {
            ColorUtility.TryParseHtmlString("#" + hex, out Color color);
            return color;
        }

        #endregion

        #region 벤치·쓰레기통

        // 최신 역 벤치 (길이 1.8): 금속 다리 2개 + 가로 보 + 개별 의자 3개
        private static GameObject BuildModernBench(Materials m)
        {
            GameObject root = Root("Bench Modern");

            for (int side = -1; side <= 1; side += 2)
            {
                GameObject leg = Group(root, side < 0 ? "Leg Left" : "Leg Right");
                float z = side * 0.8f;
                Box(leg, "Upright", new Vector3(-0.05f, 0f, z - 0.03f), new Vector3(0.05f, 0.42f, z + 0.03f), m.Metal);
                Box(leg, "Foot", new Vector3(-0.25f, 0f, z - 0.03f), new Vector3(0.25f, 0.04f, z + 0.03f), m.Metal);
            }
            Box(root, "Beam", new Vector3(-0.04f, 0.36f, -0.85f), new Vector3(0.04f, 0.42f, 0.85f), m.Metal);

            for (int i = 0; i < 3; i++)
            {
                float z = (i - 1) * 0.6f;
                GameObject seat = Group(root, $"Seat {i + 1}");
                Box(seat, "Pan", new Vector3(-0.2f, 0.42f, z - 0.25f), new Vector3(0.22f, 0.47f, z + 0.25f), m.Teal);
                Box(seat, "Back", new Vector3(-0.24f, 0.5f, z - 0.25f), new Vector3(-0.2f, 0.85f, z + 0.25f), m.Teal);
                Box(seat, "Stem", new Vector3(-0.23f, 0.42f, z - 0.05f), new Vector3(-0.21f, 0.5f, z + 0.05f), m.Metal);
            }
            return root;
        }

        // 시골 간이역 벤치 (길이 1.6): 나무 다리 4개 + 앉는 판자 3줄 + 등받이 판자 2줄
        private static GameObject BuildRuralBench(Materials m)
        {
            GameObject root = Root("Bench Rural");

            for (int ix = 0; ix < 2; ix++)
            {
                for (int iz = 0; iz < 2; iz++)
                {
                    float x = ix == 0 ? -0.17f : 0.17f;
                    float z = iz == 0 ? -0.69f : 0.69f;
                    Box(root, $"Leg {ix * 2 + iz + 1}", new Vector3(x - 0.03f, 0f, z - 0.03f), new Vector3(x + 0.03f, 0.42f, z + 0.03f), m.WoodDark);
                }
            }

            float[] slatX = { -0.14f, 0f, 0.14f };
            for (int i = 0; i < slatX.Length; i++)
            {
                Box(root, $"Seat Slat {i + 1}", new Vector3(slatX[i] - 0.06f, 0.42f, -0.8f), new Vector3(slatX[i] + 0.06f, 0.46f, 0.8f), m.Wood);
            }

            for (int side = -1; side <= 1; side += 2)
            {
                float z = side * 0.69f;
                Box(root, side < 0 ? "Back Post Left" : "Back Post Right", new Vector3(-0.22f, 0.42f, z - 0.03f), new Vector3(-0.18f, 0.85f, z + 0.03f), m.WoodDark);
            }
            Box(root, "Back Slat 1", new Vector3(-0.24f, 0.55f, -0.8f), new Vector3(-0.21f, 0.63f, 0.8f), m.Wood);
            Box(root, "Back Slat 2", new Vector3(-0.24f, 0.72f, -0.8f), new Vector3(-0.21f, 0.8f, 0.8f), m.Wood);
            return root;
        }

        // 최신 역 분리수거함 (길이 1.2): 캔·병·종이 3칸. 칸마다 뚜껑·라벨·투입구를 묶어 색·순서를 바꾸기 쉽게 합니다.
        private static GameObject BuildModernTrashBin(Materials m)
        {
            GameObject root = Root("Trash Bin Modern");
            Box(root, "Body", new Vector3(-0.2f, 0.05f, -0.6f), new Vector3(0.2f, 0.95f, 0.6f), m.BodyLight);
            Box(root, "Base", new Vector3(-0.21f, 0f, -0.61f), new Vector3(0.21f, 0.05f, 0.61f), m.Dark);

            string[] names = { "Section Cans", "Section Bottles", "Section Paper" };
            Material[] colors = { m.Blue, m.Green, m.Gray };
            for (int i = 0; i < 3; i++)
            {
                float z = (i - 1) * 0.4f;
                GameObject section = Group(root, names[i]);
                Box(section, "Lid", new Vector3(-0.2f, 0.95f, z - 0.19f), new Vector3(0.2f, 1f, z + 0.19f), colors[i]);
                Box(section, "Label", new Vector3(0.2f, 0.6f, z - 0.17f), new Vector3(0.2f + Inlay, 0.7f, z + 0.17f), colors[i]);
                if (i < 2) Cylinder(section, "Opening", new Vector3(0.2f, 0.82f, z), i == 0 ? 0.07f : 0.06f, Inlay * 2f, 'x', m.Black);
                else Box(section, "Opening", new Vector3(0.2f, 0.8f, z - 0.12f), new Vector3(0.2f + Inlay, 0.84f, z + 0.12f), m.Black);
            }

            for (int side = -1; side <= 1; side += 2)
            {
                float z = side * 0.2f;
                Box(root, side < 0 ? "Divider Left" : "Divider Right", new Vector3(0.2f, 0.05f, z - 0.01f), new Vector3(0.2f + Inlay, 0.95f, z + 0.01f), m.Dark);
            }
            return root;
        }

        // 시골 간이역 쓰레기통: 초록 원통 + 띠 + 뚜껑 + 손잡이
        private static GameObject BuildRuralTrashBin(Materials m)
        {
            GameObject root = Root("Trash Bin Rural");
            Cylinder(root, "Body", new Vector3(0f, 0.4f, 0f), 0.22f, 0.8f, 'y', m.TrashGreen);
            Cylinder(root, "Band", new Vector3(0f, 0.5f, 0f), 0.225f, 0.05f, 'y', m.Dark);
            GameObject lid = Group(root, "Lid");
            Cylinder(lid, "Cover", new Vector3(0f, 0.83f, 0f), 0.24f, 0.06f, 'y', m.Dark);
            Cylinder(lid, "Knob", new Vector3(0f, 0.88f, 0f), 0.04f, 0.04f, 'y', m.Dark);
            return root;
        }

        #endregion

        #region 안내판

        // LED 발차 안내기 (폭 2): 천장에 매다는 검은 패널. 기준점은 매다는 점(맨 위).
        // 1·3줄 주황, 2줄 초록. 시각(7세그먼트) + 행선지(한자 모양) + 량수. 1줄에는 행선지와 량수 사이에 빨간 "急行"(종별)
        // 앞뒤 양면입니다. 뒷면(Back)은 디지털 시계처럼 같은 글자를 Y 180° 돌린 그룹에 다시 만듭니다(면마다 따로 고칠 수 있음).
        private static GameObject BuildLedBoard(Materials m)
        {
            GameObject root = Root("LED Departure Board");
            const float fx = 0.06f;

            AddHangingRods(root, m, 0.8f, 0.4f);
            Box(root, "Frame Top", new Vector3(-0.07f, -0.45f, -1.02f), new Vector3(0.07f, -0.4f, 1.02f), m.Dark);
            Box(root, "Panel", new Vector3(-fx, -1.15f, -1f), new Vector3(fx, -0.45f, 1f), m.Black);
            Box(root, "Frame Bottom", new Vector3(-0.07f, -1.2f, -1.02f), new Vector3(0.07f, -1.15f, 1.02f), m.Dark);

            AddLedRows(Group(root, "Front"), fx, m);
            AddLedRows(Group(root, "Back", Vector3.zero, new Vector3(0f, 180f, 0f)), fx, m);
            return root;
        }

        // LED 안내기 한 면의 글자 3줄
        private static void AddLedRows(GameObject root, float fx, Materials m)
        {
            GameObject row1 = Group(root, "Row 1");
            Digits(row1, "Time", fx, -0.57f, -0.92f, "10:42", 0.12f, m.LedOrange);
            Word(row1, "Destination", fx, -0.57f, -0.3f, "HMK", 0.12f, m.LedOrange);   // 행선지는 두 줄 모두 역명판(Station Name Sign Modern)과 같은 글자
            Word(row1, "Type", fx, -0.57f, 0.2f, "QG", 0.12f, m.LedRed);
            Digits(row1, "Cars", fx, -0.57f, 0.6f, "3", 0.12f, m.LedOrange);
            Glyph(row1, "Cars Unit", fx, -0.57f, 0.85f, 0.12f, 'H', m.LedOrange);

            GameObject row2 = Group(root, "Row 2");
            Digits(row2, "Time", fx, -0.75f, -0.92f, "10:55", 0.12f, m.LedGreen);
            Word(row2, "Destination", fx, -0.75f, -0.3f, "HMK", 0.12f, m.LedGreen);
            Digits(row2, "Cars", fx, -0.75f, 0.6f, "3", 0.12f, m.LedGreen);
            Glyph(row2, "Cars Unit", fx, -0.75f, 0.85f, 0.12f, 'H', m.LedGreen);

            // 3줄: 흐르는 안내 문구 (막대 4개)
            GameObject row3 = Group(root, "Row 3");
            float[,] bars = { { -0.92f, -0.6f }, { -0.5f, -0.15f }, { -0.05f, 0.4f }, { 0.5f, 0.92f } };
            for (int i = 0; i < bars.GetLength(0); i++)
            {
                Box(row3, $"Text {i + 1}", new Vector3(fx, -0.975f, bars[i, 0]), new Vector3(fx + Inlay, -0.925f, bars[i, 1]), m.LedOrange);
            }
        }

        // 천장에 매다는 디지털 시계 (폭 1): 검은 패널 앞뒤에 흰색 LED 시각. 기준점은 매다는 점(맨 위).
        // 뒷면은 앞면과 같은 글자 묶음을 Y 180° 돌린 그룹에 다시 만들어서, 뒤에서 봐도 바르게 읽힙니다.
        private static GameObject BuildDigitalClock(Materials m)
        {
            GameObject root = Root("Digital Clock Hanging");
            const float fx = 0.06f;
            const float cy = -0.585f;
            const float h = 0.2f;
            const string time = "10:42";

            AddHangingRods(root, m, 0.3f, 0.4f);
            Box(root, "Frame Top", new Vector3(-0.07f, -0.42f, -0.52f), new Vector3(0.07f, -0.4f, 0.52f), m.Dark);
            Box(root, "Panel", new Vector3(-fx, -0.75f, -0.5f), new Vector3(fx, -0.42f, 0.5f), m.Black);
            Box(root, "Frame Bottom", new Vector3(-0.07f, -0.77f, -0.52f), new Vector3(0.07f, -0.75f, 0.52f), m.Dark);

            float z0 = -TextWidth(time, h) / 2f;
            Digits(root, "Time Front", fx, cy, z0, time, h, m.LedWhite);
            GameObject back = Group(root, "Back", Vector3.zero, new Vector3(0f, 180f, 0f));
            Digits(back, "Time Back", fx, cy, z0, time, h, m.LedWhite);
            return root;
        }

        // 천장에서 내려오는 봉 2개 (z ±spread, 길이 length)
        private static void AddHangingRods(GameObject root, Materials m, float spread, float length)
        {
            for (int side = -1; side <= 1; side += 2)
            {
                float z = side * spread;
                Box(root, side < 0 ? "Rod Left" : "Rod Right", new Vector3(-0.01f, -length, z - 0.01f), new Vector3(0.01f, 0f, z + 0.01f), m.Metal);
            }
        }

        // 최신 역 역명판 (폭 1.6): 흰 판 + 큰 역 이름 + 로마자 줄 + 아래쪽 노선 색 띠(양 끝에 이웃 역 이름)
        private static GameObject BuildModernNameSign(Materials m)
        {
            GameObject root = Root("Station Name Sign Modern");
            const float fx = 0.04f;

            for (int side = -1; side <= 1; side += 2)
            {
                float z = side * 0.7f;
                Box(root, side < 0 ? "Leg Left" : "Leg Right", new Vector3(-0.03f, 0f, z - 0.03f), new Vector3(0.03f, 1.3f, z + 0.03f), m.Metal);
            }
            Box(root, "Board", new Vector3(-fx, 1.3f, -0.8f), new Vector3(fx, 1.9f, 0.8f), m.White);

            Word(root, "Name", fx, 1.7f, -0.3f, "HMK", 0.22f, m.Black, 0.3f);
            Box(root, "Romaji", new Vector3(fx, 1.53f, -0.35f), new Vector3(fx + Inlay, 1.56f, 0.35f), m.Black);

            GameObject band = Group(root, "Line Band");
            Box(band, "Band", new Vector3(fx, 1.36f, -0.8f), new Vector3(fx + Inlay * 0.5f, 1.46f, 0.8f), m.Green);
            Box(band, "Prev Station", new Vector3(fx + Inlay * 0.5f, 1.4f, -0.75f), new Vector3(fx + Inlay, 1.42f, -0.45f), m.White);
            Box(band, "Next Station", new Vector3(fx + Inlay * 0.5f, 1.4f, 0.45f), new Vector3(fx + Inlay, 1.42f, 0.75f), m.White);
            return root;
        }

        // 시골 간이역 역명판 (폭 1.4): 나무 기둥 2개 + 흰 판 + 검은 역 이름 + 나무 지붕 판
        private static GameObject BuildRuralNameSign(Materials m)
        {
            GameObject root = Root("Station Name Sign Rural");
            const float fx = 0.03f;

            for (int side = -1; side <= 1; side += 2)
            {
                float z = side * 0.65f;
                Box(root, side < 0 ? "Post Left" : "Post Right", new Vector3(-0.05f, 0f, z - 0.05f), new Vector3(0.05f, 1.75f, z + 0.05f), m.WoodDark);
            }
            Box(root, "Board", new Vector3(-fx, 1.2f, -0.7f), new Vector3(fx, 1.65f, 0.7f), m.White);
            Box(root, "Roof", new Vector3(-0.08f, 1.65f, -0.75f), new Vector3(0.08f, 1.7f, 0.75f), m.WoodDark);

            Word(root, "Name", fx, 1.47f, -0.28f, "JEK", 0.2f, m.Black, 0.28f);
            Box(root, "Romaji", new Vector3(fx, 1.26f, -0.3f), new Vector3(fx + Inlay, 1.29f, 0.3f), m.Black);
            return root;
        }

        // 정지 위치 표지: 짧은 기둥 + 노란 판 + 숫자(7세그먼트)
        private static GameObject BuildStopMarker(Materials m)
        {
            GameObject root = Root("Stop Position Marker");
            const float fx = 0.015f;
            const float h = 0.22f;

            Cylinder(root, "Pole", new Vector3(0f, 0.4f, 0f), 0.025f, 0.8f, 'y', m.Metal);
            Box(root, "Board", new Vector3(-fx, 0.8f, -0.16f), new Vector3(fx, 1.12f, 0.16f), m.Yellow);
            Digits(root, "Number", fx, 0.96f, -h * 0.55f / 2f, "6", h, m.Black);
            return root;
        }

        // 천장에 매다는 출구 안내판: 기준점은 매다는 점(맨 위). 노란 판 + 위쪽 화살표 + "出口" 모양 글자
        private static GameObject BuildExitSign(Materials m)
        {
            GameObject root = Root("Exit Sign Hanging");
            const float fx = 0.04f;
            const float cy = -0.525f;

            for (int side = -1; side <= 1; side += 2)
            {
                float z = side * 0.45f;
                Box(root, side < 0 ? "Rod Left" : "Rod Right", new Vector3(-0.01f, -0.35f, z - 0.01f), new Vector3(0.01f, 0f, z + 0.01f), m.Metal);
            }
            Box(root, "Board", new Vector3(-fx, -0.7f, -0.6f), new Vector3(fx, -0.35f, 0.6f), m.Yellow);

            // 위쪽 화살표: 세로 막대 + 꼭대기(tip)에서 양옆 아래로 내려가는 머리 2개 (X축 +45°는 +Y를 +Z 쪽으로 기울임)
            GameObject arrow = Group(root, "Arrow");
            const float az = -0.4f;
            const float tip = cy + 0.13f;
            Box(arrow, "Shaft", new Vector3(fx, cy - 0.13f, az - 0.012f), new Vector3(fx + Inlay, tip - 0.005f, az + 0.012f), m.Black);
            TiltedBox(arrow, "Head Left", new Vector3(fx + Inlay / 2f, tip - 0.035f, az - 0.035f), new Vector3(Inlay, 0.1f, 0.024f), new Vector3(45f, 0f, 0f), m.Black);
            TiltedBox(arrow, "Head Right", new Vector3(fx + Inlay / 2f, tip - 0.035f, az + 0.035f), new Vector3(Inlay, 0.1f, 0.024f), new Vector3(-45f, 0f, 0f), m.Black);

            Word(root, "Text", fx, cy, 0.05f, "MK", 0.22f, m.Black, 0.28f);
            return root;
        }

        #endregion

        #region 기타 소품

        // 음료 자판기: 빨간 몸체 + 빛나는 진열창 + 음료 3줄 × 5개(줄마다 묶음) + 버튼 + 동전 패널 + 꺼내는 곳
        private static GameObject BuildVendingMachine(Materials m)
        {
            GameObject root = Root("Vending Machine");
            const float fx = 0.35f;

            Box(root, "Body", new Vector3(-fx, 0.08f, -0.5f), new Vector3(fx, 1.8f, 0.5f), m.Red);
            Box(root, "Kick Plate", new Vector3(-fx, 0f, -0.5f), new Vector3(fx, 0.08f, 0.5f), m.Dark);
            Box(root, "Top Cap", new Vector3(-0.36f, 1.8f, -0.51f), new Vector3(0.36f, 1.85f, 0.51f), m.Dark);
            Box(root, "Window", new Vector3(fx, 1f, -0.42f), new Vector3(fx + Inlay, 1.65f, 0.42f), m.VendingWindow);

            Material[][] drinkColors =
            {
                new[] { m.Red, m.Yellow, m.Green, m.Blue, m.White },
                new[] { m.Brown, m.Orange, m.Teal, m.Red, m.Yellow },
                new[] { m.Blue, m.White, m.Green, m.Brown, m.Orange },
            };
            float[] rowY = { 1.55f, 1.33f, 1.11f };
            for (int r = 0; r < rowY.Length; r++)
            {
                GameObject row = Group(root, $"Drinks Row {r + 1}");
                for (int c = 0; c < 5; c++)
                {
                    float z = (c - 2) * 0.16f;
                    float y = rowY[r];
                    Box(row, $"Drink {c + 1}", new Vector3(fx + Inlay, y - 0.07f, z - 0.045f), new Vector3(fx + 0.07f, y + 0.07f, z + 0.045f), drinkColors[r][c]);
                    Box(row, $"Button {c + 1}", new Vector3(fx + Inlay, y - 0.105f, z - 0.02f), new Vector3(fx + 0.025f, y - 0.085f, z + 0.02f), m.Black);
                }
            }

            GameObject coin = Group(root, "Coin Panel");
            Box(coin, "Panel", new Vector3(fx, 0.7f, 0.22f), new Vector3(fx + Inlay, 0.95f, 0.42f), m.Dark);
            Box(coin, "Slot", new Vector3(fx + Inlay, 0.86f, 0.3f), new Vector3(fx + Inlay * 2f, 0.9f, 0.34f), m.Black);
            Box(root, "Outlet", new Vector3(fx, 0.12f, -0.35f), new Vector3(fx + Inlay * 2f, 0.35f, 0.3f), m.Black);
            return root;
        }

        // 자동 개찰구 (3통로): 통 4개(폭 0.22, 길이 1.4, 높이 1) 사이에 통로 3개(폭 0.6). 전체 폭 2.68.
        // 사람은 X 방향(앞뒤)으로 지나가고 통은 Z로 늘어섭니다. 플랫폼 끝에서 길이 방향으로 지나가게 하려면 Y 90°로 놓습니다.
        // 통마다 IC 카드 단말(파랗게 빛남, +X 끝 윗면), 표 넣는 곳·나오는 곳, 양 끝 표시등(+X 초록 / -X 빨강), 옆 파란 띠가 있고,
        // 통로마다 양쪽 통에서 나온 주황 문짝 2장(닫힌 상태, "Lane N Flaps")이 있습니다. 문짝 그룹을 끄면 열린 개찰구가 됩니다.
        private static GameObject BuildTicketGate(Materials m)
        {
            GameObject root = Root("Ticket Gate");
            const int cabinets = 4;
            const float cabinetWidth = 0.22f;
            const float laneWidth = 0.6f;
            const float hx = 0.7f;      // 통 길이의 절반
            const float top = 1f;
            float pitch = cabinetWidth + laneWidth;
            float firstZ = -(cabinets - 1) * pitch / 2f;

            for (int i = 0; i < cabinets; i++)
            {
                float cz = firstZ + i * pitch;
                float hz = cabinetWidth / 2f;
                GameObject cabinet = Group(root, $"Cabinet {i + 1}");
                Box(cabinet, "Base", new Vector3(-hx - 0.01f, 0f, cz - hz - 0.01f), new Vector3(hx + 0.01f, 0.05f, cz + hz + 0.01f), m.Dark);
                Box(cabinet, "Body", new Vector3(-hx, 0.05f, cz - hz), new Vector3(hx, top - 0.04f, cz + hz), m.BodyLight);
                Box(cabinet, "Top", new Vector3(-hx - 0.02f, top - 0.04f, cz - hz - 0.01f), new Vector3(hx + 0.02f, top, cz + hz + 0.01f), m.Dark);

                // 옆 파란 띠 (양쪽 옆면)
                Box(cabinet, "Stripe Left", new Vector3(-hx, 0.68f, cz - hz - Inlay), new Vector3(hx, 0.74f, cz - hz), m.Blue);
                Box(cabinet, "Stripe Right", new Vector3(-hx, 0.68f, cz + hz), new Vector3(hx, 0.74f, cz + hz + Inlay), m.Blue);

                // 윗면: IC 카드 단말(+X 끝), 표 넣는 곳(+X 끝), 표 나오는 곳(-X 끝)
                Box(cabinet, "IC Reader", new Vector3(0.25f, top, cz - 0.08f), new Vector3(0.5f, top + Inlay, cz + 0.08f), m.LedBlue);
                Box(cabinet, "Ticket Slot In", new Vector3(0.56f, top, cz - 0.05f), new Vector3(0.62f, top + Inlay, cz + 0.05f), m.Black);
                Box(cabinet, "Ticket Slot Out", new Vector3(-0.62f, top, cz - 0.05f), new Vector3(-0.5f, top + Inlay, cz + 0.05f), m.Black);

                // 양 끝 표시등: 검은 판 + 들어가는 쪽(+X) 초록, 나가는 쪽(-X) 빨강
                Box(cabinet, "Sign Panel Front", new Vector3(hx, 0.76f, cz - 0.08f), new Vector3(hx + Inlay, 0.92f, cz + 0.08f), m.Black);
                Box(cabinet, "Sign Light Front", new Vector3(hx + Inlay, 0.8f, cz - 0.045f), new Vector3(hx + Inlay * 2f, 0.88f, cz + 0.045f), m.LedGreen);
                Box(cabinet, "Sign Panel Back", new Vector3(-hx - Inlay, 0.76f, cz - 0.08f), new Vector3(-hx, 0.92f, cz + 0.08f), m.Black);
                Box(cabinet, "Sign Light Back", new Vector3(-hx - Inlay * 2f, 0.8f, cz - 0.045f), new Vector3(-hx - Inlay, 0.88f, cz + 0.045f), m.LedRed);
            }

            // 통로마다 양쪽 통에서 가운데로 나온 문짝 2장 (가운데에 0.04 틈)
            for (int lane = 0; lane < cabinets - 1; lane++)
            {
                float z0 = firstZ + lane * pitch + cabinetWidth / 2f;
                float z1 = z0 + laneWidth;
                float mid = (z0 + z1) / 2f;
                GameObject flaps = Group(root, $"Lane {lane + 1} Flaps");
                Box(flaps, "Flap Left", new Vector3(-0.02f, 0.45f, z0), new Vector3(0.02f, 0.85f, mid - 0.02f), m.Orange);
                Box(flaps, "Flap Right", new Vector3(-0.02f, 0.45f, mid + 0.02f), new Vector3(0.02f, 0.85f, z1), m.Orange);
            }
            return root;
        }

        // 차장용 볼록거울: 기둥 + 팔 + 15° 아래로 기운 둥근 거울(주황 테두리)
        private static GameObject BuildPlatformMirror(Materials m)
        {
            GameObject root = Root("Platform Mirror");
            Cylinder(root, "Base", new Vector3(0f, 0.03f, 0f), 0.12f, 0.06f, 'y', m.Dark);
            Cylinder(root, "Pole", new Vector3(0f, 1.1f, 0f), 0.04f, 2.2f, 'y', m.Metal);
            Box(root, "Arm", new Vector3(0f, 2.08f, -0.02f), new Vector3(0.18f, 2.12f, 0.02f), m.Metal);

            GameObject mirror = Group(root, "Mirror", new Vector3(0.2f, 2.1f, 0f), new Vector3(0f, 0f, -15f));
            Cylinder(mirror, "Rim", Vector3.zero, 0.3f, 0.06f, 'x', m.Orange);
            Cylinder(mirror, "Surface", new Vector3(0.035f, 0f, 0f), 0.26f, 0.012f, 'x', m.Mirror);
            return root;
        }

        // 소화기함: 빨간 상자 + 흰 띠(빨간 글자) + 손잡이
        private static GameObject BuildFireExtinguisherBox(Materials m)
        {
            GameObject root = Root("Fire Extinguisher Box");
            const float fx = 0.125f;

            Box(root, "Body", new Vector3(-fx, 0f, -0.175f), new Vector3(fx, 0.7f, 0.175f), m.Red);
            GameObject band = Group(root, "Label");
            Box(band, "Band", new Vector3(fx, 0.48f, -0.15f), new Vector3(fx + Inlay * 0.5f, 0.6f, 0.15f), m.White);
            Word(band, "Text", fx + Inlay * 0.5f, 0.54f, -0.09f, "JKE", 0.08f, m.Red, 0.09f);
            Box(root, "Handle", new Vector3(fx, 0.25f, 0.08f), new Vector3(fx + 0.02f, 0.3f, 0.1f), m.Metal);
            return root;
        }

        // 시골 간이역 우체통: 받침 + 빨간 원통 + 둥근 지붕 + 투입구 + 흰 안내판
        private static GameObject BuildPostBox(Materials m)
        {
            GameObject root = Root("Post Box Rural");
            Box(root, "Base", new Vector3(-0.17f, 0f, -0.17f), new Vector3(0.17f, 0.12f, 0.17f), m.Dark);
            Cylinder(root, "Body", new Vector3(0f, 0.62f, 0f), 0.22f, 1f, 'y', m.Red);
            Ball(root, "Roof", new Vector3(0f, 1.12f, 0f), new Vector3(0.44f, 0.24f, 0.44f), m.Red);
            Box(root, "Slot", new Vector3(0.2f, 0.92f, -0.1f), new Vector3(0.225f, 0.96f, 0.1f), m.Black);

            GameObject plate = Group(root, "Plate");
            Box(plate, "Board", new Vector3(0.215f, 0.55f, -0.08f), new Vector3(0.225f, 0.72f, 0.08f), m.White);
            Box(plate, "Text", new Vector3(0.225f, 0.62f, -0.05f), new Vector3(0.23f, 0.65f, 0.05f), m.Black);
            return root;
        }

        #endregion

        #region 글자 (앞면 +X에 붙는 얇은 조각)

        // 7세그먼트 숫자: a 위, b 오른쪽 위, c 오른쪽 아래, d 아래, e 왼쪽 아래, f 왼쪽 위, g 가운데
        private static readonly string[] DigitSegments =
        {
            "abcdef", "bc", "abdeg", "abcdg", "bcfg", "acdfg", "acdefg", "abc", "abcdefg", "abcdfg",
        };

        // 숫자와 ':'로 된 글자열을 z0부터 오른쪽(+Z)으로 씁니다. fx는 붙일 앞면의 x, cy는 글자 가운데 높이입니다.
        private static void Digits(GameObject parent, string name, float fx, float cy, float z0, string text, float height, Material material)
        {
            GameObject group = Group(parent, name);
            float width = height * 0.55f;
            float z = z0;

            for (int i = 0; i < text.Length; i++)
            {
                if (text[i] == ':')
                {
                    float s = height * 0.12f;
                    Box(group, "Colon Top", new Vector3(fx, cy + height * 0.2f - s / 2f, z), new Vector3(fx + Inlay, cy + height * 0.2f + s / 2f, z + s), material);
                    Box(group, "Colon Bottom", new Vector3(fx, cy - height * 0.2f - s / 2f, z), new Vector3(fx + Inlay, cy - height * 0.2f + s / 2f, z + s), material);
                    z += s + height * 0.2f;
                    continue;
                }

                Digit(group, $"Digit {i + 1}", fx, cy, z + width / 2f, height, text[i] - '0', material);
                z += width + height * 0.2f;
            }
        }

        // Digits로 쓴 글자열의 전체 폭 (가운데 정렬용)
        private static float TextWidth(string text, float height)
        {
            float width = 0f;
            foreach (char c in text) width += (c == ':' ? height * 0.12f : height * 0.55f) + height * 0.2f;
            return width - height * 0.2f;
        }

        private static void Digit(GameObject parent, string name, float fx, float cy, float cz, float height, int digit, Material material)
        {
            GameObject group = Group(parent, name);
            float w = height * 0.55f;
            float t = height * 0.14f;
            float top = cy + height / 2f;
            float bottom = cy - height / 2f;
            float left = cz - w / 2f;
            float right = cz + w / 2f;

            foreach (char s in DigitSegments[digit])
            {
                (float y0, float y1, float z0, float z1) = s switch
                {
                    'a' => (top - t, top, left, right),
                    'b' => (cy, top, right - t, right),
                    'c' => (bottom, cy, right - t, right),
                    'd' => (bottom, bottom + t, left, right),
                    'e' => (bottom, cy, left, left + t),
                    'f' => (cy, top, left, left + t),
                    _ => (cy - t / 2f, cy + t / 2f, left, right),
                };
                Box(group, $"Segment {char.ToUpper(s)}", new Vector3(fx, y0, z0), new Vector3(fx + Inlay, y1, z1), material);
            }
        }

        // 한자 모양 글자 여러 개를 cz0부터 gap 간격으로 씁니다. (gap을 비우면 size × 1.3)
        private static void Word(GameObject parent, string name, float fx, float cy, float cz0, string shapes, float size, Material material, float gap = 0f)
        {
            GameObject group = Group(parent, name);
            if (gap <= 0f) gap = size * 1.3f;
            for (int i = 0; i < shapes.Length; i++)
            {
                Glyph(group, $"Char {i + 1}", fx, cy, cz0 + i * gap, size, shapes[i], material);
            }
        }

        // 한자처럼 보이는 글자 하나(진짜 글자 대신 막대 조합): K 口, H 日, E 工, J 十, M 山, Q 急, G 行
        private static void Glyph(GameObject parent, string name, float fx, float cy, float cz, float size, char shape, Material material)
        {
            GameObject group = Group(parent, name);
            float h = size / 2f;
            float t = size * 0.14f;

            void Bar(string part, float y0, float y1, float z0, float z1) =>
                Box(group, part, new Vector3(fx, cy + y0, cz + z0), new Vector3(fx + Inlay, cy + y1, cz + z1), material);

            switch (shape)
            {
                case 'K':
                case 'H':
                    Bar("Top", h - t, h, -h, h);
                    Bar("Bottom", -h, -h + t, -h, h);
                    Bar("Left", -h, h, -h, -h + t);
                    Bar("Right", -h, h, h - t, h);
                    if (shape == 'H') Bar("Middle", -t / 2f, t / 2f, -h, h);
                    break;
                case 'E':
                    Bar("Top", h - t, h, -h * 0.8f, h * 0.8f);
                    Bar("Bottom", -h, -h + t, -h, h);
                    Bar("Stem", -h, h, -t / 2f, t / 2f);
                    break;
                case 'J':
                    Bar("Cross", -t / 2f, t / 2f, -h, h);
                    Bar("Stem", -h, h, -t / 2f, t / 2f);
                    break;
                case 'Q':
                    // 위 ⺈ + 가운데 彐 + 아래 心
                    Bar("Top", h - t, h, -h * 0.5f, h * 0.3f);
                    Bar("Top Left", h * 0.65f, h, -h * 0.5f, -h * 0.5f + t);
                    Bar("Middle Top", h * 0.6f - t, h * 0.6f, -h * 0.6f, h * 0.6f);
                    Bar("Middle Center", h * 0.3f - t / 2f, h * 0.3f + t / 2f, -h * 0.4f, h * 0.6f);
                    Bar("Middle Bottom", 0f, t, -h * 0.6f, h * 0.6f);
                    Bar("Middle Right", 0f, h * 0.6f, h * 0.6f - t, h * 0.6f);
                    Bar("Heart Left", -h * 0.75f, -h * 0.3f, -h, -h + t);
                    Bar("Heart Stem", -h, -h * 0.3f, -h * 0.5f, -h * 0.5f + t);
                    Bar("Heart Base", -h, -h + t, -h * 0.5f, h * 0.55f);
                    Bar("Heart Dot", -h * 0.6f, -h * 0.3f, h * 0.05f, h * 0.05f + t);
                    Bar("Heart Right", -h * 0.75f, -h * 0.35f, h - t, h);
                    break;
                case 'G':
                    // 왼쪽 彳 + 오른쪽 亍
                    Bar("Left Top", h * 0.55f, h * 0.55f + t, -h, -h * 0.35f);
                    Bar("Left Middle", 0f, t, -h, -h * 0.35f);
                    Bar("Left Stem", -h, h * 0.1f, -h * 0.65f - t / 2f, -h * 0.65f + t / 2f);
                    Bar("Right Top", h - t, h, h * 0.05f, h * 0.85f);
                    Bar("Right Middle", h * 0.25f, h * 0.25f + t, -h * 0.1f, h);
                    Bar("Right Stem", -h, h * 0.25f, h * 0.5f - t / 2f, h * 0.5f + t / 2f);
                    Bar("Right Hook", -h, -h + t, h * 0.2f, h * 0.5f);
                    break;
                default:
                    Bar("Bottom", -h, -h + t, -h, h);
                    Bar("Left", -h, h * 0.4f, -h, -h + t);
                    Bar("Middle", -h, h, -t / 2f, t / 2f);
                    Bar("Right", -h, h * 0.4f, h - t, h);
                    break;
            }
        }

        #endregion

        #region 조립 도우미

        private static GameObject Root(string name) => new GameObject(name);

        private static GameObject Group(GameObject parent, string name, Vector3 position = default, Vector3 euler = default)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent.transform, false);
            go.transform.localPosition = position;
            go.transform.localRotation = Quaternion.Euler(euler);
            return go;
        }

        // 두 꼭짓점 사이를 채우는 상자 (부모 로컬 좌표, 순서는 상관없음, Collider 없음)
        private static GameObject Box(GameObject parent, string name, Vector3 a, Vector3 b, Material material)
        {
            Vector3 min = Vector3.Min(a, b);
            Vector3 max = Vector3.Max(a, b);
            return Part(parent, name, cube, (min + max) / 2f, Quaternion.identity, max - min, material);
        }

        // 가운데·크기·회전으로 놓는 상자 (화살표 머리처럼 기울어진 조각)
        private static GameObject TiltedBox(GameObject parent, string name, Vector3 center, Vector3 size, Vector3 euler, Material material)
            => Part(parent, name, cube, center, Quaternion.Euler(euler), size, material);

        // 원기둥. axis는 길이 방향('x' 앞뒤, 'y' 위아래, 'z' 좌우)입니다. Unity 기본 Cylinder는 높이 2, 지름 1입니다.
        private static GameObject Cylinder(GameObject parent, string name, Vector3 center, float radius, float length, char axis, Material material)
        {
            Quaternion rotation = axis == 'x' ? Quaternion.Euler(0f, 0f, 90f)
                : axis == 'z' ? Quaternion.Euler(90f, 0f, 0f)
                : Quaternion.identity;
            return Part(parent, name, cylinder, center, rotation, new Vector3(radius * 2f, length / 2f, radius * 2f), material);
        }

        // 구(지름 1)를 size만큼 늘린 것
        private static GameObject Ball(GameObject parent, string name, Vector3 center, Vector3 size, Material material)
            => Part(parent, name, sphere, center, Quaternion.identity, size, material);

        private static GameObject Part(GameObject parent, string name, Mesh mesh, Vector3 position, Quaternion rotation, Vector3 scale, Material material)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent.transform, false);
            go.transform.localPosition = position;
            go.transform.localRotation = rotation;
            go.transform.localScale = scale;
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            go.AddComponent<MeshRenderer>().sharedMaterial = material;
            return go;
        }

        // 부품마다 그 메시 모양의 MeshCollider를 붙입니다. 빈 곳은 눌리지 않고, 부품 하나만 정답으로 둘 수도 있습니다.
        // 클릭 판정은 맞은 부품의 부모에서 AnswerTarget을 찾으므로, 소품 통째로 정답일 때는 루트에 AnswerTarget을 붙이면 됩니다.
        private static void AddPartColliders(GameObject root)
        {
            foreach (MeshFilter filter in root.GetComponentsInChildren<MeshFilter>())
            {
                filter.gameObject.AddComponent<MeshCollider>().sharedMesh = filter.sharedMesh;
            }
        }

        private static void Save(GameObject root, Scene preview)
        {
            PivotUtility.CenterGroupPivots(root);
            AddPartColliders(root);
            SceneManager.MoveGameObjectToScene(root, preview);
            PrefabSaveUtility.SaveKeepingIds(root, $"{PrefabFolder}/{root.name}.prefab");
            Object.DestroyImmediate(root);
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            string parent = Path.GetDirectoryName(path).Replace('\\', '/');
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
        }

        #endregion
    }
}
