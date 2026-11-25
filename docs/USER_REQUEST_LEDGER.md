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
- 2026-11-23（Recordinator `01M81SA3GF8YMRZRETVZCHBGE2`、命名の仕組みの方向）: 名前の成り立ち「どれも違う」。記述「骨格のみ用意し、音の質感は付け替え可能にする。また、複数の名称を許容する。たとえば星系のような記号的な命名もあれば、別名として固有名詞が付いている場合があるなど。また、その土地に設定されている歴史的経緯や性質によって命名の方式も変化させたい。たとえば文明のない完全に未知の異世界の土地ならまず記号名、文明が存在するなら語根のある土地名が別でついている、など。」
- 2026-11-23（同じ問いの残りの設問）: ことばの範囲「地域（seed）ごとに 1 つ（今回の版）」／音と語根「seed から自動のまま」／地点名の型「重要な地点だけがエリアの名を負う」／由来「一覧など別の場所だけに出す」、記述「発見カードにも出したいです。」／足したいもの「事実から意味への対応も規則のテキストで書き換える」「人名・出来事・古い呼び名に由来する名前」「地域全体・川・山・湾そのものの名前」「語がつながる時の音の変化」。命名の第2版は SP-021 5節。
- 2026-11-23（チャット）: 「もう一度試してください」 直前の処理が失敗した後の発言。開発担当は再試行の指示と解釈し、回答の読み直しと、回答が解いた作業を続けた。
- 2026-11-23（Recordinator `01M81W8NN4XG16SQXMV2R3NBKT` revision 1、探索画面へ統合した拡大表示）: 近い／前の版（拡大しても広域の絵が粗くなるだけだった版）と比べて良くなった／次は今回の続き。広域から細部へ切り替わる見え方・右上の札（拡大率・作成中の表示）・細部の地形そのものの見え方・広域の地図との一致・拡大中のサーチと発見とカーソルの表示・縮めて広域へ戻る見え方は全て軽微（余裕があれば）。操作ごとのしやすさ（1〜7）は、ホイールで拡大して細部が出るまで待つ・細部のまま右ドラッグで動かす・矢印キーで微調整する・拡大した画面でサーチする・Home／縮小で全体へ戻る の5項目とも 4。記述は無し。
- 2026-11-23（Recordinator `01M821KYJMRRFKP1VY1F6Q1HS2` revision 1、山頂のモノリス4枚の画像を作る経路）: 「この環境で、生成の道具を使えるようにする」（選択肢の説明は「本人が、この作業の環境で使える画像生成の道具を用意する（利用条件は本人が確認する）。使えるようになったら、エージェントが4枚を生成する」）。「生成前に外したい方向」は未回答。
- 2026-11-23（Recordinator `01M825M5QZP791BTF91XA1FDAS` revision 1、命名の第2版の受入）: 近い／次は別の方向へ。記述「サーチシステムなどのゲーム性を向上させたい。」 骨格と音の質感の分け方・複数の名前の持ち方・土地の経緯の決まり方・記号名の形・土地の名の作り・発見カードの別名と由来・発見した地点の一覧の由来・地図上の名前・音の響きは全て軽微（余裕があれば）。
- 2026-11-23（Recordinator `01M82CXPP4H2DKENWRZT0V0MGH` revision 1、サーチのゲーム性の方向）: どの案が近いか「組み合わせ」、記述「CとBがより近い。ゲームシステムとして。ナラティブについては要検討。」（C は「拠点から足を伸ばす」、B は「手掛かりを読む」）。今のサーチで足りないもの「強い反応の後は、輪の中を総当たりするだけ」「見つけても一覧に載るだけで、次に効かない」「目的や終わりが無い」「反応に方角が無い」、記述「モニュメント発見に寄っており、資材獲得や何らかの基地発見など、より広範で実利的な目的を持たせたい。」
- 2026-11-23（同じ問いの残りの設問）: 「など」に入るもの「発見した地点の中身（記録・説明・拡大した時の姿）」「探す目的・依頼（何を探しに行くかが示される）」「別の地域（seed）をまたぐ収集や記録」「拡大表示との連動（細部でしか見つからない地点など）」、記述「または発見により得た通貨を利用できるストア。探索のための資材、乗り物のパーツ、サーチシステムのパーツなどを入手できるようにしたい。」。最初に作る大きさ「選んだ案の最小の実装まで」。
- 2026-11-23（Recordinator `01M828W77SDBJKNBXA22T8XJ8B` revision 1、探索画面の動きの案）: どの案が近いか「B 地図で広がる」、記述「BとCを取り入れた動き方が良いです。」（C は「部品で余韻」）。カーソルの移動・サーチの反応・発見・カードと知らせの帯と一覧の出入りは、4つとも「B 地図で広がる」。動きの長さ「長く（×1.6）」。進める範囲「選んだ案の最小の実装まで」。削る動きは未回答。
- 2026-11-23（Recordinator `01M825M7PECFN9BY6CFVJREM58` revision 1、名前の出し方の方向）: 既定の音の質感「地域（seed）ごとに質感を自動で選ぶ」／エリア・川・山・湾の名前が分かる時「最初から全部出す（今回の版）」／山の名前「文明のあるエリアから近い高い峰には、そのことばで土地の名も付ける」／土地の経緯を誰が決めるか「地域ごとに 1 つ（この地域は未踏、この地域は古い文明、のように地域全体で決める）」。記述は無し。
- 2026-11-24（Recordinator `01M847PRXSXYDYJC00SH7ZDYYS`、名前の出し方の受入。本人が差し戻した。差し戻し ID `01M84SDFCQ37SMQRDM6DXVKBVT`）: 理由「言葉の響きに没頭しすぎています。また、これは好き好みの問題でなく、生成システムの問題です。」 5設問とも未回答のまま閉じた。
- 2026-11-24（Recordinator `01M84NE26VM5MZAYR7QNM4D2WB`、鳴る場所の受入。本人が差し戻した。差し戻し ID `01M84SFCMF719G9S18YKD09AVJ`）: 理由「やっていること自体がズレている。Claudeが生成している音であることがわかり易すぎる。」 5設問とも未回答のまま閉じた。
- 2026-11-24（Recordinator `01M84QPNKRE5XXVTF20ZHEN8RF`、段6の目的と見返りの案）: どの案が近いか「案A 持ち帰って整える（通貨とストア）」。取り入れたい要素・外したい要素・最初に作る大きさは未回答（2026-11-24 に読んだ時点）。
- 2026-11-24（同じ問いの残りの設問）: 取り入れたい要素「別の要素」・外したい要素「別の要素」・最初に作る大きさ「保留」。記述は3つとも「保留」。所見（`01M84STGF0NQB6TGBG5QM7NVW3`）「以下の発見物だけでいつのまにか分類が作られていたり色々ゲーム部分を勝手に決めすぎ。」に続けて、行ごとに「モノリス」「アーチ」「環」「塔」「ドーム」「段丘沈んだ遺構」。
- 2026-11-24（Recordinator `01M831H77K6HV96N1BYM6J0JZK` revision 1、動きの受入）: 「一部直したい」、記述「サーチの波紋が対象物との距離だと誤解が生じる。」。場面ごとは、カーソルの移動だけ「このままでよい」、サーチの反応・発見・遠征の部品・遠征の終わり・前の規則（反応だけ）は「直したい（次の前に）」。記述「全体的に恣意的過ぎる。演出が機能を兼ねているように見えるが、そうではないため、ややこしい。また、矢印が出る案はその座標を指しているなら直截的過ぎる。ゲーム側が答えを知っているように誘導して振る舞うのはユーザー側からすると苛つく。」
- 2026-11-24（同じ問いの残りの設問）: C から足した3つ（座標の数え上げ・サーチボタンと反応の棒の光・一覧の行と新しい手掛かりの淡い光）は3つとも「残して直す」、記述「全て保留」。動きの長さ「場面で違う」、記述「保留」。削る動きは未回答。
- 2026-11-24（Recordinator `01M82V588FZE0C05C0YP34DNGN`、遠征の規則の受入。本人が差し戻した。差し戻し ID `01M84XYFJ9E0JYCE967Y8WNW0M`）: 理由「タスクがレガシー化しているため」。4設問とも未回答のまま閉じた。
- 2026-11-24（チャット、親の Game Projects で）: 「今現在のこのプロジェクトで間違った方向に進みつつあるので訂正してほしいです。」 開発担当が出した候補3つ（VastCore の作業対象・VastCore の進め方・親の Game Projects）から、本人は「VastCoreの作業対象」と「VastCoreの進め方」を選んだ。下の2行は選ばれた選択肢の文で、開発担当が書いた。
  - 作業対象:「VastCore が探索画面の表面（命名の響き・動き・音・遠征の規則・通貨とストア）へ局所化している。訂正: 次の作業を、鳥瞰図の詳細化と巨大構造物のプロシージャル生成へ戻す。段6（ゲームの中身）と表面の軸は止め、未回答の受入の問いは取り下げる。」
  - 進め方:「受入・方向の問いを大量に出し、仕組みの欠陥を好みの設問にし、AIが作った中身（音・名前）を成果として見せている。訂正: 開発担当が出力を数えて欠陥を直してから見せる。問いは方式の分岐だけに絞る規則を VastCore の正本へ入れる。」

