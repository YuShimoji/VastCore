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

並行枝はそれぞれのtask/spec/PRへ進捗を残し、global `runtime-state` を奪い合わない。
Project Pulse を更新するのは current outcome を所有する一枝だけで、所有枝を変える時は
handoffとして repository variable `PROJECT_PULSE_BRANCH` と
`runtime-state` の branch/outcome/next action を同時に更新する。

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

画が成果に入る環境・パッケージの初回依頼では、`Creative checkpoint` で出典付き参考画像を開き、
取り入れる点と完成想定図を本制作前に示すよう指定する。`Acceptance` には通常の入口と代表場面・
視点を入れる。初回の試作を返す前に Unity の実画面を完成想定図と同じ構図で比べ、依頼内で
直して撮り直す。機能の確認と見た目に残る差を示す。

開発AIは Outcome、Scope、Autonomy の内側を自律で進める。新しいPromptは成果単位の完了、
stop condition、またはユーザーが方向を変えた時に発行する。

## Direction Before Production

レイアウト、色、フォント、アニメーション、言語対応、コンテンツ拡張など、後から
直すほど高価になる変更では次の順序を守る。

1. 監修AIまたは開発AIが、表面的な色違いではない2〜3方向を提示する。各案に
   狙い、利用場面、代償、何が可能になるか、推奨案を添える。
2. ユーザーの選択・混合・修正を、短い design intent と anti-goals に固定する。
3. 全面実装ではなく、一画面・一フロー・一代表コンテンツの proof slice を作る。
4. 実物をまとめてレビューし、承認後に横展開する。

同じ懸念の微修正が2巡しても収束しない、または3件以上の修正が同じ原則に集まる
場合は、個別調整を止めて direction checkpoint に戻る。これは実装停止ではなく、
誤った方向への追加投資を止めるための再設計である。

## 環境（景観）の見た目を作る流れ

砂岩の台地（2026-11-25〜28）で効いたことと遅れたことから作った、どの景観にも使う順序。
草原は別の担当が同じ目的の流れを作っているので、突き合わせて1つにするまで、この節は
砂岩側の版とする（2026-11-28 本人「最終的に突き合わせて統合することを視野に」）。

1. 参照：実在の写真を遠・中・近で各1〜3枚（出典つき。既存作品は使わない）。写真ごとに
   形・表面・植生・光のどれを見るかを書く。
2. 見え方の想定：視点ごとの条件表（例 [見え方の想定](02_design/REGION_TERRAIN_LOOK_SPEC.md)
   2節）に、各条件を何で出すか（形／素材／配置／光）を付ける。距離で効く物が違う：
   遠（約400 m より先）は輪郭と明暗の塊、つまり形だけ。中（約60〜400 m）は数 m〜数十 m の
   形（段・割れ目・溝・株の塊）と大きい周期の素材。近（約60 m まで）は素材と小さな形
   （礫・草の葉）。素材では中・遠の不足は直らない（T5：写真で近景ののっぺりは消え、中遠景の
   崖は変わらなかった）。形の代わりに色を塗るのも同じ失敗（草原の「地面に緑を塗った」地表）。
3. 方向の選択：方向の違う2〜3案に、GPU の増分・場面の作成時間・作業量の見込みを付けて
   本人に見せ、本人が選ぶ。実装の副産物の画面で可否を求めない。
4. 形を先に確かめる：素材・植生の前に、細部を外した単色の絵で遠・中の形を本人に見せる。
   砂岩は骨格→岩屑→表面→植生→折り返し→光の順で進め、中遠景の崖の形の不足が表面の段の
   後まで見つからなかった。
5. 撮影：固定の遠・中・近と景観ごとの追加視点を、2地点（片方は適地を規則で選んだ別の
   地点）・太陽4つ（60/38/20/10°）で、同じ版の前（HEAD）と後の対にし、比較ページ1枚に
   まとめる（`scripts/region-map.ps1 -Mode view3d`）。本人が Editor を開いている間は写し
   `D:\vcv\VastCore` で撮る。
6. 予算：地形の描画の GPU 増分の合計を段ごとに記録する（1600×900 で 1.2 ms、
   [表面材質](02_design/REGION_SURFACE_MATERIALS_SPEC.md) 3.3節）。場面の作成時間の増分も残す。
7. 記録：結果は該当の設計文書と `runtime-state` の行を書き換える。画像と比較ページは
   `D:\AI-Runs\VastCore\<作業名>\` に置く。

### 草原での適用

[USGS・NPS の植生調査](https://irma.nps.gov/DataStore/FileSource/Get/2170394?filename=tallgrassrpt_nrpc.pdf) 図9・26〜27頁の二層の草丈、斜面と湿り気、露岩の周りの草を参照する。完成図と Unity の目の高さの絵で、短い草が土を覆うか、長い葉と穂がその上に重なるか、中景の群落から遠景の色へつながるかを照合する。Mesa との共通の基準は、距離ごとに形と材質がつながり、物の足元が地面に納まること。

固定サンプルは小道・露岩を含む。同じ seed・晴天の前後と、地域探索からの自動生成を PowerShell で撮る。

```powershell
.\scripts\region-map.ps1 -Mode view3d -GrasslandSample -Seed 8 -LookSky ours -GrasslandStyle Tussocks -GrasslandTiming -OutDir artifacts/region-map/grassland-before
.\scripts\region-map.ps1 -Mode view3d -GrasslandSample -Seed 8 -LookSky ours -GrasslandTiming -OutDir artifacts/region-map/grassland-after
.\scripts\region-map.ps1 -Mode explore -GrasslandGround -Seed 8 -OutDir artifacts/region-map/grassland-ground
```

固定撮影は遠中近・目の高さ・俯瞰・足元・露岩・柵・小道。既定の `Colonies` は近景の細葉と株を使い、中遠景は斜面に沿う低い草の面と葉先を重ねる。`Tussocks` は前版、`TussockGeometry` は近景を細葉の立体で描く比較用。

`-GrasslandTiming` は足元と目の高さの被覆マスク、視野に重なるタイルの三角形、GPU 同期を含む描画時間を出す。日照は `-SunSweep '60,38,20,10'`、別地点は `-LookBiome auto -LookCenter 'X,Y'`。Editor の入口は `Vastcore > Region Map > Open 3D Grassland Env Sample`。探索では seed 8 の X37.55、Y57.86 から `3Dへダイブ` し、Esc で戻る。

別の環境でも、完成図に必要な輪郭・重なり・影を距離別に切り出す。同一カメラで立体の入切を見て、素材と配置・陰影のどこが差を生むか特定する。既存素材を実寸で試し、素材の不足が残る場合は出所・実寸・見る角度を記録して足す。草原では細葉2種から [株アトラスと投影範囲](../scripts/bake-meadow-tussocks.py) を焼いた。前後を目の高さ・見下ろし・別地点で照合し、被覆・陰影・描画負荷を確かめる。

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
