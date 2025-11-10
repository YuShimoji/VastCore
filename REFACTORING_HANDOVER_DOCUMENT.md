# 【完全版】プロジェクト引き継ぎドキュメント: StructureGeneratorWindow のリファクタリング

**バージョン: 2.0**
**更新日: [現在の日付]**

---

## 0. エグゼクティブサマリー

このドキュメントは、`StructureGeneratorWindow.cs`のエディタ拡張機能に関するリファクタリング作業の全記録と、今後の開発に向けた引き継ぎ事項をまとめたものです。

- **目的:** 1000行超の巨大クラスを機能ごとに4つのクラスへ分割し、保守性を向上させた。
- **現状:** リファクタリングおよび、それに伴う**ProBuilder APIの破壊的変更への対応は完了**し、コンパイルエラーは解消済み。`Generation`タブおよび**地形ジェネレータの動作テストも完了**。
- **最重要課題:** `StructureGeneratorWindow`の未テスト機能（`Operations`, `Procedural`, `Settings`タブ）の動作確認を、今後の開発タスクの中で実施していく必要がある。
- **今後の開発:** 新機能の実装に着手する前に、本ドキュメント、特にセクション3（API移行の技術的詳細）とセクション4（今後の開発計画）を必ず熟読すること。

---

## 1. 背景: なぜリファクタリングが必要だったか

リファクタリング前の `StructureGeneratorWindow.cs` は、いわゆる「神クラス (God Class)」の状態にあり、以下の深刻な問題を抱えていました。

- **低可読性:** 全てのロジックが一つのファイルに混在し、特定の機能を探し出すのが困難。
- **低保守性:** 一箇所の修正が、予期せぬ別の機能に影響を及ぼす（デグレ）リスクが非常に高かった。
- **低拡張性:** 新しい図形や機能を追加するたびに、巨大なクラスをさらに肥大化させる必要があり、開発速度を著しく低下させていた。

この状況を解決し、将来の機能追加を容易にするため、責務の分離を目的としたリファクタリングが開始されました。

---

## 2. 新しいアーキテクチャ

責務の分離原則に基づき、UIのタブ単位でクラスを分割しました。

- `StructureGeneratorWindow.cs` (コントローラー):
  - 役割: 各タブクラスのインスタンスを保持し、`OnGUI`で適切なタブの描画メソッドを呼び出すだけの軽量な管理クラス。
- `StructureGenerationTab.cs` (図形生成):
  - 役割: プリミティブ図形の生成ロジックと、それに対応するUI要素の描画。
- `OperationsTab.cs` (メッシュ操作):
  - 役割: CSG演算、押し出し、ベベルなど、既存メッシュに対する編集機能とUIの描画。
- `ProceduralTab.cs` (プロシージャル生成):
  - 役割: 複数のステップを要する複雑な構造物（壁、階段など）の生成ロジックとUI。
- `SettingsTab.cs` (共通設定):
  - 役割: 生成位置、マテリアル適用、テスト環境作成など、全タブで共有される設定項目とユーティリティ機能。

---

## 3. 技術的深掘り: ProBuilder API 移行の苦闘の全記録

今回の作業で最も困難だったのは、**ProBuilder APIのサイレントかつ破壊的な変更**への対応でした。このセクションは、将来同じ問題に直面しないための警告と教訓です。

### 3.1. 直面した問題の分類

- **APIの廃止:** `SetFaces`, `PivotEditing` 等が完全に削除された。
- **APIの内部化 (Internalization):** `Toolbar`, 旧`BevelEdges`などが `internal` スコープに変更され、我々のエディタアセンブリから直接呼び出せなくなった。
- **APIの仕様変更:** `ProBuilderMesh`に標準の`Mesh`を直接代入できなくなった (`sharedMesh`経由)。エッジの取得方法も`topology.edges`から変更された。
- **名前空間の非公開化:** ブーリアン演算で不可欠な`Csg`クラスを含む`UnityEngine.ProBuilder.Csg`名前空間が、公開APIではなくなった。

