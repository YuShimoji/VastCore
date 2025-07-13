using UnityEngine;
using UnityEditor;
using UnityEngine.ProBuilder;
using System.Collections.Generic;
using System.Linq;

namespace Vastcore.Editor.Generation
{
    /// <summary>
    /// 基本的な構造物生成機能を提供するタブクラス
    /// ProBuilderの標準機能を使用した安定した実装
    /// </summary>
    public class BasicStructureTab : IStructureTab
    {
        // --- データクラス ---
        private class CylinderParams { public int subdivisions = 12; public float radius = 1f; public float height = 2f; }
        private class TorusParams { public int rows = 12; public int columns = 24; public float innerRadius = 0.5f; public float outerRadius = 1f; }
        
        // --- IStructureTab 実装 ---
        public TabCategory Category => TabCategory.Generation;
        public string DisplayName => "Basic Shapes";
        public string Description => "パラメータを指定して、基本的な3D形状を生成します。";
        public bool SupportsRealTimeUpdate => false;
        
        // --- private メンバー ---
        private StructureGeneratorWindow _parent;
        private Vector2 _scrollPosition;
        
        private enum BasicShapeType { Cube, Cylinder, Sphere, Torus, Wall, Arch, Pyramid }
        private BasicShapeType _selectedShape = BasicShapeType.Cube;
        
        private int _creationCount = 1;
        
        // --- 形状別パラメータ ---
        private CylinderParams _cylinderParams = new CylinderParams();
        private TorusParams _torusParams = new TorusParams();
        
        public BasicStructureTab(StructureGeneratorWindow parent)
        {
            _parent = parent;
        }
        
        public void DrawGUI()
        {
            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);
            
            EditorGUILayout.LabelField(DisplayName, EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(Description, MessageType.Info);
            
            EditorGUILayout.Space();
            
            _selectedShape = (BasicShapeType)EditorGUILayout.EnumPopup("生成する形状", _selectedShape);
            _creationCount = EditorGUILayout.IntSlider("生成数", _creationCount, 1, 50);
            
            EditorGUILayout.Space();
            
            // 形状に応じたパラメータUIを描画
            DrawShapeSpecificParams();
            
            EditorGUILayout.Space(20);

            // 生成ボタン
            GUI.backgroundColor = new Color(0.6f, 1f, 0.6f); // 明るい緑
            if (GUILayout.Button($"Generate {_selectedShape}", GUILayout.Height(30)))
            {
                GenerateShapes();
            }
            GUI.backgroundColor = Color.white;

            EditorGUILayout.EndScrollView();
        }

        private void DrawShapeSpecificParams()
        {
            EditorGUILayout.LabelField("形状パラメータ", EditorStyles.boldLabel);
            EditorGUI.indentLevel++;

            switch (_selectedShape)
            {
                case BasicShapeType.Cylinder:
                    _cylinderParams.subdivisions = EditorGUILayout.IntSlider("分割数", _cylinderParams.subdivisions, 3, 64);
                    _cylinderParams.height = EditorGUILayout.Slider("高さ", _cylinderParams.height, 0.1f, 20f);
                    break;
                case BasicShapeType.Torus:
                    _torusParams.columns = EditorGUILayout.IntSlider("チューブ分割数", _torusParams.columns, 3, 64);
                    _torusParams.rows = EditorGUILayout.IntSlider("断面分割数", _torusParams.rows, 3, 64);
                    _torusParams.outerRadius = EditorGUILayout.Slider("大半径", _torusParams.outerRadius, 0.1f, 20f);
                    _torusParams.innerRadius = EditorGUILayout.Slider("小半径", _torusParams.innerRadius, 0.05f, _torusParams.outerRadius);
                    break;
                // 他の形状も同様に追加...
                default:
                    EditorGUILayout.HelpBox("この形状に追加のパラメータはありません。", MessageType.None);
                    break;
        }
        
            EditorGUI.indentLevel--;
        }
        
        private void GenerateShapes()
            {
            for (int i = 0; i < _creationCount; i++)
                {
                GameObject newShape = CreateShapeObject();
                if (newShape != null)
                        {
                    float spacing = _parent.GlobalSettings.GlobalStructureScale * 2.5f;
                    newShape.transform.position = _parent.GlobalSettings.DefaultSpawnPosition + new Vector3(i * spacing, 0, 0);
                    _parent.ApplyMaterial(newShape.GetComponent<ProBuilderMesh>());
            }
        }
        }
        
