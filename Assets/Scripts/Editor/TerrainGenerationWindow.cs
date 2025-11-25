using UnityEditor;
using UnityEngine;
using Vastcore.Generation;

namespace Vastcore.Editor.Terrain
{
    /// <summary>
    /// Terrain Generation (v0) - HeightMap / Noise ベースの単一タイル地形生成ウィンドウ
    /// TerrainGenerationV0_Spec.md に基づく設計
    /// </summary>
    public class TerrainGenerationWindow : EditorWindow
    {
        #region Window State
        private Vector2 scrollPosition;
        
        // Foldout states
        private bool showContextSection = true;
        private bool showGenerationModeSection = true;
        private bool showSizeSection = true;
        private bool showHeightMapSection = true;
        private bool showNoiseSection = true;
        private bool showProfileSection = true;
        private bool showActionsSection = true;
        #endregion

        #region Context
        private UnityEngine.Terrain targetTerrain;
        #endregion

        #region Generation Mode
        private TerrainGenerator.TerrainGenerationMode generationMode = TerrainGenerator.TerrainGenerationMode.Noise;
        #endregion

        #region Terrain Size & Resolution
        private float terrainWidth = TerrainGenerationConstants.DefaultTerrainWidth;
        private float terrainLength = TerrainGenerationConstants.DefaultTerrainHeight;
        private float terrainHeight = TerrainGenerationConstants.DefaultTerrainDepth;
        private int heightmapResolution = TerrainGenerationConstants.DefaultHeightmapResolution;
        #endregion

        #region HeightMap Settings
        private Texture2D heightMapTexture;
        private HeightMapChannel heightMapChannel = HeightMapChannel.Luminance;
        private float heightScale = TerrainGenerationConstants.DefaultHeightMapScale;
        private Vector2 uvOffset = Vector2.zero;
        private Vector2 uvTiling = Vector2.one;
        private bool invertHeight = false;
        #endregion

        #region Noise Settings
        private int seed = 0;
        private float noiseScale = TerrainGenerationConstants.DefaultNoiseScale;
        private int octaves = TerrainGenerationConstants.DefaultOctaves;
        private float persistence = TerrainGenerationConstants.DefaultPersistence;
        private float lacunarity = TerrainGenerationConstants.DefaultLacunarity;
        private Vector2 noiseOffset = Vector2.zero;
        #endregion

        #region Profile
        private TerrainGenerationProfile currentProfile;
        #endregion

        #region Menu
        [MenuItem("Tools/Vastcore/Terrain/Terrain Generation (v0)")]
        public static void ShowWindow()
        {
            var window = GetWindow<TerrainGenerationWindow>("Terrain Generation (v0)");
            window.minSize = new Vector2(350, 500);
        }
        #endregion

        #region GUI
        private void OnGUI()
        {
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

            DrawHeader();
            EditorGUILayout.Space(10);

            DrawContextSection();
            DrawGenerationModeSection();
            DrawSizeSection();
            DrawHeightMapSection();
            DrawNoiseSection();
            DrawProfileSection();
            DrawActionsSection();

            EditorGUILayout.EndScrollView();
        }

        private void DrawHeader()
        {
            EditorGUILayout.LabelField("Terrain Generation (v0)", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "HeightMap / Noise ベースの単一タイル地形生成ツール。\n" +
                "Profile を使用して設定を保存・読み込みできます。",
                MessageType.Info);
        }

        private void DrawContextSection()
        {
            showContextSection = EditorGUILayout.BeginFoldoutHeaderGroup(showContextSection, "Context");
            if (showContextSection)
            {
                EditorGUI.indentLevel++;
                
                targetTerrain = (UnityEngine.Terrain)EditorGUILayout.ObjectField(
                    "Target Terrain", 
                    targetTerrain, 
                    typeof(UnityEngine.Terrain), 
                    true);

                EditorGUILayout.BeginHorizontal();
                GUILayout.FlexibleSpace();
                if (GUILayout.Button("Create New Terrain", GUILayout.Width(150)))
                {
                    CreateNewTerrain();
                }
                EditorGUILayout.EndHorizontal();

                EditorGUI.indentLevel--;
            }
            EditorGUILayout.EndFoldoutHeaderGroup();
            EditorGUILayout.Space(5);
        }

