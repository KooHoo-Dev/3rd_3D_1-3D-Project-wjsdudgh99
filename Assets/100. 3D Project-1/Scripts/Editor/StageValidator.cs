using System.Collections.Generic;
using System.Linq;
using Unity.Collections;
using UnityEditor;
using UnityEngine;
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

        private static Island leftIsland;
        private static Island rightIsland;
        private static int errorCount;
        private static int warningCount;
        private static int infoCount;
        private static readonly Dictionary<Mesh, Vector3[]> vertexCache = new Dictionary<Mesh, Vector3[]>();
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
            vertexCache.Clear();
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
            else if (a.clearFlags == CameraClearFlags.SolidColor && a.backgroundColor != b.backgroundColor) diffs.Add("배경색");
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

        // b가 a를 돌려 놓은 것과 똑같아 보이는지, 꼭짓점을 그만큼 돌려서 자기 자신과 겹치는지로 판단합니다.
        private static bool IsRotationallySymmetric(Transform a, Transform b, out string note)
        {
            note = "";
            List<Vector3> points = GetLocalPoints(a, out float tolerance);
            if (points == null)
            {
                note = " (꼭짓점을 읽지 못해 대칭 여부를 판단할 수 없음)";
                return false;
            }

            var cells = new HashSet<Vector3Int>(points.Select(p => ToCell(p, tolerance)));
            Quaternion delta = Quaternion.Inverse(a.rotation) * b.rotation;

            foreach (Vector3 point in points)
            {
                if (HasNearbyCell(cells, ToCell(delta * point, tolerance)) == false) return false;
            }
            return true;
        }

        // 루트 기준 로컬 좌표의 꼭짓점들입니다. 메시를 하나라도 못 읽으면 null입니다.
        private static List<Vector3> GetLocalPoints(Transform root, out float tolerance)
        {
            tolerance = 0f;
            var points = new List<Vector3>();
            Matrix4x4 toRoot = root.worldToLocalMatrix;

            foreach (MeshFilter meshFilter in root.GetComponentsInChildren<MeshFilter>())
            {
                if (meshFilter.sharedMesh == null) continue;

                Vector3[] vertices = GetVertices(meshFilter.sharedMesh);
                if (vertices == null || vertices.Length == 0) return null;

                Matrix4x4 matrix = toRoot * meshFilter.transform.localToWorldMatrix;
                foreach (Vector3 vertex in vertices) points.Add(matrix.MultiplyPoint3x4(vertex));
            }
            if (points.Count == 0) return null;

            Bounds bounds = new Bounds(points[0], Vector3.zero);
            foreach (Vector3 point in points) bounds.Encapsulate(point);
            tolerance = Mathf.Max(Mathf.Max(bounds.size.x, bounds.size.y, bounds.size.z) * SymmetryTolerance, 0.0001f);
            return points;
        }

        // Read/Write가 꺼진 FBX 메시도 에디터에서는 MeshData로 읽을 수 있습니다. 실패하면 null입니다.
        private static Vector3[] GetVertices(Mesh mesh)
        {
            if (vertexCache.TryGetValue(mesh, out Vector3[] cached)) return cached;

            Vector3[] result = null;
            try
            {
                if (mesh.isReadable)
                {
                    result = mesh.vertices;
                }
                else
                {
                    using (Mesh.MeshDataArray dataArray = Mesh.AcquireReadOnlyMeshData(mesh))
                    {
                        Mesh.MeshData data = dataArray[0];
                        using (var buffer = new NativeArray<Vector3>(data.vertexCount, Allocator.Temp))
                        {
                            data.GetVertices(buffer);
                            result = buffer.ToArray();
                        }
                    }
                }
            }
            catch (System.Exception)
            {
                result = null;
            }

            vertexCache[mesh] = result;
            return result;
        }

        private static Vector3Int ToCell(Vector3 point, float size)
        {
            return new Vector3Int(Mathf.RoundToInt(point.x / size), Mathf.RoundToInt(point.y / size), Mathf.RoundToInt(point.z / size));
        }

        // 반올림 경계에 걸린 점도 찾도록 주변 27칸을 봅니다.
        private static bool HasNearbyCell(HashSet<Vector3Int> cells, Vector3Int cell)
        {
            for (int x = -1; x <= 1; x++)
            for (int y = -1; y <= 1; y++)
            for (int z = -1; z <= 1; z++)
            {
                if (cells.Contains(cell + new Vector3Int(x, y, z))) return true;
            }
            return false;
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
