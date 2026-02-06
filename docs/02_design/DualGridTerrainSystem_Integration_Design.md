# Dual Grid Terrain System - 既存システム統合設計

**Version:** 0.1 (Draft)  
**Target:** Unity / C#  
**Last Updated:** 2026-01-12  
**Status:** 設計段階（実装未着手）

---

## 1. 概要

既存の2Dハイトマップシステム（`TerrainGenerator` / `HeightMapGenerator`）とDual Grid Systemを統合し、ユーザーが両方のシステムを選択・併用できるようにする設計書。

---

## 2. 統合方針

### 2.1 基本方針

- **並行運用**: 既存の2Dハイトマップシステムは維持し、Dual Grid Systemは別コンポーネントとして追加
- **共通インターフェース**: 両システムが `TerrainGenerationProfile` を参照可能にする（将来的）
- **エディタ統合**: `TerrainGenerationWindow` に「3D Dual Grid Mode」タブを追加（v0は2D専用のまま）

### 2.2 統合の利点

1. **既存資産の活用**: 既存の高さマップ生成ロジックをDual Grid Systemでも活用可能
2. **段階的移行**: 既存システムから新システムへの移行を段階的に実施可能
3. **ユーザー選択**: 用途に応じて2D/3D Dual Gridを選択可能

---

## 3. 統合ポイント

### 3.1 TerrainGenerationProfile の拡張

`TerrainGenerationProfile` に `DualGridSettings` を追加し、Dual Grid Systemの設定を管理する。

```csharp
[System.Serializable]
public class DualGridTerrainSettings
{
    [Header("Grid Settings")]
    public bool UseDualGridMode = false;
    public int GridRadius = 3;
    public int Seed = 42;
    public float JitterAmount = 0.3f;
    public bool UsePerlinNoise = true;
    
    [Header("Height Settings")]
    public int MaxHeight = 5;
    public bool UseHeightMap = false;
    public Texture2D HeightMap;
    public float HeightMapScale = 1.0f;
    public float HeightMapOffset = 0.0f;
    
    [Header("Visualization Settings")]
    public bool ShowNodes = true;
    public bool ShowEdges = true;
    public bool ShowCells = true;
    public bool ShowVerticalStacks = true;
}
```

### 3.2 TerrainGenerationWindow の拡張

`TerrainGenerationWindow` に「3D Dual Grid Mode」タブを追加し、Dual Grid Systemの設定を調整可能にする。

**実装方針**:
- 既存の「2D Mode」タブは維持（v0は2D専用のまま）
- 新規「3D Dual Grid Mode」タブを追加
- タブ切替で2D/3D Dual Gridを選択可能

### 3.3 高さマップの共有

既存の `HeightMapGenerator.GenerateFromHeightMap` の結果をDual Grid Systemの高さ生成に活用する。

**実装方針**:
- `VerticalExtrusionGenerator.GenerateFromHeightMap` で既存の高さマップを参照可能
- `TerrainGenerationProfile` の高さマップ設定をDual Grid Systemでも使用可能

---

## 4. 実装フェーズ

### Phase 1: 統合設計の確定 ✅ 完了

- [x] 統合方針の決定
- [x] 統合ポイントの特定
- [x] 設計書の作成

### Phase 2: TerrainGenerationProfile の拡張（将来実装）

- [ ] `DualGridTerrainSettings` クラスの実装
- [ ] `TerrainGenerationProfile` に `DualGridSettings` を追加
- [ ] 既存機能への影響確認

### Phase 3: TerrainGenerationWindow の拡張（将来実装）

- [ ] 「3D Dual Grid Mode」タブの追加
- [ ] Dual Grid Systemの設定UI実装
- [ ] タブ切替機能の実装

### Phase 4: 高さマップの共有（将来実装）

- [ ] `VerticalExtrusionGenerator` で既存の高さマップを参照可能にする
- [ ] `TerrainGenerationProfile` の高さマップ設定をDual Grid Systemで使用可能にする

---

## 5. 制約事項

### 5.1 既存システムへの影響

- **破壊的変更の禁止**: 既存の2Dハイトマップシステムには一切変更を加えない
- **後方互換性の維持**: 既存の `TerrainGenerationProfile` はそのまま動作することを保証

### 5.2 実装上の制約

- **Unity Editor依存**: `TerrainGenerationWindow` はEditorOnlyコード（`#if UNITY_EDITOR`）で分離
- **ScriptableObject**: `TerrainGenerationProfile` はScriptableObjectとして実装

---

## 6. 関連タスク

- **TASK_013**: Dual Grid Terrain System - Phase 1 実装（完了）
- **TASK_014（将来）**: Dual Grid Terrain System - Phase 2 実装（メッシュ生成）
- **TASK_015（将来）**: Dual Grid Terrain System - 既存システム統合

---

## 7. 備考

- 本設計書は実装の進行に合わせて更新される
- Phase 2以降の実装完了後に、統合の優先順位を再評価する
- 既存システムとの統合は、Dual Grid SystemのPhase 2完了後に着手することを推奨
