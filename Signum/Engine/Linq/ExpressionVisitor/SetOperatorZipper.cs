using System.Collections.ObjectModel;

namespace Signum.Engine.Linq;

/// <summary>
/// Walks the (already cleaned) projectors of the two sides of a SQL set operator (UNION, UNION ALL, INTERSECT, EXCEPT) in parallel,
/// unifying their shapes (Entity / ImplementedBy / ImplementedByAll / Lite / Embedded / Mixin / Anonymous / MemberInit...)
/// and declaring, for each leaf, one column with the same name and position on both sides.
/// The returned projector references the columns of the set operator alias, so entities and lites are completed afterwards (like in GroupBy).
/// </summary>
internal class SetOperatorZipper
{
    readonly Alias setAlias;
    readonly string operatorName;
    readonly ColumnGenerator generator = new ColumnGenerator();

    public readonly List<ColumnDeclaration> LeftColumns = new List<ColumnDeclaration>();
    public readonly List<ColumnDeclaration> RightColumns = new List<ColumnDeclaration>();

    SetOperatorZipper(Alias setAlias, string operatorName)
    {
        this.setAlias = setAlias;
        this.operatorName = operatorName;
    }

    public static (Expression projector, List<ColumnDeclaration> leftColumns, List<ColumnDeclaration> rightColumns) Zip(Expression left, Expression right, Alias setAlias, string operatorName)
    {
        var zipper = new SetOperatorZipper(setAlias, operatorName);
        var projector = zipper.Zip(GroupEntityCleaner.Clean(left), GroupEntityCleaner.Clean(right));
        return (projector, zipper.LeftColumns, zipper.RightColumns);
    }

    Expression Zip(Expression l, Expression r)
    {
        AssertNotCollection(l);
        AssertNotCollection(r);

        if (l.IsNull() && r.IsNull())
            return l;

        if (l.IsNull())
            l = NullLike(r);
        else if (r.IsNull())
            r = NullLike(l);

        if (IsConvertOfComposite(l) || IsConvertOfComposite(r)) // (PrimaryKey)a.Id, etc...
        {
            var zipped = Zip(StripConvert(l), StripConvert(r));
            return zipped.Type == l.Type ? zipped : Expression.Convert(zipped, l.Type);
        }

        if (IsEntityLike(l) || IsEntityLike(r))
            return ZipEntityLike(l, r);

        if (l is LiteReferenceExpression || r is LiteReferenceExpression)
            return ZipLite(As<LiteReferenceExpression>(l, r), As<LiteReferenceExpression>(r, l));

        if (l is EmbeddedEntityExpression || r is EmbeddedEntityExpression)
            return ZipEmbedded(As<EmbeddedEntityExpression>(l, r), As<EmbeddedEntityExpression>(r, l));

        if (l is MixinEntityExpression || r is MixinEntityExpression)
            return ZipMixin(As<MixinEntityExpression>(l, r), As<MixinEntityExpression>(r, l));

        if (l is PrimaryKeyExpression || r is PrimaryKeyExpression)
            return new PrimaryKeyExpression(ZipLeaf(ToPrimaryKey(l).Value.Nullify(), ToPrimaryKey(r).Value.Nullify()));

        if (l.Type == typeof(Type))
            return ZipType(l, r);

        if (l is IntervalExpression || r is IntervalExpression)
            return ZipInterval(As<IntervalExpression>(l, r), As<IntervalExpression>(r, l));

        if (l is NewExpression || r is NewExpression)
            return ZipNew(As<NewExpression>(l, r), As<NewExpression>(r, l));

        if (l is MemberInitExpression || r is MemberInitExpression)
            return ZipMemberInit(As<MemberInitExpression>(l, r), As<MemberInitExpression>(r, l));

        return ZipLeaf(l, r);
    }

    static bool IsComposite(Expression e) => e is
        EntityExpression or ImplementedByExpression or ImplementedByAllExpression or
        LiteReferenceExpression or EmbeddedEntityExpression or MixinEntityExpression or
        PrimaryKeyExpression or IntervalExpression or TypeImplementedByAllExpression;

    static bool IsConvertOfComposite(Expression e) =>
        e is UnaryExpression { NodeType: ExpressionType.Convert or ExpressionType.TypeAs } u && (IsComposite(u.Operand) || IsConvertOfComposite(u.Operand));

    static Expression StripConvert(Expression e) =>
        IsConvertOfComposite(e) ? StripConvert(((UnaryExpression)e).Operand) : e;

