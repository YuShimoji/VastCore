using System;
using System.Collections.Generic;
using UnityEngine;

namespace Vastcore.WorldGen.GrammarEngine
{
    /// <summary>
    /// 建築構造の抽象設計図。
    /// </summary>
    [Serializable]
    public sealed class StructureBlueprint
    {
        public List<Vector3> footprint = new List<Vector3>();
        public float height = 6f;
        public int floors = 1;
        public object interiorGraph;
    }
}
