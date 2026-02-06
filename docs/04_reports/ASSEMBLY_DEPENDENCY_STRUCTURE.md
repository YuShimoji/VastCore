# Vastcore アセンブリ依存関係構造

## 修正後の依存関係階層

```
Vastcore.Utils (基盤レイヤー)
├── 機能: ログ、診断、ヘルパー
└── 依存: なし

Vastcore.Core (コアレイヤー)
├── 機能: システム管理、エラーハンドリング、デバッグ
└── 依存: Utils

Vastcore.Player (プレイヤーレイヤー)
├── 機能: プレイヤーコントローラー、移動、インタラクション
└── 依存: Core, Utils

Vastcore.Terrain (地形レイヤー)
├── 機能: 地形生成、キャッシュ、GPU処理、最適化
└── 依存: Core, Utils, Unity.ProBuilder, Unity.TextMeshPro

Vastcore.Camera (カメラレイヤー)
├── 機能: カメラコントローラー、シネマティック
└── 依存: Core, Utils, Player

Vastcore.Game (ゲーム管理レイヤー)
├── 機能: ゲーム全体の管理、統合
└── 依存: Core, Utils, Player, Terrain, Camera

Vastcore.Editor.StructureGenerator (エディターレイヤー)
├── 機能: エディター拡張、構造生成
└── 依存: Core, Utils, Terrain, Unity.ProBuilder系, Deform, Parabox.CSG
```

## 修正内容

### 循環依存の解決
- **問題**: Terrain ↔ Player の循環依存
- **解決**: Terrain から Player への参照を削除

### フォルダ構造の整理
- スクリプトを機能別に分類
- 名前空間を統一
- 新しいアセンブリ定義を作成

### アセンブリファイルの整理
- 不正な `Generation.asmdef` を削除
- 新規作成: `Vastcore.Camera.asmdef`, `Vastcore.Game.asmdef`
- 依存関係を明確化

## テスト手順
1. Unityエディターでプロジェクトを開く
2. コンパイルエラーがないことを確認
3. 各システムが正常に動作することを確認
