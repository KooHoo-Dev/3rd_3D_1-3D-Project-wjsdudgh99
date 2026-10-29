using System.Collections.Generic;
using System.IO;
using System.Linq;
using Unity.Collections;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace YoureRight
{
    // Polyeler Simple Train Pack의 "Straight rail"을 잘라 두단식(막다른) 승강장용 선로 프리팹을 만드는 에디터 도구입니다.
    // Tools → You're Right → Build Terminal Rail
    //
    // 원본 조각은 메시 좌표로 z 0 ~ -101.76(길이 101.76, 복선, 선로 중심 x ±2.375)이고, 전차선 기둥이 z -90.85, -69.97, -49.47, -29.69, -11.66에 있습니다.
    // 이 중 -101.76 쪽 끝부터 기둥 2개(-90.85, -69.97)까지만 남깁니다. 선로·바닥은 TrackEnd에서, 전차선은 WireEnd에서 자르고(평면으로 잘라 UV를 보간),
    // 잘린 바닥 끝을 막는 뚜껑과 선로마다 차막이(車止め)를 붙입니다. 전차선은 원본 메시를 그대로 자른 것이라 위치가 원본과 같습니다.
    //
    // 기준점은 원본과 같은 자리(메시 z 0)라서, 씬의 Straight rail (1)과 같은 Transform으로 바꿔 놓으면 그대로 맞습니다.
    // 2-2에서 Straight rail (1)은 z 101.6(그룹 Scale 20 × 5.08)에 있으므로 선로 끝이 월드 z 34.2(플랫폼 끝)가 됩니다.
    // Collider는 붙이지 않습니다(원본처럼 무반응, 클릭 통과).
    public static class RailTerminalBuilder
    {
        private const string MenuPath = "Tools/You're Right/Build Terminal Rail";
        private const string Title = "Build Terminal Rail";
        private const string SourcePath = "Assets/Polyeler/Simple Train Pack/Prefabs/Rail/Straight rail.prefab";
        private const string PrefabPath = "Assets/100. 3D Project-1/Prefabs/Station/Straight rail Terminal.prefab";
        private const string MeshFolder = "Assets/100. 3D Project-1/Prefabs/Station/Meshes";
        private const string PropMaterialFolder = "Assets/100. 3D Project-1/Materials/Station";

        // 메시 좌표 (원본 조각의 -101.76 끝에서 시작)
        private const float TrackEnd = -67.4f;      // 선로·바닥 끝 (2-2 월드 z 34.2 = 플랫폼 끝)
        private const float WireEnd = -69.5f;       // 전차선 끝 (두 번째 기둥 -70.35 ~ -69.58 바로 뒤)
        private const float StopZ = -68.2f;         // 차막이 앞면
        private const float GroundBottom = -1.832f;
        private const float GroundTop = 0.16f;
        private const float GroundHalfWidth = 7.02f;
        private const float RailTop = 0.554f;
        private static readonly float[] TrackCenters = { -2.375f, 2.375f };
        private const float RailOffset = 0.55f;     // 선로 중심에서 레일까지 (레일 x ±1.84, ±2.94)

        private static Mesh cube;

        [MenuItem(MenuPath)]
        private static void Build()
        {
            if (EditorApplication.isPlaying)
            {
                EditorUtility.DisplayDialog(Title, "Play 모드에서는 만들 수 없습니다.", "OK");
                return;
            }

            var source = AssetDatabase.LoadAssetAtPath<GameObject>(SourcePath);
            if (source == null)
            {
                EditorUtility.DisplayDialog(Title, $"원본 프리팹이 없습니다.\n{SourcePath}", "OK");
                return;
            }

            if (AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath) != null &&
                EditorUtility.DisplayDialog(Title, "이미 만든 프리팹이 있습니다. 덮어쓸까요?", "덮어쓰기", "취소") == false)
            {
                return;
            }

            MeshRenderer trackSource = source.transform.Find("Straight rail").GetComponent<MeshRenderer>();
            MeshRenderer wireSource = source.transform.Find("wire straight ").GetComponent<MeshRenderer>();
            Material yellow = LoadMaterial("M_Prop_Yellow");
            Material black = LoadMaterial("M_Prop_Black");
            Material metal = LoadMaterial("M_Prop_Metal");
            Material lamp = LoadMaterial("M_Prop_LED_Red");
            if (yellow == null || black == null || metal == null || lamp == null)
            {
                EditorUtility.DisplayDialog(Title, "차막이에 쓰는 M_Prop_* 머티리얼이 없습니다. 먼저 Build Station Props를 실행해 주세요.", "OK");
                return;
            }

            EnsureFolder(MeshFolder);
            cube = Resources.GetBuiltinResource<Mesh>("Cube.fbx");

            Mesh track = SaveMesh(Clip(trackSource.GetComponent<MeshFilter>().sharedMesh, TrackEnd, "Rail Terminal Track"));
            Mesh wire = SaveMesh(Clip(wireSource.GetComponent<MeshFilter>().sharedMesh, WireEnd, "Rail Terminal Wire"));
            Mesh cap = SaveMesh(MakeCap(trackSource.GetComponent<MeshFilter>().sharedMesh));

            Scene preview = EditorSceneManager.NewPreviewScene();
            try
            {
                var root = new GameObject("Straight rail Terminal");
                SceneManager.MoveGameObjectToScene(root, preview);
                Part(root, "Straight rail", track, trackSource.sharedMaterials);
                Part(root, "wire straight", wire, wireSource.sharedMaterials);
                Part(root, "End Cap", cap, new[] { trackSource.sharedMaterials[0] });
                foreach (float center in TrackCenters)
                {
                    BuildBufferStop(root, center < 0f ? "Buffer Stop Left" : "Buffer Stop Right", center, yellow, black, metal, lamp);
                }

                PivotUtility.CenterGroupPivots(root);
                PrefabSaveUtility.SaveKeepingIds(root, PrefabPath);
            }
            finally
            {
                EditorSceneManager.ClosePreviewScene(preview);
            }

            AssetDatabase.SaveAssets();
            Debug.Log($"[RailTerminalBuilder] 두단식 선로 생성 완료: {PrefabPath}");
            EditorGUIUtility.PingObject(AssetDatabase.LoadAssetAtPath<Object>(PrefabPath));
        }

        #region 메시 자르기

        private struct Vertex
        {
            public Vector3 Position;
            public Vector3 Normal;
            public Vector4 Tangent;
            public Vector2 Uv;

            public static Vertex Lerp(Vertex a, Vertex b, float t) => new Vertex
            {
                Position = Vector3.Lerp(a.Position, b.Position, t),
                Normal = Vector3.Lerp(a.Normal, b.Normal, t).normalized,
                Tangent = Vector4.Lerp(a.Tangent, b.Tangent, t),
                Uv = Vector2.Lerp(a.Uv, b.Uv, t),
            };
        }

        // z > maxZ인 부분을 평면으로 잘라 냅니다. 걸친 삼각형은 잘린 다각형으로 다시 나눕니다(서브메시·머티리얼 순서 유지).
        private static Mesh Clip(Mesh source, float maxZ, string name)
        {
            using Mesh.MeshDataArray dataArray = Mesh.AcquireReadOnlyMeshData(source);
            Mesh.MeshData data = dataArray[0];
            int count = data.vertexCount;
            Vertex[] vertices = new Vertex[count];
            using (var positions = new NativeArray<Vector3>(count, Allocator.Temp))
            using (var normals = new NativeArray<Vector3>(count, Allocator.Temp))
            using (var tangents = new NativeArray<Vector4>(count, Allocator.Temp))
            using (var uvs = new NativeArray<Vector2>(count, Allocator.Temp))
            {
                data.GetVertices(positions);
                data.GetNormals(normals);
                data.GetTangents(tangents);
                data.GetUVs(0, uvs);
                for (int i = 0; i < count; i++)
                {
                    vertices[i] = new Vertex { Position = positions[i], Normal = normals[i], Tangent = tangents[i], Uv = uvs[i] };
                }
            }

            var output = new List<Vertex>();
            var subMeshes = new List<List<int>>();
            var polygon = new List<Vertex>(4);
            for (int s = 0; s < data.subMeshCount; s++)
            {
                int[] indices;
                using (var native = new NativeArray<int>(data.GetSubMesh(s).indexCount, Allocator.Temp))
                {
                    data.GetIndices(native, s);
                    indices = native.ToArray();
                }

                var triangles = new List<int>();
                for (int i = 0; i < indices.Length; i += 3)
                {
                    polygon.Clear();
                    for (int k = 0; k < 3; k++)
                    {
                        Vertex a = vertices[indices[i + k]];
                        Vertex b = vertices[indices[i + (k + 1) % 3]];
                        bool aIn = a.Position.z <= maxZ;
                        bool bIn = b.Position.z <= maxZ;
                        if (aIn) polygon.Add(a);
                        if (aIn != bIn) polygon.Add(Vertex.Lerp(a, b, (maxZ - a.Position.z) / (b.Position.z - a.Position.z)));
                    }

                    for (int k = 1; k + 1 < polygon.Count; k++)
                    {
                        triangles.Add(output.Count);
                        output.Add(polygon[0]);
                        triangles.Add(output.Count);
                        output.Add(polygon[k]);
                        triangles.Add(output.Count);
                        output.Add(polygon[k + 1]);
                    }
                }
                subMeshes.Add(triangles);
            }

            var mesh = new Mesh { name = name, indexFormat = IndexFormat.UInt32 };
            mesh.SetVertices(output.Select(v => v.Position).ToList());
            mesh.SetNormals(output.Select(v => v.Normal).ToList());
            mesh.SetTangents(output.Select(v => v.Tangent).ToList());
            mesh.SetUVs(0, output.Select(v => v.Uv).ToList());
            mesh.subMeshCount = subMeshes.Count;
            for (int s = 0; s < subMeshes.Count; s++) mesh.SetTriangles(subMeshes[s], s);
            mesh.RecalculateBounds();
            return mesh;
        }

        // 잘린 바닥 끝(+Z 쪽)을 막는 뚜껑. 바닥 옆면과 같은 색이 나오도록 옆면 꼭짓점의 UV 하나를 모든 꼭짓점에 씁니다.
        private static Mesh MakeCap(Mesh source)
        {
            Vector2 uv = Vector2.zero;
            using (Mesh.MeshDataArray dataArray = Mesh.AcquireReadOnlyMeshData(source))
            using (var positions = new NativeArray<Vector3>(dataArray[0].vertexCount, Allocator.Temp))
            using (var uvs = new NativeArray<Vector2>(dataArray[0].vertexCount, Allocator.Temp))
            {
                dataArray[0].GetVertices(positions);
                dataArray[0].GetUVs(0, uvs);
                for (int i = 0; i < positions.Length; i++)
                {
                    if (Mathf.Abs(positions[i].x) > GroundHalfWidth - 0.05f && positions[i].y < 0f)
                    {
                        uv = uvs[i];
                        break;
                    }
                }
            }

            var mesh = new Mesh { name = "Rail Terminal Cap" };
            mesh.SetVertices(new List<Vector3>
            {
                new Vector3(-GroundHalfWidth, GroundBottom, TrackEnd),
                new Vector3(GroundHalfWidth, GroundBottom, TrackEnd),
                new Vector3(GroundHalfWidth, GroundTop, TrackEnd),
                new Vector3(-GroundHalfWidth, GroundTop, TrackEnd),
            });
            mesh.SetNormals(Enumerable.Repeat(Vector3.forward, 4).ToList());
            mesh.SetUVs(0, Enumerable.Repeat(uv, 4).ToList());
            mesh.SetTriangles(new[] { 0, 2, 1, 0, 3, 2 }, 0);   // +Z를 바라보는 면
            mesh.RecalculateTangents();
            mesh.RecalculateBounds();
            return mesh;
        }

        // 메시를 에셋으로 저장합니다. 이미 있으면 내용만 바꿔 GUID를 유지합니다(프리팹·씬 참조가 끊기지 않음).
        private static Mesh SaveMesh(Mesh mesh)
        {
            string path = $"{MeshFolder}/{mesh.name}.asset";
            var existing = AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if (existing == null)
            {
                AssetDatabase.CreateAsset(mesh, path);
                return mesh;
            }

            EditorUtility.CopySerialized(mesh, existing);
            Object.DestroyImmediate(mesh);
            EditorUtility.SetDirty(existing);
            return existing;
        }

        #endregion

        #region 차막이

        // 일본식 차막이: 두 레일 위 기둥(뒤로 버팀대) + 노랑·검정 줄무늬 가로대 + 가운데 빨간 등. 앞면은 -Z(열차가 오는 쪽)입니다.
        private static void BuildBufferStop(GameObject root, string name, float center, Material yellow, Material black, Material metal, Material lamp)
        {
            var group = new GameObject(name);
            group.transform.SetParent(root.transform, false);

            const float postTop = RailTop + 0.75f;
            foreach (float side in new[] { -1f, 1f })
            {
                float x = center + side * RailOffset;
                Box(group, side < 0 ? "Post Left" : "Post Right", new Vector3(x - 0.05f, RailTop, StopZ), new Vector3(x + 0.05f, postTop, StopZ + 0.1f), metal);

                // 기둥 위에서 뒤(+Z) 레일까지 내려가는 버팀대
                Vector3 top = new Vector3(x, postTop - 0.05f, StopZ + 0.05f);
                Vector3 foot = new Vector3(x, RailTop, StopZ + 0.95f);
                Vector3 dir = foot - top;
                var strut = Box(group, side < 0 ? "Strut Left" : "Strut Right", Vector3.zero, Vector3.one, metal);
                strut.transform.localPosition = (top + foot) / 2f;
                strut.transform.localRotation = Quaternion.LookRotation(dir);
                strut.transform.localScale = new Vector3(0.08f, 0.08f, dir.magnitude);
            }

            // 가로대: 노랑·검정을 번갈아 6칸 (앞면 -Z)
            GameObject beam = new GameObject("Beam");
            beam.transform.SetParent(group.transform, false);
            const int stripes = 6;
            const float halfWidth = 0.8f;
            float width = halfWidth * 2f / stripes;
            for (int i = 0; i < stripes; i++)
            {
                float x0 = center - halfWidth + i * width;
                Box(beam, $"Stripe {i + 1}", new Vector3(x0, postTop - 0.3f, StopZ - 0.12f), new Vector3(x0 + width, postTop, StopZ), i % 2 == 0 ? yellow : black);
            }

            GameObject light = new GameObject("Lamp");
            light.transform.SetParent(group.transform, false);
            Box(light, "Base", new Vector3(center - 0.08f, postTop, StopZ - 0.1f), new Vector3(center + 0.08f, postTop + 0.06f, StopZ + 0.02f), black);
            Box(light, "Light", new Vector3(center - 0.06f, postTop + 0.06f, StopZ - 0.08f), new Vector3(center + 0.06f, postTop + 0.18f, StopZ), lamp);
        }

        #endregion

        #region 조립 도우미

        private static void Part(GameObject parent, string name, Mesh mesh, Material[] materials)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent.transform, false);
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            go.AddComponent<MeshRenderer>().sharedMaterials = materials;
        }

        private static GameObject Box(GameObject parent, string name, Vector3 a, Vector3 b, Material material)
        {
            Vector3 min = Vector3.Min(a, b);
            Vector3 max = Vector3.Max(a, b);
            var go = new GameObject(name);
            go.transform.SetParent(parent.transform, false);
            go.transform.localPosition = (min + max) / 2f;
            go.transform.localScale = max - min;
            go.AddComponent<MeshFilter>().sharedMesh = cube;
            go.AddComponent<MeshRenderer>().sharedMaterial = material;
            return go;
        }

        private static Material LoadMaterial(string name) => AssetDatabase.LoadAssetAtPath<Material>($"{PropMaterialFolder}/{name}.mat");

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
