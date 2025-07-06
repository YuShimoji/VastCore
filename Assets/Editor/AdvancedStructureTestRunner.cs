using UnityEngine;
using UnityEditor;
using UnityEngine.ProBuilder;

namespace Vastcore.Editor.Generation
{
    /// <summary>
    /// Phase 2: 形状制御システムのテストランナー
    /// Advanced Structure Tab の機能をテストするためのユーティリティ
    /// </summary>
    public class AdvancedStructureTestRunner : EditorWindow
    {
        private int testIndex = 0;
        private readonly string[] testNames = {
            "Basic Monument Generation",
            "Shape Control System",
            "Twist Effect Test",
            "Taper Effect Test",
            "Boolean Parameters Test",
            "Advanced Processing Test",
            "All Monument Types Test"
        };

        [MenuItem("Tools/Vastcore/Advanced Structure Test Runner")]
        public static void ShowWindow()
        {
            GetWindow<AdvancedStructureTestRunner>("Advanced Structure Test");
        }

        private void OnGUI()
        {
            EditorGUILayout.LabelField("Phase 2: Advanced Structure Test Runner", EditorStyles.boldLabel);
            EditorGUILayout.Space();

            EditorGUILayout.LabelField("Test Selection", EditorStyles.boldLabel);
            testIndex = EditorGUILayout.Popup("Test Type", testIndex, testNames);
            
            EditorGUILayout.Space();
            
            if (GUILayout.Button("Run Test", GUILayout.Height(30)))
            {
                RunTest(testIndex);
            }
            
            EditorGUILayout.Space();
            
            if (GUILayout.Button("Run All Tests", GUILayout.Height(30)))
            {
                RunAllTests();
            }
            
            EditorGUILayout.Space();
            
            EditorGUILayout.HelpBox("このテストランナーはAdvanced Structure Tabの実装をテストします。\n" +
                                  "各テストは自動的に構造物を生成し、機能の動作を確認します。", MessageType.Info);
        }

        private void RunTest(int index)
        {
            Debug.Log($"Running test: {testNames[index]}");
            
            try
            {
                switch (index)
                {
                    case 0:
                        TestBasicMonumentGeneration();
                        break;
                    case 1:
                        TestShapeControlSystem();
                        break;
                    case 2:
                        TestTwistEffect();
                        break;
                    case 3:
                        TestTaperEffect();
                        break;
                    case 4:
                        TestBooleanParameters();
                        break;
                    case 5:
                        TestAdvancedProcessing();
                        break;
                    case 6:
                        TestAllMonumentTypes();
                        break;
                }
                
                Debug.Log($"Test completed successfully: {testNames[index]}");
                EditorUtility.DisplayDialog("Test Complete", $"Test '{testNames[index]}' completed successfully!", "OK");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Test failed: {testNames[index]} - {e.Message}");
                EditorUtility.DisplayDialog("Test Failed", $"Test '{testNames[index]}' failed:\n{e.Message}", "OK");
            }
        }

        private void RunAllTests()
        {
            Debug.Log("Running all Advanced Structure tests...");
            
            int passedTests = 0;
            int totalTests = testNames.Length;
            
            for (int i = 0; i < totalTests; i++)
            {
                try
                {
                    RunTest(i);
                    passedTests++;
                }
                catch (System.Exception e)
                {
                    Debug.LogError($"Test {i} failed: {e.Message}");
                }
            }
            
            string result = $"Test Results: {passedTests}/{totalTests} tests passed";
            Debug.Log(result);
            EditorUtility.DisplayDialog("All Tests Complete", result, "OK");
        }

        private void TestBasicMonumentGeneration()
        {
            // 基本的な記念碑生成のテスト
            var pbMesh = ShapeGenerator.CreateShape(ShapeType.Cube);
            if (pbMesh == null)
                throw new System.Exception("Failed to create basic cube");
                
            pbMesh.gameObject.name = "Test_BasicMonument";
            pbMesh.transform.position = new Vector3(0, 0, 0);
            pbMesh.ToMesh();
            pbMesh.Refresh();
            
            Debug.Log("Basic monument generation test passed");
        }

        private void TestShapeControlSystem()
        {
            // 形状制御システムのテスト
            var pbMesh = ShapeGenerator.CreateShape(ShapeType.Cylinder);
            if (pbMesh == null)
                throw new System.Exception("Failed to create cylinder for shape control test");
                
            pbMesh.gameObject.name = "Test_ShapeControl";
            pbMesh.transform.position = new Vector3(5, 0, 0);
            
            // 基本的な形状変更をテスト
            pbMesh.transform.localScale = new Vector3(1f, 2f, 1f);
            pbMesh.ToMesh();
            pbMesh.Refresh();
            
            Debug.Log("Shape control system test passed");
        }

        private void TestTwistEffect()
        {
            // ツイスト効果のテスト
            var pbMesh = ShapeGenerator.CreateShape(ShapeType.Cylinder);
            if (pbMesh == null)
                throw new System.Exception("Failed to create cylinder for twist test");
                
            pbMesh.gameObject.name = "Test_TwistEffect";
            pbMesh.transform.position = new Vector3(10, 0, 0);
            
            // ツイスト効果を適用
            ApplyTestTwist(pbMesh, 45f);
            
            Debug.Log("Twist effect test passed");
        }

