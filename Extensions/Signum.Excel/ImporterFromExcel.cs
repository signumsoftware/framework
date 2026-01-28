using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;
using Signum.Authorization.Rules;
using Signum.DynamicQuery.Tokens;
using Signum.Engine.Sync;
using Signum.Utilities.Reflection;
using System.Collections;
using System.IO;
using Column = Signum.DynamicQuery.Column;
using Filter = Signum.DynamicQuery.Filter;

namespace Signum.Excel;


public class ImportResult
{
    public int TotalRows; 
    public ImportAction Action;
    public int RowIndex;
    public Lite<Entity>? Entity;
    public string? Error;
}

public enum ImportAction
{
    Inserted,
    Updated,
    NoChanges,
}



public class ImporterFromExcel
{
    static List<IGrouping<object?, Row>> GroupByConsecutive(IEnumerable<Row> rows, QueryToken? matchBy, int? matchByIndex, SpreadsheetDocument document)
    {
        var rowGroups = rows.GroupWhenChange(row =>
        {
            if (matchByIndex == null)
                return row.RowIndex!.ToString()!;

            var cell = row.Descendants<Cell>().SingleOrDefaultEx(a => (a.GetExcelColumnIndex()!.Value - 1) == matchByIndex);

            var valueStr =  cell == null ? null : document.GetCellValue(cell);

            return ParseExcelValue(matchBy!, valueStr, row, matchByIndex.Value);
        }).ToList();

        var duplicateKeys = rowGroups.GroupBy(r => r.Key).Where(a => a.Count() > 1).ToList();

        if (duplicateKeys.Any())
            throw new ApplicationException(ImportFromExcelMessage.DuplicatedNonConsecutive0Found1.NiceToString(matchBy,
                duplicateKeys.ToString(g => g.Key + " in rows " + g.CommaAnd(a => a.First().RowIndex), "\n")));

        return rowGroups;
    }

