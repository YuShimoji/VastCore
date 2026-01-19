using UnityEditor;
using UnityEngine;
using Vastcore.Utilities;
using System.Collections.Generic;
using System.Linq;

namespace Vastcore.Editor.Generation
{
    [System.Serializable]
    public class HeightmapLayer
    {
        public Texture2D heightMap;
        public enum BlendMode { Add, Subtract, Multiply, Average }
        public BlendMode blendMode = BlendMode.Add;
        [Range(0, 1)]
        public float strength = 1.0f;
    }

    [System.Serializable]
    public class SplatmapRule
    {
        public TerrainLayer terrainLayer;
        [Range(0, 1)]
        public float minHeight = 0f;
        [Range(0, 1)]
        public float maxHeight = 1f;
        [Range(0, 90)]
        public float minSlope = 0f;
        [Range(0, 90)]
        public float maxSlope = 90f;
    }

    public class HeightmapTerrainGeneratorWindow : EditorWindow
    {
        [SerializeField]
        private List<HeightmapLayer> heightmapLayers = new List<HeightmapLayer>();
        [SerializeField]
        private List<SplatmapRule> splatmapRules = new List<SplatmapRule>();
        
        private SerializedObject serializedObject;
        private SerializedProperty serializedLayers;
        private SerializedProperty serializedSplatmapRules;

        private float terrainHeight = 50f;
        private Vector3 terrainSize = new Vector3(512, 512, 512);

        private Texture2D combinedHeightmapTex;
        private float[,] combinedHeightmap;
        
        private Vector2 scrollPos;

        // Hydraulic Erosion Parameters
        private bool showErosionSettings = true;
        private int erosionIterations = 50000;
        private float erosionInertia = 0.5f;
        private float sedimentCapacityFactor = 4f;
        private float minSedimentCapacity = 0.01f;
        private float erosionSpeed = 0.3f;
        private float depositSpeed = 0.3f;
        private float evaporateSpeed = 0.01f;
        private float gravity = 4f;
        private int maxDropletLifetime = 30;

        [MenuItem("Tools/Vastcore/Heightmap Terrain Generator")]
        public static void ShowWindow()
        {
            var window = GetWindow<HeightmapTerrainGeneratorWindow>("Heightmap Terrain");
            window.titleContent = new GUIContent("Heightmap Terrain");
        }

        private void OnEnable()
        {
            serializedObject = new SerializedObject(this);
            serializedLayers = serializedObject.FindProperty("heightmapLayers");
            serializedSplatmapRules = serializedObject.FindProperty("splatmapRules");
        }

