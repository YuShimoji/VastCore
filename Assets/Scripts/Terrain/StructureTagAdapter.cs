using System.Collections.Generic;
using ArchitecturalType = Vastcore.Generation.ArchitecturalGenerator.ArchitecturalType;
using CompoundArchitecturalType = Vastcore.Generation.CompoundArchitecturalGenerator.CompoundArchitecturalType;
using PrimitiveType = Vastcore.Generation.PrimitiveTerrainGenerator.PrimitiveType;
using CrystalSystem = Vastcore.Generation.CrystalStructureGenerator.CrystalSystem;

namespace Vastcore.Generation
{
    /// <summary>
    /// 既存enumとタグシステムを接続するアダプター。
    /// 既存の生成コードを変更せずにタグシステムに組み込む。
    /// 各enumの値に対してデフォルトの StructureTagProfile を返す。
    /// </summary>
    public static class StructureTagAdapter
    {
        #region Tag Name Constants
        // 形態タグ
        public const string TAG_ARCH = "arch";
        public const string TAG_TOWER = "tower";
        public const string TAG_WALL = "wall";
        public const string TAG_DOME = "dome";
        public const string TAG_COLUMN = "column";
        public const string TAG_BRIDGE = "bridge";
        public const string TAG_ENCLOSURE = "enclosure";
        public const string TAG_SPIRE = "spire";
        public const string TAG_STEPPED = "stepped";
        public const string TAG_CRYSTAL = "crystal";

        // 属性タグ
        public const string TAG_MASSIVE = "massive";
        public const string TAG_ORNATE = "ornate";
        public const string TAG_WEATHERED = "weathered";
        public const string TAG_FORTIFIED = "fortified";
        public const string TAG_SACRED = "sacred";
        public const string TAG_FUNCTIONAL = "functional";
        public const string TAG_ELEGANT = "elegant";
        public const string TAG_PRIMITIVE = "primitive";
        public const string TAG_ORGANIC = "organic";
        public const string TAG_GEOMETRIC = "geometric";
        #endregion

        #region Cached Profiles
        private static Dictionary<ArchitecturalType, StructureTagProfile> s_ArchitecturalProfiles;
        private static Dictionary<CompoundArchitecturalType, StructureTagProfile> s_CompoundProfiles;
        private static Dictionary<PrimitiveType, StructureTagProfile> s_PrimitiveProfiles;
        private static Dictionary<CrystalSystem, StructureTagProfile> s_CrystalProfiles;
        #endregion

        #region Public API
        /// <summary>
        /// ArchitecturalType からデフォルトタグプロファイルを取得
        /// </summary>
        public static StructureTagProfile GetDefaultProfile(ArchitecturalType _type)
        {
            EnsureArchitecturalProfiles();
            return s_ArchitecturalProfiles.TryGetValue(_type, out var profile)
                ? profile
                : new StructureTagProfile();
        }

        /// <summary>
        /// CompoundArchitecturalType からデフォルトタグプロファイルを取得
        /// </summary>
        public static StructureTagProfile GetDefaultProfile(CompoundArchitecturalType _type)
        {
            EnsureCompoundProfiles();
            return s_CompoundProfiles.TryGetValue(_type, out var profile)
                ? profile
                : new StructureTagProfile();
        }

        /// <summary>
        /// PrimitiveType からデフォルトタグプロファイルを取得
        /// </summary>
        public static StructureTagProfile GetDefaultProfile(PrimitiveType _type)
        {
            EnsurePrimitiveProfiles();
            return s_PrimitiveProfiles.TryGetValue(_type, out var profile)
                ? profile
                : new StructureTagProfile();
        }

        /// <summary>
        /// CrystalSystem からデフォルトタグプロファイルを取得
        /// </summary>
        public static StructureTagProfile GetDefaultProfile(CrystalSystem _system)
        {
            EnsureCrystalProfiles();
            return s_CrystalProfiles.TryGetValue(_system, out var profile)
                ? profile
                : new StructureTagProfile();
        }

        /// <summary>
        /// 全組み込みタグ名の一覧を取得
        /// </summary>
        public static IReadOnlyList<string> GetBuiltInTagNames()
        {
            return new[]
            {
                TAG_ARCH, TAG_TOWER, TAG_WALL, TAG_DOME, TAG_COLUMN,
                TAG_BRIDGE, TAG_ENCLOSURE, TAG_SPIRE, TAG_STEPPED, TAG_CRYSTAL,
                TAG_MASSIVE, TAG_ORNATE, TAG_WEATHERED, TAG_FORTIFIED, TAG_SACRED,
                TAG_FUNCTIONAL, TAG_ELEGANT, TAG_PRIMITIVE, TAG_ORGANIC, TAG_GEOMETRIC
            };
        }
        #endregion

