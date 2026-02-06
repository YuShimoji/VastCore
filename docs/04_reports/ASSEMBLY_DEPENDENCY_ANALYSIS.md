# アセンブリ依存関係分析

## 現在の依存関係マップ

```
Vastcore.Utilities (基盤)
  └── (依存なし)

Vastcore.Core
  └── Vastcore.Utilities

Vastcore.Generation
  ├── Vastcore.Core
  ├── Vastcore.Utilities
  ├── Vastcore.Terrain ⚠️ 循環の原因
  └── Unity.ProBuilder

Vastcore.Player
  ├── Vastcore.Core
  ├── Vastcore.Utilities
  └── Vastcore.Generation ⚠️ 循環の原因

Vastcore.Terrain
  ├── Unity.ProBuilder
  ├── Unity.TextMeshPro
  ├── Vastcore.Core
  ├── Vastcore.Utilities
  └── Vastcore.Player ⚠️ 循環の原因

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

## 検出された循環依存

### 循環 1: Generation → Terrain → Player → Generation
```
Vastcore.Generation → Vastcore.Terrain
Vastcore.Terrain → Vastcore.Player
Vastcore.Player → Vastcore.Generation
```

## 解決策

### オプション A: Player の Generation 依存を削除（推奨）
- `Vastcore.Player.asmdef` から `Vastcore.Generation` 参照を削除
- Player は Generation に依存すべきではない（レイヤー違反）
- もし必要なら、イベントシステムやインターフェースで疎結合化

### オプション B: Generation の Terrain 依存を削除
- `Vastcore.Generation.asmdef` から `Vastcore.Terrain` 参照を削除
- 型参照エラーが発生する場合は、共通インターフェースを Core に移動

### オプション C: Terrain の Player 依存を削除
- `Vastcore.Terrain.asmdef` から `Vastcore.Player` 参照を削除
- Terrain が Player に依存するのは設計的に疑問

## 推奨アーキテクチャ

```
Layer 0: Utilities (基盤ユーティリティ)
Layer 1: Core (共通インターフェース・データ構造)
Layer 2: Generation, Terrain (独立したサブシステム)
Layer 3: Player, Camera (ゲームロジック)
Layer 4: UI (ユーザーインターフェース)
Layer 5: Game (統合レイヤー)
```

### 依存ルール
- 上位レイヤーは下位レイヤーに依存可能
- 下位レイヤーは上位レイヤーに依存不可
- 同一レイヤー間の依存は最小化
