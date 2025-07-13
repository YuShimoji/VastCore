using UnityEngine;
using UnityEditor;
using UnityEngine.ProBuilder;
using UnityEditor.ProBuilder.Actions;
using System.Collections.Generic;
using System.Linq;

namespace Vastcore.Editor.Generation
{
    public class StructureGenerationTab : IStructureTab
    {
        public TabCategory Category => TabCategory.Generation;
        public string DisplayName => "Primitives";
        public string Description => "基本的なプリミティブ形状を生成します。";
        public bool SupportsRealTimeUpdate => false;
        
        private StructureGeneratorWindow _parent;
        private Vector2 _scrollPosition;

        // 各種パラメータ
        private float cubeSize = 5f;
        private float cylinderHeight = 10f;
        private float cylinderRadius = 1f;
        private int cylinderSubdivisions = 12;
        private float wallWidth = 10f;
        private float wallHeight = 4f;
        private float wallThickness = 0.5f;
        
        // 球のパラメータ
        private float sphereRadius = 5f;
        private int sphereSubdivisions = 2;

        // トーラスのパラメータ
        private float torusMajorRadius = 5f;
        private float torusMinorRadius = 1f;
        private int torusMajorSegments = 24;
        private int torusMinorSegments = 12;

        // ピラミッドのパラメータ
        private Vector2 pyramidBaseSize = new Vector2(5f, 5f);
        private float pyramidHeight = 8f;

        public StructureGenerationTab(StructureGeneratorWindow parent)
        {
            _parent = parent;
        }

        public void DrawGUI()
        {
            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);
            EditorGUILayout.LabelField("Primitive Generation", EditorStyles.boldLabel);

            // Cube
            EditorGUILayout.LabelField("Cube", EditorStyles.boldLabel);
            cubeSize = EditorGUILayout.FloatField("Size", cubeSize);
            if (GUILayout.Button("Generate Cube"))
            {
                GenerateCube();
            }
            EditorGUILayout.Space();

            // Cylinder
            EditorGUILayout.LabelField("Cylinder", EditorStyles.boldLabel);
            cylinderHeight = EditorGUILayout.FloatField("Height", cylinderHeight);
            cylinderRadius = EditorGUILayout.FloatField("Radius", cylinderRadius);
            cylinderSubdivisions = EditorGUILayout.IntField("Subdivisions", cylinderSubdivisions);
            if (GUILayout.Button("Generate Cylinder"))
            {
                GenerateCylinder();
            }
            EditorGUILayout.Space();

            // Wall
            EditorGUILayout.LabelField("Wall", EditorStyles.boldLabel);
            wallWidth = EditorGUILayout.FloatField("Width", wallWidth);
            wallHeight = EditorGUILayout.FloatField("Height", wallHeight);
            wallThickness = EditorGUILayout.FloatField("Thickness", wallThickness);
            if (GUILayout.Button("Generate Wall"))
            {
                GenerateWall();
            }
            EditorGUILayout.Space();

            // Sphere
            EditorGUILayout.LabelField("Sphere (Icosphere)", EditorStyles.boldLabel);
            sphereRadius = EditorGUILayout.FloatField("Radius", sphereRadius);
            sphereSubdivisions = EditorGUILayout.IntSlider("Subdivisions", sphereSubdivisions, 0, 5);
            if (GUILayout.Button("Generate Sphere"))
            {
                GenerateSphere();
            }
            EditorGUILayout.Space();

            // Torus
            EditorGUILayout.LabelField("Torus", EditorStyles.boldLabel);
            torusMajorRadius = EditorGUILayout.FloatField("Major Radius", torusMajorRadius);
            torusMinorRadius = EditorGUILayout.FloatField("Minor Radius", torusMinorRadius);
            torusMajorSegments = EditorGUILayout.IntField("Major Segments", torusMajorSegments);
            torusMinorSegments = EditorGUILayout.IntField("Minor Segments", torusMinorSegments);
            if (GUILayout.Button("Generate Torus"))
            {
                GenerateTorus();
            }
            EditorGUILayout.Space();

            // Pyramid
            EditorGUILayout.LabelField("Pyramid", EditorStyles.boldLabel);
            pyramidBaseSize = EditorGUILayout.Vector2Field("Base Size", pyramidBaseSize);
            pyramidHeight = EditorGUILayout.FloatField("Height", pyramidHeight);
            if (GUILayout.Button("Generate Pyramid"))
            {
                GeneratePyramid();
            }
            EditorGUILayout.Space();
            
