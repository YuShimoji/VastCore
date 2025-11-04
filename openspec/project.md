# VastCore Project Context

## Overview
VastCore is a Unity-based terrain generation and exploration game engine. It provides procedural terrain generation, dynamic loading, and interactive gameplay features.

## Tech Stack
- **Engine**: Unity 6000.3.0b2
- **Language**: C#
- **Architecture**: Modular assembly structure (Vastcore.*.asmdef)
- **UI Framework**: Custom UI system (Vastcore.UI)
- **Rendering**: Universal Render Pipeline (URP)
- **Version Control**: Git
- **CI/CD**: GitHub Actions with auto-merge for quality gates

## Project Structure
- `Assets/Scripts/`: Core gameplay code
  - `Core/`: Base utilities and managers
  - `Generation/`: Terrain generation systems
  - `Terrain/`: Terrain rendering and management
  - `Player/`: Player controllers and movement
  - `UI/`: User interface components
- `Assets/Editor/`: Editor tools and inspectors
- `docs/`: Documentation and reports
- `Packages/`: Unity package dependencies

## Coding Conventions
- **Namespaces**: `Vastcore.*` for all components
- **Naming**: PascalCase for classes, camelCase for variables
- **Documentation**: XML comments for public APIs
- **Error Handling**: Custom logger system (`VastcoreLogger`)
- **Async**: Coroutines for Unity-specific async operations

## Current Status
- ✅ Compilation errors resolved
- ✅ UI migration completed (NarrativeGen.UI → Vastcore.UI)
- ✅ Basic terrain generation working
- ✅ Player movement and camera systems implemented
- ✅ LOD and performance optimizations added

## Future Tasks Roadmap

| フェーズ | タスク名 | 優先度 | ステータス | 概要 |
|---------|---------|-------|-----------|-----|
| Phase 3 | Deform System Integration | 高 | 進行中 | Deformパッケージ（v1.2.2）の統合。ランタイムメッシュ変形、視覚品質向上。 |
| Phase 4 | Terrain Streaming System | 中 | 未着手 | 地形のストリーミングシステム実装。 |
| Phase 5 | Advanced Terrain Synthesis | 中 | 未着手 | 高度な地形合成アルゴリズム。 |
| Phase 6 | Random Control Extensions | 低 | 未着手 | ランダム制御の拡張。 |
| Quality Gates | Unit Tests 80% Coverage | 高 | 未着手 | ユニットテストカバレッジを80%まで向上。 |
| Quality Gates | Security Scans | 高 | 未着手 | 高/重要脆弱性のゼロ化。 |
| Quality Gates | Performance 60 FPS | 中 | 未着手 | パフォーマンス最適化。 |
| Quality Gates | Code Review Process | 低 | 未着手 | Tier 2+変更のコードレビュープロセス確立。 |
| OpenSpec Changes | Player Controller Refinement | 中 | 未着手 | プレイヤーコントローラーの改良。 |
| OpenSpec Changes | Terrain Visual Improvement | 中 | 未着手 | 地形視覚効果の向上。 |
| OpenSpec Changes | UI Realtime Optimization | 低 | 未着手 | UIリアルタイム最適化。 |

## Blind Spot Proposals (Critical Analysis)

### セキュリティ強化
- APIキー管理: 環境変数や暗号化ストレージを使用
- 入力検証: ユーザー入力のサニタイズ強化
- 依存関係スキャン: 定期的な脆弱性スキャン

### ドキュメント・運用
- ユーザー向けドキュメント: エンドユーザー向けマニュアル作成
- APIドキュメント自動生成: DoxygenでHTML生成
- 変更ログ管理: 自動生成CHANGELOG.md

### CI/CD強化
- 自動デプロイ: ビルド→テスト→デプロイスクリプト
- ロールバック戦略: 失敗時の自動ロールバック
- マルチプラットフォームビルド: 並行ビルド対応

### ユーザーエクスペリエンス（UX）
- モバイル対応: タッチ操作最適化
- アクセシビリティ: 色覚障害者対応、キーボード操作
- ローカライズ: 多言語対応

### 拡張性・アーキテクチャ
- プラグインシステム: Package Manager経由拡張
- モジュール化強化: asmdef細分化、循環参照防止
- 設定管理: ScriptableObjectベース強化

### パフォーマンス・品質
- メモリ管理: 地形データ監視、GC最適化
- プロファイリング統合: 自動レポート生成
- 統合テスト/E2Eテスト: シーン単位テスト、実機自動化

### コミュニティ・ビジネス
- コミュニティ管理: Discord/GitHub Discussionsサポート
- ビジネスモデル検討: 無料/有料版、DLC展開
- フィードバック収集: ユーザー調査、アナリティクス統合

## Quality Gates
- Unit tests: 80% coverage minimum
- Security scans: No high/critical vulnerabilities
- Performance: 60 FPS target
- Code review: Required for Tier 2+ changes

## AI Development Guidelines
- Use OpenSpec for all feature changes
- Maintain backward compatibility
- Test all changes in Unity editor
- Update documentation for API changes
- Follow existing code patterns and architecture
