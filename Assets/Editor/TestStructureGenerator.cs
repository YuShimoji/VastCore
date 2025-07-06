using UnityEngine;
using UnityEditor;
using UnityEngine.ProBuilder;
using UnityEngine.ProBuilder.Shapes;

namespace Vastcore.Editor.Test
{
    public class TestStructureGenerator : EditorWindow
    {
        [MenuItem("Window/Vastcore/Generate Test Structures")]
        public static void ShowWindow()
        {
            GetWindow<TestStructureGenerator>("Test Structure Generator");
        }

        [MenuItem("Vastcore/Generate Test Structures")]
        public static void GenerateTestStructures()
        {
            // 既存のテスト構造を削除
            GameObject existing = GameObject.Find("TestStructures");
            if (existing != null)
            {
                DestroyImmediate(existing);
            }

            // 親オブジェクトを作成
            GameObject parent = new GameObject("TestStructures");
            
            // 基本形状グループ
            GameObject basicShapes = new GameObject("BasicShapes");
            basicShapes.transform.SetParent(parent.transform);
            
            // 複合形状グループ
            GameObject complexShapes = new GameObject("ComplexShapes");
            complexShapes.transform.SetParent(parent.transform);
            
            // 分散配置グループ
            GameObject distributedShapes = new GameObject("DistributedShapes");
            distributedShapes.transform.SetParent(parent.transform);

            // 基本形状を作成
            CreateBasicShapes(basicShapes.transform);
            
            // 複合形状を作成
            CreateComplexShapes(complexShapes.transform);
            
            // 分散配置形状を作成
            CreateDistributedShapes(distributedShapes.transform);
            
            Debug.Log("テスト構造が生成されました。Structure Generator Windowで各機能をテストできます。");
        }

        private void OnGUI()
        {
            GUILayout.Label("Test Structure Generator", EditorStyles.boldLabel);
            
            if (GUILayout.Button("Generate All Test Structures"))
            {
                GenerateTestStructures();
            }
            
            GUILayout.Space(10);
            
            if (GUILayout.Button("Generate Basic Shapes Only"))
            {
                GameObject parent = new GameObject("BasicShapes");
                CreateBasicShapes(parent.transform);
            }
            
            if (GUILayout.Button("Generate Complex Shapes Only"))
            {
                GameObject parent = new GameObject("ComplexShapes");
                CreateComplexShapes(parent.transform);
            }
            
            if (GUILayout.Button("Generate Distributed Shapes Only"))
            {
                GameObject parent = new GameObject("DistributedShapes");
                CreateDistributedShapes(parent.transform);
            }
            
            GUILayout.Space(10);
            
            if (GUILayout.Button("Clear All Test Structures"))
            {
                GameObject existing = GameObject.Find("TestStructures");
                if (existing != null)
                {
                    DestroyImmediate(existing);
                }
                
                GameObject basicShapes = GameObject.Find("BasicShapes");
                if (basicShapes != null)
                {
                    DestroyImmediate(basicShapes);
                }
                
                GameObject complexShapes = GameObject.Find("ComplexShapes");
                if (complexShapes != null)
                {
                    DestroyImmediate(complexShapes);
                }
                
                GameObject distributedShapes = GameObject.Find("DistributedShapes");
                if (distributedShapes != null)
                {
                    DestroyImmediate(distributedShapes);
                }
            }
        }

        private static void CreateBasicShapes(Transform parent)
        {
            // キューブ
            var cube = ShapeFactory.Instantiate<Cube>();
            cube.transform.SetParent(parent);
            cube.transform.position = new Vector3(-10, 1, 0);
            cube.name = "TestCube";
            
            // シリンダー
            var cylinder = ShapeFactory.Instantiate<Cylinder>();
            cylinder.transform.SetParent(parent);
            cylinder.transform.position = new Vector3(-5, 1, 0);
            cylinder.name = "TestCylinder";
            
            // 球
            var sphere = ShapeFactory.Instantiate<Sphere>();
            sphere.transform.SetParent(parent);
            sphere.transform.position = new Vector3(0, 1, 0);
            sphere.name = "TestSphere";
            
            // プリズム
            var prism = ShapeFactory.Instantiate<Prism>();
            prism.transform.SetParent(parent);
            prism.transform.position = new Vector3(5, 1, 0);
            prism.name = "TestPrism";
            
            // 階段
            var stairs = ShapeFactory.Instantiate<Stairs>();
            stairs.transform.SetParent(parent);
            stairs.transform.position = new Vector3(10, 1, 0);
            stairs.name = "TestStairs";
        }

