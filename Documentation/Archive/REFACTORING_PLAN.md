# Vastcore プロジェクト構造リファクタリング計画

## 現在の問題点
- スクリプトがルートディレクトリに散在している
- 機能別の整理が不十分
- 重複・不要ファイルが存在
- フォルダ構造が複雑で探しにくい

## 新しいフォルダ構造設計

### 提案する構造
```
Assets/Scripts/
├── Core/                    # コアシステム（既存）
│   ├── VastcoreSystemManager.cs
│   ├── VastcoreLogger.cs
│   └── ...
├── Player/                  # プレイヤー関連（既存）
│   ├── Controllers/         # プレイヤーコントローラー
│   ├── Interaction/         # インタラクションシステム
│   └── Movement/           # 移動システム
├── Terrain/                 # 地形生成（既存）
│   ├── Generation/         # 地形生成
│   ├── Cache/              # キャッシュシステム
│   ├── GPU/                # GPU処理
│   └── Optimization/       # 最適化
├── Camera/                  # カメラシステム（新規）
│   ├── Controllers/
│   └── Cinematic/
├── UI/                      # UIシステム（既存）
│   ├── Menus/
│   ├── HUD/
│   └── Debug/
├── Game/                    # ゲーム管理（新規）
│   ├── Managers/
│   └── States/
├── Testing/                 # テストシステム（既存）
│   ├── Unit/
│   ├── Integration/
│   └── Performance/
└── Utils/                   # ユーティリティ（既存）
    ├── Extensions/
    ├── Helpers/
    └── Diagnostics/
```

## 移動対象ファイル

### ルートから移動するファイル
- `CameraController.cs` → `Camera/Controllers/`
- `CinematicCameraController.cs` → `Camera/Cinematic/`
- `PlayerController.cs` → `Player/Controllers/`
- `VastcoreGameManager.cs` → `Game/Managers/`
- `TitleScreenManager.cs` → `UI/Menus/`

### Player フォルダ内の整理
- コントローラー系を `Controllers/` に
- インタラクション系を `Interaction/` に
- 移動システムを `Movement/` に

### Terrain フォルダ内の整理
- 既存のサブフォルダ構造を維持
- 重複ファイルの統合

## 実装手順
1. 新しいフォルダ構造を作成
2. ファイルを段階的に移動
3. 名前空間を更新
4. アセンブリ参照を調整
5. 重複・不要ファイルを削除
6. 最終コンパイル確認
