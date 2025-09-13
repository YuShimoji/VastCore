# VastCore 新規プロジェクト移設計画書
日付: 2025-09-13

## 決定事項: 新規プロジェクトへの完全移設

### 判断根拠
1. **技術的負債**: 162ファイルにハードコーディングされた数値（総計3000箇所以上）
2. **依存関係の破綻**: 削除ファイルへの参照が24箇所
3. **設計の根本的欠陥**: 関心の分離なし、密結合
4. **改修コスト > 新規作成コスト**

## Unity Version Control (Plastic SCM) への移行を推奨

### 推奨理由
| 項目 | GitHub | Unity Version Control |
|------|--------|----------------------|
| Unity統合 | 外部ツール必要 | エディタ内完結 |
| 大容量ファイル | LFS制限あり | 制限なし |
| .metaファイル | 競合多発 | 自動処理 |
| シーンマージ | 困難 | 専用ツール |
| 月額コスト | 無料〜 | 3人まで無料 |

## 移設対象ファイル（必要最小限）

### コアシステム（8ファイル）
```
Assets/Scripts/
├── Core/
│   ├── VastcoreLogger.cs          # ログシステム
│   └── SaveSystem.cs               # セーブ機能
├── Player/
│   ├── Controllers/
│   │   └── PlayerController.cs    # 基本移動
│   └── Camera/
│       └── CameraController.cs    # カメラ制御
├── Generation/
│   ├── NoiseGenerator.cs          # ノイズ生成
│   └── MeshGenerator.cs           # メッシュ生成
└── UI/
    └── UIManager.cs                # UI管理
```

### 設定ファイル（必須）
```
ProjectSettings/
├── InputManager.asset
├── TagManager.asset
└── Physics.asset
```

## 破棄するファイル（154ファイル）
- すべてのテストファイル
- ハードコーディングが多いファイル
- 実験的機能
- 不完全な実装
- 古いドキュメント

## 新規プロジェクト構造

```
VastCore-Clean/
├── Assets/
│   ├── _Project/           # プロジェクト固有
│   │   ├── Scripts/
│   │   ├── Prefabs/
│   │   └── Materials/
│   ├── Settings/           # 設定ファイル
│   └── ThirdParty/        # 外部アセット
├── Documentation/
│   ├── Specifications/    # 仕様書
│   ├── Architecture/      # 設計書
│   └── API/              # API仕様
└── Tests/                 # テストコード
```

## 移設手順

### Phase 1: 環境準備（Day 1）
1. 新規Unityプロジェクト作成
2. Unity Version Control設定
3. プロジェクト構造作成
4. .gitignore設定

### Phase 2: コア移植（Day 2）
1. 必須8ファイルをコピー
2. 名前空間を統一
3. 依存関係を解決
4. コンパイル確認

### Phase 3: 機能実装（Day 3-5）
1. 地形生成システム（新規設計）
2. バイオームシステム（シンプル版）
3. UIシステム（モダン設計）

### Phase 4: 品質保証（Day 6-7）
1. ユニットテスト作成
2. 統合テスト実施
3. パフォーマンス測定
4. ドキュメント作成

## 成功基準
- [ ] コンパイルエラー: 0
- [ ] 警告: 10個以下
- [ ] ハードコーディング: 0
- [ ] テストカバレッジ: 80%以上
- [ ] FPS: 60以上
- [ ] メモリ使用量: 1GB以下

## リスクと対策
| リスク | 対策 |
|--------|------|
| 機能の欠落 | 段階的実装、優先順位付け |
| 性能低下 | プロファイリング、最適化 |
| バグの混入 | TDD、CI/CD |

## 次のアクション
1. このドキュメントの承認
2. Unity Version Controlアカウント作成
3. 新規プロジェクト作成開始
