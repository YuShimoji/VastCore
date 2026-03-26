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
    /// <summary>
    /// 複合建築構造生成システム
    /// 複数のアーチを組み合わせた複雑な建築構造を生成
    /// </summary>
    public static partial class CompoundArchitecturalGenerator
    {
        #region 複合建築タイプ定義
        public enum CompoundArchitecturalType
        {
            MultipleBridge,     // 複数アーチ橋
            AqueductSystem,     // 水道橋システム
            CathedralComplex,   // 大聖堂複合体
            FortressWall,       // 要塞壁
            Amphitheater,       // 円形劇場
            Basilica,           // バシリカ
            Cloister,           // 回廊
            TriumphalArch       // 凱旋門
        }
        #endregion

        #region 複合建築パラメータ
        [System.Serializable]
        public struct CompoundArchitecturalParams
        {
            [Header("基本設定")]
            public CompoundArchitecturalType compoundType;
            public Vector3 position;
            public Vector3 overallSize;
            public Quaternion rotation;
            
            [Header("構造配置")]
            public int structureCount;
            public float structureSpacing;
            public bool enableSymmetry;
            public float heightVariation;
            
            [Header("建築様式")]
            public ArchitecturalGenerator.ArchitecturalType baseArchType;
            public bool mixedStyles;
            public float styleVariationFactor;
            
            [Header("装飾統合")]
            public bool unifiedDecorations;
            public float decorationComplexity;
            public bool enableConnectingElements;
            
            [Header("材質設定")]
            public Material primaryMaterial;
            public Material secondaryMaterial;
            public Material decorationMaterial;
            
            public static CompoundArchitecturalParams Default(CompoundArchitecturalType type)
            {
                return new CompoundArchitecturalParams
                {
                    compoundType = type,
                    position = Vector3.zero,
                    overallSize = GetDefaultOverallSize(type),
                    rotation = Quaternion.identity,
                    structureCount = GetDefaultStructureCount(type),
                    structureSpacing = 100f,
                    enableSymmetry = true,
                    heightVariation = 0.2f,
                    baseArchType = ArchitecturalGenerator.ArchitecturalType.RomanArch,
                    mixedStyles = false,
                    styleVariationFactor = 0.1f,
                    unifiedDecorations = true,
                    decorationComplexity = 1.0f,
                    enableConnectingElements = true,
                    primaryMaterial = null,
                    secondaryMaterial = null,
                    decorationMaterial = null
                };
            }
            
            private static Vector3 GetDefaultOverallSize(CompoundArchitecturalType type)
            {
                switch (type)
                {
                    case CompoundArchitecturalType.MultipleBridge: return new Vector3(400f, 80f, 50f);
                    case CompoundArchitecturalType.AqueductSystem: return new Vector3(600f, 120f, 60f);
                    case CompoundArchitecturalType.CathedralComplex: return new Vector3(200f, 300f, 150f);
                    case CompoundArchitecturalType.FortressWall: return new Vector3(800f, 100f, 40f);
                    case CompoundArchitecturalType.Amphitheater: return new Vector3(300f, 60f, 300f);
                    case CompoundArchitecturalType.Basilica: return new Vector3(150f, 200f, 80f);
                    case CompoundArchitecturalType.Cloister: return new Vector3(120f, 50f, 120f);
                    case CompoundArchitecturalType.TriumphalArch: return new Vector3(100f, 150f, 30f);
                    default: return new Vector3(200f, 100f, 50f);
                }
            }
            
            private static int GetDefaultStructureCount(CompoundArchitecturalType type)
            {
                switch (type)
                {
                    case CompoundArchitecturalType.MultipleBridge: return 4;
                    case CompoundArchitecturalType.AqueductSystem: return 6;
                    case CompoundArchitecturalType.CathedralComplex: return 3;
                    case CompoundArchitecturalType.FortressWall: return 8;
                    case CompoundArchitecturalType.Amphitheater: return 12;
                    case CompoundArchitecturalType.Basilica: return 5;
                    case CompoundArchitecturalType.Cloister: return 16;
                    case CompoundArchitecturalType.TriumphalArch: return 3;
                    default: return 3;
                }
            }
        }
        #endregion

        #region メイン生成関数
        /// <summary>
        /// 複合建築構造を生成
        /// </summary>
        public static GameObject GenerateCompoundArchitecturalStructure(CompoundArchitecturalParams parameters)
        {
            try
            {
                if (parameters.overallSize <= 0f || parameters.structureCount <= 0)
                {
                    VastcoreLogger.Instance.LogError("CompoundArch", $"[CompoundArchitecturalGenerator] Invalid parameters: overallSize={parameters.overallSize}, structureCount={parameters.structureCount}. Must be > 0.");
                    return null;
                }

                GameObject compoundObject = new GameObject($"Compound_{parameters.compoundType}");
                compoundObject.transform.position = parameters.position;
                compoundObject.transform.rotation = parameters.rotation;

                // 複合建築タイプに応じた生成
                switch (parameters.compoundType)
                {
                    case CompoundArchitecturalType.MultipleBridge:
                        GenerateMultipleBridge(compoundObject, parameters);
                        break;
                    case CompoundArchitecturalType.AqueductSystem:
                        GenerateAqueductSystem(compoundObject, parameters);
                        break;
                    case CompoundArchitecturalType.CathedralComplex:
                        GenerateCathedralComplex(compoundObject, parameters);
                        break;
                    case CompoundArchitecturalType.FortressWall:
                        GenerateFortressWall(compoundObject, parameters);
                        break;
                    case CompoundArchitecturalType.Amphitheater:
                        GenerateAmphitheater(compoundObject, parameters);
                        break;
                    case CompoundArchitecturalType.Basilica:
                        GenerateBasilica(compoundObject, parameters);
                        break;
                    case CompoundArchitecturalType.Cloister:
                        GenerateCloister(compoundObject, parameters);
                        break;
                    case CompoundArchitecturalType.TriumphalArch:
                        GenerateTriumphalArch(compoundObject, parameters);
                        break;
                    default:
                        VastcoreLogger.Instance.LogWarning("CompoundArch", $"Compound architectural type {parameters.compoundType} not implemented");
                        GenerateMultipleBridge(compoundObject, parameters);
                        break;
                }

                // 接続要素を追加
                if (parameters.enableConnectingElements)
                {
                    AddConnectingElements(compoundObject, parameters);
                }

                // 統一装飾を追加
                if (parameters.unifiedDecorations)
                {
                    AddUnifiedDecorations(compoundObject, parameters);
                }

                // 複合コライダーを設定
                SetupCompoundColliders(compoundObject, parameters);

                VastcoreLogger.Instance.LogInfo("CompoundArch", $"Successfully generated compound architectural structure: {parameters.compoundType}");
                return compoundObject;
            }
            catch (System.Exception e)
            {
                VastcoreLogger.Instance.LogError("CompoundArch", $"Error generating compound architectural structure {parameters.compoundType}: {e.Message}", e);
                return null;
            }
        }
        #endregion

        #region ユーティリティ関数
        /// <summary>
        /// 複合建築タイプの説明を取得
        /// </summary>
        public static string GetCompoundArchitecturalDescription(CompoundArchitecturalType type)
        {
            switch (type)
            {
                case CompoundArchitecturalType.MultipleBridge: return "複数アーチ橋梁";
                case CompoundArchitecturalType.AqueductSystem: return "古代水道橋システム";
                case CompoundArchitecturalType.CathedralComplex: return "大聖堂建築複合体";
                case CompoundArchitecturalType.FortressWall: return "要塞城壁";
                case CompoundArchitecturalType.Amphitheater: return "古代円形劇場";
                case CompoundArchitecturalType.Basilica: return "バシリカ建築";
                case CompoundArchitecturalType.Cloister: return "修道院回廊";
                case CompoundArchitecturalType.TriumphalArch: return "凱旋門";
                default: return "不明な複合建築構造";
            }
        }

        /// <summary>
        /// ランダムな複合建築タイプを取得
        /// </summary>
        public static CompoundArchitecturalType GetRandomCompoundArchitecturalType()
        {
            var values = System.Enum.GetValues(typeof(CompoundArchitecturalType));
            return (CompoundArchitecturalType)values.GetValue(Random.Range(0, values.Length));
        }

        /// <summary>
        /// 複合建築構造の複雑度を計算
        /// </summary>
        public static float CalculateComplexityScore(CompoundArchitecturalParams parameters)
        {
            float baseComplexity = parameters.structureCount * 0.1f;
            float decorationComplexity = parameters.decorationComplexity * 0.3f;
            float sizeComplexity = (parameters.overallSize.magnitude / 1000f) * 0.2f;
            float connectionComplexity = parameters.enableConnectingElements ? 0.2f : 0f;
            float unificationComplexity = parameters.unifiedDecorations ? 0.2f : 0f;
            
            return baseComplexity + decorationComplexity + sizeComplexity + connectionComplexity + unificationComplexity;
        }

        /// <summary>
        /// 建築様式の互換性をチェック
        /// </summary>
        public static bool CheckStyleCompatibility(ArchitecturalGenerator.ArchitecturalType style1, ArchitecturalGenerator.ArchitecturalType style2)
        {
            // 同じ時代・地域の建築様式は互換性が高い
            var romanStyles = new[] { ArchitecturalGenerator.ArchitecturalType.RomanArch, ArchitecturalGenerator.ArchitecturalType.Colonnade };
            var gothicStyles = new[] { ArchitecturalGenerator.ArchitecturalType.GothicArch, ArchitecturalGenerator.ArchitecturalType.Cathedral };
            
            bool bothRoman = System.Array.IndexOf(romanStyles, style1) >= 0 && System.Array.IndexOf(romanStyles, style2) >= 0;
            bool bothGothic = System.Array.IndexOf(gothicStyles, style1) >= 0 && System.Array.IndexOf(gothicStyles, style2) >= 0;
            
            return bothRoman || bothGothic || style1 == style2;
        }

        /// <summary>
        /// 推定建設時間を計算（ゲーム内時間）
        /// </summary>
        public static float CalculateEstimatedConstructionTime(CompoundArchitecturalParams parameters)
        {
            float baseTime = parameters.structureCount * 10f; // 基本時間（秒）
            float complexityMultiplier = CalculateComplexityScore(parameters);
            float sizeMultiplier = parameters.overallSize.magnitude / 100f;
            
            return baseTime * complexityMultiplier * sizeMultiplier;
        }
        #endregion
    }
    }
}
