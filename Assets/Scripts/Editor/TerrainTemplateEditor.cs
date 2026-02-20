#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using Vastcore.Generation;

using Vastcore.Generation.Map;
using TemplateBlendSettings = Vastcore.Generation.BlendSettings;

namespace Vastcore.Editor
{
    /// <summary>
    /// 地形テンプレートエディターウィンドウ
    /// スライダー中心の現代的なキャラクターエディター風UI
    /// </summary>
    public class TerrainTemplateEditor : EditorWindow
    {
        #region UI設定
        private const float SLIDER_WIDTH = 200f;
        private const float PREVIEW_SIZE = 256f;
        private const float CATEGORY_WIDTH = 200f;
        private const float MIN_WINDOW_WIDTH = 1200f;
        private const float MIN_WINDOW_HEIGHT = 800f;

        private Vector2 scrollPosition;
        private Vector2 templateListScroll;
        private int selectedTemplateIndex = -1;
        #endregion

        #region データ
        private List<DesignerTerrainTemplate> availableTemplates = new List<DesignerTerrainTemplate>();
        private DesignerTerrainTemplate selectedTemplate;
        private TerrainEngine terrainEngine;

        // プレビュー関連
        private Texture2D previewTexture;
        private bool showPreview = true;
        private bool autoUpdatePreview = true;

        // UI状態
        private bool showAdvancedSettings = false;
        private bool showBlendSettings = false;
        private int selectedTab = 0;
        private string[] tabNames = { "基本設定", "地形生成", "ブレンド設定", "プレビュー", "Deform" };
        #endregion

        #region メニュー
        [MenuItem("Window/VastCore/Terrain Template Editor")]
        public static void ShowWindow()
        {
            var window = GetWindow<TerrainTemplateEditor>("地形テンプレートエディター");
            window.minSize = new Vector2(MIN_WINDOW_WIDTH, MIN_WINDOW_HEIGHT);
        }
        #endregion

        #region Unityコールバック
        private void OnEnable()
        {
            RefreshTemplates();
            FindTerrainEngine();
            SetupPreview();
        }

        private void OnGUI()
        {
            if (position.width < MIN_WINDOW_WIDTH || position.height < MIN_WINDOW_HEIGHT)
            {
                EditorGUILayout.HelpBox($"ウィンドウサイズが小さすぎます。最小サイズ: {MIN_WINDOW_WIDTH}x{MIN_WINDOW_HEIGHT}", MessageType.Warning);
                return;
            }

            DrawToolbar();
            DrawMainContent();
        }

        private void OnDisable()
        {
            CleanupPreview();
        }
        #endregion

        #region UI描画
        /// <summary>
        /// ツールバーを描画
        /// </summary>
        private void DrawToolbar()
        {
            using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
            {
                if (GUILayout.Button("新規テンプレート", EditorStyles.toolbarButton))
                {
                    CreateNewTemplate();
                }

                if (GUILayout.Button("複製", EditorStyles.toolbarButton, GUILayout.Width(60)))
                {
                    DuplicateSelectedTemplate();
                }

                if (GUILayout.Button("削除", EditorStyles.toolbarButton, GUILayout.Width(60)))
                {
                    DeleteSelectedTemplate();
                }

                GUILayout.FlexibleSpace();

                if (GUILayout.Button("適用", EditorStyles.toolbarButton, GUILayout.Width(60)))
                {
                    ApplyTemplateToScene();
                }

                if (GUILayout.Button("保存", EditorStyles.toolbarButton, GUILayout.Width(60)))
                {
                    SaveAllTemplates();
                }
            }
        }

        /// <summary>
        /// メインコンテンツを描画
        /// </summary>
        private void DrawMainContent()
        {
            using (var scrollScope = new EditorGUILayout.ScrollViewScope(scrollPosition))
            {
                scrollPosition = scrollScope.scrollPosition;

                using (new EditorGUILayout.HorizontalScope())
                {
                    // 左側：テンプレート一覧
                    DrawTemplateList();

                    // 右側：詳細設定
                    DrawTemplateDetails();
                }
            }
        }

