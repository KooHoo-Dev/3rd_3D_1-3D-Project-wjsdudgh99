using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace YoureRight
{
    // 코드로 조립한 프리팹 안의 그룹(렌더러 없이 자식만 있는 오브젝트)의 기준점을 자기 밑면 가운데로 옮기는 에디터 도우미입니다.
    // 그룹을 원점에 만들고 조각을 프리팹 좌표로 붙이면 기준점이 프리팹 가운데에 남아서, 개찰구 통 하나만 돌려도 프리팹 가운데를 중심으로 돕니다.
    // 자식의 월드 위치는 그대로 두므로 모양은 바뀌지 않습니다. 루트의 기준점은 건드리지 않습니다(프리팹마다 정한 규칙을 따름).
    // StationKitBuilder·StationPropsBuilder가 저장 직전에 부르고, 앞으로 만드는 조립 도구도 같이 부릅니다.
    internal static class PivotUtility
    {
        public static void CenterGroupPivots(GameObject root)
        {
            foreach (Transform group in root.GetComponentsInChildren<Transform>(true))
            {
                if (group == root.transform || group.childCount == 0 || group.GetComponent<Renderer>() != null) continue;
                MovePivotToBase(group);
            }
        }

        // 그룹 자신의 좌표계에서 본 모든 메시의 경계 상자를 구해, 그 밑면 가운데로 기준점을 옮깁니다(돌려 둔 그룹도 자기 축 기준).
        private static void MovePivotToBase(Transform group)
        {
            if (TryGetLocalBounds(group, out Bounds bounds) == false) return;

            Vector3 pivot = group.TransformPoint(new Vector3(bounds.center.x, bounds.min.y, bounds.center.z));
            List<Transform> children = group.Cast<Transform>().ToList();
            List<Vector3> positions = children.Select(child => child.position).ToList();

            group.position = pivot;
            for (int i = 0; i < children.Count; i++) children[i].position = positions[i];
        }

        private static bool TryGetLocalBounds(Transform group, out Bounds bounds)
        {
            bounds = default;
            bool found = false;

            foreach (MeshFilter filter in group.GetComponentsInChildren<MeshFilter>(true))
            {
                if (filter.sharedMesh == null) continue;
                Bounds mesh = filter.sharedMesh.bounds;
                for (int i = 0; i < 8; i++)
                {
                    var sign = new Vector3((i & 1) == 0 ? -1f : 1f, (i & 2) == 0 ? -1f : 1f, (i & 4) == 0 ? -1f : 1f);
                    Vector3 corner = group.InverseTransformPoint(filter.transform.TransformPoint(mesh.center + Vector3.Scale(mesh.extents, sign)));
                    if (found) bounds.Encapsulate(corner);
                    else bounds = new Bounds(corner, Vector3.zero);
                    found = true;
                }
            }
            return found;
        }
    }
}
