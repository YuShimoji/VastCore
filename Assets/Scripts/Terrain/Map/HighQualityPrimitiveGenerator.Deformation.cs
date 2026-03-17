using UnityEngine;
using UnityEngine.ProBuilder;
using UnityEngine.ProBuilder.MeshOperations;
using System.Collections.Generic;
using System.Linq;
using Vastcore.Core;

#if DEFORM_AVAILABLE
using Deform;
#endif

namespace Vastcore.Generation
{
    public static partial class HighQualityPrimitiveGenerator
    {
        #region 高度な変形処理
        /// <summary>
        /// 高度なピラミッド変形
        /// </summary>
        private static void ApplyAdvancedPyramidDeformation(ProBuilderMesh pyramid, QualitySettings quality)
        {
            var vertices = pyramid.positions.ToArray();
            
            for (int i = 0; i < vertices.Length; i++)
            {
                Vector3 vertex = vertices[i];
                
                // ピラミッド特有の変形 - 頂点に向かうにつれて滑らかになる
                float heightFactor = Mathf.Abs(vertex.y) / pyramid.transform.localScale.y;
                float pyramidNoise = Mathf.PerlinNoise(vertex.x * 10f, vertex.z * 10f) * quality.detailIntensity * 0.03f;
                
                // 上部ほど変形を強くする
                float deformationStrength = (1f - heightFactor) * pyramidNoise;
                vertex += vertex.normalized * deformationStrength;
                
                vertices[i] = vertex;
            }
            
            pyramid.positions = vertices;
        }
        private static void ApplyAdvancedCubeDeformation(ProBuilderMesh cube, QualitySettings quality)
        {
            var vertices = cube.positions.ToArray();
            
            for (int i = 0; i < vertices.Length; i++)
            {
                Vector3 vertex = vertices[i];
                
                // エッジの丸み
                float edgeRounding = quality.detailIntensity * 0.1f;
                vertex = Vector3.Lerp(vertex, vertex.normalized * vertex.magnitude, edgeRounding);
                
                // 表面の微細な凹凸
                float surfaceDetail = (Mathf.PerlinNoise(vertex.x * 50f, vertex.z * 50f) - 0.5f) * quality.detailIntensity * 0.02f;
                vertex += vertex.normalized * surfaceDetail;
                
                vertices[i] = vertex;
            }
            
            cube.positions = vertices;
        }

        /// <summary>
        /// 高度な球体変形
        /// </summary>
        private static void ApplyAdvancedSphereDeformation(ProBuilderMesh sphere, QualitySettings quality)
        {
            var vertices = sphere.positions.ToArray();
            
            for (int i = 0; i < vertices.Length; i++)
            {
                Vector3 vertex = vertices[i];
                
                // 球面調和関数による変形
                float theta = Mathf.Atan2(vertex.z, vertex.x);
                float phi = Mathf.Acos(vertex.y / vertex.magnitude);
                
                float harmonicDeformation = Mathf.Sin(theta * 3f) * Mathf.Sin(phi * 2f) * quality.detailIntensity * 0.05f;
                vertex += vertex.normalized * harmonicDeformation;
                
                vertices[i] = vertex;
            }
            
            sphere.positions = vertices;
        }

        /// <summary>
        /// 高度な円柱変形
        /// </summary>
        private static void ApplyAdvancedCylinderDeformation(ProBuilderMesh cylinder, QualitySettings quality)
        {
            var vertices = cylinder.positions.ToArray();
            
            for (int i = 0; i < vertices.Length; i++)
            {
                Vector3 vertex = vertices[i];
                
                // 高さに応じたテーパリング
                float heightFactor = (vertex.y + 1f) * 0.5f; // 0-1に正規化
                float tapering = 1f - heightFactor * quality.detailIntensity * 0.1f;
                
                if (Mathf.Abs(vertex.y) < 0.9f) // 側面のみ
                {
                    vertex.x *= tapering;
                    vertex.z *= tapering;
                }
                
                vertices[i] = vertex;
            }
            
            cylinder.positions = vertices;
        }

