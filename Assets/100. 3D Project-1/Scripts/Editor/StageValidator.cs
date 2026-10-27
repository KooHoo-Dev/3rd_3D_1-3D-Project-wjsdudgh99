using System.Collections.Generic;
using System.Linq;
using Unity.Collections;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace YoureRight
{
    // 열린 스테이지 씬을 읽기만 해서 정답 쌍, 쌍둥이 동일성, 레이어, 양쪽 대칭 설정을 검사하는 에디터 도구 입니다.
    // 결과는 Console에 남기고(로그를 클릭하면 하이어라키에서 위치가 표시됨), 끝나면 요약 창을 띄웁니다. 씬은 고치지 않습니다.
    // 위치는 섬 중심(Pivot) 기준 상대 위치로, 회전은 월드 회전으로 비교하므로 통째로 돌려 둔 Props/Right도 화면에 보이는 그대로 비교합니다.
    public static class StageValidator
    {
        private const string MenuPath = "Tools/You're Right/Validate Stage";
        private const string Title = "Validate Stage";
        private const string LogPrefix = "[StageValidator]";
        private const string PropsRootName = "Props";
        private const string StageRootName = "Stage";
        private const string BackgroundLayerName = "Background";
        private const string LeftPivotName = "CameraPivot Left";
        private const string RightPivotName = "CameraPivot Right";

        // 같은 것으로 보는 오차
        private const float PositionTolerance = 0.01f;
        private const float RotationTolerance = 1f;       // 도
        private const float ScaleTolerance = 0.01f;       // 비율 (1%)
        private const float TrapDistance = 0.5f;          // 이 거리 안에 있는 같은 물체는 함정 후보
        private const float SymmetryTolerance = 0.01f;    // 메시 크기 대비 꼭짓점 오차 비율
        private const float ColorTolerance = 0.05f;       // 색(0~1) 채널별 오차
        private const int RayDirections = 4096;           // 대칭 판단 때 쏘는 광선 수 (방향 간격 약 3.5°)
        private const int MinPairs = 3;
        private const int MaxPairs = 5;
        private const int MaxStageLayerReports = 10;

        private enum Severity { Error, Warning, Info }

        private class Island
        {
            public string Name;
            public Transform Pivot;
        }

        // 비교 단위 하나 (프리팹 루트, 프리팹이 아니면 렌더러가 붙은 가장 위 오브젝트)
        private class Unit
        {
            public GameObject Root;
            public Island Island;
            public Vector3 RelativePosition;   // 섬 중심(Pivot) 기준
            public string Shape;               // 메시·머티리얼·자식 구성
        }

        // 메시 하나의 꼭짓점 위치, UV(없으면 null), 서브메시별 삼각형 인덱스
        private class MeshPoints
        {
            public Vector3[] Vertices;
            public Vector2[] Uvs;
            public int[][] SubMeshTriangles;
        }

        // 루트 기준 삼각형 하나와, 색을 읽을 텍스처 정보
        private class Triangle
        {
            public Vector3 A, B, C;
            public Vector2 UvA, UvB, UvC;
            public Texture2D Texture;      // 없으면 null (색은 흰색)
            public Vector2 Scale, Offset;  // 머티리얼의 Tiling·Offset
        }

        // 광선 하나의 결과: 맞았는지, 가장 바깥 면까지의 거리, 그 자리의 색
        private struct RaySample
        {
            public bool Hit;
            public float Distance;
            public Color Color;
        }

        private static Vector3[] rayDirections;   // 피보나치 구 방향 (처음 한 번만 계산)

        private static Island leftIsland;
        private static Island rightIsland;
        private static int errorCount;
        private static int warningCount;
        private static int infoCount;
        private static readonly Dictionary<Mesh, MeshPoints> meshCache = new Dictionary<Mesh, MeshPoints>();
        private static readonly Dictionary<Texture, Texture2D> readableTextures = new Dictionary<Texture, Texture2D>();
        private static readonly HashSet<Material> warnedMaterials = new HashSet<Material>();

        [MenuItem(MenuPath)]
        private static void Validate()
        {
            if (EditorApplication.isPlaying)
            {
                EditorUtility.DisplayDialog(Title, "Play 모드에서는 검사할 수 없습니다.", "OK");
                return;
            }

            Scene scene = SceneManager.GetActiveScene();
            errorCount = warningCount = infoCount = 0;
            meshCache.Clear();
            warnedMaterials.Clear();
            Debug.Log($"{LogPrefix} {scene.name} 검사를 시작합니다.");

            if (TryFindIslands(scene, out Camera[] cameras))
            {
                Transform props = FindRoot(scene, PropsRootName);
                if (props == null) Report(Severity.Error, $"씬에 '{PropsRootName}' 루트 그룹이 없습니다.");

                List<PlayArea> areas = FindInScene<PlayArea>(scene);
                List<AnswerPair> pairs = FindInScene<AnswerPair>(scene).OrderBy(pair => pair.name).ToList();

                CheckPairCount(pairs);
                foreach (AnswerPair pair in pairs) CheckPair(pair, areas);
                CheckOrphanTargets(scene, pairs);
                if (props != null) CheckNonAnswerUnits(props);
                CheckStageLayers(scene);
                CheckCameras(cameras);
                CheckLights(scene);
            }

            // 대칭 판단에 쓴 텍스처 임시 복사본을 지웁니다.
            foreach (Texture2D copy in readableTextures.Values) Object.DestroyImmediate(copy);
            readableTextures.Clear();

            Finish(scene);
        }

        #region 섬과 카메라 찾기

        // OrbitCameraController의 Views에서 Pivot 2개와 카메라 2대를 찾습니다. Pivot이 없으면 이름으로 찾습니다.
        private static bool TryFindIslands(Scene scene, out Camera[] cameras)
        {
            leftIsland = rightIsland = null;
            cameras = new Camera[0];

            var pivots = new List<Transform>();
            var cams = new List<Camera>();
            OrbitCameraController orbit = FindInScene<OrbitCameraController>(scene).FirstOrDefault();

            if (orbit != null)
            {
                // views는 private 필드라서 SerializedObject로 읽습니다.
                SerializedProperty views = new SerializedObject(orbit).FindProperty("views");
                for (int i = 0; views != null && i < views.arraySize; i++)
                {
                    SerializedProperty view = views.GetArrayElementAtIndex(i);
                    if (view.FindPropertyRelative("pivot").objectReferenceValue is Transform pivot) pivots.Add(pivot);
                    if (view.FindPropertyRelative("camera").objectReferenceValue is Transform camTransform
                        && camTransform.TryGetComponent(out Camera cam)) cams.Add(cam);
                }
            }

            if (pivots.Count != 2)
            {
                pivots = scene.GetRootGameObjects()
                    .SelectMany(go => go.GetComponentsInChildren<Transform>(true))
                    .Where(t => t.name == LeftPivotName || t.name == RightPivotName)
                    .ToList();
            }

            if (pivots.Count != 2)
            {
                Report(Severity.Error, $"섬 중심(Pivot)을 2개 찾지 못했습니다(찾은 개수 {pivots.Count}). OrbitCameraController의 Views를 확인해 주세요.", orbit);
                return false;
            }

            pivots = pivots.OrderBy(pivot => pivot.position.x).ToList();
            leftIsland = new Island { Name = "왼쪽", Pivot = pivots[0] };
            rightIsland = new Island { Name = "오른쪽", Pivot = pivots[1] };

            if (cams.Count == 2) cameras = cams.ToArray();
            else Report(Severity.Warning, "OrbitCameraController의 Views에서 카메라 2대를 찾지 못해 카메라 대칭 검사를 건너뜁니다.", orbit);

            return true;
        }

        // 더 가까운 Pivot 쪽 섬으로 판단합니다.
        private static Island GetIsland(Vector3 worldPoint)
        {
            float toLeft = Mathf.Abs(worldPoint.x - leftIsland.Pivot.position.x);
            float toRight = Mathf.Abs(worldPoint.x - rightIsland.Pivot.position.x);
            return toLeft <= toRight ? leftIsland : rightIsland;
        }

        #endregion

        #region 정답 쌍

        private static void CheckPairCount(List<AnswerPair> pairs)
        {
            if (pairs.Count == 0)
            {
                Report(Severity.Error, "정답 쌍(AnswerPair)이 하나도 없습니다. 이 스테이지는 클리어할 수 없습니다.");
            }
            else if (pairs.Count < MinPairs || pairs.Count > MaxPairs)
            {
                Report(Severity.Warning, $"정답 쌍이 {pairs.Count}개입니다. 기획서 기준은 {MinPairs}~{MaxPairs}개입니다.");
            }
        }

        private static void CheckPair(AnswerPair pair, List<PlayArea> areas)
        {
            AnswerTarget a = pair.TargetA;
            AnswerTarget b = pair.TargetB;
            if (a == null || b == null)
            {
                string empty = a == null && b == null ? "A와 B" : a == null ? "A" : "B";
                Report(Severity.Error, $"{pair.name}의 Target {empty}가 비어 있습니다.", pair);
                return;
            }

            Island islandA = GetIsland(a.transform.position);
            Island islandB = GetIsland(b.transform.position);
            if (islandA == islandB)
            {
                Report(Severity.Error, $"{pair.name}의 A({a.name})와 B({b.name})가 둘 다 {islandA.Name} 섬에 있습니다.", pair);
            }

            CheckClickable(pair, a, areas);
            CheckClickable(pair, b, areas);
            CheckTwin(pair, a.gameObject, b.gameObject);
            WarnIfNoEmission(pair, a.gameObject);
            WarnIfNoEmission(pair, b.gameObject);
        }

        // 클릭에 걸리는지 확인합니다. (Collider, 레이어, PlayArea)
        private static void CheckClickable(AnswerPair pair, AnswerTarget target, List<PlayArea> areas)
        {
            GameObject go = target.gameObject;
            string who = $"{pair.name}의 {go.name}";

            if (go.activeInHierarchy == false) Report(Severity.Error, $"{who}이(가) 꺼져 있어 보이지 않습니다.", go);

            Collider col = go.GetComponent<Collider>();
            if (col == null) Report(Severity.Error, $"{who}에 Collider가 없어 클릭할 수 없습니다.", go);
            else if (col.enabled == false) Report(Severity.Error, $"{who}의 {col.GetType().Name}이(가) 꺼져 있어 클릭할 수 없습니다.", go);
            else if (col.isTrigger) Report(Severity.Error, $"{who}의 {col.GetType().Name}이(가) Trigger라서 클릭할 수 없습니다.", go);

            int backgroundLayer = LayerMask.NameToLayer(BackgroundLayerName);
            if (backgroundLayer >= 0)
            {
                if (go.layer == backgroundLayer)
                {
                    Report(Severity.Error, $"{who}이(가) {BackgroundLayerName} 레이어라서 클릭해도 반응하지 않습니다.", go);
                }
                else
                {
                    Collider silent = go.GetComponentsInChildren<Collider>(true).FirstOrDefault(c => c.gameObject.layer == backgroundLayer);
                    if (silent != null) Report(Severity.Error, $"{who}의 자식 {silent.name}이(가) {BackgroundLayerName} 레이어라서 그 부분은 반응하지 않습니다.", silent);
                }
            }

            if (areas.Count > 0)
            {
                Vector3 center = GetCenter(go);
                if (areas.Any(area => area.Contains(center)) == false)
                {
                    Report(Severity.Error, $"{who}이(가) 어느 PlayArea에도 없어 클릭해도 반응하지 않습니다.", go);
                }
            }
        }

        // 쌍둥이가 화면에서 정말 똑같이 보이는지 확인합니다.
        private static void CheckTwin(AnswerPair pair, GameObject a, GameObject b)
        {
            Unit unitA = MakeUnit(a);
            Unit unitB = MakeUnit(b);
            var problems = new List<string>();

            float distance = Vector3.Distance(unitA.RelativePosition, unitB.RelativePosition);
            if (distance > PositionTolerance)
            {
                problems.Add($"섬 중심 기준 위치가 {distance:0.###} 다름 (A {Format(unitA.RelativePosition)}, B {Format(unitB.RelativePosition)})");
            }
            if (SameScale(a.transform, b.transform) == false)
            {
                problems.Add($"크기가 다름 (A {Format(a.transform.lossyScale)}, B {Format(b.transform.lossyScale)})");
            }
            if (unitA.Shape != unitB.Shape)
            {
                problems.Add("메시·머티리얼·자식 구성이 다름");
            }

            float angle = Quaternion.Angle(a.transform.rotation, b.transform.rotation);
            if (angle > RotationTolerance)
            {
                string note = "";
                if (problems.Count == 0 && IsRotationallySymmetric(a.transform, b.transform, out note))
                {
                    Report(Severity.Info, $"{pair.name}: 회전이 {angle:0}° 다르지만 모양이 대칭이라 똑같아 보입니다. ({a.name})", a);
                }
                else
                {
                    problems.Add($"회전이 {angle:0.#}° 다름{note}");
                }
            }

            if (problems.Count > 0)
            {
                Report(Severity.Error, $"{pair.name}의 쌍둥이({a.name}, {b.name})가 다르게 보입니다: {string.Join(", ", problems)}", a);
            }
        }

        // 빌드에서 _EMISSION 셰이더 변형이 빠지지 않도록 정답 머티리얼은 Emission을 켜 둡니다. (같은 머티리얼은 한 번만 알림)
        private static void WarnIfNoEmission(AnswerPair pair, GameObject go)
        {
            IEnumerable<Material> materials = go.GetComponentsInChildren<Renderer>(true)
                .SelectMany(renderer => renderer.sharedMaterials)
                .Where(mat => mat != null)
                .Distinct();

            foreach (Material mat in materials)
            {
                if (mat.IsKeywordEnabled("_EMISSION") || warnedMaterials.Add(mat) == false) continue;
                Report(Severity.Warning, $"{pair.name}의 {go.name}이(가) 쓰는 머티리얼 {mat.name}에 Emission이 꺼져 있습니다. 빌드에서 반짝임이 약해질 수 있으니 Emission을 켜고 색을 검정으로 두세요.", mat);
            }
        }

        // 어느 쌍에도 등록되지 않은 AnswerTarget은 클릭해도 정답·오답 어느 쪽도 아닌 이상한 물체가 됩니다.
        private static void CheckOrphanTargets(Scene scene, List<AnswerPair> pairs)
        {
            foreach (AnswerTarget target in FindInScene<AnswerTarget>(scene))
            {
                if (pairs.Any(pair => pair.TargetA == target || pair.TargetB == target)) continue;
                Report(Severity.Warning, $"{target.name}에 AnswerTarget이 붙어 있지만 어느 정답 쌍에도 등록되지 않았습니다.", target);
            }
        }

        #endregion

        #region 정답이 아닌 물체

        // 의도하지 않은 쌍둥이(똑같아 보이는데 정답이 아님)와 함정 후보(거의 같음)를 찾습니다.
        private static void CheckNonAnswerUnits(Transform props)
        {
            List<Unit> units = CollectUnits(props);
            List<Unit> lefts = units.Where(unit => unit.Island == leftIsland).ToList();
            List<Unit> rights = units.Where(unit => unit.Island == rightIsland).ToList();

            foreach (Unit left in lefts)
            {
                foreach (Unit right in rights)
                {
                    if (left.Shape != right.Shape) continue;

                    float distance = Vector3.Distance(left.RelativePosition, right.RelativePosition);
                    if (distance > TrapDistance) continue;

                    Transform l = left.Root.transform;
                    Transform r = right.Root.transform;
                    bool sameScale = SameScale(l, r);
                    float angle = Quaternion.Angle(l.rotation, r.rotation);
                    bool sameRotation = angle <= RotationTolerance;
                    bool looksSame = distance <= PositionTolerance && sameScale
                        && (sameRotation || IsRotationallySymmetric(l, r, out _));

                    if (looksSame)
                    {
                        string reason = sameRotation ? "" : $" (회전이 {angle:0}° 다르지만 대칭이라 똑같아 보임)";
                        Report(Severity.Error, $"의도하지 않은 쌍둥이: {l.name}(왼쪽)과 {r.name}(오른쪽)이 똑같아 보이지만 정답 쌍이 아닙니다{reason}. 클릭하면 오답이 되므로 정답으로 등록하거나 다르게 바꿔 주세요.", l);
                    }
                    else
                    {
                        var diffs = new List<string>();
                        if (distance > PositionTolerance) diffs.Add($"위치 {distance:0.###} 차이");
                        if (sameRotation == false) diffs.Add($"회전 {angle:0.#}° 차이");
                        if (sameScale == false) diffs.Add("크기 차이");
                        Report(Severity.Info, $"함정 후보: {l.name}(왼쪽)과 {r.name}(오른쪽)이 거의 같습니다 ({string.Join(", ", diffs)}). 의도한 차이인지 확인해 주세요.", l);
                    }
                }
            }
        }

        // Props 아래에서 정답이 아닌 비교 단위를 모읍니다.
        private static List<Unit> CollectUnits(Transform props)
        {
            var roots = new HashSet<GameObject>();
            foreach (Renderer renderer in props.GetComponentsInChildren<Renderer>())
            {
                if (renderer.enabled == false) continue;
                if (renderer.GetComponentInParent<AnswerTarget>(true) != null) continue;

                GameObject root = GetUnitRoot(renderer.transform, props);
                if (root.GetComponentInChildren<AnswerTarget>(true) != null) continue;
                roots.Add(root);
            }
            return roots.Select(MakeUnit).ToList();
        }

        private static GameObject GetUnitRoot(Transform t, Transform stop)
        {
            GameObject prefabRoot = PrefabUtility.GetOutermostPrefabInstanceRoot(t.gameObject);
            if (prefabRoot != null && prefabRoot.transform != stop && prefabRoot.transform.IsChildOf(stop)) return prefabRoot;

            // 프리팹이 아니면 렌더러가 붙은 가장 위 오브젝트까지 올라갑니다.
            Transform current = t;
            while (current.parent != null && current.parent != stop && current.parent.GetComponent<Renderer>() != null)
            {
                current = current.parent;
            }
            return current.gameObject;
        }

        #endregion

        #region 바닥 레이어

        // Stage 아래 Collider가 Background가 아니면 바닥을 클릭할 때 X가 뜹니다.
        private static void CheckStageLayers(Scene scene)
        {
            Transform stage = FindRoot(scene, StageRootName);
            int backgroundLayer = LayerMask.NameToLayer(BackgroundLayerName);
            if (stage == null || backgroundLayer < 0) return;

            List<Collider> loud = stage.GetComponentsInChildren<Collider>()
                .Where(col => col.enabled && col.isTrigger == false && col.gameObject.layer != backgroundLayer)
                .ToList();

            foreach (Collider col in loud.Take(MaxStageLayerReports))
            {
                Report(Severity.Warning, $"Stage 아래 {col.name}의 Collider가 {LayerMask.LayerToName(col.gameObject.layer)} 레이어라서 클릭하면 X가 뜹니다. {BackgroundLayerName} 레이어로 바꿔 주세요.", col);
            }
            if (loud.Count > MaxStageLayerReports)
            {
                Report(Severity.Warning, $"Stage 아래에 같은 문제가 {loud.Count - MaxStageLayerReports}개 더 있습니다.", stage);
            }
        }

        #endregion

        #region 양쪽 대칭 (카메라, 조명)

        private static void CheckCameras(Camera[] cameras)
        {
            if (cameras.Length != 2) return;

            Camera a = cameras[0];
            Camera b = cameras[1];
            var diffs = new List<string>();

            if (a.clearFlags != b.clearFlags) diffs.Add($"Background Type ({a.clearFlags} / {b.clearFlags})");

            // 배경색은 정답 비교에 영향이 없고, 1-2처럼 테마마다 일부러 다르게 둘 수 있어서 참고로만 알립니다.
            if (a.clearFlags == b.clearFlags && a.clearFlags == CameraClearFlags.SolidColor && a.backgroundColor != b.backgroundColor)
            {
                Report(Severity.Info, $"두 카메라({a.name}, {b.name})의 배경색이 다릅니다. 테마마다 일부러 다르게 둔 것이면 괜찮습니다.", a);
            }
            if (a.orthographic != b.orthographic) diffs.Add("Projection");
            if (Mathf.Abs(a.fieldOfView - b.fieldOfView) > 0.01f) diffs.Add($"Field of View ({a.fieldOfView} / {b.fieldOfView})");
            if (Mathf.Abs(a.nearClipPlane - b.nearClipPlane) > 0.001f) diffs.Add($"Near ({a.nearClipPlane} / {b.nearClipPlane})");
            if (Mathf.Abs(a.farClipPlane - b.farClipPlane) > 0.001f) diffs.Add($"Far ({a.farClipPlane} / {b.farClipPlane})");
            if (a.cullingMask != b.cullingMask) diffs.Add("Culling Mask");
            if (Mathf.Abs(a.rect.width - b.rect.width) > 0.001f || Mathf.Abs(a.rect.height - b.rect.height) > 0.001f) diffs.Add("Viewport 크기");

            if (diffs.Count > 0)
            {
                Report(Severity.Warning, $"두 카메라({a.name}, {b.name}) 설정이 다릅니다: {string.Join(", ", diffs)}. 한쪽만 다르게 보일 수 있습니다.", a);
            }
        }

        // 섬마다 따로 둔 조명(Point·Spot 등)이 같은 자리에 같은 값으로 있는지 봅니다. (Directional은 양쪽 공용이라 제외)
        private static void CheckLights(Scene scene)
        {
            List<Light> lights = FindInScene<Light>(scene)
                .Where(light => light.isActiveAndEnabled && light.type != LightType.Directional)
                .ToList();
            List<Light> lefts = lights.Where(light => GetIsland(light.transform.position) == leftIsland).ToList();
            List<Light> rights = lights.Where(light => GetIsland(light.transform.position) == rightIsland).ToList();
            var matched = new HashSet<Light>();

            foreach (Light left in lefts)
            {
                Vector3 relative = RelativeTo(left.transform, leftIsland);
                Light right = rights
                    .Where(light => matched.Contains(light) == false)
                    .OrderBy(light => Vector3.Distance(RelativeTo(light.transform, rightIsland), relative))
                    .FirstOrDefault();

                if (right == null || Vector3.Distance(RelativeTo(right.transform, rightIsland), relative) > PositionTolerance)
                {
                    Report(Severity.Warning, $"조명 {left.name}(왼쪽)과 같은 자리의 조명이 오른쪽에 없습니다. 밝기·그림자 차이가 오답 단서처럼 보일 수 있습니다.", left);
                    continue;
                }
                matched.Add(right);

                var diffs = new List<string>();
                if (left.type != right.type) diffs.Add("종류");
                if (left.color != right.color) diffs.Add("색");
                if (Mathf.Abs(left.intensity - right.intensity) > 0.001f) diffs.Add($"Intensity ({left.intensity} / {right.intensity})");
                if (Mathf.Abs(left.range - right.range) > 0.001f) diffs.Add($"Range ({left.range} / {right.range})");
                if (left.type == LightType.Spot && (Mathf.Abs(left.spotAngle - right.spotAngle) > 0.01f || Mathf.Abs(left.innerSpotAngle - right.innerSpotAngle) > 0.01f)) diffs.Add("Spot Angle");
                if (left.shadows != right.shadows) diffs.Add("그림자");
                if (Quaternion.Angle(left.transform.rotation, right.transform.rotation) > RotationTolerance) diffs.Add("방향");

                if (diffs.Count > 0)
                {
                    Report(Severity.Warning, $"조명 {left.name}(왼쪽)과 {right.name}(오른쪽)의 설정이 다릅니다: {string.Join(", ", diffs)}.", left);
                }
            }

            foreach (Light right in rights.Where(light => matched.Contains(light) == false))
            {
                Report(Severity.Warning, $"조명 {right.name}(오른쪽)과 같은 자리의 조명이 왼쪽에 없습니다.", right);
            }
        }

        #endregion

        #region 모양 비교

        private static Unit MakeUnit(GameObject root)
        {
            Island island = GetIsland(root.transform.position);
            return new Unit
            {
                Root = root,
                Island = island,
                RelativePosition = RelativeTo(root.transform, island),
                Shape = GetShape(root.transform),
            };
        }

        // 렌더러마다 (루트 기준 경로, 메시, 머티리얼, 루트 기준 위치·회전·크기)를 이어 붙인 문자열입니다.
        private static string GetShape(Transform root)
        {
            IEnumerable<string> parts = root.GetComponentsInChildren<Renderer>()
                .Where(renderer => renderer.enabled)
                .Select(renderer =>
                {
                    Transform t = renderer.transform;
                    MeshFilter meshFilter = renderer.GetComponent<MeshFilter>();
                    string mesh = meshFilter != null && meshFilter.sharedMesh != null ? meshFilter.sharedMesh.name : "-";
                    string materials = string.Join(",", renderer.sharedMaterials.Select(mat => mat != null ? mat.name : "-"));
                    if (t == root) return $"/[{mesh}][{materials}]";

                    Vector3 position = root.InverseTransformPoint(t.position);
                    Vector3 euler = (Quaternion.Inverse(root.rotation) * t.rotation).eulerAngles;
                    Vector3 scale = new Vector3(
                        t.lossyScale.x / root.lossyScale.x,
                        t.lossyScale.y / root.lossyScale.y,
                        t.lossyScale.z / root.lossyScale.z);
                    return $"{AnimationUtility.CalculateTransformPath(t, root)}[{mesh}][{materials}]"
                        + $"[{Round(position, 100f)}][{RoundAngles(euler)}][{Round(scale, 100f)}]";
                });
            return string.Join("|", parts);
        }

        // b가 a를 돌려 놓은 것과 똑같아 보이는지 판단합니다.
        // a의 기준점에서 사방으로 광선을 쏴서, 모든 방향 d에서 d와 (돌린 만큼 돌아간) Δd의 거리·색이 같으면 똑같아 보입니다.
        // 메시를 삼각형으로 어떻게 나눴는지와 상관없이 겉모습(모양 + 색)만으로 판단합니다.
        private static bool IsRotationallySymmetric(Transform a, Transform b, out string note)
        {
            note = "";
            List<Triangle> triangles = GetLocalTriangles(a, out float size);
            if (triangles == null)
            {
                note = " (메시를 읽지 못해 대칭 여부를 판단할 수 없음)";
                return false;
            }

            float distanceTolerance = Mathf.Max(size * SymmetryTolerance, 0.0001f);
            Quaternion delta = Quaternion.Inverse(a.rotation) * b.rotation;

            foreach (Vector3 direction in GetRayDirections())
            {
                RaySample first = CastRay(triangles, direction);
                RaySample second = CastRay(triangles, delta * direction);

                if (first.Hit != second.Hit) return false;
                if (first.Hit == false) continue;
                if (Mathf.Abs(first.Distance - second.Distance) > distanceTolerance) return false;
                if (SameColor(first.Color, second.Color) == false) return false;
            }
            return true;
        }

        // 루트 기준 삼각형들입니다. 서브메시마다 그 머티리얼의 텍스처를 붙여 둡니다.
        // size는 전체 크기(가장 긴 변)입니다. 메시를 못 읽으면 null입니다.
        private static List<Triangle> GetLocalTriangles(Transform root, out float size)
        {
            size = 0f;
            var triangles = new List<Triangle>();
            Matrix4x4 toRoot = root.worldToLocalMatrix;
            bool hasBounds = false;
            Bounds bounds = default;

            foreach (MeshFilter meshFilter in root.GetComponentsInChildren<MeshFilter>())
            {
                if (meshFilter.sharedMesh == null) continue;

                MeshPoints data = GetMeshPoints(meshFilter.sharedMesh);
                if (data == null || data.Vertices.Length == 0) return null;

                Renderer renderer = meshFilter.GetComponent<Renderer>();
                Material[] materials = renderer != null ? renderer.sharedMaterials : new Material[0];
                Matrix4x4 matrix = toRoot * meshFilter.transform.localToWorldMatrix;

                var vertices = new Vector3[data.Vertices.Length];
                for (int i = 0; i < vertices.Length; i++)
                {
                    vertices[i] = matrix.MultiplyPoint3x4(data.Vertices[i]);
                    if (hasBounds) bounds.Encapsulate(vertices[i]);
                    else
                    {
                        bounds = new Bounds(vertices[i], Vector3.zero);
                        hasBounds = true;
                    }
                }

                // 서브메시마다 그 서브메시에 칠해지는 머티리얼의 텍스처에서 색을 읽습니다.
                for (int s = 0; s < data.SubMeshTriangles.Length; s++)
                {
                    Material material = s < materials.Length ? materials[s] : null;
                    Texture2D readable = material != null && material.mainTexture != null && data.Uvs != null
                        ? GetReadableTexture(material.mainTexture)
                        : null;

                    int[] indices = data.SubMeshTriangles[s];
                    for (int t = 0; t + 2 < indices.Length; t += 3)
                    {
                        var triangle = new Triangle
                        {
                            A = vertices[indices[t]],
                            B = vertices[indices[t + 1]],
                            C = vertices[indices[t + 2]],
                            Texture = readable,
                        };
                        if (readable != null)
                        {
                            triangle.UvA = data.Uvs[indices[t]];
                            triangle.UvB = data.Uvs[indices[t + 1]];
                            triangle.UvC = data.Uvs[indices[t + 2]];
                            triangle.Scale = material.mainTextureScale;
                            triangle.Offset = material.mainTextureOffset;
                        }
                        triangles.Add(triangle);
                    }
                }
            }
            if (triangles.Count == 0) return null;

            size = Mathf.Max(bounds.size.x, bounds.size.y, bounds.size.z);
            return triangles;
        }

        // 기준점(원점)에서 direction 쪽으로 광선을 쏴서 가장 먼 면(바깥에서 보이는 면)을 찾습니다. (양면, Möller–Trumbore)
        private static RaySample CastRay(List<Triangle> triangles, Vector3 direction)
        {
            var result = new RaySample();
            Triangle best = null;
            float bestU = 0f;
            float bestV = 0f;

            foreach (Triangle triangle in triangles)
            {
                Vector3 edge1 = triangle.B - triangle.A;
                Vector3 edge2 = triangle.C - triangle.A;
                Vector3 p = Vector3.Cross(direction, edge2);
                float det = Vector3.Dot(edge1, p);
                if (Mathf.Abs(det) < 1e-9f) continue;

                float inverse = 1f / det;
                Vector3 s = -triangle.A;   // 광선 시작점(원점) - A
                float u = Vector3.Dot(s, p) * inverse;
                if (u < 0f || u > 1f) continue;

                Vector3 q = Vector3.Cross(s, edge1);
                float v = Vector3.Dot(direction, q) * inverse;
                if (v < 0f || u + v > 1f) continue;

                float distance = Vector3.Dot(edge2, q) * inverse;
                if (distance <= 0f || (result.Hit && distance <= result.Distance)) continue;

                result.Hit = true;
                result.Distance = distance;
                best = triangle;
                bestU = u;
                bestV = v;
            }

            result.Color = Color.white;
            if (best != null && best.Texture != null)
            {
                Vector2 uv = best.UvA * (1f - bestU - bestV) + best.UvB * bestU + best.UvC * bestV;
                result.Color = best.Texture.GetPixelBilinear(
                    uv.x * best.Scale.x + best.Offset.x,
                    uv.y * best.Scale.y + best.Offset.y);
            }
            return result;
        }

        // 구 위에 고르게 퍼진 방향들입니다. (피보나치 구, 처음 한 번만 계산)
        private static Vector3[] GetRayDirections()
        {
            if (rayDirections != null) return rayDirections;

            rayDirections = new Vector3[RayDirections];
            float goldenAngle = Mathf.PI * (3f - Mathf.Sqrt(5f));
            for (int i = 0; i < RayDirections; i++)
            {
                float y = 1f - 2f * (i + 0.5f) / RayDirections;
                float radius = Mathf.Sqrt(1f - y * y);
                float angle = goldenAngle * i;
                rayDirections[i] = new Vector3(Mathf.Cos(angle) * radius, y, Mathf.Sin(angle) * radius);
            }
            return rayDirections;
        }

        // Read/Write가 꺼진 FBX 메시도 에디터에서는 MeshData로 읽을 수 있습니다. 실패하면 null입니다.
        private static MeshPoints GetMeshPoints(Mesh mesh)
        {
            if (meshCache.TryGetValue(mesh, out MeshPoints cached)) return cached;

            MeshPoints result = null;
            try
            {
                if (mesh.isReadable)
                {
                    Vector2[] uvs = mesh.uv;
                    result = new MeshPoints
                    {
                        Vertices = mesh.vertices,
                        Uvs = uvs.Length > 0 ? uvs : null,
                        SubMeshTriangles = new int[mesh.subMeshCount][],
                    };
                    for (int s = 0; s < mesh.subMeshCount; s++) result.SubMeshTriangles[s] = mesh.GetTriangles(s);
                }
                else
                {
                    using (Mesh.MeshDataArray dataArray = Mesh.AcquireReadOnlyMeshData(mesh))
                    {
                        Mesh.MeshData data = dataArray[0];
                        result = new MeshPoints { SubMeshTriangles = new int[data.subMeshCount][] };
                        using (var vertices = new NativeArray<Vector3>(data.vertexCount, Allocator.Temp))
                        {
                            data.GetVertices(vertices);
                            result.Vertices = vertices.ToArray();
                        }
                        if (data.HasVertexAttribute(VertexAttribute.TexCoord0))
                        {
                            using (var uvs = new NativeArray<Vector2>(data.vertexCount, Allocator.Temp))
                            {
                                data.GetUVs(0, uvs);
                                result.Uvs = uvs.ToArray();
                            }
                        }
                        for (int s = 0; s < data.subMeshCount; s++)
                        {
                            // 삼각형이 아닌 서브메시(선·점)는 면이 없으므로 비워 둡니다.
                            SubMeshDescriptor subMesh = data.GetSubMesh(s);
                            if (subMesh.topology != MeshTopology.Triangles)
                            {
                                result.SubMeshTriangles[s] = new int[0];
                                continue;
                            }
                            using (var indices = new NativeArray<int>(subMesh.indexCount, Allocator.Temp))
                            {
                                data.GetIndices(indices, s);
                                result.SubMeshTriangles[s] = indices.ToArray();
                            }
                        }
                    }
                }
            }
            catch (System.Exception)
            {
                result = null;
            }

            meshCache[mesh] = result;
            return result;
        }

        // Read/Write가 꺼진 텍스처도 읽을 수 있도록 GPU로 그려서 임시 복사본을 만듭니다. (검사가 끝나면 지움)
        private static Texture2D GetReadableTexture(Texture texture)
        {
            if (readableTextures.TryGetValue(texture, out Texture2D cached)) return cached;

            RenderTexture renderTexture = RenderTexture.GetTemporary(texture.width, texture.height, 0, RenderTextureFormat.ARGB32);
            RenderTexture previous = RenderTexture.active;
            Graphics.Blit(texture, renderTexture);
            RenderTexture.active = renderTexture;

            var copy = new Texture2D(texture.width, texture.height, TextureFormat.RGBA32, false);
            copy.ReadPixels(new Rect(0, 0, texture.width, texture.height), 0, 0);
            copy.Apply();

            RenderTexture.active = previous;
            RenderTexture.ReleaseTemporary(renderTexture);

            readableTextures[texture] = copy;
            return copy;
        }

        private static bool SameColor(Color a, Color b)
        {
            return Mathf.Abs(a.r - b.r) <= ColorTolerance
                && Mathf.Abs(a.g - b.g) <= ColorTolerance
                && Mathf.Abs(a.b - b.b) <= ColorTolerance;
        }

        #endregion

        #region 도우미

        private static void Report(Severity severity, string message, Object context = null)
        {
            switch (severity)
            {
                case Severity.Error:
                    errorCount++;
                    Debug.LogError($"{LogPrefix} {message}", context);
                    break;
                case Severity.Warning:
                    warningCount++;
                    Debug.LogWarning($"{LogPrefix} {message}", context);
                    break;
                default:
                    infoCount++;
                    Debug.Log($"{LogPrefix} {message}", context);
                    break;
            }
        }

        private static void Finish(Scene scene)
        {
            string summary = $"{scene.name}: 오류 {errorCount}개, 경고 {warningCount}개, 참고 {infoCount}개";
            Debug.Log($"{LogPrefix} 검사 완료 - {summary}");

            string detail = errorCount + warningCount + infoCount == 0
                ? "문제가 없습니다."
                : "자세한 내용은 Console을 확인해 주세요. 로그를 클릭하면 하이어라키에서 위치가 표시됩니다.";
            EditorUtility.DisplayDialog(Title, $"{summary}\n\n{detail}", "OK");
        }

        private static Vector3 RelativeTo(Transform t, Island island)
        {
            return t.position - island.Pivot.position;
        }

        private static bool SameScale(Transform a, Transform b)
        {
            Vector3 sa = a.lossyScale;
            Vector3 sb = b.lossyScale;
            return SameValue(sa.x, sb.x) && SameValue(sa.y, sb.y) && SameValue(sa.z, sb.z);
        }

        private static bool SameValue(float a, float b)
        {
            return Mathf.Abs(a - b) <= ScaleTolerance * Mathf.Max(Mathf.Abs(a), Mathf.Abs(b), 0.0001f);
        }

        private static string Format(Vector3 v)
        {
            return $"({v.x:0.##}, {v.y:0.##}, {v.z:0.##})";
        }

        private static string Round(Vector3 v, float scale)
        {
            return $"{Mathf.Round(v.x * scale)},{Mathf.Round(v.y * scale)},{Mathf.Round(v.z * scale)}";
        }

        // 359.99°와 0°가 같은 값이 되도록 360으로 나눈 나머지를 씁니다.
        private static string RoundAngles(Vector3 euler)
        {
            return $"{Mathf.Round(euler.x) % 360f},{Mathf.Round(euler.y) % 360f},{Mathf.Round(euler.z) % 360f}";
        }

        private static Vector3 GetCenter(GameObject go)
        {
            Renderer[] renderers = go.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0) return go.transform.position;

            Bounds bounds = renderers[0].bounds;
            foreach (Renderer renderer in renderers) bounds.Encapsulate(renderer.bounds);
            return bounds.center;
        }

        private static Transform FindRoot(Scene scene, string name)
        {
            return scene.GetRootGameObjects().FirstOrDefault(go => go.name == name)?.transform;
        }

        private static List<T> FindInScene<T>(Scene scene) where T : Component
        {
            return Object.FindObjectsByType<T>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                .Where(component => component.gameObject.scene == scene)
                .ToList();
        }

        #endregion
    }
}