        private void DrawGenerationModeSection()
        {
            showGenerationModeSection = EditorGUILayout.BeginFoldoutHeaderGroup(showGenerationModeSection, "Generation Mode");
            if (showGenerationModeSection)
            {
                EditorGUI.indentLevel++;
                
                generationMode = (TerrainGenerator.TerrainGenerationMode)EditorGUILayout.EnumPopup(
                    "Mode", 
                    generationMode);

                string modeDescription = generationMode switch
                {
                    TerrainGenerator.TerrainGenerationMode.Noise => "Perlin Noise のみで地形を生成",
                    TerrainGenerator.TerrainGenerationMode.HeightMap => "HeightMap テクスチャのみで地形を生成",
                    TerrainGenerator.TerrainGenerationMode.NoiseAndHeightMap => "HeightMap と Noise を組み合わせて生成",
                    _ => ""
                };
                EditorGUILayout.HelpBox(modeDescription, MessageType.None);

                EditorGUI.indentLevel--;
            }
            EditorGUILayout.EndFoldoutHeaderGroup();
            EditorGUILayout.Space(5);
        }

        private void DrawSizeSection()
        {
            showSizeSection = EditorGUILayout.BeginFoldoutHeaderGroup(showSizeSection, "Terrain Size & Resolution");
            if (showSizeSection)
            {
                EditorGUI.indentLevel++;

                terrainWidth = EditorGUILayout.FloatField("Width (m)", terrainWidth);
                terrainLength = EditorGUILayout.FloatField("Length (m)", terrainLength);
                terrainHeight = EditorGUILayout.FloatField("Height (m)", terrainHeight);

                EditorGUILayout.Space(3);

                // Resolution dropdown with common values
                string[] resolutionOptions = { "33", "65", "129", "257", "513", "1025", "2049" };
                int[] resolutionValues = { 33, 65, 129, 257, 513, 1025, 2049 };
                int currentIndex = System.Array.IndexOf(resolutionValues, heightmapResolution);
                if (currentIndex < 0) currentIndex = 3; // Default to 257

                int newIndex = EditorGUILayout.Popup("Heightmap Resolution", currentIndex, resolutionOptions);
                heightmapResolution = resolutionValues[newIndex];

                EditorGUI.indentLevel--;
            }
            EditorGUILayout.EndFoldoutHeaderGroup();
            EditorGUILayout.Space(5);
        }

        private void DrawHeightMapSection()
        {
            bool showSection = generationMode == TerrainGenerator.TerrainGenerationMode.HeightMap ||
                               generationMode == TerrainGenerator.TerrainGenerationMode.NoiseAndHeightMap;

            GUI.enabled = showSection;
            showHeightMapSection = EditorGUILayout.BeginFoldoutHeaderGroup(showHeightMapSection, "HeightMap Settings");
            GUI.enabled = true;

            if (showHeightMapSection && showSection)
            {
                EditorGUI.indentLevel++;

                heightMapTexture = (Texture2D)EditorGUILayout.ObjectField(
                    "HeightMap Texture",
                    heightMapTexture,
                    typeof(Texture2D),
                    false);

                heightMapChannel = (HeightMapChannel)EditorGUILayout.EnumPopup("Channel", heightMapChannel);
                heightScale = EditorGUILayout.Slider("Height Scale", heightScale, 0f, 5f);
                
                EditorGUILayout.Space(3);
                uvOffset = EditorGUILayout.Vector2Field("UV Offset", uvOffset);
                uvTiling = EditorGUILayout.Vector2Field("UV Tiling", uvTiling);
                invertHeight = EditorGUILayout.Toggle("Invert Height", invertHeight);

                EditorGUI.indentLevel--;
            }
            EditorGUILayout.EndFoldoutHeaderGroup();
            EditorGUILayout.Space(5);
        }

        private void DrawNoiseSection()
        {
            bool showSection = generationMode == TerrainGenerator.TerrainGenerationMode.Noise ||
                               generationMode == TerrainGenerator.TerrainGenerationMode.NoiseAndHeightMap;

            GUI.enabled = showSection;
            showNoiseSection = EditorGUILayout.BeginFoldoutHeaderGroup(showNoiseSection, "Noise Settings");
            GUI.enabled = true;

            if (showNoiseSection && showSection)
            {
                EditorGUI.indentLevel++;

                EditorGUILayout.BeginHorizontal();
                seed = EditorGUILayout.IntField("Seed", seed);
                if (GUILayout.Button("Randomize", GUILayout.Width(80)))
                {
                    seed = Random.Range(int.MinValue, int.MaxValue);
                }
                EditorGUILayout.EndHorizontal();

                noiseScale = EditorGUILayout.Slider("Scale", noiseScale, 1f, 1000f);
                octaves = EditorGUILayout.IntSlider("Octaves", octaves, 1, 8);
                persistence = EditorGUILayout.Slider("Persistence", persistence, 0f, 1f);
                lacunarity = EditorGUILayout.Slider("Lacunarity", lacunarity, 1f, 4f);
                
                EditorGUILayout.Space(3);
                noiseOffset = EditorGUILayout.Vector2Field("Offset", noiseOffset);

                EditorGUI.indentLevel--;
            }
            EditorGUILayout.EndFoldoutHeaderGroup();
            EditorGUILayout.Space(5);
        }

