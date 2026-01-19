using UnityEngine;
using UnityEditor;
using Vastcore.Generation;

namespace Vastcore.Editor
{
    [CustomEditor(typeof(TerrainGenerator))]
    public class TerrainGeneratorEditor : UnityEditor.Editor
    {
    private bool m_ShowHeightMapSettings = true;
    private bool m_ShowNoiseSettings = true;
    private bool m_ShowTerrainSettings = true;
    private bool m_ShowTextureSettings = true;
    private bool m_ShowDetailSettings = true;
    private bool m_ShowTreeSettings = true;

    // Serialized Properties (use backing field names)
    private SerializedProperty sp_TerrainLayers;
    private SerializedProperty sp_TextureBlendFactors;
    private SerializedProperty sp_TextureTiling;
    private SerializedProperty sp_DetailPrototypes;
    private SerializedProperty sp_DetailResolution;
    private SerializedProperty sp_DetailResolutionPerPatch;
    private SerializedProperty sp_DetailDensity;
    private SerializedProperty sp_DetailDistance;
    private SerializedProperty sp_TreePrototypes;
    private SerializedProperty sp_TreeDistance;
    private SerializedProperty sp_TreeBillboardDistance;
    private SerializedProperty sp_TreeCrossFadeLength;
    private SerializedProperty sp_TreeMaximumFullLODCount;

