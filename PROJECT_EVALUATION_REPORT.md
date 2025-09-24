# VastCore プロジェクト総合評価レポート
日付: 2025-09-13

## 1. 現状分析

### エラー状況
- **コンパイルエラー数**: 24個（削除ファイルへの参照）
- **主な原因**: 
  - PrimitiveTerrainRule（削除済み）への参照
  - CrystalStructureGenerator（削除済み）への参照
  - BiomeDefinition, BiomeTypeの未定義
  - アセンブリ定義ファイルの不備

### プロジェクト構造の問題
1. **依存関係の破綻**: 削除したファイルへの参照が多数残存
2. **アセンブリ定義の不備**: 必須プロパティ'name'が未設定
3. **メタファイルの破損**: GUIDが無効

## 2. 仕様満足度評価

### 基本要件の充足度
| 要件 | 現状 | 評価 |
|------|------|------|
| 地形生成 | 基本機能のみ | △ |
| プレイヤー制御 | 実装済み | ○ |
| バイオームシステム | 削除済み | × |
| LODシステム | 削除済み | × |
| セーブ/ロード | 基本実装あり | △ |

### 拡張性の評価
- **ハードコーディング**: 多数発見
  - マジックナンバーが散在
  - 設定値がコード内に直接記述
  - 依存関係が密結合

## 3. 判断：新規移設を推奨

### 理由
1. **技術的負債が膨大**
   - 削除したファイルへの参照が24箇所以上
   - 依存関係が複雑に絡み合っている
   - 修正より再構築の方が効率的

2. **設計の根本的問題**
   - 関心の分離ができていない
   - テストとプロダクションコードの混在
   - 名前空間の不整合

3. **ドキュメントの散逸**
   - 体系的でない作業記録
   - 仕様書の不在
   - 更新されていない設計書

## 4. 移設計画

### 新規プロジェクトで保持すべきコア
```
必須保持:
- PlayerController.cs
- CameraController.cs
- NoiseGenerator.cs
- SaveSystem.cs
- VastcoreLogger.cs

再設計が必要:
- TerrainGenerator.cs
- MeshGenerator.cs
- UIManager.cs
```

### 段階的移設手順
1. 新規Unityプロジェクト作成
2. 最小限のコア機能を移植
3. 適切なアーキテクチャで再構築
4. テスト駆動開発で品質保証

## 5. バージョン管理システムの推奨

### Unity Version Control (Plastic SCM) を推奨

#### 理由
1. **Unity統合**: エディタ内で完結
2. **大容量ファイル**: 3Dアセット、テクスチャに最適
3. **ブランチ管理**: ビジュアルで分かりやすい
4. **マージ処理**: Unity特有のファイル形式に対応

#### GitHub の問題点
- LFSの制限（1GB/月）
- .metaファイルの競合
- シーンファイルのマージ困難

## 6. 推奨アクション

### 即座に実施
1. 新規Unityプロジェクトの作成
2. Unity Version Controlのセットアップ
3. 最小限のコア機能の移植

### 破棄すべきもの
- 現在のプロジェクトの90%
- 体系化されていないドキュメント
- テストファイル（再設計後に新規作成）

## 7. ランタイム地形再設計の戦略と実施状況（2025-09-14）

### 7.1 目的
- エディタ依存の地形・巨大オブジェクト生成から脱却し、実行時（Playモード）における動的なタイル生成・破棄・LOD・メモリ管理を可能にする。
- アセンブリ循環（Terrain ↔ Player）や密結合を解消し、将来の機能拡張（GPU生成、プリロード、バイオーム等）に耐えるモジュール構造へ再設計する。

### 7.2 実施した修正（本セッション）
- アセンブリ定義の是正
  - `Assets/Scripts/Terrain/Vastcore.Terrain.asmdef` から `Vastcore.Player` 参照を削除（Terrain → Player 依存を解消）。
  - 空の `Assets/Scripts/Vastcore.Generation.asmdef` は削除予定（冗長・不正なasmdefの除去）。