    #region Entities
    static bool IsEntityLike(Expression e) => e is EntityExpression or ImplementedByExpression or ImplementedByAllExpression;

    Expression ZipEntityLike(Expression l, Expression r)
    {
        var type = l.Type;

        if (l is ImplementedByAllExpression || r is ImplementedByAllExpression)
        {
            var lIba = GroupEntityCleaner.ToIBA(l, type);
            var rIba = GroupEntityCleaner.ToIBA(r, type);

            //ToIBA creates one id for every type in TypeValues, keep only the ones of the original IBA or with a real value
            IEnumerable<Type> RelevantKeys(Expression original, ImplementedByAllExpression iba) =>
                original is ImplementedByAllExpression ? iba.Ids.Keys : iba.Ids.Where(kvp => !kvp.Value.IsNull()).Select(kvp => kvp.Key);

            var keys = RelevantKeys(l, lIba).Union(RelevantKeys(r, rIba)).ToList();

            return ZipIBA(lIba, rIba, keys, type);
        }

        if (l is ImplementedByExpression || r is ImplementedByExpression || l.Type != r.Type)
            return ZipIB(GroupEntityCleaner.ToIB(l, type), GroupEntityCleaner.ToIB(r, type), type);

        return ZipEntity(As<EntityExpression>(l, r), As<EntityExpression>(r, l), type);
    }

    EntityExpression ZipEntity(EntityExpression l, EntityExpression r, Type type)
    {
        var id = ZipLeaf(l.ExternalId.Value.Nullify(), r.ExternalId.Value.Nullify());
        return new EntityExpression(type, new PrimaryKeyExpression(id), null, null, null, null, null, l.AvoidExpandOnRetrieving || r.AvoidExpandOnRetrieving);
    }

    ImplementedByExpression ZipIB(ImplementedByExpression l, ImplementedByExpression r, Type type)
    {
        var keys = l.Implementations.Keys.Union(r.Implementations.Keys).ToList();

        var implementations = keys.ToDictionary(t => t, t => ZipEntity(
            l.Implementations.TryGetC(t) ?? GroupEntityCleaner.NullEntity(t),
            r.Implementations.TryGetC(t) ?? GroupEntityCleaner.NullEntity(t), t));

        return new ImplementedByExpression(type, l.Strategy, implementations);
    }

    ImplementedByAllExpression ZipIBA(ImplementedByAllExpression l, ImplementedByAllExpression r, List<Type> keys, Type type)
    {
        var ids = keys.ToDictionary(t => t, t => ZipLeaf(
            (l.Ids.TryGetC(t) ?? new SqlConstantExpression(null, t.Nullify())).Nullify(),
            (r.Ids.TryGetC(t) ?? new SqlConstantExpression(null, t.Nullify())).Nullify()));

        var typeId = new TypeImplementedByAllExpression(new PrimaryKeyExpression(ZipLeaf(l.TypeId.TypeColumn.Value.Nullify(), r.TypeId.TypeColumn.Value.Nullify())));

        return new ImplementedByAllExpression(type, ids, typeId, null);
    }
    #endregion

    Expression ZipLite(LiteReferenceExpression l, LiteReferenceExpression r)
    {
        var reference = Zip(l.Reference, r.Reference);

        Expression? customModelExpression = null;
        ReadOnlyDictionary<Type, Type>? customModelTypes = null;

        if (l.CustomModelExpression != null || r.CustomModelExpression != null)
        {
            if (l.CustomModelExpression == null || r.CustomModelExpression == null || l.CustomModelExpression.Type != r.CustomModelExpression.Type)
                throw LiteModelMismatch(l, r);

            customModelExpression = Zip(l.CustomModelExpression, r.CustomModelExpression);
        }
        else if (l.CustomModelTypes != null || r.CustomModelTypes != null)
        {
            if (l.CustomModelTypes == null || r.CustomModelTypes == null)
                throw LiteModelMismatch(l, r);

            var dic = new Dictionary<Type, Type>(l.CustomModelTypes);
            foreach (var kvp in r.CustomModelTypes)
            {
                if (dic.TryGetValue(kvp.Key, out var other) && other != kvp.Value)
                    throw LiteModelMismatch(l, r);

                dic[kvp.Key] = kvp.Value;
            }

            customModelTypes = dic.ToReadOnly();
        }

        return new LiteReferenceExpression(Lite.Generate(reference.Type), reference, customModelExpression, customModelTypes,
            lazyModel: l.LazyModel && r.LazyModel,
            eagerEntity: l.EagerEntity || r.EagerEntity);
    }