        private void OnGUI()
        {
            scrollPos = EditorGUILayout.BeginScrollView(scrollPos);

            serializedObject.Update();

            GUILayout.Label("Heightmap Terrain Generator", EditorStyles.boldLabel);
            EditorGUILayout.Space();

            EditorGUILayout.LabelField("Heightmap Layers", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(serializedLayers, true);

            EditorGUILayout.Space();

            EditorGUILayout.LabelField("Texturing Rules", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(serializedSplatmapRules, true);

            EditorGUILayout.Space();

            EditorGUILayout.BeginVertical("box");
            GUILayout.Label("Global Settings", EditorStyles.boldLabel);
            terrainHeight = EditorGUILayout.FloatField("Max Terrain Height", terrainHeight);
            terrainSize = EditorGUILayout.Vector3Field("Terrain Size", terrainSize);
            EditorGUILayout.EndVertical();

            EditorGUILayout.Space();

            if (GUILayout.Button("Combine Heightmaps & Generate Preview"))
            {
                CombineHeightmaps();
            }
            
            DrawErosionSettings();
            
            if (GUILayout.Button("Generate Terrain GameObject"))
            {
                GenerateTerrain();
            }

            serializedObject.ApplyModifiedProperties();
            
            DrawTexturePreviews();

            EditorGUILayout.EndScrollView();
        }

        private void DrawErosionSettings()
        {
            EditorGUILayout.Space();
            showErosionSettings = EditorGUILayout.Foldout(showErosionSettings, "Hydraulic Erosion", true, EditorStyles.foldoutHeader);
            if (showErosionSettings)
            {
                EditorGUI.indentLevel++;
                erosionIterations = EditorGUILayout.IntSlider("Iterations", erosionIterations, 1, 250000);
                erosionInertia = EditorGUILayout.Slider("Inertia", erosionInertia, 0f, 1f);
                sedimentCapacityFactor = EditorGUILayout.Slider("Sediment Capacity", sedimentCapacityFactor, 1f, 10f);
                minSedimentCapacity = EditorGUILayout.Slider("Min Sediment Capacity", minSedimentCapacity, 0.001f, 0.1f);
                erosionSpeed = EditorGUILayout.Slider("Erosion Speed", erosionSpeed, 0f, 1f);
                depositSpeed = EditorGUILayout.Slider("Deposit Speed", depositSpeed, 0f, 1f);
                evaporateSpeed = EditorGUILayout.Slider("Evaporation Speed", evaporateSpeed, 0f, 0.1f);
                gravity = EditorGUILayout.Slider("Gravity", gravity, 1f, 8f);
                maxDropletLifetime = EditorGUILayout.IntSlider("Max Droplet Lifetime", maxDropletLifetime, 10, 50);

                if (GUILayout.Button("Apply Erosion to Preview"))
                {
                    if (combinedHeightmap != null)
                    {
                        SimulateHydraulicErosion();
                        GenerateTextureFromHeightmap(combinedHeightmap, ref combinedHeightmapTex);
                    }
                    else
                    {
                        EditorUtility.DisplayDialog("Error", "Please combine heightmaps first.", "OK");
                    }
                }
                EditorGUI.indentLevel--;
            }
        }
        
        private void DrawTexturePreviews()
        {
            if (combinedHeightmapTex != null)
            {
                GUILayout.Label("Combined Heightmap Preview", EditorStyles.boldLabel);
                EditorGUI.DrawPreviewTexture(EditorGUILayout.GetControlRect(false, 256), combinedHeightmapTex);
            }
        }

        private void CombineHeightmaps()
        {
            if (heightmapLayers == null || heightmapLayers.Count == 0 || heightmapLayers[0].heightMap == null)
            {
                EditorUtility.DisplayDialog("Error", "Please set at least one base heightmap.", "OK");
                return;
            }

            var baseLayer = heightmapLayers[0];
            int width = baseLayer.heightMap.width;
            int height = baseLayer.heightMap.height;

            combinedHeightmap = new float[height, width];
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    combinedHeightmap[y, x] = baseLayer.heightMap.GetPixel(x, y).grayscale * baseLayer.strength;
                }
            }

            for (int i = 1; i < heightmapLayers.Count; i++)
            {
                var layer = heightmapLayers[i];
                if (layer.heightMap == null) continue;

                for (int y = 0; y < height; y++)
                {
                    for (int x = 0; x < width; x++)
                    {
                        float currentHeight = combinedHeightmap[y, x];
                        float layerHeight = layer.heightMap.GetPixel(x, y).grayscale * layer.strength;

                        switch (layer.blendMode)
                        {
                            case HeightmapLayer.BlendMode.Add: currentHeight += layerHeight; break;
                            case HeightmapLayer.BlendMode.Subtract: currentHeight -= layerHeight; break;
                            case HeightmapLayer.BlendMode.Multiply: currentHeight *= layerHeight; break;
                            case HeightmapLayer.BlendMode.Average: currentHeight = (currentHeight + layerHeight) / 2f; break;
                        }
                        combinedHeightmap[y, x] = Mathf.Clamp01(currentHeight);
                    }
                }
            }
            
            GenerateTextureFromHeightmap(combinedHeightmap, ref combinedHeightmapTex);
            EditorUtility.DisplayDialog("Success", "Heightmaps combined successfully. Preview updated.", "OK");
        }
        
        private void GenerateTerrain()
        {
            if (combinedHeightmap == null)
            {
                EditorUtility.DisplayDialog("Error", "Please combine or generate a heightmap first.", "OK");
                return;
            }
            
            int width = combinedHeightmap.GetLength(1);
            int height = combinedHeightmap.GetLength(0);

            UnityEngine.TerrainData terrainData = new UnityEngine.TerrainData();
            terrainData.heightmapResolution = width;
            terrainData.size = new Vector3(terrainSize.x, terrainHeight, terrainSize.z);
            // using (LoadProfiler.Measure("TerrainData.SetHeights (EditorWindow)"))
            {
                // 大規模Terrainをバッチ処理で設定（メモリスパイク軽減）
                SetHeightsInBatches(terrainData, combinedHeightmap);
            }
            
            ApplySplatmap(terrainData);

            GameObject terrainObject = UnityEngine.Terrain.CreateTerrainGameObject(terrainData);
            Undo.RegisterCreatedObjectUndo(terrainObject, "Generate Terrain");

            Selection.activeGameObject = terrainObject;
            terrainObject.name = "Generated_Terrain";

            EditorUtility.DisplayDialog("Success", "Terrain generation complete.", "OK");
        }

