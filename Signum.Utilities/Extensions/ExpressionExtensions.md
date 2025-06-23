# ExpressionExtensions

### CompileAndStore

Compiles and stores the result function in a cache. 

> **Important Note:** The cache uses simple reference comparison, so this method **should only be used for constant compile-generated expressions stored in a static field** otherwise the cache will eat all your memory!. In order to avoid this problem, the method can not be called with two different instances of identical expressions. 

```C#
   public static T CompileAndStore<T>(this Expression<T> expression)
```

### Evaluate

Compiles and stores the result function in a cache, then evaluates the result function. It uses `CompileAndStore` for the first two steps, so the same restrictions apply.


```C#
public static T Evaluate<T>(this Expression<Func<T>> expr)
public static T Evaluate<A0, T>(this Expression<Func<A0, T>> expr, A0 a0)
public static T Evaluate<A0, A1, T>(this Expression<Func<A0, A1, T>> expr, A0 a0, A1 a1)
public static T Evaluate<A0, A1, A2, T>(this Expression<Func<A0, A1, A2, T>> expr, A0 a0, A1 a1, A2 a2)
public static T Evaluate<A0, A1, A2, A3, T>(this Expression<Func<A0, A1, A2, A3, T>> expr, A0 a0, A1 a1, A2 a2, A3 a3)
```

```C#
static Expression<Func<Entity, IQueryable<NoteEntity>>> NotesExpression =
    ident => Database.Query<NoteEntity>().Where(n => n.Target.Is(ident));
[ExpressionField]
public static IQueryable<NoteEntity> Notes(this Entity ident)
{
    return NotesExpression.Evaluate(ident);
}
```

This method can also be used in database queries to apply expressions, in this case the cache won't be used so is not an issue. 

```C#

Expression<Func<int, int>> dup = n => n  * 2; 

Database.Query<PersonEntity>()
    .Where(p=>dup.Evaluate(p.Id) == 2 || dup.Evaluate(p.Id) == 4)
    .First();
```