        /// <summary>
        /// テンプレート一覧を描画
        /// </summary>
        private void DrawTemplateList()
        {
            using (new EditorGUILayout.VerticalScope(GUILayout.Width(CATEGORY_WIDTH)))
            {
                EditorGUILayout.LabelField("テンプレート一覧", EditorStyles.boldLabel);

                using (var listScope = new EditorGUILayout.ScrollViewScope(templateListScroll, GUI.skin.box, GUILayout.Height(position.height - 100)))
                {
                    templateListScroll = listScope.scrollPosition;

                    for (int i = 0; i < availableTemplates.Count; i++)
                    {
                        var template = availableTemplates[i];
                        bool isSelected = i == selectedTemplateIndex;

                        using (new EditorGUILayout.HorizontalScope())
                        {
                            // 選択ボタン
                            GUIStyle buttonStyle = isSelected ?
                                new GUIStyle(GUI.skin.button) { normal = { background = Texture2D.whiteTexture } } :
                                GUI.skin.button;

                            if (GUILayout.Button(template != null ? template.templateName : "Null", buttonStyle))
                            {
                                SelectTemplate(i);
                            }

                            // サムネイル（小）
                            if (template != null && template.heightmapTexture != null)
                            {
                                var thumbnail = AssetPreview.GetAssetPreview(template.heightmapTexture);
                                if (thumbnail != null)
                                {
                                    GUILayout.Label(thumbnail, GUILayout.Width(32), GUILayout.Height(32));
                                }
                            }
                        }
                    }
                }

                // 新規作成ボタン
                if (GUILayout.Button("＋ 新規テンプレート"))
                {
                    CreateNewTemplate();
                }
            }
        }

        /// <summary>
        /// テンプレート詳細設定を描画
        /// </summary>
        private void DrawTemplateDetails()
        {
            using (new EditorGUILayout.VerticalScope(GUILayout.ExpandWidth(true)))
            {
                if (selectedTemplate == null)
                {
                    EditorGUILayout.HelpBox("テンプレートを選択してください", MessageType.Info);
                    return;
                }

                // タブ
                selectedTab = GUILayout.Toolbar(selectedTab, tabNames);

                using (new EditorGUILayout.VerticalScope(GUI.skin.box))
                {
                    switch (selectedTab)
                    {
                        case 0: DrawBasicSettings(); break;
                        case 1: DrawTerrainGenerationSettings(); break;
                        case 2: DrawBlendSettings(); break;
                        case 3: DrawPreviewTab(); break;
                        case 4: DrawDeformTab(); break;
                    }
                }
            }
        }

        /// <summary>
        /// 基本設定タブを描画
        /// </summary>
        private void DrawBasicSettings()
        {
            EditorGUILayout.LabelField("基本設定", EditorStyles.boldLabel);
            EditorGUI.indentLevel++;

            // 名前とタイプ
            selectedTemplate.templateName = EditorGUILayout.TextField("テンプレート名", selectedTemplate.templateName);
            selectedTemplate.templateType = (TerrainTemplateType)EditorGUILayout.EnumPopup("テンプレートタイプ", selectedTemplate.templateType);
            selectedTemplate.targetBiome = (BiomeType)EditorGUILayout.EnumPopup("対象バイオーム", selectedTemplate.targetBiome);

            EditorGUILayout.Space();

            // 地形データ
            EditorGUILayout.LabelField("地形データ", EditorStyles.boldLabel);
            selectedTemplate.heightmapTexture = (Texture2D)EditorGUILayout.ObjectField(
                "ハイトマップテクスチャ", selectedTemplate.heightmapTexture, typeof(Texture2D), false);

            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.PrefixLabel("高さスケール");
                selectedTemplate.heightScale = EditorGUILayout.Slider(selectedTemplate.heightScale, 0.1f, 10f);
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.PrefixLabel("ベース高さ");
                selectedTemplate.baseHeight = EditorGUILayout.Slider(selectedTemplate.baseHeight, -100f, 100f);
            }

            EditorGUI.indentLevel--;
        }

