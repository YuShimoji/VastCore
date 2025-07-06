# 構造ジェネレータ (StructureGenerator) 取扱説明書

## 1. 概要

`StructureGenerator`は、広大な風景に配置する巨大な人工構造物をプロシージャルに、あるいは手動で効率的に作成するためのUnityエディタ拡張です。
メニューの `Tools > Vastcore > Structure Generator` からウィンドウを開くことができます。

---

## 2. UI構成

UIは4つの主要なタブで構成されています。

- **Generation (生成):** 基本的な形状（プリミティブ）を作成する機能。
- **Operations (加工):** 作成したメッシュを加工・編集する機能。
- **Procedural (プロシージャル):** 複数の部屋や廊下を自動生成する機能。
- **Settings (設定):** 各機能で共通して使用する設定。

---

## 3. 機能詳細

### 3.1. Generation (生成) タブ

新しい構造物の元となる基本形状を生成します。

#### 3.1.1. 標準プリミティブ (Standard Primitive)
UnityのProBuilderが提供する基本的な形状を、サイズを指定して生成します。
- **Primitive Type:** 生成する形状の種類を選択します（Cube, Sphere, Cylinderなど）。
- **Primitive Size:** XYZ各軸のサイズを指定します。
- **`Create Primitive` ボタン:** 指定したパラメータでオブジェクトをシーンに生成します。

#### 3.1.2. 壁 (Wall)
一枚の壁を生成します。
- **Width / Height / Thickness:** 壁の幅、高さ、厚みを指定します。
- **`Create Wall` ボタン:** 指定したパラメータで壁を生成します。

#### 3.1.3. 円柱 (Cylinder)
円柱または多角柱を生成します。
- **Height / Radius:** 円柱の高さと半径を指定します。
- **Subdivisions:** 側面の分割数を指定します。数値が小さいほど角ばった柱になります（例: 4で四角柱）。
- **`Create Cylinder` ボタン:** 指定したパラメータで円柱を生成します。

#### 3.1.4. アーチ (Arch)
アーチ状の形状を生成します。
- **Angle:** アーチの中心角を度数法で指定します（180で半円）。
- **Radius / Thickness / Depth:** アーチの半径、厚み（太さ）、奥行きを指定します。
- **Subdivisions:** アーチの滑らかさを分割数で指定します。
- **End Caps:** アーチの両端をポリゴンで塞ぐかどうか。
- **`Create Arch` ボタン:** 指定したパラメータでアーチを生成します。

### 3.2. Operations (加工) タブ

シーン上のProBuilderメッシュを選択し、様々な加工を施します。

#### 3.2.1. Boolean (CSG) 演算
2つのメッシュを「合成」「切り抜き」「交差」させる機能です。
- **Base Object:** 加工のベースとなるオブジェクトをセットします。
- **Tool Object:** 工具として使うオブジェクトをセットします。
- **Operation:** 実行する演算（Union: 合成, Subtract: AからBを引く, Intersect: AとBの重なる部分）を選択します。
- **`Boolean演算を実行` ボタン:** 演算を実行し、新しいメッシュを生成します。
- **`選択オブジェクトをセット` ボタン:** シーンで選択しているオブジェクトを自動でフィールドにセットします（2つ選択時）。

#### 3.2.2. 配列複製 (Array Duplication)
選択したオブジェクトを直線状または円形に複製します。
- **Type:** `Linear` (直線) または `Circular` (円形) を選択。
- **Count:** 複製する総数。
- **Offset (Linear時):** 複製する際のオフセット（移動量）。
- **Radius (Circular時):** 複製する円の半径。
- **Orient to Center (Circular時):** 複製したオブジェクトを円の中心に向けるかどうか。
- **`Generate Array` ボタン:** 複製を実行します。

#### 3.2.3. ベベル (Bevel)
選択したオブジェクトの全てのエッジを面取りします。
- **Bevel Amount:** 面取りの量を指定します。
- **`Apply Bevel to All Edges` ボタン:** ベベルを適用します。

### 3.3. Procedural (プロシージャル) タブ
（このセクションは現在開発中です）

### 3.4. Settings (設定) タブ
- **Default Material:** 各機能で生成したオブジェクトに自動で適用されるマテリアル。
- **Spawn Position / Rotation:** オブジェクトが生成される際の初期位置と回転。
- **Material Palette:** よく使うマテリアルを登録しておくパレット。

---
最終更新日: 2023/10/28 