            EditorGUILayout.EndScrollView();
        }
        
        private ProBuilderMesh CreateSizedCube(Vector3 size)
        {
            var half = size * 0.5f;
            
            // 8つの頂点を定義
            var vertices = new Vector3[]
            {
                new Vector3(-half.x, -half.y, -half.z), // 0
                new Vector3(half.x, -half.y, -half.z),  // 1
                new Vector3(half.x, -half.y, half.z),   // 2
                new Vector3(-half.x, -half.y, half.z),  // 3
                new Vector3(-half.x, half.y, -half.z),  // 4
                new Vector3(half.x, half.y, -half.z),   // 5
                new Vector3(half.x, half.y, half.z),    // 6
                new Vector3(-half.x, half.y, half.z)    // 7
            };

            // 6つの面をそれぞれ2つの三角形（合計12の面）で定義
            var faces = new Face[]
            {
                // Bottom (-Y)
                new Face(new int[] { 0, 1, 2 }), new Face(new int[] { 0, 2, 3 }),
                // Top (+Y)
                new Face(new int[] { 4, 7, 6 }), new Face(new int[] { 4, 6, 5 }),
                // Back (-Z)
                new Face(new int[] { 0, 4, 5 }), new Face(new int[] { 0, 5, 1 }),
                // Front (+Z)
                new Face(new int[] { 3, 2, 6 }), new Face(new int[] { 3, 6, 7 }),
                // Left (-X)
                new Face(new int[] { 3, 7, 4 }), new Face(new int[] { 3, 4, 0 }),
                // Right (+X)
                new Face(new int[] { 1, 5, 6 }), new Face(new int[] { 1, 6, 2 })
            };

            return ProBuilderMesh.Create(vertices, faces);
        }

        private void GenerateCube()
        {
            var pb = CreateSizedCube(new Vector3(cubeSize, cubeSize, cubeSize));
            pb.gameObject.name = "Cube";
            pb.transform.position = _parent.GlobalSettings.DefaultSpawnPosition;
            pb.transform.rotation = Quaternion.identity; // Rotation is not in GlobalSettings
            
            _parent.ApplyMaterial(pb);
            Undo.RegisterCreatedObjectUndo(pb.gameObject, "Create Cube");
            Selection.activeGameObject = pb.gameObject;
        }
        
        private ProBuilderMesh CreateCylinder(float radius, float height, int subdivisions)
        {
            if (subdivisions < 3) subdivisions = 3;
            var vertices = new List<Vector3>();
            var faces = new List<Face>();
            var halfHeight = height / 2f;

            // Vertices
            var bottomCenterIndex = 0;
            vertices.Add(new Vector3(0, -halfHeight, 0)); 
            var topCenterIndex = 1;
            vertices.Add(new Vector3(0, halfHeight, 0));

            for (int i = 0; i < subdivisions; i++)
            {
                float angle = (i / (float)subdivisions) * 2f * Mathf.PI;
                float x = Mathf.Cos(angle) * radius;
                float z = Mathf.Sin(angle) * radius;
                vertices.Add(new Vector3(x, -halfHeight, z)); // Bottom ring vertex
                vertices.Add(new Vector3(x, halfHeight, z));  // Top ring vertex
            }

            // Faces
            for (int i = 0; i < subdivisions; i++)
            {
                int next = (i + 1) % subdivisions;

                // Bottom cap
                int b0 = 2 + i * 2;
                int b1 = 2 + next * 2;
                faces.Add(new Face(new int[] { bottomCenterIndex, b1, b0 }));
                
                // Top cap
                int t0 = 3 + i * 2;
                int t1 = 3 + next * 2;
                faces.Add(new Face(new int[] { topCenterIndex, t0, t1 }));
                
                // Sides
                faces.Add(new Face(new int[] { b0, b1, t1 }));
                faces.Add(new Face(new int[] { b0, t1, t0 }));
            }

            return ProBuilderMesh.Create(vertices, faces);
        }

