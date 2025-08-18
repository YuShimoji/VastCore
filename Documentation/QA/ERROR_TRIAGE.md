# エラートリアージ: 大量読み込みに起因する停止/エラー

## 概要（Incident Summary）
- 症状: 「読み込み量が多すぎてエラーが出て停止」
- 影響範囲: 実行/エディタ動作が停止 or 強制中断。関連機能は不明（暫定）。
- 発生頻度: 未計測（初回報告）。
- 優先度: 高（作業継続不能のため）。

## エラースナップショット（Error Snapshot）
- Unity Console/ログ抜粋:
  - 例1: `OutOfMemoryException` / `ArgumentException: Getting control X's position...` / `JobTempAlloc has allocations that are more than 4 frames old` など
  - 例2: `Resources.LoadAll` / 大量の `Instantiate` / 巨大配列・メッシュ結合 直後に停止
- スタックトレース（貼り付け欄）:
```
[ここに実際のエラー/警告ログを貼り付け]
```

## 再現手順（Reproduction Steps）
1. [シーン名/手順] を開く。
2. [ボタン/メニュー/スクリプト入口] を実行する。
3. 数十秒以内にコンソールにエラーが出力され、エディタが停止/重くなる。

※ 本欄は実測で更新する。判明次第、具体化。

## 実行環境（Environment）
- Unity: 6.0.0.29f1（`FUNCTION_TEST_STATUS.md`に準拠）
- OS: Windows
- プロジェクト: Vastcore（本リポジトリ）

## 原因仮説（Suspected Causes）
- 大量同時ロード/生成
  - `Resources.LoadAll<T>()` で一括ロードしている。
  - 大量 `Instantiate` により GC/メモリ/CPUがスパイク。
- メッシュ/コライダー結合の巨大一括処理
  - `CombineMeshes`/`MeshCollider` の一度きり結合でピークメモリ超過。
- テクスチャ/ハイトマップの高解像度一括展開
  - 圧縮非適用/Readable有効のまま多重読込。
- Addressables/非同期未利用
  - 同期I/Oでフリーズ。

## 即時の緩和策（Immediate Mitigations）
- バッチ分割: 大量処理を N 個ずつにし、`yield return null`/`await` でフレーム分散。
- 遅延ロード: 初期化時に全てを読まない。必要時にロード。
- 非同期化: `Addressables.LoadAssetAsync`/`Resources.LoadAsync` の活用。
- 設定見直し: 読み込み対象の `Readable`/解像度/圧縮を適正化。
- サンプリング削減: ツリー/ディテール/装飾の同時生成数に上限。
 - コルーチンのコールバック化: `PrimitiveErrorRecovery` にて無効な `yield return` の使用を廃止し、結果は `Action` コールバックで受け取る（無限リトライ/スパム防止）。

## 恒久対応計画（Deep Fix Plan）
- ローダー分離: ロード責務を専用モジュールへ分離し、キューイング+スロットリング（最大同時数 K）。
- メモリプロファイル: Unity Profiler で GC Alloc/Peak Mem を計測し、原因アセット/関数を特定。
- アセット最適化: テクスチャの圧縮/ミップマップ/解像度調整、Mesh の Read/Write 無効化。
- 生成パイプライン: メッシュ結合の段階処理化、コライダー生成の後回し/簡易化。
- アセット管理: Addressables 化（依存関係/リモート可能性含む）。

## 影響コード/箇所（候補）
- `Assets/Scripts/Generation/Map/` 周辺（構造物生成/メッシュ結合/タグ付与）。
  - `ArchitecturalGenerator.cs` 内のコライダー統合
    - `CombineMeshesForCollider(parent, meshCollider);` 呼び出し（付近行: 1016）
    - `private static void CombineMeshesForCollider(GameObject parent, MeshCollider collider)` 定義（付近行: 1022）
    - `combinedMesh.CombineMeshes(combines);` → `collider.sharedMesh = combinedMesh;`（付近行: 1034-1036）
  - `CompoundArchitecturalGenerator.cs` のメッシュ結合
    - `combinedMesh.CombineMeshes(combines);` → `collider.sharedMesh = combinedMesh;`（付近行: 1218-1219）
