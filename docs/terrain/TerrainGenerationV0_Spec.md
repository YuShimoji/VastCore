# Terrain Generation v0 仕様書

- **最終更新日時**: 2025-11-20
- **更新者**: Cascade (AI)
- **ステータス**: Active / Draft（HeightMap & Noise ベース単一タイル生成の仕様）

---

## 1. 目的と適用範囲

本ドキュメントは、VastCore プロジェクトにおける **Terrain Generation v0** の仕様を定義する。

- 対象: **HeightMap / Noise による単一 Terrain タイル生成**
- 非対象（将来フェーズ）:
  - Biome ベースの高度な TerrainEngine
  - Deform や高度構造物連携
  - LOD / メモリ管理 / ストリーミング

本仕様は、

- ランタイムの役割分担
- Editor UI（TerrainGenerationWindow v0）の項目仕様
- 将来フェーズに向けた Editor UI の骨組み

を明確にし、別端末から開発を再開しても迷わないことを目的とする。

---

## 2. 仕様ステータス分類

### 2.1 ステータス定義

- **Legacy（レガシー／段階的廃棄対象）**  
  原則として今後触らない・参照しない。ビルドに悪影響が無い範囲で徐々に削除する対象。

- **Shelved（棚上げ／将来フェーズ）**  
  現行フェーズでは実装しないが、コンセプト／ドキュメントは残す。Phase 2 以降で再検討する対象。

- **Active（現行採用）**  
  Terrain Generation v0 の設計として採用し、UI・データ・Runtime 実装を進める対象。

---

### 2.2 ランタイム仕様の分類

#### 2.2.1 Legacy（ランタイム）

- **Primitive / LOD / メモリ管理まわり**
  - 例: `PrimitiveMemoryManager`, `LODMemorySystemTest` など。
  - 現行の地形エンジンの土台としては過度に複雑で、一度リセットした方が安全なため。

- **Biomes + タイル + 高度な TerrainEngine 実装（未完成部分）**
  - `TerrainEngine_OpenSpec_v1.0` に記載された `BiomeSpecificTerrainGenerator` / `TerrainEngine` のフル機能像。
  - まずは「1 タイルの安定した生成」を優先するため、現時点ではレガシー扱いとし直接は触れない。

#### 2.2.2 Shelved（ランタイム）

- **DesignerTerrainTemplate / TerrainSynthesizer のフル機能構想**
  - 複数テンプレートのブレンド、Biome ごとのテンプレ選択など。
  - コンセプトは有用なため、Phase 2 以降で再設計のベースとして参照。

- **高度なタイル管理・ストリーミング・パフォーマンス監視**
  - 本格的なオープンワールド的運用に必要な機能群。
  - v0 では非対象とし、1〜数タイル生成の安定運用を優先する。

#### 2.2.3 Active（ランタイム）

- **単一タイルの HeightMap / Noise ベース Terrain 生成**
  - 既存 `TerrainGenerator` を「1 枚の `TerrainData` を生成する核」として再定義する。
  - タイル管理、Biome、Primitive、LOD、メモリは担当させない。

- **`TerrainGenerationProfile`（新規 ScriptableObject）**
  - 生成パラメータ（Size, Resolution, HeightMap, Noise 等）を 1 つのデータ資産としてまとめる。
  - EditorWindow とランタイムの双方がこの Profile を参照・保存する。

- **`TerrainGenerationController`（将来追加を想定する MonoBehaviour）**
  - シーン上で 1 つ以上の `TerrainGenerationProfile` を参照し、`TerrainGenerator` に委譲してタイルを生成する役割。
  - v0 では「単一タイル＋手動生成」がメインであり、Controller は小さく始める。

---

### 2.3 Editor UI 仕様の分類

#### 2.3.1 Legacy（Editor UI）

- **Primitive / LOD / メモリ管理専用のウィンドウ群**
  - テスト用 UI 含め、現行の Terrain v0 設計とは切り離して考える。

- **旧 NarrativeGen 系 UI からの直接移植で、すでに用途がないもの**
  - `OLD_TO_NEW_UI_MAPPING.md` で不要と判断できる旧 UI 群。

#### 2.3.2 Shelved（Editor UI）

- **StructureGenerator / Deform 連携の高度 Editor UI**
  - `StructureGenerator_JA`, `PHASE3_DEFORM_INTEGRATION_DESIGN.md`, `ADVANCED_STRUCTURE_DESIGN_DOCUMENT.md` に記載されたタブ構成や高度操作系。
  - Deform タブ、Operations タブ、Complex Random タブなどは Phase 3 以降で検討する。

- **RandomControl UI のフル機能版**
  - 高度なスライダー管理、Preset/Random 化、複雑なターゲット群管理など。
  - 将来の「テンプレート・構造物編集 UI」として活用することを想定し、現行 v0 では利用しない。

