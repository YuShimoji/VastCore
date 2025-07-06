using UnityEditor;
using UnityEngine;
using UnityEngine.ProBuilder;
using UnityEditor.ProBuilder;
using System.Collections.Generic;
using System.Linq;

namespace Vastcore.Editor.Generation
{
    public class ProceduralTab
    {
        private StructureGeneratorWindow window;

        // パラメータ
        private List<Vector3> wallPoints = new List<Vector3>() { new Vector3(0, 0, 0), new Vector3(10, 0, 0) };
        private int stairStepCount = 10;
        private float stairWidth = 4f;
        private float stairStepHeight = 0.2f;
        private float stairStepDepth = 0.3f;
        private int proceduralRoomCount = 5;
        private Vector2 proceduralRoomSizeMin = new Vector2(5, 5);
        private Vector2 proceduralRoomSizeMax = new Vector2(15, 15);
        private float proceduralRoomHeight = 4f;
        private bool proceduralCombineRooms = true;
        private float corridorWidth = 2f;
        
        public ProceduralTab(StructureGeneratorWindow window)
        {
            this.window = window;
        }

        public void Draw()
        {
            EditorGUILayout.LabelField("Procedural Generation", EditorStyles.boldLabel);
            
            EditorGUILayout.LabelField("Continuous Wall", EditorStyles.boldLabel);
            for (int i = 0; i < wallPoints.Count; i++)
            {
                wallPoints[i] = EditorGUILayout.Vector3Field($"Point {i + 1}", wallPoints[i]);
            }
            if (GUILayout.Button("Add Wall Point"))
            {
                wallPoints.Add(wallPoints.Count > 0 ? wallPoints[wallPoints.Count - 1] : Vector3.zero);
            }
            if (GUILayout.Button("Remove Last Wall Point") && wallPoints.Count > 2)
            {
                wallPoints.RemoveAt(wallPoints.Count - 1);
            }
            if (GUILayout.Button("Create Continuous Wall"))
            {
                CreateContinuousWall();
            }

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Stairs", EditorStyles.boldLabel);
            stairStepCount = EditorGUILayout.IntField("Step Count", stairStepCount);
            stairWidth = EditorGUILayout.FloatField("Width", stairWidth);
            stairStepHeight = EditorGUILayout.FloatField("Step Height", stairStepHeight);
            stairStepDepth = EditorGUILayout.FloatField("Step Depth", stairStepDepth);
            if (GUILayout.Button("Create Stairs"))
            {
                CreateStairs();
            }

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Room and Corridor Structure", EditorStyles.boldLabel);
            proceduralRoomCount = EditorGUILayout.IntField("Room Count", proceduralRoomCount);
            proceduralRoomSizeMin = EditorGUILayout.Vector2Field("Room Size Min", proceduralRoomSizeMin);
            proceduralRoomSizeMax = EditorGUILayout.Vector2Field("Room Size Max", proceduralRoomSizeMax);
            proceduralRoomHeight = EditorGUILayout.FloatField("Room Height", proceduralRoomHeight);
            corridorWidth = EditorGUILayout.FloatField("Corridor Width", corridorWidth);
            proceduralCombineRooms = EditorGUILayout.Toggle("Combine into Single Mesh", proceduralCombineRooms);
            if (GUILayout.Button("Generate Procedural Structure"))
            {
                GenerateProceduralStructure();
            }
        }

        private ProBuilderMesh CreateSizedCube(string name, Vector3 size)
        {
            Vector3 halfSize = size * 0.5f;
            
            Vector3[] vertices = new Vector3[]
            {
                new Vector3(-halfSize.x, -halfSize.y, -halfSize.z),
                new Vector3( halfSize.x, -halfSize.y, -halfSize.z),
                new Vector3( halfSize.x,  halfSize.y, -halfSize.z),
                new Vector3(-halfSize.x,  halfSize.y, -halfSize.z),
                new Vector3(-halfSize.x, -halfSize.y,  halfSize.z),
                new Vector3( halfSize.x, -halfSize.y,  halfSize.z),
                new Vector3( halfSize.x,  halfSize.y,  halfSize.z),
                new Vector3(-halfSize.x,  halfSize.y,  halfSize.z)
            };

            Face[] faces = new Face[]
            {
                new Face(new int[] { 0, 3, 2, 1 }),
                new Face(new int[] { 4, 5, 6, 7 }),
                new Face(new int[] { 0, 4, 7, 3 }),
                new Face(new int[] { 1, 2, 6, 5 }),
                new Face(new int[] { 3, 7, 6, 2 }),
                new Face(new int[] { 0, 1, 5, 4 })
            };

            ProBuilderMesh pb = ProBuilderMesh.Create(vertices, faces);
            pb.name = name;
            pb.Refresh();
            pb.ToMesh();
            return pb;
        }

        private void CreateContinuousWall()
        {
            if (wallPoints.Count < 2)
            {
                UnityEditor.EditorUtility.DisplayDialog("Error", "A continuous wall requires at least 2 points.", "OK");
                return;
            }

            var parent = new GameObject("ContinuousWall");
            Undo.RegisterCreatedObjectUndo(parent, "Create Continuous Wall");

            float wallThickness = 0.3f; 
            float wallHeight = 4f;

            for (int i = 0; i < wallPoints.Count - 1; i++)
            {
                Vector3 start = wallPoints[i];
                Vector3 end = wallPoints[i+1];

                Vector3 midPoint = (start + end) / 2;
                float length = Vector3.Distance(start, end);
                Quaternion rotation = Quaternion.LookRotation(end - start);

                Vector3 size = new Vector3(wallThickness, wallHeight, length);
                var pb = CreateSizedCube($"Wall_{i}-{i+1}", size);
                pb.transform.position = midPoint;
                pb.transform.rotation = rotation;

                window.ApplyMaterial(pb);
                pb.gameObject.transform.SetParent(parent.transform);
            }
        }