        /// <summary>
        /// 地形生成設定タブを描画
        /// </summary>
        private void DrawTerrainGenerationSettings()
        {
            EditorGUILayout.LabelField("地形生成設定", EditorStyles.boldLabel);
            EditorGUI.indentLevel++;

            // バリエーション設定
            selectedTemplate.enableVariations = EditorGUILayout.Toggle("バリエーション有効", selectedTemplate.enableVariations);

            if (selectedTemplate.enableVariations)
            {
                EditorGUILayout.LabelField("スケール範囲", EditorStyles.miniBoldLabel);
                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUILayout.MinMaxSlider("スケール", ref selectedTemplate.scaleRange.x, ref selectedTemplate.scaleRange.y, 0.1f, 2f);
                    selectedTemplate.scaleRange.x = EditorGUILayout.FloatField(selectedTemplate.scaleRange.x, GUILayout.Width(50));
                    selectedTemplate.scaleRange.y = EditorGUILayout.FloatField(selectedTemplate.scaleRange.y, GUILayout.Width(50));
                }

                EditorGUILayout.LabelField("回転範囲", EditorStyles.miniBoldLabel);
                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUILayout.MinMaxSlider("回転", ref selectedTemplate.rotationRange.x, ref selectedTemplate.rotationRange.y, -180f, 180f);
                    selectedTemplate.rotationRange.x = EditorGUILayout.FloatField(selectedTemplate.rotationRange.x, GUILayout.Width(50));
                    selectedTemplate.rotationRange.y = EditorGUILayout.FloatField(selectedTemplate.rotationRange.y, GUILayout.Width(50));
                }

