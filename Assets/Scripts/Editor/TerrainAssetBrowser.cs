#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;
using Vastcore.Generation;

namespace Vastcore.Editor
{
    /// <summary>
    /// 地形アセットブラウザー
    /// Unreal Engine/Unity/Planet Zoo風のツリーペイン＋画像一覧表示
    /// </summary>
    public class TerrainAssetBrowser : EditorWindow
    {
        #region UI設定
        private const float TREE_PANEL_WIDTH = 250f;
        private const float THUMBNAIL_SIZE = 128f;
        private const float THUMBNAIL_PADDING = 8f;
        private const float SEARCH_BAR_HEIGHT = 30f;
        private const int ITEMS_PER_ROW = 4;

        private Vector2 treeScrollPosition;
        private Vector2 gridScrollPosition;
        private string searchFilter = "";
        #endregion

        #region データ
        private TerrainAssetTree assetTree = new TerrainAssetTree();
        private List<TerrainAssetItem> filteredItems = new List<TerrainAssetItem>();
        private TerrainAssetItem selectedItem;
        private TerrainAssetItem draggedItem;

        // UI状態
        private bool showFavorites = false;
        private string selectedCategory = "All";
        private TerrainAssetViewMode viewMode = TerrainAssetViewMode.Grid;
        #endregion

        #region メニュー
        [MenuItem("Window/VastCore/Terrain Asset Browser")]
        public static void ShowWindow()
        {
            var window = GetWindow<TerrainAssetBrowser>("地形アセットブラウザー");
            window.minSize = new Vector2(800, 600);
        }
        #endregion

        #region Unityコールバック
        private void OnEnable()
        {
            RefreshAssets();
            SetupTree();
        }

        private void OnGUI()
        {
            DrawToolbar();
            DrawMainContent();

            // ドラッグ＆ドロップ処理
            HandleDragAndDrop();
        }

        private void OnDisable()
        {
            assetTree.Clear();
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
                // 検索バー
                EditorGUIUtility.labelWidth = 50;
                searchFilter = EditorGUILayout.TextField("検索", searchFilter, EditorStyles.toolbarTextField, GUILayout.Width(200));
                EditorGUIUtility.labelWidth = 0;

                if (GUILayout.Button("クリア", EditorStyles.toolbarButton, GUILayout.Width(60)))
                {
                    searchFilter = "";
                    RefreshFilteredItems();
                }

                GUILayout.Space(10);

                // カテゴリフィルタ
                EditorGUILayout.LabelField("カテゴリ:", GUILayout.Width(60));
                selectedCategory = EditorGUILayout.TextField(selectedCategory, EditorStyles.toolbarTextField, GUILayout.Width(100));

                // お気に入りフィルタ
                showFavorites = GUILayout.Toggle(showFavorites, "★ お気に入り", EditorStyles.toolbarButton);

                GUILayout.FlexibleSpace();

                // 表示モード
                viewMode = (TerrainAssetViewMode)EditorGUILayout.EnumPopup(viewMode, EditorStyles.toolbarPopup, GUILayout.Width(100));

                // リフレッシュボタン
                if (GUILayout.Button("更新", EditorStyles.toolbarButton, GUILayout.Width(50)))
                {
                    RefreshAssets();
                }
            }
        }

        /// <summary>
        /// メインコンテンツを描画
        /// </summary>
        private void DrawMainContent()
        {
            using (new EditorGUILayout.HorizontalScope(GUILayout.ExpandHeight(true)))
            {
                // 左側：ツリーパネル
                DrawTreePanel();

                // 右側：アセットグリッド/リスト
                DrawAssetPanel();
            }
        }

        /// <summary>
        /// ツリーパネルを描画
        /// </summary>
        private void DrawTreePanel()
        {
            using (new EditorGUILayout.VerticalScope(GUILayout.Width(TREE_PANEL_WIDTH)))
            {
                EditorGUILayout.LabelField("カテゴリ", EditorStyles.boldLabel);

                using (var treeScope = new EditorGUILayout.ScrollViewScope(treeScrollPosition, GUI.skin.box))
                {
                    treeScrollPosition = treeScope.scrollPosition;

                    DrawTreeNode(assetTree.rootNode, 0);
                }
            }
        }

