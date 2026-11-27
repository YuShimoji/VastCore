using System;
using System.Collections.Generic;

namespace Vastcore.RegionMap
{
    /// <summary>チャンクストリーミングの距離リング。</summary>
    public enum RegionChunkRing : byte
    {
        /// <summary>近景（512 m、257²、Collider とディテール有効）。</summary>
        Near = 0,
        /// <summary>中景（1536 m、Collider なし）。</summary>
        Mid = 1,
        /// <summary>遠景スカート（6144 m、広域 RegionData 直参照）。</summary>
        Far = 2,
    }

    /// <summary>リングと整数格子座標で一意に定まるチャンク識別子。</summary>
    public struct RegionChunkId : IEquatable<RegionChunkId>
    {
        public readonly RegionChunkRing Ring;
        public readonly int X;
        public readonly int Z;

        public RegionChunkId(RegionChunkRing ring, int x, int z)
        {
            Ring = ring;
            X = x;
            Z = z;
        }

        public bool Equals(RegionChunkId other)
        {
            return Ring == other.Ring && X == other.X && Z == other.Z;
        }

        public override bool Equals(object obj)
        {
            return obj is RegionChunkId other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = (int)Ring;
                hash = (hash * 397) ^ X;
                hash = (hash * 397) ^ Z;
                return hash;
            }
        }

        public static bool operator ==(RegionChunkId a, RegionChunkId b) { return a.Equals(b); }
        public static bool operator !=(RegionChunkId a, RegionChunkId b) { return !a.Equals(b); }

