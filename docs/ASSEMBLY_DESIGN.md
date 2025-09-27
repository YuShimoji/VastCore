# Vastcore Assembly Design

本ドキュメントは、Vastcore プロジェクト内の Assembly Definition (.asmdef) の依存関係設計を示します。

## 依存関係図（Mermaid）

```mermaid
graph TD
  subgraph CoreLayer
    Utils["Vastcore.Utils"]
    Core["Vastcore.Core\n→ Utils"]
  end

  subgraph FeatureLayer
    Generation["Vastcore.Generation\n→ Core, Utils, Unity.ProBuilder"]
    Terrain["Vastcore.Terrain\n→ Core, Utils, Generation, Unity.ProBuilder, TMP"]
    Player["Vastcore.Player\n→ Core, Utils, Generation"]
    CameraAsm["Vastcore.Camera\n→ Core, Utils, Player, TMP, InputSystem"]
    GameAsm["Vastcore.Game\n→ Core, Utils, Player, Terrain, Camera"]
    UIAsm["Vastcore.UI\n→ Core, Utils, Player, TMP, Unity.Rendering.DebugUI"]
  end

  subgraph Testing
    TestingAsm["Vastcore.Testing\n→ Core, Utils, Generation, Terrain, Player, UI\noptionalUnityReferences: TestAssemblies"]
  end

  Utils --> Core
  Core --> Generation
  Core --> Terrain
  Core --> Player
  Core --> CameraAsm
  Core --> GameAsm
  Core --> UIAsm

  Utils --> Generation
  Utils --> Terrain
  Utils --> Player
  Utils --> CameraAsm
  Utils --> GameAsm
  Utils --> UIAsm

  Generation --> Terrain
  Player --> CameraAsm
  Player --> GameAsm
  Terrain --> GameAsm
  CameraAsm --> GameAsm

  Core --> TestingAsm
  Utils --> TestingAsm
  Generation --> TestingAsm
  Terrain --> TestingAsm
  Player --> TestingAsm
  UIAsm --> TestingAsm
```

## 設計方針
- Core/Utils をボトムレイヤーに固定し、上位モジュールからのみ参照（DIP）。
- Generation/Terrain/Player/Camera/UI は機能領域ごとに分割（SRP, SoC）。
- Game はエントリポイントの統合層として各機能を参照。
- Testing は実行時テスト専用。`optionalUnityReferences: ["TestAssemblies"]` を付与済み。

## メモ
- Deform（外部パッケージ）統合時は、各 asmdef に対し defineConstraints と可用性を調整し、`DEFORM_AVAILABLE` を正しく尊重する。
- URP/TMP/InputSystem/DebugUI などの Unity パッケージは、利用箇所のみに限定。