        private void GenerateCylinder()
        {
            var pb = CreateCylinder(cylinderRadius, cylinderHeight, cylinderSubdivisions);
            pb.gameObject.name = "Cylinder";
            pb.transform.position = _parent.GlobalSettings.DefaultSpawnPosition;
            pb.transform.rotation = Quaternion.identity;
            
            _parent.ApplyMaterial(pb);
            Undo.RegisterCreatedObjectUndo(pb.gameObject, "Create Cylinder");
            Selection.activeGameObject = pb.gameObject;
        }

        private void GenerateWall()
        {
            var pb = CreateSizedCube(new Vector3(wallWidth, wallHeight, wallThickness));
            pb.gameObject.name = "Wall";
            pb.transform.position = _parent.GlobalSettings.DefaultSpawnPosition;
            pb.transform.rotation = Quaternion.identity;
            
            _parent.ApplyMaterial(pb);
            Undo.RegisterCreatedObjectUndo(pb.gameObject, "Create Wall");
            Selection.activeGameObject = pb.gameObject;
        }

        private void GenerateSphere()
        {
            var go = new GameObject("Icosphere");
            var pb = go.AddComponent<ProBuilderMesh>();

            // Icosphere generation logic (remains mostly the same)
            var t = (1.0f + Mathf.Sqrt(5.0f)) / 2.0f;

            var vertices = new List<Vector3>
            {
                new Vector3(-1,  t,  0).normalized, new Vector3( 1,  t,  0).normalized, new Vector3(-1, -t,  0).normalized, new Vector3( 1, -t,  0).normalized,
                new Vector3( 0, -1,  t).normalized, new Vector3( 0,  1,  t).normalized, new Vector3( 0, -1, -t).normalized, new Vector3( 0,  1, -t).normalized,
                new Vector3( t,  0, -1).normalized, new Vector3( t,  0,  1).normalized, new Vector3(-t,  0, -1).normalized, new Vector3(-t,  0,  1).normalized
            };

            var faces = new List<Face>
            {
                new Face(new int[]{0, 11, 5}), new Face(new int[]{0, 5, 1}), new Face(new int[]{0, 1, 7}), new Face(new int[]{0, 7, 10}), new Face(new int[]{0, 10, 11}),
                new Face(new int[]{1, 5, 9}), new Face(new int[]{5, 11, 4}), new Face(new int[]{11, 10, 2}), new Face(new int[]{10, 7, 6}), new Face(new int[]{7, 1, 8}),
                new Face(new int[]{3, 9, 4}), new Face(new int[]{3, 4, 2}), new Face(new int[]{3, 2, 6}), new Face(new int[]{3, 6, 8}), new Face(new int[]{3, 8, 9}),
                new Face(new int[]{4, 9, 5}), new Face(new int[]{2, 4, 11}), new Face(new int[]{6, 2, 10}), new Face(new int[]{8, 6, 7}), new Face(new int[]{9, 8, 1})
            };
            
            var middlePointIndexCache = new Dictionary<long, int>();

            for (int i = 0; i < sphereSubdivisions; i++)
            {
                var faces2 = new List<Face>();
                foreach (var tri in faces)
                {
                    int a = GetMiddlePoint(tri.indexes[0], tri.indexes[1], ref vertices, ref middlePointIndexCache);
                    int b = GetMiddlePoint(tri.indexes[1], tri.indexes[2], ref vertices, ref middlePointIndexCache);
                    int c = GetMiddlePoint(tri.indexes[2], tri.indexes[0], ref vertices, ref middlePointIndexCache);
                    
                    faces2.Add(new Face(new int[]{tri.indexes[0], a, c}));
                    faces2.Add(new Face(new int[]{tri.indexes[1], b, a}));
                    faces2.Add(new Face(new int[]{tri.indexes[2], c, b}));
                    faces2.Add(new Face(new int[]{a, b, c}));
                }
                faces = faces2;
            }

            pb.positions = vertices.Select(x => x * sphereRadius).ToArray();
            pb.faces = faces;
            pb.ToMesh();
            pb.Refresh();
            
            go.transform.position = _parent.GlobalSettings.DefaultSpawnPosition;
            go.transform.rotation = Quaternion.identity;
            
            _parent.ApplyMaterial(pb);
            Undo.RegisterCreatedObjectUndo(go, "Create Sphere");
            Selection.activeGameObject = go;
        }
        