    public static async IAsyncEnumerable<ImportResult> ImportExcel(QueryRequest request, ImportExcelModel model, OperationSymbol saveOperation)
    {
        var transactionalResults = new List<ImportResult>();

        var file = model.ExcelFile.ToFileContent();

        var pq = ParseQueryRequest(request);

        var elementTokens = (pq.ElementTopToken?.Follow(a => a.Parent)).EmptyIfNull().OfType<CollectionElementToken>().ToList();

        var mlistGetters = elementTokens.ToDictionary(a => a, a => GetMListGetter(a));

        var columnGetterSetter = GetColumnGettersAndSetters(pq.Columns/*.Where(a => !a.HasElement()).ToList()*/, pq.MainType);
        var filtersGetterSetter = GetColumnGettersAndSetters(pq.SimpleFilters.Keys.ToList(), pq.MainType);

        var qd = QueryLogic.Queries.QueryDescription(request.QueryName);
        var matchBy = !model.MatchByColumn.HasText() ? null : QueryUtils.Parse(model.MatchByColumn!, qd, 0);
        var matchByIndex = matchBy == null ? (int?)null : pq.Columns.IndexOf(Normalize(matchBy, qd, pq.MainType));

        var keyByElementToken = model.Collections.ToDictionary(
            col => elementTokens.SingleEx(a => a.FullKey() == col.CollectionElement),
            col =>
            {
                if (!col.MatchByColumn.HasText())
                    return null;

                var index = pq.Columns.FindIndex(a => a.FullKey() == col.MatchByColumn);
                var keyToken = pq.Columns[index];
                var elementToken = elementTokens.SingleEx(a => a.FullKey() == col.CollectionElement);
                var keyGetter = GetMListElementKeyGetter(keyToken, elementToken);
                return new { MatchByIndex = index, MatchBy = keyToken, Getter = keyGetter };
            });

        var table = Signum.Engine.Maps.Schema.Current.Table(pq.MainType);

        var columnTree = TreeHelper.ToTreeC(pq.Columns, a => a.Follow(a => a.Parent).Skip(1).FirstOrDefault(a => a is CollectionElementToken));

        using (var tr = model.Transactional ? new Transaction() : null)
        using (model.IdentityInsert && model.Transactional ? Administrator.DisableIdentity(table) : null)
        using (var ms = new MemoryStream(file.Bytes))
        using (var document = SpreadsheetDocument.Open(ms, false))
        {
            WorkbookPart workbookPart = document.WorkbookPart!;

            WorksheetPart worksheetPart = document.GetWorksheetPartBySheetName("Sheet1");

            var data = worksheetPart.Worksheet!.Descendants<SheetData>().Single();


            var headerRow = data.Descendants<Row>().ElementAt(1);
            var excelColumns = headerRow.Descendants<Cell>().ToList().Select((c, i) => document.GetCellValue(c)).TakeWhile(a => a.HasText()).ToString(", ");
            var queryColumns = request.Columns.ToString(a => a.DisplayName, ", ");

            if (excelColumns != queryColumns)
                throw new ApplicationException(ImportFromExcelMessage.ColumnsDoNotMatchExcelColumns0QueryColumns1.NiceToString(excelColumns, queryColumns));


            var allRows = data.Descendants<Row>().Skip(2).TakeWhile(a => a.Descendants<Cell>().Any(c => document.GetCellValue(c).HasText())).ToList();

            bool hasErros = false;

            var rowGroups = GroupByConsecutive(allRows, matchBy, matchByIndex, document);

               foreach (var rg in rowGroups)
            {
                ImportResult res = new ImportResult
                {
                    RowIndex = (int)rg.First().RowIndex!.Value,
                    TotalRows = rowGroups.Count,
                };

                try
                {

                    Entity entity;
                    if (matchBy != null)
                    {
                        if (rg.Key != null)
                        {

                            entity = QueryLogic.Queries.GetEntitiesFull(new QueryEntitiesRequest
                            {
                                QueryName = request.QueryName,
                                Filters = request.Filters.And(new FilterCondition(matchBy, FilterOperation.EqualTo, rg.Key)).ToList(),
                                Orders = new List<Order>(),
                                Count = null,
                            }).SingleOrDefaultEx()!;
                        }
                        else
                        {
                            entity = null!;
                        }

                        if (entity != null)
                        {
                            res.Action = ImportAction.Updated;
                            res.Entity = entity.ToLite();
                        }
                        else
                        {
                            if (model.Mode == ImportExcelMode.InsertOrUpdate || model.Mode == ImportExcelMode.Insert)
                            {
                                entity = (Entity)Activator.CreateInstance(pq.MainType)!;
                                res.Action = ImportAction.Inserted;
                            }
                            else
                            {
                                res.Action = ImportAction.Updated;
                                res.Error = ImportFromExcelMessage.No0FoundInThisQueryWith1EqualsTo2.NiceToString(pq.MainType.NiceName(), matchBy, rg.Key ?? "null");
                                goto exit;
                            }
                        }
                    }
                    else
                    {
                        entity = (Entity)Activator.CreateInstance(pq.MainType)!;
                        res.Action = ImportAction.Inserted;
                    }

                    if (res.Action == ImportAction.Inserted)
                        foreach (var kvp in pq.SimpleFilters)
                        {
                            var getSet = filtersGetterSetter.GetOrThrow(kvp.Key);

                            var parent = getSet.ParentGetter != null ? getSet.ParentGetter(entity) : entity;

                            if (!getSet.IsId)
                            {
                                var value = getSet.EntityFinder != null ? getSet.EntityFinder(kvp.Value) : kvp.Value; 

                                getSet.Setter!(parent, value);
                            }
                        }

                    //Simple properties
                    {
                        var firstRow = rg.FirstEx();

                        var cells = firstRow.Descendants<Cell>().ToDictionary(a => a.GetExcelColumnIndex()!.Value - 1);
                        foreach (var node in columnTree.Where(a => a.Value is not CollectionElementToken))
                        {
                            var token = node.Value;
                            var colIndex = pq.Columns.IndexOf(token);

                            var getSet = columnGetterSetter.GetOrThrow(token);

                            var cell = cells.TryGetC(colIndex);
                            var strValue = cell == null ? null : document.GetCellValue(cell);

                            if (getSet.IsId)
                            {
                                var id = strValue.HasText() ? PrimaryKey.Parse(strValue, pq.MainType) : (PrimaryKey?)null;

                                if (id != null)
                                {
                                    if (entity.IdOrNull == null)
                                    {
                                        if (!model.IdentityInsert)
                                            throw new InvalidOperationException($"Unable to set ID because IdentityInsert is not true. Cell Reference = {CellReference(firstRow, colIndex)}");

                                        entity.SetId(id);
                                    }
                                    else
                                    {
                                        if (!entity.IdOrNull.Equals(id))
                                            throw new InvalidOperationException($"Id does not match. Cell Reference = {CellReference(firstRow, colIndex)}");
                                    }
                                }
                            }
                            else
                            {
                                object? value = ParseExcelValue(token, strValue, firstRow, colIndex);

                                if (pq.SimpleFilters.TryGetValue(token, out var filterValue))
                                {
                                    if(!object.Equals(value, filterValue))
                                        throw new InvalidOperationException($"Value of column {token} ({value ?? "null"}) does not match the filter value ({filterValue ?? "null"}). Cell Reference = {CellReference(firstRow, colIndex)}");
                                }

                                value = getSet.EntityFinder != null ? getSet.EntityFinder(value) : value;
                                var parent = getSet.ParentGetter != null ? getSet.ParentGetter(entity) : entity;
                            
                                if (parent == null)
                                {
                                    if (value != null)
                                        throw new InvalidOperationException($"Unable to assign value {value} (from token {token}) because the parent is null");
                                }
                                else
                                {
                                    if (getSet.Required && value == null)
                                        throw new InvalidOperationException($"Value of column {token} is null");

                                    getSet.Setter!(parent, value);
                                }
                            }
                        }
                    }

                    object? ApplyChanges(object? previousValue, Node<QueryToken> node, CollectionElementToken token, Type cleanType, Row row)
                    {
                        var cells = row.Descendants<Cell>().ToDictionary(a => a.GetExcelColumnIndex()!.Value - 1);
                        var embeddedOrPart = cleanType.IsEmbeddedEntity() || cleanType.IsEntity() && EntityKindCache.GetEntityKind(cleanType) is EntityKind.Part or EntityKind.SharedPart;
                        if (embeddedOrPart)
                        {
                            var me = (ModifiableEntity)(previousValue ?? Activator.CreateInstance(cleanType))!;
                            foreach (var c in node.Children.Where(a => a.Value is not CollectionElementToken))
                            {
                                var colIndex = pq.Columns.IndexOf(c.Value);
                                var cell = cells.TryGetC(colIndex);
                                var strValue = cell == null ? null : document.GetCellValue(cell);

                                object? value = ParseExcelValue(c.Value, strValue, row, colIndex);

                                var getSet = columnGetterSetter.GetOrThrow(c.Value);

                                value = getSet.EntityFinder != null ? getSet.EntityFinder(value) : value;
                                var parent = getSet.ParentGetter != null ? getSet.ParentGetter(me) : me;
                                getSet.Setter!(parent, value);
                            }
                            return me;
                        }
                        else
                        {
                            var colIndex = pq.Columns.IndexOf(token);
                            var cell = cells.TryGetC(colIndex);
                            var strValue = cell == null ? null : document.GetCellValue(cell);
                            object? value = ParseExcelValue(token, strValue, row, colIndex);
                            var getSet = columnGetterSetter.GetOrThrow(token);
                            value = getSet.EntityFinder != null ? getSet.EntityFinder(value) : value;
                            return value;
                        }
                    }

                    foreach (var node in columnTree.Where(a => a.Value is CollectionElementToken))
                    {
                        var token = (CollectionElementToken)node.Value!;
                        var mlist = (IList)mlistGetters.GetOrThrow(token)(entity);
                        var key = keyByElementToken.GetOrThrow(token);
                        var cleanType = token.Type.CleanType();
                        if (key == null) //Last MList in an Insert Mode
                        {
                            if (mlist.Count != 0)
                                throw new InvalidOperationException("MList should be empty");

                            foreach (var row in rg)
                            {
                                var elem = ApplyChanges(null, node, token, cleanType, row);
                                mlist.Add(elem);
                            }
                        }
                        else
                        {
                            var shouldGroups = GroupByConsecutive(rg, key.MatchBy, key.MatchByIndex, document);
                            var should = shouldGroups.Count == 1 && shouldGroups.SingleEx().Key == null ? new() : shouldGroups.ToDictionary(a => a.Key!);
                            var current = mlist.Cast<object>().ToDictionary(key.Getter);

                            Synchronizer.Synchronize(
                                  newDictionary: should,
                                  oldDictionary: current,
                                  createNew: (k, n) =>
                                  {
                                      var elem = ApplyChanges(null, node, token, cleanType, n.FirstEx());
                                      mlist.Add(elem);
                                  },
                                  removeOld: (k, o) =>
                                  {
                                      mlist.Remove(o);
                                  },
                                  merge: (k, n, o) =>
                                  {
                                      ApplyChanges(o, node, token, cleanType, n.FirstEx());
                                  });
                        }
                    }
                    

                    var oldTicks = entity.Ticks;

                    if (!model.Transactional && model.IdentityInsert && entity.IsNew)
                    {
                        using (var tr2 = new Transaction())
                        using (Administrator.DisableIdentity(table))
                        {
                            OperationLogic.ServiceExecute(entity, saveOperation);

                            tr2.Commit();
                        }
                    }
                    else
                    {
                        OperationLogic.ServiceExecute(entity, saveOperation);
                    }

                    if (oldTicks == entity.Ticks && res.Action == ImportAction.Updated)
                        res.Action = ImportAction.NoChanges;

                    res.Entity = entity.ToLite();
                }
                catch (Exception e)
                {
                    e.LogException();
                    hasErros = true;
                    res.Error = e.Message;
                }
            exit:
                if (model.Transactional)
                    transactionalResults.Add(res);
                else
                    yield return res;

                await Task.Yield();
            }

            if (tr != null && !hasErros)
                tr.Commit();
        }

        if (!model.Transactional)
        {
            foreach (var res in transactionalResults)
            {
                yield return res;
            }
        }
    }

   

