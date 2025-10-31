#if HAS_PROBUILDER
using UnityEngine;
using UnityEditor;
using UnityEngine.ProBuilder;
using UnityEngine.ProBuilder.MeshOperations;
using UnityEditor.ProBuilder;
using UnityEditor.ProBuilder.Actions;
using System.Collections.Generic;
using System.Linq;
using EditorUtility = UnityEditor.EditorUtility;

namespace Vastcore.Editor.Generation
{
    /// <summary>
    /// 高度な構造物生成機能を提供するタブクラス
    /// ProBuilderメッシュを使用した本格的な構造物生成
    /// Phase 2: 形状制御システム実装
    /// </summary>
    public class AdvancedStructureTab : IStructureTab
    {
        // --- データクラス定義 ---
        [System.Serializable]
        private class ShapeParams
        {
            public float twist = 0f;
            public float taper = 1f;
            public Vector2 partialTwistRange = new Vector2(0, 1);
        }

        [System.Serializable]
        private class ModificationParams
        {
            public float bendStrength = 0f;
            public Vector3 bendDirection = Vector3.up;
            public float noiseAmount = 0f;
            public float noiseFrequency = 1f;
        }
        
        // --- public Enums ---
        public enum AdvancedShapeType { Monolith, TwistedTower, ProceduralColumn }

        // --- IStructureTab 実装 ---
        public TabCategory Category => TabCategory.Generation;
        public string DisplayName => "Advanced";
        public string Description => "手続き的な形状や複雑な構造を生成します。";
        public bool SupportsRealTimeUpdate => false;

        // --- private メンバー ---
        private StructureGeneratorWindow _parent;
        private Vector2 _scrollPosition;

        private AdvancedShapeType _selectedShape = AdvancedShapeType.Monolith;

        private ShapeParams _shapeParams = new ShapeParams();
        private ModificationParams _modParams = new ModificationParams();
            
        // --- UI表示制御用 ---
        private bool _showShapeParams = true;
        private bool _showModParams = true;

        public AdvancedStructureTab(StructureGeneratorWindow parent)
        {
            _parent = parent;
        }
        
        public void DrawGUI()
        {
            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);

            EditorGUILayout.LabelField(DisplayName, EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(Description, MessageType.Info);
            
            EditorGUILayout.Space();
            
            _selectedShape = (AdvancedShapeType)EditorGUILayout.EnumPopup("生成タイプ", _selectedShape);
            
            DrawParametersUI();
            
            EditorGUILayout.Space(20);

            GUI.backgroundColor = new Color(0.6f, 0.8f, 1f); // 明るい青
            if (GUILayout.Button($"Generate {_selectedShape}", GUILayout.Height(30)))
        {
                Generate();
            }
            GUI.backgroundColor = Color.white;
            
            EditorGUILayout.EndScrollView();
        }

        private void DrawParametersUI()
        {
            _showShapeParams = EditorGUILayout.Foldout(_showShapeParams, "基本形状パラメータ", true);
            if (_showShapeParams)
            {
                EditorGUI.indentLevel++;
                _shapeParams.twist = EditorGUILayout.Slider("ツイスト", _shapeParams.twist, -360f, 360f);
                _shapeParams.taper = EditorGUILayout.Slider("テーパー (末広がり)", _shapeParams.taper, 0.1f, 5f);
                EditorGUILayout.MinMaxSlider("部分ツイスト範囲", ref _shapeParams.partialTwistRange.x, ref _shapeParams.partialTwistRange.y, 0f, 1f);
                EditorGUI.indentLevel--;
            }
            
            _showModParams = EditorGUILayout.Foldout(_showModParams, "形状変形パラメータ", true);
            if (_showModParams)
            {
                EditorGUI.indentLevel++;
                _modParams.bendStrength = EditorGUILayout.Slider("ベンド強度", _modParams.bendStrength, 0f, 5f);
                _modParams.bendDirection = EditorGUILayout.Vector3Field("ベンド方向", _modParams.bendDirection);
                _modParams.noiseAmount = EditorGUILayout.Slider("ノイズ量", _modParams.noiseAmount, 0f, 1f);
                _modParams.noiseFrequency = EditorGUILayout.Slider("ノイズ周波数", _modParams.noiseFrequency, 0.1f, 20f);
                EditorGUI.indentLevel--;
            }
        }
        
