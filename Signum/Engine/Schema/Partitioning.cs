using Signum.Engine.Maps;
using System.Data;

namespace Signum.Engine.Maps;


public class SqlPartitionFunction
{
    public readonly string Name;
    public readonly AbstractDbType DbType;
    public readonly Type ColumnType;
    public readonly object[] Points;

    public SqlPartitionFunction(string name, IEnumerable<object> points, PartitionRange range = PartitionRange.Right, Type? columnType = null, AbstractDbType? dbType = null)
    { 
        this.Name = name;

        if (columnType == null)
            this.ColumnType = points.Select(a => a.GetType()).Distinct().SingleEx();
        else
        {
            var error = points.Where(a => a.GetType() != columnType).ToList();
            if (error.Any())
                throw new InvalidOperationException("Wrong type " + error.ToString(", "));
            this.ColumnType = columnType;
        }

        this.Points = points.ToArray();
        this.DbType = dbType ?? Connector.Current.Schema.Settings.DefaultSqlType(this.ColumnType);
    }


}

public enum PartitionRange
{
    Left,
    Right,
}

public class SqlPartitionScheme
{
    public SqlPartitionFunction PartitionFunction;
    public string Name;
    public object FileGroupNames; //string or string[]
    
    public SqlPartitionScheme(string name, SqlPartitionFunction function, string fileGroupName = "Primary")
    {
        this.Name = name;
        this.PartitionFunction = function;
        this.FileGroupNames = fileGroupName;
    }

    public SqlPartitionScheme(string name, SqlPartitionFunction function, string[] fileGroupNames)
    {
        this.Name = name;
        this.PartitionFunction = function;
        if (fileGroupNames.Length != function.Points.Length + 1)
        {
            throw new InvalidOperationException($"PartitionFunction has {function.Points.Length} split points so the schema should have {function.Points.Length} file group names (instead of {fileGroupNames.Length})");
        }

        this.FileGroupNames = fileGroupNames;
    }
}