    private static object? ParseExcelValue(QueryToken token, string? strValue, Row row, int colIndex)
    {
        try
        {
            var ut = token.Type.UnNullify();

            object? value = !strValue.HasText() ? null :
                ut switch
                {
                    var t when t.IsLite() => ParseOrFindByText(strValue, token),
                    var t when t.IsEntity() => ParseOrFindByText(strValue, token).Retrieve(),
                    var t when t.IsEnum => EnumExtensions.TryParse(strValue, ut, true, out var result) ? result : null,
                    var t when t == typeof(decimal) => RoundToValidator(ExcelExtensions.FromExcelNumber(strValue), token),
                    var t when ExcelExtensions.IsNumber(t) => Convert.ChangeType(ExcelExtensions.FromExcelNumber(strValue), ut),
                    var t when ExcelExtensions.IsDate(t) => ReflectionTools.ChangeType(ExcelExtensions.FromExcelDate(strValue, token.DateTimeKind), ut),
                    var t when t == typeof(TimeOnly) => ExcelExtensions.FromExcelTime(strValue),
                    var t when t == typeof(bool) => strValue == "TRUE" ? true : strValue == "FALSE" ? false : ExcelExtensions.FromExcelNumber(strValue) == 1,
                    _ => ReflectionTools.TryParse(strValue, token.Type, out value) ? value :
                       throw new ApplicationException($"Unable to convert '{strValue}' to {token.Type.TypeName()}. Cell Reference = {CellReference(row, colIndex)}")
                };
            return value;
        }
        catch (Exception e)
        {
            throw new Exception($"Error converting '{strValue}' to {token.Type.TypeName()} in cell {CellReference(row, colIndex)}:\n" + e.Message, e);
        }
    }