        /// <summary>
        /// 高さマップをバッチ処理で設定（メモリスパイク軽減）
        /// </summary>
        private void SetHeightsInBatches(TerrainData terrainData, float[,] heights)
        {
            int height = heights.GetLength(0);
            int width = heights.GetLength(1);
            int batchSize = 256; // 256x256 のバッチサイズ

            for (int yStart = 0; yStart < height; yStart += batchSize)
            {
                for (int xStart = 0; xStart < width; xStart += batchSize)
                {
                    int yEnd = Mathf.Min(yStart + batchSize, height);
                    int xEnd = Mathf.Min(xStart + batchSize, width);
                    int batchHeight = yEnd - yStart;
                    int batchWidth = xEnd - xStart;

                    float[,] batchHeights = new float[batchHeight, batchWidth];
                    for (int y = 0; y < batchHeight; y++)
                    {
                        for (int x = 0; x < batchWidth; x++)
                        {
                            batchHeights[y, x] = heights[yStart + y, xStart + x];
                        }
                    }

                    terrainData.SetHeights(yStart, xStart, batchHeights);
                }
            }
        }

        private void SimulateHydraulicErosion()
        {
            int width = combinedHeightmap.GetLength(1);
            int height = combinedHeightmap.GetLength(0);
            float[,] map = (float[,])combinedHeightmap.Clone();

            for (int iteration = 0; iteration < erosionIterations; iteration++)
            {
                if (iteration % 1000 == 0)
                {
                    // This progress bar showing is an editor-only feature, so we wrap it
                    #if UNITY_EDITOR
                    if (EditorUtility.DisplayCancelableProgressBar("Eroding...", $"Iteration {iteration}/{erosionIterations}", (float)iteration / erosionIterations))
                        break;
                    #endif
                }
                
                float posX = Random.Range(0, width - 1);
                float posY = Random.Range(0, height - 1);
                float dirX = 0;
                float dirY = 0;
                float speed = 1;
                float water = 1;
                float sediment = 0;

                for (int lifetime = 0; lifetime < maxDropletLifetime; lifetime++)
                {
                    int nodeX = (int)posX;
                    int nodeY = (int)posY;
                    
                    if (nodeX < 0 || nodeX >= width - 1 || nodeY < 0 || nodeY >= height - 1) break;

                    (float gradX, float gradY) = CalculateGradient(map, nodeX, nodeY, width, height);

                    dirX = (dirX * erosionInertia) - gradX * (1 - erosionInertia);
                    dirY = (dirY * erosionInertia) - gradY * (1 - erosionInertia);

                    float len = Mathf.Max(0.01f, Mathf.Sqrt(dirX * dirX + dirY * dirY));
                    dirX /= len;
                    dirY /= len;

                    posX += dirX;
                    posY += dirY;

                    if (posX < 0 || posX >= width - 1 || posY < 0 || posY >= height - 1) break;

                    float newHeight = map[(int)posY, (int)posX];
                    float deltaHeight = newHeight - map[nodeY, nodeX];
                    
                    float sedimentCapacity = Mathf.Max(-deltaHeight * speed * water * sedimentCapacityFactor, minSedimentCapacity);

                    if (sediment > sedimentCapacity || deltaHeight > 0)
                    {
                        float amountToDeposit = (deltaHeight > 0) ? Mathf.Min(sediment, deltaHeight) : (sediment - sedimentCapacity) * depositSpeed;
                        sediment -= amountToDeposit;
                        map[nodeY, nodeX] += amountToDeposit;
                    }
                    else
                    {
                        float amountToErode = Mathf.Min((sedimentCapacity - sediment) * erosionSpeed, -deltaHeight);
                        sediment += amountToErode;
                        map[nodeY, nodeX] -= amountToErode;
                    }
                    
                    speed = Mathf.Sqrt(Mathf.Max(0, speed * speed + deltaHeight * gravity));
                    water *= (1 - evaporateSpeed);
                }
            }
            
            combinedHeightmap = map;
            #if UNITY_EDITOR
            EditorUtility.ClearProgressBar();
            #endif
        }