        private void CreateStairs()
        {
            var parent = new GameObject("Stairs");
            Undo.RegisterCreatedObjectUndo(parent, "Create Stairs");

            for(int i = 0; i < stairStepCount; i++)
            {
                Vector3 size = new Vector3(stairWidth, stairStepHeight, stairStepDepth);
                var pb = CreateSizedCube($"Step_{i}", size);
                
                Vector3 pos = new Vector3(0, i * stairStepHeight + stairStepHeight / 2, i * stairStepDepth + stairStepDepth / 2);
                pb.transform.position = parent.transform.TransformPoint(pos);
                pb.transform.rotation = parent.transform.rotation;
                
                window.ApplyMaterial(pb);
                pb.gameObject.transform.SetParent(parent.transform);
            }
            
            Selection.activeGameObject = parent;
        }

        private void GenerateProceduralStructure()
        {
            var parent = new GameObject("ProceduralStructure");
            Undo.RegisterCreatedObjectUndo(parent, "Generate Procedural Structure");

            List<ProBuilderMesh> rooms = new List<ProBuilderMesh>();
            List<GameObject> allObjectsToCombine = new List<GameObject>();

            for (int i = 0; i < proceduralRoomCount; i++)
            {
                float width = Random.Range(proceduralRoomSizeMin.x, proceduralRoomSizeMax.x);
                float depth = Random.Range(proceduralRoomSizeMin.y, proceduralRoomSizeMax.y);
                Vector3 pos = new Vector3(Random.Range(-50, 50), proceduralRoomHeight / 2f, Random.Range(-50, 50));

                Vector3 size = new Vector3(width, proceduralRoomHeight, depth);
                var pb = CreateSizedCube($"Room_{i}", size);
                pb.transform.position = pos;

                rooms.Add(pb);
                allObjectsToCombine.Add(pb.gameObject);
            }

            SeparateRooms(rooms);
            GenerateCorridors(rooms, allObjectsToCombine);

            foreach (var obj in allObjectsToCombine)
            {
                obj.transform.SetParent(parent.transform);
                window.ApplyMaterial(obj.GetComponent<ProBuilderMesh>());
            }
        }

        private void SeparateRooms(List<ProBuilderMesh> rooms)
        {
            // Simple physics-based separation
            for (int i = 0; i < 10; i++) // Iterations
            {
                for (int j = 0; j < rooms.Count; j++)
                {
                    for (int k = j + 1; k < rooms.Count; k++)
                    {
                        var r1 = new Bounds(rooms[j].transform.position, rooms[j].GetComponent<MeshRenderer>().bounds.size);
                        var r2 = new Bounds(rooms[k].transform.position, rooms[k].GetComponent<MeshRenderer>().bounds.size);

                        if (r1.Intersects(r2))
                        {
                            Vector3 move = (r1.center - r2.center).normalized * 0.5f;
                            rooms[j].transform.position += move;
                            rooms[k].transform.position -= move;
                        }
                    }
                }
            }
        }

        private void GenerateCorridors(List<ProBuilderMesh> rooms, List<GameObject> allObjectsToCombine)
        {
            // Simplified corridor generation using MST (Prim's or Kruskal's)
            List<Edge> edges = new List<Edge>();
            for(int i = 0; i < rooms.Count; i++)
            {
                for (int j = i + 1; j < rooms.Count; j++)
                {
                    float dist = Vector3.Distance(rooms[i].transform.position, rooms[j].transform.position);
                    edges.Add(new Edge(i, j, dist));
                }
            }

            edges = edges.OrderBy(e => e.distance).ToList();
            var mst = new List<Edge>();
            var ds = new DisjointSet(rooms.Count);

            foreach (var edge in edges)
            {
                if (ds.Find(edge.from) != ds.Find(edge.to))
                {
                    ds.Union(edge.from, edge.to);
                    mst.Add(edge);
                }
            }
            
            foreach (var edge in mst)
            {
                var room1 = rooms[edge.from];
                var room2 = rooms[edge.to];
                var pos1 = room1.transform.position;
                var pos2 = room2.transform.position;

                string corridorName = $"Corridor_{edge.from}-{edge.to}";
                Vector3 corridorPosition = (pos1 + pos2) / 2;
                float length = Vector3.Distance(pos1, pos2);
                Quaternion corridorRotation = Quaternion.LookRotation(pos2 - pos1);
                
                Vector3 size = new Vector3(corridorWidth, proceduralRoomHeight, length);
                var corridorPb = CreateSizedCube(corridorName, size);
                corridorPb.transform.position = corridorPosition;
                corridorPb.transform.rotation = corridorRotation;
                allObjectsToCombine.Add(corridorPb.gameObject);
            }
        }
        
        private class Edge 
        {
            public int from, to;
            public float distance;
            public Edge(int f, int t, float d) { from = f; to = t; distance = d; }
        }

        private class DisjointSet 
        {
            private int[] parent;
            public int Count { get; private set; }
            public DisjointSet(int size) 
            {
                parent = new int[size];
                Count = size;
                for(int i=0; i<size; i++) parent[i] = i;
            }
            public int Find(int i) 
            {
                if (parent[i] == i) return i;
                return parent[i] = Find(parent[i]);
            }
            public void Union(int i, int j) 
            {
                int root_i = Find(i);
                int root_j = Find(j);
                if (root_i != root_j) 
                {
                    parent[root_i] = root_j;
                    Count--;
                }
            }
        }
    }
} 