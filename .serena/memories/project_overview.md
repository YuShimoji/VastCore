# VastCore - Project Overview

## Purpose
大規模テレイン生成・レンダリングエンジン。Unity製。2D/3Dテレイン生成、Marching Squares、Dual Grid、Voxel等のアルゴリズムを実装。

## Tech Stack
- **Engine**: Unity (URP想定)
- **Language**: C# (.NET Standard 2.1, C# 9.0制約)
- **Input**: Input System (New)
- **Solution**: `VastCore.slnx`

## Project Structure
```
Assets/
  Scripts/           # メインC#スクリプト
    Camera/          カメラシステム
    Core/            コアユーティリティ
    Deform/          地形変形システム
    Editor/          カスタムエディタ
    Game/            ゲームロジック
    Generation/      プロシージャル生成
    Player/          プレイヤーコントロール
    Terrain/         テレイン管理
    Testing/         テスト用
    Tests/           ユニットテスト
    UI/              UIシステム
    Utilities/       ユーティリティ
    WorldGen/        ワールド生成
  MapGenerator/      マップ生成モジュール (asmdef)
  Deform/            地形変形プラグイン
  _Scripts/          (レガシー?)
docs/
  01_planning/       計画・ロードマップ
  02_design/         設計・アーキテクチャ (ASSEMBLY_ARCHITECTURE.md)
  03_guides/         ガイド (COMPILATION_GUARD_PROTOCOL.md, UNITY_CODE_STANDARDS.md)
  04_reports/        作業レポート
  tasks/             タスクチケット
(deleted)            OpenSpec変更提案プロセスは廃止。CLAUDE.md SPEC FIRSTに移行
```

## Key Documents (必読)
- `docs/02_design/ASSEMBLY_ARCHITECTURE.md` — asmdef依存グラフ、名前空間規約
- `docs/03_guides/UNITY_CODE_STANDARDS.md` — コーディング規約、禁止パターン
- `docs/03_guides/COMPILATION_GUARD_PROTOCOL.md` — コンパイルエラー診断手順
- `docs/SSOT_WORLD.md` — SSOT階層
- `AGENTS.md` — AI Agent行動規約

## Current Phase
Phase A (Stabilization) 完了、Phase B テスト基盤強化完了（TB-1~4, PB-1/PB-2）。v1.0.0安定版。EditModeテスト100+追加済み。TODO残3件。次: Phase C 機能完成（Deform正式統合 + CSG検証）。

## Critical Design Rules
1. Debug.Log禁止 → `VastcoreLogger.Instance.LogInfo(...)` を使用
2. 引数なしstructコンストラクタ禁止 (C# 9非対応)
3. 下位→上位のasmdef参照追加禁止 (循環参照防止)
4. 同名型の複数アセンブリ定義禁止
5. 場当たり修正禁止 → COMPILATION_GUARD_PROTOCOL.mdに従う
