using UnityEngine;
using UnityEditor;
using Vastcore.Terrain.Map;

namespace Vastcore.Editor.StructureGenerator.Map
{
    /// <summary>
    /// BiomePresetManagerのエディタ拡張
    /// </summary>
    [CustomEditor(typeof(BiomePresetManager))]
    public class BiomePresetManagerEditor : UnityEditor.Editor
    {
        /// <summary>
        /// エディタでプリセットを作成
        /// </summary>
        [MenuItem("Vastcore/Create New Biome Preset")]
        public static void CreateNewPresetInEditor()
        {
            var preset = ScriptableObject.CreateInstance<BiomePreset>();
            preset.InitializeDefault();
            
            // ディレクトリが存在するか確認し、なければ作成
            if (!AssetDatabase.IsValidFolder("Assets/Data"))
            {
                AssetDatabase.CreateFolder("Assets", "Data");
            }
            
            if (!AssetDatabase.IsValidFolder("Assets/Data/BiomePresets"))
            {
                AssetDatabase.CreateFolder("Assets/Data", "BiomePresets");
            }
            
            string path = AssetDatabase.GenerateUniqueAssetPath("Assets/Data/BiomePresets/NewBiomePreset.asset");
            AssetDatabase.CreateAsset(preset, path);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            
            Selection.activeObject = preset;
            EditorGUIUtility.PingObject(preset);
            
            Debug.Log($"新しいバイオームプリセットを作成しました: {path}");
        }
        
        /// <summary>
        /// インスペクターの表示
        /// </summary>
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            
            BiomePresetManager manager = (BiomePresetManager)target;
            
            if (GUILayout.Button("デフォルトプリセットを作成"))
            {
                manager.CreateDefaultPresets();
            }
            
            if (GUILayout.Button("プリセット一覧を更新"))
            {
                manager.RefreshAvailablePresets();
            }
        }
    }
}
