# Composite Structure Assembly Rules 仕様書

- **最終更新日時**: 2026-03-08
- **ステータス**: Draft
- **難易度**: 低〜中
- **前提**: Phase C 完了

---

## 1. 目的

構造物生成をハードコードからデータ駆動に移行する。
CompoundArchitecturalGenerator の反復配置パターンを汎用化し、
ScriptableObject ベースのレシピで任意の構造物を組み立て可能にする。
GrammarEngine のスタブインターフェースを最小実装に置換する。

---

## 2. 既存インフラ

| コンポーネント | 状態 | 役割 |
|--------------|------|------|
| GrammarEngine (Stub) | インターフェースのみ | IGrammarEngine + IGrammarGenerator + StructureBlueprint |
| CompoundArchitecturalGenerator | 動作中 (1310行) | 8種の複合構造 (FortressWall, CathedralComplex 等) |
| ArchitecturalGenerator | 動作中 | 8種の建築要素 (Arch, Cathedral, Colonnade 等) |
| BasePrimitiveGenerator | 動作中 | Cube/Cylinder/Pyramid/Sphere (ProBuilder) |
| WorldGenRecipe | 動作中 | ScriptableObject レシピパターン |
| DeformPresetLibrary | 動作中 | カテゴリ別プリセット管理 |
| StructureBlueprint | 動作中 | footprint + height + floors + interiorGraph |

**欠けているもの**:
- 文法ルール定義構造 (AST/DSL)
- ルールの再帰展開エンジン
- 接続点 (Snap Point) システム
- エディタ UI

---

## 3. コアデータ構造

```csharp
[CreateAssetMenu(menuName = "VastCore/StructureAssemblyRecipe")]
public class StructureAssemblyRecipe : ScriptableObject
{
    public string recipeName;
    public List<CompositionSlot> slots;
}

[Serializable]
public class CompositionSlot
{
    public string slotName;           // "Base", "Column", "Roof"
    public SlotType type;             // Primitive / Architectural / Nested
    public PrimitiveType primitive;   // Cube, Cylinder, etc.
    public ArchitectureType architecture; // Arch, Column, etc.
    public StructureAssemblyRecipe nestedRecipe; // 再帰用

    // 配置パラメータ
    public int repeatCount;           // 反復回数
    public Vector3 spacing;           // 反復間隔
    public Vector3 offset;            // 基準位置からのオフセット
    public Vector3 scaleRange;        // ランダムスケール範囲
    public Vector3 rotationRange;     // ランダム回転範囲
}
```

---

## 4. フェーズ分割

### Phase S1: データ構造定義 (3h)
- StructureAssemblyRecipe ScriptableObject 作成
- CompositionSlot 定義
- StructureBlueprint に `List<CompositionSlot> slots` を追加
- Vastcore.Generation asmdef 内に配置

### Phase S2: 反復ロジック汎用化 (4h)
- CompoundArchitecturalGenerator から共通パターン抽出:
  ```csharp
  static void RepeatAndPlace(
      Transform parent,
      GameObject prefab,
      int count,
      Vector3 spacing,
      Vector3 offset)
  ```
- 8つの Generate*() メソッドの反復部分をこのメソッドに委譲
  (後方互換性維持: 既存メソッドのシグネチャは変えない)

### Phase S3: 最小文法エンジン (6h)
- GrammarEngineStub を実装に置換:
  ```csharp
  public class SimpleGrammarEngine : IGrammarEngine
  {
      public GameObject Generate(StructureAssemblyRecipe recipe)
      {
          foreach (var slot in recipe.slots)
          {
              switch (slot.type)
              {
                  case SlotType.Primitive:
                      RepeatAndPlace(...);
                      break;
                  case SlotType.Architectural:
                      // ArchitecturalGenerator に委譲
                      break;
                  case SlotType.Nested:
                      Generate(slot.nestedRecipe); // 再帰
                      break;
              }
          }
      }
  }
  ```
- 再帰深度制限 (maxDepth = 4) でパフォーマンス保護

### Phase S4: 接続点システム (8h, 将来)
- SnapPoint コンポーネント: 位置 + 方向 + 接続タイプ
- スロット間の自動接続 (SnapPoint マッチング)
- 構造物間のギャップ/重複自動調整

### Phase S5: エディタ UI (6h, 将来)
- StructureAssemblyRecipe カスタムインスペクタ
- スロットのドラッグ&ドロップ並べ替え
- プレビュー生成ボタン

---

## 5. 使用例

```
Tower Recipe:
  Slot "Base"    → Cube, repeat=1, scale=(3,1,3)
  Slot "Column"  → Cylinder, repeat=5, spacing=(0,2,0)
  Slot "Top"     → Pyramid, repeat=1, scale=(4,2,4)

Colonnade Recipe:
  Slot "Column"  → Architectural.Column, repeat=8, spacing=(3,0,0)
  Slot "Beam"    → Cube, repeat=7, spacing=(3,0,0), offset=(1.5,5,0)
```

---

## 6. リスク

- **再帰深度**: 深いネストで ProBuilder メッシュが爆発する可能性
  - 対策: maxDepth=4、メッシュ頂点数上限チェック
  - CompoundArchitecturalGenerator が12要素反復を問題なく処理済み → 3-4階層は安全
- **既存コードとの互換性**: CompoundArchitecturalGenerator の8メソッドは温存
  - 新しいレシピ方式と旧ハードコード方式が共存する過渡期あり

---

## 7. 完了条件

- [ ] Phase S1: StructureAssemblyRecipe が Unity エディタで作成・編集可能
- [ ] Phase S2: RepeatAndPlace が既存 Generate*() メソッドと同等の出力を生成
- [ ] Phase S3: レシピから Tower (Base + Columns + Top) が自動生成されること
