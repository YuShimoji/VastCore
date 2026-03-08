# Phase C 完了後クイックウィン一覧

- **最終更新日時**: 2026-03-08
- **ステータス**: Active

4つの新機能提案から抽出した、各4時間以内で完了可能な小タスク群。
Phase D（最適化）とは独立に着手可能。

---

## QW-A: 気候ビジュアル連携 (Climate Visuals)

最もローリスク・ハイリターンな領域。既存 ClimateSystem のデータを視覚に反映するだけで効果が出る。

| ID | タスク | 工数 | 前提 | 価値 |
|----|--------|------|------|------|
| QW-A1 | Shader Globals ブリッジ | 2h | なし | 全シェーダーが気候データ参照可能に |
| QW-A2 | 季節駆動テレイン色調変化 | 1h | なし | OnSeasonChanged → terrainTint 変更 |
| QW-A3 | 動的スノーライン | 3h | QW-A1 | 温度から雪線高度を動的算出 |
| QW-A4 | 風駆動の草アニメーション | 3h | QW-A1 | 頂点シェーダーで風揺れ |

### QW-A1 実装概要
```csharp
// ClimateVisualBridge.cs (MonoBehaviour)
// ClimateSystem.OnClimateDataUpdated に subscribe
// Shader.SetGlobalFloat("_VCTemperature", data.temperature);
// Shader.SetGlobalFloat("_VCMoisture", data.moisture);
// Shader.SetGlobalVector("_VCWindDir", new Vector4(data.windDirection.x, ...));
```

---

## QW-B: エコシステム生成 (Ecosystem)

IDensityField パイプラインがプラグイン構造のため、1層追加するだけで動く。

| ID | タスク | 工数 | 前提 | 価値 |
|----|--------|------|------|------|
| QW-B1 | BiomePreset → TreeGenerator 連携強化 | 3h | なし | バイオーム別の樹木密度・種類 |
| QW-B2 | DetailGenerator に湿度フィルタ追加 | 2h | なし | 乾燥地帯で草を減らす |
| QW-B3 | FieldLayerType.Ecosystem 追加 | 4h | なし | WorldGenRecipe に生態系レイヤー追加 |

---

## QW-C: 複合構造ルール (Composite Structure)

データ構造定義が中心。既存コードのパターンを汎用化する。

| ID | タスク | 工数 | 前提 | 価値 |
|----|--------|------|------|------|
| QW-C1 | StructureAssemblyRecipe ScriptableObject 定義 | 2h | なし | データ駆動構造生成の基盤 |
| QW-C2 | StructureBlueprint に slots フィールド追加 | 1h | なし | 文法出力として機能 |
| QW-C3 | CompoundArchitecturalGenerator 反復ロジック抽出 | 4h | なし | RepeatAndPlace 汎用メソッド |

---

## QW-D: 破壊可能建築 (Destructible Architecture)

DensityGrid + VolumetricStreamingController が既に再構築パイプラインを持つ。

| ID | タスク | 工数 | 前提 | 価値 |
|----|--------|------|------|------|
| QW-D1 | DensityGrid.SubtractSphere() 実装 | 2h | なし | 破壊の基本プリミティブ |
| QW-D2 | DeformMask ユーティリティ抽出 | 1h | なし | #if DEFORM_AVAILABLE ゲート除去 |
| QW-D3 | MarchingCubes バッファ再利用 | 2h | なし | GC 圧力軽減 |
| QW-D4 | ランタイム破壊ブラシ | 3h | QW-D1 | SubtractSphere + MarkDirty で動く破壊 |
| QW-D5 | Mesh → Voxel 変換プロトタイプ | 4h | なし | 構造物の密度場変換 |

---

## 推奨着手順序

1. **QW-A1 → QW-A2** (3h) -- 最小労力で視覚的インパクト最大
2. **QW-B2** (2h) -- 既存コードの小修正で生態系リアリティ向上
3. **QW-C1 → QW-C2** (3h) -- データ構造だけ先に定義
4. **QW-D1 → QW-D4** (5h) -- 破壊可能テレインのプロトタイプ
5. 残りは Phase D 最適化と並行して進行

合計: 約30時間（全 QW 実施時）