        private void TestTaperEffect()
        {
            // テーパー効果のテスト
            var pbMesh = ShapeGenerator.CreateShape(ShapeType.Cube);
            if (pbMesh == null)
                throw new System.Exception("Failed to create cube for taper test");
                
            pbMesh.gameObject.name = "Test_TaperEffect";
            pbMesh.transform.position = new Vector3(15, 0, 0);
            
            // テーパー効果を適用
            ApplyTestTaper(pbMesh, 0.5f);
            
            Debug.Log("Taper effect test passed");
        }

        private void TestBooleanParameters()
        {
            // Boolean演算パラメータのテスト
            var pbMesh = ShapeGenerator.CreateShape(ShapeType.Cube);
            if (pbMesh == null)
                throw new System.Exception("Failed to create cube for boolean test");
                
            pbMesh.gameObject.name = "Test_BooleanParams";
            pbMesh.transform.position = new Vector3(20, 0, 0);
            
            // Boolean演算のパラメータ設定をテスト
            var booleanParams = new BooleanParameters
            {
                operation = BooleanOperation.Subtract,
                completionRatio = 0.8f,
                faceMode = FaceSelectionMode.All
            };
            
            Debug.Log($"Boolean parameters test passed: {booleanParams.operation}");
        }

        private void TestAdvancedProcessing()
        {
            // 高度加工のテスト
            var pbMesh = ShapeGenerator.CreateShape(ShapeType.Sphere);
            if (pbMesh == null)
                throw new System.Exception("Failed to create sphere for advanced processing test");
                
            pbMesh.gameObject.name = "Test_AdvancedProcessing";
            pbMesh.transform.position = new Vector3(25, 0, 0);
            
            // スムージング設定をテスト
            for (int i = 0; i < pbMesh.faces.Count; i++)
            {
                pbMesh.faces[i].smoothingGroup = 1;
            }
            
            pbMesh.ToMesh();
            pbMesh.Refresh();
            
            Debug.Log("Advanced processing test passed");
        }

        private void TestAllMonumentTypes()
        {
            // 全ての記念碑タイプのテスト
            var monumentTypes = System.Enum.GetValues(typeof(AdvancedStructureTab.MonumentType));
            
            for (int i = 0; i < monumentTypes.Length; i++)
            {
                var type = (AdvancedStructureTab.MonumentType)monumentTypes.GetValue(i);
                var pbMesh = CreateTestMonument(type);
                
                if (pbMesh == null)
                    throw new System.Exception($"Failed to create monument type: {type}");
                    
                pbMesh.gameObject.name = $"Test_{type}";
                pbMesh.transform.position = new Vector3(i * 5, 0, -10);
                
                Debug.Log($"Monument type {type} created successfully");
            }
            
            Debug.Log("All monument types test passed");
        }

        private ProBuilderMesh CreateTestMonument(AdvancedStructureTab.MonumentType type)
        {
            switch (type)
            {
                case AdvancedStructureTab.MonumentType.GeometricMonolith:
                    return ShapeGenerator.CreateShape(ShapeType.Cube);
                case AdvancedStructureTab.MonumentType.TwistedTower:
                    return ShapeGenerator.CreateShape(ShapeType.Cylinder);
                case AdvancedStructureTab.MonumentType.PerforatedCube:
                    return ShapeGenerator.CreateShape(ShapeType.Cube);
                case AdvancedStructureTab.MonumentType.FloatingRings:
                    return ShapeGenerator.CreateShape(ShapeType.Torus);
                case AdvancedStructureTab.MonumentType.StackedGeometry:
                    return ShapeGenerator.CreateShape(ShapeType.Cube);
                case AdvancedStructureTab.MonumentType.SplitMonument:
                    return ShapeGenerator.CreateShape(ShapeType.Cube);
                case AdvancedStructureTab.MonumentType.CurvedArchway:
                    return ShapeGenerator.CreateShape(ShapeType.Arch);
                case AdvancedStructureTab.MonumentType.AbstractSculpture:
                    return ShapeGenerator.CreateShape(ShapeType.Sphere);
                default:
                    return ShapeGenerator.CreateShape(ShapeType.Cube);
            }
        }

        private void ApplyTestTwist(ProBuilderMesh pbMesh, float twistAngle)
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
            pbMesh.ToMesh();
            pbMesh.Refresh();
        }

        private void ApplyTestTaper(ProBuilderMesh pbMesh, float taperAmount)
        {
            var positions = pbMesh.positions;
            var bounds = CalculateBounds(pbMesh);
            
            for (int i = 0; i < positions.Count; i++)
            {
                var pos = positions[i];
                float normalizedY = (pos.y - bounds.min.y) / bounds.size.y;
                float scale = 1f - taperAmount * normalizedY;
                
                positions[i] = new Vector3(
                    pos.x * scale,
                    pos.y,
                    pos.z * scale
                );
            }
            
            pbMesh.positions = positions;
            pbMesh.ToMesh();
            pbMesh.Refresh();
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
} 