    private static Lite<Entity> ParseOrFindByText(string strValue, QueryToken token)
    {
        if (Lite.TryParseLite(strValue, out var result) == null)
            return result!;

        return giFindByText.GetInvoker(token.Type.CleanType())(strValue);
    }

    static GenericInvoker<Func<string, Lite<Entity>>> giFindByText =
        new GenericInvoker<Func<string, Lite<Entity>>>(str => FindByText<ExceptionEntity>(str));
    private static Lite<T> FindByText<T>(string strValue)
        where T : Entity
    {
        return Database.Query<T>().Where(a => a.ToString().Trim() == strValue.Trim()).Select(a => a.ToLite()).SingleEx();
    }

    private static decimal RoundToValidator(decimal val, QueryToken token)
    {
        var pr = token.GetPropertyRoute();

        var decimalValidator = pr == null ? null : Validator.TryGetPropertyValidator(pr)?.Validators.OfType<DecimalsValidatorAttribute>().SingleOrDefaultEx();

        return decimalValidator == null ? val : val.RoundTo(decimalValidator.DecimalPlaces);
    }

    private static string CellReference(Row row, int colIndex)
    {
        return ExcelExtensions.GetExcelColumnName((uint)colIndex + 1) + row.RowIndex;
    }

    static Func<ModifiableEntity, IMListPrivate> GetMListGetter(CollectionElementToken token)
    {
        var pr = token.Parent!.GetPropertyRoute()!; //No other case since MList can not neast without going through Root entities

        return pr.GetLambdaExpression<ModifiableEntity, IMListPrivate>(false).Compile();
    }

