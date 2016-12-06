## LINQ `Join` differences

We tried to make Linq to Signum as easy for the user as possible. One cool feature of Linq in general is that it provides a unified model for querying in memory objects and the database (sometimes, the hardest part is to know where you are).

On LINQ Joins, however, this idea of having an unified model has gone too far, making it hard to use. We had to follow a different approach. 

Let's see how Linq to Sql/Entity Framework does joins first: 

#### OUTER JOIN in LINQ to Objects / Linq to SQL

In Linq there are two kind of joins, Join and GroupJoin:


```C#
//Given two sequences (outer & inner) mix them by a common key and using resultSelector combines each possible pair. 
public static IQueryable<TResult> Join<TOuter, TInner, TKey, TResult>(
    this IQueryable<TOuter> outer, 
         IEnumerable<TInner> inner, 
         Expression<Func<TOuter, TKey>> outerKeySelector, 
         Expression<Func<TInner, TKey>> innerKeySelector, 
         Expression<Func<TOuter, TInner, TResult>> resultSelector)

//Given two sequences (outer and inner) mix them by a common key and using resultSelector combines each element in outer with all the elements with the same key on inner.
public static IQueryable<TResult> GroupJoin<TOuter, TInner, TKey, TResult>(
    this IQueryable<TOuter> outer, 
         IEnumerable<TInner> inner,
         Expression<Func<TOuter, TKey>> outerKeySelector, 
         Expression<Func<TInner, TKey>> innerKeySelector, 
         Expression<Func<TOuter, IEnumerable<TInner>, TResult>> resultSelector)
```

As you see, there's no explicit way to do `OUTER JOIN` with [Join operator](http://www.hookedonlinq.com/JoinOperator.ashx). 

However, `GroupJoin` returns all the elements on outer collection, even if the group of the elements of the same key in inner is empty. By combining `GroupJoin` + `SelectMany` + `DefaultIfEmpty` you can achieve `LEFT OUTER JOIN` behaviour.

```C#
using(BugContext db = new BugContext())
{
	var q = from b in db.Bugs
	        join c in db.Comments on b.Id equals c.idBug into g //GrouJoin
	        from c in g.DefaultIfEmpty() //SelectMany + DefaultIfEmpty
	        select new { b.Description, OrderNumber = c == null ? "(no comment)" : c.Text };
}
```

In our opinion, this method signatures were designed with Linq to Objects in mind. They promote hierarchical sequences (`GroupJoin`) and try to centralize the nasty `DefaultIfEmpty` method. When used in Linq to Sql is has some disadvantages though: 

* It takes 3 operators to make a `LEFT OUTER JOIN`. In database, outer joins are so common that this is just not acceptable.
* This approach doesn't work for `RIGHT OUTER JOIN`, neither for `FULL OUTER JOIN`.
* The `GroupJoin` translation is affected by how g is actually used. What if it is used twice, with and without the `DefaultIfEmpty` operator?. 

At the end, we found the Linq to Sql join strategy is over-complicated. They try too hard to keep the 'they are just objects' abstraction but instead of making it simpler (like with dot joins) they make it much more complex. That's why we are following a different path here.


#### OUTER JOIN in LINQ to Signum

Our approach is closer to Sql. On a `Join` (or `GroupJoin`) operator you can mark any of the input collections (inner and/or outer) with `DefaultIfEmpty`. The side marked with `DefaultIfEmpty` will be allowed to have null values when no counterpart is found on the other side. You can use `DefaultIfEmpty` on left side, right side, or both!!.

The example above, using Linq to Signum, will just be: 


```C#
var q = from b in Database.Query<BugEntity>()
        join c in Database.Query<CommentEntity>().DefaultIfEmpty() on b equals c.Bug
        select new { b.Description, OrderNumber = c == null ? "(no comment)" : c.Text };
```

### Types of JOIN

In Linq queries, explicit joins are used when joining with something different than foreign keys (otherwise implicit joins are usually more convenient).

In this super-simple database we have to make up an artificial example. Let's for example join pairs of consecutive bugs by `Start` and `End` date.   

#### INNER JOIN

Returns only the matched pair, with `b1` being the previous bug and `b2` the next bug: 

```C#
from b1 in Database.Query<BugEntity>()
join b2 in Database.Query<BugEntity>() on b.Start equals c.Date 
select new { b1, b2 }
````

#### LEFT OUTER JOIN

Returns all the bugs in `b1`, with `null` or the next bug in `b2`:

```C#
from b1 in Database.Query<BugEntity>()
join b2 in Database.Query<BugEntity>().DefaultIfEmpty() on b.Start equals c.Date 
select new { b1, b2 }
````

#### RIGHT OUTER JOIN

Returns all the bugs in `b2`, with `null` or the previous bug in `b1`:

```C#
from b1 in Database.Query<BugEntity>().DefaultIfEmpty() 
join b2 in Database.Query<BugEntity>() on b.Start equals c.Date 
select new { b1, b2 }
````

#### FULL OUTER JOIN

Returns all the bugs in `b1` and `b2`, with matching pairs if exists or `null` if not. 

```C#
from b1 in Database.Query<BugEntity>().DefaultIfEmpty() 
join b2 in Database.Query<BugEntity>().DefaultIfEmpty() on b.Start equals c.Date 
select new { b1, b2 }
````