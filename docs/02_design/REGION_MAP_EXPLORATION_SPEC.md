# SP-021 地域鳥瞰図と探索の土台（Region Map / Exploration）

状態: 2026-11-21 に最初の開発区切りを実装した。見え方・操作感の採否は本人が決める（未記録）。
現在地と次の作業は [runtime-state](../runtime-state.md) が持つ。本書は仕様・全体工程・実行方法を持つ。

## 1. 本人の依頼（2026-11-21、原文）

> VastCoreプロジェクトについて、今まで延々とArchの開発に局所化していたところです。大元のビジョンを解釈して、全体を作り直してほしいです。実現したいこととしてプロシージャルなマップ生成、オブジェクトの生成であることは変わりありません。
> ・目標として、地域一体の詳細な鳥瞰図を自動で作成できるようにしたいです。ここにポイントクリック的な宝探し探索ゲームを載せられるようにする、という想定で開発区切りとしてください。イメージとしてはFFXのマップサーチのようなものです。

添付は画面写真2枚。真上から見た広域の地図（海・陸・山・雪・雲）の上に、左のパネル（リスト／インプット／サーチ、X・Y・Z の座標）と、発見時に「行き先リストに追加された」と知らせる文面が載っている。

きっかけの観察（本人の言葉）: 開発が Arch の往復検証（SP-017）に局所化し、マップ生成・オブジェクト生成という大元へ進んでいなかった。

## 2. 開発担当の解釈（本人の採用ではない）

- 作るものの中心を「構造物1件の Editor 往復」から「地域一帯を seed から作り、真上から見た一枚の絵にする」へ移す。
- 大元の目的「広大な景観に映える、ユニークで巨大な人工構造物」は、**地図の上で探し当てる地点**として接続する。地点の種類（Monolith / Arch / Ring / Tower / Dome / Terrace / Sunken）は構造物の系統を指す名前で、形そのものはまだ作っていない（工程5）。
- 参照された画面は「座標を指定してサーチし、隠れた地点を発見して一覧へ加える」という仕組みとして受け取った。既存作品の名称・画面の見た目・文言は再現しない。
- 地点の呼び名（例「北東・雪原のドーム」）は位置と地形から機械的に作った仮の呼び名。作品としての命名・文章・色・様式は本人が決める。

## 3. 今回の開発区切り（完成条件）

| # | 条件 | 確かめ方 |
|---|---|---|
| 1 | seed 1つから、海・陸・山・川・気候の違いを持つ地域データを自動で作れる | `tools/RegionMapPreview` または Unity メニューで seed を変えて書き出す |
| 2 | 地域データを真上から見た絵（PNG）にできる。描き方を複数持つ | Satellite / Atlas / Relief の3様式の PNG |
| 3 | 地域の中に、探し当てる地点が地形に応じて自動で置かれる | `--sites` 付きの PNG、`RegionData.Sites` |
| 4 | 座標を指定してサーチし、近さの反応と発見・一覧への追加が動く | `Assets/Scenes/RegionExplorer.unity` を再生 |
| 5 | 別のゲームを載せるための入口（データ・判定・保存）がコードから使える | 6節の公開 API、EditMode テスト |

見え方の質（地形の説得力、配色、雲、画面の配置と操作感）は条件に含めず、本人の受入で決める。

## 4. 全体工程

