using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace YoureRight
{
    // 선택한 두 물체(왼쪽 섬 하나, 오른쪽 섬 하나)를 정답 쌍으로 만드는 에디터 도구 입니다.
    // 두 물체에 AnswerTarget과 Collider를 붙이고, Props 아래에 AnswerPair_NN을 만들어 연결합니다.
    // 조건에 맞지 않으면 아무것도 바꾸지 않고 이유를 알려 줍니다. 전체 작업은 Ctrl + Z 한 번으로 되돌릴 수 있습니다.
    public static class AnswerPairCreator
    {
        private const string MenuPath = "Tools/You're Right/Create Answer Pair";
        private const string Title = "Create Answer Pair";
        private const string PropsRootName = "Props";
        private const string PairNamePrefix = "AnswerPair_";
        private const string BackgroundLayerName = "Background";

        private static readonly Regex PairNumberRegex = new Regex(@"^AnswerPair_(\d+)$");

        [MenuItem(MenuPath)]
        private static void CreateAnswerPair()
        {
            if (TryValidate(out GameObject left, out GameObject right, out Transform props, out string error) == false)
            {
                Debug.LogError($"[AnswerPairCreator] {error}");
                EditorUtility.DisplayDialog(Title, error, "OK");
                return;
            }

            // 검사를 모두 통과한 뒤에만 씬을 바꿉니다. (아래 작업 전체를 Undo 한 번으로 묶음)
            Undo.SetCurrentGroupName(Title);
            int undoGroup = Undo.GetCurrentGroup();

            AnswerTarget targetA = PrepareTarget(left);
            AnswerTarget targetB = PrepareTarget(right);
            GameObject pairObject = CreatePairObject(left.scene, props, targetA, targetB);

            Undo.CollapseUndoOperations(undoGroup);
            EditorSceneManager.MarkSceneDirty(left.scene);
            Selection.activeGameObject = pairObject;

            Debug.Log($"[AnswerPairCreator] {pairObject.name} 생성: A = {left.name}, B = {right.name}", pairObject);
            WarnIfDifferent(left, right);
            WarnIfNoEmission(left);
            WarnIfNoEmission(right);
        }

        #region 검사

        // 선택한 두 물체가 정답 쌍이 될 수 있는지 확인하고, 왼쪽(A)/오른쪽(B) 순서로 돌려줍니다.
        private static bool TryValidate(out GameObject left, out GameObject right, out Transform props, out string error)
        {
            left = right = null;
            props = null;
            error = null;

            if (EditorApplication.isPlaying)
            {
                error = "Play 모드에서는 만들 수 없습니다.";
                return false;
            }

            GameObject[] selected = Selection.gameObjects;
            if (selected.Length != 2)
            {
                error = $"왼쪽 섬 물체 하나와 오른쪽 섬 물체 하나, 모두 2개를 선택해 주세요. (지금 {selected.Length}개)";
                return false;
            }

            GameObject a = selected[0];
            GameObject b = selected[1];

            if (EditorUtility.IsPersistent(a) || EditorUtility.IsPersistent(b))
            {
                error = "Project 창의 프리팹 에셋이 아니라 씬에 놓인 물체를 선택해 주세요.";
                return false;
            }
            if (a.scene != b.scene)
            {
                error = "두 물체가 서로 다른 씬에 있습니다.";
                return false;
            }
            if (a.transform.IsChildOf(b.transform) || b.transform.IsChildOf(a.transform))
            {
                error = $"{a.name}와(과) {b.name} 중 하나가 다른 하나의 자식입니다.";
                return false;
            }

            // 플랫폼은 대개 PlayArea 아래에 있으므로, 레이어를 먼저 봐야 실제 이유(Background)를 알려 줄 수 있습니다.
            if (CheckLayer(a, out error) == false) return false;
            if (CheckLayer(b, out error) == false) return false;

            if (TryOrderBySide(a, b, out left, out right, out error) == false) return false;

            List<AnswerPair> pairs = FindInScene<AnswerPair>(a.scene);
            if (CheckTarget(left, pairs, out error) == false) return false;
            if (CheckTarget(right, pairs, out error) == false) return false;

            props = a.scene.GetRootGameObjects().FirstOrDefault(go => go.name == PropsRootName)?.transform;
            if (props == null)
            {
                error = $"씬에 '{PropsRootName}' 루트 그룹이 없습니다. 하이어라키 구조를 먼저 맞춰 주세요.";
                return false;
            }

            return true;
        }

        // 두 물체가 서로 다른 섬에 있는지 확인하고 왼쪽 → 오른쪽 순서로 정렬합니다.
        // PlayArea가 있으면 어느 PlayArea 안에 있는지로, 없으면 x 좌표의 부호로 판단합니다.
        private static bool TryOrderBySide(GameObject a, GameObject b, out GameObject left, out GameObject right, out string error)
        {
            left = right = null;
            error = null;

            Vector3 pointA = GetCenter(a);
            Vector3 pointB = GetCenter(b);
            List<PlayArea> areas = FindInScene<PlayArea>(a.scene);

            if (areas.Count > 0)
            {
                PlayArea areaA = areas.FirstOrDefault(area => area.Contains(pointA));
                PlayArea areaB = areas.FirstOrDefault(area => area.Contains(pointB));

                if (areaA == null || areaB == null)
                {
                    error = $"{(areaA == null ? a.name : b.name)}이(가) 어느 PlayArea에도 없습니다. 클릭해도 반응하지 않는 위치입니다.";
                    return false;
                }
                if (areaA == areaB)
                {
                    error = $"{a.name}와(과) {b.name}이(가) 같은 섬({areaA.name})에 있습니다. 섬마다 하나씩 선택해 주세요.";
                    return false;
                }
            }
            else if (pointA.x * pointB.x >= 0f)
            {
                error = $"{a.name}와(과) {b.name}이(가) 같은 쪽(x 부호가 같음)에 있습니다. 섬마다 하나씩 선택해 주세요.";
                return false;
            }

            bool aIsLeft = pointA.x < pointB.x;
            left = aIsLeft ? a : b;
            right = aIsLeft ? b : a;
            return true;
        }

        // 이미 다른 쌍에 등록되어 있으면 거부합니다.
        private static bool CheckTarget(GameObject go, List<AnswerPair> pairs, out string error)
        {
            error = null;

            // 부모의 AnswerTarget은 클릭할 때 GetComponentInParent로 잡히고, 자식의 AnswerTarget은 이 물체와 겹칩니다.
            AnswerTarget[] related = go.GetComponentsInParent<AnswerTarget>(true)
                .Concat(go.GetComponentsInChildren<AnswerTarget>(true))
                .Distinct()
                .ToArray();

            foreach (AnswerTarget target in related)
            {
                AnswerPair owner = pairs.FirstOrDefault(pair => pair.TargetA == target || pair.TargetB == target);
                if (owner != null)
                {
                    error = $"{go.name}은(는) 이미 {owner.name}에 등록된 {target.name}과(와) 겹칩니다.";
                    return false;
                }
                if (target.gameObject != go)
                {
                    error = $"{go.name}의 {(target.transform.IsChildOf(go.transform) ? "자식" : "부모")} {target.name}에 AnswerTarget이 붙어 있습니다. 먼저 정리해 주세요.";
                    return false;
                }
            }

            return true;
        }

        // 클릭해도 반응하지 않는 레이어(Background)면 거부합니다. (자식 Collider 포함)
        private static bool CheckLayer(GameObject go, out string error)
        {
            error = null;

            int backgroundLayer = LayerMask.NameToLayer(BackgroundLayerName);
            if (backgroundLayer >= 0)
            {
                if (go.layer == backgroundLayer)
                {
                    error = $"{go.name}이(가) {BackgroundLayerName} 레이어라서 클릭해도 반응하지 않습니다.";
                    return false;
                }

                Collider silent = go.GetComponentsInChildren<Collider>(true).FirstOrDefault(col => col.gameObject.layer == backgroundLayer);
                if (silent != null)
                {
                    error = $"{go.name}의 자식 {silent.name}이(가) {BackgroundLayerName} 레이어라서 그 부분은 클릭해도 반응하지 않습니다.";
                    return false;
                }
            }

            return true;
        }

        #endregion

        #region 생성

        // 루트에 클릭용 Collider와 AnswerTarget을 준비합니다. (AnswerTarget은 같은 오브젝트에 Collider가 필요함)
        private static AnswerTarget PrepareTarget(GameObject go)
        {
            Collider col = go.GetComponent<Collider>();
            if (col == null)
            {
                MeshFilter meshFilter = go.GetComponent<MeshFilter>();
                if (meshFilter != null && meshFilter.sharedMesh != null)
                {
                    // 모양에 딱 맞게 MeshCollider를 붙입니다. (메시는 MeshFilter에서 자동으로 들어감)
                    Undo.AddComponent<MeshCollider>(go);
                }
                else
                {
                    // 메시가 자식에 나뉘어 있으면 자식 렌더러 전체를 감싸는 상자를 붙입니다.
                    BoxCollider box = Undo.AddComponent<BoxCollider>(go);
                    FitBoxToRenderers(box);
                }
            }
            else if (col.isTrigger || col.enabled == false)
            {
                // Raycast가 Trigger와 꺼진 Collider를 무시하므로 클릭에 걸리도록 고칩니다.
                Undo.RecordObject(col, Title);
                col.isTrigger = false;
                col.enabled = true;
                Debug.Log($"[AnswerPairCreator] {go.name}의 {col.GetType().Name}을(를) 클릭할 수 있게 켜고 Trigger를 껐습니다.", go);
            }

            AnswerTarget target = go.GetComponent<AnswerTarget>();
            if (target == null) target = Undo.AddComponent<AnswerTarget>(go);
            return target;
        }

        private static void FitBoxToRenderers(BoxCollider box)
        {
            Transform root = box.transform;
            Renderer[] renderers = root.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0)
            {
                Debug.LogWarning($"[AnswerPairCreator] {root.name}에 렌더러가 없어 기본 크기 BoxCollider를 붙였습니다. 크기를 직접 맞춰 주세요.", root);
                return;
            }

            // 월드 기준 Bounds의 꼭짓점 8개를 로컬 좌표로 옮겨서 로컬 상자를 만듭니다. (회전·스케일 반영)
            bool hasBounds = false;
            Bounds local = default;
            foreach (Renderer renderer in renderers)
            {
                Bounds world = renderer.bounds;
                for (int i = 0; i < 8; i++)
                {
                    Vector3 corner = world.center + Vector3.Scale(world.extents, new Vector3(
                        (i & 1) == 0 ? -1f : 1f,
                        (i & 2) == 0 ? -1f : 1f,
                        (i & 4) == 0 ? -1f : 1f));
                    Vector3 point = root.InverseTransformPoint(corner);

                    if (hasBounds) local.Encapsulate(point);
                    else
                    {
                        local = new Bounds(point, Vector3.zero);
                        hasBounds = true;
                    }
                }
            }

            box.center = local.center;
            box.size = local.size;
        }

        private static GameObject CreatePairObject(Scene scene, Transform props, AnswerTarget targetA, AnswerTarget targetB)
        {
            GameObject pairObject = new GameObject(GetNextPairName(scene));
            pairObject.transform.SetParent(props, false);

            // targetA/targetB는 private 필드라서 SerializedObject로 채웁니다.
            AnswerPair pair = pairObject.AddComponent<AnswerPair>();
            SerializedObject serialized = new SerializedObject(pair);
            serialized.FindProperty("targetA").objectReferenceValue = targetA;
            serialized.FindProperty("targetB").objectReferenceValue = targetB;
            serialized.ApplyModifiedPropertiesWithoutUndo();

            Undo.RegisterCreatedObjectUndo(pairObject, Title);
            return pairObject;
        }

        // 씬에 있는 AnswerPair_NN 중 가장 큰 번호 + 1을 씁니다. (중간 번호를 지워도 겹치지 않음)
        private static string GetNextPairName(Scene scene)
        {
            int max = 0;
            foreach (AnswerPair pair in FindInScene<AnswerPair>(scene))
            {
                Match match = PairNumberRegex.Match(pair.name);
                if (match.Success) max = Mathf.Max(max, int.Parse(match.Groups[1].Value));
            }
            return $"{PairNamePrefix}{max + 1:D2}";
        }

        #endregion

        #region 경고

        // 자세한 동일성 검사는 스테이지 검사 도구가 맡고, 여기서는 메시·머티리얼 구성만 간단히 비교합니다.
        private static void WarnIfDifferent(GameObject left, GameObject right)
        {
            if (GetSignature(left) != GetSignature(right))
            {
                Debug.LogWarning($"[AnswerPairCreator] {left.name}와(과) {right.name}의 메시나 머티리얼 구성이 다릅니다. 쌍둥이가 맞는지 확인해 주세요.", left);
            }
        }

        private static string GetSignature(GameObject go)
        {
            IEnumerable<string> parts = go.GetComponentsInChildren<Renderer>(true).Select(renderer =>
            {
                MeshFilter meshFilter = renderer.GetComponent<MeshFilter>();
                string mesh = meshFilter != null && meshFilter.sharedMesh != null ? meshFilter.sharedMesh.name : "-";
                string materials = string.Join(",", renderer.sharedMaterials.Select(mat => mat != null ? mat.name : "-"));
                return $"{mesh}[{materials}]";
            });
            return string.Join("|", parts);
        }

        // 빌드에서 _EMISSION 셰이더 변형이 빠지지 않도록 정답 머티리얼은 Emission을 켜 둡니다. (공용 머티리얼이라 직접 고치지는 않음)
        private static void WarnIfNoEmission(GameObject go)
        {
            IEnumerable<Material> materials = go.GetComponentsInChildren<Renderer>(true)
                .SelectMany(renderer => renderer.sharedMaterials)
                .Where(mat => mat != null)
                .Distinct();

            foreach (Material mat in materials)
            {
                if (mat.IsKeywordEnabled("_EMISSION")) continue;
                Debug.LogWarning($"[AnswerPairCreator] {go.name}의 머티리얼 {mat.name}에 Emission이 꺼져 있습니다. 빌드에서 반짝임이 약해질 수 있으니 Emission을 켜고 색을 검정으로 두세요.", mat);
            }
        }

        #endregion

        private static Vector3 GetCenter(GameObject go)
        {
            Renderer[] renderers = go.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0) return go.transform.position;

            Bounds bounds = renderers[0].bounds;
            foreach (Renderer renderer in renderers) bounds.Encapsulate(renderer.bounds);
            return bounds.center;
        }

        private static List<T> FindInScene<T>(Scene scene) where T : Component
        {
            return Object.FindObjectsByType<T>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                .Where(component => component.gameObject.scene == scene)
                .ToList();
        }
    }
}
