using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;

namespace Vastcore.Editor.Generation
{
    /// <summary>
    /// 構造物間の関係性を管理するシステム
    /// 親子関係、相対位置、配置パターンを制御
    /// </summary>
    [System.Serializable]
    public class StructureRelationshipSystem
    {
        [Header("関係性設定")]
        public StructureRelationship relationshipType = StructureRelationship.OnTop;
        public GameObject parentStructure;
        public GameObject childStructure;
        
        [Header("相対位置制御")]
        public RelativePosition relativePosition = new RelativePosition();
        
        [Header("配置オプション")]
        public bool autoCalculatePosition = true;
        public bool maintainRelationship = true;
        public bool scaleWithParent = false;
        
        // 関係性を適用
        public void ApplyRelationship()
        {
            if (parentStructure == null || childStructure == null)
            {
                Debug.LogWarning("Parent or Child structure is null");
                return;
            }
            
            Vector3 targetPosition = CalculateTargetPosition();
            Vector3 targetRotation = CalculateTargetRotation();
            Vector3 targetScale = CalculateTargetScale();
            
            // 位置・回転・スケールを適用
            childStructure.transform.position = targetPosition;
            childStructure.transform.rotation = Quaternion.Euler(targetRotation);
            childStructure.transform.localScale = targetScale;
            
            // 親子関係を設定（必要に応じて）
            if (maintainRelationship && ShouldParent())
            {
                childStructure.transform.SetParent(parentStructure.transform);
            }
            
            Debug.Log($"Applied relationship: {relationshipType} between {parentStructure.name} and {childStructure.name}");
        }
        
        private Vector3 CalculateTargetPosition()
        {
            Bounds parentBounds = GetObjectBounds(parentStructure);
            Bounds childBounds = GetObjectBounds(childStructure);
            
            Vector3 basePosition = parentStructure.transform.position;
            Vector3 calculatedOffset = Vector3.zero;
            
            switch (relationshipType)
            {
                case StructureRelationship.OnTop:
                    calculatedOffset = Vector3.up * (parentBounds.size.y * 0.5f + childBounds.size.y * 0.5f);
                    break;
                    
                case StructureRelationship.Inside:
                    // リング内部などの場合
                    calculatedOffset = Vector3.zero;
                    break;
                    
                case StructureRelationship.OnSide:
                    calculatedOffset = Vector3.right * (parentBounds.size.x * 0.5f + childBounds.size.x * 0.5f);
                    break;
                    
                case StructureRelationship.Around:
                    // 円周上に配置
                    float angle = relativePosition.angle * Mathf.Deg2Rad;
                    float radius = parentBounds.size.x * 0.5f + relativePosition.distance;
                    calculatedOffset = new Vector3(
                        Mathf.Cos(angle) * radius,
                        0,
                        Mathf.Sin(angle) * radius
                    );
                    break;
                    
                case StructureRelationship.OrbitAround:
                    // 軌道配置
                    float orbitAngle = relativePosition.angle * Mathf.Deg2Rad;
                    float orbitRadius = relativePosition.distance;
                    calculatedOffset = new Vector3(
                        Mathf.Cos(orbitAngle) * orbitRadius,
                        Mathf.Sin(orbitAngle * 0.5f) * orbitRadius * 0.3f, // 楕円軌道
                        Mathf.Sin(orbitAngle) * orbitRadius
                    );
                    break;
                    
                case StructureRelationship.StackedOn:
                    calculatedOffset = Vector3.up * parentBounds.size.y + Vector3.up * relativePosition.offset.y;
                    break;
                    
                case StructureRelationship.ConnectedTo:
                    // 接続点を計算
                    calculatedOffset = CalculateConnectionPoint(parentBounds, childBounds);
                    break;
                    
                case StructureRelationship.MirroredFrom:
                    // 鏡像配置
                    calculatedOffset = Vector3.Scale(relativePosition.offset, new Vector3(-1, 1, 1));
                    break;
                    
                default:
                    calculatedOffset = relativePosition.offset;
                    break;
            }
            
            // 基本オフセットを加算
            calculatedOffset += relativePosition.offset;
            
            // 分布カーブを適用
            if (relativePosition.distribution != null)
            {
                float curveValue = relativePosition.distribution.Evaluate(0.5f);
                calculatedOffset *= curveValue;
            }
            
            return basePosition + calculatedOffset;
        }
        
