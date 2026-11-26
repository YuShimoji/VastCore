using UnityEngine;

namespace Vastcore.RegionMap
{
    /// <summary>
    /// DS-011 プレースホルダの外形寸法・ピボット・正面軸 (+Z)・V6 群生境界を Scene/Gizmo で確認するコンポーネント。
    /// </summary>
    public sealed class RegionPropGizmo : MonoBehaviour
    {
        public string PropId = "";
        public string Role = "";
        public Vector3 DimensionsM = Vector3.one;
        public Vector3 EffectiveScale = Vector3.one;
        public float EmbedDepthM;
        public RegionPropPivotRule PivotRule = RegionPropPivotRule.StackableUpright;
        public RegionPropClusterPattern ClusterPattern = RegionPropClusterPattern.Single;
        public float ClusterRadiusM;
        public Vector3 FlowDirection = Vector3.forward;
        public float ConeAngleDeg = 70f;
        public bool IsClusterAnchor;

        public Vector3 ScaledDimensionsM => new Vector3(
            DimensionsM.x * EffectiveScale.x,
            DimensionsM.y * EffectiveScale.y,
            DimensionsM.z * EffectiveScale.z);

        public void Configure(RegionPropInstance inst, RegionPropClusterPlan cluster = null)
        {
            if (inst == null) return;
            PropId = inst.PropId;
            Role = inst.Role;
            DimensionsM = new Vector3(inst.DimensionsX, inst.DimensionsY, inst.DimensionsZ);
            EffectiveScale = new Vector3(inst.ScaleX, inst.ScaleY, inst.ScaleZ);
            EmbedDepthM = inst.EmbedDepthM;
            PivotRule = inst.PivotRule;
            if (cluster != null && inst.MemberIndex == 0 && !inst.Role.StartsWith("DebrisSkirt_"))
            {
                IsClusterAnchor = true;
                ClusterPattern = cluster.Pattern;
                ClusterRadiusM = cluster.BoundaryRadiusM;
                FlowDirection = new Vector3(cluster.FlowDirX, 0f, -cluster.FlowDirZ);
                ConeAngleDeg = cluster.ConeAngleDeg;
            }
        }

        private void OnDrawGizmosSelected()
        {
            DrawBoundsAndAxes(selected: true);
        }

        private void OnDrawGizmos()
        {
            DrawBoundsAndAxes(selected: false);
        }

        private void DrawBoundsAndAxes(bool selected)
        {
            Matrix4x4 prev = Gizmos.matrix;
            Gizmos.matrix = transform.localToWorldMatrix;

            Vector3 size = ScaledDimensionsM;
            float centerY = PivotRule == RegionPropPivotRule.NaturalEmbed
                ? size.y * 0.35f
                : size.y * 0.5f;

            Gizmos.color = selected
                ? new Color(1f, 0.85f, 0.25f, 0.95f)
                : new Color(0.25f, 0.8f, 0.95f, 0.35f);
            Gizmos.DrawWireCube(new Vector3(0f, centerY, 0f), size);

            // ピボット位置（接地基準原点）
            Gizmos.color = new Color(1f, 0.45f, 0.15f, selected ? 1f : 0.6f);
            Gizmos.DrawSphere(Vector3.zero, Mathf.Clamp(Mathf.Min(size.x, size.z) * 0.08f, 0.04f, 0.16f));

            // 正面軸 (+Z) と上方向 (+Y)
            float axisLen = Mathf.Max(0.45f, Mathf.Max(size.x, size.z) * 0.6f);
            Gizmos.color = Color.blue;
            Gizmos.DrawLine(Vector3.zero, new Vector3(0f, 0f, axisLen));
            Gizmos.color = Color.green;
            Gizmos.DrawLine(Vector3.zero, new Vector3(0f, axisLen * 0.7f, 0f));

            Gizmos.matrix = prev;

            if (IsClusterAnchor && ClusterRadiusM > 0.1f)
            {
                Gizmos.color = selected
                    ? new Color(0.3f, 0.95f, 0.45f, 0.9f)
                    : new Color(0.3f, 0.95f, 0.45f, 0.3f);
                Vector3 origin = transform.position;
                int segs = 24;
                Vector3 prevPt = origin + new Vector3(ClusterRadiusM, 0.05f, 0f);
                for (int i = 1; i <= segs; i++)
                {
                    float a = (i / (float)segs) * Mathf.PI * 2f;
                    Vector3 pt = origin + new Vector3(Mathf.Cos(a) * ClusterRadiusM, 0.05f, Mathf.Sin(a) * ClusterRadiusM);
                    Gizmos.DrawLine(prevPt, pt);
                    prevPt = pt;
                }

                if (ClusterPattern == RegionPropClusterPattern.DirectionalCone && FlowDirection.sqrMagnitude > 1e-4f)
                {
                    Vector3 dir = FlowDirection.normalized;
                    float halfRad = ConeAngleDeg * 0.5f * Mathf.Deg2Rad;
                    Vector3 left = Quaternion.Euler(0f, -ConeAngleDeg * 0.5f, 0f) * dir * ClusterRadiusM;
                    Vector3 right = Quaternion.Euler(0f, ConeAngleDeg * 0.5f, 0f) * dir * ClusterRadiusM;
                    Gizmos.color = new Color(1f, 0.55f, 0.15f, selected ? 0.95f : 0.45f);
                    Gizmos.DrawLine(origin + Vector3.up * 0.08f, origin + Vector3.up * 0.08f + dir * ClusterRadiusM);
                    Gizmos.DrawLine(origin + Vector3.up * 0.08f, origin + Vector3.up * 0.08f + left);
                    Gizmos.DrawLine(origin + Vector3.up * 0.08f, origin + Vector3.up * 0.08f + right);
                    _ = halfRad;
                }
            }
        }
    }
}
