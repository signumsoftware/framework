# DebugTextWriter 

This simple class implements a `TextWriter` writing in `System.Diagnostics.Debug` class. Useful for tests.

Example: 

```C#
[¬TestInitialize]
public void Initialize()
{
   ¬Connection.CurrentLog = new ¬DebugTextWriter();
}
```   