        /// <summary>
        /// ツリーノードを描画
        /// </summary>
        private void DrawTreeNode(TerrainAssetTreeNode node, int depth)
        {
            if (node == null) return;

            // インデント
            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.Space(depth * 15f);

                // 展開/折りたたみボタン
                if (node.children.Count > 0)
                {
                    bool expanded = EditorGUILayout.Foldout(node.expanded, "", false);
                    if (expanded != node.expanded)
                    {
                        node.expanded = expanded;
                    }
                }
                else
                {
                    GUILayout.Space(15f);
                }

                // ノードボタン
                GUIStyle nodeStyle = node.isSelected ? GetSelectedNodeStyle() : GetNormalNodeStyle();
                if (GUILayout.Button(node.name, nodeStyle))
                {
                    SelectTreeNode(node);
                }

                // お気に入りアイコン
                if (node.isFavorite)
                {
                    GUI.Label(GUILayoutUtility.GetLastRect(), "★", GetFavoriteStyle());
                }
            }

            // 子ノードの描画
            if (node.expanded)
            {
                foreach (var child in node.children)
                {
                    DrawTreeNode(child, depth + 1);
                }
            }
        }

        /// <summary>
        /// アセットパネルを描画
        /// </summary>
        private void DrawAssetPanel()
        {
            using (new EditorGUILayout.VerticalScope(GUILayout.ExpandWidth(true)))
            {
                // ヘッダー
                DrawAssetHeader();

                // アセットグリッド/リスト
                using (var gridScope = new EditorGUILayout.ScrollViewScope(gridScrollPosition, GUI.skin.box, GUILayout.ExpandHeight(true)))
                {
                    gridScrollPosition = gridScope.scrollPosition;

                    switch (viewMode)
                    {
                        case TerrainAssetViewMode.Grid:
                            DrawAssetGrid();
                            break;
                        case TerrainAssetViewMode.List:
                            DrawAssetList();
                            break;
                    }
                }
            }
        }

        /// <summary>
        /// アセットヘッダーを描画
        /// </summary>
        private void DrawAssetHeader()
        {
            using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
            {
                EditorGUILayout.LabelField($"{filteredItems.Count} アイテム", EditorStyles.boldLabel);
                GUILayout.FlexibleSpace();

                // ソートオプション
                EditorGUILayout.LabelField("ソート:", GUILayout.Width(40));
                // ソートドロップダウン（実装省略）

                // 表示サイズスライダー
                EditorGUILayout.LabelField("サイズ:", GUILayout.Width(40));
                // サイズスライダー（実装省略）
            }
        }

        /// <summary>
        /// アセットグリッドを描画
        /// </summary>
        private void DrawAssetGrid()
        {
            if (filteredItems.Count == 0)
            {
                EditorGUILayout.HelpBox("該当するアセットが見つかりません", MessageType.Info);
                return;
            }

            float itemWidth = (position.width - TREE_PANEL_WIDTH - 40) / ITEMS_PER_ROW;
            int currentRow = 0;

            using (new EditorGUILayout.VerticalScope())
            {
                while (currentRow * ITEMS_PER_ROW < filteredItems.Count)
                {
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        for (int col = 0; col < ITEMS_PER_ROW; col++)
                        {
                            int itemIndex = currentRow * ITEMS_PER_ROW + col;
                            if (itemIndex >= filteredItems.Count) break;

                            DrawAssetGridItem(filteredItems[itemIndex], itemWidth);
                        }
                    }
                    currentRow++;
                }
            }
        }

        /// <summary>
        /// アセットグリッドアイテムを描画
        /// </summary>
        private void DrawAssetGridItem(TerrainAssetItem item, float width)
        {
            using (new EditorGUILayout.VerticalScope(GUILayout.Width(width), GUILayout.Height(width + 40)))
            {
                // 選択状態
                bool isSelected = selectedItem == item;
                GUIStyle itemStyle = isSelected ? GetSelectedItemStyle() : GetNormalItemStyle();

                // サムネイル
                Rect thumbnailRect = GUILayoutUtility.GetRect(width - THUMBNAIL_PADDING * 2, width - THUMBNAIL_PADDING * 2);
                thumbnailRect = new Rect(thumbnailRect.x + THUMBNAIL_PADDING, thumbnailRect.y + THUMBNAIL_PADDING,
                                       thumbnailRect.width - THUMBNAIL_PADDING * 2, thumbnailRect.height - THUMBNAIL_PADDING * 2);

                if (item.thumbnail != null)
                {
                    GUI.DrawTexture(thumbnailRect, item.thumbnail, ScaleMode.ScaleToFit);
                }
                else
                {
                    // デフォルトサムネイル
                    EditorGUI.DrawRect(thumbnailRect, new Color(0.3f, 0.3f, 0.3f, 1f));
                    GUI.Label(thumbnailRect, "No Preview", GetCenteredLabelStyle());
                }

                // アイテムクリック
                if (Event.current.type == EventType.MouseDown && thumbnailRect.Contains(Event.current.mousePosition))
                {
                    if (Event.current.button == 0) // 左クリック
                    {
                        SelectAssetItem(item);
                        Event.current.Use();
                    }
                }

                // ドラッグ開始
                if (Event.current.type == EventType.MouseDrag && thumbnailRect.Contains(Event.current.mousePosition))
                {
                    StartDrag(item);
                }

                // 名前ラベル
                GUIStyle nameStyle = isSelected ? GetSelectedNameStyle() : GetNormalNameStyle();
                EditorGUILayout.LabelField(item.name, nameStyle, GUILayout.Height(20));

                // お気に入りボタン
                Rect favoriteRect = new Rect(thumbnailRect.xMax - 20, thumbnailRect.yMin + 5, 15, 15);
                if (GUI.Button(favoriteRect, item.isFavorite ? "★" : "☆", GetFavoriteButtonStyle()))
                {
                    ToggleFavorite(item);
                }

                // 境界線
                if (isSelected)
                {
                    EditorGUI.DrawRect(new Rect(thumbnailRect.x - 2, thumbnailRect.y - 2,
                                              thumbnailRect.width + 4, thumbnailRect.height + 4),
                                     new Color(0.24f, 0.49f, 0.91f, 1f));
                }
            }
        }

        /// <summary>
        /// アセットリストを描画
        /// </summary>
        private void DrawAssetList()
        {
            foreach (var item in filteredItems)
            {
                DrawAssetListItem(item);
            }
        }

        /// <summary>
        /// アセットリストアイテムを描画
        /// </summary>
        private void DrawAssetListItem(TerrainAssetItem item)
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                bool isSelected = selectedItem == item;

                // サムネイル
                if (item.thumbnail != null)
                {
                    GUILayout.Label(item.thumbnail, GUILayout.Width(64), GUILayout.Height(64));
                }
                else
                {
                    EditorGUILayout.LabelField("", GUILayout.Width(64), GUILayout.Height(64));
                }

                // 情報
                using (new EditorGUILayout.VerticalScope())
                {
                    EditorGUILayout.LabelField(item.name, isSelected ? EditorStyles.boldLabel : EditorStyles.label);
                    EditorGUILayout.LabelField(item.category, EditorStyles.miniLabel);
                    EditorGUILayout.LabelField($"サイズ: {item.size}", EditorStyles.miniLabel);
                }

                // お気に入り
                if (GUILayout.Button(item.isFavorite ? "★" : "☆", GUILayout.Width(30)))
                {
                    ToggleFavorite(item);
                }
            }

            // クリック処理
            if (Event.current.type == EventType.MouseDown && GUILayoutUtility.GetLastRect().Contains(Event.current.mousePosition))
            {
                SelectAssetItem(item);
            }
        }
        #endregion

        #region 機能メソッド
        /// <summary>
        /// アセットを更新
        /// </summary>
        private void RefreshAssets()
        {
            assetTree.Clear();
            SetupTree();

            // テンプレートアセットを検索
            var templateGuids = AssetDatabase.FindAssets("t:DesignerTerrainTemplate");
            foreach (var guid in templateGuids)
            {
                var template = AssetDatabase.LoadAssetAtPath<DesignerTerrainTemplate>(AssetDatabase.GUIDToAssetPath(guid));
                if (template != null)
                {
                    AddTemplateToTree(template);
                }
            }

            RefreshFilteredItems();
        }

        /// <summary>
        /// ツリーを初期化
        /// </summary>
        private void SetupTree()
        {
            assetTree.rootNode = new TerrainAssetTreeNode("Root");
            assetTree.rootNode.expanded = true;

            // カテゴリノードを作成
            var templatesNode = new TerrainAssetTreeNode("テンプレート");
            var biomesNode = new TerrainAssetTreeNode("バイオーム");

            assetTree.rootNode.children.Add(templatesNode);
            assetTree.rootNode.children.Add(biomesNode);

            // バイオームサブカテゴリ
            foreach (BiomeType biome in System.Enum.GetValues(typeof(BiomeType)))
            {
                biomesNode.children.Add(new TerrainAssetTreeNode(biome.ToString()));
            }
        }

        /// <summary>
        /// テンプレートをツリーに追加
        /// </summary>
        private void AddTemplateToTree(DesignerTerrainTemplate template)
        {
            var item = new TerrainAssetItem
            {
                name = template.templateName,
                category = template.targetBiome.ToString(),
                asset = template,
                thumbnail = template.heightmapTexture,
                size = template.heightmapTexture != null ?
                      $"{template.heightmapTexture.width}x{template.heightmapTexture.height}" : "No Texture"
            };

            assetTree.AddItem(item);
        }

        /// <summary>
        /// フィルタリングされたアイテムを更新
        /// </summary>
        private void RefreshFilteredItems()
        {
            filteredItems.Clear();

            foreach (var item in assetTree.GetAllItems())
            {
                if (MatchesFilter(item))
                {
                    filteredItems.Add(item);
                }
            }

            // ソート（アルファベット順）
            filteredItems.Sort((a, b) => string.Compare(a.name, b.name));
        }

        /// <summary>
        /// フィルタにマッチするかチェック
        /// </summary>
        private bool MatchesFilter(TerrainAssetItem item)
        {
            // 検索フィルタ
            if (!string.IsNullOrEmpty(searchFilter) &&
                !item.name.ToLower().Contains(searchFilter.ToLower()) &&
                !item.category.ToLower().Contains(searchFilter.ToLower()))
            {
                return false;
            }

            // カテゴリフィルタ
            if (!string.IsNullOrEmpty(selectedCategory) && selectedCategory != "All" &&
                item.category != selectedCategory)
            {
                return false;
            }

            // お気に入りフィルタ
            if (showFavorites && !item.isFavorite)
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// ツリーノードを選択
        /// </summary>
        private void SelectTreeNode(TerrainAssetTreeNode node)
        {
            // 以前の選択を解除
            assetTree.ClearSelection();

            // 新しい選択を設定
            node.isSelected = true;
            selectedCategory = node.name;

            RefreshFilteredItems();
        }

        /// <summary>
        /// アセットアイテムを選択
        /// </summary>
        private void SelectAssetItem(TerrainAssetItem item)
        {
            selectedItem = item;

            // インスペクターに表示
            Selection.activeObject = item.asset;
        }

        /// <summary>
        /// お気に入りを切り替え
        /// </summary>
        private void ToggleFavorite(TerrainAssetItem item)
        {
            item.isFavorite = !item.isFavorite;
            // 保存処理（実装省略）
        }

        /// <summary>
        /// ドラッグを開始
        /// </summary>
        private void StartDrag(TerrainAssetItem item)
        {
            draggedItem = item;
            DragAndDrop.PrepareStartDrag();
            DragAndDrop.objectReferences = new Object[] { item.asset };
            DragAndDrop.StartDrag("Terrain Asset");
        }

        /// <summary>
        /// ドラッグ＆ドロップ処理
        /// </summary>
        private void HandleDragAndDrop()
        {
            Event e = Event.current;

            switch (e.type)
            {
                case EventType.DragUpdated:
                    if (draggedItem != null)
                    {
                        DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
                        e.Use();
                    }
                    break;

                case EventType.DragPerform:
                    if (draggedItem != null)
                    {
                        // ドロップ処理（TerrainEngineへの適用など）
                        ApplyDraggedAsset();
                        DragAndDrop.AcceptDrag();
                        e.Use();
                    }
                    break;

                case EventType.DragExited:
                    draggedItem = null;
                    break;
            }
        }

        /// <summary>
        /// ドラッグされたアセットを適用
        /// </summary>
        private void ApplyDraggedAsset()
        {
            if (draggedItem == null || draggedItem.asset == null) return;

            // TerrainEngineにテンプレートを追加
            var terrainEngine = FindObjectOfType<TerrainEngine>();
            if (terrainEngine != null && draggedItem.asset is DesignerTerrainTemplate template)
            {
                if (!terrainEngine.availableTemplates.Contains(template))
                {
                    terrainEngine.availableTemplates.Add(template);
                    EditorUtility.SetDirty(terrainEngine);
                    Debug.Log($"テンプレート '{template.templateName}' をTerrainEngineに追加しました");
                }
            }
        }
        #endregion

        #region スタイルメソッド
        private GUIStyle GetSelectedNodeStyle()
        {
            var style = new GUIStyle(GUI.skin.button);
            style.normal.background = Texture2D.whiteTexture;
            style.normal.textColor = Color.blue;
            return style;
        }

        private GUIStyle GetNormalNodeStyle()
        {
            return GUI.skin.button;
        }

        private GUIStyle GetFavoriteStyle()
        {
            var style = new GUIStyle(GUI.skin.label);
            style.normal.textColor = Color.yellow;
            return style;
        }

        private GUIStyle GetSelectedItemStyle()
        {
            var style = new GUIStyle(GUI.skin.box);
            style.normal.background = Texture2D.whiteTexture;
            return style;
        }

        private GUIStyle GetNormalItemStyle()
        {
            return GUI.skin.box;
        }

        private GUIStyle GetCenteredLabelStyle()
        {
            var style = new GUIStyle(GUI.skin.label);
            style.alignment = TextAnchor.MiddleCenter;
            return style;
        }

        private GUIStyle GetSelectedNameStyle()
        {
            var style = new GUIStyle(EditorStyles.boldLabel);
            style.normal.textColor = new Color(0.24f, 0.49f, 0.91f, 1f);
            return style;
        }

        private GUIStyle GetNormalNameStyle()
        {
            return EditorStyles.label;
        }

        private GUIStyle GetFavoriteButtonStyle()
        {
            var style = new GUIStyle(GUI.skin.button);
            style.fontSize = 12;
            style.padding = new RectOffset(0, 0, 0, 0);
            return style;
        }
        #endregion
    }

    #region データ構造
    /// <summary>
    /// アセットツリー
    /// </summary>
    public class TerrainAssetTree
    {
        public TerrainAssetTreeNode rootNode;

        public void AddItem(TerrainAssetItem item)
        {
            // カテゴリに基づいてノードに追加
            var categoryNode = FindOrCreateCategoryNode(item.category);
            categoryNode.items.Add(item);
        }

        public List<TerrainAssetItem> GetAllItems()
        {
            var allItems = new List<TerrainAssetItem>();
            CollectItemsRecursive(rootNode, allItems);
            return allItems;
        }

        public void ClearSelection()
        {
            ClearSelectionRecursive(rootNode);
        }

        public void Clear()
        {
            rootNode = null;
        }

        private void CollectItemsRecursive(TerrainAssetTreeNode node, List<TerrainAssetItem> items)
        {
            if (node == null) return;

            items.AddRange(node.items);

            foreach (var child in node.children)
            {
                CollectItemsRecursive(child, items);
            }
        }

        private void ClearSelectionRecursive(TerrainAssetTreeNode node)
        {
            if (node == null) return;

            node.isSelected = false;

            foreach (var child in node.children)
            {
                ClearSelectionRecursive(child);
            }
        }

        private TerrainAssetTreeNode FindOrCreateCategoryNode(string category)
        {
            return FindOrCreateNodeRecursive(rootNode, category.Split('/'));
        }

        private TerrainAssetTreeNode FindOrCreateNodeRecursive(TerrainAssetTreeNode current, string[] path)
        {
            if (path.Length == 0) return current;

            var childName = path[0];
            var remainingPath = path.Skip(1).ToArray();

            // 子ノードを検索
            var childNode = current.children.Find(c => c.name == childName);
            if (childNode == null)
            {
                childNode = new TerrainAssetTreeNode(childName);
                current.children.Add(childNode);
            }

            return FindOrCreateNodeRecursive(childNode, remainingPath);
        }
    }

    /// <summary>
    /// ツリーノード
    /// </summary>
    public class TerrainAssetTreeNode
    {
        public string name;
        public bool expanded = false;
        public bool isSelected = false;
        public bool isFavorite = false;
        public List<TerrainAssetTreeNode> children = new List<TerrainAssetTreeNode>();
        public List<TerrainAssetItem> items = new List<TerrainAssetItem>();

        public TerrainAssetTreeNode(string name)
        {
            this.name = name;
        }
    }

    /// <summary>
    /// アセットアイテム
    /// </summary>
    public class TerrainAssetItem
    {
        public string name;
        public string category;
        public string size;
        public bool isFavorite = false;
        public Texture2D thumbnail;
        public Object asset;
    }

    /// <summary>
    /// 表示モード
    /// </summary>
    public enum TerrainAssetViewMode
    {
        Grid,
        List
    }
    #endregion
}
#endif