        private void DrawProfileSection()
        {
            showProfileSection = EditorGUILayout.BeginFoldoutHeaderGroup(showProfileSection, "Profile");
            if (showProfileSection)
            {
                EditorGUI.indentLevel++;

                currentProfile = (TerrainGenerationProfile)EditorGUILayout.ObjectField(
                    "Generation Profile",
                    currentProfile,
                    typeof(TerrainGenerationProfile),
                    false);

                EditorGUILayout.BeginHorizontal();
                
                GUI.enabled = currentProfile != null;
                if (GUILayout.Button("Load From Profile"))
                {
                    LoadFromProfile();
                }
                if (GUILayout.Button("Save To Profile"))
                {
                    SaveToProfile();
                }
                GUI.enabled = true;

                EditorGUILayout.EndHorizontal();

                if (GUILayout.Button("Create New Profile"))
                {
                    CreateNewProfile();
                }

                EditorGUI.indentLevel--;
            }
            EditorGUILayout.EndFoldoutHeaderGroup();
            EditorGUILayout.Space(5);
        }

        private void DrawActionsSection()
        {
            showActionsSection = EditorGUILayout.BeginFoldoutHeaderGroup(showActionsSection, "Actions");
            if (showActionsSection)
            {
                EditorGUILayout.Space(5);

                EditorGUILayout.BeginHorizontal();
                
                GUI.backgroundColor = new Color(0.4f, 0.8f, 0.4f);
                if (GUILayout.Button("Generate Preview", GUILayout.Height(30)))
                {
                    GeneratePreview();
                }
                GUI.backgroundColor = Color.white;

                GUI.backgroundColor = new Color(0.8f, 0.4f, 0.4f);
                if (GUILayout.Button("Clear Terrain", GUILayout.Height(30)))
                {
                    ClearTerrain();
                }
                GUI.backgroundColor = Color.white;

                EditorGUILayout.EndHorizontal();
            }
            EditorGUILayout.EndFoldoutHeaderGroup();
        }
        #endregion

        #region Actions
        private void CreateNewTerrain()
        {
            // Create TerrainData
            TerrainData terrainData = new TerrainData();
            terrainData.heightmapResolution = heightmapResolution;
            terrainData.size = new Vector3(terrainWidth, terrainHeight, terrainLength);

            // Create Terrain GameObject
            GameObject terrainObject = UnityEngine.Terrain.CreateTerrainGameObject(terrainData);
            terrainObject.name = "Vastcore_Terrain";
            
            // Save TerrainData as asset
            string path = EditorUtility.SaveFilePanelInProject(
                "Save Terrain Data",
                "NewTerrainData",
                "asset",
                "Save terrain data asset");

            if (!string.IsNullOrEmpty(path))
            {
                AssetDatabase.CreateAsset(terrainData, path);
                AssetDatabase.SaveAssets();
            }

            targetTerrain = terrainObject.GetComponent<UnityEngine.Terrain>();
            Selection.activeGameObject = terrainObject;

            Debug.Log("[TerrainGenerationWindow] Created new terrain.");
        }

        private void GeneratePreview()
        {
            if (targetTerrain == null)
            {
                EditorUtility.DisplayDialog("Error", "Please select or create a Target Terrain first.", "OK");
                return;
            }

            if (generationMode != TerrainGenerator.TerrainGenerationMode.Noise && heightMapTexture == null)
            {
                EditorUtility.DisplayDialog("Error", "HeightMap Texture is required for this generation mode.", "OK");
                return;
            }

            // Find or create TerrainGenerator
            TerrainGenerator generator = targetTerrain.GetComponent<TerrainGenerator>();
            if (generator == null)
            {
                generator = targetTerrain.gameObject.AddComponent<TerrainGenerator>();
            }

            // Apply settings to generator
            ApplySettingsToGenerator(generator);

            // Generate terrain
            EditorApplication.delayCall += () =>
            {
                var enumerator = generator.GenerateTerrain();
                while (enumerator.MoveNext()) { }
                Debug.Log("[TerrainGenerationWindow] Terrain generation completed.");
            };
        }

