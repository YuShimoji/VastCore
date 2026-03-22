# SP-018: Parametric Variation (V1)

> 上位SSOT: [SSOT_WORLD.md](../SSOT_WORLD.md)
> 関連: SP-010 (Prefab Stamp Placement), SP-017 (Stamp Export Pipeline)

## Status: PARTIAL (Unity実機検証待ち)

## 概要

PrefabStampDefinition に変異パラメータを追加し、同じ Definition から配置された
複数スタンプが個体差を持つようにする。V1 は最小限のパラメトリック変異を実装する。

## 設計方針

- T1 (オーサリング主体) 前提: デザイナーが Inspector で変異範囲を設定
- V4 (段階的) の第1段階: 後方互換を維持し、既存の ScaleRange/RotationMode に追加
- シード駆動: System.Random で決定論的再現性を保証

## 変異パラメータ

### 既存 (変更なし)

| パラメータ | 型 | 説明 |
|-----------|-----|------|
| ScaleRange | Vector2 | (min, max) の均一スケール |
| RotationMode | enum | Fixed / Step90 / Free |

### V1 追加

| パラメータ | 型 | デフォルト | 説明 |
|-----------|-----|-----------|------|
| PositionJitter | float | 0f | XZ平面での位置ずれ半径 (セル内、ワールド単位) |
| MaterialVariants | Material[] | empty | ランダム選択されるマテリアル候補。空の場合はPrefab既定 |
| ChildToggleGroups | string[] | empty | 表示/非表示を切り替える子オブジェクト名。各配置で1つをランダム表示 |

### PositionJitter

- セル中心からの XZ オフセット
- `_random.NextDouble() * 2π` で角度、`_random.NextDouble() * PositionJitter` で距離
- 0 の場合はオフセットなし (後方互換)

### MaterialVariants

- 空の場合: Prefab 既定のマテリアルを使用 (後方互換)
- 1つ以上の場合: インスタンス化後に MeshRenderer.sharedMaterial を差し替え
- 対象: ルートの MeshRenderer (子は対象外。将来拡張可)

### ChildToggleGroups

- 空の場合: 全子オブジェクトを表示 (後方互換)
- 1つ以上の場合: 全グループを非表示にし、ランダムに1つだけ表示
- 名前が見つからない場合は警告ログを出して全表示にフォールバック
- 用途: 窓の有無、屋根の形状差、装飾パーツの差替

## 実装計画

### Step 1: PrefabStampDefinition 拡張
- `m_PositionJitter`, `m_MaterialVariants`, `m_ChildToggleGroups` フィールド追加
- 対応する public property 追加
- `GetRandomPositionOffset(System.Random)` メソッド追加
- `GetRandomMaterial(System.Random)` メソッド追加
- `GetRandomChildToggleIndex(System.Random)` メソッド追加

### Step 2: PrefabStampPlacer 変異適用
- `Instantiate` メソッド内で変異を適用:
  - position += positionOffset
  - material 差替 (MaterialVariants が空でない場合)
  - child toggle (ChildToggleGroups が空でない場合)

### Step 3: EditMode テスト
- GetRandomPositionOffset の範囲検証
- GetRandomMaterial の選択検証
- GetRandomChildToggleIndex の範囲検証
- 後方互換: デフォルト値で従来動作が変わらないこと

### Step 4: StampExporter 連携 (実装済み)
- Export 時に Prefab 直接子オブジェクトの MeshRenderer/MeshFilter 持ちを自動検出
- 2つ以上の候補がある場合のみ ChildToggleGroups に設定
- DetectChildToggleCandidates メソッド追加

### Step 5 (将来): RandomControlTab 連携
- RandomControlTab の位置/回転/スケール設定値を PrefabStampDefinition に転写
- Export ワークフロー内で「RandomControl の設定を含める」オプション

## 受け入れ条件

1. 既存テスト (PrefabStampTests) が全て通る (後方互換)
2. PositionJitter > 0 のとき、配置位置がセル中心からずれる
3. MaterialVariants に2つ以上のマテリアルがあるとき、複数配置で異なるマテリアルが選ばれる
4. ChildToggleGroups に2つ以上の名前があるとき、配置ごとに異なる子が表示される
5. 同一シードで同一結果が再現される