    static bool IsNavigatingLite(QueryToken t)
    {
        if (t.Parent is EntityPropertyToken ept)
            return ept.PropertyInfo.PropertyType.IsLite();

        if (t.Parent is CollectionElementToken ce && ce.Parent is EntityPropertyToken eptc)
            return eptc.PropertyInfo.PropertyType.ElementType()!.IsLite();

        return false;
    }

    static Func<object, object> GetMListElementKeyGetter(QueryToken keyToken, QueryToken elementToken)
    {
        var extraTokens = keyToken.Follow(a => a.Parent).TakeWhile(t => !t.Equals(elementToken)).Reverse().ToList();

        var localExtraTokens = extraTokens.TakeWhile(t => !IsNavigatingLite(t)).ToList();

        if (localExtraTokens.Count == extraTokens.Count)
        {
            var pr = keyToken.GetPropertyRoute()!;
            return pr.GetLambdaExpression<object, object>(false, pr.GetMListItemsRoute()).Compile();
        }
        else
        {
            var lastLocal = localExtraTokens.Last();
            var pr = lastLocal.GetPropertyRoute()!;
            var func = pr.GetLambdaExpression<object, object>(false, pr.GetMListItemsRoute()).Compile();

            var type = pr.Type.CleanType();

            var qd = QueryLogic.Queries.QueryDescription(type);
            var entityToken = QueryUtils.Parse("Entity", qd, 0);
            var columnToken = QueryUtils.Parse("Entity" + keyToken.FullKey().After(lastLocal.FullKey()), qd, 0);

            return elem =>
            {
                var lite = func(elem);
                if (lite == null)
                    throw new ApplicationException($"{pr} returned null");

                var result =  InDBToken((Lite<Entity>)lite, entityToken, columnToken);
                if (result == null)
                    throw new ApplicationException($"{columnToken} returned null");

                return result;
            };
        }
    }

