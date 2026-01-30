# 【技術調査報告書】遺跡生成におけるCSG機能の調査と方針転換の全記録（レガシー）

**作成日:** 不明（過去ログ）
**作成者:** AIアシスタント

> ⚠️ **注意**: 本ドキュメントは過去の調査ログであり、現行 `master` のコード/依存関係とは一致しません。
>
> 最新状況（正本）は以下を参照してください:
> - `docs/DEV_HANDOFF_2025-12-12.md`
> - `docs/ISSUES_BACKLOG.md`
> - `FUNCTION_TEST_STATUS.md`
> - `docs/SG1_TEST_VERIFICATION_PLAN.md`
> - `CSG_INTEGRATION_LOG.md`（過去ログ）

---

## 1. エグゼクティブサマリー

本ドキュメントは、「広大な地形上の巨大遺跡」生成プロジェクトにおいて、当初計画していた「プロシージャルな風化表現」を実現するために行った、CSG（ブーリアン演算）機能に関する一連の技術調査と、その結果に基づく方針転換の経緯を記録するものである。

- **当初計画:** 自作エディタ拡張 `StructureGeneratorWindow` のCSG機能を使い、遺跡の柱などをスクリプトで自動的に削り、風化表現を行う計画だった。
- **問題発生:** `StructureGeneratorWindow` のCSG機能は、ProBuilderのAPI仕様変更に追随しておらず、**メニューパスが存在しないため実行エラーとなる致命的なバグ**を抱えていることが判明した。
- **代替案の調査:** バグを回避するため、代替案としてサードパーティ製のCSGライブラリ `KaimaChen/MeshBoolean` の導入を試みた。
- **代替案の失敗:** `MeshBoolean` は、調査の結果、汎用的な3Dメッシュのブーリアン演算ライブラリではなく、**2D地形の穴あけなどに特化した特殊なツールセット**であり、本プロジェクトの目的には適合しないことが判明した。
- **最終結論:** 安定した自動CSG実行手段が現時点で存在しないため、方針を転換。ProBuilderに標準搭載されている**手動のブーリアンツール (`Tools > ProBuilder > Experimental > Boolean (CSG) Tool`) を利用**して、風化表現を作成する。

---

## 2. 調査の経緯

### Step 1: `StructureGeneratorWindow` のCSG機能バグの特定

遺跡の土台と柱を生成後、計画通り `StructureGeneratorWindow` の `Operations` タブからCSGの減算（Subtract）を実行しようとした。

- **現象:** `Perform Boolean Operation` ボタンをクリックしても柱は削られず、コンソールに `ExecuteMenuItem failed because there is no menu named 'ProBuilder/Geometry/Subtract'` というエラーが出力された。
- **原因:** スクリプト `OperationsTab.cs` 内で呼び出しているメニューパス `ProBuilder/Geometry/Subtract` が、現在のProBuilderのバージョンでは廃止・変更されているため。

### Step 2: バグ修正の試みと失敗

当初、このバグはスクリプト内のパスを修正すれば解決できると考え、以下の2つのアプローチを試みたが、いずれも失敗に終わった。

1.  **`BooleanEditor`クラスの直接呼び出し:**
    *   ProBuilderのソースコードを調査し、`BooleanEditor.MenuSubtract()` という `public static` なメソッドを発見。これを直接呼び出すように `OperationsTab.cs` を修正した。
    *   **失敗理由:** `BooleanEditor` クラス自体が `internal` で保護されており、外部アセンブリ（我々のエディタスクリプト）からのアクセスが許可されていなかったため、コンパイルエラー（`CS0122: protection level`）が発生した。

2.  **正しいメニューパスの再調査:**
    *   Web検索やProBuilderのドキュメントを再調査した結果、現在のブーリアン機能は `Tools > ProBuilder > Experimental > Boolean (CSG) Tool` から開く専用UIで行う仕様であり、単一のメニューパスで直接減算を実行するコマンドは提供されていないことが判明した。

### Step 3: 代替ライブラリ `KaimaChen/MeshBoolean` の調査と断念

`StructureGeneratorWindow` の自動化が不可能と判断し、スクリプトから直接呼び出せるサードパーティ製のCSGライブラリとして `KaimaChen/MeshBoolean` を選定し、導入を試みた。

- **インストールの試行:**
    *   Unityパッケージマネージャ経由でのインストールは、`package.json` が無いため失敗。
    *   手動でのファイルコピーを複数回試みたが、筆者（AI）のライブラリ構造に対する理解不足から、ユーザーに誤った手順を複数回にわたり指示してしまい、多大な混乱と時間を費やす結果となった。

- **ライブラリの機能分析と断念:**
    *   最終的にインストールされたファイル群 (`GroundManager.cs` など) を分析した結果、このライブラリが汎用的な3D-CSGツールではなく、「2Dの平面メッシュに円形の穴を開ける」といった、非常に特殊な用途に特化したものであることが判明した。
    *   **結論:** 我々の目的である「3Dの柱を、3Dの立方体で削る」という汎用的な操作には利用できず、代替案として不適格であると判断。

---

## 3. 最終的な方針と次のステップ

以上の調査結果に基づき、**CSG演算による風化表現は、ProBuilderの標準機能を「手動」で利用する**方針に決定する。

- **利点:** 最も確実性が高く、Unityが公式にサポートする方法である。ツールのバグや外部ライブラリの仕様に悩まされることがない。
- **欠点:** 自動化はできないため、複数の柱を処理する際には手作業の繰り返しとなる。

**現在の状況:**
- プロジェクトは、`MeshBoolean`導入前のクリーンな状態に戻されている。
- シーンには、遺跡の土台と12本の柱が配置済みである。
- 1本の柱に重ねる形で、破壊用の立方体 (`Destruction_Tool_01`) が配置されている。

**次の一歩:**
- ProBuilderの標準機能 `Tools > ProBuilder > Experimental > Boolean (CSG) Tool` を使い、手動で柱の減算を実行する。 