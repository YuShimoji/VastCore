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

画を作る環境・パッケージは、制作前に参考画像を開き、採る点と完成想定図を出典とともに示す。
`Acceptance` は通常の入口と代表視点。初回返却までに、試写の差を単色や入切で一条件ずつ
確かめて依頼内で直し、同じ視点・光・seed（動きは時刻も）で撮り直す。参考・想定・Unity 前後、
効いた修正、残る差、機能と関係する負荷を示す。方向選択が必要なら、選ぶ範囲を示し、
依存しない調査や基準撮影を進める。

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
4. 形と接地：素材・植生の前に、単色で遠・中の形と接地を本人に見せる。
   砂岩は骨格→岩屑→表面→植生→折り返し→光の順で、崖の形の不足が表面の後まで見つからなかった。
   材質・配置・光は入切で効きを確かめる。
5. 撮影：遠・中・近と追加視点を、2地点（片方は適地を規則で選定）・太陽4つ（60/38/20/10°）で
   前（HEAD）と後を同条件で対にする（`scripts/region-map.ps1 -Mode view3d`）。比較は1ページ。
   Editor 使用中の写しは `D:\vcv\VastCore`。起動前は [Git And Tests](REPO_LOCAL_RULES.md#git-and-tests) を参照。
6. 予算：地形の描画の GPU 増分の合計を段ごとに記録する（1600×900 で 1.2 ms、
   [表面材質](02_design/REGION_SURFACE_MATERIALS_SPEC.md) 3.3節）。場面の作成時間の増分も残す。
7. 記録：結果は該当の設計文書と `runtime-state` の行を書き換える。画像と比較ページは
   `D:\AI-Runs\VastCore\<作業名>\` に置く。

### 草原での適用

[USGS・NPS の植生調査](https://kindscher.ku.edu/sites/kindscher/files/documents/2011-Vegetation-Classification-and-Mapping-of-Tallgrass-Prairie-National-Preserve.pdf#page=38) の図9・26〜27頁で二層の草丈、斜面と湿り気、露岩まわりを見る。完成図と Unity の同じ目の高さで、短い草の被覆、長い葉と穂の重なり、中遠景のつながりを照合する。Mesa と同様に距離別の形と材質、物の接地を確かめる。

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