        private (float, float) CalculateGradient(float[,] map, int posX, int posY, int width, int height)
        {
            float height_x0 = map[posY, posX];
            float height_x1 = posX < width - 1 ? map[posY, posX + 1] : height_x0;
            float height_y0 = map[posY, posX];
            float height_y1 = posY < height - 1 ? map[posY + 1, posX] : height_y0;

            return (height_x1 - height_x0, height_y1 - height_y0);
        }

        private void GenerateTextureFromHeightmap(float[,] map, ref Texture2D texture)
        {
            int width = map.GetLength(1);
            int height = map.GetLength(0);

            if (texture == null || texture.width != width || texture.height != height)
            {
                texture = new Texture2D(width, height);
            }

            Color[] colors = new Color[width * height];
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    float h = map[y, x];
                    colors[y * width + x] = new Color(h, h, h);
                }
            }
            texture.SetPixels(colors);
            texture.Apply();
        }
        
        private void ApplySplatmap(TerrainData terrainData)
        {
            if (splatmapRules == null || splatmapRules.Count == 0) return;

            terrainData.terrainLayers = splatmapRules.Select(r => r.terrainLayer).ToArray();

            int alphaMapWidth = terrainData.alphamapWidth;
            int alphaMapHeight = terrainData.alphamapHeight;
            float[,,] alphaMap = new float[alphaMapWidth, alphaMapHeight, terrainData.alphamapLayers];

            for (int y = 0; y < alphaMapHeight; y++)
            {
                for (int x = 0; x < alphaMapWidth; x++)
                {
                    float normalizedX = (float)x / (alphaMapWidth - 1);
                    float normalizedY = (float)y / (alphaMapHeight - 1);

                    float height01 = terrainData.GetHeight(
                        Mathf.RoundToInt(normalizedY * terrainData.heightmapResolution),
                        Mathf.RoundToInt(normalizedX * terrainData.heightmapResolution));
                    float normalizedHeight = height01 / terrainData.size.y;
                    float slope = terrainData.GetSteepness(normalizedX, normalizedY);

                    float[] splatWeights = new float[terrainData.alphamapLayers];

                    for (int i = 0; i < splatmapRules.Count; i++)
                    {
                        var rule = splatmapRules[i];
                        if (normalizedHeight >= rule.minHeight && normalizedHeight <= rule.maxHeight &&
                            slope >= rule.minSlope && slope <= rule.maxSlope)
                        {
                            splatWeights[i] = 1f;
                        }
                    }

                    float sumOfWeights = splatWeights.Sum();
                    if (sumOfWeights > 0f)
                    {
                        for (int i = 0; i < terrainData.alphamapLayers; i++)
                        {
                            splatWeights[i] /= sumOfWeights;
                            alphaMap[y, x, i] = splatWeights[i];
                        }
                    }
                    else if (terrainData.alphamapLayers > 0)
                    {
                        // デフォルトで第0レイヤーに割り当て
                        alphaMap[y, x, 0] = 1f;
                    }
                }
            }

            // using (LoadProfiler.Measure("TerrainData.SetAlphamaps (EditorWindow)"))
            {
                // 大規模Terrainをバッチ処理で設定（メモリスパイク軽減）
                SetAlphamapsInBatches(terrainData, alphaMap);
            }
        }

        /// <summary>
        /// アルファマップをバッチ処理で設定（メモリスパイク軽減）
        /// </summary>
        private void SetAlphamapsInBatches(TerrainData terrainData, float[,,] alphaMap)
        {
            int height = alphaMap.GetLength(0);
            int width = alphaMap.GetLength(1);
            int layers = alphaMap.GetLength(2);
            int batchSize = 256;

            for (int yStart = 0; yStart < height; yStart += batchSize)
            {
                for (int xStart = 0; xStart < width; xStart += batchSize)
                {
                    int yEnd = Mathf.Min(yStart + batchSize, height);
                    int xEnd = Mathf.Min(xStart + batchSize, width);
                    int batchHeight = yEnd - yStart;
                    int batchWidth = xEnd - xStart;

                    float[,,] batchAlphaMap = new float[batchHeight, batchWidth, layers];
                    for (int y = 0; y < batchHeight; y++)
                    {
                        for (int x = 0; x < batchWidth; x++)
                        {
                            for (int l = 0; l < layers; l++)
                            {
                                batchAlphaMap[y, x, l] = alphaMap[yStart + y, xStart + x, l];
                            }
                        }
                    }

                    terrainData.SetAlphamaps(yStart, xStart, batchAlphaMap);
                }
            }
        }
    }
}