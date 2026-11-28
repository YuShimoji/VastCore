# Operator Workflow

人間オペレーター、監修AI、開発AIの実ワークフロー・痛点・品質目標を保持する正本。

## Overall Flow

1. 監修AIは `runtime-state` と owning artifact を読み、今回変える差分だけを
   Mission Packet にする。過去経緯や全手順を Prompt に複製しない。
2. 高コストなUI・ビジュアル・コンテンツ変更だけは、実装前に2〜3方向を
   比較し、ユーザーが選択または混合して design intent を固定する。
3. 開発AIは一つの reviewable outcome までを coherent batch として実装し、
   関連する狭い修正、機械検証、正本同期まで継続する。
4. 進捗共有は、方針が変わった時、意味のある中間成果が出た時、または長い
   ツール待ちの時に行う。ファイル単位・コマンド単位では報告しない。
5. Unity Editor / scene / visual judgement が必要なら、開発AIは機械検証と分離
   した一つの確認バッチとして対象、操作、成功条件を渡す。
6. 開発AIは `runtime-state` と必要な owning docs を同期し、検証済み範囲と
   未検証範囲を分けて commit/push する。
7. GitHub Project Pulse は `runtime-state` から自動更新し、次のPromptは
   その current outcome が閉じたか、stop condition に達した時だけ発行する。

