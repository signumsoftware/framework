# SqlPreCommand

Signum Engine is all about generating Sql commands. Usually these commands are big strings and expensive to concatenate. `StringBuilder`, on the other hand, is more efficient but tends to generate code in a very imperative way. 

In order to have a good balance between usability (functional style) and performance, `SqlPreCommand` was created. 

`SqlPreCommand` is an abstract class, there are two implementations: `SqlPreCommandSimple` and `SqlPreCommandConcat`. 

In memory, `SqlPreCommands` is a tree with `SqlPreCommandSimple` on the leaves, and `SqlPreCommandConcat` mixing them. At the very end, when the command is going to be executed, the tree gets flattened once in a efficient way, or the leaves are executed one by one independently. 


### SqlPreCommandSimple 

Contains a piece of sql text, consistent and executable by itself. Also, contains a list with all the related `SqlParameter` objects to execute it (to avoid SQL injection attack)

It has a pair of constructors like this: 

```C#
public SqlPreCommandSimple(string sql)
public SqlPreCommandSimple(string sql, List<DbParameter> parameters)
```

### SqlPreCommandConcat 

Contains an array with all the `SqlPreCommand` (Simple or Concat), as well as a value of the `Spacing` `enum` to determine how many blank lines will be rendered between commands. 

It has no public constructor, instead the static method `Combine` in `SqlPreCommand` should be used instead. It's defined like this: 

```C#
public static SqlPreCommand Combine(Spacing spacing, params SqlPreCommand[] sentences)
{
    if (sentences.Contains(null))
        sentences = sentences.NotNull().ToArray();

    if (sentences.Length == 0)
        return null;

    if (sentences.Length == 1)
        return sentences[0];

    return new SqlPreCommandConcat(spacing, sentences);
}
```
There's also another overloading over any `IEnumerable<SqlPrecommand>` using extension methods:


```C#
public static SqlPreCommand Combine(this IEnumerable<SqlPreCommand> preCommands, Spacing spacing)
{
    return SqlPreCommand.Combine(spacing, preCommands.ToArray());
}
````

### SqlPreCommand.ToSimple

Turns a `SqlPreCommand` into a `SqlPreCommandSimple`. In case of being `SqlPreCommandConcat`, concatenates the sql queries and the parameters. `SqlPreCommandSimple` just returns itself. Use `ToSimple`, not casting. 

### SqlPreCommand.PlainSql

Returns a string with all the `SqlParameter` inlined in the SQL commands. Good for debugging or generating scripts, but not for production code because it's sensitive to SQL Injection attacks. 
