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

namespace Vastcore.Editor.Generation
{
    public class StructureGeneratorWindow : EditorWindow
    {
        private int selectedTab = 0;
        private readonly string[] tabNames = { "Basic", "Advanced", "Operations", "Relationships", "Distribution" };
        
        // タブクラス
        private BasicStructureTab basicTab;
        private AdvancedStructureTab advancedTab;
        private OperationsTab operationsTab;
        private RelationshipTab relationshipTab;
        private ParticleDistributionTab distributionTab;
        
        // --- Shared Parameters ---
        public Material defaultMaterial;
        public Vector3 spawnPosition = Vector3.zero;
        public Vector3 spawnRotation = Vector3.zero;
        
        // --- Settings Tab Parameters ---
        public List<Material> materialPalette = new List<Material>();
        public int selectedMaterialIndex = 0;

        private enum ToolbarOption { Generation, Operations, Procedural, Advanced, Settings }
        // private ToolbarOption currentToolbar = ToolbarOption.Generation; // 未使用のため一時的にコメントアウト

        public enum BooleanOperation { Union, Subtract, Intersect }

        private Vector2 scrollPosition = Vector2.zero;

        [MenuItem("Tools/Structure Generator")]
        public static void ShowWindow()
        {
            GetWindow<StructureGeneratorWindow>("Structure Generator");
        }

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
                
                CompilationPipeline.RequestScriptCompilation();
            }
            else
            {
                UnityEditor.EditorUtility.DisplayDialog("Already Enabled", "ProBuilder実験的機能は既に有効です。", "OK");
                CompilationPipeline.RequestScriptCompilation();
            }
        }

        private void OnEnable()
        {
            // タブクラスを初期化
            basicTab = new BasicStructureTab(this);
            advancedTab = new AdvancedStructureTab(this);
            operationsTab = new OperationsTab(this);
            relationshipTab = new RelationshipTab(this);
            distributionTab = new ParticleDistributionTab(this);
        }
        
        private void OnGUI()
        {
            // タブ選択
            selectedTab = GUILayout.Toolbar(selectedTab, tabNames);
            
            EditorGUILayout.Space();
            
            // 選択されたタブの内容を表示
            switch (selectedTab)
            {
                case 0:
                    basicTab?.OnGUI();
                    break;
                case 1:
                    advancedTab?.OnGUI();
                    break;
                case 2:
                    operationsTab?.Draw();
                    break;
                case 3:
                    relationshipTab?.OnGUI();
                    break;
                case 4:
                    distributionTab?.OnGUI();
                    break;
            }
        }
        
        private void OnSceneGUI(SceneView sceneView)
        {
            // 関係性タブのシーンビューでの描画
            if (selectedTab == 3 && relationshipTab != null)
            {
                relationshipTab.OnSceneGUI();
            }
        }
        
        private void OnFocus()
        {
            // シーンビューのコールバックを登録
            SceneView.duringSceneGui -= OnSceneGUI;
            SceneView.duringSceneGui += OnSceneGUI;
        }
        
        private void OnDestroy()
        {
            // シーンビューのコールバックを解除
            SceneView.duringSceneGui -= OnSceneGUI;
        }

        public void ApplyMaterial(ProBuilderMesh pb)
        {
            if (defaultMaterial != null)
            {
                var renderer = pb.GetComponent<MeshRenderer>();
                if (renderer != null)
                {
                    renderer.sharedMaterial = defaultMaterial;
                }
                pb.Refresh();
            }
            else
            {
                Debug.LogWarning("デフォルトマテリアルが設定されていません。Settingsタブで設定してください。");
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