        /// <summary>
        /// 高度なトーラス変形
        /// </summary>
        private static void ApplyAdvancedTorusDeformation(ProBuilderMesh torus, QualitySettings quality) 
        {
            var vertices = torus.positions.ToArray();
            
            for (int i = 0; i < vertices.Length; i++)
            {
                Vector3 vertex = vertices[i];
                
                // 円環の歪み
                float angle = Mathf.Atan2(vertex.z, vertex.x);
                float radialDistance = new Vector2(vertex.x, vertex.z).magnitude;
                
                // 非対称な歪み
                float asymmetry = Mathf.Sin(angle * 3f) * quality.detailIntensity * 0.05f;
                vertex.x += Mathf.Cos(angle) * asymmetry;
                vertex.z += Mathf.Sin(angle) * asymmetry;
                
                // 表面の凹凸
                float surfaceNoise = Mathf.PerlinNoise(angle * 5f, vertex.y * 10f) * quality.detailIntensity * 0.02f;
                Vector3 radialDirection = new Vector3(vertex.x, 0, vertex.z).normalized;
                vertex += radialDirection * surfaceNoise;
                
                vertices[i] = vertex;
            }
            
            torus.positions = vertices;
        }
        
        /// <summary>
        /// 高度な角柱変形
        /// </summary>
        private static void ApplyAdvancedPrismDeformation(ProBuilderMesh prism, QualitySettings quality) 
        {
            var vertices = prism.positions.ToArray();
            
            for (int i = 0; i < vertices.Length; i++)
            {
                Vector3 vertex = vertices[i];
                
                // 角の丸み
                if (Mathf.Abs(vertex.y) < 0.9f) // 側面
                {
                    float angle = Mathf.Atan2(vertex.z, vertex.x);
                    int sides = 6; // 六角柱
                    float sideAngle = 2f * Mathf.PI / sides;
                    float nearestSideAngle = Mathf.Round(angle / sideAngle) * sideAngle;
                    
                    // 角の丸み効果
                    float cornerRounding = quality.detailIntensity * 0.1f;
                    float angleDeviation = Mathf.Abs(angle - nearestSideAngle);
                    if (angleDeviation < sideAngle * 0.2f)
                    {
                        float radius = new Vector2(vertex.x, vertex.z).magnitude;
                        radius *= (1f - cornerRounding * (1f - angleDeviation / (sideAngle * 0.2f)));
                        vertex.x = radius * Mathf.Cos(angle);
                        vertex.z = radius * Mathf.Sin(angle);
                    }
                }
                
                vertices[i] = vertex;
                float noise = Mathf.PerlinNoise(vertex.x * 12f, vertex.z * 12f) * quality.detailIntensity * 0.04f;
                vertices[i] = vertex + vertex.normalized * noise;
            }
            prism.positions = vertices;
        }
        
        private static void ApplyAdvancedConeDeformation(ProBuilderMesh cone, QualitySettings quality) 
        {
            // 基本的な変形を適用
            var vertices = cone.positions.ToArray();
            for (int i = 0; i < vertices.Length; i++)
            {
                Vector3 vertex = vertices[i];
                float noise = Mathf.PerlinNoise(vertex.x * 15f, vertex.z * 15f) * quality.detailIntensity * 0.03f;
                vertices[i] = vertex + vertex.normalized * noise;
            }
            cone.positions = vertices;
        }
        
        private static void ApplyAdvancedOctahedronDeformation(ProBuilderMesh octahedron, QualitySettings quality) 
        {
            // 基本的な変形を適用
            var vertices = octahedron.positions.ToArray();
            for (int i = 0; i < vertices.Length; i++)
            {
                Vector3 vertex = vertices[i];
                float noise = Mathf.PerlinNoise(vertex.x * 20f, vertex.z * 20f) * quality.detailIntensity * 0.02f;
                vertices[i] = vertex + vertex.normalized * noise;
            }
            octahedron.positions = vertices;
        }
        
