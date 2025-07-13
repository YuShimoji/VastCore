using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;

namespace Vastcore.Editor.Generation
{
    /// <summary>
    /// 構造物関係性を管理するエディタタブ
    /// 複数の構造物間の関係性を設定・管理
    /// </summary>
    public class RelationshipTab : IStructureTab
    {
        public TabCategory Category => TabCategory.Settings;
        public string DisplayName => "Relationships";
        public string Description => "構造物間の親子関係や接続関係を定義・管理します。";
        public bool SupportsRealTimeUpdate => true;

        private StructureGeneratorWindow _parent;
        private Vector2 _scrollPosition;
        
        // 関係性リスト
        private List<StructureRelationshipSystem> relationships = new List<StructureRelationshipSystem>();
        
        // 選択された構造物
        private GameObject selectedParent;
        private GameObject selectedChild;
        
        // UI状態
        private bool showRelationshipList = true;
        private bool showAddRelationship = true;
        private bool showPreview = true;
        
        public RelationshipTab(StructureGeneratorWindow parent)
        {
            _parent = parent;
        }
        
        public void DrawGUI()
        {
            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);
            
            EditorGUILayout.LabelField(DisplayName, EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(Description, MessageType.Info);
            
            EditorGUILayout.Space();

            DrawRelationshipList();
            
            EditorGUILayout.Space();
            
            DrawAddRelationship();
            
            EditorGUILayout.Space();
            
            DrawPreviewControls();

            EditorGUILayout.EndScrollView();
        }
        
        public void HandleRealTimeUpdate()
        {
            if (!showPreview) return;
            
            foreach (var relationship in relationships)
            {
                if(relationship.maintainRelationship)
                {
                    relationship.ApplyRelationship();
                }
            }
        }

        public void OnSceneGUI()
        {
            if (!showPreview) return;
            
            foreach (var relationship in relationships)
            {
                relationship.DrawGizmos();
            }
        }

        public void ProcessSelectedObjects()
        {
            ApplyAllRelationships();
        }
        
        private void DrawRelationshipList()
        {
            showRelationshipList = EditorGUILayout.Foldout(showRelationshipList, "Active Relationships", true);
            
            if (showRelationshipList)
            {
                EditorGUI.indentLevel++;
                
                if (relationships.Count == 0)
                {
                    EditorGUILayout.HelpBox("No relationships defined. Add relationships to control structure placement.", MessageType.Info);
                }
                else
                {
                    for (int i = 0; i < relationships.Count; i++)
                    {
                        DrawRelationshipItem(i);
                    }
                }
                
                EditorGUI.indentLevel--;
            }
        }
        
        private void DrawRelationshipItem(int index)
        {
            var relationship = relationships[index];
            
            EditorGUILayout.BeginVertical(GUI.skin.box);
            
            // ヘッダー
            EditorGUILayout.BeginHorizontal();
            
            string relationshipName = GetRelationshipDisplayName(relationship);
            EditorGUILayout.LabelField($"#{index + 1}: {relationshipName}", EditorStyles.boldLabel);
            
            // 適用ボタン
            if (GUILayout.Button("Apply", GUILayout.Width(60)))
            {
                relationship.ApplyRelationship();
            }
            
            // 削除ボタン
            GUI.backgroundColor = Color.red;
            if (GUILayout.Button("×", GUILayout.Width(25)))
            {
                relationships.RemoveAt(index);
                return;
            }
            GUI.backgroundColor = Color.white;
            
            EditorGUILayout.EndHorizontal();
            
            // 詳細設定
            EditorGUI.indentLevel++;
            
            // 関係性タイプ
            relationship.relationshipType = (StructureRelationship)EditorGUILayout.EnumPopup("Relationship Type", relationship.relationshipType);
            
            // 親・子構造物
            relationship.parentStructure = (GameObject)EditorGUILayout.ObjectField("Parent Structure", relationship.parentStructure, typeof(GameObject), true);
            relationship.childStructure = (GameObject)EditorGUILayout.ObjectField("Child Structure", relationship.childStructure, typeof(GameObject), true);
            
            // 相対位置設定
            DrawRelativePositionSettings(relationship);
            
            // オプション
            relationship.autoCalculatePosition = EditorGUILayout.Toggle("Auto Calculate Position", relationship.autoCalculatePosition);
            relationship.maintainRelationship = EditorGUILayout.Toggle("Maintain Relationship", relationship.maintainRelationship);
            relationship.scaleWithParent = EditorGUILayout.Toggle("Scale With Parent", relationship.scaleWithParent);
            
            EditorGUI.indentLevel--;
            
            EditorGUILayout.EndVertical();
            EditorGUILayout.Space();
        }
        