| 段 | 内容 | 現在地（2026-11-21） | 不足・ずれ | 次の担当 |
|---|---|---|---|---|
| 1 地域データ生成 | 海陸・隆起・侵食・気候・川・バイオーム | 実装済み。1024² を .NET 9 で約8秒、Unity Editor（Mono）で約20秒 | 湖が無い。乾いた山地に平行な溝、海に薄い格子模様。Mono での生成時間 | 開発担当（本人の受入の後） |
| 2 鳥瞰図の描画 | 3様式、陰影、落ち影、水深、川、雪、雲 | 実装済み。PNG 書き出し可 | 雲の形、配色の採否は本人未判断。1024² 超の出力は未確認 | 本人（採否）→ 開発担当 |
| 3 探索地点 | 地形の採点による配置、種類、仮の呼び名 | 実装済み（18地点、間隔9以上） | 地点の中身（何があるか）は無い。命名は仮 | 本人（方向）→ 開発担当 |
| 4 探索画面 | 座標カーソル、サーチ、反応、発見一覧、地域の作り直し、描き方切替 | 実装済み。Unity で動作を確認（7節） | 配置・文言・操作感の採否は本人未判断。保存は API のみで画面に無い | 本人（採否）→ 開発担当 |
| 5 地点のローカル表示 | 発見した地点を近くで見る（3D の地形と構造物） | 未着手 | 既存の StructureGenerator・Prefab Stamp・DualGrid（SP-017 / SP-020）をここで使うかは未決 | 本人（方式の判断） |
| 6 ゲームを載せる | 目的・報酬・進行・保存・演出 | 未着手（入口のみ） | ゲームの内容は本人の領分 | 本人 |

今回の区切りは段1〜4。段5・6は範囲外で、着手には本人の判断が要る。

## 5. 構成

```text
Assets/Scripts/RegionMap/
  Vastcore.RegionMap.asmdef            （参照: UnityEngine.UI のみ）
  Core/   UnityEngine に依存しない C#（生成・描画・地点・探索の判定）
  View/   RegionExplorer（探索画面）、RegionMapTexture
  Editor/ Vastcore.RegionMap.Editor.asmdef、メニューとバッチ入口、画面確認用スモーク
Assets/Scenes/RegionExplorer.unity     探索画面のシーン（カメラと RegionExplorer だけ）
Assets/Tests/EditMode/RegionMapTests.cs
tools/RegionMapPreview/                Core を dotnet で直接コンパイルして PNG を書き出す確認用コンソール
scripts/region-map.ps1                 Unity のバッチ入口（setup / explore）
```

`Core/` は Unity の asmdef と `tools/RegionMapPreview` の両方から同じソースをコンパイルする。絵の調整は Unity を起動せずに数秒で回せる。既存の `Vastcore.Terrain` などへは依存しない（Deform・ProBuilder を巻き込まないため）。

### 生成の流れ（`RegionGenerator.Generate`）

1. `BuildBase`: ドメインワープした fBm で海陸を決め、外周は必ず海にする。山脈帯（ridged）と平野のマスクから隆起の強さを作る。
2. `Evolve`: Priority-Flood で窪地を埋めつつ下流→上流の順序を得て、流量を累積し、陰解法の stream power 侵食・斜面の上限（安息角）・斜面拡散を反復する。半解像度で反復の3/4、残りを全解像度で行う。
3. `BuildClimate`: 緯度と標高（6.5℃/km）から気温、西風が海で湿り陸と山で雨を落とす掃引から湿り気（雨陰が出る）。
4. `BuildRivers` / `ClassifyBiomes`: 流量のしきい値で川、気温×湿り気×標高×斜面でバイオーム。
5. `RegionSiteGenerator.Populate`: 候補点を地形で採点し、種類ごとに間隔を空けて選ぶ。

同じ `RegionSpec` からは同じ地域・同じ地点が出る（テストで固定）。

### 描画（`RegionMapRenderer.Render`）

気温×湿り気の色表の補間、北西315°からの陰影、落ち影、海の水深色、川、雪、雲と雲の影。`RegionMapStyle` は Satellite（写真風）/ Atlas（地図帳風）/ Relief（起伏を強調）。出力は RGBA32（北西の角から行順）。

## 6. 探索のルールと、ゲームを載せる入口