        private static void CreateComplexShapes(Transform parent)
        {
            // 複数のキューブを組み合わせた構造
            CreateTowerStructure(parent, new Vector3(-10, 0, 10));
            
            // L字型構造
            CreateLShapeStructure(parent, new Vector3(-5, 0, 10));
            
            // 十字型構造
            CreateCrossStructure(parent, new Vector3(0, 0, 10));
            
            // 城壁風構造
            CreateWallStructure(parent, new Vector3(5, 0, 10));
            
            // アーチ構造
            CreateArchStructure(parent, new Vector3(10, 0, 10));
        }

        private static void CreateDistributedShapes(Transform parent)
        {
            // 円形配置
            CreateCircularDistribution(parent, new Vector3(-10, 0, 20), 5, 3f);
            
            // グリッド配置
            CreateGridDistribution(parent, new Vector3(-5, 0, 20), 3, 3, 2f);
            
            // ランダム配置
            CreateRandomDistribution(parent, new Vector3(0, 0, 20), 8, 5f);
            
            // スパイラル配置
            CreateSpiralDistribution(parent, new Vector3(5, 0, 20), 10, 4f);
            
            // 線形配置
            CreateLinearDistribution(parent, new Vector3(10, 0, 20), 5, 2f);
        }

        private static void CreateTowerStructure(Transform parent, Vector3 position)
        {
            GameObject tower = new GameObject("Tower");
            tower.transform.SetParent(parent);
            tower.transform.position = position;
            
            for (int i = 0; i < 5; i++)
            {
                var cube = ShapeFactory.Instantiate<Cube>();
                cube.transform.SetParent(tower.transform);
                cube.transform.localPosition = new Vector3(0, i * 2, 0);
                cube.transform.localScale = new Vector3(2 - i * 0.2f, 2, 2 - i * 0.2f);
                cube.name = $"TowerLevel_{i}";
            }
        }

        private static void CreateLShapeStructure(Transform parent, Vector3 position)
        {
            GameObject lShape = new GameObject("LShape");
            lShape.transform.SetParent(parent);
            lShape.transform.position = position;
            
            // 横の部分
            for (int i = 0; i < 4; i++)
            {
                var cube = ShapeFactory.Instantiate<Cube>();
                cube.transform.SetParent(lShape.transform);
                cube.transform.localPosition = new Vector3(i * 2, 1, 0);
                cube.name = $"LShape_Horizontal_{i}";
            }
            
            // 縦の部分
            for (int i = 1; i < 4; i++)
            {
                var cube = ShapeFactory.Instantiate<Cube>();
                cube.transform.SetParent(lShape.transform);
                cube.transform.localPosition = new Vector3(0, 1, i * 2);
                cube.name = $"LShape_Vertical_{i}";
            }
        }

        private static void CreateCrossStructure(Transform parent, Vector3 position)
        {
            GameObject cross = new GameObject("Cross");
            cross.transform.SetParent(parent);
            cross.transform.position = position;
            
            // 中央
            var center = ShapeFactory.Instantiate<Cube>();
            center.transform.SetParent(cross.transform);
            center.transform.localPosition = Vector3.zero;
            center.name = "Cross_Center";
            
            // 4方向の腕
            Vector3[] directions = { Vector3.forward, Vector3.back, Vector3.left, Vector3.right };
            for (int i = 0; i < directions.Length; i++)
            {
                var arm = ShapeFactory.Instantiate<Cube>();
                arm.transform.SetParent(cross.transform);
                arm.transform.localPosition = directions[i] * 2;
                arm.name = $"Cross_Arm_{i}";
            }
        }

        private static void CreateWallStructure(Transform parent, Vector3 position)
        {
            GameObject wall = new GameObject("Wall");
            wall.transform.SetParent(parent);
            wall.transform.position = position;
            
            for (int x = 0; x < 6; x++)
            {
                for (int y = 0; y < 3; y++)
                {
                    var cube = ShapeFactory.Instantiate<Cube>();
                    cube.transform.SetParent(wall.transform);
                    cube.transform.localPosition = new Vector3(x * 2, y * 2 + 1, 0);
                    cube.transform.localScale = new Vector3(2, 2, 0.5f);
                    cube.name = $"Wall_Block_{x}_{y}";
                }
            }
        }

