using UnityEngine;

namespace Vastcore.Generation
{
    /// <summary>
    /// 地形生成の表現モード種別
    /// Heightmap / Voxel / Mesh など複数の表現を併用するための区別用。
    /// 既存の TerrainGenerationMode（テンプレート/プロシージャル/ハイブリッド）とは別概念。
    /// </summary>
    public enum TerrainRepresentationMode
    {
        Heightmap,
        Voxel,
        Primitive,
        Hybrid,
        Experimental,
    }

    /// <summary>
    /// Heightmap チャネルへのアクセスをカプセル化したラッパー。
    /// 利用可能かどうかを IsAvailable で判定しつつ、安全に参照できるようにする。
    /// </summary>
    public readonly struct HeightmapChannel
    {
        public readonly bool IsAvailable;
        public readonly int Width;
        public readonly int Height;
        public readonly float[,] Data;

        public HeightmapChannel(float[,] data)
        {
            Data = data;
            IsAvailable = data != null;
            Width = data?.GetLength(0) ?? 0;
            Height = data?.GetLength(1) ?? 0;
        }
    }

    /// <summary>
    /// Voxel チャネルのプレースホルダ。
    /// 具体的なボクセル表現は今後の実装で定義する前提とし、
    /// まずは IsAvailable のみを持つ薄いラッパーとしておく。
    /// </summary>
    public readonly struct VoxelChannel
    {
        public readonly bool IsAvailable;

        public VoxelChannel(bool isAvailable)
        {
            IsAvailable = isAvailable;
        }
    }

    /// <summary>
    /// 地形生成処理に渡されるコンテキスト。
    /// 対象領域やシード、出力先 Transform などをまとめて保持する。
    /// </summary>
    public readonly struct TerrainGenerationContext
    {
        /// <summary>
        /// この生成処理が担当するワールド空間上の領域。
        /// 可変サイズチャンクや部分領域を扱う前提で Bounds として定義する。
        /// </summary>
        public readonly Bounds WorldBounds;

        /// <summary>
        /// 高さ場やグリッド解像度。
        /// Heightmap ベースのジェネレータはこれを使用する想定。
        /// </summary>
        public readonly Vector2Int GridResolution;

        /// <summary>
        /// ワールド全体のシード。
        /// </summary>
        public readonly int WorldSeed;

        /// <summary>
        /// チャンク＋ジェネレータ固有のローカルシード。
        /// </summary>
        public readonly int LocalSeed;

        /// <summary>
        /// 生成された GameObject をぶら下げる親 Transform。
        /// </summary>
        public readonly Transform ParentTransform;

        /// <summary>
        /// 高さ場入力／出力用チャネル。
        /// 必要なジェネレータのみが使用する前提で、IsAvailable をチェックしてから参照する。
        /// </summary>
        public readonly HeightmapChannel Heightmap;

        /// <summary>
        /// Voxel 入力／出力用チャネルのプレースホルダ。
        /// </summary>
        public readonly VoxelChannel Voxel;

        /// <summary>
        /// 任意のタグ／メタデータ。
        /// バイオームやデバッグフラグなど、柔軟に付加情報を持たせるための一覧。
        /// </summary>
        public readonly string[] Tags;

        public TerrainGenerationContext(
            Bounds worldBounds,
            Vector2Int gridResolution,
            int worldSeed,
            int localSeed,
            Transform parentTransform,
            HeightmapChannel heightmap,
            VoxelChannel voxel,
            string[] tags)
        {
            WorldBounds = worldBounds;
            GridResolution = gridResolution;
            WorldSeed = worldSeed;
            LocalSeed = localSeed;
            ParentTransform = parentTransform;
            Heightmap = heightmap;
            Voxel = voxel;
            Tags = tags ?? System.Array.Empty<string>();
        }

        /// <summary>
        /// 指定タグを持っているか簡易チェックするユーティリティ。
        /// </summary>
        public bool HasTag(string tag)
        {
            if (Tags == null || tag == null) return false;
            for (int i = 0; i < Tags.Length; i++)
            {
                if (Tags[i] == tag) return true;
            }
            return false;
        }
    }

    /// <summary>
    /// 地形ジェネレータの共通インターフェース。
    /// Heightmap / Voxel / Prefab＋変形など、異なる実装を同じ枠組みで扱うためのベース。
    /// </summary>
    public interface ITerrainGenerator
    {
        /// <summary>
        /// 内部用の一意な ID。設定やデバッグでの識別に使用する。
        /// </summary>
        string Id { get; }

        /// <summary>
        /// エディタや UI 向けの表示名。
        /// </summary>
        string DisplayName { get; }

        /// <summary>
        /// このジェネレータが主に扱う表現モード。
        /// </summary>
        TerrainRepresentationMode Mode { get; }

        /// <summary>
        /// このコンテキストに対してこのジェネレータが適用可能かどうかを判定する。
        /// 例: 特定タグや解像度条件を満たすかどうか等。
        /// </summary>
        bool CanHandle(in TerrainGenerationContext context);

        /// <summary>
        /// 実際に地形生成を行うメイン処理。
        /// context.WorldBounds / ParentTransform などを用いて
        /// GameObject の生成やチャネルへの書き込みを行う。
        /// </summary>
        void Generate(in TerrainGenerationContext context);
    }
}