        private int GetMiddlePoint(int p1, int p2, ref List<Vector3> vertices, ref Dictionary<long, int> cache)
        {
            bool firstIsSmaller = p1 < p2;
            long smallerIndex = firstIsSmaller ? p1 : p2;
            long greaterIndex = firstIsSmaller ? p2 : p1;
            long key = (smallerIndex << 32) + greaterIndex;

            int ret;
            if (cache.TryGetValue(key, out ret))
            {
                return ret;
            }

            Vector3 point1 = vertices[p1];
            Vector3 point2 = vertices[p2];
            Vector3 middle = new Vector3(
                (point1.x + point2.x) / 2.0f,
                (point1.y + point2.y) / 2.0f,
                (point1.z + point2.z) / 2.0f);

            ret = vertices.Count;
            vertices.Add(middle.normalized);
            cache.Add(key, ret);

            return ret;
        }

        private void GenerateTorus()
        {
            var pb = CreateProBuilderTorus(torusMajorRadius, torusMinorRadius, torusMajorSegments, torusMinorSegments);
            pb.gameObject.name = "Torus";
            pb.transform.position = _parent.GlobalSettings.DefaultSpawnPosition;
            pb.transform.rotation = Quaternion.identity;
            
            _parent.ApplyMaterial(pb);
            Undo.RegisterCreatedObjectUndo(pb.gameObject, "Create Torus");
            Selection.activeGameObject = pb.gameObject;
        }

        private ProBuilderMesh CreateProBuilderTorus(float majorRadius, float minorRadius, int majorSegments, int minorSegments)
        {
            var vertices = new List<Vector3>();
            var faces = new List<Face>();

            for (int i = 0; i <= majorSegments; i++)
            {
                float u = i / (float)majorSegments * 2 * Mathf.PI;
                var center = new Vector3(Mathf.Cos(u) * majorRadius, 0, Mathf.Sin(u) * majorRadius);

                for (int j = 0; j <= minorSegments; j++)
                {
                    float v = j / (float)minorSegments * 2 * Mathf.PI;
                    var dir = new Vector3(Mathf.Cos(u) * Mathf.Cos(v), Mathf.Sin(v), Mathf.Sin(u) * Mathf.Cos(v));
                    vertices.Add(center + dir * minorRadius);
                }
            }

            for (int i = 0; i < majorSegments; i++)
            {
                for (int j = 0; j < minorSegments; j++)
                {
                    int p0 = i * (minorSegments + 1) + j;
                    int p1 = i * (minorSegments + 1) + (j + 1);
                    int p2 = (i + 1) * (minorSegments + 1) + (j + 1);
                    int p3 = (i + 1) * (minorSegments + 1) + j;
                    faces.Add(new Face(new int[] { p0, p3, p2, p1 }));
                }
            }
            return ProBuilderMesh.Create(vertices, faces);
        }

        private void GeneratePyramid()
        {
            var pb = ProBuilderMesh.Create(new Vector3[] {
                new Vector3(-pyramidBaseSize.x/2, 0, -pyramidBaseSize.y/2),
                new Vector3(pyramidBaseSize.x/2, 0, -pyramidBaseSize.y/2),
                new Vector3(pyramidBaseSize.x/2, 0, pyramidBaseSize.y/2),
                new Vector3(-pyramidBaseSize.x/2, 0, pyramidBaseSize.y/2),
                new Vector3(0, pyramidHeight, 0)
            }, new Face[] {
                new Face(new int[] { 0, 1, 2, 3 }), // base
                new Face(new int[] { 0, 4, 1 }),    // side
                new Face(new int[] { 1, 4, 2 }),    // side
                new Face(new int[] { 2, 4, 3 }),    // side
                new Face(new int[] { 3, 4, 0 })     // side
            });
            pb.gameObject.name = "Pyramid";
            pb.transform.position = _parent.GlobalSettings.DefaultSpawnPosition;
            pb.transform.rotation = Quaternion.identity;

            _parent.ApplyMaterial(pb);
            Undo.RegisterCreatedObjectUndo(pb.gameObject, "Create Pyramid");
            Selection.activeGameObject = pb.gameObject;
        }
        
        public void HandleRealTimeUpdate() { }
        public void OnSceneGUI() { }
        public void ProcessSelectedObjects() { }
        public void OnTabSelected() { }
        public void OnTabDeselected() { }
    }
} 