using UnityEngine;

namespace Vastcore.Core
{
    /// <summary>
    /// 生成系と地形系の循環依存を避けるために切り出したプリミティブタイプ定義。
    /// Terrain 側（PrimitiveTerrainGenerator）からも参照されます。
    /// </summary>
    public enum GenerationPrimitiveType
    {
        // 基本プリミティブ
        Cube,           // 巨大立方体
        Sphere,         // 巨大球体
        Cylinder,       // 巨大円柱
        Pyramid,        // 巨大ピラミッド

        // 複合プリミティブ
        Torus,          // 巨大ドーナツ型
        Prism,          // 巨大角柱
        Cone,           // 巨大円錐
        Octahedron,     // 巨大八面体

        // 特殊プリミティブ
        Crystal,        // 結晶構造
        Monolith,       // モノリス（石柱）
        Arch,           // アーチ構造
        Ring,           // リング構造

        // 地形統合プリミティブ
        Mesa,           // メサ（台地）
        Spire,          // 尖塔
        Boulder,        // 巨石
        Formation       // 岩石層
    }
}
