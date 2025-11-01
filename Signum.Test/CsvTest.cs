using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Signum.Test;


public class CsvTest
{
    [Fact]
    public void SimpleCsvUntyped()
    {
        var str = """
            Id,Name
            10,John
            11,Anna
            11,
            ,Peter
            """;

        var result = Csv.ReadUntypedBytes(Encoding.UTF8.GetBytes(str), culture: CultureInfo.InvariantCulture);

        Assert.Equal(str, result.ToString(a => a.ToString(","), "\n"));
    }

#pragma warning disable CS0649 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
    class Person
    {
        public int? Id;
        public string? Name;
    }

#pragma warning restore CS0649 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
    [Fact]
    public void SimpleCsv()
    {
        var str = """
            Id,Name
            10,John
            11,Anna
            12,
            ,Peter
            """;

        var result = Csv.ReadBytes<Person>(Encoding.UTF8.GetBytes(str), culture: CultureInfo.InvariantCulture);

        Assert.Equal(4, result.Count);
        Assert.Equal("10|11|12|", result.ToString(a => a.Id.ToString(), "|"));
        Assert.Equal("John|Anna||Peter", result.ToString(a => a.Name, "|"));
    }
}