- Terrain 系（`TerrainGenerator`/Detail/Tree）: 高解像度の一括適用がスパイク要因
  - `Assets/MapGenerator/Scripts/TerrainGenerator.cs`
    - `terrainData.SetHeights(0, 0, heights);`（付近行: 105）
  - `Assets/MapGenerator/Scripts/Editor/HeightmapTerrainGeneratorWindow.cs`
    - `terrainData.SetHeights(0, 0, combinedHeightmap);`（付近行: 228-229）
    - `terrainData.SetAlphamaps(0, 0, alphaMap);`（付近行: 393-394）
  - `Assets/MapGenerator/Scripts/TextureGenerator.cs`
    - `terrainData.SetAlphamaps(0, 0, splatmapData);`（付近行: 88-89）
- 大量 `Instantiate` 発生箇所（代表例）
  - `Assets/Scripts/VastcoreGameManager.cs`（プレイヤー/カメラ生成、付近行: 208-226）
  - `Assets/Scripts/Player/EnhancedTranslocationSystem.cs`（投射体/インジケータ生成、付近行: 131, 369-370）
  - `Assets/Scripts/Generation/Map/AdvancedTerrainAlgorithmsTest.cs`（可視化生成、付近行: 233）
  - `Assets/Scripts/Generation/Map/PrimitiveTerrainObjectPool.cs`（プール初期生成、付近行: 151）
- `Resources/` 直下の一括ロード呼び出し有無（現状 `Resources.LoadAll` の直接使用は未検出）
 - `Assets/Scripts/Generation/PrimitiveErrorRecovery.cs`（エラー復旧ロジック: 位置再試行/フォールバック生成/ログ）

## 計測項目（Measurements）
- 起動〜停止までの時間（秒）
- 直前フレームの GC Alloc（KB/MB）
- Peak Memory（MB）/ VRAM（MB）
- 同時生成オブジェクト数 / メッシュ結合対象数

## 検証・テスト手順（Validation/Test Steps）
1. プロファイラ有効化（Deep Profileは必要時のみ）。
2. テレイン適用スパイクの計測
   - `SetHeights` 実行フレームの CPU/GPU/GC Alloc を記録（対象: 上記3箇所）
   - `SetAlphamaps` 実行フレームの CPU/GC Alloc を記録（対象: 上記2箇所）
3. 構造物結合スパイクの計測
   - `combinedMesh.CombineMeshes` 実行直後のメモリピーク/フリーズ有無を記録（対象: 上記2箇所）
4. フレーム分散（バッチ処理）版に切替 → 同条件で再実行。
5. Addressables/非同期版に切替 → 再実行。
6. テクスチャ/メッシュ最適化（Readable/圧縮）適用 → 再実行。
7. 各段階でエラー消失/時間短縮/ピークメモリ低下を記録。
8. `PrimitiveErrorRecovery` の検証（エディタ再生）
   - 衝突しやすい配置条件でプリミティブ生成を行い、復旧が有限回で収束することを確認。
   - 例外が 0 件であること、ログが秒間スパムにならないことを確認（試行回数/結果が要点のみ出力）。

## 進行状況（Owner/Status）
- Owner: 未割当
- 現状: 重負荷APIの実在箇所を特定・記録済み（上記参照）
- 次アクション:
  - Unity Console の実エラーログ貼付
  - 再現手順の具体化（対象メソッドをピンポイントで実行）
  - バッチ/非同期化のプロトタイプ実装と比較計測
  - `PrimitiveErrorRecovery` 修正後の挙動確認（無限リトライ/ログスパムが解消されていることの実測）

## 参考/リンク
- `DEV_LOG.md`（本件の作業ログ）
- `FUNCTION_TEST_STATUS.md`（検証項目・結果集約）
