using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Vastcore.Generation;

namespace Vastcore.Editor.Terrain
{
    /// <summary>
    /// 地形生成プリセット管理クラス
    /// TerrainGenerationProfile をプリセットとして保存・読み込みする機能を提供
    /// </summary>
    public static class TerrainPresetManager
    {
        #region Constants
        private const string c_PresetFolderPath = "Assets/TerrainPresets";
        private const string c_PresetAssetPrefix = "TerrainPreset_";
        #endregion

        #region Public Methods
        /// <summary>
        /// プリセット保存先フォルダのパスを取得（存在しない場合は作成）
        /// </summary>
        public static string GetPresetFolderPath()
        {
            if (!AssetDatabase.IsValidFolder(c_PresetFolderPath))
            {
                // Assets フォルダが存在することを確認
                if (!AssetDatabase.IsValidFolder("Assets"))
                {
                    Debug.LogError("[TerrainPresetManager] Assets folder not found.");
                    return null;
                }

                // TerrainPresets フォルダを作成
                string guid = AssetDatabase.CreateFolder("Assets", "TerrainPresets");
                if (string.IsNullOrEmpty(guid))
                {
                    Debug.LogError("[TerrainPresetManager] Failed to create TerrainPresets folder.");
                    return null;
                }

                AssetDatabase.Refresh();
                Debug.Log($"[TerrainPresetManager] Created preset folder: {c_PresetFolderPath}");
            }

            return c_PresetFolderPath;
        }

        /// <summary>
        /// 現在の設定を新しいプリセットとして保存
        /// </summary>
        /// <param name="_presetName">プリセット名</param>
        /// <param name="_sourceProfile">保存元のプロファイル（nullの場合は現在の設定から新規作成）</param>
        /// <returns>作成されたプリセットアセット、失敗時はnull</returns>
        public static TerrainGenerationProfile SavePreset(string _presetName, TerrainGenerationProfile _sourceProfile = null)
        {
            if (string.IsNullOrEmpty(_presetName))
            {
                Debug.LogError("[TerrainPresetManager] Preset name cannot be empty.");
                return null;
            }

            // プリセット名から無効な文字を削除
            string sanitizedName = SanitizePresetName(_presetName);
            if (string.IsNullOrEmpty(sanitizedName))
            {
                Debug.LogError("[TerrainPresetManager] Preset name contains only invalid characters.");
                return null;
            }

            // フォルダパスを取得（存在しない場合は作成）
            string folderPath = GetPresetFolderPath();
            if (string.IsNullOrEmpty(folderPath))
            {
                return null;
            }

            // アセットパスを生成
            string assetPath = $"{folderPath}/{c_PresetAssetPrefix}{sanitizedName}.asset";

            // 既存のアセットが存在する場合は確認
            if (File.Exists(assetPath))
            {
                if (!EditorUtility.DisplayDialog(
                    "Preset Already Exists",
                    $"Preset '{sanitizedName}' already exists. Overwrite?",
                    "Overwrite",
                    "Cancel"))
                {
                    return null;
                }
            }

            // 新しいプロファイルを作成
            TerrainGenerationProfile newPreset = _sourceProfile != null
                ? CreatePresetFromProfile(_sourceProfile)
                : CreateInstance<TerrainGenerationProfile>();

            // アセットとして保存
            if (File.Exists(assetPath))
            {
                // 既存アセットを上書き
                TerrainGenerationProfile existingPreset = AssetDatabase.LoadAssetAtPath<TerrainGenerationProfile>(assetPath);
                if (existingPreset != null)
                {
                    existingPreset.CopyFrom(newPreset);
                    EditorUtility.SetDirty(existingPreset);
                    AssetDatabase.SaveAssets();
                    Debug.Log($"[TerrainPresetManager] Updated preset: {assetPath}");
                    return existingPreset;
                }
            }

            // 新規アセットを作成
            AssetDatabase.CreateAsset(newPreset, assetPath);
            AssetDatabase.SaveAssets();
            Debug.Log($"[TerrainPresetManager] Created preset: {assetPath}");

            return newPreset;
        }

        /// <summary>
        /// 保存済みプリセットの一覧を取得
        /// </summary>
        /// <returns>プリセットアセットのリスト</returns>
        public static List<TerrainGenerationProfile> GetAllPresets()
        {
            List<TerrainGenerationProfile> presets = new List<TerrainGenerationProfile>();

            string folderPath = GetPresetFolderPath();
            if (string.IsNullOrEmpty(folderPath))
            {
                return presets;
            }

            // フォルダ内のすべての TerrainGenerationProfile アセットを検索
            string[] guids = AssetDatabase.FindAssets($"t:{typeof(TerrainGenerationProfile).Name}", new[] { folderPath });
            
            foreach (string guid in guids)
            {
                string assetPath = AssetDatabase.GUIDToAssetPath(guid);
                TerrainGenerationProfile preset = AssetDatabase.LoadAssetAtPath<TerrainGenerationProfile>(assetPath);
                if (preset != null)
                {
                    presets.Add(preset);
                }
            }

            // 名前でソート
            presets = presets.OrderBy(p => p.name).ToList();

            return presets;
        }