        public override string ToString()
        {
            return Ring + "(" + X + "," + Z + ")";
        }
    }

    /// <summary>リング 1 層あたりのチャンク仕様。</summary>
    [Serializable]
    public sealed class RegionChunkRingSpec
    {
        public RegionChunkRing Ring;
        public float ChunkMeters;
        public int Resolution;
        public int RadiusChunks;
        public bool HasCollider;
        public bool HasDetails;
        public float SkirtDepthMeters;
    }

    /// <summary>
    /// フォーカス座標周囲の Ring 0 / Ring 1 / Ring 2 チャンク割当と境界座標を計算する。UnityEngine 非依存。
    /// </summary>
    public sealed class RegionChunkPlanner
    {
        public readonly RegionChunkRingSpec[] Rings;

        public RegionChunkPlanner(RegionChunkRingSpec[] rings = null)
        {
            Rings = rings ?? CreateDefaultRings();
        }

        public static RegionChunkRingSpec[] CreateDefaultRings()
        {
            return new[]
            {
                new RegionChunkRingSpec
                {
                    Ring = RegionChunkRing.Near,
                    ChunkMeters = 512f,
                    Resolution = 257,
                    RadiusChunks = 1, // 3x3 = 1536m x 1536m
                    HasCollider = true,
                    HasDetails = true,
                },
                new RegionChunkRingSpec
                {
                    Ring = RegionChunkRing.Mid,
                    ChunkMeters = 1536f,
                    Resolution = 130, // 129 cells: 43 per 512 m near chunk.
                    RadiusChunks = 2, // 5x5 = 7680m x 7680m
                    HasCollider = false,
                    HasDetails = false,
                    SkirtDepthMeters = 24f,
                },
                new RegionChunkRingSpec
                {
                    Ring = RegionChunkRing.Far,
                    ChunkMeters = 6144f,
                    Resolution = 133, // 132 cells: 11 per 512 m near chunk.
                    RadiusChunks = 3, // 7x7 = 43008m x 43008m
                    HasCollider = false,
                    HasDetails = false,
                    SkirtDepthMeters = 60f,
                },
            };
        }

        public RegionChunkRingSpec GetSpec(RegionChunkRing ring)
        {
            for (int i = 0; i < Rings.Length; i++)
                if (Rings[i].Ring == ring) return Rings[i];
            throw new ArgumentOutOfRangeException("ring");
        }

        public RegionChunkId ChunkAt(RegionChunkRing ring, double eastM, double southM)
        {
            RegionChunkRingSpec spec = GetSpec(ring);
            int cx = (int)Math.Floor(eastM / spec.ChunkMeters);
            int cz = (int)Math.Floor(southM / spec.ChunkMeters);
            return new RegionChunkId(ring, cx, cz);
        }

        public RegionChunkId ChunkWithinWorld(RegionChunkRing ring, double eastM, double southM, double worldSizeMeters)
        {
            RegionChunkId id = ChunkAt(ring, eastM, southM);
            int last = LastChunkIndex(GetSpec(ring), worldSizeMeters);
            return new RegionChunkId(ring, Math.Clamp(id.X, 0, last), Math.Clamp(id.Z, 0, last));
        }

        private static int LastChunkIndex(RegionChunkRingSpec spec, double worldSizeMeters)
        {
            return Math.Max(0, (int)Math.Ceiling(worldSizeMeters / spec.ChunkMeters) - 1);
        }

        public void ChunkBoundsMeters(RegionChunkId id, out double minEastM, out double minSouthM, out float sizeMeters)
        {
            RegionChunkRingSpec spec = GetSpec(id.Ring);
            sizeMeters = spec.ChunkMeters;
            minEastM = id.X * (double)sizeMeters;
            minSouthM = id.Z * (double)sizeMeters;
        }

        public void ChunkCenterMeters(RegionChunkId id, out double centerEastM, out double centerSouthM)
        {
            ChunkBoundsMeters(id, out double minE, out double minS, out float size);
            centerEastM = minE + size * 0.5;
            centerSouthM = minS + size * 0.5;
        }

        /// <summary>
        /// 指定フォーカス周囲でロードすべき全チャンクを、距離の近い順（かつ近景優先）で返す。
        /// ワールド範囲 [0, worldSizeMeters] の外にあるチャンクは除外する。
        /// </summary>
        public List<RegionChunkId> PlanRequiredChunks(double focusEastM, double focusSouthM, double worldSizeMeters)
        {
            var result = new List<RegionChunkId>(64);
            var scored = new List<KeyValuePair<RegionChunkId, double>>(64);

            for (int r = 0; r < Rings.Length; r++)
            {
                RegionChunkRingSpec spec = Rings[r];
                RegionChunkId center = ChunkWithinWorld(spec.Ring, focusEastM, focusSouthM, worldSizeMeters);
                int maxIdx = LastChunkIndex(spec, worldSizeMeters);

                for (int dz = -spec.RadiusChunks; dz <= spec.RadiusChunks; dz++)
                {
                    int cz = center.Z + dz;
                    if (cz < 0 || cz > maxIdx) continue;
                    for (int dx = -spec.RadiusChunks; dx <= spec.RadiusChunks; dx++)
                    {
                        int cx = center.X + dx;
                        if (cx < 0 || cx > maxIdx) continue;
                        var id = new RegionChunkId(spec.Ring, cx, cz);
                        ChunkCenterMeters(id, out double ce, out double cs);
                        double distSq = (ce - focusEastM) * (ce - focusEastM) + (cs - focusSouthM) * (cs - focusSouthM);
                        // Ring 0 を最優先、次に Ring 1、Ring 2 の順に並べるため距離にリングオフセットを加算
                        double priority = (int)spec.Ring * 1e12 + distSq;
                        scored.Add(new KeyValuePair<RegionChunkId, double>(id, priority));
                    }
                }
            }

            scored.Sort((a, b) => a.Value.CompareTo(b.Value));
            for (int i = 0; i < scored.Count; i++)
                result.Add(scored[i].Key);
            return result;
        }

        /// <summary>
        /// 2D→3D ダイブ直前に最優先で生成する着地・背景チャンク。
        /// </summary>
        public List<RegionChunkId> PlanDivePrefetch(double targetEastM, double targetSouthM, double worldSizeMeters)
        {
            var result = new List<RegionChunkId>(20);
            result.Add(ChunkWithinWorld(RegionChunkRing.Near, targetEastM, targetSouthM, worldSizeMeters));
            AddRingBox(result, RegionChunkRing.Far, targetEastM, targetSouthM, 1, worldSizeMeters);
            AddRingBox(result, RegionChunkRing.Mid, targetEastM, targetSouthM, 1, worldSizeMeters);
            return result;
        }

        private void AddRingBox(List<RegionChunkId> list, RegionChunkRing ring, double eastM, double southM,
            int radius, double worldSizeMeters)
        {
            RegionChunkRingSpec spec = GetSpec(ring);
            RegionChunkId center = ChunkWithinWorld(ring, eastM, southM, worldSizeMeters);
            int maxIdx = LastChunkIndex(spec, worldSizeMeters);
            for (int dz = -radius; dz <= radius; dz++)
            {
                int cz = center.Z + dz;
                if (cz < 0 || cz > maxIdx) continue;
                for (int dx = -radius; dx <= radius; dx++)
                {
                    int cx = center.X + dx;
                    if (cx < 0 || cx > maxIdx) continue;
                    list.Add(new RegionChunkId(ring, cx, cz));
                }
            }
        }
    }
}
