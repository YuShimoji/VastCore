# Task: Worktree整理とPush統合

## Status
Status: OPEN

## Tier
Tier: 1

## Branch
Branch: develop / feature/TASK_013

## Created
Created: 2026-02-02

## Objective
複数ブランチでの未Pushコミットを整理し、worktree状態をクリーンにする。

## Context
- develop: 40コミット先行（未Push）
- feature/TASK_013: 124コミット先行（未Push）
- worktree汚染: 409行の変更（主にMCPForUnity削除D:390）
- MCPForUnity削除390件はTASK_028として別worktree（cascadeブランチ）で実施済み

## Focus Area
- `.git/` 配下のworktree状態
- `develop` ブランチ
- `feature/TASK_013` ブランチ
- cascadeブランチのMCPForUnity削除コミット（`da0e5b0`）

## Forbidden Area
- リモートへの直接Push（ユーザー承認後のみ）
- ブランチの削除

## DoD
- [ ] worktree状態の詳細調査（409行の変更内訳確認）
- [ ] MCPForUnity削除のdevelopへのマージ判断
- [ ] develop未Pushコミット（40件）の整理方針決定
- [ ] feature/TASK_013未Pushコミット（124件）の整理方針決定
- [ ] `git status` がクリーン（または意図した変更のみ）
- [ ] Push実行前の最終確認レポート作成（`docs/inbox/REPORT_TASK_030_WorktreeCleanup.md`）

## Constraints
- 安全第一：不明な変更は復旧可能な状態を維持
- ユーザー承認後にPush実行
- ブランチ統合方針は明確に記録

## Stopping Conditions
- worktree整理方針が確定し、ユーザー承認待ちの状態になった時点
- または、全て整理完了し、Pushが完了した時点