        private Vector3 CalculateTargetRotation()
        {
            Vector3 baseRotation = parentStructure.transform.rotation.eulerAngles;
            Vector3 additionalRotation = relativePosition.rotationOffset;
            
            switch (relationshipType)
            {
                case StructureRelationship.Around:
                case StructureRelationship.OrbitAround:
                    // 親の方向を向く
                    Vector3 directionToParent = (parentStructure.transform.position - childStructure.transform.position).normalized;
                    additionalRotation.y = Mathf.Atan2(directionToParent.x, directionToParent.z) * Mathf.Rad2Deg;
                    break;
                    
                case StructureRelationship.MirroredFrom:
                    // 鏡像回転
                    additionalRotation.y = -relativePosition.rotationOffset.y;
                    break;
            }
            
            return baseRotation + additionalRotation;
        }
        
        private Vector3 CalculateTargetScale()
        {
            Vector3 baseScale = childStructure.transform.localScale;
            
            if (scaleWithParent)
            {
                Vector3 parentScale = parentStructure.transform.localScale;
                baseScale = Vector3.Scale(baseScale, relativePosition.scaleMultiplier);
                baseScale = Vector3.Scale(baseScale, parentScale);
            }
            else
            {
                baseScale = Vector3.Scale(baseScale, relativePosition.scaleMultiplier);
            }
            
            return baseScale;
        }
        
        private Vector3 CalculateConnectionPoint(Bounds parentBounds, Bounds childBounds)
        {
            // 最も近い面同士を接続
            Vector3 connectionOffset = Vector3.zero;
            
            // 簡単な接続点計算（実際はより複雑な処理が必要）
            float minDistance = float.MaxValue;
            Vector3[] directions = { Vector3.up, Vector3.down, Vector3.left, Vector3.right, Vector3.forward, Vector3.back };
            
            foreach (var direction in directions)
            {
                float distance = Vector3.Distance(parentBounds.center, parentBounds.center + direction * parentBounds.size.magnitude);
                if (distance < minDistance)
                {
                    minDistance = distance;
                    connectionOffset = direction * (parentBounds.size.magnitude * 0.5f + childBounds.size.magnitude * 0.5f);
                }
            }
            
            return connectionOffset;
        }
        
        private bool ShouldParent()
        {
            // 特定の関係性では親子関係を設定しない
            return relationshipType != StructureRelationship.Around && 
                   relationshipType != StructureRelationship.OrbitAround &&
                   relationshipType != StructureRelationship.MirroredFrom;
        }
        
        private Bounds GetObjectBounds(GameObject obj)
        {
            var renderers = obj.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0) return new Bounds(obj.transform.position, Vector3.one);
            
            var bounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++)
            {
                bounds.Encapsulate(renderers[i].bounds);
            }
            return bounds;
        }
        
        // プレビュー用のギズモ描画
        public void DrawGizmos()
        {
            if (parentStructure == null || childStructure == null) return;
            
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(parentStructure.transform.position, childStructure.transform.position);
            
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(parentStructure.transform.position, 1f);
            
            Gizmos.color = Color.blue;
            Gizmos.DrawWireSphere(childStructure.transform.position, 0.5f);
        }
    }
    
    /// <summary>
    /// 構造物関係性のタイプ
    /// </summary>
    public enum StructureRelationship
    {
        [InspectorName("台の上")]
        OnTop,
        
        [InspectorName("内側")]
        Inside,
        
        [InspectorName("側面")]
        OnSide,
        
        [InspectorName("周囲")]
        Around,
        
        [InspectorName("軌道配置")]
        OrbitAround,
        
        [InspectorName("積み重ね")]
        StackedOn,
        
        [InspectorName("連結")]
        ConnectedTo,
        
        [InspectorName("鏡像")]
        MirroredFrom,
        
        [InspectorName("パス追従")]
        FollowPath,
        
        [InspectorName("クラスター")]
        ClusterAround,
        
        [InspectorName("ネットワーク")]
        NetworkNode
    }
    
    /// <summary>
    /// 相対位置制御パラメータ
    /// </summary>
    [System.Serializable]
    public struct RelativePosition
    {
        [Header("基本オフセット")]
        public Vector3 offset;
        
        [Header("回転オフセット")]
        public Vector3 rotationOffset;
        
        [Header("スケール倍率")]
        public Vector3 scaleMultiplier;
        
        [Header("距離・角度")]
        public float distance;
        public float angle;
        
        [Header("分布制御")]
        public AnimationCurve distribution;
        
        public static RelativePosition Default => new RelativePosition
        {
            offset = Vector3.zero,
            rotationOffset = Vector3.zero,
            scaleMultiplier = Vector3.one,
            distance = 10f,
            angle = 0f,
            distribution = AnimationCurve.Linear(0, 1, 1, 1)
        };
    }
} 