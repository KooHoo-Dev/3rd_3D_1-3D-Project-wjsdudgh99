using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace YoureRight
{
    // 2-1 자동차 전시장용 키트(Cube·Cylinder 조각 + 단색 URP Lit)를 프리팹으로 만드는 에디터 도구입니다.
    // Tools → You're Right → Build Showroom Kit
    //
    // 바닥 윗면이 y 0이고, 차는 바닥(또는 받침대 윗면 y 0.12)에 바로 올립니다. 차 4~5대를 X 방향으로 한 줄로 세우고
    // 카메라는 -Z 쪽에서 보며, 벽은 +Z 쪽 뒤에 둡니다(벽 앞면이 기준점이라 벽의 z = 바닥 뒤쪽 끝).
    //   - Showroom Floor 4x4: 윗면 y 0, 두께 0.5, 기준점은 윗면 가운데. 4 간격으로 깔기
    //   - Showroom Floor Edge 4m: 바닥 가장자리 마감(빛나는 줄). 기준점은 바닥 가장자리 선·윗면, 몸체는 -Z(바깥)로 0.15
    //     네 변에 두름: 앞 0 / 뒤 180 / 왼쪽 90 / 오른쪽 -90
    //   - Showroom Floor Edge Corner: 가장자리 두 줄이 만나는 모서리 조각. 기준점은 바닥 모서리 점, 회전 0이면 앞 왼쪽 모서리
    //   - Showroom Wall 4m: 높이 6, 가운데 세로 빛줄기 하나. 기준점은 밑면 가운데·앞면, 몸체는 +Z(뒤)로 0.3
    //   - Car Pedestal: 지름 5.6 원형 받침대(윗면 0.12) + 밑동의 빛나는 테두리
    //   - Info Stand: 차 옆 안내판(기둥 + 기울어진 화면). 화면 색·글자 줄을 바꿔 미끼로 씀
    // 바닥·가장자리·벽·받침대는 Background 레이어 + Collider(클릭을 막고 무반응), 안내판은 부품마다 MeshCollider(Default, 클릭하면 오답)입니다.
    // 머티리얼은 모두 Emission을 켜 둡니다(검정, 빛나는 부분만 색). 다시 실행하면 같은 이름의 머티리얼·프리팹을 덮어씁니다.
    public static class ShowroomKitBuilder
    {
        private const string MenuPath = "Tools/You're Right/Build Showroom Kit";
        private const string Title = "Build Showroom Kit";
        private const string MaterialFolder = "Assets/100. 3D Project-1/Materials/Showroom";
        private const string PrefabFolder = "Assets/100. 3D Project-1/Prefabs/Showroom";
        private const string BackgroundLayerName = "Background";

        private const float Tile = 4f;
        private const float FloorThickness = 0.5f;
        private const float WallHeight = 6f;
        private const float WallThickness = 0.3f;
        private const float PedestalRadius = 2.8f;
        private const float PedestalTop = 0.12f;

        private static Mesh cube;
        private static Mesh cylinder;

        [MenuItem(MenuPath)]
        private static void Build()
        {
            if (EditorApplication.isPlaying)
            {
                EditorUtility.DisplayDialog(Title, "Play 모드에서는 만들 수 없습니다.", "OK");
                return;
            }

            if (AssetDatabase.IsValidFolder(PrefabFolder) &&
                EditorUtility.DisplayDialog(Title, "이미 만든 키트가 있습니다. 머티리얼과 프리팹을 덮어쓸까요?\n(프리팹에서 직접 고친 값은 사라집니다)", "덮어쓰기", "취소") == false)
            {
                return;
            }

            EnsureFolder(MaterialFolder);
            EnsureFolder(PrefabFolder);
            cube = Resources.GetBuiltinResource<Mesh>("Cube.fbx");
            cylinder = Resources.GetBuiltinResource<Mesh>("Cylinder.fbx");

            Scene preview = EditorSceneManager.NewPreviewScene();
            try
            {
                var m = new Materials();
                Save(BuildFloor(m), "Showroom Floor 4x4", preview);
                Save(BuildFloorEdge(m), "Showroom Floor Edge 4m", preview);
                Save(BuildFloorEdgeCorner(m), "Showroom Floor Edge Corner", preview);
                Save(BuildWall(m), "Showroom Wall 4m", preview);
                Save(BuildPedestal(m), "Car Pedestal", preview);
                Save(BuildInfoStand(m), "Info Stand", preview);
            }
            finally
            {
                EditorSceneManager.ClosePreviewScene(preview);
            }

            AssetDatabase.SaveAssets();
            Debug.Log($"[ShowroomKitBuilder] 키트 생성 완료: {PrefabFolder}");
            EditorGUIUtility.PingObject(AssetDatabase.LoadAssetAtPath<Object>(PrefabFolder));
        }

        #region 머티리얼

        // 어두운 전시장: 광택 바닥, 짙은 벽, 차가운 흰 빛줄기
        private class Materials
        {
            public readonly Material Floor = Lit("M_Showroom_Floor", "1C1F24", 0.88f);
            public readonly Material FloorSeam = Lit("M_Showroom_Floor_Seam", "2A2E35", 0.88f);
            public readonly Material Edge = Lit("M_Showroom_Edge", "2E3238", 0.6f);
            public readonly Material Wall = Lit("M_Showroom_Wall", "15171B", 0.3f);
            public readonly Material Baseboard = Lit("M_Showroom_Baseboard", "0E0F12", 0.5f);
            public readonly Material Pedestal = Lit("M_Showroom_Pedestal", "2A2D33", 0.75f);
            public readonly Material Metal = Lit("M_Showroom_Metal", "8C939C", 0.7f, 0.8f);
            public readonly Material Panel = Lit("M_Showroom_Panel", "202329", 0.5f);
            public readonly Material StripWhite = Glow("M_Showroom_Glow_White", "DCE6FF", 2.5f);
            public readonly Material StripBlue = Glow("M_Showroom_Glow_Blue", "3FA9FF", 2f);
            public readonly Material StripAmber = Glow("M_Showroom_Glow_Amber", "FFB347", 2f);
            // 미끼·장식용 빛줄 색 (2026-11-03 추가, 키트 조각에는 쓰지 않음)
            public readonly Material StripRed = Glow("M_Showroom_Glow_Red", "FF4D5E", 2f);
            public readonly Material StripGreen = Glow("M_Showroom_Glow_Green", "4DFF88", 2f);
            public readonly Material StripPurple = Glow("M_Showroom_Glow_Purple", "A06BFF", 2f);
            public readonly Material StripPink = Glow("M_Showroom_Glow_Pink", "FF6BC8", 2f);
            public readonly Material StripCyan = Glow("M_Showroom_Glow_Cyan", "3FF2FF", 2f);
            public readonly Material StripYellow = Glow("M_Showroom_Glow_Yellow", "FFE14D", 2f);
            public readonly Material Screen = Glow("M_Showroom_Screen", "1E5FA8", 1.2f);
            public readonly Material ScreenText = Glow("M_Showroom_Screen_Text", "F2F6FF", 1.5f);
        }

        private static Material Lit(string name, string hex, float smoothness, float metallic = 0f)
        {
            Material material = LoadOrCreate(name);
            material.SetColor("_BaseColor", Hex(hex));
            material.SetFloat("_Smoothness", smoothness);
            material.SetFloat("_Metallic", metallic);
            SetEmission(material, Color.black);
            return material;
        }

        private static Material Glow(string name, string hex, float intensity)
        {
            Material material = LoadOrCreate(name);
            Color color = Hex(hex);
            material.SetColor("_BaseColor", color);
            material.SetFloat("_Smoothness", 0.5f);
            SetEmission(material, color * intensity);
            return material;
        }

        // 정답 반짝임을 위해 Emission을 켭니다. EmissiveIsBlack으로 두면 URP가 저장할 때 _EMISSION을 꺼 버리므로 RealtimeEmissive로 둡니다.
        private static void SetEmission(Material material, Color color)
        {
            material.SetColor("_EmissionColor", color);
            material.EnableKeyword("_EMISSION");
            material.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
            EditorUtility.SetDirty(material);
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

        #region 바닥·벽

        // 4 × 4 바닥 타일. 앞쪽(-Z)과 왼쪽(-X) 가장자리에 줄눈을 넣어 이어 깔면 4 간격 격자가 됩니다.
        private static GameObject BuildFloor(Materials m)
        {
            GameObject root = Root("Showroom Floor 4x4");
            float h = Tile / 2f;
            Box(root, "Slab", new Vector3(-h, -FloorThickness, -h), new Vector3(h, 0f, h), m.Floor);
            Box(root, "Seam X", new Vector3(-h, 0f, -h), new Vector3(h, 0.004f, -h + 0.03f), m.FloorSeam);
            Box(root, "Seam Z", new Vector3(-h, 0f, -h), new Vector3(-h + 0.03f, 0.004f, h), m.FloorSeam);
            AddBackgroundBox(root, new Vector3(-h, -FloorThickness, -h), new Vector3(h, 0f, h));
            return root;
        }

        // 바닥 앞쪽 가장자리 마감: 바닥 두께만큼의 띠 + 가운데 빛나는 줄
        private static GameObject BuildFloorEdge(Materials m)
        {
            GameObject root = Root("Showroom Floor Edge 4m");
            float h = Tile / 2f;
            const float depth = 0.15f;
            Box(root, "Trim", new Vector3(-h, -FloorThickness, -depth), new Vector3(h, 0.02f, 0f), m.Edge);
            Box(root, "Glow Line", new Vector3(-h, -0.3f, -depth - 0.01f), new Vector3(h, -0.25f, -depth), m.StripBlue);
            AddBackgroundBox(root, new Vector3(-h, -FloorThickness, -depth - 0.01f), new Vector3(h, 0.02f, 0f));
            return root;
        }

        // 가장자리 마감 두 줄이 만나는 모서리의 0.15 × 0.15 빈틈을 채웁니다. 빛줄도 두 바깥 면을 따라 돌아갑니다.
        // 기준점은 바닥 모서리 점·윗면이고, 회전 0이면 몸체가 -X·-Z(앞 왼쪽 모서리)로 나갑니다.
        //   앞 왼쪽 0 / 앞 오른쪽 -90 / 뒤 오른쪽 180 / 뒤 왼쪽 90
        private static GameObject BuildFloorEdgeCorner(Materials m)
        {
            GameObject root = Root("Showroom Floor Edge Corner");
            const float depth = 0.15f;
            Box(root, "Trim", new Vector3(-depth, -FloorThickness, -depth), new Vector3(0f, 0.02f, 0f), m.Edge);
            Box(root, "Glow Line Front", new Vector3(-depth - 0.01f, -0.3f, -depth - 0.01f), new Vector3(0f, -0.25f, -depth), m.StripBlue);
            Box(root, "Glow Line Side", new Vector3(-depth - 0.01f, -0.3f, -depth), new Vector3(-depth, -0.25f, 0f), m.StripBlue);
            AddBackgroundBox(root, new Vector3(-depth - 0.01f, -FloorThickness, -depth - 0.01f), new Vector3(0f, 0.02f, 0f));
            return root;
        }

        // 벽 4m: 짙은 판 + 걸레받이 + 가운데 세로 빛줄기
        private static GameObject BuildWall(Materials m)
        {
            GameObject root = Root("Showroom Wall 4m");
            float h = Tile / 2f;
            Box(root, "Panel", new Vector3(-h, 0f, 0f), new Vector3(h, WallHeight, WallThickness), m.Wall);
            Box(root, "Baseboard", new Vector3(-h, 0f, -0.03f), new Vector3(h, 0.15f, 0f), m.Baseboard);
            Box(root, "Light Strip", new Vector3(-0.1f, 0.4f, -0.04f), new Vector3(0.1f, WallHeight - 0.4f, 0f), m.StripWhite);
            AddBackgroundBox(root, new Vector3(-h, 0f, -0.04f), new Vector3(h, WallHeight, WallThickness));
            return root;
        }

        #endregion

        #region 받침대·안내판

        // 원형 받침대: 밑동(빛나는 테두리) + 윗판. 차는 윗면(y 0.12)에 올립니다.
        private static GameObject BuildPedestal(Materials m)
        {
            GameObject root = Root("Car Pedestal");
            Cylinder(root, "Glow Ring", PedestalRadius + 0.05f, 0f, 0.05f, m.StripWhite);
            Cylinder(root, "Top", PedestalRadius, 0.05f, PedestalTop, m.Pedestal);
            AddBackgroundLayer(root);
            foreach (MeshFilter filter in root.GetComponentsInChildren<MeshFilter>())
            {
                filter.gameObject.AddComponent<MeshCollider>().sharedMesh = filter.sharedMesh;
            }
            return root;
        }

        // 안내판: 바닥판 + 기둥 + 뒤로 25° 기운 화면(앞면 -Z, 처음에 -25°로 만들어 화면이 바닥을 봤던 것을 고침). 화면에 흰 줄 3개(차 이름·가격 자리)를 둡니다.
        private static GameObject BuildInfoStand(Materials m)
        {
            GameObject root = Root("Info Stand");
            Box(root, "Base", new Vector3(-0.2f, 0f, -0.15f), new Vector3(0.2f, 0.03f, 0.15f), m.Metal);
            Box(root, "Pole", new Vector3(-0.03f, 0.03f, -0.03f), new Vector3(0.03f, 0.95f, 0.03f), m.Metal);

            GameObject head = Group(root, "Head");
            head.transform.localPosition = new Vector3(0f, 0.95f, 0f);
            head.transform.localRotation = Quaternion.Euler(25f, 0f, 0f);   // 윗부분이 뒤(+Z)로 기울어 화면이 위를 봄
            Box(head, "Frame", new Vector3(-0.32f, 0f, -0.02f), new Vector3(0.32f, 0.42f, 0.02f), m.Panel);
            Box(head, "Screen", new Vector3(-0.29f, 0.03f, -0.025f), new Vector3(0.29f, 0.39f, -0.02f), m.Screen);
            GameObject lines = Group(head, "Lines");
            Box(lines, "Line 1", new Vector3(-0.24f, 0.29f, -0.03f), new Vector3(0.24f, 0.33f, -0.025f), m.ScreenText);
            Box(lines, "Line 2", new Vector3(-0.24f, 0.19f, -0.03f), new Vector3(0.12f, 0.22f, -0.025f), m.ScreenText);
            Box(lines, "Line 3", new Vector3(-0.24f, 0.09f, -0.03f), new Vector3(0.04f, 0.12f, -0.025f), m.StripAmber);

            foreach (MeshFilter filter in root.GetComponentsInChildren<MeshFilter>())
            {
                filter.gameObject.AddComponent<MeshCollider>().sharedMesh = filter.sharedMesh;
            }
            return root;
        }

        #endregion

        #region 조립 도우미

        private static GameObject Root(string name) => new GameObject(name);

        private static GameObject Group(GameObject parent, string name)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent.transform, false);
            return go;
        }

        // min ~ max 범위를 채우는 상자 하나 (부모 로컬 좌표, Collider 없음)
        private static GameObject Box(GameObject parent, string name, Vector3 min, Vector3 max, Material material)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent.transform, false);
            go.transform.localPosition = (min + max) / 2f;
            go.transform.localScale = max - min;
            go.AddComponent<MeshFilter>().sharedMesh = cube;
            go.AddComponent<MeshRenderer>().sharedMaterial = material;
            return go;
        }

        // y0 ~ y1 높이의 원기둥 (Unity Cylinder는 높이 2라서 Y 스케일은 높이의 절반)
        private static GameObject Cylinder(GameObject parent, string name, float radius, float y0, float y1, Material material)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent.transform, false);
            go.transform.localPosition = new Vector3(0f, (y0 + y1) / 2f, 0f);
            go.transform.localScale = new Vector3(radius * 2f, (y1 - y0) / 2f, radius * 2f);
            go.AddComponent<MeshFilter>().sharedMesh = cylinder;
            go.AddComponent<MeshRenderer>().sharedMaterial = material;
            return go;
        }

        // 루트에 전체를 감싸는 BoxCollider + Background 레이어(클릭을 막고 무반응)
        private static void AddBackgroundBox(GameObject root, Vector3 min, Vector3 max)
        {
            var collider = root.AddComponent<BoxCollider>();
            collider.center = (min + max) / 2f;
            collider.size = max - min;
            AddBackgroundLayer(root);
        }

        private static void AddBackgroundLayer(GameObject root)
        {
            int layer = LayerMask.NameToLayer(BackgroundLayerName);
            if (layer < 0)
            {
                Debug.LogWarning($"[ShowroomKitBuilder] '{BackgroundLayerName}' 레이어가 없어서 {root.name}을(를) Default로 둡니다.");
                return;
            }
            foreach (Transform t in root.GetComponentsInChildren<Transform>(true)) t.gameObject.layer = layer;
        }

        private static void Save(GameObject root, string fileName, Scene preview)
        {
            PivotUtility.CenterGroupPivots(root);
            SceneManager.MoveGameObjectToScene(root, preview);
            PrefabSaveUtility.SaveKeepingIds(root, $"{PrefabFolder}/{fileName}.prefab");
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
