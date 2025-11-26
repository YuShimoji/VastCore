# Phase 2: Template 系統合仕様書

## 概要

V01 地形生成システムと DesignerTerrainTemplate 系の統合ポイントを整理し、
Phase 2 での実装方針を定義する。

---

## 現状分析

### V01 系（単一タイル生成）

| コンポーネント | 役割 |
|---------------|------|
| TerrainGenerationWindow | Editor UI |
| TerrainGenerationProfile | パラメータ保存 |
| TerrainGenerator | 生成実行 |
| HeightMapGenerator | 高さマップ生成 |

### Template 系（高度テンプレート）

| コンポーネント | 役割 |
|---------------|------|
| TerrainTemplateEditor | Editor UI (754行) |
| DesignerTerrainTemplate | テンプレート定義 |
| TerrainEngine | 生成エンジン (730行) |
| BlendSettings | ブレンド設定 |

---

## 統合ポイント

### 1. Profile と Template の連携

```text
現状:
- TerrainGenerationProfile: ノイズ/HeightMap パラメータ
- DesignerTerrainTemplate: テンプレート + バイオーム + Deform

統合案:
TerrainGenerationProfile
    ├─ 基本パラメータ（現行）
    └─ [Optional] TemplateReference → DesignerTerrainTemplate
```

**実装方針**:

- `TerrainGenerationProfile` に `DesignerTerrainTemplate` 参照フィールドを追加
- Template が設定されている場合、Profile パラメータを上書き
- 段階的移行: 既存 Profile は Template なしで動作

### 2. Editor UI の統合

```text
現状:
- TerrainGenerationWindow: シンプル UI (503行)
- TerrainTemplateEditor: 高度 UI (754行)

統合案:
TerrainGenerationWindow
    ├─ Basic タブ（現行機能）
    └─ Template タブ（DesignerTerrainTemplate 選択・プレビュー）
```

**実装方針**:

- TerrainGenerationWindow に Template タブを追加
- TerrainTemplateEditor は独立した高度エディタとして維持
- 共通コンポーネント（プレビュー生成など）は再利用

### 3. 生成エンジンの統合

```text
現状:
- TerrainGenerator: V01 生成ロジック
- TerrainEngine: Template/Streaming 生成ロジック

統合案:
ITerrainGenerator (インターフェース)
    ├─ TerrainGenerator (V01)
    └─ TemplateTerrainGenerator (Template 系)

TerrainEngine (ファサード)
    └─ モードに応じて適切な Generator を選択
```

---

## Phase 2 タスク分解

### 2.1 Profile 拡張（優先度: 高）

```markdown
- [ ] TerrainGenerationProfile に TemplateReference フィールド追加
- [ ] Template からのパラメータ読み込みメソッド追加
- [ ] 既存 Profile との後方互換性確認
```

### 2.2 UI 拡張（優先度: 中）

```markdown
- [ ] TerrainGenerationWindow に Template タブ追加
- [ ] Template 選択 UI 実装
- [ ] プレビュー機能追加
```

### 2.3 生成エンジン抽象化（優先度: 低）

```markdown
- [ ] ITerrainGenerator インターフェース定義
- [ ] TerrainGenerator のインターフェース実装
- [ ] TemplateTerrainGenerator 新規作成
```

---

## BlendSettings 重複解消

### 現状

- `Vastcore.Generation.Map.BlendSettings` (TemplateBlendMode)
- `Vastcore.Terrain.Map.BlendSettings` (BlendMode)

### 解消案

```csharp
// 統合案: Vastcore.Generation.Map に統一
namespace Vastcore.Generation.Map
{
    public enum BlendMode
    {
        Additive,
        Multiplicative
    }

    [CreateAssetMenu]
    public class BlendSettings : ScriptableObject
    {
        public BlendMode blendMode = BlendMode.Additive;
        [Range(0f, 1f)] public float blendStrength = 1f;
        public float fadeDistance = 100f;
        public bool enableEdgeBlending = true;
        public float edgeBlendWidth = 10f;
    }
}

// Vastcore.Terrain.Map.BlendSettings → 削除または廃止
```

---

## 依存関係

```text
Phase 1.5 (完了)
    └─ V01 責務整理、テスト追加

Phase 2 (次フェーズ)
    ├─ 2.1 Profile 拡張
    ├─ 2.2 UI 拡張
    └─ 2.3 生成エンジン抽象化

Phase 2.5 (将来)
    └─ TerrainEngine Lite 統合
```

---

## リスクと対策

| リスク | 影響 | 対策 |
|--------|------|------|
| BlendSettings 変更による既存参照破損 | 高 | エイリアス + 段階的移行 |
| TerrainEngine の複雑性 | 中 | V01 との疎結合維持 |
| Editor UI 肥大化 | 低 | タブ分離、コンポーネント化 |

---

## 関連ドキュメント

- [Phase15_RuntimeRefactor_Design.md](./Phase15_RuntimeRefactor_Design.md)
- [V01_TestPlan.md](../terrain/V01_TestPlan.md)
- [TerrainGenerationV0_Spec.md](../terrain/TerrainGenerationV0_Spec.md)

---

- **作成日**: 2025-11-26
- **作成者**: Cascade (AI)