        private static void ApplyAdvancedCrystalDeformation(ProBuilderMesh crystal, QualitySettings quality) 
        {
            // 結晶特有の変形
            if (crystal == null) return;
            var vertices = crystal.positions.ToArray();
            for (int i = 0; i < vertices.Length; i++)
            {
                Vector3 vertex = vertices[i];
                float crystallineNoise = Mathf.PerlinNoise(vertex.x * 25f, vertex.z * 25f) * quality.detailIntensity * 0.01f;
                vertices[i] = vertex + vertex.normalized * crystallineNoise;
            }
            crystal.positions = vertices;
        }
        
        private static void ApplyAdvancedMonolithDeformation(ProBuilderMesh monolith, QualitySettings quality) 
        {
            // モノリス特有の風化変形
            var vertices = monolith.positions.ToArray();
            for (int i = 0; i < vertices.Length; i++)
            {
                Vector3 vertex = vertices[i];
                float weathering = Mathf.PerlinNoise(vertex.x * 30f, vertex.y * 30f) * quality.detailIntensity * 0.02f;
                vertices[i] = vertex + vertex.normalized * weathering;
            }
            monolith.positions = vertices;
        }
        
        private static void ApplyAdvancedRingDeformation(ProBuilderMesh ring, QualitySettings quality) 
        {
            // リング特有の変形
            var vertices = ring.positions.ToArray();
            for (int i = 0; i < vertices.Length; i++)
            {
                Vector3 vertex = vertices[i];
                float ringNoise = Mathf.PerlinNoise(vertex.x * 18f, vertex.z * 18f) * quality.detailIntensity * 0.025f;
                vertices[i] = vertex + vertex.normalized * ringNoise;
            }
            ring.positions = vertices;
        }
        
        private static void ApplyAdvancedMesaDeformation(ProBuilderMesh mesa, QualitySettings quality) 
        {
            // メサ特有の浸食変形
            var vertices = mesa.positions.ToArray();
            for (int i = 0; i < vertices.Length; i++)
            {
                Vector3 vertex = vertices[i];
                float erosion = Mathf.PerlinNoise(vertex.x * 8f, vertex.z * 8f) * quality.detailIntensity * 0.06f;
                vertices[i] = vertex + Vector3.up * erosion;
            }
            mesa.positions = vertices;
        }
        
        private static void ApplyAdvancedSpireDeformation(ProBuilderMesh spire, QualitySettings quality) 
        {
            // 尖塔特有の変形
            var vertices = spire.positions.ToArray();
            for (int i = 0; i < vertices.Length; i++)
            {
                Vector3 vertex = vertices[i];
                float spireNoise = Mathf.PerlinNoise(vertex.x * 40f, vertex.y * 40f) * quality.detailIntensity * 0.015f;
                vertices[i] = vertex + vertex.normalized * spireNoise;
            }
            spire.positions = vertices;
        }
        
        private static void ApplyAdvancedBoulderDeformation(ProBuilderMesh boulder, QualitySettings quality) 
        {
            // 巨石特有の追加変形
            var vertices = boulder.positions.ToArray();
            for (int i = 0; i < vertices.Length; i++)
            {
                Vector3 vertex = vertices[i];
                float roughness = Mathf.PerlinNoise(vertex.x * 35f, vertex.z * 35f) * quality.detailIntensity * 0.08f;
                vertices[i] = vertex + vertex.normalized * roughness;
            }
            boulder.positions = vertices;
        }
        
        private static void ApplyAdvancedFormationDeformation(ProBuilderMesh formation, QualitySettings quality) 
        {
            // 岩石層特有の地質学的変形
            var vertices = formation.positions.ToArray();
            for (int i = 0; i < vertices.Length; i++)
            {
                Vector3 vertex = vertices[i];
                float geological = Mathf.PerlinNoise(vertex.x * 25f, vertex.y * 25f) * quality.detailIntensity * 0.03f;
                vertices[i] = vertex + Vector3.right * geological;
            }
            formation.positions = vertices;
        }
        #endregion

        #endregion // 高品質メッシュ生成
        
    }
}
