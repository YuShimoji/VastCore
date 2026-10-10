# VastCoreをCloud Codexで開発する

CloudではC#・Shader・Editorツールのソースを編集し、Unity構造の検査を実行できる。ライセンス付きUnity Editorがない環境では、コンパイル、EditMode / PlayMode、Designer Cockpitの画面確認を実行していない状態で返す。

## 環境の設定

| 設定 | 値 |
|---|---|
| Repository | `YuShimoji/VastCore` |
| 開発branch | `codex/cloud-codex-environment-20261011`。基点は`codex/vc-development-readiness-20260711`の`4c7f9fa` |
| Unity project root | Repository root (`.`) |
| Unity Editor | `6000.3.6f1`。`ProjectSettings/ProjectVersion.txt`を正本とする |
| ソース検査用runtime | Node.js 22以上、Git。追加のnpm依存は不要 |
| Install script / legacy Setup script | `bash scripts/codex/setup.sh` |
| legacy Maintenance script | `bash scripts/codex/maintenance.sh` |
| Secrets | ソース編集・構造検査には不要 |
| Privacy | Only me |

GitHubの既定branchは`master`で、現在の開発正本とは異なる。新しい環境では上記branchを明示して準備する。既定branchの変更や別branchのmergeは、この環境整備に含めない。

現行CloudではInstall scriptとStart skillを保存してPublishする。既存環境の編集はSave後にRepublishし、新しいtaskで確認する。legacy環境にはSetup / Maintenance scriptの同じコマンドを設定できる。[公式の環境設定手順](https://learn.chatgpt.com/docs/environments/cloud-environments)を参照。

Start skillには次の手順を指定する:

1. Repository rootで`AGENTS.md`と`docs/REPO_LOCAL_RULES.md`を読み、今回の対象に必要な正本を確認する。
2. `bash scripts/codex/maintenance.sh`を実行する。ソース検査だけで完了するため、常駐サービスは不要。
3. C#、Shader、asmdef、Sceneの変更時は対象を限定し、対応する`.meta`を保つ。Unityを使えない検証項目を実行済みと報告しない。
4. 結果は同じtaskと担当ファイルへ保存する。別chatへの通知・ACK・自動転送を行わない。

## 検査とEditorへ渡す範囲

```bash
bash scripts/codex/setup.sh
node --test scripts/codex/check.test.mjs
node scripts/codex/check.mjs
git diff --check
```

検査はmanifest/lockのJSONとdirect依存の存在、asmdef等のJSON、GUIDの妥当性・重複、ファイルの`.meta`の欠落、大小文字が衝突するpathを確認する。trackedと未追跡の編集対象を読み、Libraryや生成csprojに依存しない。C#の意味・型・Unity API互換や画面の動作は検査しない。

LFSのpointerが残っていてもソース編集は可能。Editor import前には、同じrepositoryの認証で`git lfs pull`を行い、`node scripts/codex/check.mjs --require-assets`を通す。認証やLFSが不足した状態でバイナリを代用品へ置き換えない。

Editor検証は正しい版のライセンス付き端末で、既存の`scripts/check-compile.ps1`、`scripts/run-tests.ps1`と変更対象の試験を使う。Cockpitは`docs/DESIGNER_COCKPIT_SMOKE_TEST.md`の実画面確認を残す。環境整備branchはProject Pulseの発行branchを変更しない。

2026-10-11の隔離checkoutでsetup、maintenanceと検査の異常入力7試験を実行した。Unity root判別、17件のJSON、879件のmetaにエラーはなかった。LFS pointerは161件で、Editor検証は未実施。Cloudでの実行結果は環境のsetup reportで確認する。
