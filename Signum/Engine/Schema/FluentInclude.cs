namespace Signum.Engine.Maps;

public class FluentInclude<T> where T : Entity
{
    public SchemaBuilder SchemaBuilder { get; private set; }
    public Table Table { get; private set; }

    public FluentInclude(Table table, SchemaBuilder schemaBuilder)
    {
        Table = table;
        SchemaBuilder = schemaBuilder;
    }

    public FluentInclude<T> WithUniqueIndex(Expression<Func<T, object?>> fields, Expression<Func<T, bool>>? where = null, Expression<Func<T, object?>>? includeFields = null, bool onlyOneNull_SqlServerOnly = false)
    {
        this.SchemaBuilder.AddUniqueIndex<T>(fields, where, includeFields, onlyOneNull_SqlServerOnly);
        return this;
    }

    public FluentInclude<T> WithIndex(Expression<Func<T, object?>> fields, 
        Expression<Func<T, bool>>? where = null, 
        Expression<Func<T, object>>? includeFields = null)
    {
        this.SchemaBuilder.AddIndex<T>(fields, where, includeFields);
        return this;
    }

    public FluentInclude<T> WithFullTextIndex(Expression<Func<T, object?>> fields, Action<FullTextTableIndex>? customize = null)
    {
        var result = this.SchemaBuilder.AddFullTextIndex<T>(fields, customize);
        return this;
    }

    public FluentInclude<T> WithVectorIndex(Expression<Func<T, object?>> fields, Action<VectorTableIndex>? customize = null)
    {
        this.SchemaBuilder.AddVectorIndex<T>(fields, customize);
        return this;
    }

    public FluentInclude<T> WithUniqueIndexMList<M>(Expression<Func<T, MList<M>>> mlist, 
        Expression<Func<MListElement<T, M>, object>>? fields = null, 
        Expression<Func<MListElement<T, M>, bool>>? where = null, 
        Expression<Func<MListElement<T, M>, object>>? includeFields = null)
    {
        if (fields == null)
            fields = mle => new { mle.Parent, mle.Element };

        this.SchemaBuilder.AddUniqueIndexMList<T, M>(mlist, fields, where, includeFields);
        return this;
    }

    public FluentInclude<T> WithIndexMList<M>(Expression<Func<T, MList<M>>> mlist, 
        Expression<Func<MListElement<T, M>, object>> fields, 
        Expression<Func<MListElement<T, M>, bool>>? where = null, 
        Expression<Func<MListElement<T, M>, object>>? includeFields = null)
    {
        this.SchemaBuilder.AddIndexMList<T, M>(mlist, fields, where, includeFields);
        return this;
    }

    public FluentInclude<T> WithFullTextIndexMList<M>(Expression<Func<T, MList<M>>> mlist,
        Expression<Func<MListElement<T, M>, object>> fields, Action<FullTextTableIndex>? customize = null)
    {
        var result = this.SchemaBuilder.AddFullTextIndexMList<T, M>(mlist, fields, customize);
        return this;
    }

    public FluentInclude<T> WithVectorIndexMList<M>(Expression<Func<T, MList<M>>> mlist,
        Expression<Func<MListElement<T, M>, object>> fields, Action<VectorTableIndex>? customize = null)
    {
        this.SchemaBuilder.AddVectorIndexMList<T, M>(mlist, fields, customize);
        return this;
    }

    public FluentInclude<T> WithLiteModel<M>(Expression<Func<T, M>> constructorExpression, bool isDefault = true)
    {
        Lite.RegisterLiteModelConstructor(constructorExpression, isDefault);
        return this;
    }
    public FluentInclude<T> WithAdditionalField<M>(Expression<Func<T, M>> property, Func<bool> shouldSet, Expression<Func<T, PrimaryKey?, M>> expression)
    {
        this.SchemaBuilder.Schema.EntityEvents<T>().RegisterBinding(property, shouldSet, ()=> expression);
        return this;
    }

}

