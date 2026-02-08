# 循環依存エラー詳細レポート

## 検出日時
2025-11-11 14:54 (JST)

## 問題の概要
Unityで「One or more cyclic dependencies detected」エラーが継続して発生。
前回の修正後も解消されていない状態。

## 完全な依存関係マップ

### Runtime Assemblies

```
Vastcore.Utilities
  └── (依存なし)

Vastcore.Core
  └── Vastcore.Utilities

Vastcore.Generation
  ├── Vastcore.Core
  ├── Vastcore.Utilities
  └── Unity.ProBuilder

Vastcore.Terrain ⚠️ 循環の原因
  ├── Unity.ProBuilder
  ├── Unity.TextMeshPro
  ├── Vastcore.Core
  ├── Vastcore.Utilities
  └── Vastcore.Player ⚠️ 循環の原因

Vastcore.Player ⚠️ 循環の原因
  ├── Vastcore.Core
  ├── Vastcore.Utilities
  └── Vastcore.Terrain ⚠️ 循環の原因

Vastcore.Camera
  ├── Vastcore.Core
  ├── Vastcore.Utilities
  ├── Vastcore.Player
  ├── Unity.TextMeshPro
  └── Unity.InputSystem

Vastcore.UI
  ├── Vastcore.Core
  ├── Vastcore.Utilities
  ├── Vastcore.Player
  └── Unity.TextMeshPro

Vastcore.Game
  ├── Vastcore.Core
  ├── Vastcore.Utilities
  ├── Vastcore.Player
  ├── Vastcore.Terrain
  ├── Vastcore.Camera
  └── Vastcore.UI
```

### Editor Assemblies

```
Vastcore.Editor
  ├── Vastcore.Core
  ├── Vastcore.Utilities
  ├── Vastcore.Generation
  ├── Vastcore.Terrain
  └── Unity.TextMeshPro

Vastcore.Editor.StructureGenerator
  ├── Unity.ProBuilder (all modules)
  ├── Vastcore.Terrain
  ├── Vastcore.Core
  └── Vastcore.Utilities
```

### Test Assemblies

```
Vastcore.Testing
  ├── Vastcore.Core
  ├── Vastcore.Utilities
  ├── Vastcore.Generation
  ├── Vastcore.Terrain
  ├── Vastcore.Player
  ├── Vastcore.UI
  ├── Unity.ProBuilder (all modules)
  ├── Unity.TextMeshPro
  └── Unity.TestFramework

Vastcore.Tests.EditMode
  ├── UnityEngine.TestRunner
  ├── UnityEditor.TestRunner
  ├── Vastcore.Generation
  └── Vastcore.Testing
```

## 検出された循環依存

### 循環 1: Terrain ⇄ Player (直接循環) ⚠️ **重大**

```
Vastcore.Terrain → Vastcore.Player
Vastcore.Player → Vastcore.Terrain
```

**影響範囲**: すべてのRuntime/Editor/Testアセンブリに波及

## 根本原因分析

### Vastcore.Terrain.asmdef
- **問題**: Line 9 に `"Vastcore.Player"` 参照が存在
- **理由**: PlayerTrackingSystem等がPlayer型を参照している可能性

### Vastcore.Player.asmdef  
- **問題**: Line 7 に `"Vastcore.Terrain"` 参照が存在
- **理由**: PrimitiveTerrainObjectを参照するため

## 解決策

### 即時対応（Critical Fix）

**Step 1: Terrain → Player 依存を削除**

Vastcore.Terrain.asmdef から `"Vastcore.Player"` 参照を削除。

#### 影響を受けるファイルの修正が必要：
1. `PlayerTrackingSystem.cs` - AdvancedPlayerController参照
2. その他のTerrain内でPlayer型を参照しているファイル

#### 修正方法：
- Transform型に置き換え（Player特有の機能を使わない）
- インターフェース経由で疎結合化
- Core層に共通インターフェースを移動

### 根本的な設計修正

#### アーキテクチャ原則の徹底

```
Layer 0: Utilities (基盤)
Layer 1: Core (共通インターフェース・データ構造)
Layer 2-A: Generation (地形生成ロジック)
Layer 2-B: Terrain (地形管理・レンダリング)
Layer 3: Player, Camera (ゲームロジック)
Layer 4: UI (ユーザーインターフェース)
Layer 5: Game (統合レイヤー)
```

**依存ルール**:
- 同一レイヤー間の依存は禁止
- 上位レイヤーから下位レイヤーへの依存のみ許可
- 下位から上位への通信はイベント/インターフェースで実現

## 追加調査が必要な項目

1. PlayerTrackingSystemでのPlayer型使用状況
2. Terrain内の他のPlayer依存箇所
3. インターフェース抽出候補の特定

## 再コンパイル問題

Libraryキャッシュに古い依存関係が残っている可能性あり。

### 推奨対応：
1. アセンブリ定義修正後、Unity Editor再起動
2. `Library/ScriptAssemblies` フォルダ削除
3. `Library/Bee` フォルダ削除（Unity 2021以降）
4. Assets → Reimport All

## 優先度

- **P0 (Critical)**: Terrain.asmdef から Player参照削除
- **P1 (High)**: PlayerTrackingSystem等の修正
- **P2 (Medium)**: インターフェース設計の見直し
- **P3 (Low)**: Libraryキャッシュクリーンアップ

## 次のアクション

1. ✅ 完全な依存関係分析（完了）
2. ⏳ Terrain内のPlayer依存箇所を特定
3. ⏳ Terrain.asmdef修正
4. ⏳ 影響を受けるコード修正
5. ⏳ Libraryキャッシュクリーンアップ
6. ⏳ コンパイル確認
