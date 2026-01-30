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
    /// 建築学的構造生成システム
    /// 構造力学に基づいた物理的に安定したアーチ構造を生成
    /// </summary>
    public static class ArchitecturalGenerator
    {
        #region 建築構造タイプ定義
        public enum ArchitecturalType
        {
            SimpleArch,         // 単純アーチ
            RomanArch,          // ローマ式アーチ
            GothicArch,         // ゴシック式アーチ
            Bridge,             // 橋梁
            Aqueduct,           // 水道橋
            Cathedral,          // 大聖堂
            Colonnade,          // 列柱廊
            Viaduct             // 高架橋
        }
        #endregion

        #region 建築生成パラメータ
        [System.Serializable]
        public struct ArchitecturalParams
        {
            [Header("基本設定")]
            public ArchitecturalType architecturalType;
            public Vector3 position;
            public float span;              // スパン（幅）
            public float height;            // 高さ
            public float thickness;         // 厚さ
            public Quaternion rotation;
            
            [Header("構造力学設定")]
            public float keyStoneRatio;     // キーストーンの比率
            public int archSegments;        // アーチのセグメント数
            public float compressionFactor; // 圧縮係数
            public bool enableStructuralOptimization; // 構造最適化
            
            [Header("装飾設定")]
            public bool enableDecorations; // 装飾要素
            public float decorationScale;  // 装飾スケール
            public int decorationDensity;  // 装飾密度
            
            [Header("材質設定")]
            public Material stoneMaterial;
            public Material decorationMaterial;
            public bool weatheringEffect;   // 風化効果
            
            public static ArchitecturalParams Default(ArchitecturalType type)
            {
                return new ArchitecturalParams
                {
                    architecturalType = type,
                    position = Vector3.zero,
                    span = GetDefaultSpan(type),
                    height = GetDefaultHeight(type),
                    thickness = GetDefaultThickness(type),
                    rotation = Quaternion.identity,
                    keyStoneRatio = 0.15f,
                    archSegments = 12,
                    compressionFactor = 1.2f,
                    enableStructuralOptimization = true,
                    enableDecorations = true,
                    decorationScale = 1.0f,
                    decorationDensity = 3,
                    stoneMaterial = null,
                    decorationMaterial = null,
                    weatheringEffect = true
                };
            }
            
            private static float GetDefaultSpan(ArchitecturalType type)
            {
                switch (type)
                {
                    case ArchitecturalType.SimpleArch: return 50f;
                    case ArchitecturalType.RomanArch: return 80f;
                    case ArchitecturalType.GothicArch: return 60f;
                    case ArchitecturalType.Bridge: return 200f;
                    case ArchitecturalType.Aqueduct: return 150f;
                    case ArchitecturalType.Cathedral: return 120f;
                    case ArchitecturalType.Colonnade: return 40f;
                    case ArchitecturalType.Viaduct: return 300f;
                    default: return 50f;
                }
            }
            
            private static float GetDefaultHeight(ArchitecturalType type)
            {
                switch (type)
                {
                    case ArchitecturalType.SimpleArch: return 30f;
                    case ArchitecturalType.RomanArch: return 40f;
                    case ArchitecturalType.GothicArch: return 80f;
                    case ArchitecturalType.Bridge: return 60f;
                    case ArchitecturalType.Aqueduct: return 100f;
                    case ArchitecturalType.Cathedral: return 200f;
                    case ArchitecturalType.Colonnade: return 50f;
                    case ArchitecturalType.Viaduct: return 150f;
                    default: return 30f;
                }
            }
            
            private static float GetDefaultThickness(ArchitecturalType type)
            {
                switch (type)
                {
                    case ArchitecturalType.SimpleArch: return 8f;
                    case ArchitecturalType.RomanArch: return 12f;
                    case ArchitecturalType.GothicArch: return 6f;
                    case ArchitecturalType.Bridge: return 20f;
                    case ArchitecturalType.Aqueduct: return 15f;
                    case ArchitecturalType.Cathedral: return 25f;
                    case ArchitecturalType.Colonnade: return 5f;
                    case ArchitecturalType.Viaduct: return 30f;
                    default: return 8f;
                }
            }
        }
        #endregion

        #region メイン生成関数
        /// <summary>
        /// 建築構造を生成
        /// </summary>
        public static GameObject GenerateArchitecturalStructure(ArchitecturalParams parameters)
        {
            try
            {
                GameObject architecturalObject = new GameObject($"Architectural_{parameters.architecturalType}");
                architecturalObject.transform.position = parameters.position;
                architecturalObject.transform.rotation = parameters.rotation;

                // 構造力学に基づく最適化
                if (parameters.enableStructuralOptimization)
                {
                    parameters = OptimizeStructuralParameters(parameters);
                }

                // 建築タイプに応じた生成
                switch (parameters.architecturalType)
                {
                    case ArchitecturalType.SimpleArch:
                        GenerateSimpleArch(architecturalObject, parameters);
                        break;
                    case ArchitecturalType.RomanArch:
                        GenerateRomanArch(architecturalObject, parameters);
                        break;
                    case ArchitecturalType.GothicArch:
                        GenerateGothicArch(architecturalObject, parameters);
                        break;
                    case ArchitecturalType.Bridge:
                        GenerateBridge(architecturalObject, parameters);
                        break;
                    case ArchitecturalType.Aqueduct:
                        GenerateAqueduct(architecturalObject, parameters);
                        break;
                    case ArchitecturalType.Cathedral:
                        GenerateCathedral(architecturalObject, parameters);
                        break;
                    case ArchitecturalType.Colonnade:
                        GenerateColonnade(architecturalObject, parameters);
                        break;
                    case ArchitecturalType.Viaduct:
                        GenerateViaduct(architecturalObject, parameters);
                        break;
                    default:
                        Debug.LogWarning($"Architectural type {parameters.architecturalType} not implemented");
                        GenerateSimpleArch(architecturalObject, parameters);
                        break;
                }

                // 装飾要素を追加
                if (parameters.enableDecorations)
                {
                    AddArchitecturalDecorations(architecturalObject, parameters);
                }

                // 風化効果を適用
                if (parameters.weatheringEffect)
                {
                    ApplyWeatheringEffect(architecturalObject, parameters);
                }

                // コライダーとインタラクション設定
                SetupArchitecturalColliders(architecturalObject, parameters);
                SetupArchitecturalInteractions(architecturalObject, parameters);

                Debug.Log($"Successfully generated architectural structure: {parameters.architecturalType}");
                return architecturalObject;
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Error generating architectural structure {parameters.architecturalType}: {e.Message}");
                return null;
            }
        }
        #endregion

        #region 構造力学計算
        /// <summary>
        /// 構造力学に基づくパラメータ最適化
        /// </summary>
        private static ArchitecturalParams OptimizeStructuralParameters(ArchitecturalParams parameters)
        {
            // スパンと高さの比率を構造的に安定させる
            float optimalHeightRatio = CalculateOptimalHeightRatio(parameters.span, parameters.architecturalType);
            parameters.height = Mathf.Max(parameters.height, parameters.span * optimalHeightRatio);

            // 厚さを構造荷重に基づいて調整
            float requiredThickness = CalculateRequiredThickness(parameters.span, parameters.height);
            parameters.thickness = Mathf.Max(parameters.thickness, requiredThickness);

            // キーストーン比率を最適化
            parameters.keyStoneRatio = CalculateOptimalKeystoneRatio(parameters.span, parameters.height);

            // セグメント数を構造的に最適化
            parameters.archSegments = CalculateOptimalSegments(parameters.span, parameters.thickness);

            return parameters;
        }

        /// <summary>
        /// 最適な高さ比率を計算
        /// </summary>
        private static float CalculateOptimalHeightRatio(float span, ArchitecturalType type)
        {
            switch (type)
            {
                case ArchitecturalType.SimpleArch:
                case ArchitecturalType.RomanArch:
                    return 0.5f; // 半円アーチ
                case ArchitecturalType.GothicArch:
                    return 1.2f; // 尖頭アーチ
                case ArchitecturalType.Bridge:
                    return 0.3f; // 低いアーチ
                case ArchitecturalType.Aqueduct:
                    return 0.6f; // やや高いアーチ
                case ArchitecturalType.Cathedral:
                    return 1.5f; // 非常に高いアーチ
                default:
                    return 0.5f;
            }
        }

        /// <summary>
        /// 必要な厚さを計算（構造荷重に基づく）
        /// </summary>
        private static float CalculateRequiredThickness(float span, float height)
        {
            // 簡略化された構造計算
            float loadFactor = (span * span) / (8 * height); // 最大モーメント係数
            float requiredThickness = Mathf.Sqrt(loadFactor) * 0.1f; // 安全係数を含む
            return Mathf.Max(requiredThickness, span * 0.05f); // 最小厚さ制限
        }

        /// <summary>
        /// 最適なキーストーン比率を計算
        /// </summary>
        private static float CalculateOptimalKeystoneRatio(float span, float height)
        {
            // アーチの形状に基づくキーストーンサイズ
            float curvature = height / span;
            return Mathf.Clamp(0.1f + curvature * 0.1f, 0.08f, 0.25f);
        }

        /// <summary>
        /// 最適なセグメント数を計算
        /// </summary>
        private static int CalculateOptimalSegments(float span, float thickness)
        {
            // スパンと厚さに基づく適切なセグメント数
            int baseSegments = Mathf.RoundToInt(span / 10f);
            return Mathf.Clamp(baseSegments, 8, 24);
        }
        #endregion

        #region アーチ形状計算
        /// <summary>
        /// アーチの形状曲線を計算
        /// </summary>
        private static Vector3[] CalculateArchCurve(float span, float height, int segments, ArchitecturalType type)
        {
            Vector3[] points = new Vector3[segments + 1];
            
            for (int i = 0; i <= segments; i++)
            {
                float t = (float)i / segments;
                float x = (t - 0.5f) * span;
                float y = CalculateArchHeight(t, height, type);
                points[i] = new Vector3(x, y, 0);
            }
            
            return points;
        }

        /// <summary>
        /// アーチタイプに応じた高さを計算
        /// </summary>
        private static float CalculateArchHeight(float t, float maxHeight, ArchitecturalType type)
        {
            switch (type)
            {
                case ArchitecturalType.SimpleArch:
                case ArchitecturalType.RomanArch:
                    // 半円アーチ
                    return maxHeight * Mathf.Sin(t * Mathf.PI);
                    
                case ArchitecturalType.GothicArch:
                    // 尖頭アーチ（2つの円弧の組み合わせ）
                    if (t < 0.5f)
                    {
                        float localT = t * 2f;
                        return maxHeight * Mathf.Sqrt(1f - (1f - localT) * (1f - localT));
                    }
                    else
                    {
                        float localT = (t - 0.5f) * 2f;
                        return maxHeight * Mathf.Sqrt(1f - localT * localT);
                    }
                    
                case ArchitecturalType.Bridge:
                case ArchitecturalType.Viaduct:
                    // 放物線アーチ（より構造的に効率的）
                    return maxHeight * (1f - 4f * (t - 0.5f) * (t - 0.5f));
                    
                default:
                    return maxHeight * Mathf.Sin(t * Mathf.PI);
            }
        }

        /// <summary>
        /// キーストーンの位置とサイズを計算
        /// </summary>
        private static (Vector3 position, Vector3 size) CalculateKeystone(Vector3[] archCurve, float keystoneRatio, float thickness)
        {
            int centerIndex = archCurve.Length / 2;
            Vector3 centerPoint = archCurve[centerIndex];
            
            float keystoneWidth = Vector3.Distance(archCurve[centerIndex - 1], archCurve[centerIndex + 1]) * keystoneRatio;
            float keystoneHeight = keystoneWidth * 0.8f;
            
            Vector3 position = centerPoint + Vector3.up * keystoneHeight * 0.5f;
            Vector3 size = new Vector3(keystoneWidth, keystoneHeight, thickness * 1.2f);
            
            return (position, size);
        }
        #endregion

        #region 基本アーチ生成
        /// <summary>
        /// 単純アーチを生成
        /// </summary>
        private static void GenerateSimpleArch(GameObject parent, ArchitecturalParams parameters)
        {
            // アーチ曲線を計算
            Vector3[] archCurve = CalculateArchCurve(parameters.span, parameters.height, parameters.archSegments, parameters.architecturalType);
            
            // 支柱を生成
            GenerateArchPillars(parent, parameters, archCurve);
            
            // アーチ本体を生成
            GenerateArchBody(parent, parameters, archCurve);
            
            // キーストーンを生成
            GenerateKeystone(parent, parameters, archCurve);
        }

        /// <summary>
        /// ローマ式アーチを生成
        /// </summary>
        private static void GenerateRomanArch(GameObject parent, ArchitecturalParams parameters)
        {
            // ローマ式の特徴：半円アーチ + 装飾的な支柱
            GenerateSimpleArch(parent, parameters);
        }

        /// <summary>
        /// ゴシック式アーチを生成
        /// </summary>
        private static void GenerateGothicArch(GameObject parent, ArchitecturalParams parameters)
        {
            // ゴシック式の特徴：尖頭アーチ + 飛び梁
            Vector3[] archCurve = CalculateArchCurve(parameters.span, parameters.height, parameters.archSegments, parameters.architecturalType);
            
            // 支柱を生成（より細く高い）
            GenerateGothicPillars(parent, parameters, archCurve);
            
            // 尖頭アーチ本体を生成
            GenerateArchBody(parent, parameters, archCurve);
            
            // ゴシック式キーストーンを生成
            GenerateGothicKeystone(parent, parameters, archCurve);
            
            // 飛び梁を追加
            GenerateFlyingButtresses(parent, parameters);
        }

        /// <summary>
        /// 橋梁を生成
        /// </summary>
        private static void GenerateBridge(GameObject parent, ArchitecturalParams parameters)
        {
            // 橋梁は複数のアーチの組み合わせ
            int archCount = Mathf.Max(1, Mathf.RoundToInt(parameters.span / 100f));
            float archSpan = parameters.span / archCount;
            
            for (int i = 0; i < archCount; i++)
            {
                var archParams = parameters;
                archParams.span = archSpan;
                archParams.position = new Vector3((i - archCount * 0.5f + 0.5f) * archSpan, 0, 0);
                
                GenerateSimpleArch(parent, archParams);
            }
            
            // 橋面を追加
            GenerateBridgeDeck(parent, parameters);
        }

        /// <summary>
        /// 水道橋を生成
        /// </summary>
        private static void GenerateAqueduct(GameObject parent, ArchitecturalParams parameters)
        {
            // 水道橋は多層アーチ構造
            int levels = 2;
            
            for (int level = 0; level < levels; level++)
            {
                var levelParams = parameters;
                levelParams.height = parameters.height * (0.6f + level * 0.4f);
                levelParams.position = new Vector3(0, level * parameters.height * 0.7f, 0);
                
                GenerateBridge(parent, levelParams);
            }
        }

        /// <summary>
        /// 大聖堂を生成
        /// </summary>
        private static void GenerateCathedral(GameObject parent, ArchitecturalParams parameters)
        {
            // 大聖堂は複雑な構造
            GenerateGothicArch(parent, parameters);
            
            // 側廊を追加
            GenerateSideAisles(parent, parameters);
            
            // 塔を追加
            GenerateTowers(parent, parameters);
        }

        /// <summary>
        /// 列柱廊を生成
        /// </summary>
        private static void GenerateColonnade(GameObject parent, ArchitecturalParams parameters)
        {
            int columnCount = Mathf.RoundToInt(parameters.span / 20f);
            
            for (int i = 0; i < columnCount; i++)
            {
                float x = (i - columnCount * 0.5f + 0.5f) * (parameters.span / columnCount);
                Vector3 position = new Vector3(x, parameters.height * 0.5f, 0);
                
                CreateColumn(parent, $"Column_{i}", position, parameters);
            }
            
            // 上部の梁を追加
            GenerateEntablature(parent, parameters);
        }

        /// <summary>
        /// 高架橋を生成
        /// </summary>
        private static void GenerateViaduct(GameObject parent, ArchitecturalParams parameters)
        {
            // 高架橋は非常に大きな橋梁
            var viaductParams = parameters;
            viaductParams.height *= 1.5f;
            viaductParams.thickness *= 1.2f;
            
            GenerateBridge(parent, viaductParams);
            
            // 追加の支持構造
            GenerateViaductSupports(parent, parameters);
        }
        #endregion 
       #region アーチ構成要素生成
        /// <summary>
        /// アーチの支柱を生成
        /// </summary>
        private static void GenerateArchPillars(GameObject parent, ArchitecturalParams parameters, Vector3[] archCurve)
        {
            float pillarWidth = parameters.thickness * 1.5f;
            float pillarHeight = parameters.height * 1.2f;
            float pillarDepth = parameters.thickness;
            
            // 左の支柱
            Vector3 leftPillarPos = new Vector3(-parameters.span * 0.5f, pillarHeight * 0.5f, 0);
            CreatePillar(parent, "LeftPillar", leftPillarPos, new Vector3(pillarWidth, pillarHeight, pillarDepth), parameters);
            
            // 右の支柱
            Vector3 rightPillarPos = new Vector3(parameters.span * 0.5f, pillarHeight * 0.5f, 0);
            CreatePillar(parent, "RightPillar", rightPillarPos, new Vector3(pillarWidth, pillarHeight, pillarDepth), parameters);
        }

        /// <summary>
        /// 支柱を作成
        /// </summary>
        private static void CreatePillar(GameObject parent, string name, Vector3 position, Vector3 size, ArchitecturalParams parameters)
        {
            var pillar = ShapeGenerator.CreateShape(ShapeType.Cube);
            pillar.name = name;
            pillar.transform.SetParent(parent.transform);
            pillar.transform.localPosition = position;
            pillar.transform.localScale = size;
            
            // 材質を設定
            if (parameters.stoneMaterial != null)
            {
                pillar.GetComponent<MeshRenderer>().material = parameters.stoneMaterial;
            }
        }

        /// <summary>
        /// アーチ本体を生成
        /// </summary>
        private static void GenerateArchBody(GameObject parent, ArchitecturalParams parameters, Vector3[] archCurve)
        {
            GameObject archBody = new GameObject("ArchBody");
            archBody.transform.SetParent(parent.transform);
            
            // アーチのセグメントを生成
            for (int i = 0; i < archCurve.Length - 1; i++)
            {
                CreateArchSegment(archBody, i, archCurve[i], archCurve[i + 1], parameters);
            }
        }

        /// <summary>
        /// アーチセグメントを作成
        /// </summary>
        private static void CreateArchSegment(GameObject parent, int index, Vector3 startPoint, Vector3 endPoint, ArchitecturalParams parameters)
        {
            Vector3 center = (startPoint + endPoint) * 0.5f;
            Vector3 direction = (endPoint - startPoint).normalized;
            float length = Vector3.Distance(startPoint, endPoint);
            
            var segment = ShapeGenerator.CreateShape(ShapeType.Cube);
            segment.name = $"ArchSegment_{index}";
            segment.transform.SetParent(parent.transform);
            segment.transform.localPosition = center;
            segment.transform.localScale = new Vector3(length, parameters.thickness * 0.8f, parameters.thickness);
            
            // セグメントを適切な角度に回転
            if (direction != Vector3.zero)
            {
                segment.transform.localRotation = Quaternion.LookRotation(Vector3.forward, direction);
            }
            
            // 材質を設定
            if (parameters.stoneMaterial != null)
            {
                segment.GetComponent<MeshRenderer>().material = parameters.stoneMaterial;
            }
        }

        /// <summary>
        /// キーストーンを生成
        /// </summary>
        private static void GenerateKeystone(GameObject parent, ArchitecturalParams parameters, Vector3[] archCurve)
        {
            var (position, size) = CalculateKeystone(archCurve, parameters.keyStoneRatio, parameters.thickness);
            
            var keystone = ShapeGenerator.CreateShape(ShapeType.Cube);
            keystone.name = "Keystone";
            keystone.transform.SetParent(parent.transform);
            keystone.transform.localPosition = position;
            keystone.transform.localScale = size;
            
            // キーストーン特有の形状変形
            ApplyKeystoneDeformation(keystone, parameters);
            
            // 特別な材質を設定
            if (parameters.decorationMaterial != null)
            {
                keystone.GetComponent<MeshRenderer>().material = parameters.decorationMaterial;
            }
            else if (parameters.stoneMaterial != null)
            {
                keystone.GetComponent<MeshRenderer>().material = parameters.stoneMaterial;
            }
        }

        /// <summary>
        /// キーストーンの形状変形を適用
        /// </summary>
        private static void ApplyKeystoneDeformation(ProBuilderMesh keystone, ArchitecturalParams parameters)
        {
            var vertices = keystone.positions.ToArray();
            
            // キーストーン特有の台形形状を作成
            for (int i = 0; i < vertices.Length; i++)
            {
                Vector3 vertex = vertices[i];
                
                // 上部を狭くして台形にする
                if (vertex.y > 0)
                {
                    vertex.x *= 0.7f;
                    vertex.z *= 0.9f;
                }
                
                vertices[i] = vertex;
            }
            
            keystone.positions = vertices;
            keystone.ToMesh();
            keystone.Refresh();
        }

        /// <summary>
        /// ゴシック式支柱を生成
        /// </summary>
        private static void GenerateGothicPillars(GameObject parent, ArchitecturalParams parameters, Vector3[] archCurve)
        {
            float pillarWidth = parameters.thickness * 1.2f;
            float pillarHeight = parameters.height * 1.4f; // より高い
            float pillarDepth = parameters.thickness * 0.8f; // より細い
            
            // 左の支柱（束柱付き）
            Vector3 leftPillarPos = new Vector3(-parameters.span * 0.5f, pillarHeight * 0.5f, 0);
            CreateGothicPillar(parent, "LeftGothicPillar", leftPillarPos, new Vector3(pillarWidth, pillarHeight, pillarDepth), parameters);
            
            // 右の支柱（束柱付き）
            Vector3 rightPillarPos = new Vector3(parameters.span * 0.5f, pillarHeight * 0.5f, 0);
            CreateGothicPillar(parent, "RightGothicPillar", rightPillarPos, new Vector3(pillarWidth, pillarHeight, pillarDepth), parameters);
        }

        /// <summary>
        /// ゴシック式支柱を作成
        /// </summary>
        private static void CreateGothicPillar(GameObject parent, string name, Vector3 position, Vector3 size, ArchitecturalParams parameters)
        {
            GameObject pillarGroup = new GameObject(name);
            pillarGroup.transform.SetParent(parent.transform);
            pillarGroup.transform.localPosition = position;
            
            // メイン支柱
            var mainPillar = ShapeGenerator.CreateShape(ShapeType.Cube);
            mainPillar.name = "MainPillar";
            mainPillar.transform.SetParent(pillarGroup.transform);
            mainPillar.transform.localPosition = Vector3.zero;
            mainPillar.transform.localScale = size;
            
            // 束柱（複数の細い柱）
            for (int i = 0; i < 4; i++)
            {
                float angle = i * 90f;
                Vector3 offset = Quaternion.Euler(0, angle, 0) * Vector3.forward * size.x * 0.3f;
                
                var bundlePillar = ShapeGenerator.CreateShape(ShapeType.Cylinder);
                bundlePillar.name = $"BundlePillar_{i}";
                bundlePillar.transform.SetParent(pillarGroup.transform);
                bundlePillar.transform.localPosition = offset;
                bundlePillar.transform.localScale = new Vector3(size.x * 0.2f, size.y, size.x * 0.2f);
                
                if (parameters.stoneMaterial != null)
                {
                    bundlePillar.GetComponent<MeshRenderer>().material = parameters.stoneMaterial;
                }
            }
            
            if (parameters.stoneMaterial != null)
            {
                mainPillar.GetComponent<MeshRenderer>().material = parameters.stoneMaterial;
            }
        }

        /// <summary>
        /// ゴシック式キーストーンを生成
        /// </summary>
        private static void GenerateGothicKeystone(GameObject parent, ArchitecturalParams parameters, Vector3[] archCurve)
        {
            var (position, size) = CalculateKeystone(archCurve, parameters.keyStoneRatio, parameters.thickness);
            
            // ゴシック式は装飾的なキーストーン
            var keystone = ShapeGenerator.CreateShape(ShapeType.Cube);
            keystone.name = "GothicKeystone";
            keystone.transform.SetParent(parent.transform);
            keystone.transform.localPosition = position;
            keystone.transform.localScale = size * 1.2f; // より大きく
            
            // ゴシック式の装飾変形
            ApplyGothicKeystoneDeformation(keystone, parameters);
            
            if (parameters.decorationMaterial != null)
            {
                keystone.GetComponent<MeshRenderer>().material = parameters.decorationMaterial;
            }
        }

        /// <summary>
        /// ゴシック式キーストーンの変形
        /// </summary>
        private static void ApplyGothicKeystoneDeformation(ProBuilderMesh keystone, ArchitecturalParams parameters)
        {
            var vertices = keystone.positions.ToArray();
            
            for (int i = 0; i < vertices.Length; i++)
            {
                Vector3 vertex = vertices[i];
                
                // より複雑な装飾形状
                if (vertex.y > 0)
                {
                    // 上部に装飾的な突起
                    vertex.y *= 1.3f;
                    vertex.x *= 0.6f;
                }
                
                vertices[i] = vertex;
            }
            
            keystone.positions = vertices;
            keystone.ToMesh();
            keystone.Refresh();
        }

        /// <summary>
        /// 飛び梁を生成
        /// </summary>
        private static void GenerateFlyingButtresses(GameObject parent, ArchitecturalParams parameters)
        {
            float buttressLength = parameters.span * 0.3f;
            float buttressHeight = parameters.height * 0.6f;
            
            // 左側の飛び梁
            CreateFlyingButtress(parent, "LeftButtress", 
                new Vector3(-parameters.span * 0.7f, buttressHeight, 0),
                new Vector3(-parameters.span * 0.5f, parameters.height * 0.8f, 0),
                parameters);
            
            // 右側の飛び梁
            CreateFlyingButtress(parent, "RightButtress",
                new Vector3(parameters.span * 0.7f, buttressHeight, 0),
                new Vector3(parameters.span * 0.5f, parameters.height * 0.8f, 0),
                parameters);
        }

        /// <summary>
        /// 飛び梁を作成
        /// </summary>
        private static void CreateFlyingButtress(GameObject parent, string name, Vector3 startPos, Vector3 endPos, ArchitecturalParams parameters)
        {
            Vector3 center = (startPos + endPos) * 0.5f;
            Vector3 direction = (endPos - startPos).normalized;
            float length = Vector3.Distance(startPos, endPos);
            
            var buttress = ShapeGenerator.CreateShape(ShapeType.Cube);
            buttress.name = name;
            buttress.transform.SetParent(parent.transform);
            buttress.transform.localPosition = center;
            buttress.transform.localScale = new Vector3(length, parameters.thickness * 0.6f, parameters.thickness * 0.4f);
            buttress.transform.localRotation = Quaternion.LookRotation(Vector3.forward, direction);
            
            if (parameters.stoneMaterial != null)
            {
                buttress.GetComponent<MeshRenderer>().material = parameters.stoneMaterial;
            }
        }
        #endregion      
  #region 複合構造生成
        /// <summary>
        /// 橋面を生成
        /// </summary>
        private static void GenerateBridgeDeck(GameObject parent, ArchitecturalParams parameters)
        {
            Vector3 position = new Vector3(0, parameters.height * 1.1f, 0);
            Vector3 size = new Vector3(parameters.span, parameters.thickness * 0.5f, parameters.thickness * 2f);
            
            var deck = ShapeGenerator.CreateShape(ShapeType.Cube);
            deck.name = "BridgeDeck";
            deck.transform.SetParent(parent.transform);
            deck.transform.localPosition = position;
            deck.transform.localScale = size;
            
            if (parameters.stoneMaterial != null)
            {
                deck.GetComponent<MeshRenderer>().material = parameters.stoneMaterial;
            }
        }

        /// <summary>
        /// 側廊を生成
        /// </summary>
        private static void GenerateSideAisles(GameObject parent, ArchitecturalParams parameters)
        {
            float aisleWidth = parameters.span * 0.3f;
            
            // 左側廊
            var leftAisleParams = parameters;
            leftAisleParams.span = aisleWidth;
            leftAisleParams.height *= 0.7f;
            leftAisleParams.position = new Vector3(-parameters.span * 0.6f, 0, 0);
            GenerateSimpleArch(parent, leftAisleParams);
            
            // 右側廊
            var rightAisleParams = parameters;
            rightAisleParams.span = aisleWidth;
            rightAisleParams.height *= 0.7f;
            rightAisleParams.position = new Vector3(parameters.span * 0.6f, 0, 0);
            GenerateSimpleArch(parent, rightAisleParams);
        }

        /// <summary>
        /// 塔を生成
        /// </summary>
        private static void GenerateTowers(GameObject parent, ArchitecturalParams parameters)
        {
            float towerHeight = parameters.height * 2f;
            float towerWidth = parameters.thickness * 3f;
            
            // 左の塔
            CreateTower(parent, "LeftTower", 
                new Vector3(-parameters.span * 0.6f, towerHeight * 0.5f, 0),
                new Vector3(towerWidth, towerHeight, towerWidth), parameters);
            
            // 右の塔
            CreateTower(parent, "RightTower",
                new Vector3(parameters.span * 0.6f, towerHeight * 0.5f, 0),
                new Vector3(towerWidth, towerHeight, towerWidth), parameters);
        }

        /// <summary>
        /// 塔を作成
        /// </summary>
        private static void CreateTower(GameObject parent, string name, Vector3 position, Vector3 size, ArchitecturalParams parameters)
        {
            var tower = ShapeGenerator.CreateShape(ShapeType.Cube);
            tower.name = name;
            tower.transform.SetParent(parent.transform);
            tower.transform.localPosition = position;
            tower.transform.localScale = size;
            
            // 塔の上部に尖塔を追加
            var spire = ShapeGenerator.CreateShape(ShapeType.Cone);
            spire.name = "Spire";
            spire.transform.SetParent(tower.transform);
            spire.transform.localPosition = new Vector3(0, 0.6f, 0);
            spire.transform.localScale = new Vector3(0.8f, 0.4f, 0.8f);
            
            if (parameters.stoneMaterial != null)
            {
                tower.GetComponent<MeshRenderer>().material = parameters.stoneMaterial;
                spire.GetComponent<MeshRenderer>().material = parameters.stoneMaterial;
            }
        }

        /// <summary>
        /// 柱を作成
        /// </summary>
        private static void CreateColumn(GameObject parent, string name, Vector3 position, ArchitecturalParams parameters)
        {
            var column = ShapeGenerator.CreateShape(ShapeType.Cylinder);
            column.name = name;
            column.transform.SetParent(parent.transform);
            column.transform.localPosition = position;
            column.transform.localScale = new Vector3(parameters.thickness * 0.8f, parameters.height, parameters.thickness * 0.8f);
            
            if (parameters.stoneMaterial != null)
            {
                column.GetComponent<MeshRenderer>().material = parameters.stoneMaterial;
            }
        }

        /// <summary>
        /// エンタブラチュアを生成
        /// </summary>
        private static void GenerateEntablature(GameObject parent, ArchitecturalParams parameters)
        {
            Vector3 position = new Vector3(0, parameters.height * 1.1f, 0);
            Vector3 size = new Vector3(parameters.span, parameters.thickness * 0.6f, parameters.thickness);
            
            var entablature = ShapeGenerator.CreateShape(ShapeType.Cube);
            entablature.name = "Entablature";
            entablature.transform.SetParent(parent.transform);
            entablature.transform.localPosition = position;
            entablature.transform.localScale = size;
            
            if (parameters.decorationMaterial != null)
            {
                entablature.GetComponent<MeshRenderer>().material = parameters.decorationMaterial;
            }
        }

        /// <summary>
        /// 高架橋支持構造を生成
        /// </summary>
        private static void GenerateViaductSupports(GameObject parent, ArchitecturalParams parameters)
        {
            int supportCount = Mathf.RoundToInt(parameters.span / 80f);
            
            for (int i = 0; i < supportCount; i++)
            {
                float x = (i - supportCount * 0.5f + 0.5f) * (parameters.span / supportCount);
                Vector3 position = new Vector3(x, parameters.height * 0.3f, parameters.thickness * 2f);
                
                CreateViaductSupport(parent, $"Support_{i}", position, parameters);
            }
        }

        /// <summary>
        /// 高架橋支持体を作成
        /// </summary>
        private static void CreateViaductSupport(GameObject parent, string name, Vector3 position, ArchitecturalParams parameters)
        {
            var support = ShapeGenerator.CreateShape(ShapeType.Cube);
            support.name = name;
            support.transform.SetParent(parent.transform);
            support.transform.localPosition = position;
            support.transform.localScale = new Vector3(parameters.thickness, parameters.height * 0.6f, parameters.thickness * 1.5f);
            
            if (parameters.stoneMaterial != null)
            {
                support.GetComponent<MeshRenderer>().material = parameters.stoneMaterial;
            }
        }
        #endregion

        #region 装飾・効果システム
        /// <summary>
        /// 建築装飾を追加
        /// </summary>
        private static void AddArchitecturalDecorations(GameObject parent, ArchitecturalParams parameters)
        {
            // 基本的な装飾要素を追加
            if (parameters.decorationDensity > 0)
            {
                AddBasicDecorations(parent, parameters);
            }
        }

        /// <summary>
        /// 基本装飾を追加
        /// </summary>
        private static void AddBasicDecorations(GameObject parent, ArchitecturalParams parameters)
        {
            // 装飾的なモールディングを追加
            for (int i = 0; i < parameters.decorationDensity; i++)
            {
                float height = parameters.height * (0.2f + i * 0.3f);
                CreateDecorativeMolding(parent, $"Decoration_{i}", height, parameters);
            }
        }

        /// <summary>
        /// 装飾的なモールディングを作成
        /// </summary>
        private static void CreateDecorativeMolding(GameObject parent, string name, float height, ArchitecturalParams parameters)
        {
            Vector3 position = new Vector3(0, height, 0);
            Vector3 size = new Vector3(parameters.span * 1.05f, parameters.thickness * 0.2f, parameters.thickness * 1.1f);
            
            var molding = ShapeGenerator.CreateShape(ShapeType.Cube);
            molding.name = name;
            molding.transform.SetParent(parent.transform);
            molding.transform.localPosition = position;
            molding.transform.localScale = size;
            
            if (parameters.decorationMaterial != null)
            {
                molding.GetComponent<MeshRenderer>().material = parameters.decorationMaterial;
            }
        }

        /// <summary>
        /// 風化効果を適用
        /// </summary>
        private static void ApplyWeatheringEffect(GameObject parent, ArchitecturalParams parameters)
        {
            // 全ての子オブジェクトに風化効果を適用
            var renderers = parent.GetComponentsInChildren<MeshRenderer>();
            
            foreach (var renderer in renderers)
            {
                if (renderer.material != null)
                {
                    // 色を少し暗くして古い感じを演出
                    var material = new Material(renderer.material);
                    material.color *= 0.8f;
                    renderer.material = material;
                }
            }
        }

        /// <summary>
        /// 建築コライダーを設定
        /// </summary>
        private static void SetupArchitecturalColliders(GameObject parent, ArchitecturalParams parameters)
        {
            // 親オブジェクトに複合コライダーを追加
            var meshCollider = parent.AddComponent<MeshCollider>();
            meshCollider.convex = false;
            
            // 全ての子メッシュを結合してコライダーを作成
            CombineMeshesForCollider(parent, meshCollider);
        }

        /// <summary>
        /// メッシュを結合してコライダーを作成
        /// </summary>
        private static void CombineMeshesForCollider(GameObject parent, MeshCollider collider)
        {
            MeshCombineHelper.CombineChildrenToCollider(parent, collider, "ArchitecturalGenerator");
        }

        /// <summary>
        /// 建築インタラクションを設定
        /// </summary>
        private static void SetupArchitecturalInteractions(GameObject parent, ArchitecturalParams parameters)
        {
            // プリミティブ地形オブジェクトコンポーネントを追加
            var architecturalComponent = parent.AddComponent<PrimitiveTerrainObject>();
            architecturalComponent.primitiveType = GenerationPrimitiveType.Arch;
            architecturalComponent.isClimbable = true;
            architecturalComponent.isGrindable = true;
            architecturalComponent.hasCollision = true;
            
            // 建築物専用のタグを設定
            parent.tag = "Architecture";
        }
        #endregion

        #region ユーティリティ関数
        /// <summary>
        /// 建築タイプの説明を取得
        /// </summary>
        public static string GetArchitecturalDescription(ArchitecturalType type)
        {
            switch (type)
            {
                case ArchitecturalType.SimpleArch: return "単純なアーチ構造";
                case ArchitecturalType.RomanArch: return "ローマ式半円アーチ";
                case ArchitecturalType.GothicArch: return "ゴシック式尖頭アーチ";
                case ArchitecturalType.Bridge: return "石造橋梁";
                case ArchitecturalType.Aqueduct: return "古代水道橋";
                case ArchitecturalType.Cathedral: return "大聖堂建築";
                case ArchitecturalType.Colonnade: return "列柱廊";
                case ArchitecturalType.Viaduct: return "高架橋";
                default: return "不明な建築構造";
            }
        }

        /// <summary>
        /// ランダムな建築タイプを取得
        /// </summary>
        public static ArchitecturalType GetRandomArchitecturalType()
        {
            var values = System.Enum.GetValues(typeof(ArchitecturalType));
            return (ArchitecturalType)values.GetValue(Random.Range(0, values.Length));
        }

        /// <summary>
        /// 建築構造の構造的安定性を検証
        /// </summary>
        public static bool ValidateStructuralStability(ArchitecturalParams parameters)
        {
            // 基本的な構造チェック
            float heightToSpanRatio = parameters.height / parameters.span;
            float thicknessToSpanRatio = parameters.thickness / parameters.span;
            
            // 構造的に不安定な比率をチェック
            if (heightToSpanRatio < 0.2f || heightToSpanRatio > 2.0f)
            {
                Debug.LogWarning($"Potentially unstable height to span ratio: {heightToSpanRatio}");
                return false;
            }
            
            if (thicknessToSpanRatio < 0.02f)
            {
                Debug.LogWarning($"Insufficient thickness for span: {thicknessToSpanRatio}");
                return false;
            }
            
            return true;
        }

        /// <summary>
        /// 建築構造の推定重量を計算
        /// </summary>
        public static float CalculateEstimatedWeight(ArchitecturalParams parameters)
        {
            // 石材の密度を仮定（kg/m³）
            float stoneDensity = 2500f;
            
            // 概算体積を計算
            float volume = parameters.span * parameters.height * parameters.thickness;
            
            // 建築タイプに応じた体積係数
            float volumeFactor = GetVolumeFactorForType(parameters.architecturalType);
            
            return volume * volumeFactor * stoneDensity;
        }

        /// <summary>
        /// 建築タイプに応じた体積係数を取得
        /// </summary>
        private static float GetVolumeFactorForType(ArchitecturalType type)
        {
            switch (type)
            {
                case ArchitecturalType.SimpleArch: return 0.3f;
                case ArchitecturalType.RomanArch: return 0.4f;
                case ArchitecturalType.GothicArch: return 0.25f;
                case ArchitecturalType.Bridge: return 0.5f;
                case ArchitecturalType.Aqueduct: return 0.6f;
                case ArchitecturalType.Cathedral: return 0.8f;
                case ArchitecturalType.Colonnade: return 0.2f;
                case ArchitecturalType.Viaduct: return 0.7f;
                default: return 0.3f;
            }
        }
        #endregion
    }
}