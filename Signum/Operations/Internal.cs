namespace Signum.Operations;

public interface IConstructOperation : IOperation
{
    IEntity Construct(params object?[]? parameters);
}

public interface IConstructorFromOperation : IEntityOperation
{
    IEntity Construct(IEntity entity, params object?[]? parameters);
    bool ResultIsSaved { get; }
}

public interface IConstructorFromManyOperation : IOperation
{
    IEntity Construct(IEnumerable<Lite<IEntity>> lites, params object?[]? parameters);
    Type BaseType { get; }
}

public interface IExecuteOperation : IEntityOperation
{
    bool ForReadonlyEntity { get; set; }

    void Execute(IEntity entity, params object?[]? parameters);
}

public interface IDeleteOperation : IEntityOperation
{
    void Delete(IEntity entity, params object?[]? parameters);
}

//The only point of this clases is to avoid 'member names cannot be the same as their enclosing type' compilation message
public class _Construct<T>
    where T : class, IEntity
{
    public Func<object?[]?, T?> Construct { get; set; } = null!;
}

public class _Execute<T>
    where T : class, IEntity
{
    public Action<T, object?[]?> Execute { get; set; } = null!;
}


public class _Delete<T>
  where T : class, IEntity
{
    public Action<T, object?[]?> Delete { get; set; } = null!;
}