        /// <summary>
        /// プリセット名のリストを取得（UI表示用）
        /// </summary>
        /// <returns>プリセット名の配列</returns>
        public static string[] GetPresetNames()
        {
            List<TerrainGenerationProfile> presets = GetAllPresets();
            return presets.Select(p => ExtractPresetName(p.name)).ToArray();
        }

        /// <summary>
        /// プリセット名からプロファイルを取得
        /// </summary>
        /// <param name="_presetName">プリセット名</param>
        /// <returns>プロファイル、見つからない場合はnull</returns>
        public static TerrainGenerationProfile GetPresetByName(string _presetName)
        {
            if (string.IsNullOrEmpty(_presetName))
            {
                return null;
            }

            List<TerrainGenerationProfile> presets = GetAllPresets();
            string sanitizedName = SanitizePresetName(_presetName);

            foreach (TerrainGenerationProfile preset in presets)
            {
                string presetName = ExtractPresetName(preset.name);
                if (presetName == sanitizedName || presetName == _presetName)
                {
                    return preset;
                }
            }

            return null;
        }

        /// <summary>
        /// プリセットを削除
        /// </summary>
        /// <param name="_preset">削除するプリセット</param>
        /// <returns>削除に成功した場合true</returns>
        public static bool DeletePreset(TerrainGenerationProfile _preset)
        {
            if (_preset == null)
            {
                return false;
            }

            string assetPath = AssetDatabase.GetAssetPath(_preset);
            if (string.IsNullOrEmpty(assetPath))
            {
                return false;
            }

            if (!EditorUtility.DisplayDialog(
                "Delete Preset",
                $"Are you sure you want to delete preset '{ExtractPresetName(_preset.name)}'?",
                "Delete",
                "Cancel"))
            {
                return false;
            }

            bool deleted = AssetDatabase.DeleteAsset(assetPath);
            if (deleted)
            {
                AssetDatabase.Refresh();
                Debug.Log($"[TerrainPresetManager] Deleted preset: {assetPath}");
            }

            return deleted;
        }

        /// <summary>
        /// 現在のウィンドウ設定からプロファイルを作成
        /// </summary>
        /// <param name="_window">TerrainGenerationWindow インスタンス</param>
        /// <returns>作成されたプロファイル</returns>
        public static TerrainGenerationProfile CreateProfileFromWindow(TerrainGenerationWindow _window)
        {
            if (_window == null)
            {
                return null;
            }

            TerrainGenerationProfile profile = CreateInstance<TerrainGenerationProfile>();
            _window.CopySettingsToProfile(profile);
            return profile;
        }
        #endregion

        #region Private Methods
        /// <summary>
        /// プリセット名から無効な文字を削除
        /// </summary>
        private static string SanitizePresetName(string _name)
        {
            if (string.IsNullOrEmpty(_name))
            {
                return string.Empty;
            }

            // Unity のアセット名で使用できない文字を削除
            char[] invalidChars = Path.GetInvalidFileNameChars();
            string sanitized = _name;
            
            foreach (char c in invalidChars)
            {
                sanitized = sanitized.Replace(c.ToString(), string.Empty);
            }

            // 先頭・末尾の空白を削除
            sanitized = sanitized.Trim();

            return sanitized;
        }

        /// <summary>
        /// アセット名からプリセット名を抽出（TerrainPreset_ プレフィックスを除去）
        /// </summary>
        private static string ExtractPresetName(string _assetName)
        {
            if (string.IsNullOrEmpty(_assetName))
            {
                return string.Empty;
            }

            if (_assetName.StartsWith(c_PresetAssetPrefix))
            {
                return _assetName.Substring(c_PresetAssetPrefix.Length);
            }

            return _assetName;
        }

        /// <summary>
        /// 既存のプロファイルから新しいプリセットを作成（コピー）
        /// </summary>
        private static TerrainGenerationProfile CreatePresetFromProfile(TerrainGenerationProfile _source)
        {
            if (_source == null)
            {
                return CreateInstance<TerrainGenerationProfile>();
            }

            TerrainGenerationProfile newPreset = CreateInstance<TerrainGenerationProfile>();
            newPreset.CopyFrom(_source);
            return newPreset;
        }
        #endregion
    }
}
