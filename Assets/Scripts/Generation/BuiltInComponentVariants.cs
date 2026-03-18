namespace Vastcore.Generation
{
    /// <summary>
    /// 組み込みの構成要素バリエーション定義。
    /// ComponentSelector に初期バリエーションを登録する。
    /// </summary>
    public static class BuiltInComponentVariants
    {
        /// <summary>
        /// 全組み込みバリエーションを登録したComponentSelectorを返す
        /// </summary>
        public static ComponentSelector CreateDefaultSelector()
        {
            var selector = new ComponentSelector();
            RegisterWindows(selector);
            RegisterDoors(selector);
            RegisterColumns(selector);
            RegisterRoofs(selector);
            RegisterWalls(selector);
            RegisterOrnaments(selector);
            return selector;
        }

        #region Window Variants
        private static void RegisterWindows(ComponentSelector _selector)
        {
            _selector.Register(new ComponentVariant("GothicWindow",
                ComponentCategory.Aperture, ComponentType.Window,
                new StructureTagProfile(
                    new TagEntry(StructureTagAdapter.TAG_ORNATE, 0.8f),
                    new TagEntry(StructureTagAdapter.TAG_SACRED, 0.7f),
                    new TagEntry(StructureTagAdapter.TAG_SPIRE, 0.5f))));

            _selector.Register(new ComponentVariant("RoundWindow",
                ComponentCategory.Aperture, ComponentType.Window,
                new StructureTagProfile(
                    new TagEntry(StructureTagAdapter.TAG_DOME, 0.6f),
                    new TagEntry(StructureTagAdapter.TAG_ORNATE, 0.5f),
                    new TagEntry(StructureTagAdapter.TAG_ELEGANT, 0.6f))));

            _selector.Register(new ComponentVariant("SlitWindow",
                ComponentCategory.Aperture, ComponentType.Window,
                new StructureTagProfile(
                    new TagEntry(StructureTagAdapter.TAG_FORTIFIED, 0.9f),
                    new TagEntry(StructureTagAdapter.TAG_WALL, 0.7f),
                    new TagEntry(StructureTagAdapter.TAG_FUNCTIONAL, 0.5f))));

            _selector.Register(new ComponentVariant("LatticeWindow",
                ComponentCategory.Aperture, ComponentType.Window,
                new StructureTagProfile(
                    new TagEntry(StructureTagAdapter.TAG_ORNATE, 0.6f),
                    new TagEntry(StructureTagAdapter.TAG_ELEGANT, 0.7f),
                    new TagEntry(StructureTagAdapter.TAG_ORGANIC, 0.4f))));

            _selector.Register(new ComponentVariant("PlainWindow",
                ComponentCategory.Aperture, ComponentType.Window,
                new StructureTagProfile(
                    new TagEntry(StructureTagAdapter.TAG_FUNCTIONAL, 0.8f),
                    new TagEntry(StructureTagAdapter.TAG_PRIMITIVE, 0.5f))));
        }
        #endregion

        #region Door Variants
        private static void RegisterDoors(ComponentSelector _selector)
        {
            _selector.Register(new ComponentVariant("ArchedDoor",
                ComponentCategory.Aperture, ComponentType.Door,
                new StructureTagProfile(
                    new TagEntry(StructureTagAdapter.TAG_ARCH, 0.8f),
                    new TagEntry(StructureTagAdapter.TAG_ORNATE, 0.5f),
                    new TagEntry(StructureTagAdapter.TAG_SACRED, 0.4f))));

            _selector.Register(new ComponentVariant("FortifiedGate",
                ComponentCategory.Aperture, ComponentType.Door,
                new StructureTagProfile(
                    new TagEntry(StructureTagAdapter.TAG_FORTIFIED, 0.9f),
                    new TagEntry(StructureTagAdapter.TAG_MASSIVE, 0.7f),
                    new TagEntry(StructureTagAdapter.TAG_WALL, 0.5f))));

            _selector.Register(new ComponentVariant("PlainDoor",
                ComponentCategory.Aperture, ComponentType.Door,
                new StructureTagProfile(
                    new TagEntry(StructureTagAdapter.TAG_FUNCTIONAL, 0.8f),
                    new TagEntry(StructureTagAdapter.TAG_PRIMITIVE, 0.5f))));
        }
        #endregion

        #region Column Variants
        private static void RegisterColumns(ComponentSelector _selector)
        {
            _selector.Register(new ComponentVariant("DoricColumn",
                ComponentCategory.Ornament, ComponentType.Column,
                new StructureTagProfile(
                    new TagEntry(StructureTagAdapter.TAG_COLUMN, 0.9f),
                    new TagEntry(StructureTagAdapter.TAG_MASSIVE, 0.6f),
                    new TagEntry(StructureTagAdapter.TAG_FUNCTIONAL, 0.5f))));

            _selector.Register(new ComponentVariant("IonicColumn",
                ComponentCategory.Ornament, ComponentType.Column,
                new StructureTagProfile(
                    new TagEntry(StructureTagAdapter.TAG_COLUMN, 0.9f),
                    new TagEntry(StructureTagAdapter.TAG_ORNATE, 0.6f),
                    new TagEntry(StructureTagAdapter.TAG_ELEGANT, 0.5f))));

            _selector.Register(new ComponentVariant("CorinthianColumn",
                ComponentCategory.Ornament, ComponentType.Column,
                new StructureTagProfile(
                    new TagEntry(StructureTagAdapter.TAG_COLUMN, 0.9f),
                    new TagEntry(StructureTagAdapter.TAG_ORNATE, 0.9f),
                    new TagEntry(StructureTagAdapter.TAG_ELEGANT, 0.7f))));

            _selector.Register(new ComponentVariant("PlainPillar",
                ComponentCategory.Ornament, ComponentType.Column,
                new StructureTagProfile(
                    new TagEntry(StructureTagAdapter.TAG_COLUMN, 0.7f),
                    new TagEntry(StructureTagAdapter.TAG_PRIMITIVE, 0.6f),
                    new TagEntry(StructureTagAdapter.TAG_FUNCTIONAL, 0.5f))));
        }
        #endregion

        #region Roof Variants
        private static void RegisterRoofs(ComponentSelector _selector)
        {
            _selector.Register(new ComponentVariant("DomeRoof",
                ComponentCategory.Shell, ComponentType.Roof,
                new StructureTagProfile(
                    new TagEntry(StructureTagAdapter.TAG_DOME, 0.9f),
                    new TagEntry(StructureTagAdapter.TAG_SACRED, 0.5f),
                    new TagEntry(StructureTagAdapter.TAG_MASSIVE, 0.5f))));

            _selector.Register(new ComponentVariant("SpireRoof",
                ComponentCategory.Shell, ComponentType.Roof,
                new StructureTagProfile(
                    new TagEntry(StructureTagAdapter.TAG_SPIRE, 0.9f),
                    new TagEntry(StructureTagAdapter.TAG_SACRED, 0.6f),
                    new TagEntry(StructureTagAdapter.TAG_ELEGANT, 0.5f))));

            _selector.Register(new ComponentVariant("FlatRoof",
                ComponentCategory.Shell, ComponentType.Roof,
                new StructureTagProfile(
                    new TagEntry(StructureTagAdapter.TAG_FUNCTIONAL, 0.8f),
                    new TagEntry(StructureTagAdapter.TAG_FORTIFIED, 0.4f))));

            _selector.Register(new ComponentVariant("BattlementRoof",
                ComponentCategory.Shell, ComponentType.Roof,
                new StructureTagProfile(
                    new TagEntry(StructureTagAdapter.TAG_FORTIFIED, 0.9f),
                    new TagEntry(StructureTagAdapter.TAG_WALL, 0.6f),
                    new TagEntry(StructureTagAdapter.TAG_MASSIVE, 0.5f))));
        }
        #endregion

        #region Wall Variants
        private static void RegisterWalls(ComponentSelector _selector)
        {
            _selector.Register(new ComponentVariant("StoneWall",
                ComponentCategory.Shell, ComponentType.Wall,
                new StructureTagProfile(
                    new TagEntry(StructureTagAdapter.TAG_MASSIVE, 0.6f),
                    new TagEntry(StructureTagAdapter.TAG_FORTIFIED, 0.5f),
                    new TagEntry(StructureTagAdapter.TAG_PRIMITIVE, 0.4f))));

            _selector.Register(new ComponentVariant("OrnateWall",
                ComponentCategory.Shell, ComponentType.Wall,
                new StructureTagProfile(
                    new TagEntry(StructureTagAdapter.TAG_ORNATE, 0.8f),
                    new TagEntry(StructureTagAdapter.TAG_ELEGANT, 0.5f),
                    new TagEntry(StructureTagAdapter.TAG_SACRED, 0.3f))));

            _selector.Register(new ComponentVariant("RuinedWall",
                ComponentCategory.Shell, ComponentType.Wall,
                new StructureTagProfile(
                    new TagEntry(StructureTagAdapter.TAG_WEATHERED, 0.9f),
                    new TagEntry(StructureTagAdapter.TAG_PRIMITIVE, 0.6f))));
        }
        #endregion

        #region Ornament Variants
        private static void RegisterOrnaments(ComponentSelector _selector)
        {
            _selector.Register(new ComponentVariant("FlyingButtress",
                ComponentCategory.Ornament, ComponentType.Buttress,
                new StructureTagProfile(
                    new TagEntry(StructureTagAdapter.TAG_ARCH, 0.7f),
                    new TagEntry(StructureTagAdapter.TAG_SACRED, 0.6f),
                    new TagEntry(StructureTagAdapter.TAG_MASSIVE, 0.5f))));

            _selector.Register(new ComponentVariant("StandardButtress",
                ComponentCategory.Ornament, ComponentType.Buttress,
                new StructureTagProfile(
                    new TagEntry(StructureTagAdapter.TAG_WALL, 0.6f),
                    new TagEntry(StructureTagAdapter.TAG_FORTIFIED, 0.5f),
                    new TagEntry(StructureTagAdapter.TAG_FUNCTIONAL, 0.4f))));

            _selector.Register(new ComponentVariant("GothicPinnacle",
                ComponentCategory.Ornament, ComponentType.Pinnacle,
                new StructureTagProfile(
                    new TagEntry(StructureTagAdapter.TAG_SPIRE, 0.8f),
                    new TagEntry(StructureTagAdapter.TAG_ORNATE, 0.7f),
                    new TagEntry(StructureTagAdapter.TAG_SACRED, 0.5f))));

            _selector.Register(new ComponentVariant("Merlon",
                ComponentCategory.Ornament, ComponentType.Battlement,
                new StructureTagProfile(
                    new TagEntry(StructureTagAdapter.TAG_FORTIFIED, 0.9f),
                    new TagEntry(StructureTagAdapter.TAG_WALL, 0.7f))));
        }
        #endregion
    }
}
