# VastCore テストプラン

## 目的
ProBuilder API移行後の動作確認とコンパイルエラー修正の検証を行います。

## テスト環境
- Unity バージョン: 2023.3+
- ProBuilder バージョン: 6.0+
- プロジェクト: VastCore Terrain Engine

## 前提条件チェック

### 1. コンパイル状態
- [ ] Unity Editorでプロジェクトを開く
- [ ] Console ウィンドウでエラー数を確認（0であるべき）
- [ ] CS0436警告が消えているか確認
- [ ] その他の警告を確認（CS0219, CS0414は許容範囲）

### 2. アセンブリの確認
- [ ] `Vastcore.Generation.asmdef` が正しくコンパイルされているか
- [ ] `Vastcore.Terrain.asmdef` が正しくコンパイルされているか
- [ ] 重複DLL（`Vastcore.Generation.dll`）が存在しないか確認

## テストケース

### Phase 1: 基本コンパイルテスト ✅

#### TC-001: プロジェクトコンパイル
**目的:** プロジェクト全体が正常にコンパイルされることを確認

**手順:**
1. Unity Editorを起動
2. VastCoreプロジェクトを開く
3. 自動コンパイルを待つ
4. Consoleを確認

**期待結果:**
- エラー数: 0
- CS0436警告: 0（重複クラス警告が消えている）
- CS0219/CS0414警告: あっても問題なし（未使用変数）

**状態:** 🟡 実施待ち

---

### Phase 2: プリミティブ生成テスト

#### TC-002: 基本プリミティブ生成（Subdivision無し）
**目的:** 細分化なしでプリミティブが正常に生成されることを確認

**前提条件:**
- テストシーンまたは新規シーンを準備
- `PrimitiveTerrainGenerator`クラスが使用可能

**手順:**
1. 以下のテストスクリプトを作成・実行:

```csharp
using UnityEngine;
using Vastcore.Generation;

public class BasicPrimitiveTest : MonoBehaviour
{
    void Start()
    {
        // Cubeの生成テスト
        var cubeParams = PrimitiveTerrainGenerator.PrimitiveGenerationParams.Default(
            PrimitiveTerrainGenerator.PrimitiveType.Cube
        );
        cubeParams.position = Vector3.zero;
        cubeParams.subdivisionLevel = 0; // 細分化なし
        
        var cube = PrimitiveTerrainGenerator.GeneratePrimitiveTerrain(cubeParams);
        
        if (cube != null)
        {
            Debug.Log("✓ Cube generated successfully");
        }
        else
        {
            Debug.LogError("✗ Cube generation failed");
        }
        
        // Sphereの生成テスト
        var sphereParams = PrimitiveTerrainGenerator.PrimitiveGenerationParams.Default(
            PrimitiveTerrainGenerator.PrimitiveType.Sphere
        );
        sphereParams.position = new Vector3(150, 0, 0);
        sphereParams.subdivisionLevel = 0;
        
        var sphere = PrimitiveTerrainGenerator.GeneratePrimitiveTerrain(sphereParams);
        
        if (sphere != null)
        {
            Debug.Log("✓ Sphere generated successfully");
        }
        else
        {
            Debug.LogError("✗ Sphere generation failed");
        }
    }
}
```

**期待結果:**
- Cubeが正常に生成される
- Sphereが正常に生成される
- 警告ログ: Subdivisionが無効化されている旨の警告が出る（これは正常）
- エラー: なし

**状態:** 🟡 実施待ち

---

#### TC-003: 全プリミティブタイプの生成確認
**目的:** 16種類すべてのプリミティブが生成可能か確認

**手順:**
1. 各プリミティブタイプで生成テストを実行
2. 生成結果をシーンで確認

