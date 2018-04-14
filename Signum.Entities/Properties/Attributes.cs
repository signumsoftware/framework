using Signum.Entities;
using Signum.Utilities;
using System;
using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("Signum.Engine")]

[assembly: ImportInTypeScript(typeof(DayOfWeek), "Signum.Entities")]
[assembly: ImportInTypeScript(typeof(CollectionMessage), "Signum.Entities")]