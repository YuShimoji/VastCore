using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

namespace Vastcore.Editor.Generation
{
    /// <summary>
    /// 全てのタブで共有されるグローバル設定を管理するタブ
    /// </summary>
    public class GlobalSettingsTab : IStructureTab
    {
        public TabCategory Category => TabCategory.Settings;
        public string DisplayName => "⚙️ Global Settings";
        public string Description => "プロジェクト全体の設定を一元管理します。";
        public bool SupportsRealTimeUpdate => true;

        private StructureGeneratorWindow _parent;

        // --- 設定項目 ---
        public List<Material> MaterialPalette { get; private set; } = new List<Material>();
        public int SelectedMaterialIndex { get; set; } = 0;
        public Vector3 DefaultSpawnPosition { get; set; } = Vector3.zero;
        public float GlobalStructureScale { get; set; } = 5f;
        
        // --- UI用変数 ---
        private Vector2 _scrollPosition;

        public GlobalSettingsTab(StructureGeneratorWindow parent)
        {
            _parent = parent;
            // TODO: 設定のロード/保存機能を追加
        }

        public void DrawGUI()
        {
            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);

            EditorGUILayout.LabelField(DisplayName, EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(Description, MessageType.Info);
            
            DrawMaterialSettings();
            DrawGenerationSettings();

            EditorGUILayout.EndScrollView();
        }
        
        private void DrawMaterialSettings()
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("マテリアル設定", EditorStyles.boldLabel);
            
            // マテリアルパレットのUI
            for (int i = 0; i < MaterialPalette.Count; i++)
            {
                EditorGUILayout.BeginHorizontal();
                MaterialPalette[i] = (Material)EditorGUILayout.ObjectField($"マテリアル {i+1}", MaterialPalette[i], typeof(Material), false);
                if (GUILayout.Button("削除", GUILayout.Width(50)))
                {
                    MaterialPalette.RemoveAt(i);
                    i--; // インデックス調整
                }
                EditorGUILayout.EndHorizontal();
            }

            if (GUILayout.Button("マテリアルを追加"))
            {
                MaterialPalette.Add(null);
            }
        }
        
        private void DrawGenerationSettings()
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("生成設定", EditorStyles.boldLabel);

            DefaultSpawnPosition = EditorGUILayout.Vector3Field("デフォルト生成位置", DefaultSpawnPosition);
            GlobalStructureScale = EditorGUILayout.Slider("グローバルスケール", GlobalStructureScale, 0.1f, 50f);
        }

        public void OnSceneGUI()
        {
            // シーンビューでの設定は不要
        }

        public void HandleRealTimeUpdate() { }

        public void ProcessSelectedObjects()
        {
            // このタブはオブジェクトを直接処理しない
        }

        public void OnTabSelected() { }
        public void OnTabDeselected() { }
    }
} 