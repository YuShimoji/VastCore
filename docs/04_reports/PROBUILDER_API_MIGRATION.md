# ProBuilder API移行ガイド

## 概要
ProBuilder 6.0（Unity 2023.3+対応）へのアップグレードにより、いくつかのAPIが変更されました。
このドキュメントは、無効化された機能と今後の対応方針をまとめています。

## 無効化された機能

### 1. メッシュ細分化（Subdivide）
**影響を受けるファイル:**
- `Assets/Scripts/Terrain/Map/PrimitiveTerrainGenerator.cs`
- `Assets/Scripts/Terrain/Map/HighQualityPrimitiveGenerator.cs`
- `Assets/Scripts/Generation/PrimitiveTerrainGenerator.cs` (廃止予定)

**旧API:**
```csharp
mesh.Subdivide();
```

**現状:**
```csharp
// TODO: Subdivide機能は一時的に無効化（ProBuilderのAPI変更により）
Debug.LogWarning($"Subdivision feature is temporarily disabled due to ProBuilder API changes. Requested level: {subdivisionLevel}");
```

**代替案（検討中）:**
- `UnityEngine.ProBuilder.MeshOperations.ConnectElements`を使用した手動細分化
- カスタム細分化アルゴリズムの実装
- 参考: https://discussions.unity.com/t/how-to-subdivide-probuilder-faces-with-code-runtime/768524

### 2. メッシュ再構築（RebuildFromMesh）
**影響を受けるファイル:**
- `Assets/Scripts/Terrain/Map/PrimitiveTerrainGenerator.cs` (行387-389)
- `Assets/Scripts/Terrain/Map/HighQualityPrimitiveGenerator.cs` (行558-560)

**旧API:**
```csharp
proBuilderMesh.RebuildFromMesh(meshFilter.sharedMesh);
```

**現状:**
```csharp
// TODO: RebuildFromMesh機能はProBuilder API変更により一時的に無効化
Debug.LogWarning($"RebuildFromMesh feature is temporarily disabled due to ProBuilder API changes.");
```

**代替案（検討中）:**
- `ProBuilderMesh.Create()`を使用した新規メッシュ作成
- 直接頂点・面データを設定する方法

### 3. スムージンググループ（SetSmoothingGroup）
**影響を受けるファイル:**
- `Assets/Scripts/Terrain/Map/HighQualityPrimitiveGenerator.cs` (行1033-1037)

**旧API:**
```csharp
mesh.SetSmoothingGroup(mesh.faces, 1);
```

**現状:**
```csharp
// TODO: SetSmoothingGroup機能はProBuilder API変更により一時的に無効化
Debug.LogWarning($"SetSmoothingGroup feature is temporarily disabled due to ProBuilder API changes.");
```

**代替案（検討中）:**
- Unity標準の`Mesh.RecalculateNormals()`を使用
- ProBuilderの新しいノーマル計算APIを調査

### 4. メッシュ最適化（Optimize）
**影響を受けるファイル:**
- `Assets/Scripts/Terrain/Map/HighQualityPrimitiveGenerator.cs` (行1058-1062)

**旧API:**
```csharp
mesh.Optimize();
```

**現状:**
```csharp
// TODO: Optimize機能はProBuilder API変更により一時的に無効化
Debug.LogWarning($"Optimize feature is temporarily disabled due to ProBuilder API changes.");
```

**代替案（検討中）:**
- `MeshUtility.CollapseSharedVertices()`を使用
- `EditorMeshUtility.Optimize()`（エディタ内のみ）

### 5. UV展開とメッシュ検証
**影響を受けるファイル:**
- `Assets/Scripts/Terrain/Map/HighQualityPrimitiveGenerator.cs` (行1040-1063)

**旧API:**
```csharp
UnwrapParameters unwrapParams = UnwrapParameters.Default;
Unwrapping.Unwrap(mesh, unwrapParams);
MeshValidation.EnsureMeshIsValid(mesh);
```

**現状:**
無効化済み（コメントアウト）

## CS0436警告の解決

### 問題
重複する`PrimitiveTerrainGenerator`クラスが2つのファイルに存在し、型衝突が発生していました：
- `Assets/Scripts/Generation/PrimitiveTerrainGenerator.cs` (185行、簡易版)
- `Assets/Scripts/Terrain/Map/PrimitiveTerrainGenerator.cs` (763行、完全版)

### 解決策
`Assets/Scripts/Generation/PrimitiveTerrainGenerator.cs`を無効化（マルチラインコメントで囲む）しました。
このファイルは将来的に削除予定です。

## 今後のアクションアイテム

### 優先度：高
1. **Unity Editorでのコンパイル確認**
   - CS0436警告が解消されているか確認
   - その他のコンパイルエラー・警告の確認

2. **基本的な動作テスト**
   - プリミティブ生成機能の動作確認
   - 既存の細分化無しでの生成が正常に動作するか確認

### 優先度：中
3. **ProBuilder 6.0 APIドキュメントの精査**
   - 公式ドキュメント: https://docs.unity3d.com/Packages/com.unity.probuilder@6.0/manual/
   - GitHub変更ログ: https://github.com/Unity-Technologies/com.unity.probuilder/blob/master/CHANGELOG.md
   - 特に`MeshOperations`名前空間の新しいメソッドを調査

4. **代替実装の開発**
   - Subdivide機能の代替実装
   - RebuildFromMesh機能の代替実装
   - スムージング・最適化機能の代替実装

### 優先度：低
5. **パフォーマンステスト**
   - 無効化された機能がパフォーマンスに与える影響を測定
   - 必要に応じて最適化

6. **ドキュメント更新**
   - ユーザー向けドキュメントの更新
   - APIリファレンスの更新

## Unity Editorでの確認手順

1. **プロジェクトを開く**
   - Unity Hub経由でVastCoreプロジェクトを開く

2. **コンソールの確認**
   - Windowメニュー → General → Console
   - エラー（赤）と警告（黄）の数を確認
   - CS0436警告が消えているか確認

3. **スクリプト再コンパイル**
   - Assets → Reimport All（必要に応じて）
   - または、任意のC#ファイルを編集して保存

4. **基本テストシーンの実行**
   - テストシーンを開く（存在する場合）
   - Play modeで実行し、エラーが発生しないか確認

5. **プリミティブ生成テスト**
   - 簡単なプリミティブ（Cube、Sphere等）の生成を試す
   - Subdivisionレベル0での生成を確認
   - 警告ログの内容を確認

## 参考リンク

- ProBuilder 6.0 ドキュメント: https://docs.unity3d.com/Packages/com.unity.probuilder@6.0/
- ProBuilder Scripting API: https://docs.unity3d.com/Packages/com.unity.probuilder@6.0/api/
- Unity Discussions - Subdivide: https://discussions.unity.com/t/how-to-subdivide-probuilder-faces-with-code-runtime/768524
- GitHub CHANGELOG: https://github.com/Unity-Technologies/com.unity.probuilder/blob/master/CHANGELOG.md

## 変更履歴

- 2025-01-25: 初版作成（CS0436警告解決、ProBuilder API無効化対応）
