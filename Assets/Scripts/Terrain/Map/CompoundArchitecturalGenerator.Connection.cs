using UnityEngine;
using UnityEngine.ProBuilder;
using UnityEngine.ProBuilder.MeshOperations;
using System.Collections.Generic;
using System.Linq;
using Vastcore.Utilities;
using Vastcore.Core;
using Vastcore.Generation;

namespace Vastcore.Generation
{
    public static partial class CompoundArchitecturalGenerator
    {
        #region 接続・統合システム
        /// <summary>
        /// 接続要素を追加
        /// </summary>
        private static void AddConnectingElements(GameObject parent, CompoundArchitecturalParams parameters)
        {
            switch (parameters.compoundType)
            {
                case CompoundArchitecturalType.MultipleBridge:
                case CompoundArchitecturalType.AqueductSystem:
                    AddBridgeConnections(parent, parameters);
                    break;
                case CompoundArchitecturalType.CathedralComplex:
                    AddCathedralConnections(parent, parameters);
                    break;
                case CompoundArchitecturalType.FortressWall:
                    AddWallConnections(parent, parameters);
                    break;
                case CompoundArchitecturalType.Cloister:
                    AddCloisterConnections(parent, parameters);
                    break;
            }
        }

        /// <summary>
        /// 橋梁接続要素を追加
        /// </summary>
        private static void AddBridgeConnections(GameObject parent, CompoundArchitecturalParams parameters)
        {
            // 支柱間の接続梁
            int connectionCount = parameters.structureCount - 1;
            float archSpan = parameters.overallSize.x / parameters.structureCount;
            
            for (int i = 0; i < connectionCount; i++)
            {
                float x = (i - connectionCount * 0.5f + 0.5f) * archSpan;
                Vector3 position = new Vector3(x, parameters.overallSize.y * 0.8f, 0);
                Vector3 size = new Vector3(parameters.overallSize.z, parameters.overallSize.z * 0.5f, parameters.overallSize.z);
                
                CreateConnectionBeam(parent, $"ConnectionBeam_{i}", position, size, parameters);
            }
        }

        /// <summary>
        /// 大聖堂接続要素を追加
        /// </summary>
        private static void AddCathedralConnections(GameObject parent, CompoundArchitecturalParams parameters)
        {
            // 身廊と側廊を接続する横断アーチ
            int transeptCount = 3;
            
            for (int i = 0; i < transeptCount; i++)
            {
                float z = (i - transeptCount * 0.5f + 0.5f) * parameters.overallSize.z * 0.6f;
                Vector3 position = new Vector3(0, parameters.overallSize.y * 0.7f, z);
                Vector3 size = new Vector3(parameters.overallSize.x * 0.8f, parameters.overallSize.z * 0.3f, parameters.overallSize.z * 0.2f);
                
                CreateTranseptArch(parent, $"TranseptArch_{i}", position, size, parameters);
            }
        }

        /// <summary>
        /// 城壁接続要素を追加
        /// </summary>
        private static void AddWallConnections(GameObject parent, CompoundArchitecturalParams parameters)
        {
            // 城壁上部の歩廊
            Vector3 position = new Vector3(0, parameters.overallSize.y * 1.05f, 0);
            Vector3 size = new Vector3(parameters.overallSize.x, parameters.overallSize.z * 0.2f, parameters.overallSize.z * 0.8f);
            
            var walkway = ShapeGenerator.CreateShape(ShapeType.Cube);
            walkway.name = "WallWalkway";
            walkway.transform.SetParent(parent.transform);
            walkway.transform.localPosition = position;
            walkway.transform.localScale = size;
            
            if (parameters.secondaryMaterial != null)
            {
                walkway.GetComponent<MeshRenderer>().material = parameters.secondaryMaterial;
            }
        }

