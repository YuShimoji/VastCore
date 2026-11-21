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
