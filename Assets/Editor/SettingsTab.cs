using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using System.Reflection;

namespace Vastcore.Editor.Generation
{
    public class SettingsTab
    {
        private StructureGeneratorWindow window;

        public SettingsTab(StructureGeneratorWindow window)
        {
            this.window = window;
        }

        public void Draw()
        {
            EditorGUILayout.LabelField("Common Settings", EditorStyles.boldLabel);

            window.spawnPosition = EditorGUILayout.Vector3Field("Spawn Position", window.spawnPosition);
            window.spawnRotation = EditorGUILayout.Vector3Field("Spawn Rotation", window.spawnRotation);

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Material Palette", EditorStyles.boldLabel);
            
            window.defaultMaterial = (Material)EditorGUILayout.ObjectField("Default Material", window.defaultMaterial, typeof(Material), false);

            for (int i = 0; i < window.materialPalette.Count; i++)
            {
                window.materialPalette[i] = (Material)EditorGUILayout.ObjectField($"Material {i+1}", window.materialPalette[i], typeof(Material), false);
            }

            if (GUILayout.Button("Add Material Slot"))
            {
                window.materialPalette.Add(null);
            }
            if (window.materialPalette.Count > 0 && GUILayout.Button("Remove Last Material"))
            {
                window.materialPalette.RemoveAt(window.materialPalette.Count - 1);
            }

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Development Utilities", EditorStyles.boldLabel);
            if (GUILayout.Button("Create Test Setup"))
            {
                CreateTestSetup();
            }
            if (GUILayout.Button("Place Structure on Terrain"))
            {
                PlaceStructureOnTerrain();
            }
            if (GUILayout.Button("Move Player To Start"))
            {
                MovePlayerToStart();
            }
        }

        private void CreateTestSetup()
        {
            GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ground.name = "Ground";
            ground.transform.localScale = new Vector3(50, 1, 50);

            GameObject player = new GameObject("Player");
            player.AddComponent<Rigidbody>();
            player.AddComponent<CapsuleCollider>();
            
            // Note: This is a placeholder. A real project might use a different controller.
            player.transform.position = new Vector3(0, 1, 0);

            Light light = new GameObject("Directional Light").AddComponent<Light>();
            light.type = LightType.Directional;
            light.transform.rotation = Quaternion.Euler(50, -30, 0);

            var cam = Camera.main;
            if (cam != null)
            {
                cam.transform.position = new Vector3(0, 10, -20);
                cam.transform.LookAt(Vector3.zero);
            }

            UnityEditor.EditorUtility.DisplayDialog("Test Setup", "A test environment with a ground plane, player, and light has been created.", "OK");
        }
        
        private void PlaceStructureOnTerrain()
        {
            var selection = Selection.activeGameObject;
            if (selection == null)
            {
                UnityEditor.EditorUtility.DisplayDialog("Error", "Please select a structure to place on the terrain.", "OK");
                return;
            }

            var pbMeshes = selection.GetComponentsInChildren<UnityEngine.ProBuilder.ProBuilderMesh>();
            if (pbMeshes.Length == 0)
            {
                UnityEditor.EditorUtility.DisplayDialog("Error", "Selected object and its children contain no ProBuilder meshes.", "OK");
                return;
            }

            if (pbMeshes[0].GetComponent<MeshRenderer>() == null)
            {
                UnityEditor.EditorUtility.DisplayDialog("Error", "ProBuilder mesh is missing a MeshRenderer.", "OK");
                return;
            }

            Bounds combinedBounds = pbMeshes[0].GetComponent<MeshRenderer>().bounds;
            for (int i = 1; i < pbMeshes.Length; i++)
            {
                var renderer = pbMeshes[i].GetComponent<MeshRenderer>();
                if (renderer != null)
                {
                    combinedBounds.Encapsulate(renderer.bounds);
                }
            }
            
            float lowestPoint = combinedBounds.min.y;

            if (Physics.Raycast(selection.transform.position, Vector3.down, out RaycastHit hit, Mathf.Infinity))
            {
                Vector3 targetPosition = hit.point;
                targetPosition.y += selection.transform.position.y - lowestPoint;
                selection.transform.position = targetPosition;
                Debug.Log($"Structure placed on {hit.collider.name} at height {targetPosition.y}");
            }
            else
            {
                Debug.LogWarning("No terrain or collidable object found below the structure.");
            }
        }

        private void MovePlayerToStart()
        {
            GameObject player = GameObject.FindWithTag("Player");
            if(player == null) player = GameObject.Find("Player");

            GameObject startPoint = GameObject.Find("StartPoint");

            if (player != null && startPoint != null)
            {
                player.transform.position = startPoint.transform.position;
                player.transform.rotation = startPoint.transform.rotation;
                Debug.Log("Player moved to StartPoint.");
            }
            else
            {
                UnityEditor.EditorUtility.DisplayDialog("Error", "Could not find Player or StartPoint in the scene. Ensure they are named correctly.", "OK");
            }
        }
    }
} 