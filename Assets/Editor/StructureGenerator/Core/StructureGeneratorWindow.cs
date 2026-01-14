/*
 * =================================================================================================
 * PROJECT: Vastcore 構造ジェネレータ
 * -------------------------------------------------------------------------------------------------
 * 目的:
 * 広大な自然景観の中に配置する、ミニマルかつ巨大な人工構造物をプロシージャルに生成するための
 * Unityカスタムエディタウィンドウ。単品の巨大建築物や、部屋と廊下で構成される線形構造物を
 * 生成し、CSG演算による加工をサポートする。
 * =================================================================================================
 */

using UnityEditor;
using UnityEngine;
using UnityEngine.ProBuilder;
using UnityEditor.ProBuilder;
using System.Linq;
using System.Collections.Generic;
using UnityEditor.Compilation;
using System.Collections;
using Vastcore.Editor.StructureGenerator.Tabs;

namespace Vastcore.Editor.Generation
{
    public class StructureGeneratorWindow : EditorWindow
    {
        private int _selectedTab = 0;
        public List<IStructureTab> _tabs = new List<IStructureTab>(); // デバッグ用にpublicに変更
        private string[] _tabNames;
        
        // グローバル設定タブへの参照
        public GlobalSettingsTab GlobalSettings { get; private set; }

        [MenuItem("Tools/Vastcore/Structure Generator")]
        public static void ShowWindow()
        {
            GetWindow<StructureGeneratorWindow>("Vastcore Generator");
        }

        private void OnEnable()
        {
            // タブの初期化とリストへの追加
            _tabs = new List<IStructureTab>(); // リストを明示的にクリア
            GlobalSettings = new GlobalSettingsTab(this);
            
            _tabs.Add(GlobalSettings);
            _tabs.Add(new BasicStructureTab(this));
            _tabs.Add(new AdvancedStructureTab(this));
            // _tabs.Add(new OperationsTab(this)); // 一時的にコメントアウト
            _tabs.Add(new RelationshipTab(this));
            _tabs.Add(new ParticleDistributionTab(this));
            _tabs.Add(new DeformerTab(this)); // Deformタブを追加
            _tabs.Add(new CompositionTab(this)); // CT-1: スケルトン実装完了
            _tabs.Add(new RandomControlTab(this)); // Phase 1: 基本版を有効化
        
            // タブ名配列を動的に生成
            _tabNames = _tabs.Select(tab => tab.DisplayName).ToArray();
        }
        
        private void OnGUI()
        {
            if (_tabs == null || _tabs.Count == 0)
            {
                OnEnable(); // 再初期化
            }

            int newSelectedTab = GUILayout.Toolbar(_selectedTab, _tabNames);
            if (newSelectedTab != _selectedTab)
            {
                if (_selectedTab >= 0 && _selectedTab < _tabs.Count)
                    _tabs[_selectedTab].OnTabDeselected();
                _selectedTab = newSelectedTab;
                if (_selectedTab >= 0 && _selectedTab < _tabs.Count)
                    _tabs[_selectedTab].OnTabSelected();
            }
            
            EditorGUILayout.Space();

            // 選択されたタブの描画
            if (_selectedTab >= 0 && _selectedTab < _tabs.Count)
            {
                _tabs[_selectedTab]?.DrawGUI();
            }
        }
        
        private void OnSceneGUI(SceneView sceneView)
        {
            // 現在選択されているタブのOnSceneGUIを呼び出す
            _tabs[_selectedTab]?.OnSceneGUI();
        }
        
        private void OnFocus()
        {
            SceneView.duringSceneGui -= OnSceneGUI;
            SceneView.duringSceneGui += OnSceneGUI;
        }
        
        private void OnDestroy()
        {
            SceneView.duringSceneGui -= OnSceneGUI;
        }

        // ApplyMaterialはGlobalSettingsTabが提供するマテリアルを使用するように変更
        public void ApplyMaterial(ProBuilderMesh pb)
        {
            if (GlobalSettings == null || GlobalSettings.MaterialPalette.Count == 0)
            {
                Debug.LogWarning("マテリアルが設定されていません。'Global Settings' タブでマテリアルパレットに追加してください。");
                return;
            }

            var materialToApply = GlobalSettings.MaterialPalette[GlobalSettings.SelectedMaterialIndex];
            if (materialToApply != null)
            {
                var renderer = pb.GetComponent<MeshRenderer>();
                if (renderer != null)
                {
                    renderer.sharedMaterial = materialToApply;
                }
                pb.Refresh();
            }
            else
            {
                Debug.LogWarning($"選択されたマテリアル (インデックス: {GlobalSettings.SelectedMaterialIndex}) がnullです。");
            }
        }
        
        // 外部からタブを選択するためのメソッド
        public void SelectTab(int index)
        {
            if (index >= 0 && index < _tabs.Count)
            {
                _selectedTab = index;
        }
        }

        // ProBuilder CSG有効化機能は変更なし
        [MenuItem("Tools/Vastcore/Enable ProBuilder CSG")]
        public static void EnableProBuilderCSG()
        {
            var target = UnityEditor.Build.NamedBuildTarget.FromBuildTargetGroup(EditorUserBuildSettings.selectedBuildTargetGroup);
            var defines = PlayerSettings.GetScriptingDefineSymbols(target);
            
            if (!defines.Contains("PROBUILDER_EXPERIMENTAL_FEATURES"))
            {
                if (string.IsNullOrEmpty(defines))
                    defines = "PROBUILDER_EXPERIMENTAL_FEATURES";
                else
                    defines += ";PROBUILDER_EXPERIMENTAL_FEATURES";
                
                PlayerSettings.SetScriptingDefineSymbols(target, defines);
                UnityEditor.EditorUtility.DisplayDialog("CSG Enabled", 
                    "ProBuilder実験的機能が有効になりました。スクリプトが再コンパイルされます。", "OK");
            }
            else
            {
                UnityEditor.EditorUtility.DisplayDialog("Already Enabled", "ProBuilder実験的機能は既に有効です。", "OK");
            }
        }

        private void SaveSettings()
        {
            string path = UnityEditor.EditorUtility.SaveFilePanel("Save Settings", "", "StructureSettings", "json");
            if (!string.IsNullOrEmpty(path))
            {
                string json = JsonUtility.ToJson(this);
                System.IO.File.WriteAllText(path, json);
            }
        }

        private void LoadSettings()
        {
            string path = UnityEditor.EditorUtility.OpenFilePanel("Load Settings", "", "json");
            if (!string.IsNullOrEmpty(path))
            {
                string data = System.IO.File.ReadAllText(path);
                JsonUtility.FromJsonOverwrite(data, this);
            }
        }
    }
}




