> **上位SSOT**: [SSOT_WORLD.md](../SSOT_WORLD.md) | **索引**: [spec-index.json](../spec-index.json) SP-017

# SP-017: Stamp Export Pipeline Spec

**Status:** partial (75%)
**Version:** v1.0
**Last Updated:** 2026-03-17

---

## 概要

StructureGenerator で生成した構造物メッシュを PrefabStampDefinition に変換し、
DualGrid 配置パイプラインに接続する。

これにより「構造物生成 → 配置 → 地形表示」のエンドツーエンドフローが成立する。

## ミッション接続

**ミッション**: 広大な景観に映える、ユニークで巨大な人工構造物をプロシージャルに生成する

このスペックが解決する分断:
- StructureGenerator (22ファイル/8タブ) — メッシュ生成
- DualGrid + PrefabStampPlacer — グリッド配置
- **欠落していたもの**: 両者をつなぐ変換レイヤー

## データフロー

```
StructureGenerator (任意のタブ)
  → GameObject (MeshFilter + MeshRenderer) をシーンに生成
    → StampExporter.ExportAsStamp()
      → Assets/Resources/Stamps/Prefabs/{name}.prefab
      → Assets/Resources/Stamps/Definitions/{name}_StampDef.asset
        → PrefabStampDefinition (ScriptableObject)
          → TerrainWithStampsBootstrap.stampDefinition にアサイン
            → StampRegistry.Place() → PrefabStampPlacer.InstantiateAll()
              → 地形上に構造物が表示
```

## コンポーネント

### StampExporter (Editor-only static class)

- `ExportAsStamp(GameObject target) → PrefabStampDefinition`
- 入力: シーン上の任意の MeshFilter 付き GameObject
- 出力:
  1. Prefab アセット (.prefab)
  2. PrefabStampDefinition アセット (.asset)
- デフォルト設定: Step90回転、TopOfStack高さ、0.8-1.2xスケール範囲
- 位置/回転はリセットしてPrefab化

### StructureGeneratorWindow UI

- "Export as Stamp" ボタン（Selection.activeGameObjectがMeshFilter付きの場合のみ表示）
- エクスポート後にInspectorでDefinitionを選択・表示

## フォルダ構成

```
Assets/Resources/Stamps/
  ├── Prefabs/       ← エクスポートされた Prefab
  └── Definitions/   ← PrefabStampDefinition SO
```

## 体験スライス: "1構造物が地形に立つ"

1. StructureGenerator > Basic Shapes > Arch を生成
2. シーン上の Arch を選択
3. "Export as Stamp" ボタンをクリック
4. 生成された StampDef を TerrainWithStampsBootstrap.stampDefinition にアサイン
5. Play → DualGrid セル上に Arch が配置される

### 成功条件

- [ ] Prefab が Assets/Resources/Stamps/Prefabs/ に保存される
- [ ] StampDef が Assets/Resources/Stamps/Definitions/ に保存される
- [ ] StampDef.Prefab が正しく参照されている
- [ ] TerrainWithStampsBootstrap 経由で地形上に配置される

## 制約

- Editor-only（ランタイムでの動的エクスポートは対象外）
- 単一セル配置のみ（マルチセルフットプリントの自動推定は将来課題）
- マテリアルは元のGameObjectのものを保持（StampDefinition側では管理しない）

## 将来拡張

- バッチエクスポート（複数構造物の一括変換）
- フットプリント自動推定（メッシュバウンズから占有セル数を算出）
- バリエーション生成（パラメータ変異→複数Prefab→StampDefinition群）
- StructureGenerator内でのプレビュー（配置シミュレーション表示）