    static object? InDBToken(Lite<Entity> lite, QueryToken entityToken, QueryToken columnToken)
    {
        var rt = QueryLogic.Queries.ExecuteQuery(new QueryRequest
        {
            QueryName = lite.EntityType,
            Filters = new List<Filter>
            {
                new FilterCondition(entityToken, FilterOperation.EqualTo, lite),
            },
            Columns = new List<Column>
            {
                new Column(columnToken, null),
            },
            Orders = new List<Order>(),
            Pagination = new Pagination.Firsts(1),
        });

        return rt.Rows[0][0];
    }

    static bool IsMainTypeOrPart(QueryToken token, Type mainType)
    {
        var pr = token.GetPropertyRoute();

        if (pr == null)
            return false;

        if (pr.RootType == mainType)
            return true;

        if (EntityKindCache.GetAttribute(pr.RootType).EntityKind != EntityKind.Part)
            return false;

        return token.Parent != null && IsMainTypeOrPart(token.Parent, mainType);
    }

    private static Dictionary<QueryToken, TokenGettersAndSetters> GetColumnGettersAndSetters(List<QueryToken> columns, Type mainType)
    {
        return columns.ToDictionary<QueryToken, QueryToken, TokenGettersAndSetters>(c => c, c =>
        {
            if (c is HasValueToken)
            {
                var pr = c.Parent!.GetPropertyRoute()!;

                if (!IsMainTypeOrPart(c.Parent, mainType))
                    throw new InvalidOperationException("Invalid token " + c);

                var parentGetter = pr.Parent!.PropertyRouteType != PropertyRouteType.Root ?
                    pr.Parent!.GetLambdaExpression<ModifiableEntity, ModifiableEntity>(false, pr.Parent.GetMListItemsRoute()) : null;

                var pi = pr.PropertyInfo!;

                if (!pi.PropertyType.IsEmbeddedEntity())
                    throw new InvalidOperationException("HasValue only supported for embedded entities");

                var p = Expression.Parameter(typeof(ModifiableEntity));
                var obj = Expression.Parameter(typeof(object));

                var prop = Expression.Property(Expression.Convert(p, pi.DeclaringType!), pi);

                var value = Expression.Condition(Expression.Convert(obj, typeof(bool)),
                     Expression.Coalesce(prop, Expression.New(pi.PropertyType!)), //Prevent unnecessary new 
                     Expression.Constant(null, pi.PropertyType));


                var lambda = Expression.Lambda<Action<ModifiableEntity, object?>>(Expression.Assign(prop, value), p, obj);

                return new TokenGettersAndSetters
                {
                    ParentGetter = parentGetter?.Compile(),
                    Setter = lambda.Compile()
                };
            }
            else
            {
                var pr = c.GetPropertyRoute()!;

                Func<object?, object?>? entityFinder = null;
                if (!IsMainTypeOrPart(c, mainType))
                {
                    var parents = c.Follow(a => a.Parent).Reverse().ToList();

                    var index = parents.FindIndex(t => !IsMainTypeOrPart(t, mainType));

                    pr = parents[index - 1].GetPropertyRoute()!;

                    var first = parents[index] is AsTypeToken ? parents[index] : parents[index - 1];

                    var queryName = first.Type.CleanType();
                    var qd = QueryLogic.Queries.QueryDescription(queryName);
                    var tokenFullKey = "Entity." + c.FullKey().After(first.FullKey() + ".");
                    var token = QueryUtils.Parse(tokenFullKey, qd,  0);

                    entityFinder = a =>
                    {
                        if (a == null)
                            return null;

                        var lite = QueryLogic.Queries.ExecuteUniqueEntity(new UniqueEntityRequest
                        {
                            QueryName = queryName,
                            UniqueType = UniqueType.SingleOrDefault,
                            Orders = new List<Order>(),
                            Filters = new List<Filter>
                            {
                                new FilterCondition(token, FilterOperation.EqualTo, a)
                            }
                        });

                        if (lite == null)
                            throw new InvalidOperationException($"No {queryName} found with {token} equals to '{a}'");

                        if (pr.Type.IsLite())
                            return lite!;

                        if (pr.Type.IsEntity())
                            return lite!.Retrieve();

                        throw new UnexpectedValueException(pr.Type);
                    };
                }
                else if(pr.Type.IsEntity())
                {
                    entityFinder = v =>
                    {
                        if (v == null)
                            return null;

                        return ((Lite<Entity>)v).Retrieve();
                    };
                }

                if (pr.PropertyRouteType == PropertyRouteType.MListItems)
                    return new TokenGettersAndSetters
                    {
                        ParentGetter = null,
                        Setter = null,
                        EntityFinder = entityFinder,
                    };

                var parentGetter = pr.Parent!.PropertyRouteType != PropertyRouteType.Root ?
                    pr.Parent!.GetLambdaExpression<ModifiableEntity, ModifiableEntity>(false, pr.Parent.GetMListItemsRoute()) : null;

                var prop = pr.PropertyInfo!;

                if (ReflectionTools.PropertyEquals(prop, piId))
                    return new TokenGettersAndSetters
                    {
                        IsId = true,
                        ParentGetter = parentGetter?.Compile(),
                        Setter = null
                    };

                var p = Expression.Parameter(typeof(ModifiableEntity));
                var obj = Expression.Parameter(typeof(object));

                var value = Expression.Convert(obj, prop.PropertyType);

                var body = (Expression)Expression.Assign(Expression.Property(Expression.Convert(p, prop.DeclaringType!), prop), value);

                var lambda = Expression.Lambda<Action<ModifiableEntity, object?>>(body, p, obj);

                return new TokenGettersAndSetters 
                {
                    ParentGetter = parentGetter?.Compile(),
                    Setter = lambda.Compile(),
                    EntityFinder = entityFinder,
                    Required = prop.PropertyType.IsValueType && !prop.PropertyType.IsNullable(),
                };
            }
        });

    }