        private void Generate()
            {
            GameObject generatedObject = null;
            switch (_selectedShape)
        {
                case AdvancedShapeType.Monolith:
                    generatedObject = CreateMonolith();
                    break;
                // 他の形状も後で追加
            }

            if (generatedObject != null)
            {
                generatedObject.transform.position = _parent.GlobalSettings.DefaultSpawnPosition;
                _parent.ApplyMaterial(generatedObject.GetComponent<ProBuilderMesh>());
                Selection.activeGameObject = generatedObject;
            }
        }
        
        private GameObject CreateMonolith()
        {
            // 1. 基本となる立方体を生成
            var pbMesh = ShapeGenerator.CreateShape(ShapeType.Cube);
            pbMesh.name = "Monolith";
            
            // 2. 基本スケールを適用
            float baseScale = _parent.GlobalSettings.GlobalStructureScale;
            pbMesh.transform.localScale = new Vector3(baseScale / 2f, baseScale * 2f, baseScale / 2f);

            // 3. 頂点変形を適用
            ApplyVertexModifications(pbMesh);
                
                pbMesh.ToMesh();
                pbMesh.Refresh();
            return pbMesh.gameObject;
        }
        
        private void ApplyVertexModifications(ProBuilderMesh pbMesh)
            {
            var vertices = pbMesh.positions.ToArray();
            Bounds bounds = CalculateBounds(vertices); // 手動で境界を計算

            for (int i = 0; i < vertices.Length; i++)
            {
                Vector3 p = vertices[i];
                float normalizedY = Mathf.InverseLerp(bounds.min.y, bounds.max.y, p.y);

                // --- テーパー適用 ---
                float taperScale = Mathf.Lerp(1f, _shapeParams.taper, normalizedY);
                p.x *= taperScale;
                p.z *= taperScale;
                
                // --- ツイスト適用 ---
                float twistRange = _shapeParams.partialTwistRange.y - _shapeParams.partialTwistRange.x;
                if (twistRange > 0.01f && normalizedY >= _shapeParams.partialTwistRange.x)
            {
                    float twistNormalizedY = Mathf.InverseLerp(_shapeParams.partialTwistRange.x, _shapeParams.partialTwistRange.y, normalizedY);
                    float twistAmount = _shapeParams.twist * twistNormalizedY;
                    Quaternion twistRotation = Quaternion.Euler(0, twistAmount, 0);
                    p = twistRotation * p;
                }
                
                // --- ベンド適用 ---
                if (_modParams.bendStrength > 0.01f)
                {
                    Vector3 bendOffset = _modParams.bendDirection.normalized * _modParams.bendStrength * normalizedY * normalizedY;
                    p += bendOffset;
            }
            
                // --- ノイズ適用 ---
                if (_modParams.noiseAmount > 0.01f)
                {
                    float noise = (Mathf.PerlinNoise(p.x * _modParams.noiseFrequency, p.z * _modParams.noiseFrequency) - 0.5f) * 2f;
                    p += new Vector3(noise, noise, noise) * _modParams.noiseAmount;
                }

                vertices[i] = p;
                }
            pbMesh.positions = vertices;
            }

        private Bounds CalculateBounds(IList<Vector3> points)
        {
            if (points == null || points.Count == 0) return new Bounds();
            var min = points[0];
            var max = points[0];
            for (int i = 1; i < points.Count; i++)
            {
                min = Vector3.Min(min, points[i]);
                max = Vector3.Max(max, points[i]);
            }
            return new Bounds((min + max) / 2f, max - min);
        }

        public void OnSceneGUI() { }
        public void ProcessSelectedObjects() { }
        public void OnTabSelected() { }
        public void OnTabDeselected() { }

        public void HandleRealTimeUpdate() { }
    }

    // Phase 2: 形状制御システム用の構造体とEnum定義