        private GameObject CreateShapeObject()
        {
            ProBuilderMesh pbMesh = null;
            
            switch (_selectedShape)
            {
                case BasicShapeType.Cube:
                        pbMesh = ShapeGenerator.CreateShape(ShapeType.Cube);
                    pbMesh.transform.localScale = Vector3.one * _parent.GlobalSettings.GlobalStructureScale;
                        break;
                case BasicShapeType.Cylinder:
                    pbMesh = CreateManualCylinder();
                        break;
                case BasicShapeType.Sphere:
                        pbMesh = ShapeGenerator.CreateShape(ShapeType.Sphere);
                    pbMesh.transform.localScale = Vector3.one * _parent.GlobalSettings.GlobalStructureScale;
                        break;
                case BasicShapeType.Torus:
                    pbMesh = CreateManualTorus();
                        break;
                case BasicShapeType.Wall:
                    pbMesh = ShapeGenerator.CreateShape(ShapeType.Cube);
                    pbMesh.transform.localScale = new Vector3(5f, 3f, 0.2f) * _parent.GlobalSettings.GlobalStructureScale;
                        break;
                 // TODO: Arch, Pyramid
                }
                
                if (pbMesh != null)
                {
                pbMesh.name = $"{_selectedShape}_{System.DateTime.Now:HHmmss}";
                pbMesh.ToMesh();
                pbMesh.Refresh();
                    return pbMesh.gameObject;
            }
            return null;
        }
        
        private ProBuilderMesh CreateManualCylinder()
        {
            int sides = _cylinderParams.subdivisions;
            float height = _cylinderParams.height;
            float radius = _parent.GlobalSettings.GlobalStructureScale / 2f; 

            List<Vector3> vertices = new List<Vector3>();
                    
            // 天面と底面の頂点
            for (int i = 0; i < sides; i++)
                    {
                float angle = (float)i / sides * 360f * Mathf.Deg2Rad;
                float x = Mathf.Cos(angle) * radius;
                float z = Mathf.Sin(angle) * radius;
                vertices.Add(new Vector3(x, height / 2f, z)); // 天面
            }
            for (int i = 0; i < sides; i++)
            {
                float angle = (float)i / sides * 360f * Mathf.Deg2Rad;
                float x = Mathf.Cos(angle) * radius;
                float z = Mathf.Sin(angle) * radius;
                vertices.Add(new Vector3(x, -height / 2f, z)); // 底面
            }

            // 面 (Face) の定義
            List<Face> faces = new List<Face>();
            // 側面
            for(int i = 0; i < sides; i++)
                    {
                int next = (i + 1) % sides;
                faces.Add(new Face(new int[] { i, next, i + sides, next + sides }));
            }
            
            // 天面 (三角形の集まりとして)
            for(int i = 0; i < sides - 2; i++)
            {
                faces.Add(new Face(new int[] { 0, i + 1, i + 2 }));
                    }
                    
            // 底面 (三角形の集まりとして)
            for(int i = 0; i < sides - 2; i++)
            {
                faces.Add(new Face(new int[] { sides, sides + i + 2, sides + i + 1 }));
            }
            
            ProBuilderMesh pbMesh = ProBuilderMesh.Create(vertices, faces);
            return pbMesh;
        }
        
        private ProBuilderMesh CreateManualTorus()
        {
            int tubeSegments = _torusParams.columns;
            int radialSegments = _torusParams.rows;
            float tubeRadius = _torusParams.innerRadius;
            float mainRadius = _torusParams.outerRadius;

            List<Vector3> vertices = new List<Vector3>();
            
            for (int i = 0; i < tubeSegments; i++)
            {
                float u = (float)i / tubeSegments * 2f * Mathf.PI;
                Vector3 center = new Vector3(Mathf.Cos(u) * mainRadius, 0, Mathf.Sin(u) * mainRadius);

                for (int j = 0; j < radialSegments; j++)
                {
                    float v = (float)j / radialSegments * 2f * Mathf.PI;
                    Vector3 point = new Vector3(
                        (mainRadius + tubeRadius * Mathf.Cos(v)) * Mathf.Cos(u),
                        tubeRadius * Mathf.Sin(v),
                        (mainRadius + tubeRadius * Mathf.Cos(v)) * Mathf.Sin(u)
                    );
                    vertices.Add(point);
                }
            }

            List<Face> faces = new List<Face>();
            for (int i = 0; i < tubeSegments; i++)
            {
                for (int j = 0; j < radialSegments; j++)
                {
                    int nextI = (i + 1) % tubeSegments;
                    int nextJ = (j + 1) % radialSegments;
                    
                    int p0 = i * radialSegments + j;
                    int p1 = i * radialSegments + nextJ;
                    int p2 = nextI * radialSegments + nextJ;
                    int p3 = nextI * radialSegments + j;

                    faces.Add(new Face(new int[] { p0, p1, p2, p3 }));
                }
            }

            ProBuilderMesh pbMesh = ProBuilderMesh.Create(vertices, faces);
            float scale = _parent.GlobalSettings.GlobalStructureScale;
            pbMesh.transform.localScale = Vector3.one * scale;
            return pbMesh;
        }

        public void HandleRealTimeUpdate() { }

        public void OnSceneGUI() { }
        public void ProcessSelectedObjects() { }
        public void OnTabSelected() { }
        public void OnTabDeselected() { }
    }
} 