    internal class TokenGettersAndSetters
    {
        public bool IsId { get; set; }
        public required Func<ModifiableEntity, ModifiableEntity>? ParentGetter { get; set; }
        public required Action<ModifiableEntity, object?>? Setter { get; set; }
        public Func<object?, object?>? EntityFinder { get; set; }
        public bool Required { get; set; }
    }

    public class ParsedQueryForImport
    {
        public required Type MainType;
        public required List<QueryToken> Columns;
        public required Dictionary<QueryToken, object?> SimpleFilters;
        public QueryToken? ElementTopToken;
    }

    public static ParsedQueryForImport ParseQueryRequest(QueryRequest request)
    {
        var qd = QueryLogic.Queries.QueryDescription(request.QueryName);

        Type entityType = GetEntityType(qd);

        var simpleFilters = GetSimpleFilters(request.Filters, qd, entityType);

        var columns = GetSimpleColumns(request.Columns, qd, entityType);

        //var repeatedColumns = columns.Where(token => simpleFilters.ContainsKey(token)).ToList();

        //if (repeatedColumns.Any())
        //    throw new ApplicationException(ImportFromExcelMessage.Columns0AlreadyHaveConstanValuesFromFilters.NiceToString(repeatedColumns.CommaAnd()));

        var authErrors = simpleFilters.Keys.Concat(columns).Distinct().Select(a =>
        {
            var pr = a is HasValueToken ? a.Parent!.GetPropertyRoute() : a.GetPropertyRoute();

            return PropertyAuthLogic.CanBeAllowedFor(pr!, PropertyAllowed.Write);
        }).NotNull().ToList();

        if (authErrors.Any())
            throw new ApplicationException(authErrors.ToString("\n"));

        var result = new ParsedQueryForImport { MainType = entityType, Columns = columns, SimpleFilters = simpleFilters };

        var elements = columns.SelectMany(a => a.Follow(a => a.Parent)).OfType<CollectionElementToken>().Distinct();
        if (!elements.Any())
            return result;


        var elemeX = elements.Select(a => a.CollectionElementType).Distinct().Where(a => a != CollectionElementType.Element).ToList();
        if (elemeX.Any())
            throw new ApplicationException(ImportFromExcelMessage._0IsNotSupported.NiceToString(elemeX.CommaOr()));

        var top = elements.SingleOrDefaultEx(e => elements.All(e2 => e2.FullKey().StartsWith(e.FullKey())));

        if (top == null)
            throw new ApplicationException(ImportFromExcelMessage.UnableToAssignMoreThanOneUnrelatedCollections0.NiceToString(elements.CommaAnd()));

        result.ElementTopToken = top;

        return result;
    }

