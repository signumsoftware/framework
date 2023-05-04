namespace Signum.Upgrade.Upgrades;

class Upgrade_20211130_ChunksAndMinBy : CodeUpgradeBase
{
    public override string Description => "Replace some Signum.Utilities methods (GroupsOf, WithMin, WithMax) with the .Net 6 counterparts (Chunk, MinBy, MaxBy)";

    public override void Execute(UpgradeContext uctx)
    {
        uctx.ForeachCodeFile(@"*.cs", file =>
        {
            file.Replace(".WithMin(", ".MinBy(");
            file.Replace(".WithMax(", ".MaxBy(");
            file.Replace(".GroupsOf(", ".Chunk(");
        });
    }
}