#### 2.3.3 Active（Editor UI）

- **TerrainGenerationWindow (v0)**
  - HeightMap / Noise を用いて **1 タイルの Terrain を生成**するための主役ウィンドウ。
  - 本ドキュメント 3 章で仕様を定義する。

- **TerrainGenerationProfile インスペクタ拡張**
  - プロファイルの編集 UI。構成は TerrainGenerationWindow とほぼ共通とし、
    - EditorWindow 側: 一時的な編集／試行
    - Profile 側: 保存された設定の編集
    と役割を分ける。

- **将来フェーズ向け Template 系 UI の骨組み**
  - Template Browser / Template Editor / TerrainEngine Manager などの「役割」だけ定義し、実装は Phase 2 以降とする（4 章）。

---

## 3. TerrainGenerationWindow (v0) 仕様

### 3.1 概要

- **ウィンドウ名**: `Terrain Generation (v0)`
- **メニュー**: `Tools/Vastcore/Terrain/Terrain Generation (v0)`
- **目的**:
  - シーン上の 1 つの `Terrain`（または新規作成する `Terrain`）に対し、
    - Noise
    - HeightMap
    - 両者の組み合わせ
    による高さ情報の生成を行う。

### 3.2 セクション構成

TerrainGenerationWindow (v0) は、以下のセクションで構成する。

1. **Context**
2. **Generation Mode**
3. **Terrain Size & Resolution**
4. **HeightMap Settings**
5. **Noise Settings**
6. **Profile**
7. **Actions**

---

### 3.3 各セクションの仕様

#### 3.3.1 Context セクション

- **Target Terrain**
  - ラベル: `Target Terrain`
  - 型: `Terrain`（ObjectField）
  - 説明:
    - 生成結果を書き込む対象となる `Terrain` を指定する。
    - 指定が無い場合の挙動は、プロジェクト方針に応じて次のいずれかとする:
      - エラー表示（推奨: 安全）
      - 自動的に新規 Terrain を生成し、`Target Terrain` に設定

- **Create New Terrain ボタン（任意実装）**
  - ラベル: `Create New Terrain in Scene`
  - 動作:
    - シーン内の原点もしくは指定位置に新しい `Terrain` + `TerrainData` を生成する。
    - 生成した Terrain を `Target Terrain` に設定する。

#### 3.3.2 Generation Mode セクション

- **Mode**
  - ラベル: `Generation Mode`
  - 型: Enum
    - `NoiseOnly`
    - `HeightMapOnly`
    - `NoiseAndHeightMap`
  - 説明:
    - v0 ではこの 3 種のみをサポートする。
    - 将来的にブレンドアルゴリズムを追加したい場合、Enum を拡張する形で対応する。

#### 3.3.3 Terrain Size & Resolution セクション

- **Terrain Size**
  - ラベル: `Terrain Size`
  - 型: `Vector3`
    - `Width`（default: 500）
    - `Length`（default: 500）
    - `Height`（default: 100）
  - 意味:
    - `TerrainData.size` に反映される。シーン上の実際の地形サイズを決定する。

- **Heightmap Resolution**
  - ラベル: `Heightmap Resolution`
  - 型: `int`（Popup もしくは IntField + バリデーション）
  - 推奨候補値: `129`, `257`, `513`
  - デフォルト値: `257`
  - 説明:
    - `TerrainData.heightmapResolution` に対応する。
    - 不正な値が入力された場合はもっとも近い有効値に丸めるか、警告を表示する。

- **Detail Resolution（オプション）**
  - ラベル: `Detail Resolution`
  - 型: `int`
  - デフォルト値: `512`（暫定）
  - 説明:
    - v0 では利用しないか、将来の草・ディテール表現向けに予約する。

#### 3.3.4 HeightMap Settings セクション

- **HeightMap Texture**
  - ラベル: `HeightMap Texture`
  - 型: `Texture2D`
  - 説明:
    - グレースケール画像を想定。
    - `HeightMapOnly` および `NoiseAndHeightMap` モードで参照される。

- **Channel**
  - ラベル: `Channel`
  - 型: Enum
    - `R`, `G`, `B`, `A`, `Luminance`
  - 説明:
    - 高さ値としてどのチャンネルを使用するかを指定する。

- **Height Scale**
  - ラベル: `Height Scale`
  - 型: `float`
  - 範囲の目安: `0.0` 〜 `5.0`
  - デフォルト値: `1.0`
  - 説明:
    - テクスチャからサンプリングした 0〜1 の値に対するスケール係数。

- **UV Offset / UV Tiling**
  - ラベル: `UV Offset`, `UV Tiling`
  - 型: それぞれ `Vector2`
  - 説明:
    - HeightMap テクスチャのオフセットとタイル回数。
    - v0 実装では「将来対応予定」として UI のみ／あるいは仕様のみ定義しておく形でもよい。

