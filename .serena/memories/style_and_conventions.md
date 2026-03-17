# Code Style and Conventions

## Naming (Unity C# standard)
- クラス/メソッド: PascalCase
- privateフィールド: _camelCase
- SerializeField: _camelCase
- 定数: UPPER_SNAKE
- 名前空間: asmdefに合わせる (ASSEMBLY_ARCHITECTURE.md参照)

## Prohibited Patterns
| 禁止 | 理由 | 代替 |
|------|------|------|
| `Debug.Log` | ログ統一 | `VastcoreLogger.Instance.LogInfo(...)` |
| `FindFirstObjectByType<IInterface>()` | CS0311 | `FindGameObjectWithTag` + `GetComponent` |
| 引数なしstructコンストラクタ | C# 9非対応 | オブジェクト初期化子 / static factory |
| 同名型の複数アセンブリ定義 | CS0029 | 1型=1アセンブリ |
| `#if`外のオプショナルシンボル参照 | CS0103 | `#if`で完全隔離 |
| 下位→上位のasmdef参照追加 | 循環参照 | アーキテクチャを見直す |
| `autoReferenced`の無断変更 | Assembly-CSharp破壊 | ASSEMBLY_ARCHITECTURE.mdと同時更新 |

## Change Process
1. CLAUDE.md の SPEC FIRST に従い docs/ 配下に仕様を記述
2. アセンブリ影響範囲を含む仕様を docs/ に記録
3. タスク分解 → 実装 → 検証 → アーカイブ

## AI Rules
- 日本語で返信
- 実装前にASSEMBLY_ARCHITECTURE.mdを必ず確認
- 実装後にCOMPILATION_GUARD_PROTOCOLの最小検証を実施
- レポートに変更ファイル、アセンブリ名、using追加、コンパイル確認を記載