- 2026-11-24（チャット、自然地形の生成可否と人工地形の要望）。本人原文

  > 次が作れるようになっていますか？
  > 砂漠地帯、森林地帯、大河、入江など。
  >
  > また、次のような人工的な地形が作れるようにしたいです。
  > ・巨大都市（中世・現代・未来）
  > ・高速道路など都市間を結ぶ交通網
  > ・基地や空港や工場など、ある程度の高さから視認可能な施設
  > ・その他、散逸的に建物が存在する地帯など。

  続く本人指示「上記推奨で進めてください。完了後にPushしてください。」初回の現代試作は2都市・都市間幹線・空港1か所・郊外の建物。

- 2026-11-24（チャット、親の Game Projects で。現代試作の建物の見え方）。本人原文

  > VastCoreで上空からの建物などの再現をこころみていますが、今はプリミティブな形状が重なっているだけです。Googleマップの記号的なレイヤーでの表現としてはこれでも良いですが、より近づいて写真に切り替わったときのような詳細さを出したいです。

  開発担当の読み（本人の確認は未）: 今の 8 km の拡大図（1 画素 約7.8 m）は記号の層として残し、もっと寄った時に航空写真の縮尺（1 画素 約0.5〜2.5 m）の細部へ切り替える。最初の実装は SP-021 5節「写真の縮尺の細部」。