- **Invert**
  - ラベル: `Invert Height`
  - 型: `bool`
  - 説明:
    - 高さ値を 0↔1 で反転するかどうかを指定する。

#### 3.3.5 Noise Settings セクション

- **Seed**
  - ラベル: `Seed`
  - 型: `int`
  - 備考:
    - `Randomize` ボタンを付けてランダム Seed を生成することも検討する。

- **Scale**
  - ラベル: `Noise Scale`
  - 型: `float`
  - 範囲の目安: `1.0` 〜 `1000.0`
  - デフォルト値: `50.0` 付近（暫定）

- **Octaves**
  - ラベル: `Octaves`
  - 型: `int`
  - 範囲: `1` 〜 `8`
  - デフォルト値: `4`

- **Persistence**
  - ラベル: `Persistence`
  - 型: `float`
  - 範囲: `0.0` 〜 `1.0`
  - デフォルト値: `0.5`

- **Lacunarity**
  - ラベル: `Lacunarity`
  - 型: `float`
  - 範囲: `1.0` 〜 `4.0`
  - デフォルト値: `2.0`

- **Offset**
  - ラベル: `Offset`
  - 型: `Vector2`
  - 説明:
    - Noise サンプル開始位置のオフセット。

#### 3.3.6 Profile セクション

- **Generation Profile**
  - ラベル: `Generation Profile`
  - 型: `TerrainGenerationProfile`（ScriptableObject）
  - 説明:
    - 現在のウィンドウ上の設定を、Profile 資産として保存／読み込みするための参照。

- **Load / Save ボタン**
  - `Load From Profile`
    - 選択中 Profile の値でウィンドウ上の UI 値を上書きする。
  - `Save To Profile`
    - 現在の UI 値を選択中 Profile に書き戻す。

#### 3.3.7 Actions セクション

- **Generate Preview**
  - ラベル: `Generate Preview`
  - 動作:
    - `Target Terrain` に対して、現在の設定に基づき高さ情報を生成し書き込む。
    - `Target Terrain` が未指定の場合の挙動は、Context セクションの設計に従う。

- **Clear Generated Terrain**
  - ラベル: `Clear`
  - 動作:
    - `Target Terrain` の高さをフラットな状態（0 または baseHeight）にリセットする。

---

## 4. 将来フェーズ Editor UI の骨組み（Shelved）

本章では、現行 v0 では実装しないものの、将来フェーズでの UI 設計の「大きな枠組み」を定義する。
実装は Phase 2 以降で検討する。

### 4.1 Template Browser

- **役割**
  - `DesignerTerrainTemplate` 資産の一覧・検索・プレビューを行う。

- **レイアウト案**
  - 左ペイン: テンプレ一覧（ListView）
  - 右ペイン: 選択テンプレのサムネイル＋主要パラメータの概要表示

- **備考**
  - RandomControl / StructureGenerator の UI デザインパターン（折りたたみセクション＋スライダー＋プレビュー）をインスパイア元とする。

### 4.2 Template Editor

- **役割**
  - 個々の `DesignerTerrainTemplate` を編集する。
  - HeightMap 設定と、簡単なバリエーション（スケールレンジ、回転レンジなど）を扱うところから始める。

- **UI パターン**
  - foldout（折りたたみ）＋スライダー＋リアルタイムプレビュー。
  - RandomControl ドキュメントのスライダー UI 本体を再利用可能なコンポーネントとして設計することを検討。

### 4.3 TerrainEngine Manager

- **役割**
  - 複数のテンプレートとシーン上のタイルを紐付け、まとめて Bake / Regenerate を行うマネージャ UI。

- **ステータス**
  - 完全に Future（Phase 2〜3）とし、v0 では一切実装しない。
  - この仕様は「どのウィンドウがどの責務を持つか」を迷わないためのメモとして扱う。

---

## 5. 今後の実装ガイド

1. **TerrainGenerationProfile のクラス定義**
   - 本ドキュメント 3.3 の項目をベースに、ScriptableObject フィールドを設計する。
2. **TerrainGenerationWindow (v0) の実装／既存ウィンドウの整理**
   - `HeightmapTerrainGeneratorWindow` など既存 EditorWindow がある場合、本仕様に沿って責務を整理・改名する。
3. **TerrainGenerator の責務の明確化**
   - 単一タイル生成に専念させ、タイル管理や Biome、Primitive 等の責務を取り除く／依存を減らす。

本ドキュメントは、HeightMap & Noise ベースの単一タイル生成を安定化させるための基準点として扱い、
将来フェーズ（Template / Biome / Deform 等）の設計は別ドキュメントで段階的に拡張していく。