- 地図座標は X・Y とも 0〜100。X は東へ、Y は南へ増える。高さ（m）は `RegionData.HeightAtMap`。
- `ExplorationSession.Search(x, y)`: いちばん近い未発見の地点との距離で `Found`（2.5以内）/ `Strong`（8以内）/ `Faint`（18以内）/ `None` を返し、`Found` なら発見済みに加える。距離の値は調整用の公開フィールド。
- `ExplorationSession.Sense(x, y)`: 回数を消費せずに反応だけを見る。
- `SaveDiscoveredIds()` / `Restore(ids)`: 発見済みの地点 ID を保存・復元する（保存先は載せるゲームが決める）。
- `RegionExplorer`（MonoBehaviour）: `IsReady`、`Region`、`Session`、`CursorMapPosition`、`MoveCursor(x, y)`、`SearchAtCursor()`、`NextRegion()`、`NextStyle()`。
- 探索画面の操作: 地図を左クリックでカーソル移動、Space / Enter / ボタンでサーチ、矢印キーで微調整、ホイールで拡大、右または中ドラッグで地図を動かす。

## 7. 実行方法と確認の記録

Unity を起動しない確認（PowerShell、リポジトリのルートで）:

```powershell
dotnet build tools/RegionMapPreview -c Release -v q --nologo
dotnet run --project tools/RegionMapPreview -c Release --no-build -- --seed 7 --res 1024 --style satellite,atlas,relief --scale 2 --out artifacts/region-map
```

Unity（6000.3.6f1。Unity Hub を起動してサインイン済みであること）:

```powershell
.\scripts\region-map.ps1 -Mode setup     # バッチ: コンパイル、探索シーン作成、PNG 書き出し
.\scripts\region-map.ps1 -Mode explore   # 通常 Editor: 探索シーンを再生し画面を画像に残して終了
```

Editor のメニューは `Vastcore > Region Map > Open Explorer Scene` と `Export Bird's-Eye PNG...`。出力先 `artifacts/` は git 管理外。

確認の記録は [runtime-state](../runtime-state.md) の Current Trust Assessment に置く。

## 8. 既知の制限

- 湖は生成されない（窪地を毎回埋める）。`RegionBiome.Lake` と `LakeDepth` は場所だけある。
- 乾いた山地に平行な溝が出る（流向が8方向で、斜面の上限と重なるため）。海に薄い格子模様が出る。
- 生成と描画は CPU の単一スレッド。探索画面は開始時に生成が終わるまで待つ（Unity Editor で約30秒）。
- 地点の中身、発見後の遷移、音、演出、保存の画面は無い。
- 探索画面の文字は OS のフォントを使う（Windows で確認）。他の環境の表示は未確認。
- 画面は 1920×1080 を基準に高さへ合わせて縮む。縦長の画面は未対応。

## 9. 本人の判断を待つもの

| 判断 | 選択肢 | 選ぶと変わること | 止まっている作業 |
|---|---|---|---|
| 鳥瞰図の描き方の方向 | Satellite / Atlas / Relief のどれに寄せるか、別の方向 | 配色・陰影・雲の調整の向き、既知の制限のどれを先に直すか | 段1・2の見た目の磨き込み |
| 探索画面の配置と操作 | 今の配置を直す点、反応を常時出すかサーチ後だけか | 画面の作り直しの範囲 | 段4の磨き込み |
| 地点のローカル表示の方式 | 既存の構造物経路（SP-017 / SP-020）を使う / 新しく作る / 行わない | SP-017 の Arch 往復検証を再開するか | 段5 |
| 地域の規模と解像度 | 256km・1024² のまま / 広げる / 細かくする | 生成時間、タイル分割の要否 | 段1の拡張 |

## 10. 出典（2026-11-21 に開いて確かめたもの）

- Red Blob Games "Making maps with noise functions": 海陸の作り方、標高の再配分、気温×湿り気によるバイオーム。
- Barnes, Lehman, Mulla "Priority-Flood" (arXiv:1511.04463): 窪地を埋める順序付きの洪水。
- Esri "How Hillshade works": 光の方位315°・高度45°を標準とする陰影の式。

未取得: Cordonnier ら 2016 の本文、水滴式の侵食（Beyer、Lague）、Whittaker のバイオーム閾値、Imhof の配色。stream power の陰解法は一般に知られた形で実装し、出力の絵で確かめた。
