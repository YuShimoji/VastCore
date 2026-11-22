# User Request Ledger

ユーザーの継続要望・差分要求・backlog delta を保持する台帳。

## Currently Active Requests

- Project-local Agent instruction files should start from a fresh, modern state.
- Align VastCore's AI instruction structure with newer projects such as
  NLMYTGen: thin adapters, repo-local rules front-door, runtime state as current
  truth, and no operational history inside `AGENTS.md`.
- Treat missing referenced docs as stale references, not blockers.
- For residual work, report purpose, effect, requirements, current state, owner,
  and next move.
- 2026-07-10: Supervisor prompts and development execution must optimize for one
  coherent outcome, not a chain of tiny instructions. Safe, reversible, in-scope
  work should continue through related fixes and verification without repeated
  approval.
- 2026-07-10: Safety gates must not become the main work. Stop only for a
  concrete destructive/contract/authority conflict or an unresolved expensive
  creative direction.
- 2026-07-10: Progress and next work must remain understandable outside chat.
  Keep repo state current and publish a generated external Project Pulse rather
  than relying on manually maintained Wiki prose.
- 2026-07-10: AI reviews must contribute creative alternatives across layout,
  localization, content adjacency, color, typography, and motion when relevant.
  Expensive production starts only after the user can compare directions.
- 2026-07-10: Avoid the broad-delivery-to-micro-tweak loop. Use an early
  direction checkpoint, a small proof slice, batched review, and return to the
  design principle when local tweaks do not converge after two rounds.

- 2026-11-21: 「推奨で出ているSP-017を先に。間が開いてからの再開なので、まず何よりも
  手を入れやすく整理のついた状態にしてほしい」。間を置いた再開では、実作業の前に
  入口・状態文書・作業ツリーを整理し、次の作業へ直接入れる状態にする。

- 2026-11-21: 「今まで延々とArchの開発に局所化していた」「大元のビジョンを解釈して、
  全体を作り直してほしい」「目標として、地域一体の詳細な鳥瞰図を自動で作成できるように
  したい。ここにポイントクリック的な宝探し探索ゲームを載せられるようにする、という想定で
  開発区切りと」する。プロシージャルなマップ生成とオブジェクト生成の軸は変えない。
  一つの部品の検証を続けるより、目標の絵と遊びの土台が先。仕様と全体工程はSP-021。

- 2026-11-21: 「衛星写真風」。鳥瞰図の3つの描き方（衛星写真風・地図帳風・陰影図）のどれに寄せるかの問いへの回答。
- 2026-11-21: 「近い」。作り直した衛星写真風は求めていた見え方に近いかの問いへの回答。
- 2026-11-22（Recordinator `01M7Z6YNHPZKBMGY0Z5429WE71` revision 1）: 近い／次は今回の続き。直したい（次の前に）は雲・探索画面・待ち時間。記述「雲はもっと多くて良いです。現実に近い量で良いです。機能として透過などが選べれば良い。また可能ならゆっくりと流れているアニメーションができるとさらに良いです。」
- 2026-11-22（Recordinator `01M7ZABDTFVETDP0AAFAGVWP0C` revision 1、探索画面）: 直したい（次の前に）は文字と文言・見た目の質感・操作の流れ・カーソルと印・発見した時。配置と反応の出方は軽微。所見「実際のアニメーションを見れておらず、Unityもコンパイルがずっと走っているため操作できないが、十字キーが重なって縦が上になっていたりするので、洗練が必要のように見えています。」
- 2026-11-22（チャット）: 「CとAの混合が良いです。画面全体をCのように広げ、地図グラフィックと重なっているパネルUI部分は透過し、今の広いパネルのパーツをより重なっても互いが見えやすい、邪魔にならないデザインにします。」 探索画面の洗練ラフ3案（A 観測卓・B 野帳・C 地図が主役）への回答。Recordinator の方向の問い `01M7ZCPS41GTYK18C3BP6ZS3T6` には入力されていないので、問いは取り下げた。
  この依頼は、上の「SP-017を先に」（同日・再開準備時）より後に出た。
