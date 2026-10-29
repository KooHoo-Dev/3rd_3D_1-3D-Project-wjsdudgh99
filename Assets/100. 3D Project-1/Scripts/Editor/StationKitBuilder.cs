using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace YoureRight
{
    // 2-2 기차역용 스타일라이즈드 플랫폼 키트(Cube 조각 + 단색 머티리얼)를 프리팹으로 만드는 에디터 도구입니다.
    // Tools → You're Right → Build Station Kit
    //
    // 플랫폼의 기준점은 "가장자리 선, 바닥(y 0), 길이 가운데"입니다. 몸체는 로컬 -X 쪽으로 뻗고, 가장자리는 +X(선로 쪽)를 봅니다.
    //   - 왼쪽 플랫폼(선로의 -X 쪽): 회전 Y 0,   위치 x = 선로 중심 - 1.45
    //   - 오른쪽 플랫폼(선로의 +X 쪽): 회전 Y 180, 위치 x = 선로 중심 + 1.45
    // 스크린도어·울타리의 기준점은 자기 밑면 가운데입니다. 플랫폼과 같은 회전으로, 플랫폼 윗면(y 1.2)에 둡니다.
    //   - 스크린도어: 가장자리에서 0.5 안쪽 / 울타리: 가장자리에서 4.65 안쪽(뒤쪽 끝)
    //
    // 플랫폼은 Background 레이어 + BoxCollider(클릭을 막고 무반응), 스크린도어는 양쪽 하우징 Body에만 MeshCollider(Default, 클릭하면 오답), 문짝·유리 패널·울타리는 Collider 없음(무반응, 클릭이 통과)입니다.
    // 다시 실행하면 같은 이름의 머티리얼·프리팹을 덮어씁니다(씬에 놓인 인스턴스는 그대로 따라 바뀜).
    public static class StationKitBuilder
    {
        private const string MenuPath = "Tools/You're Right/Build Station Kit";
        private const string Title = "Build Station Kit";
        private const string MaterialFolder = "Assets/100. 3D Project-1/Materials/Station";
        private const string PrefabFolder = "Assets/100. 3D Project-1/Prefabs/Station";
        private const string BackgroundLayerName = "Background";

        // 플랫폼 치수 (지금 씬의 Japanese Station 플랫폼과 같은 높이·폭)
        private const float PlatformTop = 1.2f;
        private const float PlatformDepth = 4.8f;
        private const float ModuleLength = 4f;

        // 스크린도어 치수
        private const float DoorLineX = 0f;         // 문 선 = 문 프리팹의 기준점 (놓을 때는 플랫폼 가장자리에서 0.5 안쪽)
        private const float DoorHeight = 1.35f;     // 플랫폼 윗면부터 문 높이

        private static Mesh cube;

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

            // 현재 씬을 더럽히지 않도록 미리보기 씬에서 조립한 뒤 프리팹으로 저장합니다.
            Scene preview = EditorSceneManager.NewPreviewScene();
            try
            {
                var m = new Materials();
                Save(BuildModernPlatform(m), "Platform Modern 4m", preview);
                Save(BuildRuralPlatform(m), "Platform Rural 4m", preview);
                Save(BuildScreenDoorGate(m), "Screen Door Gate", preview);
                Save(BuildScreenDoorPanel(m), "Screen Door Panel", preview);
                Save(BuildModernFence(m), "Fence Modern 2m", preview);
                Save(BuildRuralFence(m), "Fence Rural 2m", preview);
                Save(BuildModernPost(m), "Fence Modern Post", preview);
                Save(BuildRuralPost(m), "Fence Rural Post", preview);
            }
            finally
            {
                EditorSceneManager.ClosePreviewScene(preview);
            }

            AssetDatabase.SaveAssets();
            Debug.Log($"[StationKitBuilder] 키트 생성 완료: {PrefabFolder}");
            EditorGUIUtility.PingObject(AssetDatabase.LoadAssetAtPath<Object>(PrefabFolder));
        }

        #region 머티리얼

        // 키트에서 쓰는 단색 머티리얼 모음 (스타일라이즈드: 무광, 채도 약간)
        private class Materials
        {
            // 최신 역
            public readonly Material ModernFloor = Lit("M_Station_Modern_Floor", "C9CDD2");
            public readonly Material ModernSide = Lit("M_Station_Modern_Side", "7D8590");
            public readonly Material ModernEdge = Lit("M_Station_Modern_Edge", "EEF0F2");
            public readonly Material ModernSeam = Lit("M_Station_Modern_Seam", "B3B8BF");
            public readonly Material Tactile = Lit("M_Station_Tactile", "F2C230");
            public readonly Material DoorBody = Lit("M_Station_Door_Body", "E9EDF0");
            public readonly Material DoorFrame = Lit("M_Station_Door_Frame", "AEB6BF", 0.4f);
            public readonly Material DoorAccent = Lit("M_Station_Door_Accent", "2BB3A8");
            public readonly Material DoorSeal = Lit("M_Station_Door_Seal", "3A3F45");
            public readonly Material Glass = TransparentLit("M_Station_Glass", "A8D8EA", 0.35f);
            public readonly Material FenceMetal = Lit("M_Station_Fence_Metal", "9AA3AD", 0.4f);

            // 시골 간이역
            public readonly Material RuralTop = Lit("M_Station_Rural_Top", "8A847C");
            public readonly Material RuralWall = Lit("M_Station_Rural_Wall", "A69B88");
            public readonly Material RuralEdge = Lit("M_Station_Rural_Edge", "D6CDBB");
            public readonly Material RuralLine = Lit("M_Station_Rural_Line", "F4F1E8");
            public readonly Material RuralWallDark = Lit("M_Station_Rural_Wall_Dark", "8E8473");
            public readonly Material Wood = Lit("M_Station_Wood", "8A5A3A");
            public readonly Material WoodDark = Lit("M_Station_Wood_Dark", "6B4329");
        }

        private static Material Lit(string name, string hex, float smoothness = 0.15f)
        {
            Material material = LoadOrCreate(name);
            material.SetColor("_BaseColor", Hex(hex));
            material.SetFloat("_Smoothness", smoothness);
            EditorUtility.SetDirty(material);
            return material;
        }

        // URP Lit의 Surface Type을 Transparent(Alpha 블렌드)로 둡니다. Inspector의 설정과 같은 값입니다.
        private static Material TransparentLit(string name, string hex, float alpha)
        {
            Material material = LoadOrCreate(name);
            Color color = Hex(hex);
            color.a = alpha;
            material.SetColor("_BaseColor", color);
            material.SetFloat("_Smoothness", 0.9f);
            material.SetFloat("_Surface", 1f);
            material.SetFloat("_Blend", 0f);
            material.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
            material.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
            material.SetFloat("_SrcBlendAlpha", (float)BlendMode.One);
            material.SetFloat("_DstBlendAlpha", (float)BlendMode.OneMinusSrcAlpha);
            material.SetFloat("_ZWrite", 0f);
            material.SetOverrideTag("RenderType", "Transparent");
            material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            material.renderQueue = (int)RenderQueue.Transparent;
            material.SetShaderPassEnabled("DepthOnly", false);
            material.SetShaderPassEnabled("ShadowCaster", false);
            EditorUtility.SetDirty(material);
            return material;
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

        #region 플랫폼

        // 최신 역 플랫폼: 밝은 타일 바닥 + 흰 가장자리 + 노란 점자 블록 + 1m 간격 줄눈
        private static GameObject BuildModernPlatform(Materials m)
        {
            GameObject root = Root("Platform Modern 4m");
            float h = ModuleLength / 2f;

            Box(root, "Body", new Vector3(-PlatformDepth, 0f, -h), new Vector3(-0.25f, PlatformTop - 0.05f, h), m.ModernSide);
            Box(root, "Floor", new Vector3(-PlatformDepth, PlatformTop - 0.15f, -h), new Vector3(-0.35f, PlatformTop, h), m.ModernFloor);
            Box(root, "Edge", new Vector3(-0.35f, PlatformTop - 0.2f, -h), new Vector3(0f, PlatformTop, h), m.ModernEdge);
            Box(root, "Tactile", new Vector3(-1.3f, PlatformTop, -h), new Vector3(-0.9f, PlatformTop + 0.015f, h), m.Tactile);

            for (int i = -1; i <= 2; i++)
            {
                float z = i;
                Box(root, $"Seam {i + 2}", new Vector3(-PlatformDepth, PlatformTop, z - 0.02f), new Vector3(-1.3f, PlatformTop + 0.005f, Mathf.Min(z + 0.02f, h)), m.ModernSeam);
            }

            AddPlatformCollider(root);
            return root;
        }

        // 시골 간이역 플랫폼: 어두운 아스팔트 윗면 + 밝은 콘크리트 가장자리 + 흰 선 + 돌벽 옆면
        private static GameObject BuildRuralPlatform(Materials m)
        {
            GameObject root = Root("Platform Rural 4m");
            float h = ModuleLength / 2f;

            Box(root, "Wall", new Vector3(-PlatformDepth, 0f, -h), new Vector3(-0.3f, PlatformTop - 0.1f, h), m.RuralWall);
            Box(root, "Wall Band", new Vector3(-0.31f, 0f, -h), new Vector3(-0.29f, 0.35f, h), m.RuralWallDark);
            Box(root, "Top", new Vector3(-PlatformDepth, PlatformTop - 0.15f, -h), new Vector3(-0.6f, PlatformTop - 0.02f, h), m.RuralTop);
            Box(root, "Edge", new Vector3(-0.6f, PlatformTop - 0.2f, -h), new Vector3(0f, PlatformTop, h), m.RuralEdge);
            Box(root, "Line", new Vector3(-0.95f, PlatformTop - 0.02f, -h), new Vector3(-0.8f, PlatformTop - 0.01f, h), m.RuralLine);

            // 돌벽 줄눈 (선로 쪽 옆면에 세로 줄 2개)
            for (int i = 0; i < 2; i++)
            {
                float z = -1f + i * 2f;
                Box(root, $"Wall Joint {i + 1}", new Vector3(-0.31f, 0.35f, z - 0.03f), new Vector3(-0.29f, PlatformTop - 0.2f, z + 0.03f), m.RuralWallDark);
            }

            AddPlatformCollider(root);
            return root;
        }

        // 플랫폼 전체를 감싸는 BoxCollider 하나(루트)와 Background 레이어
        private static void AddPlatformCollider(GameObject root)
        {
            var collider = root.AddComponent<BoxCollider>();
            collider.center = new Vector3(-PlatformDepth / 2f, PlatformTop / 2f, 0f);
            collider.size = new Vector3(PlatformDepth, PlatformTop, ModuleLength);

            int layer = LayerMask.NameToLayer(BackgroundLayerName);
            if (layer < 0)
            {
                Debug.LogWarning($"[StationKitBuilder] '{BackgroundLayerName}' 레이어가 없어서 플랫폼을 Default로 둡니다.");
                return;
            }
            foreach (Transform t in root.GetComponentsInChildren<Transform>(true)) t.gameObject.layer = layer;
        }

        #endregion

        #region 스크린도어

        // 문 한 칸 (길이 2.4): 양쪽 문 주머니(하우징) + 닫힌 유리 문짝 2장. 문짝은 따로 움직여 열린 모습도 만들 수 있습니다.
        private static GameObject BuildScreenDoorGate(Materials m)
        {
            GameObject root = Root("Screen Door Gate");
            const float y0 = 0f;
            const float y1 = DoorHeight;

            for (int side = -1; side <= 1; side += 2)
            {
                string name = side < 0 ? "Housing Front" : "Housing Back";
                float zIn = side * 0.75f;
                float zOut = side * 1.2f;
                GameObject housing = Group(root, name);
                GameObject body = Box(housing, "Body", new Vector3(DoorLineX - 0.12f, y0, Mathf.Min(zIn, zOut)), new Vector3(DoorLineX + 0.12f, y1 + 0.1f, Mathf.Max(zIn, zOut)), m.DoorBody);
                body.AddComponent<MeshCollider>().sharedMesh = cube;   // 하우징만 클릭을 받음(Default 레이어라 오답), 문짝·유리는 통과
                Box(housing, "Accent", new Vector3(DoorLineX + 0.12f, y1 - 0.12f, Mathf.Min(zIn, zOut)), new Vector3(DoorLineX + 0.13f, y1 - 0.04f, Mathf.Max(zIn, zOut)), m.DoorAccent);
                Box(housing, "Cap", new Vector3(DoorLineX - 0.13f, y1 + 0.1f, Mathf.Min(zIn, zOut)), new Vector3(DoorLineX + 0.13f, y1 + 0.14f, Mathf.Max(zIn, zOut)), m.DoorFrame);
            }

            for (int side = -1; side <= 1; side += 2)
            {
                string name = side < 0 ? "Door Leaf Front" : "Door Leaf Back";
                GameObject leaf = Group(root, name);
                leaf.transform.localPosition = new Vector3(DoorLineX, 0f, 0f);
                float zA = 0f;
                float zB = side * 0.75f;
                Box(leaf, "Glass", new Vector3(-0.03f, y0 + 0.12f, Mathf.Min(zA, zB)), new Vector3(0.03f, y1 - 0.1f, Mathf.Max(zA, zB)), m.Glass);
                Box(leaf, "Top Rail", new Vector3(-0.05f, y1 - 0.1f, Mathf.Min(zA, zB)), new Vector3(0.05f, y1, Mathf.Max(zA, zB)), m.DoorFrame);
                Box(leaf, "Bottom Rail", new Vector3(-0.05f, y0 + 0.04f, Mathf.Min(zA, zB)), new Vector3(0.05f, y0 + 0.12f, Mathf.Max(zA, zB)), m.DoorFrame);
                Box(leaf, "Seal", new Vector3(-0.05f, y0 + 0.04f, side * 0.03f - 0.03f), new Vector3(0.05f, y1, side * 0.03f + 0.03f), m.DoorSeal);
            }

            Box(root, "Sill", new Vector3(DoorLineX - 0.15f, y0, -1.2f), new Vector3(DoorLineX + 0.15f, y0 + 0.03f, 1.2f), m.DoorSeal);
            return root;
        }

        // 고정 유리 패널 한 칸 (길이 1). 문 칸 사이를 채웁니다.
        private static GameObject BuildScreenDoorPanel(Materials m)
        {
            GameObject root = Root("Screen Door Panel");
            const float y0 = 0f;
            const float y1 = DoorHeight;

            Box(root, "Glass", new Vector3(DoorLineX - 0.03f, y0 + 0.15f, -0.45f), new Vector3(DoorLineX + 0.03f, y1 - 0.1f, 0.45f), m.Glass);
            Box(root, "Bottom Rail", new Vector3(DoorLineX - 0.1f, y0, -0.5f), new Vector3(DoorLineX + 0.1f, y0 + 0.15f, 0.5f), m.DoorBody);
            Box(root, "Top Rail", new Vector3(DoorLineX - 0.1f, y1 - 0.1f, -0.5f), new Vector3(DoorLineX + 0.1f, y1 + 0.05f, 0.5f), m.DoorBody);
            Box(root, "Accent", new Vector3(DoorLineX + 0.1f, y1 - 0.08f, -0.5f), new Vector3(DoorLineX + 0.11f, y1 - 0.02f, 0.5f), m.DoorAccent);
            Box(root, "Post Front", new Vector3(DoorLineX - 0.08f, y0, -0.5f), new Vector3(DoorLineX + 0.08f, y1 + 0.05f, -0.45f), m.DoorFrame);
            Box(root, "Post Back", new Vector3(DoorLineX - 0.08f, y0, 0.45f), new Vector3(DoorLineX + 0.08f, y1 + 0.05f, 0.5f), m.DoorFrame);
            return root;
        }

        #endregion

        #region 울타리 (플랫폼 뒤쪽 가장자리)

        // 울타리 한 칸에는 기둥이 로컬 -Z 끝에만 있습니다. 줄의 반대쪽 끝을 막을 때는 기둥 프리팹(Fence ... Post)을
        // 마지막 칸과 같은 위치에 Y 180으로 돌려 놓습니다(돌린 울타리를 겹쳐 놓던 자리와 같음, 가로대가 두 번 그려지지 않음).

        // 최신 역: 금속 난간 (길이 2)
        private static GameObject BuildModernFence(Materials m)
        {
            GameObject root = Root("Fence Modern 2m");
            const float x = 0f;
            const float y0 = 0f;

            AddModernPost(root, m);
            Box(root, "Rail Top", new Vector3(x - 0.05f, y0 + 1.05f, -1f), new Vector3(x + 0.05f, y0 + 1.12f, 1f), m.FenceMetal);
            Box(root, "Rail Mid", new Vector3(x - 0.02f, y0 + 0.55f, -1f), new Vector3(x + 0.02f, y0 + 0.59f, 1f), m.FenceMetal);
            Box(root, "Glass", new Vector3(x - 0.015f, y0 + 0.1f, -0.92f), new Vector3(x + 0.015f, y0 + 1.02f, 1f), m.Glass);
            return root;
        }

        // 시골 간이역: 나무 울타리 (길이 2, 기둥 + 가로대 2줄)
        private static GameObject BuildRuralFence(Materials m)
        {
            GameObject root = Root("Fence Rural 2m");
            const float x = 0f;
            const float y0 = 0f;

            AddRuralPost(root, m);
            Box(root, "Rail Top", new Vector3(x - 0.03f, y0 + 0.75f, -1f), new Vector3(x + 0.03f, y0 + 0.87f, 1f), m.Wood);
            Box(root, "Rail Bottom", new Vector3(x - 0.03f, y0 + 0.3f, -1f), new Vector3(x + 0.03f, y0 + 0.42f, 1f), m.Wood);
            return root;
        }

        // 줄 끝을 막는 기둥만 있는 프리팹 (기준점은 울타리 한 칸과 같음)
        private static GameObject BuildModernPost(Materials m)
        {
            GameObject root = Root("Fence Modern Post");
            AddModernPost(root, m);
            return root;
        }

        private static GameObject BuildRuralPost(Materials m)
        {
            GameObject root = Root("Fence Rural Post");
            AddRuralPost(root, m);
            return root;
        }

        private static void AddModernPost(GameObject root, Materials m)
        {
            Box(root, "Post", new Vector3(-0.04f, 0f, -1f), new Vector3(0.04f, 1.1f, -0.92f), m.FenceMetal);
        }

        private static void AddRuralPost(GameObject root, Materials m)
        {
            Box(root, "Post", new Vector3(-0.06f, 0f, -1f), new Vector3(0.06f, 1f, -0.88f), m.WoodDark);
            Box(root, "Post Cap", new Vector3(-0.08f, 1f, -1.02f), new Vector3(0.08f, 1.04f, -0.86f), m.WoodDark);
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
            var renderer = go.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
            if (material.renderQueue >= (int)RenderQueue.Transparent) renderer.shadowCastingMode = ShadowCastingMode.Off;
            return go;
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