        /// <summary>
        /// 回廊接続要素を追加
        /// </summary>
        private static void AddCloisterConnections(GameObject parent, CompoundArchitecturalParams parameters)
        {
            // 角の接続柱
            float cornerRadius = parameters.overallSize.x * 0.25f;
            
            for (int corner = 0; corner < 4; corner++)
            {
                float angle = corner * 90f + 45f;
                Vector3 position = new Vector3(
                    Mathf.Cos(angle * Mathf.Deg2Rad) * cornerRadius,
                    parameters.overallSize.y * 0.5f,
                    Mathf.Sin(angle * Mathf.Deg2Rad) * cornerRadius
                );
                
                CreateCornerColumn(parent, $"CornerColumn_{corner}", position, parameters);
            }
        }

        /// <summary>
        /// 接続梁を作成
        /// </summary>
        private static void CreateConnectionBeam(GameObject parent, string name, Vector3 position, Vector3 size, CompoundArchitecturalParams parameters)
        {
            var beam = ShapeGenerator.CreateShape(ShapeType.Cube);
            beam.name = name;
            beam.transform.SetParent(parent.transform);
            beam.transform.localPosition = position;
            beam.transform.localScale = size;
            
            if (parameters.secondaryMaterial != null)
            {
                beam.GetComponent<MeshRenderer>().material = parameters.secondaryMaterial;
            }
        }

        /// <summary>
        /// 横断アーチを作成
        /// </summary>
        private static void CreateTranseptArch(GameObject parent, string name, Vector3 position, Vector3 size, CompoundArchitecturalParams parameters)
        {
            var transept = ShapeGenerator.CreateShape(ShapeType.Cube);
            transept.name = name;
            transept.transform.SetParent(parent.transform);
            transept.transform.localPosition = position;
            transept.transform.localScale = size;
            
            // アーチ形状に変形
            ApplyArchDeformation(transept, parameters);
            
            if (parameters.primaryMaterial != null)
            {
                transept.GetComponent<MeshRenderer>().material = parameters.primaryMaterial;
            }
        }

        /// <summary>
        /// アーチ変形を適用
        /// </summary>
        private static void ApplyArchDeformation(ProBuilderMesh mesh, CompoundArchitecturalParams parameters)
        {
            var vertices = mesh.positions.ToArray();
            
            for (int i = 0; i < vertices.Length; i++)
            {
                Vector3 vertex = vertices[i];
                
                // 下部をアーチ形状にする
                if (vertex.y < 0)
                {
                    float archCurve = 1f - (vertex.x * vertex.x) / 0.25f;
                    if (archCurve > 0)
                    {
                        vertex.y *= archCurve;
                    }
                    else
                    {
                        vertex.y = 0; // アーチの外側は削除
                    }
                }
                
                vertices[i] = vertex;
            }
            
            mesh.positions = vertices;
            mesh.ToMesh();
            mesh.Refresh();
        }

        /// <summary>
        /// 角柱を作成
        /// </summary>
        private static void CreateCornerColumn(GameObject parent, string name, Vector3 position, CompoundArchitecturalParams parameters)
        {
            var column = ShapeGenerator.CreateShape(ShapeType.Cylinder);
            column.name = name;
            column.transform.SetParent(parent.transform);
            column.transform.localPosition = position;
            column.transform.localScale = new Vector3(parameters.overallSize.x * 0.04f, parameters.overallSize.y, parameters.overallSize.x * 0.04f);
            
            if (parameters.primaryMaterial != null)
            {
                column.GetComponent<MeshRenderer>().material = parameters.primaryMaterial;
            }
        }

        /// <summary>
        /// 統一装飾を追加
        /// </summary>
        private static void AddUnifiedDecorations(GameObject parent, CompoundArchitecturalParams parameters)
        {
            // 全体的な装飾テーマを適用
            ApplyDecorativeTheme(parent, parameters);
            
            // 装飾的な要素を追加
            if (parameters.decorationComplexity > 0.5f)
            {
                AddComplexDecorations(parent, parameters);
            }
        }