        private void DrawRelativePositionSettings(StructureRelationshipSystem relationship)
        {
            EditorGUILayout.LabelField("Relative Position Settings", EditorStyles.boldLabel);
            
            // 基本オフセット
            relationship.relativePosition.offset = EditorGUILayout.Vector3Field("Offset", relationship.relativePosition.offset);
            relationship.relativePosition.rotationOffset = EditorGUILayout.Vector3Field("Rotation Offset", relationship.relativePosition.rotationOffset);
            relationship.relativePosition.scaleMultiplier = EditorGUILayout.Vector3Field("Scale Multiplier", relationship.relativePosition.scaleMultiplier);
            
            // 距離・角度（特定の関係性で使用）
            if (UsesDistanceAngle(relationship.relationshipType))
            {
                relationship.relativePosition.distance = EditorGUILayout.FloatField("Distance", relationship.relativePosition.distance);
                relationship.relativePosition.angle = EditorGUILayout.Slider("Angle", relationship.relativePosition.angle, 0f, 360f);
            }
            
            // 分布カーブ
            relationship.relativePosition.distribution = EditorGUILayout.CurveField("Distribution Curve", relationship.relativePosition.distribution);
        }
        
        private void DrawAddRelationship()
        {
            showAddRelationship = EditorGUILayout.Foldout(showAddRelationship, "Add New Relationship", true);
            
            if (showAddRelationship)
            {
                EditorGUI.indentLevel++;
                
                EditorGUILayout.BeginVertical(GUI.skin.box);
                
                // 構造物選択
                selectedParent = (GameObject)EditorGUILayout.ObjectField("Parent Structure", selectedParent, typeof(GameObject), true);
                selectedChild = (GameObject)EditorGUILayout.ObjectField("Child Structure", selectedChild, typeof(GameObject), true);
                
                EditorGUILayout.Space();
                
                // クイック関係性ボタン
                EditorGUILayout.LabelField("Quick Relationships", EditorStyles.boldLabel);
                
                EditorGUILayout.BeginHorizontal();
                
                if (GUILayout.Button("On Top"))
                {
                    AddQuickRelationship(StructureRelationship.OnTop);
                }
                
                if (GUILayout.Button("Inside"))
                {
                    AddQuickRelationship(StructureRelationship.Inside);
                }
                
                if (GUILayout.Button("On Side"))
                {
                    AddQuickRelationship(StructureRelationship.OnSide);
                }
                
                EditorGUILayout.EndHorizontal();
                
                EditorGUILayout.BeginHorizontal();
                
                if (GUILayout.Button("Around"))
                {
                    AddQuickRelationship(StructureRelationship.Around);
                }
                
                if (GUILayout.Button("Orbit"))
                {
                    AddQuickRelationship(StructureRelationship.OrbitAround);
                }
                
                if (GUILayout.Button("Stacked"))
                {
                    AddQuickRelationship(StructureRelationship.StackedOn);
                }
                
                EditorGUILayout.EndHorizontal();
                
                EditorGUILayout.Space();
                
                // 選択オブジェクトから追加
                if (GUILayout.Button("Add from Selection"))
                {
                    AddFromSelection();
                }
                
                EditorGUILayout.EndVertical();
                
                EditorGUI.indentLevel--;
            }
        }
        
