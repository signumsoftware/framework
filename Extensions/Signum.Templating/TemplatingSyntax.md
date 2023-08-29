# Templating

This is the available syntax for writing Word and Email templates. 

The main benefit of Signum Templating infrastructure is that the template defines not only the look of the report, but also the query. 

If you make a mistake you'll typically find it at Upload-template-time. Not when running it. And if the entities of models change the synchronizer will fix the templates. 

## Value Providers

### Query fields

* `@[TotalAmount]`: Access column TotalAmount in the Query (implicit query provider)
* `@[q:TotalAmount]`: Access column TotalAmount in the Query (explicity query provider)
* `@[Customer.Name]`: Access Customer column in the Query and joins with customer table to get the name
* `@[Entity.CreationDate]`: Access CreationDate field in the main Entity that is not shown by default in the query.

### Fields from the Model (requires properties in `SystemWordTemplate` or `SystemEmailTemplate`)

* `@[m:ShortAddress]`: Access column Name in the Query (implicit query provider)

### Global fields (registered with `GlobalValueProvider.RegisterGlobalVariable`)

* `@[g:Now]`: Access keys globaly registered for every template (company address, date time, etc..)

### Translate fields

* `@[t:Product.Name]`: Joins with Product table, gets the name, and the `TranslatedInstanceEntity` name if exists



## Formating

1. Format for numbers and dates is always dependent on the culture of the report.
2. You can change the format using (format strings)[https://msdn.microsoft.com/en-us/library/26etazsy(v=vs.110).aspx]. 
	* `@[TotalAmount:0.00]`: (Custom)[https://msdn.microsoft.com/en-us/library/8kb3ddd4(v=vs.110).aspx] 
	* `@[TotalAmount:C]`: (Standard)[https://msdn.microsoft.com/en-us/library/az4se3k1(v=vs.110).aspx]
3. Enums use translations automatically.


## Conditions and Loops

### If
Allows to conditionally show elements depending on a condition.

```
* @if[IsCancelled]
* Your account is cancelled
* @else
* Your account is accepted
* @endif
```

The `@else` branch is optional and there is no support (yet) for `@elseif`.

### Conditions
* **Values:** In case of an expression from a ValueProvider will be dynamically casted to boolean (`null`, `0` and `""` are considered `false`).
	 * `@if[IsCancelled]`: If IsCancelled is true
	 * `@if[Name]`: If name is not null or empty
	 * `@if[ShippingAddress]`: If Shipping address is not null

* **Comparisons:'' Values can also be compared to Expression Operation ConstantValue
	
Examples: 
* `@if[State=Cancelled]`
* `@if[Name!=John]`

#### List of comparisons

```
switch (operationString)
{
    case "=":
    case "==": return FilterOperation.EqualTo;
    case "<=": return FilterOperation.LessThanOrEqual;
    case ">=": return FilterOperation.GreaterThanOrEqual;
    case "<": return FilterOperation.LessThan;
    case ">": return FilterOperation.GreaterThan;
    case "^=": return FilterOperation.StartsWith;
    case "$=": return FilterOperation.EndsWith;
    case "*=": return FilterOperation.Contains;
    case "%=": return FilterOperation.Like;

    case "!=": return FilterOperation.DistinctTo;
    case "!^=": return FilterOperation.NotStartsWith;
    case "!$=": return FilterOperation.NotEndsWith;
    case "!*=": return FilterOperation.NotContains;
    case "!%=": return FilterOperation.NotLike;
}
```

 #### Comparison Values 

 Values in a comparison are parsed using `FilterValueConverter.TryParse`, this means: 
 * Numbers and Dates should be written in Invariant Culture
 * Strings don't need to be quoted
 * `null` keyword is *not* supported, instead just write nothing. Example: `@if[Name!=]`
 * Collections can be used separating values by `|`, usefull for `IsIn`
 * Enums do not require the type (infered from the expression on the left). Example: `@if[State==Cancelled]` is right but `@if[State==OrderState.Cancelled]` is wrong
 * Custom value providers can be used. `@if[User==[CurrentUser]]`

 #### Complex conditions

 * An `@if` (or `@any`) can contain ANDs and ORs in the conditions, using the operator `AND`, `OR`, `&&` or `||`. Example: `@if[FirstName=John AND LastName=Connor]`


### Foreach
Allows to repeat some block of text for each element

```
* @foreach[Entity.Lines.Element]
* Your @[Entity.Lines.Element.Quantity] @[Entity.Lines.Element.Product.Name] cost @[Entity.Lines.Element.Product.UnitPrice] each, and @[Entity.Lines.Element.SubTotalPrice] in total
* @endforeach
```

You can also avoid repetition by declaring an alias

```
* @foreach[Entity.Lines.Element] as $e
* Your @[$e.Quantity] @[$e.Product.Name] cost @[$e.Product.UnitPrice] each, and @[$e.SubTotalPrice] in total
* @endforeach
```

When using a foreach on a model, or global value provider it works as expected. 
When using a foreach on a query however it does the following: 

1. The query gets joined to all the tables, including the collections (because of the use of `Element`).
2. When rendering a @foreach the rows are grouped by its token, in this case `Entity.Lines.Element`
3. When rendering a normal token, the value has to be unambiguous. Example:


```
* ProductName: @[Entity.Lines.Element.Name]  <--- WRONG Ambiguous
* @foreach[Entity.Lines.Element]
* ProductName: @[Entity.Lines.Element.Name]  <--- RIGHT Unambiguous
* @endforeach
```

This also means that you can make some counter-intuitive nested @foreach

```
* @foreach[Entity.Lines.Element.Product.Category]
    CATEGORY @[Entity.Lines.Element.Product.Category]
    * @foreach[Entity.Lines.Element]
    * ProductName: @[Entity.Lines.Element.Name]
    * @endforeach
* @endforeach
```


### Any

Allows you to do something like

```
* @any[Entity.Lines.Element.Product.IsDiscontinued=true]
* Some products are discontinued
* @notany
* All products available
* @endany
```


### Tree Structure

Conditions and loops use a combination of tokens, for WordReports they can be in the same line, in different paragraph or different rows of a table, but *all the tokens should be at the same level* in the underlying tree structure.

```
RIGHT: 
* @if[IsCancelled]
* Your account is cancelled
* @else
* Your account is accepted
* @endif
```

```
WRONG: 
* @if[IsCancelled]
* Your account is cancelled
* @else Your account is accepted @endif
```

```
WRONG: 
* @if[Canceled]
* Your account is cancelled
	* @else 
	* Your account is accepted 
	* @endif
```

The underlying tree structure is invisible using Microsoft Word, but resembles HTML.  

## Declarations

Any kind of node can declare aliases of the expression they are using: 

```
@if[User.Role!=] as $r

@foreach[Entity.Line.Element] as $l

@[Entity.Product] as $p
```

But you can also use a `@declare` token to *only* create an alias.


```
@declare[Entity.ShippingAddress] as $sa
@[$sa.Address]
@[$sa.City]
```

## Recomendations

In WordTemplates use crazy colors (like green or magenta) for tokens that should not be rendered (@if, @declare, @foreach) as a poor man syntax hightlight. 

The template will be easier to understand and the errors easier to spot in the generated report.  