### 3.2. 最も困難だった実装: ブーリアン演算 (CSG)

CSG機能は、安定した公開APIが存在せず、以下の試行錯誤を繰り返しました。

1.  **試行1: `BooleanTool` (失敗):** API自体が廃止されていた。
2.  **試行2: `Csg.Perform` (失敗):** `UnityEngine.ProBuilder.Csg`名前空間を追加しても、アセンブリが公開されていないため参照できず、コンパイルエラーとなった。これは、パッケージとしてソースコードが公開されていても、アセンブリ定義(.asmdef)によって外部への公開が制限されている典型的な例である。
3.  **試行3: `Toolbar.DoBooleanOperation` (失敗):** `Toolbar`クラスが`internal`であるため、保護レベル違反でアクセスできなかった。

4.  **最終解決策: `EditorApplication.ExecuteMenuItem` (採用):**
    - **ロジック:** ユーザーがメニューをクリックする操作を、コードで完全に模倣する。
      1. 操作対象となる複数のGameObjectを `Selection.objects` に設定する。
      2. `EditorApplication.ExecuteMenuItem("ProBuilder/Geometry/Subtract")` のように、メニューのパスを文字列で指定して実行する。
    - **利点:** ProBuilderの内部実装から完全に独立しており、メニュー構造が変わらない限り、将来のアップデートでも動作し続ける可能性が最も高い。
    - **欠点:** 文字列による実行のため、タイプミスに弱く、コンパイル時チェックが効かない。また、メニューパスが将来変更されるリスクは残る。

**教訓: サードパーティ製アセットの非公開APIに依存したエディタ拡張は、アップデート時に破綻するリスクが極めて高い。可能な限り、メニュー実行など、より抽象的で安定した方法を模索するべきである。**

### 3.3. 副次的なエラー: Burst Compiler `Failed to find entry-points`
リファクタリングの過程で、C#のコンパイルエラーと同時にBurstコンパイラのエラーが頻繁に発生しました。
- **原因:** このエラーは、C#コードのコンパイルが失敗したために、Burstコンパイラが依存するアセンブリ(`Assembly-CSharp-Editor.dll`など)を正しく読み込めなかったことに起因する二次的な問題です。
- **解決策:** C#のコンパイルエラー（本ドキュメントに記載されているProBuilder APIの問題など）をすべて解決することで、このBurstエラーも自然に解消されました。
- **教訓:** 今後このエラーに遭遇した場合、まずはBurstコンパイラ自体ではなく、先行するC#のコンパイルエラーを調査・修正することが解決への近道です。

---

## 4. 今後の開発計画とロジック

このセクションは、これから実装する新機能の設計とロジックを記述するためのものです。

### 4.1. (例) 機能A: 「窓枠付きの壁」自動生成

- **目的:** 現在の「壁生成」と「開口部作成」を組み合わせ、窓枠メッシュも同時に生成する複合機能を実装する。
- **UI/UX:**
  - `ProceduralTab` に新しいセクションを追加。
  - 壁のサイズ（幅、高さ、厚み）を指定。
  - 窓の数、サイズ、配置（均等割付など）を指定。
  - 窓枠の太さを指定。
- ** proposed Logic:**
  1. `StructureGenerationTab`の壁生成ロジックを呼び出し、ベースとなる壁を作成 (`CreateSizedCube`)。
  2. UIで指定された窓の数と配置に基づき、開口部の中心座標を計算するループ処理を開始。
  3. ループ内で、`OperationsTab`の開口部作成ロジック（`ExecuteMenuItem`を使用）を呼び出し、壁に穴を開ける。
  4. 開口部の座標とサイズ、窓枠の太さから、窓枠を構成する4つの立方体（上下左右）のサイズと位置を計算。
  5. `CreateSizedCube`を4回呼び出して窓枠メッシュを生成。
  6. (オプション) 壁、窓枠を一つのオブジェクトとして結合する。