        #region Profile Initialization
        private static void EnsureArchitecturalProfiles()
        {
            if (s_ArchitecturalProfiles != null) return;
            s_ArchitecturalProfiles = new Dictionary<ArchitecturalType, StructureTagProfile>
            {
                [ArchitecturalType.SimpleArch] = new StructureTagProfile(
                    new TagEntry(TAG_ARCH, 0.9f),
                    new TagEntry(TAG_FUNCTIONAL, 0.6f)),

                [ArchitecturalType.RomanArch] = new StructureTagProfile(
                    new TagEntry(TAG_ARCH, 0.9f),
                    new TagEntry(TAG_MASSIVE, 0.5f),
                    new TagEntry(TAG_ORNATE, 0.6f),
                    new TagEntry(TAG_FUNCTIONAL, 0.5f)),

                [ArchitecturalType.GothicArch] = new StructureTagProfile(
                    new TagEntry(TAG_ARCH, 0.9f),
                    new TagEntry(TAG_MASSIVE, 0.5f),
                    new TagEntry(TAG_ORNATE, 0.8f),
                    new TagEntry(TAG_SACRED, 0.6f)),

                [ArchitecturalType.Bridge] = new StructureTagProfile(
                    new TagEntry(TAG_ARCH, 0.7f),
                    new TagEntry(TAG_BRIDGE, 0.9f),
                    new TagEntry(TAG_MASSIVE, 0.6f),
                    new TagEntry(TAG_FUNCTIONAL, 0.8f)),

                [ArchitecturalType.Aqueduct] = new StructureTagProfile(
                    new TagEntry(TAG_ARCH, 0.8f),
                    new TagEntry(TAG_BRIDGE, 0.8f),
                    new TagEntry(TAG_MASSIVE, 0.7f),
                    new TagEntry(TAG_FUNCTIONAL, 0.9f)),

                [ArchitecturalType.Cathedral] = new StructureTagProfile(
                    new TagEntry(TAG_ARCH, 0.7f),
                    new TagEntry(TAG_TOWER, 0.4f),
                    new TagEntry(TAG_DOME, 0.5f),
                    new TagEntry(TAG_MASSIVE, 0.8f),
                    new TagEntry(TAG_ORNATE, 0.9f),
                    new TagEntry(TAG_SACRED, 0.95f)),

                [ArchitecturalType.Colonnade] = new StructureTagProfile(
                    new TagEntry(TAG_COLUMN, 0.9f),
                    new TagEntry(TAG_ORNATE, 0.6f),
                    new TagEntry(TAG_FUNCTIONAL, 0.5f)),

                [ArchitecturalType.Viaduct] = new StructureTagProfile(
                    new TagEntry(TAG_ARCH, 0.6f),
                    new TagEntry(TAG_BRIDGE, 0.9f),
                    new TagEntry(TAG_MASSIVE, 0.7f),
                    new TagEntry(TAG_FUNCTIONAL, 0.9f))
            };
        }

