using Azure.Core;
using DocumentFormat.OpenXml.Office2016.Drawing;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Signum.Entities.Basics;
using Signum.Entities.Reflection;
using Signum.Utilities.Reflection;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using Column = Signum.Entities.DynamicQuery.Column;
using Filter = Signum.Entities.DynamicQuery.Filter;

namespace Signum.Engine.Excel;


public class ImportResult
{
    public string RowIdentifier;
    public Lite<Entity>? Entity;
    public string? Error;

    public ImportResult(string rowIdentifier)
    {
        RowIdentifier = rowIdentifier;
    }

}

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
internal class TokenGettersAndSetters
{
    public Func<Entity, ModifiableEntity>? ParentGetter { get; set; }
    public Action<ModifiableEntity, object?> Setter { get; set; }
}
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.


public class ImporterFromExcel
{
    public static async IAsyncEnumerable<ImportResult> ImportExcel(QueryRequest request, FileContent file, OperationSymbol saveOperation)
    {
        var (mainType, columns, simpleFilters) = ParseQueryRequest(request);

        var columnGetterSetter = GetColumnGettersAndSetters(columns);
        var filtersGetterSetter = GetColumnGettersAndSetters(simpleFilters.Keys.ToList());

        using (var ms = new MemoryStream(file.Bytes))
        using (var document = SpreadsheetDocument.Open(ms, false))
        {
            WorkbookPart workbookPart = document.WorkbookPart!;

            WorksheetPart worksheetPart = document.GetWorksheetPartById("rId1");

            var data = worksheetPart.Worksheet.Descendants<SheetData>().Single();

            foreach (var row in data.Descendants<Row>().Skip(1))
            {
                var entity = (Entity)Activator.CreateInstance(mainType)!;

                foreach (var kvp in simpleFilters)
                {
                    var getSet = filtersGetterSetter.GetOrThrow(kvp.Key);

                    var parent = getSet.ParentGetter != null ? getSet.ParentGetter(entity) : entity;

                    getSet.Setter(parent, kvp.Value);
                }

                var cells = row.Descendants<Cell>().ToList();

                for (int i = 0; i < columns.Count; i++)
                {
                    var token = columns[i];

                    var getSet = columnGetterSetter.GetOrThrow(token);

                    var strValue = document.GetCellValue(cells[i]);

                    if (!ReflectionTools.TryParse(strValue, token!.Type, out var value))
                        throw new ApplicationException($"Unable to convert '{strValue}' to {token.Type.TypeName()}. Cell Reference = {cells[i].CellReference}");

                    var parent = getSet.ParentGetter != null ? getSet.ParentGetter(entity) : entity;

                    getSet.Setter(parent, value);
                }

                OperationLogic.ServiceExecute(entity, saveOperation);

                yield return new ImportResult(row.RowIndex!.ToString()!)
                {
                    Entity = entity.ToLite(),
                };

                await Task.Yield();
            }
        }
    }

    private static Dictionary<QueryToken, TokenGettersAndSetters> GetColumnGettersAndSetters(List<QueryToken> columns)
    {
        var columnParentGetter = columns.Select(c =>
        {
            if (c is HasValueToken)
            {
                var pr = c.Parent!.GetPropertyRoute()!;

                if (pr.Parent!.PropertyRouteType != PropertyRouteType.Root)
                    return pr.Parent!.GetLambdaExpression<Entity, ModifiableEntity>(false);

                return null;
            }
            else
            {
                var pr = c.GetPropertyRoute()!;

                if (pr.Parent!.PropertyRouteType != PropertyRouteType.Root)
                    return pr.Parent!.GetLambdaExpression<Entity, ModifiableEntity>(false);

                return null;
            }
        }).ToList();

        var columnSetter = columns.Select(c =>
        {
            if (c is HasValueToken)
            {
                var pr = c.Parent!.GetPropertyRoute()!;
                var prop = pr.PropertyInfo!;

                var p = Expression.Parameter(typeof(ModifiableEntity));
                var obj = Expression.Parameter(typeof(object));

                var value = Expression.Condition(Expression.Convert(obj, typeof(bool)),
                    Expression.New(prop.PropertyType!), Expression.Constant(null, prop.PropertyType));

                var lambda = Expression.Lambda<Action<ModifiableEntity, object?>>(Expression.Assign(Expression.Convert(p, prop.DeclaringType!), value));

                return lambda;
            }
            else
            {
                var pr = c.GetPropertyRoute()!;
                var prop = pr.PropertyInfo!;

                var p = Expression.Parameter(typeof(ModifiableEntity));
                var obj = Expression.Parameter(typeof(object));

                var value = Expression.Convert(obj, prop.PropertyType);

                var lambda = Expression.Lambda<Action<ModifiableEntity, object?>>(Expression.Assign(Expression.Convert(p, prop.DeclaringType!), value));

                return lambda;
            }
        }).ToList();

        var columnGetterSetter = columns.Select((c, i) => KeyValuePair.Create(c,  new TokenGettersAndSetters
        {
            ParentGetter = columnParentGetter[i]?.Compile(),
            Setter = columnSetter[i].Compile(),
        })).ToList();

        return columnGetterSetter.ToDictionaryEx();
    }