- **ProBuilder API利用予測:**
  - `CreateSizedCube` (自作ヘルパー)
  - `EditorApplication.ExecuteMenuItem("ProBuilder/Geometry/Subtract")`
  - `CombineMeshes.Perform`

### 4.2. 機能B: 「巨大遺跡」のプロシージャル生成
- **目的:** 生成した広大な地形の上に、古代の遺跡やSF的な巨大建造物をプロシージャルに（複数の機能を組み合わせて）生成する。
- **UI/UX:** `StructureGeneratorWindow`の各タブを横断的に使用する。
- ** proposed Logic:**
  1. **土台の生成:** `Generation`タブを使い、巨大な円柱または立方体を遺跡の土台として生成する。
  2. **柱の配置:** `Operations`タブの円形配列(`Circular Array`)機能を使用し、土台の上に多数の巨大な柱を円状に配置する。
  3. **中心構造物の作成:** `Generation`タブで中心にさらに高いタワーを生成する。
  4. **風化表現:** `Operations`タブのCSG減算(`Subtract`)機能を利用する。小さな立方体や球体を複数生成し、柱や壁と減算させることで、部分的に破壊され、風化したような見た目を作り出す。
  5. **結合:** 全てのパーツを`Combine`機能で一つにまとめ、単一のゲームオブジェクトとして完成させる。
- **ProBuilder API利用予測:**
  - `CreateSizedCube`, `CreateCylinder` (自作ヘルパー)
  - `EditorApplication.ExecuteMenuItem` (配列、CSG演算など)
  - `CombineMeshes.Perform`

---

## 5. 推奨される開発プラクティスと次の一歩

今回の経験に基づき、今後の開発では以下のプラクティスを強く推奨します。

### 5.1. ProBuilder API 抽象化レイヤーの導入

ProBuilderのAPIを直接各タブクラスから呼び出すのではなく、**仲介役となる静的クラス (`ProBuilderAdapter.cs`など) を作成する**ことを提案します。

```csharp
// 例: Assets/Editor/ProBuilderAdapter.cs
public static class ProBuilderAdapter
{
    public static ProBuilderMesh CreateCube(Vector3 size) { /* ... */ }

    public static void Subtract(ProBuilderMesh target, ProBuilderMesh source)
    {
        Selection.objects = new GameObject[] { target.gameObject, source.gameObject };
        EditorApplication.ExecuteMenuItem("ProBuilder/Geometry/Subtract");
    }
    // 他のProBuilder操作も同様にラップする...
}
```

- **利点:**
  - 将来、ProBuilder APIに再度変更があっても、修正箇所はこの`ProBuilderAdapter`クラスのみに限定される。
  - 各機能クラスは、ProBuilderの複雑な仕様（メニュー実行など）を意識する必要がなくなり、コードがクリーンになる。

### 5.2. 厳格な動作テストの実施 (最優先タスク)

リファクタリングとAPI移行の影響範囲が不明なため、以下のチェックリストに従い、**全機能の動作確認を必ず実施してください。**

**[x] `Generation` タブ:** [x] Cube, [x] Cylinder, [x] Wall, [x] Sphere, [x] Torus, [x] Pyramid
**[ ] `未テスト機能群`:** [ ] `Operations`タブ, [ ] `Procedural`タブ, [ ] `Settings`タブの各機能は、今後の開発タスク（セクション4.2など）の中で、必要に応じて随時テストを実施する。

---

このドキュメントが、本プロジェクトの継続的な開発における信頼できる情報源となることを願っています。 

---

## 6. 【補遺】地形ジェネレータの修復とテスト (2024/XX/XX実施)

`StructureGeneratorWindow`のリファクタリング完了後、プロジェクトの目標である「広大な地形と巨大な人工物」のイメージを再現するため、地形生成機能の調査とテストを実施した。