    Exception LiteModelMismatch(LiteReferenceExpression l, LiteReferenceExpression r)
    {
        return new InvalidOperationException($"The Lite models of both sides of {operatorName} do not match:\n{l}\n{r}");
    }

    EmbeddedEntityExpression ZipEmbedded(EmbeddedEntityExpression l, EmbeddedEntityExpression r)
    {
        if (l.Type != r.Type)
            throw new InvalidOperationException($"Embedded types do not match in {operatorName}: {l.Type.TypeName()} vs {r.Type.TypeName()}");

        var hasValue = ZipLeaf(l.HasValue, r.HasValue);

        var bindings = l.Bindings.Select(b => new FieldBinding(b.FieldInfo, Zip(b.Binding, r.GetBinding(b.FieldInfo)))).ToList();

        var mixins = l.Mixins?.Select(m => ZipMixin(m, r.Mixins!.SingleEx(rm => rm.Type == m.Type))).ToList();

        return new EmbeddedEntityExpression(l.Type, hasValue, bindings, mixins, l.FieldEmbedded, l.ViewTable, null);
    }

    MixinEntityExpression ZipMixin(MixinEntityExpression l, MixinEntityExpression r)
    {
        var bindings = l.Bindings.Select(b => new FieldBinding(b.FieldInfo, Zip(b.Binding, r.GetBinding(b.FieldInfo)))).ToList();

        return new MixinEntityExpression(l.Type, bindings, null, l.FieldMixin, null);
    }

    Expression ZipType(Expression l, Expression r)
    {
        if (l is ConstantExpression lc && r is ConstantExpression rc && Equals(lc.Value, rc.Value))
            return l;

        return new TypeImplementedByAllExpression(new PrimaryKeyExpression(ZipLeaf(TypeId(l).Value.Nullify(), TypeId(r).Value.Nullify())));
    }

    static PrimaryKeyExpression TypeId(Expression typeExpression)
    {
        if (typeExpression is ConstantExpression c)
            return c.Value == null ?
                new PrimaryKeyExpression(Expression.Constant(null, PrimaryKey.Type(typeof(TypeEntity)).Nullify())) :
                new PrimaryKeyExpression(QueryBinder.TypeConstant((Type)c.Value).Nullify());

        return QueryBinder.ExtractTypeId(typeExpression);
    }

    IntervalExpression ZipInterval(IntervalExpression l, IntervalExpression r)
    {
        Expression? ZipPart(Expression? lp, Expression? rp) =>
            lp == null && rp == null ? null :
            lp == null || rp == null ? throw new InvalidOperationException($"Interval representations do not match in {operatorName}") :
            ZipLeaf(lp, rp);

        return new IntervalExpression(l.Type, ZipPart(l.Min, r.Min), ZipPart(l.Max, r.Max), ZipPart(l.PostgresRange, r.PostgresRange));
    }

    NewExpression ZipNew(NewExpression l, NewExpression r)
    {
        if (l.Constructor != r.Constructor)
            throw new InvalidOperationException($"Both sides of {operatorName} should use the same constructor: {l} vs {r}");

        var args = l.Arguments.Zip(r.Arguments, (la, ra) => Zip(la, ra)).ToList();

        return l.Members != null ?
            Expression.New(l.Constructor!, args, l.Members) :
            Expression.New(l.Constructor!, args);
    }

    MemberInitExpression ZipMemberInit(MemberInitExpression l, MemberInitExpression r)
    {
        var newExpr = ZipNew(l.NewExpression, r.NewExpression);

        var lMembers = l.Bindings.Select(b => b.Member).ToHashSet();
        var rMembers = r.Bindings.Select(b => b.Member).ToHashSet();

        if (!lMembers.SetEquals(rMembers) || l.Bindings.Concat(r.Bindings).Any(b => b is not MemberAssignment))
            throw new InvalidOperationException($"Both sides of {operatorName} should assign the same members:\n{l}\n{r}");

        var rDic = r.Bindings.Cast<MemberAssignment>().ToDictionary(b => b.Member);

        var bindings = l.Bindings.Cast<MemberAssignment>()
            .Select(lb => Expression.Bind(lb.Member, Zip(lb.Expression, rDic[lb.Member].Expression)))
            .ToList();

        return Expression.MemberInit(newExpr, bindings);
    }

