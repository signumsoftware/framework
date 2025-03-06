
using Microsoft.SqlServer.Types;
using Signum.Engine.Maps;

namespace Signum.Test;

public class SortableHierarchyTest
{
    [Fact]
    public void SqlHierarchyIdExtension()
    {
        var list = new List<SqlHierarchyId>
        {
            SqlHierarchyId.Null,
            SqlHierarchyId.GetRoot(),
            SqlHierarchyId.Parse("/-12312312/"),
            SqlHierarchyId.Parse("/-23423.324324.234234/"),
            SqlHierarchyId.Parse("/-1/"),
            SqlHierarchyId.Parse("/-1.-234/"),
            SqlHierarchyId.Parse("/0/"),
            SqlHierarchyId.Parse("/0.0.0/"),
            SqlHierarchyId.Parse("/0.112312/"),
            SqlHierarchyId.Parse("/0.112312123/"),
            SqlHierarchyId.Parse("/463456345/"),
        };


        var dic = list.ToDictionary(a => a, a => a.ToSortableString());

        var inOrder = list.Order().ToArray();

        foreach (var original in inOrder)
        {
            string? encoded = original.ToSortableString();
            var reborn = HierarchyIdString.FromSortableString(encoded);

            Assert.Equal(reborn, original);
        }

        var inOrderEncoded = inOrder.Select(a => a.ToSortableString()).Order().ToList();

        var inOrderReborn = inOrderEncoded.Select(a => HierarchyIdString.FromSortableString(a)).ToList();

        Assert.True(inOrder.SequenceEqual(inOrderReborn));
    }

}