- 2026-11-24（チャット、SP-021 の雲）。本人原文

  > このプロジェクトの雲に関して、以下の仕様を実現したいです。
  > ・地上の天気
  > ・ハリケーンのような特殊な雲の流れ方
  > ・地域や地形に特有の雲の流れ方が出来ているか

  鳥瞰図の比較試作は `RegionMapPreview --weather-preview`。地域の風向と地形・湿り気から雲と地表の雨を求め、風向反転と渦を比較する。渦の構成は [NOAA/NHC](https://www.nhc.noaa.gov/marinersguide.pdf)、湿った風が斜面を上昇して雲と雨を生む点は [気象庁](https://www.jma.go.jp/jma/kids/kids/faq/a5_21.html) を参照。探索画面への統合と地上視点の要否は未決。

- 2026-11-25（チャット、親の Game Projects で。鳴る場所の差し戻し `01M84NE26VM5MZAYR7QNM4D2WB` の後の音の扱い）: 開発担当の問い「開発担当が合成した音は、どう扱いますか？」（選択肢: 既定で切る（鳴る場所と差し替えの入口は残す）／コードごと外す／別の素材へ替える）に、本人は「既定で切る」。音を用意する経路は未決。同じ日に本人は、命名の生成の欠陥を直す提案カード（規則の案は開発担当）を自分で始めた。

- 2026-11-25（チャット、3D化の相談）。本人原文

  > このプロジェクトの現在生成可能なマップの3D化について相談したいです。
  > ・今はいわゆるUnityでのTerrain的なマップになっているが、これを3D化させた際にちゃんと複雑性の高いマップにしたい。
  > ・単にのっぺりとした、マップを3Dハイトマップにしたものではなく、ちゃんと冒険できる高低差や起伏や導線のあるマップにしたい。
  > ・特にハイトマップだと、崖下や洞窟のような折り返された地形が出ないので、その辺りを克服したい。

  開発担当の提案は3段（B 崖と段のある高さ場 → C 通行の区分と道筋 → D 地点の周りだけの局所の体積）。問い「段階B（拡大窓に崖と段を入れる試作）から始めますか？」に、本人は「始める」と答えた。段階 B の絵を見せた後の問い「段階Bの崖の出方は、この方向でよいですか？」には「この方向でよい」と答えた。残る判断4件は SP-021 9節。その後の問い2つには、次の作業「段階C 道筋の解析」、洞窟・アーチのための局所の体積「局所だけ承認」と答えた（選択肢の文は SP-021 9節と [INVARIANTS](INVARIANTS.md)）。

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