                selectedTemplate.allowFlipHorizontal = EditorGUILayout.Toggle("水平反転許可", selectedTemplate.allowFlipHorizontal);
                selectedTemplate.allowFlipVertical = EditorGUILayout.Toggle("垂直反転許可", selectedTemplate.allowFlipVertical);
            }

            EditorGUI.indentLevel--;

            // 高度設定
            showAdvancedSettings = EditorGUILayout.Foldout(showAdvancedSettings, "高度設定");
            if (showAdvancedSettings)
            {
                EditorGUI.indentLevel++;
                // 高度設定の内容（必要に応じて追加）
                EditorGUI.indentLevel--;
            }
        }

        /// <summary>
        /// ブレンド設定タブを描画
        /// </summary>
        private void DrawBlendSettings()
        {
            EditorGUILayout.LabelField("ブレンド設定", EditorStyles.boldLabel);
            EditorGUI.indentLevel++;

            // BlendSettingsはScriptableObjectとして実装されている想定
            if (selectedTemplate.blendSettings == null)
            {
                if (GUILayout.Button("ブレンド設定を作成"))
                {
                    selectedTemplate.blendSettings = CreateInstance<TemplateBlendSettings>();
                    AssetDatabase.CreateAsset(selectedTemplate.blendSettings, "Assets/Settings/BlendSettings_" + selectedTemplate.templateName + ".asset");
                }
            }
            else
            {
                EditorGUILayout.ObjectField("ブレンド設定", selectedTemplate.blendSettings, typeof(TemplateBlendSettings), false);
                // BlendSettingsのプロパティをインスペクター表示
                DrawBlendSettingsInspector((TemplateBlendSettings)selectedTemplate.blendSettings);
            }

            EditorGUI.indentLevel--;
        }

        /// <summary>
        /// ブレンド設定インスペクターを描画
        /// </summary>
        private void DrawBlendSettingsInspector(TemplateBlendSettings settings)
        {
            if (settings == null) return;

            settings.blendMode = (BlendMode)EditorGUILayout.EnumPopup("ブレンドモード", settings.blendMode);

            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.PrefixLabel("ブレンド強度");
                settings.blendStrength = EditorGUILayout.Slider(settings.blendStrength, 0f, 1f);
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.PrefixLabel("フェード距離");
                settings.fadeDistance = EditorGUILayout.Slider(settings.fadeDistance, 0f, 1000f);
            }

            settings.enableEdgeBlending = EditorGUILayout.Toggle("エッジブレンド有効", settings.enableEdgeBlending);
            settings.edgeBlendWidth = EditorGUILayout.Slider("エッジ幅", settings.edgeBlendWidth, 0f, 50f);
        }

        /// <summary>
        /// プレビュータブを描画
        /// </summary>
        private void DrawPreviewTab()
        {
            EditorGUILayout.LabelField("プレビュー", EditorStyles.boldLabel);

            using (new EditorGUILayout.HorizontalScope())
            {
                showPreview = EditorGUILayout.Toggle("プレビュー表示", showPreview);
                autoUpdatePreview = EditorGUILayout.Toggle("自動更新", autoUpdatePreview);

                if (GUILayout.Button("更新", GUILayout.Width(60)))
                {
                    UpdatePreview();
                }
            }

            if (showPreview)
            {
                if (previewTexture != null)
                {
                    // プレビュー画像表示
                    Rect previewRect = GUILayoutUtility.GetRect(PREVIEW_SIZE, PREVIEW_SIZE);
                    GUI.DrawTexture(previewRect, previewTexture, ScaleMode.ScaleToFit);

                    // プレビュー情報
                    EditorGUILayout.LabelField($"解像度: {previewTexture.width}x{previewTexture.height}", EditorStyles.miniLabel);
                }
                else
                {
                    EditorGUILayout.HelpBox("プレビュー画像がありません", MessageType.Info);
                }
            }

            // テスト生成ボタン
            if (GUILayout.Button("テスト地形生成"))
            {
                TestGenerateTerrain();
            }
        }
        #endregion

        #region 機能メソッド
        /// <summary>
        /// テンプレート一覧を更新
        /// </summary>
        private void RefreshTemplates()
        {
            availableTemplates.Clear();

            // TerrainEngineからテンプレートを取得
            if (terrainEngine != null)
            {
                availableTemplates.AddRange(terrainEngine.availableTemplates);
            }

            // プロジェクト内の全テンプレートを検索
            var templateGuids = AssetDatabase.FindAssets("t:DesignerTerrainTemplate");
            foreach (var guid in templateGuids)
            {
                var template = AssetDatabase.LoadAssetAtPath<DesignerTerrainTemplate>(AssetDatabase.GUIDToAssetPath(guid));
                if (template != null && !availableTemplates.Contains(template))
                {
                    availableTemplates.Add(template);
                }
            }
        }

        /// <summary>
        /// TerrainEngineを検索
        /// </summary>
        private void FindTerrainEngine()
        {
            terrainEngine = FindObjectOfType<TerrainEngine>();
        }

        /// <summary>
        /// テンプレートを選択
        /// </summary>
        private void SelectTemplate(int index)
        {
            if (index >= 0 && index < availableTemplates.Count)
            {
                selectedTemplateIndex = index;
                selectedTemplate = availableTemplates[index];
                UpdatePreview();
            }
        }

        /// <summary>
        /// 新規テンプレートを作成
        /// </summary>
        private void CreateNewTemplate()
        {
            var template = CreateInstance<DesignerTerrainTemplate>();
            template.templateName = "New Terrain Template";
            template.templateType = TerrainTemplateType.Heightmap;
            template.targetBiome = BiomeType.Grassland;

            // アセットとして保存
            string path = "Assets/Templates/" + template.templateName + ".asset";
            AssetDatabase.CreateAsset(template, path);
            AssetDatabase.Refresh();

            availableTemplates.Add(template);
            SelectTemplate(availableTemplates.Count - 1);
        }

        /// <summary>
        /// 選択中のテンプレートを複製
        /// </summary>
        private void DuplicateSelectedTemplate()
        {
            if (selectedTemplate == null) return;

            var duplicate = Instantiate(selectedTemplate);
            duplicate.templateName = selectedTemplate.templateName + " (Copy)";

            string path = "Assets/Templates/" + duplicate.templateName + ".asset";
            AssetDatabase.CreateAsset(duplicate, path);
            AssetDatabase.Refresh();

            availableTemplates.Add(duplicate);
            SelectTemplate(availableTemplates.Count - 1);
        }

        /// <summary>
        /// 選択中のテンプレートを削除
        /// </summary>
        private void DeleteSelectedTemplate()
        {
            if (selectedTemplate == null) return;

            if (EditorUtility.DisplayDialog("テンプレート削除",
                $"テンプレート '{selectedTemplate.templateName}' を削除しますか？", "削除", "キャンセル"))
            {
                availableTemplates.Remove(selectedTemplate);
                AssetDatabase.DeleteAsset(AssetDatabase.GetAssetPath(selectedTemplate));
                AssetDatabase.Refresh();

                selectedTemplate = null;
                selectedTemplateIndex = -1;
            }
        }

        /// <summary>
        /// テンプレートをシーンに適用
        /// </summary>
        private void ApplyTemplateToScene()
        {
            if (selectedTemplate == null || terrainEngine == null) return;

            // 現在のシーンに適用（テスト用）
            terrainEngine.availableTemplates.Add(selectedTemplate);
            EditorUtility.SetDirty(terrainEngine);
        }

        /// <summary>
        /// 全テンプレートを保存
        /// </summary>
        private void SaveAllTemplates()
        {
            foreach (var template in availableTemplates)
            {
                if (template != null)
                {
                    EditorUtility.SetDirty(template);
                }
            }
            AssetDatabase.SaveAssets();
        }

        /// <summary>
        /// プレビューを設定
        /// </summary>
        private void SetupPreview()
        {
            if (previewTexture == null)
            {
                previewTexture = new Texture2D((int)PREVIEW_SIZE, (int)PREVIEW_SIZE);
            }
        }

        /// <summary>
        /// プレビューを更新
        /// </summary>
        private void UpdatePreview()
        {
            if (selectedTemplate == null || previewTexture == null) return;

            // 簡易プレビュー生成（実際の実装ではTerrainSynthesizerを使用）
            GenerateTemplatePreview(selectedTemplate, previewTexture);
            Repaint();
        }

        /// <summary>
        /// テンプレートプレビューを生成
        /// </summary>
        private void GenerateTemplatePreview(DesignerTerrainTemplate template, Texture2D texture)
        {
            if (template.heightmapTexture != null)
            {
                // ハイトマップテクスチャをプレビューにコピー
                RenderTexture.active = RenderTexture.GetTemporary(texture.width, texture.height);
                Graphics.Blit(template.heightmapTexture, RenderTexture.active);
                texture.ReadPixels(new Rect(0, 0, texture.width, texture.height), 0, 0);
                texture.Apply();
                RenderTexture.ReleaseTemporary(RenderTexture.active);
            }
            else
            {
                // デフォルトプレビュー
                Color[] colors = new Color[texture.width * texture.height];
                for (int i = 0; i < colors.Length; i++)
                {
                    colors[i] = Color.gray;
                }
                texture.SetPixels(colors);
                texture.Apply();
            }
        }

        /// <summary>
        /// テスト地形生成
        /// </summary>
        private void TestGenerateTerrain()
        {
            if (selectedTemplate == null || terrainEngine == null) return;

            // テスト用の地形生成（実際の実装では適切な位置に生成）
            var tile = terrainEngine.GenerateTerrainTileSync(new Vector2Int(0, 0), Vector3.zero);
            if (tile != null)
            {
                Debug.Log($"テスト地形生成完了: {tile.coordinate}");
            }
        }

        /// <summary>
        /// プレビュークリーンアップ
        /// </summary>
        private void CleanupPreview()
        {
            if (previewTexture != null)
            {
                DestroyImmediate(previewTexture);
                previewTexture = null;
            }
        }
        /// <summary>
        /// Deformタブを描画
        /// </summary>
        private void DrawDeformTab()
        {
            EditorGUILayout.LabelField("Deform設定", EditorStyles.boldLabel);

#if DEFORM_AVAILABLE
            EditorGUI.indentLevel++;

            // Deform統合有効化
            selectedTemplate.enableDeformIntegration = EditorGUILayout.Toggle("Deform統合有効", selectedTemplate.enableDeformIntegration);

            if (selectedTemplate.enableDeformIntegration)
            {
                // Deformプリセットライブラリ
                selectedTemplate.deformPresetLibrary = (Vastcore.Core.DeformPresetLibrary)EditorGUILayout.ObjectField(
                    "Deformプリセットライブラリ", selectedTemplate.deformPresetLibrary, typeof(Vastcore.Core.DeformPresetLibrary), false);

                EditorGUILayout.Space();

                // Deformプリセット選択
                if (selectedTemplate.deformPresetLibrary != null)
                {
                    EditorGUILayout.LabelField("利用可能なプリセット", EditorStyles.boldLabel);

                    // Geological presets
                    if (selectedTemplate.deformPresetLibrary.geologicalPresets.Count > 0)
                    {
                        EditorGUILayout.LabelField("地質学的プリセット", EditorStyles.miniBoldLabel);
                        foreach (var preset in selectedTemplate.deformPresetLibrary.geologicalPresets)
                        {
                            if (preset.enabled)
                            {
                                EditorGUILayout.LabelField($"• {preset.presetName} ({preset.deformType})");
                            }
                        }
                    }

                    // Architectural presets
                    if (selectedTemplate.deformPresetLibrary.architecturalPresets.Count > 0)
                    {
                        EditorGUILayout.LabelField("建築的プリセット", EditorStyles.miniBoldLabel);
                        foreach (var preset in selectedTemplate.deformPresetLibrary.architecturalPresets)
                        {
                            if (preset.enabled)
                            {
                                EditorGUILayout.LabelField($"• {preset.presetName}");
                            }
                        }
                    }

                    // Organic presets
                    if (selectedTemplate.deformPresetLibrary.organicPresets.Count > 0)
                    {
                        EditorGUILayout.LabelField("有機的プリセット", EditorStyles.miniBoldLabel);
                        foreach (var preset in selectedTemplate.deformPresetLibrary.organicPresets)
                        {
                            if (preset.enabled)
                            {
                                EditorGUILayout.LabelField($"• {preset.presetName}");
                            }
                        }
                    }
                }
                else
                {
                    EditorGUILayout.HelpBox("Deformプリセットライブラリを選択してください", MessageType.Info);
                }

                EditorGUILayout.Space();

                // 適用オプション
                selectedTemplate.autoApplyDeform = EditorGUILayout.Toggle("自動適用", selectedTemplate.autoApplyDeform);
                selectedTemplate.deformQuality = (Vastcore.Core.VastcoreDeformManager.DeformQualityLevel)EditorGUILayout.EnumPopup("品質レベル", selectedTemplate.deformQuality);

                EditorGUILayout.Space();

                // テスト適用ボタン
                if (GUILayout.Button("Deform適用テスト"))
                {
                    TestApplyDeform();
                }
            }
            else
            {
                EditorGUILayout.HelpBox("Deform統合を有効化すると、地形生成時に自動的に変形が適用されます", MessageType.Info);
            }

            EditorGUI.indentLevel--;
#else
            EditorGUILayout.HelpBox("Deformパッケージが利用できません。Package ManagerからDeformをインストールしてください。", MessageType.Warning);
#endif
        }

        /// <summary>
        /// Deform適用テスト
        /// </summary>
        private void TestApplyDeform()
        {
#if DEFORM_AVAILABLE
            if (selectedTemplate == null || selectedTemplate.deformPresetLibrary == null)
            {
                Debug.LogWarning("テンプレートまたはDeformプリセットライブラリが設定されていません");
                return;
            }

            // テスト用のゲームオブジェクトを作成
            GameObject testObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
            testObject.name = "DeformTestObject";

            // Deform統合を適用
            var deformIntegration = testObject.AddComponent<Vastcore.Generation.DeformIntegration>();
            deformIntegration.deformPresetLibrary = selectedTemplate.deformPresetLibrary;

            // 最初の有効なプリセットを適用（テスト用）
            var testPreset = CreateTestDeformPreset();
            if (testPreset != null)
            {
                deformIntegration.ApplyDeformPreset(testObject, testPreset);
                Debug.Log("Deformテスト適用完了");
            }
#endif
        }

        /// <summary>
        /// テスト用Deformプリセット作成
        /// </summary>
        private Vastcore.Generation.DeformPreset CreateTestDeformPreset()
        {
#if DEFORM_AVAILABLE
            if (selectedTemplate.deformPresetLibrary == null) return null;

            // 最初の有効な地質学的プリセットを使用
            var geoPreset = selectedTemplate.deformPresetLibrary.geologicalPresets.Find(p => p.enabled);
            if (geoPreset != null)
            {
                return new Vastcore.Generation.DeformPreset
                {
                    presetName = geoPreset.presetName,
                    presetType = Vastcore.Generation.DeformPreset.DeformPresetType.Custom,
                    intensity = geoPreset.intensity,
                    enabled = true
                };
            }
#endif
            return null;
        }

        #endregion
    }
}
#endif