### 6.1. 発見された問題: NullReferenceException
- **現象:** メニューの`Tools > Vastcore > Heightmap Terrain Generator`からウィンドウを開くと、`NullReferenceException`が発生し、UIが正常に描画されない。
- **原因:** `HeightmapTerrainGeneratorWindow.cs`スクリプト内で、シリアライズして状態を保持すべき`List<T>`型のフィールドに `[SerializeField]` 属性が付与されておらず、エディタ再起動時や再コンパイル時にリストのインスタンスが失われていた。
- **解決策:** 対象のフィールドに`[SerializeField]`を追加し、リストが正しくシリアライズされるように修正した。

### 6.2. 地形生成プロセスの確立
修復後、以下の手順で地形生成のテストを成功裏に完了した。
1.  **Terrain Layerの作成:** プロジェクト内に`Terrain Layer`アセットが存在しなかったため、草、土、岩の3種類のレイヤーアセットを `Assets/MapGenerator/TerrainLayers` フォルダに手動で作成した。
2.  **テクスチャルールの設定:** ジェネレータウィンドウで、各`Terrain Layer`に対し、表示されるべき高さ(Height)と傾斜(Slope)の範囲を指定した。
3.  **プレビュー生成:** `Combine Heightmaps & Generate Preview`ボタンで、ハイトマップに基づいた起伏とテクスチャを持つ地形プレビューを生成した。
4.  **水理浸食の適用:** `Apply Erosion to Preview`ボタンで、浸食シミュレーションを実行。これにより、より自然で滑らかな地形形状に進化した。
5.  **ゲームオブジェクトの確定:** `Generate Terrain GameObject`ボタンで、完成した地形を永続的なゲームオブジェクトとしてシーンに保存した。

---

## 7. 【補遺】アセンブリ定義の整理とUnityコンパイルエラーの解決 (2025/11/07実施)

Unity Editorのコンパイル時に発生していた `ArgumentNullException: Value cannot be null. Parameter name: key` エラーを解決するためのアセンブリ定義ファイル(.asmdef)の全面的な整理を実施した。

### 7.1. 発見された問題

- **存在しないパッケージ参照:** `Vastcore.Editor.StructureGenerator.asmdef` が存在しない `Deform`, `Parabox.CSG` を参照。
- **未導入パッケージ参照:** `Vastcore.UI.asmdef` が未導入の `Unity.Rendering.DebugUI` を参照。
- **空参照ファイル:** `Assets/MapGenerator/Scripts/Vastcore.Generation.asmref` が空ファイルで存在。
- **循環依存のリスク:** EditorアセンブリとRuntimeアセンブリの依存関係が整理されていない。

### 7.2. 実施した修正

1. **Vastcore.Editor.StructureGenerator.asmdef の修正:**
   - `overrideReferences` を `false` に戻し、標準参照解決へ統一。
   - 存在しない参照 (`Deform`, `Parabox.CSG`) と versionDefines を削除。

2. **Vastcore.UI.asmdef の修正:**
   - 未導入パッケージ `Unity.Rendering.DebugUI` の参照を削除。

3. **空ファイルの削除:**
   - `Vastcore.Generation.asmref` を削除。

4. **新規アセンブリ定義の作成:**
   - `Vastcore.Editor.asmdef` を作成し、Editorウィンドウ (`TerrainTemplateEditor`, `TerrainAssetBrowser`) を専用アセンブリに配置。

### 7.3. 検証結果

- 全 `.asmdef` が存在するアセンブリのみを参照する状態に整理。
- Unity再起動後のコンパイルエラーが解消。
- `Window > VastCore` メニューにEditorウィンドウが表示されることを確認。

### 7.4. 今後の推奨事項

- `.asmdef` 変更時は必ずUnity再起動とコンパイルテストを実施。
- 新規パッケージ導入時は依存関係の検証を徹底。
- Editor/Runtimeの境界を明確にし、循環依存を回避。

---

このドキュメントが、プロジェクトの継続開発における参考となることを願います。 