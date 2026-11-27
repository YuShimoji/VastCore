using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Vastcore.RegionMap.Editor
{
    /// <summary>
    /// DS-011 の実アセットの読み出し口（Editor）。<c>Assets/Art/Models/&lt;ドメイン&gt;/&lt;カタログ ID&gt;/</c> の、Unity が
    /// GameObject として取り込めたモデル（FBX・OBJ・取り込み手段のある GLB など）を変種と LOD にまとめて返す。
    /// GameObject として読めないファイル（取り込み手段の無い .glb など）は数えないので、その Props はプレースホルダのまま描かれる。
    /// </summary>
    [InitializeOnLoad]
    public sealed class RegionPropAssetDatabase : IRegionPropAssetSource
    {
        static RegionPropAssetDatabase()
        {
            RegionPropAssets.Source = new RegionPropAssetDatabase();
        }

        private readonly Dictionary<string, IReadOnlyList<RegionPropModel>> cache = new Dictionary<string, IReadOnlyList<RegionPropModel>>(StringComparer.Ordinal);

        public IReadOnlyList<RegionPropModel> Models(string domain, string catalogId)
        {
            string folder = RegionPropAssets.ModelsRoot + "/" + domain + "/" + catalogId;
            if (cache.TryGetValue(folder, out IReadOnlyList<RegionPropModel> hit) && AllAlive(hit)) return hit;
            var models = new List<RegionPropModel>();
            if (AssetDatabase.IsValidFolder(folder))
            {
                var loadable = new List<string>();
                foreach (string file in Directory.GetFiles(folder))
                {
                    if (file.EndsWith(".meta", StringComparison.OrdinalIgnoreCase)) continue;
                    string path = file.Replace('\\', '/');
                    if (AssetDatabase.LoadAssetAtPath<GameObject>(path) != null) loadable.Add(path);
                }
                foreach (RegionPropModelFiles files in RegionPropAssets.GroupModelFiles(loadable))
                {
                    models.Add(new RegionPropModel
                    {
                        Variant = files.Variant,
                        Lod0 = AssetDatabase.LoadAssetAtPath<GameObject>(files.Lod0),
                        Lod1 = files.Lod1.Length > 0 ? AssetDatabase.LoadAssetAtPath<GameObject>(files.Lod1) : null
                    });
                }
            }
            cache[folder] = models;
            return models;
        }

        public Texture2D Texture(string assetPath)
        {
            return AssetDatabase.LoadAssetAtPath<Texture2D>(assetPath);
        }

        private static bool AllAlive(IReadOnlyList<RegionPropModel> models)
        {
            foreach (RegionPropModel model in models)
                if (model.Lod0 == null) return false;
            return true;
        }
    }

    /// <summary>
    /// <c>Assets/Art/Models/</c> のモデルを初めて取り込む時の既定。カメラ・光・動き・当たり判定を取り込まず、接線を計算し（法線の画像のため）、
    /// 材質は名前だけを使う（RegionPropView が名前から共有の材質へ読み替える）。既にあるモデルの設定は変えない。
    /// </summary>
    public sealed class RegionPropModelImport : AssetPostprocessor
    {
        private void OnPreprocessModel()
        {
            if (!assetPath.StartsWith(RegionPropAssets.ModelsRoot + "/", StringComparison.Ordinal)) return;
            var importer = (ModelImporter)assetImporter;
            if (!importer.importSettingsMissing) return;
            importer.importCameras = false;
            importer.importLights = false;
            importer.importAnimation = false;
            importer.animationType = ModelImporterAnimationType.None;
            importer.addCollider = false;
            importer.isReadable = false;
            importer.importTangents = ModelImporterTangents.CalculateMikk;
            importer.materialImportMode = ModelImporterMaterialImportMode.ImportViaMaterialDescription;
            importer.materialLocation = ModelImporterMaterialLocation.InPrefab;
            importer.useFileScale = true;
        }
    }
}