    private void OnEnable()
    {
        sp_TerrainLayers = serializedObject.FindProperty("m_TerrainLayers");
        sp_TextureBlendFactors = serializedObject.FindProperty("m_TextureBlendFactors");
        sp_TextureTiling = serializedObject.FindProperty("m_TextureTiling");
        sp_DetailPrototypes = serializedObject.FindProperty("m_DetailPrototypes");
        sp_DetailResolution = serializedObject.FindProperty("m_DetailResolution");
        sp_DetailResolutionPerPatch = serializedObject.FindProperty("m_DetailResolutionPerPatch");
        sp_DetailDensity = serializedObject.FindProperty("m_DetailDensity");
        sp_DetailDistance = serializedObject.FindProperty("m_DetailDistance");
        sp_TreePrototypes = serializedObject.FindProperty("m_TreePrototypes");
        sp_TreeDistance = serializedObject.FindProperty("m_TreeDistance");
        sp_TreeBillboardDistance = serializedObject.FindProperty("m_TreeBillboardDistance");
        sp_TreeCrossFadeLength = serializedObject.FindProperty("m_TreeCrossFadeLength");
        sp_TreeMaximumFullLODCount = serializedObject.FindProperty("m_TreeMaximumFullLODCount");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        var generator = (TerrainGenerator)target;

        // 生成モードの選択
        EditorGUILayout.Space();
        var generationMode = (TerrainGenerationMode)EditorGUILayout.EnumPopup("Generation Mode", generator.GenerationMode);
        if (generationMode != generator.GenerationMode)
        {
            generator.GenerationMode = generationMode;
            EditorUtility.SetDirty(generator);
        }
        
        // 地形設定
        m_ShowTerrainSettings = EditorGUILayout.Foldout(m_ShowTerrainSettings, "Terrain Settings", true);
        if (m_ShowTerrainSettings)
        {
            EditorGUI.indentLevel++;
            generator.Width = EditorGUILayout.IntField("Width", generator.Width);
            generator.Height = EditorGUILayout.IntField("Height", generator.Height);
            generator.Depth = EditorGUILayout.IntField("Depth", generator.Depth);
            generator.Resolution = EditorGUILayout.IntField("Resolution", generator.Resolution);
            generator.TerrainMaterial = (Material)EditorGUILayout.ObjectField("Terrain Material", generator.TerrainMaterial, typeof(Material), false);
            EditorGUI.indentLevel--;
        }

        // ハイトマップ設定
        if (generator.GenerationMode != TerrainGenerationMode.Noise)
        {
            m_ShowHeightMapSettings = EditorGUILayout.Foldout(m_ShowHeightMapSettings, "Height Map Settings", true);
            if (m_ShowHeightMapSettings)
            {
                EditorGUI.indentLevel++;
                generator.HeightMap = (Texture2D)EditorGUILayout.ObjectField("Height Map", generator.HeightMap, typeof(Texture2D), false);
                generator.HeightMapScale = EditorGUILayout.FloatField("Height Scale", generator.HeightMapScale);
                generator.HeightMapOffset = EditorGUILayout.FloatField("Height Offset", generator.HeightMapOffset);
                generator.FlipHeightMapVertically = EditorGUILayout.Toggle("Flip Vertically", generator.FlipHeightMapVertically);
                
                if (generator.HeightMap != null)
                {
                    EditorGUILayout.HelpBox($"Height Map Size: {generator.HeightMap.width}x{generator.HeightMap.height}", MessageType.Info);
                }
                else
                {
                    EditorGUILayout.HelpBox("Please assign a Height Map texture.", MessageType.Warning);
                }
                EditorGUI.indentLevel--;
            }
        }

        // ノイズ設定
        if (generator.GenerationMode != TerrainGenerationMode.HeightMap)
        {
            m_ShowNoiseSettings = EditorGUILayout.Foldout(m_ShowNoiseSettings, "Noise Settings", true);
            if (m_ShowNoiseSettings)
            {
                EditorGUI.indentLevel++;
                generator.Scale = EditorGUILayout.FloatField("Scale", generator.Scale);
                generator.Octaves = EditorGUILayout.IntSlider("Octaves", generator.Octaves, 1, 10);
                generator.Persistence = EditorGUILayout.Slider("Persistence", generator.Persistence, 0f, 1f);
                generator.Lacunarity = EditorGUILayout.FloatField("Lacunarity", generator.Lacunarity);
                generator.Offset = EditorGUILayout.Vector2Field("Offset", generator.Offset);
                EditorGUI.indentLevel--;
            }
        }

        // テクスチャ設定
        m_ShowTextureSettings = EditorGUILayout.Foldout(m_ShowTextureSettings, "Texture Settings", true);
        if (m_ShowTextureSettings)
        {
            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(sp_TerrainLayers, new GUIContent("Terrain Layers"), true);
            EditorGUILayout.PropertyField(sp_TextureBlendFactors, new GUIContent("Texture Blend Factors"), true);
            EditorGUILayout.PropertyField(sp_TextureTiling, new GUIContent("Texture Tiling"), true);
            EditorGUI.indentLevel--;
        }

        // ディテール設定
        m_ShowDetailSettings = EditorGUILayout.Foldout(m_ShowDetailSettings, "Detail Settings", true);
        if (m_ShowDetailSettings)
        {
            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(sp_DetailPrototypes, new GUIContent("Detail Prototypes"), true);
            EditorGUILayout.PropertyField(sp_DetailResolution, new GUIContent("Detail Resolution"));
            EditorGUILayout.PropertyField(sp_DetailResolutionPerPatch, new GUIContent("Detail Resolution Per Patch"));
            EditorGUILayout.PropertyField(sp_DetailDensity, new GUIContent("Detail Density"));
            EditorGUILayout.PropertyField(sp_DetailDistance, new GUIContent("Detail Distance"));
            EditorGUI.indentLevel--;
        }

        // ツリー設定
        m_ShowTreeSettings = EditorGUILayout.Foldout(m_ShowTreeSettings, "Tree Settings", true);
        if (m_ShowTreeSettings)
        {
            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(sp_TreePrototypes, new GUIContent("Tree Prototypes"), true);
            EditorGUILayout.PropertyField(sp_TreeDistance, new GUIContent("Tree Distance"));
            EditorGUILayout.PropertyField(sp_TreeBillboardDistance, new GUIContent("Tree Billboard Distance"));
            EditorGUILayout.PropertyField(sp_TreeCrossFadeLength, new GUIContent("Tree Cross Fade Length"));
            EditorGUILayout.PropertyField(sp_TreeMaximumFullLODCount, new GUIContent("Tree Maximum Full LOD Count"));
            EditorGUI.indentLevel--;
        }

        // 生成ボタン
        EditorGUILayout.Space(10);
        if (GUILayout.Button("Generate Terrain"))
        {
            generator.StartCoroutine(generator.GenerateTerrain());
        }

            // 変更を適用
            if (GUI.changed)
            {
                EditorUtility.SetDirty(generator);
            }

            serializedObject.ApplyModifiedProperties();
        }
    }
}