    public static (Type mainType, List<QueryToken> columns, Dictionary<QueryToken, object?> simpleFilters) ParseQueryRequest(QueryRequest request)
    {
        var qd = QueryLogic.Queries.QueryDescription(request.QueryName);

        Type entityType = GetEntityType(qd);

        var simpleFilters = GetSimpleFilters(request.Filters, qd, entityType);

        var columns = GetSimpleColumns(request.Columns, qd, entityType);

        var repeatedColumns = columns.Where(token => simpleFilters.ContainsKey(token)).ToList();

        if (repeatedColumns.Any())
            throw new ApplicationException($"Column(s) {repeatedColumns.CommaAnd()} have constant values from filters");

        return (entityType, columns, simpleFilters);

    }

    public static Type GetEntityType(QueryDescription qd)
    {
        var implementations = qd.Columns.Single(a => a.IsEntity).Implementations;

        if (implementations == null || implementations.Value.IsByAll || implementations.Value.Types.Only() == null)
            throw new ApplicationException($"Implementations of Entity column should be a simple entity (instead of {implementations})");

        var mainType = implementations.Value.Types.SingleEx();
        return mainType;
    }

    static List<QueryToken> GetSimpleColumns(List<Column> columns, QueryDescription qd, Type mainType)
    {
        var errors = columns.Select(c => IsSimpleProperty(c.Token)).NotNull();

        if (errors.Any())
            throw new ApplicationException(@"Some Columns are incompatible for Importing from Excel.
" + errors.ToString("\n"));

        var pairs = columns.GroupBy(c => Normalize(c.Token, qd, mainType)).Select(gr => new { gr.Key, Error = gr.Count() == 1 ? null : $"Column '{gr.Key}' is repeated {gr.Count()} times" }).ToList();

        errors = pairs.Select(a => a.Error).NotNull();

        if (errors != null)
            throw new ApplicationException(errors.ToString("\n"));

        return pairs.Select(a => a.Key).ToList();
    }

    static Dictionary<QueryToken, object?> GetSimpleFilters(List<Filter> filters, QueryDescription qd, Type mainType)
    {
        var errors = filters.Select(f =>
        f is FilterGroup fg ? $"{FilterGroupOperation.And.NiceToString()}/{FilterGroupOperation.Or.NiceToString()} is not supported:" + fg.ToString() :
        f is FilterCondition fc ?
        (fc.Operation != FilterOperation.EqualTo ? $"Operation {fc.Operation.NiceToString()} is not supported:" + fc.ToString() :
        IsSimpleProperty(fc.Token)) :
        "Unexpected filter " + f.GetType().TypeName()
        ).NotNull().ToList();

        if (errors.Any())
            throw new ApplicationException(@"Some Filters are incompatible for Importing from Excel.
Simple 'Property = Value' filters can be used to assign constant values, anything else is not allowed:
" + errors.ToString("\r\n"));


        return filters.Cast<FilterCondition>().AgGroupToDictionary(a => Normalize(a.Token, qd, mainType), gr =>
        {
            var values = gr.Select(a => a.Value).Distinct();
            if (values.Count() > 1)
                throw new ApplicationException($"Many filters try to assign the same property '{gr.Key}' with different values ({values.ToString(", ")})");

            return values.Only();
        });
    }

    static QueryToken Normalize(QueryToken token, QueryDescription qd, Type mainType)
    {
        if (token is ColumnToken ct)
        {
            var pr = ct.GetPropertyRoute()!;

            if (pr.RootType == mainType)
                return QueryUtils.Parse("Entity." + ct.GetPropertyRoute()!.PropertyInfo!.Name, qd, 0);
        }

        return token;
    }

    static readonly PropertyInfo piId = ReflectionTools.GetPropertyInfo((Entity e) => e.Id);


    static string? IsSimpleProperty(QueryToken token)
    {
        var filterType = QueryUtils.TryGetFilterType(token.Type);
        if (filterType == null)
            return $"{token.NiceTypeName} is not supported";

        if (filterType == FilterType.Embedded)
            return $"{token.Type.TypeName()} ({token.NiceTypeName}) can not be assigned directly. Each nested field should be assigned independently."
                + (token.GetPropertyRoute()?.PropertyInfo?.IsNullable() == true ? $" {token}.[HasValue] can also be used" : null);

        var incompatible = token.Follow(t => t.Parent).Reverse()
        .Select(t =>
            t is ColumnToken c && c.GetPropertyRoute() is { } pr && pr.RootType == token.Type ? null :
            t is EntityPropertyToken ep ? null :
            t is CollectionElementToken ce ? null :
            $"{t} ({t.GetType().Name} is incompatible"
        ).NotNull().First();

        if (incompatible != null)
            return incompatible;

        var pi =
            token is ColumnToken c ? c.GetPropertyRoute()?.PropertyInfo :
            token is EntityPropertyToken ept ? ept.PropertyInfo :
            null;

        if (pi == null)
            return null;

        if (ReflectionTools.PropertyEquals(pi, piId))
            return null;

        if (pi.IsReadOnly())
            return $"{pi.NiceName()} is read-only";

        return null;
    }

}
