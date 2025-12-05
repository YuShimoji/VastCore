using UnityEngine;

namespace Vastcore.Generation
{
    /// <summary>
    /// UnifiedTerrainParams を各ジェネレータ固有のパラメータに変換するユーティリティクラス。
    /// 
    /// T3ギャップ分析（docs/T3_TERRAIN_GAP_ANALYSIS.md）に基づく方針Aの実装。
    /// </summary>
    public static class TerrainParamsConverter
    {
        #region To PrimitiveTerrainGenerator
        
        /// <summary>
        /// UnifiedTerrainParams を PrimitiveGenerationParams に変換
        /// </summary>
        /// <param name="unified">統一パラメータ</param>
        /// <returns>PrimitiveGenerationParams</returns>
        public static PrimitiveGenerationParams ToPrimitive(UnifiedTerrainParams unified)
        {
            return new PrimitiveGenerationParams
            {
                position = Vector3.zero,
                scale = new Vector3(unified.worldSize, unified.maxElevation, unified.worldSize),
                rotation = Quaternion.identity,
                enableDeformation = true,
                noiseIntensity = unified.noiseSettings.scale * 0.5f, // 正規化スケールを変換
                subdivisionLevel = Mathf.Clamp(unified.noiseSettings.octaves / 2, 1, 4),
                generateCollider = true
            };
        }
        
        #endregion
        
        #region To MeshGenerator
        
        /// <summary>
        /// UnifiedTerrainParams を MeshGenerator.TerrainGenerationParams に変換
        /// </summary>
        /// <param name="unified">統一パラメータ</param>
        /// <returns>MeshGenerator.TerrainGenerationParams</returns>
        public static MeshGenerator.TerrainGenerationParams ToMeshGenerator(UnifiedTerrainParams unified)
        {
            var result = MeshGenerator.TerrainGenerationParams.Default();
            result.resolution = unified.meshResolution;
            result.size = unified.worldSize;
            result.maxHeight = unified.maxElevation;
            result.noiseType = ConvertNoiseType(unified.noiseSettings.noiseType);
            result.noiseScale = ConvertNoiseScale(unified.noiseSettings.scale, unified.worldSize);
            result.octaves = unified.noiseSettings.octaves;
            result.persistence = unified.noiseSettings.persistence;
            result.lacunarity = unified.noiseSettings.lacunarity;
            result.offset = unified.noiseSettings.offset;
            return result;
        }
        
        /// <summary>
        /// 統一NoiseTypeをMeshGenerator.NoiseTypeに変換
        /// </summary>
        private static MeshGenerator.NoiseType ConvertNoiseType(NoiseType unified)
        {
            return unified switch
            {
                NoiseType.Perlin => MeshGenerator.NoiseType.Perlin,
                NoiseType.Simplex => MeshGenerator.NoiseType.Simplex,
                NoiseType.Ridged => MeshGenerator.NoiseType.Ridged,
                NoiseType.Fractal => MeshGenerator.NoiseType.Fractal,
                NoiseType.Voronoi => MeshGenerator.NoiseType.Voronoi,
                _ => MeshGenerator.NoiseType.Perlin
            };
        }
        
        /// <summary>
        /// MeshGenerator.NoiseTypeを統一NoiseTypeに変換
        /// </summary>
        private static NoiseType ConvertNoiseTypeFromMesh(MeshGenerator.NoiseType meshType)
        {
            return meshType switch
            {
                MeshGenerator.NoiseType.Perlin => NoiseType.Perlin,
                MeshGenerator.NoiseType.Simplex => NoiseType.Simplex,
                MeshGenerator.NoiseType.Ridged => NoiseType.Ridged,
                MeshGenerator.NoiseType.Fractal => NoiseType.Fractal,
                MeshGenerator.NoiseType.Voronoi => NoiseType.Voronoi,
                _ => NoiseType.Perlin
            };
        }
        
        #endregion
        
        #region From Existing Params
        
        /// <summary>
        /// PrimitiveGenerationParams から UnifiedTerrainParams に変換
        /// </summary>
        public static UnifiedTerrainParams FromPrimitive(PrimitiveGenerationParams primitive)
        {
            return new UnifiedTerrainParams
            {
                worldSize = Mathf.Max(primitive.scale.x, primitive.scale.z),
                maxElevation = primitive.scale.y,
                meshResolution = 256, // Primitiveは固定解像度
                noiseSettings = new NoiseSettings
                {
                    noiseType = NoiseType.Perlin,
                    scale = primitive.noiseIntensity * 2f,
                    octaves = primitive.subdivisionLevel * 2,
                    persistence = NoiseSettings.Default().persistence,
                    lacunarity = NoiseSettings.Default().lacunarity,
                    offset = Vector2.zero,
                    seed = 0
                },
                outputType = TerrainOutputType.ProBuilder
            };
        }
        
        /// <summary>
        /// MeshGenerator.TerrainGenerationParams から UnifiedTerrainParams に変換
        /// </summary>
        public static UnifiedTerrainParams FromMeshGenerator(MeshGenerator.TerrainGenerationParams meshParams)
        {
            return new UnifiedTerrainParams
            {
                worldSize = meshParams.size,
                maxElevation = meshParams.maxHeight,
                meshResolution = meshParams.resolution,
                noiseSettings = new NoiseSettings
                {
                    noiseType = ConvertNoiseTypeFromMesh(meshParams.noiseType),
                    scale = ConvertNoiseScaleToNormalized(meshParams.noiseScale, meshParams.size),
                    octaves = meshParams.octaves,
                    persistence = meshParams.persistence,
                    lacunarity = meshParams.lacunarity,
                    offset = meshParams.offset,
                    seed = 0
                },
                outputType = TerrainOutputType.Mesh
            };
        }
        
        #endregion
        
        #region Helper Methods
        
        /// <summary>
        /// 正規化されたノイズスケールをMeshGenerator用の値に変換
        /// MeshGeneratorは 0.005f がデフォルト、worldSizeに依存
        /// </summary>
        private static float ConvertNoiseScale(float normalizedScale, float worldSize)
        {
            // 正規化スケール(0-1) を MeshGenerator の範囲に変換
            // 基準: worldSize=2000 で noiseScale=0.005 が normalizedScale=0.1 に相当
            return normalizedScale * 0.05f / (worldSize / 2000f);
        }
        
        /// <summary>
        /// MeshGenerator のノイズスケールを正規化された値に変換
        /// </summary>
        private static float ConvertNoiseScaleToNormalized(float noiseScale, float worldSize)
        {
            // 逆変換
            return noiseScale * (worldSize / 2000f) / 0.05f;
        }
        
        #endregion
        
        #region Validation
        
        /// <summary>
        /// パラメータの妥当性を検証
        /// </summary>
        public static bool Validate(UnifiedTerrainParams @params, out string errorMessage)
        {
            if (@params.worldSize <= 0)
            {
                errorMessage = "worldSize must be greater than 0";
                return false;
            }
            
            if (@params.maxElevation <= 0)
            {
                errorMessage = "maxElevation must be greater than 0";
                return false;
            }
            
            if (@params.meshResolution < 2)
            {
                errorMessage = "meshResolution must be at least 2";
                return false;
            }
            
            if (@params.noiseSettings.octaves < 1)
            {
                errorMessage = "noiseSettings.octaves must be at least 1";
                return false;
            }
            
            errorMessage = null;
            return true;
        }
        
        #endregion
    }
    
}