**プリミティブタイプ:**
- [ ] Cube
- [ ] Sphere
- [ ] Cylinder
- [ ] Pyramid
- [ ] Torus
- [ ] Prism
- [ ] Cone
- [ ] Octahedron
- [ ] Crystal
- [ ] Monolith
- [ ] Arch
- [ ] Ring
- [ ] Mesa
- [ ] Spire
- [ ] Boulder
- [ ] Formation

**期待結果:**
- すべてのタイプが生成される
- メッシュが視覚的に確認できる
- コライダーが正しく設定されている

**状態:** 🟡 実施待ち

---

### Phase 3: 高品質プリミティブテスト

#### TC-004: HighQualityPrimitiveGenerator テスト
**目的:** 高品質生成システムが動作することを確認

**手順:**
1. `HighQualityPrimitiveGenerator.GenerateHighQualityPrimitive()` を呼び出し
2. 各品質レベルでテスト:
   - Low
   - Medium
   - High
   - Ultra

**期待結果:**
- 各品質レベルで正常に生成される
- 品質設定に応じた詳細度の違いが見られる
- エラーなし（警告は許容）

**状態:** 🟡 実施待ち

---

### Phase 4: 既存テストの実行

#### TC-005: 単体テストスイート
**目的:** 既存の単体テストが通ることを確認

**テストファイル:**
- `BiomePresetManagerTest.cs`
- `ComprehensivePrimitiveTest.cs`
- `RuntimeTerrainManagerTest.cs`
- `PrimitiveQualityTestRunner.cs`

**手順:**
1. Unity Test Runner を開く（Window → General → Test Runner）
2. Play Mode テストを実行
3. Edit Mode テストを実行（該当する場合）

**期待結果:**
- すべてのテストがパス
- 失敗したテストがあれば、原因を調査

**状態:** 🟡 実施待ち

---

## 既知の問題と制限事項

### ProBuilder API無効化の影響

1. **Subdivide機能**
   - メッシュ細分化が無効化されています
   - `subdivisionLevel > 0` を指定しても細分化されません
   - 警告ログが出力されますが、これは正常動作です

2. **RebuildFromMesh機能**
   - 外部メッシュからの再構築が無効化されています
   - アーチ構造など一部のプリミティブに影響があります

3. **スムージング・最適化**
   - `SetSmoothingGroup` が無効化されています
   - `Optimize` が無効化されています
   - 視覚的な品質に若干の影響があるかもしれません

### 回避策
現時点では、これらの機能を必要としない基本的なプリミティブ生成を使用してください。
代替実装は今後のアップデートで提供予定です。

---

## テスト結果記録

### 実施日時: [未実施]

#### Phase 1 結果
| TC番号 | テスト名 | 結果 | 備考 |
|--------|----------|------|------|
| TC-001 | プロジェクトコンパイル | - | - |

#### Phase 2 結果
| TC番号 | テスト名 | 結果 | 備考 |
|--------|----------|------|------|
| TC-002 | 基本プリミティブ生成 | - | - |
| TC-003 | 全プリミティブタイプ | - | - |

#### Phase 3 結果
| TC番号 | テスト名 | 結果 | 備考 |
|--------|----------|------|------|
| TC-004 | HighQualityPrimitiveGenerator | - | - |

#### Phase 4 結果
| TC番号 | テスト名 | 結果 | 備考 |
|--------|----------|------|------|
| TC-005 | 単体テストスイート | - | - |

---

## 次のステップ

### Unity Editorでの実施が必要
このテストプランを実行するには、Unity Editorを開く必要があります。

**推奨手順:**
1. Unity Hub から VastCore プロジェクトを開く
2. このテストプランに従って各フェーズを実施
3. 結果を記録
4. 問題があれば Issue として記録

### 自動テスト化
将来的には、これらのテストをUnity Test Runnerで自動化することを検討してください。

---

## 参考資料
- `PROBUILDER_API_MIGRATION.md` - API移行に関する詳細情報
- Unity Test Runner ドキュメント: https://docs.unity3d.com/Manual/testing-editortestsrunner.html
