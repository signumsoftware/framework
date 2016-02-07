using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Signum.Utilities;
using Signum.Entities;
using System;

// General Information about an assembly is controlled through the following 
// set of attributes. Change these attribute values to modify the information
// associated with an assembly.
[assembly: AssemblyTitle("Signum.Entities")]
[assembly: AssemblyDescription("")]
[assembly: AssemblyConfiguration("")]
[assembly: AssemblyCompany("")]
[assembly: AssemblyProduct("Signum.Entities")]
[assembly: AssemblyCopyright("Copyright Signum Software©  2008")]
[assembly: AssemblyTrademark("")]
[assembly: AssemblyCulture("")]

// Setting ComVisible to false makes the types in this assembly not visible 
// to COM components.  If you need to access a type in this assembly from 
// COM, set the ComVisible attribute to true on that type.
[assembly: ComVisible(false)]

// The following GUID is for the ID of the typelib if this project is exposed to COM
[assembly: Guid("e273ab91-88a9-4d39-8081-592c0770dd33")]

[assembly: InternalsVisibleTo("Signum.Engine")]
[assembly: InternalsVisibleTo("Signum.Web")]
[assembly: InternalsVisibleTo("Signum.React")]

[assembly: DefaultAssemblyCulture("en")]

[assembly: ImportInTypeScript(typeof(DayOfWeek), "Signum.Entities")]
[assembly: ImportInTypeScript(typeof(CollectionMessage), "Signum.Entities")]
