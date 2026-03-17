# VastCore Terrain Engine — クイックスタート検証ガイド

## 前提

Phase C 実装完了後の動作検証手順。全アセットを一括生成し、各機能を確認する。

## Step 1: ブートストラップアセット生成

1. Unity Editor を開く
2. メニュー: **Vastcore > Bootstrap > Create All Required Assets**
3. `Assets/Resources/Bootstrap/` に以下が生成される:

| アセット | 用途 |
|---------|------|
| NoiseHeightmap_Default | FBM ノイズによるハイトマップ設定 |
| Erosion_Default | 水力+熱エロージョン設定 |
| TerrainConfig_Default | 地形生成統合設定 (Heightmap + Erosion) |
| Stamp_Cube | テスト用 Cube スタンプ定義 |
| StampPrefab_Cube | テスト用 Cube Prefab |
| Player_Minimal | 最小プレイヤー (Capsule + カメラ + CharacterController) |

## Step 2: 基本地形生成 (TerrainGridBootstrap)

1. 新しいシーンを作成
2. 空の GameObject を作成し `TerrainGridBootstrap` を追加
3. Inspector で `Config` に **TerrainConfig_Default** をアサイン
4. `Grid X` = 2, `Grid Z` = 2 (2x2 チャンク)
5. Play → **4枚の Unity Terrain チャンクにエロージョン済み地形が表示される**

### 確認ポイント
- 地形が生成されるか
- エロージョンの効果が見えるか (谷筋・尾根の形成)
- Erosion_Default の `Enabled` を false にして再生成し、差を比較

## Step 3: 統合テスト (TerrainWithStampsBootstrap)

1. 空の GameObject に `TerrainWithStampsBootstrap` を追加
2. Inspector で:
   - `Config` → **TerrainConfig_Default**
   - `Stamp Definition` → **Stamp_Cube**
   - `Auto Place Probability` → 0.3
   - `Grid X` = 2, `Grid Z` = 2
   - `Dual Grid Radius` = 3
3. Play → **地形 + DualGrid セル上に Cube が散布される**

### 確認ポイント
- Cube が地形表面に接地しているか (IHeightSampler 動作)
- 自動配置がランダムに散布されるか
- Stamp Seed を変えると配置パターンが変わるか

## Step 4: エロージョンプレビュー (ErosionPreview)

1. 空の GameObject に `ErosionPreview` を追加 (MeshFilter + MeshRenderer 自動追加)
2. MeshRenderer のマテリアルを適当な Lit マテリアルに設定
3. Play → **エロージョン適用済みのプロシージャル地形メッシュが表示される**

### 確認ポイント
- メッシュが表示されるか
- `Enable Hydraulic` / `Enable Thermal` を切り替えて差を確認
- `Erosion Rate` / `Talus Angle` を変えて効果の強弱を確認

## Step 5: DualGrid 可視化 (GridDebugVisualizer)

1. 空の GameObject に `GridDebugVisualizer` を追加
2. **Scene View** で確認 (Game View には表示されない — Gizmo 描画)
3. Inspector で `Test Stamp Definition` に **Stamp_Cube** をアサイン
4. `Test Stamp Cell Ids` に [0, 1, 2] 等を設定
5. Scene View に **ノード (黄) + エッジ (白) + セル (赤) + スタック (緑) + スタンプ (青)** が表示

### 確認ポイント
- グリッド構造が六角形ベースで表示されるか
- スタンプ Gizmo が指定セルに表示されるか
- 占有セルがオレンジでハイライトされるか

## Step 6: GameManager 起動 (VastcoreGameManager)

1. 空の GameObject に `VastcoreGameManager` を追加
2. Inspector で:
   - `Player Prefab` → **Player_Minimal**
   - `Skip Intro Cinematic` → true (Cinematic Prefab がないため)
3. 別の GameObject に `TerrainFacade` を追加し `Classic Config` に **TerrainConfig_Default** をアサイン
4. Play → **プレイヤーが生成され、地形がストリーミング開始される**

### 確認ポイント
- プレイヤーが m_DefaultSpawnPosition (0, 50, 0) に出現するか
- コンソールに GameSequence Complete が表示されるか
- WASD で移動できるか

## Step 7: StructureGenerator (PC-2/PC-3)

1. メニュー: **Window > Vastcore > Structure Generator** (存在する場合)
2. **Basic Shapes** タブで Arch / Pyramid を選択して Generate
3. **Composition** タブで 2つの形状を選択し Layered Blend を実行

### 確認ポイント
- Arch が ProBuilder メッシュとして生成されるか
- Pyramid が手動メッシュとして生成されるか
- Layered Blend が 2 メッシュを頂点補間するか

## トラブルシューティング

| 症状 | 原因 | 対処 |
|------|------|------|
| 地形が表示されない | Config が null | Bootstrap Assets を生成し直す |
| スタンプが浮いている | IHeightSampler 未接続 | TerrainWithStampsBootstrap を使う |
| Player が出現しない | PlayerPrefab 未アサイン | Player_Minimal をアサイン |
| Gizmo が見えない | Game View で確認している | Scene View に切り替え |
| エロージョンの効果がない | ErosionSettings.enabled = false | Inspector で true にする |