        private void ClearTerrain()
        {
            if (targetTerrain == null)
            {
                EditorUtility.DisplayDialog("Error", "No Target Terrain selected.", "OK");
                return;
            }

            if (!EditorUtility.DisplayDialog("Confirm", "Clear the terrain heightmap to flat?", "Yes", "Cancel"))
            {
                return;
            }

            TerrainData terrainData = targetTerrain.terrainData;
            int resolution = terrainData.heightmapResolution;
            float[,] heights = new float[resolution, resolution];
            
            // Set all heights to 0
            terrainData.SetHeights(0, 0, heights);

            Debug.Log("[TerrainGenerationWindow] Terrain cleared.");
        }

        private void ApplySettingsToGenerator(TerrainGenerator generator)
        {
            generator.GenerationMode = generationMode;
            generator.Width = (int)terrainWidth;
            generator.Height = (int)terrainLength;
            generator.Depth = (int)terrainHeight;
            generator.Resolution = heightmapResolution;

            generator.HeightMap = heightMapTexture;
            generator.HeightMapScale = heightScale;

            generator.Scale = noiseScale;
            generator.Octaves = octaves;
            generator.Persistence = persistence;
            generator.Lacunarity = lacunarity;
            generator.Offset = noiseOffset;
        }

        private void LoadFromProfile()
        {
            if (currentProfile == null) return;

            generationMode = currentProfile.GenerationMode;
            terrainWidth = currentProfile.TerrainWidth;
            terrainLength = currentProfile.TerrainLength;
            terrainHeight = currentProfile.TerrainHeight;
            heightmapResolution = currentProfile.HeightmapResolution;

            heightMapTexture = currentProfile.HeightMapTexture;
            heightMapChannel = currentProfile.HeightMapChannel;
            heightScale = currentProfile.HeightScale;
            uvOffset = currentProfile.UVOffset;
            uvTiling = currentProfile.UVTiling;
            invertHeight = currentProfile.InvertHeight;

            seed = currentProfile.Seed;
            noiseScale = currentProfile.NoiseScale;
            octaves = currentProfile.Octaves;
            persistence = currentProfile.Persistence;
            lacunarity = currentProfile.Lacunarity;
            noiseOffset = currentProfile.NoiseOffset;

            Debug.Log($"[TerrainGenerationWindow] Loaded settings from profile: {currentProfile.name}");
            Repaint();
        }

        private void SaveToProfile()
        {
            if (currentProfile == null) return;

            currentProfile.GenerationMode = generationMode;
            currentProfile.TerrainWidth = terrainWidth;
            currentProfile.TerrainLength = terrainLength;
            currentProfile.TerrainHeight = terrainHeight;
            currentProfile.HeightmapResolution = heightmapResolution;

            currentProfile.HeightMapTexture = heightMapTexture;
            currentProfile.HeightMapChannel = heightMapChannel;
            currentProfile.HeightScale = heightScale;
            currentProfile.UVOffset = uvOffset;
            currentProfile.UVTiling = uvTiling;
            currentProfile.InvertHeight = invertHeight;

            currentProfile.Seed = seed;
            currentProfile.NoiseScale = noiseScale;
            currentProfile.Octaves = octaves;
            currentProfile.Persistence = persistence;
            currentProfile.Lacunarity = lacunarity;
            currentProfile.NoiseOffset = noiseOffset;

            EditorUtility.SetDirty(currentProfile);
            AssetDatabase.SaveAssets();

            Debug.Log($"[TerrainGenerationWindow] Saved settings to profile: {currentProfile.name}");
        }

        private void CreateNewProfile()
        {
            string path = EditorUtility.SaveFilePanelInProject(
                "Create Terrain Generation Profile",
                "NewTerrainGenerationProfile",
                "asset",
                "Create a new terrain generation profile");

            if (string.IsNullOrEmpty(path)) return;

            TerrainGenerationProfile newProfile = CreateInstance<TerrainGenerationProfile>();
            AssetDatabase.CreateAsset(newProfile, path);
            AssetDatabase.SaveAssets();

            currentProfile = newProfile;
            SaveToProfile();

            Selection.activeObject = newProfile;
            Debug.Log($"[TerrainGenerationWindow] Created new profile: {path}");
        }
        #endregion
    }
}
