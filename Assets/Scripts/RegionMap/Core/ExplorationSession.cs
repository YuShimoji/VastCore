using System;
using System.Collections.Generic;

namespace Vastcore.RegionMap
{
    /// <summary>サーチした位置での反応の強さ。</summary>
    public enum SearchSignal
    {
        None = 0,
        Faint = 1,
        Strong = 2,
        Found = 3,
    }

    public struct SearchResult
    {
        public SearchSignal Signal;

        /// <summary>発見した地点。発見していなければ null。</summary>
        public RegionSite Site;

        /// <summary>いちばん近い未発見地点までの距離（地図座標）。未発見が残っていなければ負の値。</summary>
        public float Distance;

        /// <summary>いちばん近い未発見地点（反応の方角を出すため）。未発見が残っていなければ null。発見した時は Site と同じ。</summary>
        public RegionSite Nearest;
    }

    /// <summary>
    /// 1 回の探索の状態。地図座標を指定してサーチすると、近くに隠れた地点があれば発見し、
    /// 無ければ「強い反応・弱い反応・反応なし」を返す。表示や入力には依存しない。
    /// </summary>
    public sealed class ExplorationSession
    {
        private readonly RegionData _data;
        private readonly HashSet<int> _discoveredIds = new HashSet<int>();
        private readonly List<RegionSite> _discovered = new List<RegionSite>();

        /// <summary>この距離（地図座標）以内でサーチすると発見になる。</summary>
        public float FoundRadius = 2.5f;

        /// <summary>強い反応・弱い反応になる距離（地図座標）。</summary>
        public float StrongRadius = 8f;
        public float FaintRadius = 18f;

        public int SearchCount { get; private set; }

        public ExplorationSession(RegionData data)
        {
            if (data == null) throw new ArgumentNullException("data");
            _data = data;
        }

        public RegionData Region
        {
            get { return _data; }
        }

        public IReadOnlyList<RegionSite> Discovered
        {
            get { return _discovered; }
        }

        public int TotalSites
        {
            get { return _data.Sites.Count; }
        }

        public bool IsDiscovered(RegionSite site)
        {
            return _discoveredIds.Contains(site.Id);
        }

        /// <summary>サーチを 1 回行う。近くの未発見地点があれば発見として記録する。</summary>
        public SearchResult Search(float mapX, float mapY)
        {
            SearchCount++;
            SearchResult result = Sense(mapX, mapY);
            if (result.Signal == SearchSignal.Found)
            {
                _discoveredIds.Add(result.Site.Id);
                _discovered.Add(result.Site);
            }
            return result;
        }

        /// <summary>回数を消費せず、発見もせずに、その位置の反応だけを調べる。</summary>
        public SearchResult Sense(float mapX, float mapY)
        {
            RegionSite nearest = null;
            float best = float.MaxValue;
            for (int s = 0; s < _data.Sites.Count; s++)
            {
                RegionSite site = _data.Sites[s];
                if (_discoveredIds.Contains(site.Id)) continue;
                float dx = site.MapX - mapX;
                float dy = site.MapY - mapY;
                float d = dx * dx + dy * dy;
                if (d < best)
                {
                    best = d;
                    nearest = site;
                }
            }

            var result = new SearchResult();
            if (nearest == null)
            {
                result.Signal = SearchSignal.None;
                result.Distance = -1f;
                return result;
            }

            float distance = (float)Math.Sqrt(best);
            result.Distance = distance;
            result.Nearest = nearest;
            if (distance <= FoundRadius)
            {
                result.Signal = SearchSignal.Found;
                result.Site = nearest;
            }
            else if (distance <= StrongRadius) result.Signal = SearchSignal.Strong;
            else if (distance <= FaintRadius) result.Signal = SearchSignal.Faint;
            else result.Signal = SearchSignal.None;
            return result;
        }

        /// <summary>発見済みとサーチの回数を消して、始めの状態へ戻す（同じ地域でもう一度探す時）。</summary>
        public void Reset()
        {
            _discoveredIds.Clear();
            _discovered.Clear();
            SearchCount = 0;
        }

        /// <summary>保存してあった発見済みの番号を戻す。</summary>
        public void Restore(IEnumerable<int> discoveredIds)
        {
            _discoveredIds.Clear();
            _discovered.Clear();
            foreach (int id in discoveredIds)
            {
                for (int s = 0; s < _data.Sites.Count; s++)
                {
                    if (_data.Sites[s].Id != id) continue;
                    if (_discoveredIds.Add(id)) _discovered.Add(_data.Sites[s]);
                    break;
                }
            }
        }

        public List<int> SaveDiscoveredIds()
        {
            var ids = new List<int>(_discovered.Count);
            for (int s = 0; s < _discovered.Count; s++) ids.Add(_discovered[s].Id);
            return ids;
        }
    }
}