        private static void EnsureCompoundProfiles()
        {
            if (s_CompoundProfiles != null) return;
            s_CompoundProfiles = new Dictionary<CompoundArchitecturalType, StructureTagProfile>
            {
                [CompoundArchitecturalType.MultipleBridge] = new StructureTagProfile(
                    new TagEntry(TAG_ARCH, 0.7f),
                    new TagEntry(TAG_BRIDGE, 0.9f),
                    new TagEntry(TAG_MASSIVE, 0.7f),
                    new TagEntry(TAG_FUNCTIONAL, 0.8f)),

                [CompoundArchitecturalType.AqueductSystem] = new StructureTagProfile(
                    new TagEntry(TAG_ARCH, 0.8f),
                    new TagEntry(TAG_BRIDGE, 0.8f),
                    new TagEntry(TAG_MASSIVE, 0.8f),
                    new TagEntry(TAG_FUNCTIONAL, 0.9f)),

                [CompoundArchitecturalType.CathedralComplex] = new StructureTagProfile(
                    new TagEntry(TAG_ARCH, 0.7f),
                    new TagEntry(TAG_TOWER, 0.5f),
                    new TagEntry(TAG_DOME, 0.6f),
                    new TagEntry(TAG_ENCLOSURE, 0.4f),
                    new TagEntry(TAG_MASSIVE, 0.9f),
                    new TagEntry(TAG_ORNATE, 0.9f),
                    new TagEntry(TAG_SACRED, 0.95f)),

                [CompoundArchitecturalType.FortressWall] = new StructureTagProfile(
                    new TagEntry(TAG_TOWER, 0.7f),
                    new TagEntry(TAG_WALL, 0.9f),
                    new TagEntry(TAG_ENCLOSURE, 0.6f),
                    new TagEntry(TAG_MASSIVE, 0.9f),
                    new TagEntry(TAG_FORTIFIED, 0.95f),
                    new TagEntry(TAG_FUNCTIONAL, 0.6f)),

                [CompoundArchitecturalType.Amphitheater] = new StructureTagProfile(
                    new TagEntry(TAG_ARCH, 0.5f),
                    new TagEntry(TAG_ENCLOSURE, 0.9f),
                    new TagEntry(TAG_STEPPED, 0.6f),
                    new TagEntry(TAG_MASSIVE, 0.7f),
                    new TagEntry(TAG_ORNATE, 0.5f),
                    new TagEntry(TAG_FUNCTIONAL, 0.6f)),

                [CompoundArchitecturalType.Basilica] = new StructureTagProfile(
                    new TagEntry(TAG_ARCH, 0.6f),
                    new TagEntry(TAG_DOME, 0.4f),
                    new TagEntry(TAG_MASSIVE, 0.7f),
                    new TagEntry(TAG_ORNATE, 0.7f),
                    new TagEntry(TAG_SACRED, 0.8f)),

                [CompoundArchitecturalType.Cloister] = new StructureTagProfile(
                    new TagEntry(TAG_ARCH, 0.5f),
                    new TagEntry(TAG_COLUMN, 0.6f),
                    new TagEntry(TAG_ENCLOSURE, 0.9f),
                    new TagEntry(TAG_MASSIVE, 0.4f),
                    new TagEntry(TAG_ORNATE, 0.6f),
                    new TagEntry(TAG_SACRED, 0.7f)),

                [CompoundArchitecturalType.TriumphalArch] = new StructureTagProfile(
                    new TagEntry(TAG_ARCH, 0.9f),
                    new TagEntry(TAG_MASSIVE, 0.8f),
                    new TagEntry(TAG_ORNATE, 0.9f))
            };
        }

