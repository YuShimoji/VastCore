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
        private class ArchParams { public int segments = 16; public float width = 3f; public float height = 4f; public float thickness = 0.5f; public float depth = 1f; }
        private class PyramidParams { public int baseSides = 4; public float baseRadius = 2f; public float height = 3f; }
        
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
        private ArchParams _archParams = new ArchParams();
        private PyramidParams _pyramidParams = new PyramidParams();
        
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
                case BasicShapeType.Arch:
                    _archParams.segments = EditorGUILayout.IntSlider("アーチ分割数", _archParams.segments, 4, 64);
                    _archParams.width = EditorGUILayout.Slider("幅", _archParams.width, 0.5f, 20f);
                    _archParams.height = EditorGUILayout.Slider("高さ", _archParams.height, 0.5f, 20f);
                    _archParams.thickness = EditorGUILayout.Slider("厚み", _archParams.thickness, 0.1f, 5f);
                    _archParams.depth = EditorGUILayout.Slider("奥行き", _archParams.depth, 0.1f, 10f);
                    break;
                case BasicShapeType.Pyramid:
                    _pyramidParams.baseSides = EditorGUILayout.IntSlider("底面の辺数", _pyramidParams.baseSides, 3, 12);
                    _pyramidParams.baseRadius = EditorGUILayout.Slider("底面半径", _pyramidParams.baseRadius, 0.1f, 20f);
                    _pyramidParams.height = EditorGUILayout.Slider("高さ", _pyramidParams.height, 0.1f, 20f);
                    break;
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
                case BasicShapeType.Arch:
                    pbMesh = CreateManualArch();
                    break;
                case BasicShapeType.Pyramid:
                    pbMesh = CreateManualPyramid();
                    break;
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

        private ProBuilderMesh CreateManualArch()
        {
            int segments = _archParams.segments;
            float halfWidth = _archParams.width / 2f;
            float archHeight = _archParams.height;
            float thickness = _archParams.thickness;
            float depth = _archParams.depth * _parent.GlobalSettings.GlobalStructureScale;
            float halfDepth = depth / 2f;

            // アーチ = 半円の外輪 + 内輪 を前後面で繋いだ形状
            float outerRadius = Mathf.Sqrt(halfWidth * halfWidth + archHeight * archHeight) * 0.5f;
            // 実用的には幅と高さから楕円的にサンプリング
            float radiusX = halfWidth;
            float radiusY = archHeight;
            float innerRadiusX = radiusX - thickness;
            float innerRadiusY = radiusY - thickness;

            List<Vector3> vertices = new List<Vector3>();
            // 前面: 外輪 (0..segments) + 内輪 (segments+1..2*segments+1)
            // 後面: 外輪 (2*(segments+1)..3*segments+2) + 内輪 (3*(segments+1)..4*segments+3)
            int ringCount = segments + 1;

            for (int face = 0; face < 2; face++)
            {
                float z = face == 0 ? halfDepth : -halfDepth;
                // 外輪
                for (int i = 0; i <= segments; i++)
                {
                    float t = (float)i / segments;
                    float angle = t * Mathf.PI; // 0 to PI (半円)
                    float x = Mathf.Cos(angle) * radiusX;
                    float y = Mathf.Sin(angle) * radiusY;
                    vertices.Add(new Vector3(x, y, z));
                }
                // 内輪
                for (int i = 0; i <= segments; i++)
                {
                    float t = (float)i / segments;
                    float angle = t * Mathf.PI;
                    float x = Mathf.Cos(angle) * Mathf.Max(0.01f, innerRadiusX);
                    float y = Mathf.Sin(angle) * Mathf.Max(0.01f, innerRadiusY);
                    vertices.Add(new Vector3(x, y, z));
                }
            }

            List<Face> faces = new List<Face>();
            int frontOuterStart = 0;
            int frontInnerStart = ringCount;
            int backOuterStart = ringCount * 2;
            int backInnerStart = ringCount * 3;

            for (int i = 0; i < segments; i++)
            {
                // 前面 (外輪-内輪)
                faces.Add(new Face(new int[] { frontOuterStart + i, frontOuterStart + i + 1, frontInnerStart + i + 1, frontInnerStart + i }));
                // 後面 (外輪-内輪) -- 反転ワインディング
                faces.Add(new Face(new int[] { backOuterStart + i, backInnerStart + i, backInnerStart + i + 1, backOuterStart + i + 1 }));
                // 外側面 (前面外輪-後面外輪)
                faces.Add(new Face(new int[] { frontOuterStart + i, backOuterStart + i, backOuterStart + i + 1, frontOuterStart + i + 1 }));
                // 内側面 (前面内輪-後面内輪) -- 反転
                faces.Add(new Face(new int[] { frontInnerStart + i, frontInnerStart + i + 1, backInnerStart + i + 1, backInnerStart + i }));
            }

            // 左右の柱底面キャップ (アーチの両端)
            // 左端: i=0
            faces.Add(new Face(new int[] { frontOuterStart, frontInnerStart, backInnerStart, backOuterStart }));
            // 右端: i=segments
            faces.Add(new Face(new int[] { frontOuterStart + segments, backOuterStart + segments, backInnerStart + segments, frontInnerStart + segments }));

            ProBuilderMesh pbMesh = ProBuilderMesh.Create(vertices, faces);
            float scale = _parent.GlobalSettings.GlobalStructureScale;
            pbMesh.transform.localScale = Vector3.one * scale;
            return pbMesh;
        }

        private ProBuilderMesh CreateManualPyramid()
        {
            int sides = _pyramidParams.baseSides;
            float radius = _pyramidParams.baseRadius;
            float height = _pyramidParams.height;

            List<Vector3> vertices = new List<Vector3>();

            // 底面の頂点
            for (int i = 0; i < sides; i++)
            {
                float angle = (float)i / sides * 2f * Mathf.PI;
                float x = Mathf.Cos(angle) * radius;
                float z = Mathf.Sin(angle) * radius;
                vertices.Add(new Vector3(x, 0f, z));
            }
            // 頂点 (apex)
            int apexIndex = sides;
            vertices.Add(new Vector3(0f, height, 0f));

            List<Face> faces = new List<Face>();

            // 側面 (三角形)
            for (int i = 0; i < sides; i++)
            {
                int next = (i + 1) % sides;
                faces.Add(new Face(new int[] { i, next, apexIndex }));
            }

            // 底面 (三角形ファン)
            for (int i = 0; i < sides - 2; i++)
            {
                faces.Add(new Face(new int[] { 0, i + 2, i + 1 }));
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