        /// <summary>
        /// 装飾テーマを適用
        /// </summary>
        private static void ApplyDecorativeTheme(GameObject parent, CompoundArchitecturalParams parameters)
        {
            // 全ての子オブジェクトに統一された装飾スタイルを適用
            var renderers = parent.GetComponentsInChildren<MeshRenderer>();
            
            foreach (var renderer in renderers)
            {
                if (renderer.gameObject.name.Contains("Decoration") || renderer.gameObject.name.Contains("Keystone"))
                {
                    if (parameters.decorationMaterial != null)
                    {
                        renderer.material = parameters.decorationMaterial;
                    }
                }
            }
        }

        /// <summary>
        /// 複雑な装飾を追加
        /// </summary>
        private static void AddComplexDecorations(GameObject parent, CompoundArchitecturalParams parameters)
        {
            // 装飾的な尖塔を追加
            int spireCount = Mathf.RoundToInt(parameters.decorationComplexity * 6f);
            
            for (int i = 0; i < spireCount; i++)
            {
                Vector3 position = GetRandomDecorationPosition(parameters);
                CreateDecorativeSpire(parent, $"DecorativeSpire_{i}", position, parameters);
            }
        }

        /// <summary>
        /// ランダムな装飾位置を取得
        /// </summary>
        private static Vector3 GetRandomDecorationPosition(CompoundArchitecturalParams parameters)
        {
            float x = Random.Range(-parameters.overallSize.x * 0.4f, parameters.overallSize.x * 0.4f);
            float y = parameters.overallSize.y * Random.Range(0.8f, 1.2f);
            float z = Random.Range(-parameters.overallSize.z * 0.4f, parameters.overallSize.z * 0.4f);
            
            return new Vector3(x, y, z);
        }

        /// <summary>
        /// 装飾的な尖塔を作成
        /// </summary>
        private static void CreateDecorativeSpire(GameObject parent, string name, Vector3 position, CompoundArchitecturalParams parameters)
        {
            var spire = ShapeGenerator.CreateShape(ShapeType.Cone);
            spire.name = name;
            spire.transform.SetParent(parent.transform);
            spire.transform.localPosition = position;
            spire.transform.localScale = Vector3.one * parameters.overallSize.x * 0.03f * parameters.decorationComplexity;
            
            if (parameters.decorationMaterial != null)
            {
                spire.GetComponent<MeshRenderer>().material = parameters.decorationMaterial;
            }
        }

        /// <summary>
        /// 複合コライダーを設定
        /// </summary>
        private static void SetupCompoundColliders(GameObject parent, CompoundArchitecturalParams parameters)
        {
            // 親オブジェクトに複合コライダーを追加
            var meshCollider = parent.AddComponent<MeshCollider>();
            meshCollider.convex = false;
            
            // 全ての子メッシュを結合してコライダーを作成
            CombineAllMeshesForCollider(parent, meshCollider);
            
            // インタラクション設定
            SetupCompoundInteractions(parent, parameters);
        }

        /// <summary>
        /// 全メッシュを結合してコライダーを作成
        /// </summary>
        private static void CombineAllMeshesForCollider(GameObject parent, MeshCollider collider)
        {
            MeshCombineHelper.CombineChildrenToCollider(parent, collider, "CompoundArchitecturalGenerator");
        }

        /// <summary>
        /// 複合インタラクションを設定
        /// </summary>
        private static void SetupCompoundInteractions(GameObject parent, CompoundArchitecturalParams parameters)
        {
            // プリミティブ地形オブジェクトコンポーネントを追加
            var compoundComponent = parent.AddComponent<PrimitiveTerrainObject>();
            compoundComponent.primitiveType = GenerationPrimitiveType.Arch;
            compoundComponent.isClimbable = true;
            compoundComponent.isGrindable = true;
            compoundComponent.hasCollision = true;
            
            // 複合建築物専用のタグを設定
            try
            {
                parent.tag = "CompoundArchitecture";
            }
            catch (System.Exception ex)
            {
                VastcoreLogger.Instance.LogWarning("CompoundArch", $"Tag 'CompoundArchitecture' is not defined. Skipping tag assignment. Details: {ex.Message}");
            }
        }

        #endregion

    }
}
