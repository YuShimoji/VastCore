using UnityEngine;
using UnityEditor;

namespace Vastcore.Editor.Generation
{
    /// <summary>
    /// 全タブで統一されたインターフェース
    /// 生成・編集・設定の分類を明確化
    /// </summary>
    public interface IStructureTab
    {
        /// <summary>
        /// タブの種類（生成・編集・設定）
        /// </summary>
        TabCategory Category { get; }
        
        /// <summary>
        /// タブの表示名
        /// </summary>
        string DisplayName { get; }
        
        /// <summary>
        /// タブの説明
        /// </summary>
        string Description { get; }
        
        /// <summary>
        /// リアルタイム更新対応の有無
        /// </summary>
        bool SupportsRealTimeUpdate { get; }
        
        /// <summary>
        /// 統一されたGUI描画
        /// </summary>
        void DrawGUI();
        
        /// <summary>
        /// リアルタイム更新処理
        /// </summary>
        void HandleRealTimeUpdate();
        
        /// <summary>
        /// 選択オブジェクトに対する処理
        /// </summary>
        void ProcessSelectedObjects();

        /// <summary>
        /// シーンビューでのGUI描画
        /// </summary>
        void OnSceneGUI();

        /// <summary>
        /// タブが選択されたときの処理
        /// </summary>
        void OnTabSelected();

        /// <summary>
        /// タブの選択が外れたときの処理
        /// </summary>
        void OnTabDeselected();
    }
    
    /// <summary>
    /// タブのカテゴリ分類
    /// </summary>
    public enum TabCategory
    {
        Generation,  // 🏗️ 生成
        Editing,     // ✏️ 編集  
        Settings     // ⚙️ 設定
    }
    
    /// <summary>
    /// 統一されたタブベースクラス
    /// </summary>
    public abstract class BaseStructureTab : IStructureTab
    {
        protected StructureGeneratorWindow parentWindow;
        protected Vector2 scrollPosition = Vector2.zero;
        protected bool enableRealTimeUpdate = false;
        protected float lastUpdateTime = 0f;
        protected bool parametersChanged = false;
        
        public abstract TabCategory Category { get; }
        public abstract string DisplayName { get; }
        public abstract string Description { get; }
        public virtual bool SupportsRealTimeUpdate => false;
        
        protected BaseStructureTab(StructureGeneratorWindow parent)
        {
            parentWindow = parent;
        }
        
        public void DrawGUI()
        {
            // 統一されたヘッダー
            DrawTabHeader();
            
            // スクロールビュー開始
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
            
            // 各タブ固有のコンテンツ
            DrawTabContent();
            
            // リアルタイム更新コントロール（対応タブのみ）
            if (SupportsRealTimeUpdate)
            {
                DrawRealTimeUpdateControls();
            }
            
            // 統一されたフッター
            DrawTabFooter();
            
            // スクロールビュー終了
            EditorGUILayout.EndScrollView();
            
            // リアルタイム更新処理
            if (SupportsRealTimeUpdate && enableRealTimeUpdate)
            {
                HandleRealTimeUpdate();
            }
        }
        
        protected virtual void DrawTabHeader()
        {
            // カテゴリアイコンと名前
            string categoryIcon = GetCategoryIcon(Category);
            EditorGUILayout.LabelField($"{categoryIcon} {DisplayName}", EditorStyles.boldLabel);
            
            // 説明
            if (!string.IsNullOrEmpty(Description))
            {
                EditorGUILayout.HelpBox(Description, MessageType.Info);
            }
            
            EditorGUILayout.Space(5);
        }
        
        protected virtual void DrawRealTimeUpdateControls()
        {
            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("🔄 リアルタイム制御", EditorStyles.boldLabel);
            
            EditorGUI.BeginChangeCheck();
            enableRealTimeUpdate = EditorGUILayout.Toggle("リアルタイム更新", enableRealTimeUpdate);
            if (EditorGUI.EndChangeCheck() && enableRealTimeUpdate)
            {
                parametersChanged = true;
            }
            
            if (enableRealTimeUpdate)
            {
                EditorGUILayout.HelpBox("パラメータ変更時に自動的にオブジェクトが更新されます", MessageType.Info);
            }
        }
        
        protected virtual void DrawTabFooter()
        {
            EditorGUILayout.Space(10);
            
            // 統一されたアクションボタン
            if (Category == TabCategory.Generation)
            {
                GUI.backgroundColor = Color.green;
                if (GUILayout.Button("🏗️ 生成実行", GUILayout.Height(35)))
                {
                    ProcessSelectedObjects();
                }
                GUI.backgroundColor = Color.white;
            }
            else if (Category == TabCategory.Editing)
            {
                GUI.backgroundColor = Color.cyan;
                if (GUILayout.Button("✏️ 編集適用", GUILayout.Height(35)))
                {
                    ProcessSelectedObjects();
                }
                GUI.backgroundColor = Color.white;
            }
        }
        
        protected string GetCategoryIcon(TabCategory category)
        {
            switch (category)
            {
                case TabCategory.Generation: return "🏗️";
                case TabCategory.Editing: return "✏️";
                case TabCategory.Settings: return "⚙️";
                default: return "📋";
            }
        }
        
        /// <summary>
        /// 各タブ固有のコンテンツ描画（サブクラスで実装）
        /// </summary>
        protected abstract void DrawTabContent();
        
        /// <summary>
        /// リアルタイム更新処理（必要に応じてオーバーライド）
        /// </summary>
        public virtual void HandleRealTimeUpdate()
        {
            if (!enableRealTimeUpdate) return;
            
            float currentTime = (float)EditorApplication.timeSinceStartup;
            
            if (parametersChanged && (currentTime - lastUpdateTime) > 0.5f)
            {
                ProcessSelectedObjects();
                lastUpdateTime = currentTime;
                parametersChanged = false;
            }
        }
        
        /// <summary>
        /// 選択オブジェクトに対する処理（サブクラスで実装）
        /// </summary>
        public abstract void ProcessSelectedObjects();
        
        /// <summary>
        /// パラメータ変更時に呼び出す
        /// </summary>
        protected void OnParameterChanged()
        {
            parametersChanged = true;
        }

        public virtual void OnSceneGUI() { }
        public virtual void OnTabSelected() { }
        public virtual void OnTabDeselected() { }
    }
} 