    public static Type GetEntityType(QueryDescription qd)
    {
        var implementations = qd.Columns.Single(a => a.IsEntity).Implementations;

        if (implementations == null || implementations.Value.IsByAll || implementations.Value.Types.Only() == null)
            throw new ApplicationException(ImportFromExcelMessage.ThisQueryHasMultipleImplementations0.NiceToString(implementations));

        var mainType = implementations.Value.Types.SingleEx();
        return mainType;
    }

    static List<QueryToken> GetSimpleColumns(List<Column> columns, QueryDescription qd, Type mainType)
    {
        var errors = columns.Select(c => IsSimpleProperty(c.Token, mainType)).NotNull();

        if (errors.Any())
            throw new ApplicationException(ImportFromExcelMessage.SomeColumnsAreIncompatibleWithImportingFromExcel.NiceToString() + "\n" +  errors.ToString("\n"));

        var pairs = columns.GroupBy(c => Normalize(c.Token, qd, mainType)).Select(gr => new { gr.Key, Error = gr.Count() == 1 ? null : $"Column '{gr.Key}' is repeated {gr.Count()} times" }).ToList();

        errors = pairs.Select(a => a.Error).NotNull();

        if (errors.Any())
            throw new ApplicationException(errors.ToString("\n"));

        return pairs.Select(a => a.Key).ToList();
    }

    static Dictionary<QueryToken, object?> GetSimpleFilters(List<Filter> filters, QueryDescription qd, Type mainType)
    {
        return filters.OfType<FilterCondition>()
            .Where(fc => IsSimpleProperty(fc.Token, mainType) == null && !fc.Token.HasElement())
            .AgGroupToDictionary(a => Normalize(a.Token, qd, mainType), gr =>
        {
            var values = gr.Select(a => a.Value).Distinct();
            if (values.Count() > 1)
                throw new ApplicationException(ImportFromExcelMessage.ManyFiltersTryToAssignTheSameProperty0WithDifferentValues1.NiceToString(gr.Key, values.ToString(", ")));

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


    static string? IsSimpleProperty(QueryToken token, Type mainType)
    {
        var filterType = QueryUtils.TryGetFilterType(token.Type);
        if (filterType == null)
            return ImportFromExcelMessage._0IsNotSupported.NiceToString(token.NiceTypeName);

        if (filterType == FilterType.Embedded)
            return " ".Combine(
                ImportFromExcelMessage._01CanNotBeAssignedDirectylEachNestedFieldShouldBeAssignedIndependently.NiceToString(token.Type.TypeName(), token.NiceTypeName),
                token.GetPropertyRoute()?.PropertyInfo?.IsNullable() == true ? ImportFromExcelMessage._01CanAlsoBeUsed.NiceToString(token, QueryTokenMessage.HasValue.NiceToString()) : null);

        var incompatible = token.Follow(t => t.Parent).Reverse()
        .Select(t =>
            t is ColumnToken c && c.GetPropertyRoute() is { } pr && pr.RootType == mainType ? null :
            t is EntityPropertyToken ep ? null :
            t is CollectionElementToken ce ? null : 
            t is AsTypeToken at ? null :
            t is HasValueToken hv && hv.Parent!.Type.IsEmbeddedEntity() ? null :
            ImportFromExcelMessage._01IsIncompatible.NiceToString(t, t.GetType().Name)
        ).NotNull().FirstOrDefault();

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

        //if (pi.IsReadOnly())
        //    return ImportFromExcelMessage._0IsReadOnly.NiceToString(pi.NiceName());

        return null;
    }

}