- コードの依存除去（Player直参照 → タグ/カメラ参照）
  - `Assets/Scripts/Terrain/Map/TileManager.cs`: `Vastcore.Player.AdvancedPlayerController` 参照を排除し、`Player`タグ/`Camera.main`でプレイヤーTransform取得に変更。ファイル破損を復旧の上、クラス定義と初期化シーケンスを整備。
  - `Assets/Scripts/Terrain/Map/RuntimeTerrainManager.cs`: `using Vastcore.Player;` を削除。
  - `Assets/Scripts/Terrain/Map/PlayerTrackingSystem.cs`: `using Vastcore.Player;` と `AdvancedPlayerController` 参照を `Player`タグ/`Camera.main`に置換。ヘッダ/コメント整備。
  - `Assets/Scripts/Terrain/Map/TerrainTexturingSystem.cs`: 同様にPlayer参照を除去し、タグ/カメラ参照に置換。
  - `Assets/Scripts/Terrain/Cache/IntelligentCacheSystem.cs`: Player参照を除去。タグ/カメラでプレイヤーTransformを解決。
  - `Assets/Scripts/Terrain/Cache/TerrainCacheManager.cs`: 不要な `using Vastcore.Player;` を削除。

これにより、`Vastcore.Terrain` は `Vastcore.Core`/`Vastcore.Utils`/`Vastcore.Generation` のみに依存し、プレイヤー実装に依存しないランタイム生成基盤となった。循環参照の芽を事前に摘み、アーキテクチャ上の境界を強化。

### 7.3 目標アーキテクチャ（高レベル）
- 依存関係（方向）
  - `Utils` →（なし）
  - `Core` → `Utils`
  - `Generation` → `Core`, `Utils`
  - `Terrain` → `Generation`, `Core`, `Utils`
  - `Player` → `Core`, `Utils`
  - `Camera` → `Player`, `Core`, `Utils`
  - `Game` → `Terrain`, `Player`, `Camera`, `Core`, `Utils`
- 実行時の責務分担
  - `RuntimeTerrainManager`: 動的生成のオーケストレーション（要求生成/削除、フレーム時間制御、メモリ監視、簡易統計）
  - `TileManager`: アクティブタイルの辞書管理、座標変換、ロード/アンロード、LOD、メモリ見積
  - `*Cache*`: キャッシュ/プリロード（オプション、将来的にインターフェース化）

### 7.4 次の対応（ロードマップ）
1. アセンブリ/設定
   - `Assets/Scripts/Vastcore.Generation.asmdef` の削除（git管理からも除去）。
   - `ASSEMBLY_DEPENDENCY_STRUCTURE.md` を今回の実態に更新（Terrain→Player依存削除を明記）。
2. ランタイム生成品質
   - `TileManager` の個別タイル再ロードAPI整備（全削除フォールバックの解消）。
   - `RuntimeTerrainManager` の削除戦略最適化（距離/優先度/メモリ閾値による選択的削除）。
3. プレイヤー非依存化の徹底
   - Transform供給の抽象化（`IPlayerLocator`インターフェース）でタグやカメラ以外の供給にも対応。
4. GPU生成統合
   - `GPUTerrainGenerator` 経路のパラメータ/エラー処理の強化、CPUフォールバックの品質向上。

### 7.5 テスト計画（最小手順）
1. Unityを起動し、対象シーンを開く。
2. コンソールにエラーがないことを確認（asmdef循環/参照エラーが出ない）。
3. 空のGameObjectに `RuntimeTerrainManager` を付与し、`TileManager` が自動付与/連携されることを確認。
4. `Player`タグを持つオブジェクトがある場合はそれが、ない場合は `Main Camera` が追跡対象として機能することを確認。
5. Play開始後、移動に応じてタイルがロード/アンロードされ、メモリ警告時にクリーンアップが作動することを確認。
6. `TerrainTexturingSystem` を有効化してもプレイヤー参照解決が失敗しないこと（タグ/カメラで補完）を確認。
7. ログ（`VastcoreLogger`）にエラーが出力されないことを確認。

### 7.6 判定基準
- コンパイルエラー0（循環/参照不整合なし）。
- Playモードでタイル生成/削除が安定動作（ウォッチドッグ発火なし）。
- プレイヤー実装が未導入でも、カメラのみで最低限の動作確認が可能。