        private void DrawPreviewControls()
        {
            showPreview = EditorGUILayout.Foldout(showPreview, "Preview & Controls", true);
            
            if (showPreview)
            {
                EditorGUI.indentLevel++;
                
                EditorGUILayout.BeginHorizontal();
                
                // 全関係性を適用
                if (GUILayout.Button("Apply All Relationships"))
                {
                    ApplyAllRelationships();
                }
                
                // 関係性をクリア
                if (GUILayout.Button("Clear All Relationships"))
                {
                    if (EditorUtility.DisplayDialog("Clear Relationships", "Are you sure you want to clear all relationships?", "Yes", "Cancel"))
                    {
                        relationships.Clear();
                    }
                }
                
                EditorGUILayout.EndHorizontal();
                
                EditorGUILayout.Space();
                
                // 統計情報
                EditorGUILayout.LabelField("Statistics", EditorStyles.boldLabel);
                EditorGUILayout.LabelField($"Total Relationships: {relationships.Count}");
                EditorGUILayout.LabelField($"Active Relationships: {relationships.Count(r => r.parentStructure != null && r.childStructure != null)}");
                
                EditorGUI.indentLevel--;
            }
        }
        
        private void AddQuickRelationship(StructureRelationship relationshipType)
        {
            if (selectedParent == null || selectedChild == null)
            {
                EditorUtility.DisplayDialog("Error", "Please select both parent and child structures.", "OK");
                return;
            }
            
            var newRelationship = new StructureRelationshipSystem
            {
                relationshipType = relationshipType,
                parentStructure = selectedParent,
                childStructure = selectedChild,
                relativePosition = RelativePosition.Default
            };
            
            // 関係性タイプに応じたデフォルト値を設定
            SetDefaultValuesForRelationship(newRelationship);
            
            relationships.Add(newRelationship);
            
            Debug.Log($"Added relationship: {relationshipType} between {selectedParent.name} and {selectedChild.name}");
        }
        
        private void AddFromSelection()
        {
            var selectedObjects = Selection.gameObjects;
            
            if (selectedObjects.Length < 2)
            {
                EditorUtility.DisplayDialog("Error", "Please select at least 2 objects to create relationships.", "OK");
                return;
            }
            
            // 最初のオブジェクトを親とし、残りを子として関係性を作成
            var parent = selectedObjects[0];
            
            for (int i = 1; i < selectedObjects.Length; i++)
            {
                var child = selectedObjects[i];
                
                var newRelationship = new StructureRelationshipSystem
                {
                    relationshipType = StructureRelationship.Around,
                    parentStructure = parent,
                    childStructure = child,
                    relativePosition = RelativePosition.Default
                };
                
                // 角度を分散
                newRelationship.relativePosition.angle = (360f / (selectedObjects.Length - 1)) * (i - 1);
                
                relationships.Add(newRelationship);
            }
            
            Debug.Log($"Added {selectedObjects.Length - 1} relationships from selection");
        }
        
        private void SetDefaultValuesForRelationship(StructureRelationshipSystem relationship)
        {
            switch (relationship.relationshipType)
            {
                case StructureRelationship.Around:
                case StructureRelationship.OrbitAround:
                    relationship.relativePosition.distance = 50f;
                    relationship.relativePosition.angle = 0f;
                    break;
                    
                case StructureRelationship.OnTop:
                case StructureRelationship.StackedOn:
                    relationship.relativePosition.offset = Vector3.zero;
                    break;
                    
                case StructureRelationship.OnSide:
                    relationship.relativePosition.offset = Vector3.right * 10f;
                    break;
                    
                case StructureRelationship.Inside:
                    relationship.relativePosition.scaleMultiplier = Vector3.one * 0.8f;
                    break;
            }
        }
        
        private void ApplyAllRelationships()
        {
            int applied = 0;
            
            foreach (var relationship in relationships)
            {
                if (relationship.parentStructure != null && relationship.childStructure != null)
                {
                    relationship.ApplyRelationship();
                    applied++;
                }
            }
            
            Debug.Log($"Applied {applied} relationships");
            
            // シーンビューを更新
            SceneView.RepaintAll();
        }
        
        private string GetRelationshipDisplayName(StructureRelationshipSystem relationship)
        {
            string parentName = relationship.parentStructure ? relationship.parentStructure.name : "None";
            string childName = relationship.childStructure ? relationship.childStructure.name : "None";
            
            return $"{childName} → {relationship.relationshipType} → {parentName}";
        }
        
        private bool UsesDistanceAngle(StructureRelationship relationshipType)
        {
            return relationshipType == StructureRelationship.Around ||
                   relationshipType == StructureRelationship.OrbitAround ||
                   relationshipType == StructureRelationship.ClusterAround;
        }
        
        public void OnTabSelected() { }
        public void OnTabDeselected() { }
    }
} 