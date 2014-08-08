# ExpressionToString

All the Expression nodes defined by .Net Framework use an internal method BuildString (using StringBuilder) to perform ToString functionality. 

While this is a good idea for performance reasons it makes it really hard to compose with user-defined expressions, because your `ToString` never gets called when part of a .Net Expression node. 

In order to make this work is necessary to replicate all the `ToString` logic in a visitor. 

```C#
internal class ¬ExpressionToString: ¬ExpressionVisitor
{
   public static string NiceToString(¬Expression exp)
}
```

Then, instead of call `ToString` over your `BinaryExpression`, call `NiceToString` instead.  