    Expression ZipLeaf(Expression l, Expression r)
    {
        var type = l.Type;

        if (type.UnNullify().IsEnum)
        {
            var intType = type.IsNullable() ? typeof(int?) : typeof(int);
            return ZipLeaf(l.TryConvert(intType), r.TryConvert(intType)).TryConvert(type);
        }

        if (r.Type != type)
        {
            if (r.Type.UnNullify() != type.UnNullify())
                throw new InvalidOperationException($"Column types do not match in {operatorName}: {type.TypeName()} vs {r.Type.TypeName()}");

            type = type.Nullify();
            l = l.Nullify();
            r = r.Nullify();
        }

        if (l.IsNull() && r.IsNull())
            return l;

        var ln = DbExpressionNominator.FullNominate(l)!;
        var rn = DbExpressionNominator.FullNominate(r)!;

        var suggestedName = (l.RemoveAllConvert(a => true) as ColumnExpression)?.Name ?? (r.RemoveAllConvert(a => true) as ColumnExpression)?.Name;

        var name = suggestedName != null ? generator.GetUniqueColumnName(suggestedName) : generator.GetNextColumnName();
        generator.AddUsedName(name);

        LeftColumns.Add(new ColumnDeclaration(name, ln));
        RightColumns.Add(new ColumnDeclaration(name, rn));

        return new ColumnExpression(type, setAlias, name);
    }

    T As<T>(Expression e, Expression other) where T : Expression
    {
        if (e is T t)
            return t;

        throw new InvalidOperationException($"Both sides of {operatorName} should have the same shape. Unable to combine:\n{e}\nwith\n{other}");
    }

    static PrimaryKeyExpression ToPrimaryKey(Expression e)
    {
        if (e is PrimaryKeyExpression pk)
            return pk;

        throw new UnexpectedValueException(e);
    }

    void AssertNotCollection(Expression e)
    {
        if (e is DbExpression db && db.DbNodeType is
            DbExpressionType.Projection or
            DbExpressionType.ChildProjection or
            DbExpressionType.MList or
            DbExpressionType.MListProjection or
            DbExpressionType.MListElement or
            DbExpressionType.AdditionalField)
            throw new NotSupportedException($"Collections are not supported in the projections of {operatorName}: {e}");
    }

    static Expression NullLike(Expression e)
    {
        switch (e)
        {
            case EntityExpression ee: return GroupEntityCleaner.NullEntity(ee.Type);
            case ImplementedByExpression ib:
                return new ImplementedByExpression(ib.Type, ib.Strategy, ib.Implementations.ToDictionary(kvp => kvp.Key, kvp => GroupEntityCleaner.NullEntity(kvp.Key)));
            case ImplementedByAllExpression iba:
                return GroupEntityCleaner.ToIBA(Expression.Constant(null, iba.Type), iba.Type);
            case LiteReferenceExpression lite:
                return new LiteReferenceExpression(lite.Type, NullLike(lite.Reference),
                    lite.CustomModelExpression == null ? null : NullLike(lite.CustomModelExpression),
                    lite.CustomModelTypes, lite.LazyModel, lite.EagerEntity);
            case EmbeddedEntityExpression emb:
                return new EmbeddedEntityExpression(emb.Type, Expression.Constant(false),
                    emb.Bindings.Select(b => new FieldBinding(b.FieldInfo, NullLike(b.Binding))),
                    emb.Mixins?.Select(m => (MixinEntityExpression)NullLike(m)),
                    emb.FieldEmbedded, emb.ViewTable, null);
            case MixinEntityExpression mixin:
                return new MixinEntityExpression(mixin.Type, mixin.Bindings.Select(b => new FieldBinding(b.FieldInfo, NullLike(b.Binding))), null, mixin.FieldMixin, null);
            case PrimaryKeyExpression pk:
                return new PrimaryKeyExpression(Expression.Constant(null, pk.Value.Type.Nullify()));
            case TypeImplementedByAllExpression:
                return new TypeImplementedByAllExpression(new PrimaryKeyExpression(Expression.Constant(null, PrimaryKey.Type(typeof(TypeEntity)).Nullify())));
            case NewExpression or MemberInitExpression:
                throw new NotSupportedException($"Unable to combine null with {e}");
            default:
                return e.Type.IsValueType && !e.Type.IsNullable() ?
                    Expression.Constant(null, e.Type.Nullify()).UnNullify() :
                    Expression.Constant(null, e.Type);
        }
    }
}