    /// <summary>
    /// 基本形状パラメータ
    /// </summary>
    [System.Serializable]
    public struct ShapeParameters
    {
        [Header("基本形状")]
        public float twist;              // ねじれ (0-360度)
        public float length;             // 長さ倍率
        public float thickness;          // 太さ倍率
        public float smoothness;         // 滑らかさ (0-1)
        
        [Header("押し出し制御")]
        public float extrudeRandomness;  // 押し出しランダム分布
        public int extrudeIterations;    // 押し出し回数
    }

    /// <summary>
    /// 形状変形パラメータ
    /// </summary>
    [System.Serializable]
    public struct ShapeModification
    {
        [Header("変形操作")]
        public float topTaper;           // 上部絞り
        public float bottomTaper;        // 下部絞り
        public float waistConstriction;  // くびれ
        public Vector3 bend;             // ベンド方向・強度
        
        [Header("破壊的操作")]
        public float cutPercentage;      // カット割合
        public float fracture;           // 破砕強度
        public float liquefaction;       // 液状化効果
        public bool partialTwist;        // 部分ねじれ
        public Vector2 twistRange;       // ねじれ範囲 (0-1)
    }

    /// <summary>
    /// Boolean演算制御パラメータ
    /// </summary>
    [System.Serializable]
    public struct BooleanParameters
    {
        [Header("内部加工制御")]
        public float edgeInsetDistance;  // 縁からの内側距離
        public float edgeOutsetDistance; // 縁からの外側距離
        public BooleanOperation operation; // Union/Subtract/Intersect
        public float operationStrength;  // 演算強度
        
        [Header("完成形制御")]
        public float completionRatio;    // 完成度 (0-1)
        
        [Header("追加制御機能")]
        public FaceSelectionMode faceMode; // 面選択モード
        public float volumeThreshold;    // 体積閾値
        public bool preserveOriginalUV;  // 元UV保持
        public float blendSharpness;     // ブレンドの鋭さ
        public Vector3 operationCenter;  // 演算中心点
        public float falloffRadius;      // 減衰半径
    }

    /// <summary>
    /// 面選択モード
    /// </summary>
    public enum FaceSelectionMode
    {
        All,                 // 全面
        TopFaces,            // 上面のみ
        SideFaces,           // 側面のみ
        BottomFaces,         // 下面のみ
        NormalDirection,     // 法線方向指定
        CurvatureBased,      // 曲率ベース
        AreaBased,           // 面積ベース
        RandomSelection,     // ランダム選択
        ConditionalSelection // 条件付き選択
    }

    /// <summary>
    /// 高度加工システムパラメータ
    /// </summary>
    [System.Serializable]
    public struct AdvancedProcessing
    {
        [Header("表面加工")]
        public float surfaceRoughness;   // 表面粗さ
        public float detailScale;        // 詳細スケール
        public int noiseOctaves;         // ノイズオクターブ
        public float noiseFrequency;     // ノイズ周波数
        
        [Header("エッジ処理")]
        public float edgeBevel;          // エッジベベル
        public float edgeSharpness;      // エッジ鋭さ
        public bool autoSmooth;          // 自動スムージング
        public float smoothingAngle;     // スムージング角度
        
        [Header("構造的加工")]
        public float hollowThickness;    // 中空化厚さ
        public bool createShell;         // シェル作成
        public float ribThickness;       // リブ厚さ
        public int ribCount;             // リブ数
        
        [Header("風化・劣化")]
        public float weatheringIntensity; // 風化強度
        public WeatheringType weatherType; // 風化タイプ
        public float erosionPattern;     // 侵食パターン
        public float crackDensity;       // ひび割れ密度
    }

    /// <summary>
    /// 風化タイプ
    /// </summary>
    public enum WeatheringType
    {
        None,
        WindErosion,        // 風化
        WaterErosion,       // 水による侵食
        ChemicalWeathering, // 化学的風化
        FrostWeathering,    // 凍結風化
        BiologicalErosion,  // 生物による侵食
        CombinedWeathering  // 複合風化
    }

    /// <summary>
    /// Boolean演算タイプ
    /// </summary>
    public enum BooleanOperation
    {
        Union,
        Subtract,
        Intersect
    }
}
#endif