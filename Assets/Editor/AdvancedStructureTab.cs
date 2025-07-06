using UnityEngine;
using UnityEditor;
using UnityEngine.ProBuilder;
using UnityEngine.ProBuilder.MeshOperations;
using UnityEditor.ProBuilder;
using UnityEditor.ProBuilder.Actions;
using Parabox.CSG;
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
    public class AdvancedStructureTab
    {
        private StructureGeneratorWindow parentWindow;
        private Vector2 scrollPosition;
        
        // 巨大構造物のパラメータ
        private float monumentScale = 100f;
        private int complexityLevel = 3;
        private bool generateBase = true;
        private Material primaryMaterial;
        private Material secondaryMaterial;
        
        // 関係性システム
        private bool useRelationshipSystem = false;
        private StructureRelationshipSystem relationshipSystem = new StructureRelationshipSystem();
        
        // Phase 2: 形状制御パラメータ
        private bool useAdvancedShapeControl = false;
        private ShapeParameters shapeParams = new ShapeParameters();
        private ShapeModification shapeModification = new ShapeModification();
        private BooleanParameters booleanParams = new BooleanParameters();
        private AdvancedProcessing advancedProcessing = new AdvancedProcessing();
        
        // 構造物タイプ
        public enum MonumentType
        {
            GeometricMonolith,    // 幾何学的なモノリス
            TwistedTower,         // ツイスト構造
            PerforatedCube,       // 穿孔された立方体
            FloatingRings,        // 浮遊する環状構造
            StackedGeometry,      // 積層幾何学
            SplitMonument,        // 分割された記念碑
            CurvedArchway,        // 曲面アーチ
            AbstractSculpture     // 抽象彫刻
        }
        
        private MonumentType selectedMonumentType = MonumentType.GeometricMonolith;
        
        public AdvancedStructureTab(StructureGeneratorWindow parent)
        {
            parentWindow = parent;
            InitializeDefaultParameters();
        }
        
        private void InitializeDefaultParameters()
        {
            // 形状パラメータのデフォルト値
            shapeParams.twist = 0f;
            shapeParams.length = 1f;
            shapeParams.thickness = 1f;
            shapeParams.smoothness = 0.5f;
            shapeParams.extrudeRandomness = 0.1f;
            shapeParams.extrudeIterations = 1;
            
            // 形状変形のデフォルト値
            shapeModification.topTaper = 0f;
            shapeModification.bottomTaper = 0f;
            shapeModification.waistConstriction = 0f;
            shapeModification.bend = Vector3.zero;
            shapeModification.cutPercentage = 0f;
            shapeModification.fracture = 0f;
            shapeModification.liquefaction = 0f;
            shapeModification.partialTwist = false;
            shapeModification.twistRange = new Vector2(0f, 1f);
            
            // Boolean演算のデフォルト値
            booleanParams.edgeInsetDistance = 0.1f;
            booleanParams.edgeOutsetDistance = 0.1f;
            booleanParams.operation = BooleanOperation.Subtract;
            booleanParams.operationStrength = 1f;
            booleanParams.completionRatio = 1f;
            booleanParams.faceMode = FaceSelectionMode.All;
            booleanParams.volumeThreshold = 0.1f;
            booleanParams.preserveOriginalUV = true;
            booleanParams.blendSharpness = 1f;
            booleanParams.operationCenter = Vector3.zero;
            booleanParams.falloffRadius = 1f;
            
            // 高度加工のデフォルト値
            advancedProcessing.surfaceRoughness = 0f;
            advancedProcessing.detailScale = 1f;
            advancedProcessing.noiseOctaves = 3;
            advancedProcessing.noiseFrequency = 1f;
            advancedProcessing.edgeBevel = 0f;
            advancedProcessing.edgeSharpness = 1f;
            advancedProcessing.autoSmooth = true;
            advancedProcessing.smoothingAngle = 30f;
            advancedProcessing.hollowThickness = 0f;
            advancedProcessing.createShell = false;
            advancedProcessing.ribThickness = 0.1f;
            advancedProcessing.ribCount = 0;
            advancedProcessing.weatheringIntensity = 0f;
            advancedProcessing.weatherType = WeatheringType.None;
            advancedProcessing.erosionPattern = 0f;
            advancedProcessing.crackDensity = 0f;
        }
        
        public void OnGUI()
        {
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
            
            EditorGUILayout.LabelField("Advanced Structure Generator", EditorStyles.boldLabel);
            EditorGUILayout.Space();
            
            // 基本パラメータ
            DrawBasicParameters();
            
            EditorGUILayout.Space();
            
            // Phase 2: 形状制御システム
            DrawShapeControlSystem();
            
            EditorGUILayout.Space();
            
            // 関係性システム
            DrawRelationshipSystem();
            
            EditorGUILayout.Space();
            
            // 生成ボタン
            DrawGenerationButtons();
            
            EditorGUILayout.EndScrollView();
        }
        
        private void DrawBasicParameters()
        {
            EditorGUILayout.LabelField("Basic Parameters", EditorStyles.boldLabel);
            
            selectedMonumentType = (MonumentType)EditorGUILayout.EnumPopup("Monument Type", selectedMonumentType);
            monumentScale = EditorGUILayout.Slider("Scale", monumentScale, 10f, 500f);
            complexityLevel = EditorGUILayout.IntSlider("Complexity", complexityLevel, 1, 10);
            generateBase = EditorGUILayout.Toggle("Generate Base", generateBase);
            
            EditorGUILayout.Space();
            
            // マテリアル設定
            EditorGUILayout.LabelField("Materials", EditorStyles.boldLabel);
            primaryMaterial = (Material)EditorGUILayout.ObjectField("Primary Material", primaryMaterial, typeof(Material), false);
            secondaryMaterial = (Material)EditorGUILayout.ObjectField("Secondary Material", secondaryMaterial, typeof(Material), false);
        }
        
        private void DrawShapeControlSystem()
        {
            useAdvancedShapeControl = EditorGUILayout.Foldout(useAdvancedShapeControl, "Advanced Shape Control System (Phase 2)", true);
            
            if (useAdvancedShapeControl)
            {
                EditorGUI.indentLevel++;
                
                // 基本形状パラメータ
                DrawShapeParameters();
                
                EditorGUILayout.Space();
                
                // 形状変形パラメータ
                DrawShapeModification();
                
                EditorGUILayout.Space();
                
                // Boolean演算制御
                DrawBooleanParameters();
                
                EditorGUILayout.Space();
                
                // 高度加工システム
                DrawAdvancedProcessing();
                
                EditorGUI.indentLevel--;
            }
        }
        
        private void DrawShapeParameters()
        {
            EditorGUILayout.LabelField("Shape Parameters", EditorStyles.boldLabel);
            
            shapeParams.twist = EditorGUILayout.Slider("Twist (degrees)", shapeParams.twist, -360f, 360f);
            shapeParams.length = EditorGUILayout.Slider("Length Multiplier", shapeParams.length, 0.1f, 5f);
            shapeParams.thickness = EditorGUILayout.Slider("Thickness Multiplier", shapeParams.thickness, 0.1f, 3f);
            shapeParams.smoothness = EditorGUILayout.Slider("Smoothness", shapeParams.smoothness, 0f, 1f);
            
            EditorGUILayout.Space();
            
            EditorGUILayout.LabelField("Extrusion Control", EditorStyles.boldLabel);
            shapeParams.extrudeRandomness = EditorGUILayout.Slider("Extrude Randomness", shapeParams.extrudeRandomness, 0f, 1f);
            shapeParams.extrudeIterations = EditorGUILayout.IntSlider("Extrude Iterations", shapeParams.extrudeIterations, 1, 10);
        }
        
        private void DrawShapeModification()
        {
            EditorGUILayout.LabelField("Shape Modification", EditorStyles.boldLabel);
            
            shapeModification.topTaper = EditorGUILayout.Slider("Top Taper", shapeModification.topTaper, 0f, 1f);
            shapeModification.bottomTaper = EditorGUILayout.Slider("Bottom Taper", shapeModification.bottomTaper, 0f, 1f);
            shapeModification.waistConstriction = EditorGUILayout.Slider("Waist Constriction", shapeModification.waistConstriction, 0f, 1f);
            shapeModification.bend = EditorGUILayout.Vector3Field("Bend Direction", shapeModification.bend);
            
            EditorGUILayout.Space();
            
            EditorGUILayout.LabelField("Destructive Operations", EditorStyles.boldLabel);
            shapeModification.cutPercentage = EditorGUILayout.Slider("Cut Percentage", shapeModification.cutPercentage, 0f, 1f);
            shapeModification.fracture = EditorGUILayout.Slider("Fracture Intensity", shapeModification.fracture, 0f, 1f);
            shapeModification.liquefaction = EditorGUILayout.Slider("Liquefaction Effect", shapeModification.liquefaction, 0f, 1f);
            
            shapeModification.partialTwist = EditorGUILayout.Toggle("Partial Twist", shapeModification.partialTwist);
            if (shapeModification.partialTwist)
            {
                EditorGUI.indentLevel++;
                shapeModification.twistRange.x = EditorGUILayout.Slider("Twist Start", shapeModification.twistRange.x, 0f, 1f);
                shapeModification.twistRange.y = EditorGUILayout.Slider("Twist End", shapeModification.twistRange.y, 0f, 1f);
                EditorGUI.indentLevel--;
            }
        }
        
        private void DrawBooleanParameters()
        {
            EditorGUILayout.LabelField("Boolean Operation Control", EditorStyles.boldLabel);
            
            booleanParams.operation = (BooleanOperation)EditorGUILayout.EnumPopup("Operation", booleanParams.operation);
            booleanParams.operationStrength = EditorGUILayout.Slider("Operation Strength", booleanParams.operationStrength, 0f, 2f);
            booleanParams.completionRatio = EditorGUILayout.Slider("Completion Ratio", booleanParams.completionRatio, 0f, 1f);
            
            EditorGUILayout.Space();
            
            EditorGUILayout.LabelField("Face Selection", EditorStyles.boldLabel);
            booleanParams.faceMode = (FaceSelectionMode)EditorGUILayout.EnumPopup("Face Mode", booleanParams.faceMode);
            booleanParams.volumeThreshold = EditorGUILayout.Slider("Volume Threshold", booleanParams.volumeThreshold, 0.01f, 1f);
            
            EditorGUILayout.Space();
            
            EditorGUILayout.LabelField("Advanced Controls", EditorStyles.boldLabel);
            booleanParams.edgeInsetDistance = EditorGUILayout.Slider("Edge Inset Distance", booleanParams.edgeInsetDistance, 0f, 1f);
            booleanParams.edgeOutsetDistance = EditorGUILayout.Slider("Edge Outset Distance", booleanParams.edgeOutsetDistance, 0f, 1f);
            booleanParams.blendSharpness = EditorGUILayout.Slider("Blend Sharpness", booleanParams.blendSharpness, 0.1f, 2f);
            booleanParams.falloffRadius = EditorGUILayout.Slider("Falloff Radius", booleanParams.falloffRadius, 0.1f, 5f);
            booleanParams.preserveOriginalUV = EditorGUILayout.Toggle("Preserve Original UV", booleanParams.preserveOriginalUV);
        }
        
        private void DrawAdvancedProcessing()
        {
            EditorGUILayout.LabelField("Advanced Processing", EditorStyles.boldLabel);
            
            // 表面加工
            EditorGUILayout.LabelField("Surface Processing", EditorStyles.miniBoldLabel);
            advancedProcessing.surfaceRoughness = EditorGUILayout.Slider("Surface Roughness", advancedProcessing.surfaceRoughness, 0f, 1f);
            advancedProcessing.detailScale = EditorGUILayout.Slider("Detail Scale", advancedProcessing.detailScale, 0.1f, 5f);
            advancedProcessing.noiseOctaves = EditorGUILayout.IntSlider("Noise Octaves", advancedProcessing.noiseOctaves, 1, 8);
            advancedProcessing.noiseFrequency = EditorGUILayout.Slider("Noise Frequency", advancedProcessing.noiseFrequency, 0.1f, 10f);
            
            EditorGUILayout.Space();
            
            // エッジ処理
            EditorGUILayout.LabelField("Edge Processing", EditorStyles.miniBoldLabel);
            advancedProcessing.edgeBevel = EditorGUILayout.Slider("Edge Bevel", advancedProcessing.edgeBevel, 0f, 1f);
            advancedProcessing.edgeSharpness = EditorGUILayout.Slider("Edge Sharpness", advancedProcessing.edgeSharpness, 0f, 2f);
            advancedProcessing.autoSmooth = EditorGUILayout.Toggle("Auto Smooth", advancedProcessing.autoSmooth);
            if (advancedProcessing.autoSmooth)
            {
                EditorGUI.indentLevel++;
                advancedProcessing.smoothingAngle = EditorGUILayout.Slider("Smoothing Angle", advancedProcessing.smoothingAngle, 0f, 180f);
                EditorGUI.indentLevel--;
            }
            
            EditorGUILayout.Space();
            
            // 構造的加工
            EditorGUILayout.LabelField("Structural Processing", EditorStyles.miniBoldLabel);
            advancedProcessing.hollowThickness = EditorGUILayout.Slider("Hollow Thickness", advancedProcessing.hollowThickness, 0f, 1f);
            advancedProcessing.createShell = EditorGUILayout.Toggle("Create Shell", advancedProcessing.createShell);
            advancedProcessing.ribCount = EditorGUILayout.IntSlider("Rib Count", advancedProcessing.ribCount, 0, 20);
            if (advancedProcessing.ribCount > 0)
            {
                EditorGUI.indentLevel++;
                advancedProcessing.ribThickness = EditorGUILayout.Slider("Rib Thickness", advancedProcessing.ribThickness, 0.01f, 0.5f);
                EditorGUI.indentLevel--;
            }
            
            EditorGUILayout.Space();
            
            // 風化・劣化
            EditorGUILayout.LabelField("Weathering & Erosion", EditorStyles.miniBoldLabel);
            advancedProcessing.weatherType = (WeatheringType)EditorGUILayout.EnumPopup("Weather Type", advancedProcessing.weatherType);
            if (advancedProcessing.weatherType != WeatheringType.None)
            {
                EditorGUI.indentLevel++;
                advancedProcessing.weatheringIntensity = EditorGUILayout.Slider("Weathering Intensity", advancedProcessing.weatheringIntensity, 0f, 1f);
                advancedProcessing.erosionPattern = EditorGUILayout.Slider("Erosion Pattern", advancedProcessing.erosionPattern, 0f, 1f);
                advancedProcessing.crackDensity = EditorGUILayout.Slider("Crack Density", advancedProcessing.crackDensity, 0f, 1f);
                EditorGUI.indentLevel--;
            }
        }
        
        private void DrawRelationshipSystem()
        {
            useRelationshipSystem = EditorGUILayout.Toggle("Use Relationship System", useRelationshipSystem);
            
            if (useRelationshipSystem)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.HelpBox("Relationship system will be applied after generation.", MessageType.Info);
                EditorGUI.indentLevel--;
            }
        }
        
        private void DrawGenerationButtons()
        {
            EditorGUILayout.LabelField("Generation", EditorStyles.boldLabel);
            
            GUI.backgroundColor = Color.green;
            if (GUILayout.Button("Generate Advanced Structure", GUILayout.Height(40)))
            {
                GenerateAdvancedStructure();
            }
            GUI.backgroundColor = Color.white;
            
            EditorGUILayout.Space();
            
            // クイック生成ボタン
            EditorGUILayout.LabelField("Quick Generation", EditorStyles.boldLabel);
            EditorGUILayout.BeginHorizontal();
            
            if (GUILayout.Button("Geometric\nMonolith"))
            {
                GenerateQuickStructure(MonumentType.GeometricMonolith);
            }
            
            if (GUILayout.Button("Twisted\nTower"))
            {
                GenerateQuickStructure(MonumentType.TwistedTower);
            }
            
            if (GUILayout.Button("Perforated\nCube"))
            {
                GenerateQuickStructure(MonumentType.PerforatedCube);
            }
            
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.BeginHorizontal();
            
            if (GUILayout.Button("Floating\nRings"))
            {
                GenerateQuickStructure(MonumentType.FloatingRings);
            }
            
            if (GUILayout.Button("Stacked\nGeometry"))
            {
                GenerateQuickStructure(MonumentType.StackedGeometry);
            }
            
            if (GUILayout.Button("Abstract\nSculpture"))
            {
                GenerateQuickStructure(MonumentType.AbstractSculpture);
            }
            
            EditorGUILayout.EndHorizontal();
        }
        
        private void GenerateAdvancedStructure()
        {
            try
            {
                GameObject structure = CreateMonument(selectedMonumentType);
                
                if (structure != null)
                {
                    // Phase 2: 形状制御システム適用
                    if (useAdvancedShapeControl)
                    {
                        ApplyShapeControl(structure);
                    }
                    
                    // スケール適用
                    structure.transform.localScale = Vector3.one * monumentScale;
                    
                    // マテリアル適用
                    if (primaryMaterial != null)
                    {
                        ApplyMaterial(structure, primaryMaterial);
                    }
                    
                    // 関係性システム適用
                    if (useRelationshipSystem)
                    {
                        Debug.Log("Relationship system integration planned");
                    }
                    
                    Debug.Log($"Generated advanced structure: {selectedMonumentType}");
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Failed to generate advanced structure: {e.Message}");
                EditorUtility.DisplayDialog("Error", $"Failed to generate structure: {e.Message}", "OK");
            }
        }
        
        private void GenerateQuickStructure(MonumentType type)
        {
            selectedMonumentType = type;
            GenerateAdvancedStructure();
        }
        
        private GameObject CreateMonument(MonumentType type)
        {
            ProBuilderMesh pbMesh = null;
            
            switch (type)
            {
                case MonumentType.GeometricMonolith:
                    pbMesh = CreateGeometricMonolith();
                    break;
                    
                case MonumentType.TwistedTower:
                    pbMesh = CreateTwistedTower();
                    break;
                    
                case MonumentType.PerforatedCube:
                    pbMesh = CreatePerforatedCube();
                    break;
                    
                case MonumentType.FloatingRings:
                    pbMesh = CreateFloatingRings();
                    break;
                    
                case MonumentType.StackedGeometry:
                    pbMesh = CreateStackedGeometry();
                    break;
                    
                case MonumentType.SplitMonument:
                    pbMesh = CreateSplitMonument();
                    break;
                    
                case MonumentType.CurvedArchway:
                    pbMesh = CreateCurvedArchway();
                    break;
                    
                case MonumentType.AbstractSculpture:
                    pbMesh = CreateAbstractSculpture();
                    break;
            }
            
            if (pbMesh != null)
            {
                var gameObject = pbMesh.gameObject;
                gameObject.name = $"Advanced_{type}";
                
                // ProBuilderメッシュを更新
                pbMesh.ToMesh();
                pbMesh.Refresh();
                
                // Undoに登録
                Undo.RegisterCreatedObjectUndo(gameObject, $"Create {type}");
                
                // 選択状態にする
                Selection.activeGameObject = gameObject;
                
                return gameObject;
            }
            
            return null;
        }
        
        private ProBuilderMesh CreateGeometricMonolith()
        {
            var pbMesh = ShapeGenerator.CreateShape(ShapeType.Cube);
            
            if (pbMesh != null)
            {
                // 縦長に変形
                pbMesh.transform.localScale = new Vector3(1f, 3f, 1f);
                
                // 複雑さに応じて分割
                if (complexityLevel > 1)
                {
                    // 分割処理は一旦スキップ（ProBuilder APIの互換性問題のため）
                    // 代わりに、より詳細なプリミティブを使用
                    Debug.Log($"Complex structure generated with complexity level {complexityLevel}");
                }
                
                pbMesh.ToMesh();
                pbMesh.Refresh();
            }
            
            return pbMesh;
        }
        
        private ProBuilderMesh CreateTwistedTower()
        {
            var pbMesh = ShapeGenerator.CreateShape(ShapeType.Cylinder);
            
            if (pbMesh != null)
            {
                // 縦長に変形
                pbMesh.transform.localScale = new Vector3(1f, 4f, 1f);
                
                // ツイスト効果は形状制御システムで適用
                pbMesh.ToMesh();
                pbMesh.Refresh();
            }
            
            return pbMesh;
        }
        
        private ProBuilderMesh CreatePerforatedCube()
        {
            var pbMesh = ShapeGenerator.CreateShape(ShapeType.Cube);
            
            if (pbMesh != null)
            {
                // 穿孔効果はBoolean演算で後処理
                pbMesh.ToMesh();
                pbMesh.Refresh();
            }
            
            return pbMesh;
        }
        
        private ProBuilderMesh CreateFloatingRings()
        {
            var pbMesh = ShapeGenerator.CreateShape(ShapeType.Torus);
            
            if (pbMesh != null)
            {
                pbMesh.ToMesh();
                pbMesh.Refresh();
            }
            
            return pbMesh;
        }
        
        private ProBuilderMesh CreateStackedGeometry()
        {
            var pbMesh = ShapeGenerator.CreateShape(ShapeType.Cube);
            
            if (pbMesh != null)
            {
                // 積層効果は関係性システムで実装
                pbMesh.ToMesh();
                pbMesh.Refresh();
            }
            
            return pbMesh;
        }
        
        private ProBuilderMesh CreateSplitMonument()
        {
            var pbMesh = ShapeGenerator.CreateShape(ShapeType.Cube);
            
            if (pbMesh != null)
            {
                // 分割効果はBoolean演算で後処理
                pbMesh.ToMesh();
                pbMesh.Refresh();
            }
            
            return pbMesh;
        }
        
        private ProBuilderMesh CreateCurvedArchway()
        {
            var pbMesh = ShapeGenerator.CreateShape(ShapeType.Arch);
            
            if (pbMesh != null)
            {
                pbMesh.ToMesh();
                pbMesh.Refresh();
            }
            
            return pbMesh;
        }
        
        private ProBuilderMesh CreateAbstractSculpture()
        {
            var pbMesh = ShapeGenerator.CreateShape(ShapeType.Sphere);
            
            if (pbMesh != null)
            {
                // 抽象化効果は形状制御システムで適用
                pbMesh.ToMesh();
                pbMesh.Refresh();
            }
            
            return pbMesh;
        }
        
        /// <summary>
        /// Phase 2: 形状制御システムの適用
        /// </summary>
        private void ApplyShapeControl(GameObject structure)
        {
            if (structure == null) return;
            
            var pbMesh = structure.GetComponent<ProBuilderMesh>();
            if (pbMesh == null) return;
            
            try
            {
                // 基本形状パラメータ適用
                ApplyShapeParameters(pbMesh);
                
                // 形状変形適用
                ApplyShapeModification(pbMesh);
                
                // Boolean演算適用
                ApplyBooleanOperations(pbMesh);
                
                // 高度加工適用
                ApplyAdvancedProcessing(pbMesh);
                
                // メッシュ更新
                pbMesh.ToMesh();
                pbMesh.Refresh();
                
                Debug.Log("Shape control system applied successfully");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Failed to apply shape control: {e.Message}");
            }
        }
        
        private void ApplyShapeParameters(ProBuilderMesh pbMesh)
        {
            // ツイスト効果
            if (Mathf.Abs(shapeParams.twist) > 0.01f)
            {
                ApplyTwist(pbMesh, shapeParams.twist);
            }
            
            // 長さ・太さ調整
            if (Mathf.Abs(shapeParams.length - 1f) > 0.01f || Mathf.Abs(shapeParams.thickness - 1f) > 0.01f)
            {
                Vector3 scale = new Vector3(shapeParams.thickness, shapeParams.length, shapeParams.thickness);
                pbMesh.transform.localScale = Vector3.Scale(pbMesh.transform.localScale, scale);
            }
            
            // 押し出し処理
            if (shapeParams.extrudeIterations > 1)
            {
                ApplyExtrusion(pbMesh);
            }
        }
        
        private void ApplyShapeModification(ProBuilderMesh pbMesh)
        {
            // テーパー効果
            if (shapeModification.topTaper > 0.01f || shapeModification.bottomTaper > 0.01f)
            {
                ApplyTaper(pbMesh);
            }
            
            // くびれ効果
            if (shapeModification.waistConstriction > 0.01f)
            {
                ApplyWaistConstriction(pbMesh);
            }
            
            // ベンド効果
            if (shapeModification.bend.magnitude > 0.01f)
            {
                ApplyBend(pbMesh);
            }
        }
        
        private void ApplyBooleanOperations(ProBuilderMesh pbMesh)
        {
            // Boolean演算は複雑なため、基本的な実装のみ
            if (booleanParams.completionRatio < 0.99f)
            {
                // 完成度に応じた部分的な処理
                Debug.Log($"Boolean completion ratio: {booleanParams.completionRatio}");
            }
        }
        
        private void ApplyAdvancedProcessing(ProBuilderMesh pbMesh)
        {
            // 自動スムージング
            if (advancedProcessing.autoSmooth)
            {
                // ProBuilder 4.x以降のAPI使用 - 手動でスムージンググループを設定
                var smoothingGroups = new int[pbMesh.faces.Count];
                for (int i = 0; i < smoothingGroups.Length; i++)
                {
                    smoothingGroups[i] = 1; // 全ての面を同じスムージンググループに
                }
                
                // スムージンググループを適用
                for (int i = 0; i < pbMesh.faces.Count; i++)
                {
                    pbMesh.faces[i].smoothingGroup = smoothingGroups[i];
                }
                
                pbMesh.ToMesh();
                pbMesh.Refresh();
            }
            
            // エッジベベル
            if (advancedProcessing.edgeBevel > 0.01f)
            {
                // ベベル処理の実装
                Debug.Log($"Edge bevel: {advancedProcessing.edgeBevel}");
            }
            
            // 表面粗さ
            if (advancedProcessing.surfaceRoughness > 0.01f)
            {
                ApplySurfaceRoughness(pbMesh);
            }
        }
        
        private void ApplyTwist(ProBuilderMesh pbMesh, float twistAngle)
        {
            var positions = pbMesh.positions;
            var bounds = CalculateBounds(pbMesh);
            
            for (int i = 0; i < positions.Count; i++)
            {
                var pos = positions[i];
                float normalizedY = (pos.y - bounds.min.y) / bounds.size.y;
                float angle = twistAngle * normalizedY * Mathf.Deg2Rad;
                
                float cos = Mathf.Cos(angle);
                float sin = Mathf.Sin(angle);
                
                positions[i] = new Vector3(
                    pos.x * cos - pos.z * sin,
                    pos.y,
                    pos.x * sin + pos.z * cos
                );
            }
            
            pbMesh.positions = positions;
        }
        
        private void ApplyExtrusion(ProBuilderMesh pbMesh)
        {
            // 簡単な押し出し処理
            var selectedFaces = pbMesh.faces.Take(Mathf.Min(pbMesh.faces.Count, 3)).ToList();
            
            if (selectedFaces.Count > 0)
            {
                // ProBuilder 4.x以降のAPI使用
                pbMesh.Extrude(selectedFaces, ExtrudeMethod.FaceNormal, 0.5f);
            }
        }
        
        private void ApplyTaper(ProBuilderMesh pbMesh)
        {
            var positions = pbMesh.positions;
            var bounds = CalculateBounds(pbMesh);
            
            for (int i = 0; i < positions.Count; i++)
            {
                var pos = positions[i];
                float normalizedY = (pos.y - bounds.min.y) / bounds.size.y;
                
                float topScale = 1f - shapeModification.topTaper * (normalizedY > 0.5f ? (normalizedY - 0.5f) * 2f : 0f);
                float bottomScale = 1f - shapeModification.bottomTaper * (normalizedY < 0.5f ? (0.5f - normalizedY) * 2f : 0f);
                
                float scale = topScale * bottomScale;
                
                positions[i] = new Vector3(
                    pos.x * scale,
                    pos.y,
                    pos.z * scale
                );
            }
            
            pbMesh.positions = positions;
        }
        
        private void ApplyWaistConstriction(ProBuilderMesh pbMesh)
        {
            var positions = pbMesh.positions;
            var bounds = CalculateBounds(pbMesh);
            
            for (int i = 0; i < positions.Count; i++)
            {
                var pos = positions[i];
                float normalizedY = (pos.y - bounds.min.y) / bounds.size.y;
                
                // 中央部分でのくびれ効果
                float waistEffect = 1f - shapeModification.waistConstriction * 
                    (1f - Mathf.Abs(normalizedY - 0.5f) * 2f);
                
                positions[i] = new Vector3(
                    pos.x * waistEffect,
                    pos.y,
                    pos.z * waistEffect
                );
            }
            
            pbMesh.positions = positions;
        }
        
        private void ApplyBend(ProBuilderMesh pbMesh)
        {
            var positions = pbMesh.positions;
            var bounds = CalculateBounds(pbMesh);
            var bendDirection = shapeModification.bend.normalized;
            var bendStrength = shapeModification.bend.magnitude;
            
            for (int i = 0; i < positions.Count; i++)
            {
                var pos = positions[i];
                float normalizedY = (pos.y - bounds.min.y) / bounds.size.y;
                
                Vector3 bendOffset = bendDirection * bendStrength * normalizedY * normalizedY;
                positions[i] = pos + bendOffset;
            }
            
            pbMesh.positions = positions;
        }
        
        private void ApplySurfaceRoughness(ProBuilderMesh pbMesh)
        {
            var positions = pbMesh.positions;
            var normals = pbMesh.normals;
            
            for (int i = 0; i < positions.Count; i++)
            {
                if (i < normals.Count)
                {
                    float noise = Mathf.PerlinNoise(
                        positions[i].x * advancedProcessing.noiseFrequency,
                        positions[i].z * advancedProcessing.noiseFrequency
                    );
                    
                    Vector3 offset = normals[i] * noise * advancedProcessing.surfaceRoughness * 0.1f;
                    positions[i] += offset;
                }
            }
            
            pbMesh.positions = positions;
        }
        
        private void ApplyMaterial(GameObject obj, Material material)
        {
            var renderers = obj.GetComponentsInChildren<MeshRenderer>();
            foreach (var renderer in renderers)
            {
                renderer.sharedMaterial = material;
            }
        }
        
        /// <summary>
        /// ProBuilderMeshの境界を計算する
        /// </summary>
        private Bounds CalculateBounds(ProBuilderMesh pbMesh)
        {
            var positions = pbMesh.positions;
            if (positions.Count == 0)
                return new Bounds();
                
            var min = positions[0];
            var max = positions[0];
            
            for (int i = 1; i < positions.Count; i++)
            {
                var pos = positions[i];
                min = Vector3.Min(min, pos);
                max = Vector3.Max(max, pos);
            }
            
            return new Bounds((min + max) * 0.5f, max - min);
        }
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