並行枝の記録先と Project Pulse は [State Sync](REPO_LOCAL_RULES.md#state-sync-is-part-of-completion) に従う。

## Supervisor To Developer Mission Packet

一つのPromptで一つの成果単位を渡し、実装手順は任せる。既知の状態は commit と正本パスを参照する。

```text
[VASTCORE MISSION]
Outcome: 完了時に誰が何をできるようになるか
Why now: 解消する現在の bottleneck
Scope: 今回含む境界 / 明示的に含まない境界
Acceptance: 必要なコード・画面・テスト・手動確認の証拠
Risk tier: routine（既定・自律実行）/ boundary-changing（具体的な危険と判断を記述）
Autonomy: outcome 内の実装、関連修正、検証、正本同期、commit/push は開発AI判断
Stop only if: 破壊的変更、依存/DB/認証/API契約、仕様衝突、未確定の高コスト方向
Creative checkpoint: 不要 / 方向案を先に提示 / 選択済み intent と proof slice
Sync on close: runtime-state と今回変わる owning artifact
Start from: branch/commit と必要最小限の task/spec/正本パス
```

画を作る環境・パッケージは、統一正本 [DS-015 環境パッケージ制作パイプライン](02_design/ENVIRONMENT_PRODUCTION_PIPELINE.md)
に従う。制作前に共有資産インベントリ（DS-015 §2）を照合し、出典付き参考画像・完成想定図・距離帯ごとの
実現手段マップ（§3、プリミティブ/頂点カラー単色の最終利用禁止）を揃える。`Acceptance` は通常の入口と
代表視点。承認済み方向の内側では、初回試写で止めずに「試写→4軸乖離診断（単色・入切）→原因別修正→
再撮影」を最低2巡（収束しなければ構造ピボット1回）まで自律完遂し、`scripts/check-env-package-review.ps1`
を通したうえで、参考・想定・Unity 前後、再利用資産、効いた修正、残る差、負荷を一式で返す。方向選択が
必要な場合も、依存しない調査・資産照合・基準撮影は先に進める。

インポスターなどの LOD は、通常入口で対象・配置・視点・照明を揃える。現行メッシュや結合 LOD を
基準に生成・CPU・GPU を分けて測り、境界で輪郭・影・照明の飛びを撮る。効いた条件を残し、
対象ごとの画と負荷で採否を判断する。

開発AIは Outcome、Scope、Autonomy の内側を自律で進める。新しいPromptは成果単位の完了、
stop condition、またはユーザーが方向を変えた時に発行する。

## Direction Before Production

手戻りの大きい UI・見た目・コンテンツ変更は次の順で進める。

1. 監修AIまたは開発AIが、表面的な色違いではない2〜3方向を提示する。各案に
   狙い、利用場面、代償、何が可能になるか、推奨案を添える。
2. ユーザーの選択・混合・修正を、短い design intent と anti-goals に固定する。
3. 全面実装ではなく、一画面・一フロー・一代表コンテンツの proof slice を作る。
4. 実物をまとめてレビューし、承認後に横展開する。

同じ懸念が2巡で収束しないか、3件以上の修正が同じ原則に集まれば、個別調整を止めて
direction checkpoint に戻る。方向に依存しない実装は進める。

## 環境（景観）の見た目を作る流れ

砂岩（2026-11-25〜28）と草原・海岸・森林・都市の知見を統合した単一手順は
[DS-015 環境パッケージ制作パイプライン](02_design/ENVIRONMENT_PRODUCTION_PIPELINE.md) を正本とする。
要点は以下のとおり：

1. **参照と資産照合（DS-015 §2）**：実在写真を遠・中・近で各1〜3枚（出典付き。既存作品は使わない）集め、同時に既存リポジトリ資産（`Surfaces/`・`RegionMap3D` HLSL・DS-011 FBX・`com.vastcore.water/weather`）→ CC0/Blender ベイク → 所有済みアセット候補の順で再利用手段を特定する。
2. **実現手段マップ（DS-015 §3）**：遠（輪郭と霞）・中（数 m〜数十 m の構造と大周期材質）・近（実寸 PBR と実形状 FBX/ベイク葉群・接地）ごとに理想図の要素を具体的な資産・シェーダーへ紐付ける。C# 頂点カラー単色や Box/Tube 合成を最終表現にしない。
3. **方向選択と自律収束ループ（DS-015 §4）**：方向未確定なら 2〜3 案に負荷・作業量見込みを添えて選択を得る。方向確定後は、単色での形・接地確認や Round 1 試写で本人確認へ止めず、**最低2巡の診断・修正（未収束なら手法・素材切替の構造ピボット1回）**を自律完遂する。
4. **同条件撮影・計測・機械検証（DS-015 §4.3・§5）**：通常入口から 2 地点・太陽 4 角（60/38/20/10°）で前後を比較1ページにまとめ、GPU 予算（1600×900 で地形増分 1.2 ms）と構築時間を記録し、`.\scripts\check-env-package-review.ps1` を通過させる。
5. **記録**：結果は該当設計文書と `runtime-state` に反映し、画像・比較ページ・`package-review.json` は `D:\AI-Runs\VastCore\<作業名>\` に置く。

### 草原での適用（固有コマンド・ベイク手順）

[USGS・NPS の植生調査](https://kindscher.ku.edu/sites/kindscher/files/documents/2011-Vegetation-Classification-and-Mapping-of-Tallgrass-Prairie-National-Preserve.pdf#page=38) の図9・26〜27頁で二層の草丈、斜面と湿り気、露岩まわりを見る。完成図と Unity の同じ目の高さで、短い草の被覆、長い葉と穂の重なり、中遠景のつながりを照合する。Mesa と同様に距離別の形と材質、物の接地を確かめる。

固定サンプルは小道・露岩を含む。同じ seed・晴天の前後をそれぞれの実装版で撮り、地域探索の自動生成も撮る。

```powershell
.\scripts\region-map.ps1 -Mode view3d -GrasslandSample -Seed 8 -LookSky ours -GrasslandTiming -OutDir artifacts/region-map/grassland-before
.\scripts\region-map.ps1 -Mode view3d -GrasslandSample -Seed 8 -LookSky ours -GrasslandTiming -OutDir artifacts/region-map/grassland-after
.\scripts\region-map.ps1 -Mode explore -GrasslandGround -Seed 8 -OutDir artifacts/region-map/grassland-ground
```

固定撮影は遠中近・目の高さ・俯瞰・足元・露岩・柵・小道。既定の `Colonies` は近景の細葉を保ち、中遠景で低い葉と背の高い株を重ねる。`Tussocks` と `TussockGeometry` は別方式の比較用。

`-GrasslandTiming` は足元と目の高さの被覆マスク、視野に重なるタイルの三角形、GPU 同期を含む描画時間を出す。日照は `-SunSweep '60,38,20,10'`、別地点は `-LookBiome auto -LookCenter 'X,Y'`。Editor の入口は `Vastcore > Region Map > Open 3D Grassland Env Sample`。探索では seed 8 の X37.55、Y57.86 から `3Dへダイブ` し、Esc で戻る。

別の環境でも、立体の入切を同じ視点で比べ、輪郭・重なり・影を距離別に見る。被覆率と併せ、小道など意図した空地を除き、地面が帯状に続く箇所や配置の反復を見下ろしで探す。入切の差分で葉から遠い領域を見積もる。素材は透過後の輪郭を実寸で確かめる。草原では草面に沿う低い葉を重ね、背の高い株を群れにした。距離の切替と描画時間は別地点でも照合する。素材が足りなければ出所・実寸・見る角度を記録して足す。

元の 1K Blend 2種を付属の `textures` とともに展開し、`python scripts/bake-meadow-tussocks.py <Blender.exe> <素材フォルダ> <作業フォルダ> Assets/Resources/RegionMap3D/MeadowTussocks.png` で PNG と投影範囲 JSON を作る。出所とライセンスは [素材記録](../Assets/Resources/RegionMap3D/texture-source.json) にある。

## Creative Exploration Pulse

主要な成果単位を閉じる前に、現在の主レーンとは異なる視点から最大2件だけ提案する。
UIならレイアウト/情報階層、i18n、色/フォント、motion/accessibility、隣接コンテンツを
候補レンズにする。各提案には仮説、利得、コスト、今決める意味を添え、未承認の提案を
成果物へ黙って混ぜない。

## Actor Boundaries

- user: direction checkpoint の選択、final visual/creative judgement、Unity
  Editor acceptance、frozen product frontier の再開。
- supervisor AI: outcome と bottleneck の定義、異なる方向案、stop condition、
  成果レビュー。実装手順の逐次指示は担当しない。
- assistant: source edits, doc sync, static checks, focused tests, readback, gap
  reports, outcome 内の実装判断、creative option の具体化。
- tool: scripted checks, local servers, browser or editor automation when
  available and task-relevant.
- shared: manual Editor verification followed by assistant evidence capture.

## Manual Verification Boundaries

- Unity Editor compile/play/scene behavior is not accepted from prose alone.
- If the assistant cannot run the required Unity verification, it must state the
  missing check and provide the exact user-owned verification target.
- Do not mix manual verification requests with strategic next-direction choices.

## Pain Points to Avoid

- Rebuilding context from long historical handoffs when `runtime-state` and the
  owning spec are enough.
- Treating stale March-era progress summaries as current acceptance.
- Spreading the same rule across `AGENTS.md`, `CLAUDE.md`, docs, and tool config.
- Asking the user to choose between commit/no-commit instead of explaining the
  actual bottleneck.
- Treating every safe implementation choice as a new approval gate.
- Delivering a broad visual result before direction is selected, then spending
  repeated blocks on local polish without revisiting the design intent.
- Writing progress only in chat or a historical handoff while leaving
  `runtime-state` and the external Project Pulse stale.

## Quality Goal

The workflow is healthy when a new agent can read the normal 3-file restart set,
identify the current outcome and bottleneck, finish a coherent batch without a
micro-prompt chain, and leave both the repo state and public Project Pulse ready
for the next decision.
