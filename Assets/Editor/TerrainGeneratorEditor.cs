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

    public override void OnInspectorGUI()
    {
        var generator = (TerrainGenerator)target;

        // 生成モードの選択
        EditorGUILayout.Space();
        var generationMode = (TerrainGenerator.TerrainGenerationMode)EditorGUILayout.EnumPopup("Generation Mode", generator.GenerationMode);
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
        if (generator.GenerationMode != TerrainGenerator.TerrainGenerationMode.Noise)
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
        if (generator.GenerationMode != TerrainGenerator.TerrainGenerationMode.HeightMap)
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
        }
    }
}