        private static void CreateArchStructure(Transform parent, Vector3 position)
        {
            GameObject arch = new GameObject("Arch");
            arch.transform.SetParent(parent);
            arch.transform.position = position;
            
            // 左の柱
            var leftPillar = ShapeFactory.Instantiate<Cube>();
            leftPillar.transform.SetParent(arch.transform);
            leftPillar.transform.localPosition = new Vector3(-3, 2, 0);
            leftPillar.transform.localScale = new Vector3(1, 4, 1);
            leftPillar.name = "Arch_LeftPillar";
            
            // 右の柱
            var rightPillar = ShapeFactory.Instantiate<Cube>();
            rightPillar.transform.SetParent(arch.transform);
            rightPillar.transform.localPosition = new Vector3(3, 2, 0);
            rightPillar.transform.localScale = new Vector3(1, 4, 1);
            rightPillar.name = "Arch_RightPillar";
            
            // アーチの上部
            var archTop = ShapeFactory.Instantiate<Cube>();
            archTop.transform.SetParent(arch.transform);
            archTop.transform.localPosition = new Vector3(0, 4, 0);
            archTop.transform.localScale = new Vector3(6, 1, 1);
            archTop.name = "Arch_Top";
        }

        private static void CreateCircularDistribution(Transform parent, Vector3 center, int count, float radius)
        {
            GameObject group = new GameObject("CircularDistribution");
            group.transform.SetParent(parent);
            group.transform.position = center;
            
            for (int i = 0; i < count; i++)
            {
                float angle = (360f / count) * i;
                Vector3 pos = new Vector3(
                    Mathf.Sin(angle * Mathf.Deg2Rad) * radius,
                    1,
                    Mathf.Cos(angle * Mathf.Deg2Rad) * radius
                );
                
                var cube = ShapeFactory.Instantiate<Cube>();
                cube.transform.SetParent(group.transform);
                cube.transform.localPosition = pos;
                cube.name = $"Circular_{i}";
            }
        }

        private static void CreateGridDistribution(Transform parent, Vector3 center, int width, int height, float spacing)
        {
            GameObject group = new GameObject("GridDistribution");
            group.transform.SetParent(parent);
            group.transform.position = center;
            
            for (int x = 0; x < width; x++)
            {
                for (int z = 0; z < height; z++)
                {
                    var cube = ShapeFactory.Instantiate<Cube>();
                    cube.transform.SetParent(group.transform);
                    cube.transform.localPosition = new Vector3(x * spacing, 1, z * spacing);
                    cube.name = $"Grid_{x}_{z}";
                }
            }
        }

        private static void CreateRandomDistribution(Transform parent, Vector3 center, int count, float range)
        {
            GameObject group = new GameObject("RandomDistribution");
            group.transform.SetParent(parent);
            group.transform.position = center;
            
            for (int i = 0; i < count; i++)
            {
                Vector3 randomPos = new Vector3(
                    Random.Range(-range, range),
                    1,
                    Random.Range(-range, range)
                );
                
                var cube = ShapeFactory.Instantiate<Cube>();
                cube.transform.SetParent(group.transform);
                cube.transform.localPosition = randomPos;
                cube.name = $"Random_{i}";
            }
        }

        private static void CreateSpiralDistribution(Transform parent, Vector3 center, int count, float maxRadius)
        {
            GameObject group = new GameObject("SpiralDistribution");
            group.transform.SetParent(parent);
            group.transform.position = center;
            
            for (int i = 0; i < count; i++)
            {
                float t = (float)i / count;
                float angle = t * 360f * 2; // 2回転
                float radius = t * maxRadius;
                
                Vector3 pos = new Vector3(
                    Mathf.Sin(angle * Mathf.Deg2Rad) * radius,
                    1,
                    Mathf.Cos(angle * Mathf.Deg2Rad) * radius
                );
                
                var cube = ShapeFactory.Instantiate<Cube>();
                cube.transform.SetParent(group.transform);
                cube.transform.localPosition = pos;
                cube.name = $"Spiral_{i}";
            }
        }

        private static void CreateLinearDistribution(Transform parent, Vector3 center, int count, float spacing)
        {
            GameObject group = new GameObject("LinearDistribution");
            group.transform.SetParent(parent);
            group.transform.position = center;
            
            for (int i = 0; i < count; i++)
            {
                var cube = ShapeFactory.Instantiate<Cube>();
                cube.transform.SetParent(group.transform);
                cube.transform.localPosition = new Vector3(i * spacing, 1, 0);
                cube.name = $"Linear_{i}";
            }
        }
    }
} 