- 2026-11-22（Recordinator `01M7ZCPPXKKC8V0HS21SYQ9G2Y` revision 1、雲と待ち時間を直した版）: 近い／次は別の方向へ。雲の量・雲の形と影・濃さの切替・流れる動き・待ち時間・十字カーソルは全て軽微。気になる領域の記述「カーソルのデザインなど差し替えがデザイン次第の箇所は今後差し替える可能性が高いです。アニメーションやエフェクトなども加えます。雲の流れですが一様に見えるのでより複雑性を出せませんか？」。次の記述「地図上のポイントの位置の蓋然性をどう判定しているか知りたい。また、エリアごとに名前分けできたりするか。今の大地形よりも拡大したときに、巨大構造物などを表示できるか。」
- 2026-11-22（Recordinator `01M7ZR26FG8ND2E55JDM2PMJX3` revision 1、全面の地図へ組み直した探索画面）: 近い／ラフ（C＋A）との差は構想図どおり／次は別の方向へ。配置・部品の透け方・文字と数字・発見した時・カーソルと印・操作の流れ・地域の全体は全て軽微。次の記述「UIエフェクト、サウンド、より拡大した表示、別の回答にも記述したエリアごとの蓋然性の高い区分けなど。」
- 2026-11-22（チャット）: 「VastCoreについてRecordinatorに回答しています。今後、複数のタスクの軸が考えられるため整理して、そのまま可能なら開発を継続してください。」 軸の整理は SP-021 4.1節。
- 2026-11-22（Recordinator `01M80215JPBGRPF79KE7J0YQQF`、エリア区分け・地点の根拠・雲の流れを足した版）: 近い／次はここで十分。「エリアと地点の呼び名」は直したい（次の前に）。エリアの分け方・地図上の地名と境界の見え方・雲の流れ・地点の置き方・発見カードと一覧の表記は軽微。記述は無し。
- 2026-11-22（Recordinator `01M80219XNGW9T27XQMXDZJQFG`、次の軸の進め方）: 次に最初に着手する軸「D 拡大した表示と巨大構造物」／拡大した表示の方式「地図をそのまま細かくする」／エフェクトとアニメーション「開発担当が動きの案を出し本人が選ぶ」／サウンド「仮の音で鳴る場所を先に作る」／デザインの差し替え「差し替えの入口を今作る」／呼び名「seed から固有の地名を自動で作る」。記述は無し。
- 2026-11-22（別のチャットでの本人の発言。そのチャットから開発担当へ伝えられた）: 「次以降の推奨工程について解説してください」「回答しました。推奨で元のセッションに戻し開発再開を促してください。」
- 2026-11-22（Recordinator `01M81JA2CC4WC37TF841JJ8ZEE`、拡大した地形の確認）: 近い／次は今回の続き。地形の細かさ・海岸と川の位置の合い方・川の見え方・細い谷・影と光・2km四方の地面はどれも軽微。記述は無し。
- 2026-11-22（Recordinator `01M81JA6HWR4TJQXZA6T07TC6J`、呼び名の響きと巨大構造物の形の方向）: 呼び名の響き「どれも違う」（記述「方向性を変えるというより、命名方法自体をちゃんとシステム構築したい。」）／地点の呼び名の形「固有名＋の＋構造物の語」（記述「基本的に１と３の混合。適宜変えたい。」。3 は「エリア名＋構造物の語」）／巨大構造物の形の方向「どれも違う」（記述「プリミティブからの構築というより、画像生成から逆算したい。」）／構造物の大きさ「今の大きさが近い」／川の上に来た地点の置き方「川岸へずらす」。
- 2026-11-22（チャット）: 「回答しました。命名のシステムについてはより体系的に構築したいです。」 命名の仕組みの最初の版は SP-021 5節。

## Standing Reporting Preferences

- Use Japanese for normal project reports.
- Keep responses concise and repo-grounded.
- Report observable files, commands, checks, and unresolved items when the user
  asks for strict scope.
- Do not ask the user to re-explain context that the repo already stores.
- When the user asks to keep context in the project and make another terminal
  resume immediately, update repo-local state/handoff docs and push that state
  instead of leaving the handoff only in chat.
- Prefer a small number of enforced owners and automation over adding more
  overlapping instruction resources.

## Backlog Delta

- If a future task needs richer feature status, add or update the narrow owning
  registry/spec instead of expanding `AGENTS.md`.
- If local tool startup friction recurs, keep local machine settings out of
  tracked repo files unless the setting is intentionally shared.

## Operating Rule

Record durable user corrections here when they affect future work. Do not store
one-off status, temporary plans, or completed command logs.
