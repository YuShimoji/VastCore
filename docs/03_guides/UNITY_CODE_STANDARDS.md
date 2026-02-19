# VastCore Unity Code Standards

本ドキュメントは VastCore プロジェクトにおけるコーディング規約を定める。
Orchestrator / Worker / 人間を問わず、コード変更時に遵守すること。

最終更新: 2026-02-18

---

## 1. 名前空間

### 1.1 命名規則
- すべてのランタイムコードは `Vastcore.*` 名前空間に属する。
- 名前空間はアセンブリの `rootNamespace` と一致させる。
- サブ名前空間は `{rootNamespace}.{SubFeature}` で切る（例: `Vastcore.Generation.Map`）。

### 1.2 禁止事項
- **グローバル名前空間にクラスを定義しない。**
- **同じ完全修飾名の型を複数アセンブリに定義しない。**（CS0029, CS0433 の原因）
- `using` で別アセンブリの名前空間をインポートする場合、そのアセンブリが asmdef の `references` に含まれていることを確認する。

---

## 2. アセンブリ境界

### 2.1 参照ルール
- 依存方向は `docs/02_design/ASSEMBLY_ARCHITECTURE.md` の DAG に従う。
- **逆方向の参照を追加してはならない。**
- 新しい参照が必要な場合、先に ASSEMBLY_ARCHITECTURE.md を更新する。

### 2.2 型の可視性
- 他アセンブリに公開する型のみ `public` にする。
- アセンブリ内部のヘルパーは `internal` にする。
- `[assembly: InternalsVisibleTo("...")]` は Test アセンブリに対してのみ許可。

### 2.3 autoReferenced
- `autoReferenced: true` は Assembly-CSharp (asmdef なしスクリプト) から参照される場合にのみ設定。
- 変更時は ASSEMBLY_ARCHITECTURE.md を同時に更新すること。

---

## 3. Unity API の使用規約

### 3.1 オブジェクト検索
```csharp
// 禁止: interface 型を generic 引数に渡す
var player = FindFirstObjectByType<IPlayerController>(); // CS0311

// 推奨: Tag + GetComponent パターン
var playerObj = GameObject.FindGameObjectWithTag("Player");
var player = playerObj != null ? playerObj.GetComponent<IPlayerController>() : null;
```

### 3.2 ログ出力
```csharp
// 禁止: Unity 標準
Debug.Log("message");

// 推奨: VastcoreLogger
VastcoreLogger.Instance.LogInfo("SystemName", "message");
```

### 3.3 条件コンパイル
```csharp
// 正しい: シンボル参照を #if で完全隔離
#if DEFORM_AVAILABLE
var manager = VastcoreDeformManager.Instance;
manager.RegisterDeformable(obj, quality.deformQuality);
#endif

// 誤り: #if の外にシンボルが漏れている
var manager = VastcoreDeformManager.Instance; // コンパイルエラー
#if DEFORM_AVAILABLE
manager.Register(obj);
#endif
```

### 3.4 Editor 専用コード
```csharp
// Runtime アセンブリ内で Editor API を使う場合
#if UNITY_EDITOR
using UnityEditor;
// ...
AssetDatabase.FindAssets("t:MyAsset");
#endif
```

---

## 4. C# 言語制約

### 4.1 バージョン制限 (C# 9.0)
Unity 6000.x は C# 9.0。以下は使用禁止:

| 機能 | C# バージョン | 代替 |
|------|-------------|------|
| 引数なし struct コンストラクタ | 10.0 | オブジェクト初期化子を使用 |
| `global using` | 10.0 | 各ファイルに `using` を記述 |
| `file` スコープ型 | 11.0 | `internal` クラスを使用 |
| required メンバー | 11.0 | コンストラクタで初期化 |

```csharp
// 禁止: C# 10 の引数なし struct コンストラクタ
public struct Stats {
    public int count;
    public Stats() { count = 0; } // CS8773
}

// 推奨: オブジェクト初期化子
var stats = new Stats { count = 0 };
// または static ファクトリ
public static Stats CreateDefault() => new Stats { count = 0 };
```

---

## 5. 型設計

### 5.1 型の配置
新しい型を追加する前に、`docs/02_design/ASSEMBLY_ARCHITECTURE.md` の「型の配置ルール」を確認し、適切なアセンブリに配置する。

### 5.2 重複防止チェック
型を追加する前に、同名の型が既に存在しないか確認する:
```bash
rg "class MyNewType" Assets/ --glob "*.cs"
```

### 5.3 ScriptableObject
- ゲームパラメータ、バイオーム設定等は ScriptableObject に外部化する。
- `[CreateAssetMenu]` 属性で Inspector から作成可能にする。
- 1つの ScriptableObject クラスは 1 つのアセンブリにのみ定義する。

---

## 6. ドキュメント規約

### 6.1 XML ドキュメント
- `public` クラス、メソッド、プロパティには `<summary>` を付ける。
- 既存コードへの追加時、変更していないメンバーにドキュメントを追加する義務はない。

### 6.2 インラインコメント
- ロジックが自明でない場合のみコメントを追加する。
- 「何をしているか」ではなく「なぜそうしているか」を書く。

---

## 7. 変更時の必須チェックリスト

コードを変更した後、以下を確認すること:

- [ ] 追加した `using` のアセンブリ参照が asmdef に存在するか
- [ ] 同名の型が別アセンブリに存在しないか
- [ ] `#if` シンボルの外にオプショナル依存が漏れていないか
- [ ] C# 9.0 で利用不可能な言語機能を使っていないか
- [ ] 依存方向が ASSEMBLY_ARCHITECTURE.md の DAG に違反していないか
- [ ] `public` にする必要がある型のみ `public` になっているか