        private static void EnsurePrimitiveProfiles()
        {
            if (s_PrimitiveProfiles != null) return;
            s_PrimitiveProfiles = new Dictionary<PrimitiveType, StructureTagProfile>
            {
                [PrimitiveType.Cube] = new StructureTagProfile(
                    new TagEntry(TAG_MASSIVE, 0.7f),
                    new TagEntry(TAG_GEOMETRIC, 0.9f),
                    new TagEntry(TAG_PRIMITIVE, 0.6f)),

                [PrimitiveType.Sphere] = new StructureTagProfile(
                    new TagEntry(TAG_DOME, 0.7f),
                    new TagEntry(TAG_GEOMETRIC, 0.9f),
                    new TagEntry(TAG_PRIMITIVE, 0.5f)),

                [PrimitiveType.Cylinder] = new StructureTagProfile(
                    new TagEntry(TAG_COLUMN, 0.7f),
                    new TagEntry(TAG_GEOMETRIC, 0.8f),
                    new TagEntry(TAG_PRIMITIVE, 0.5f)),

                [PrimitiveType.Pyramid] = new StructureTagProfile(
                    new TagEntry(TAG_STEPPED, 0.5f),
                    new TagEntry(TAG_MASSIVE, 0.8f),
                    new TagEntry(TAG_GEOMETRIC, 0.9f),
                    new TagEntry(TAG_PRIMITIVE, 0.7f)),

                [PrimitiveType.Torus] = new StructureTagProfile(
                    new TagEntry(TAG_GEOMETRIC, 0.9f),
                    new TagEntry(TAG_ORGANIC, 0.3f),
                    new TagEntry(TAG_PRIMITIVE, 0.5f)),

                [PrimitiveType.Prism] = new StructureTagProfile(
                    new TagEntry(TAG_GEOMETRIC, 0.9f),
                    new TagEntry(TAG_PRIMITIVE, 0.6f)),

                [PrimitiveType.Cone] = new StructureTagProfile(
                    new TagEntry(TAG_SPIRE, 0.5f),
                    new TagEntry(TAG_GEOMETRIC, 0.8f),
                    new TagEntry(TAG_PRIMITIVE, 0.6f)),

                [PrimitiveType.Octahedron] = new StructureTagProfile(
                    new TagEntry(TAG_CRYSTAL, 0.5f),
                    new TagEntry(TAG_GEOMETRIC, 0.95f),
                    new TagEntry(TAG_PRIMITIVE, 0.5f)),

                [PrimitiveType.Crystal] = new StructureTagProfile(
                    new TagEntry(TAG_CRYSTAL, 0.9f),
                    new TagEntry(TAG_SPIRE, 0.5f),
                    new TagEntry(TAG_GEOMETRIC, 0.8f),
                    new TagEntry(TAG_ELEGANT, 0.5f)),

                [PrimitiveType.Monolith] = new StructureTagProfile(
                    new TagEntry(TAG_MASSIVE, 0.95f),
                    new TagEntry(TAG_PRIMITIVE, 0.8f),
                    new TagEntry(TAG_GEOMETRIC, 0.6f)),

                [PrimitiveType.Arch] = new StructureTagProfile(
                    new TagEntry(TAG_ARCH, 0.9f),
                    new TagEntry(TAG_PRIMITIVE, 0.6f),
                    new TagEntry(TAG_FUNCTIONAL, 0.4f)),

                [PrimitiveType.Ring] = new StructureTagProfile(
                    new TagEntry(TAG_ENCLOSURE, 0.5f),
                    new TagEntry(TAG_GEOMETRIC, 0.8f),
                    new TagEntry(TAG_PRIMITIVE, 0.5f)),

                [PrimitiveType.Mesa] = new StructureTagProfile(
                    new TagEntry(TAG_MASSIVE, 0.8f),
                    new TagEntry(TAG_PRIMITIVE, 0.9f),
                    new TagEntry(TAG_STEPPED, 0.3f)),

                [PrimitiveType.Spire] = new StructureTagProfile(
                    new TagEntry(TAG_SPIRE, 0.9f),
                    new TagEntry(TAG_ELEGANT, 0.5f),
                    new TagEntry(TAG_GEOMETRIC, 0.6f)),

                [PrimitiveType.Boulder] = new StructureTagProfile(
                    new TagEntry(TAG_MASSIVE, 0.6f),
                    new TagEntry(TAG_PRIMITIVE, 0.9f),
                    new TagEntry(TAG_ORGANIC, 0.5f)),

                [PrimitiveType.Formation] = new StructureTagProfile(
                    new TagEntry(TAG_MASSIVE, 0.7f),
                    new TagEntry(TAG_PRIMITIVE, 0.8f),
                    new TagEntry(TAG_ORGANIC, 0.6f),
                    new TagEntry(TAG_WEATHERED, 0.5f))
            };
        }

        private static void EnsureCrystalProfiles()
        {
            if (s_CrystalProfiles != null) return;
            s_CrystalProfiles = new Dictionary<CrystalSystem, StructureTagProfile>
            {
                [CrystalSystem.Cubic] = new StructureTagProfile(
                    new TagEntry(TAG_CRYSTAL, 0.9f),
                    new TagEntry(TAG_GEOMETRIC, 0.95f)),

                [CrystalSystem.Hexagonal] = new StructureTagProfile(
                    new TagEntry(TAG_CRYSTAL, 0.9f),
                    new TagEntry(TAG_GEOMETRIC, 0.9f),
                    new TagEntry(TAG_ELEGANT, 0.4f)),

                [CrystalSystem.Tetragonal] = new StructureTagProfile(
                    new TagEntry(TAG_CRYSTAL, 0.9f),
                    new TagEntry(TAG_GEOMETRIC, 0.9f),
                    new TagEntry(TAG_SPIRE, 0.3f)),

                [CrystalSystem.Orthorhombic] = new StructureTagProfile(
                    new TagEntry(TAG_CRYSTAL, 0.9f),
                    new TagEntry(TAG_GEOMETRIC, 0.85f)),

                [CrystalSystem.Monoclinic] = new StructureTagProfile(
                    new TagEntry(TAG_CRYSTAL, 0.9f),
                    new TagEntry(TAG_GEOMETRIC, 0.7f),
                    new TagEntry(TAG_ORGANIC, 0.3f)),

                [CrystalSystem.Triclinic] = new StructureTagProfile(
                    new TagEntry(TAG_CRYSTAL, 0.9f),
                    new TagEntry(TAG_GEOMETRIC, 0.6f),
                    new TagEntry(TAG_ORGANIC, 0.4f))
            };
        }
        #endregion
    }
}
