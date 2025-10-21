# CodeGenerator

Whenever possible, we prefer hand-written code, a succinct API, and runtime intelligence over code generation. However, when language limitations make repetitive code unavoidable, automated code generation can save significant development time.

The code generation classes are designed to run in the Load application, so the solution must compile successfully to use them. Unlike Visual Studio Item Templates, these generators do not require installation and offer greater power and customization.

The goal remains: once code is generated, you own it and should adapt it to your needs.

The `CodeGenerator` class acts as a facade for several specialized code generators:

- **[EntityCodeGenerator](EntityCodeGenerator.md):** Reads legacy database system views and generates compatible entities and operation declarations.
- **[LogicCodeGenerator](LogicCodeGenerator.md):** Generates logic classes from compiled entity classes, including entity tables, query registration, expressions, and operations.
- **[ReactCodeGenerator](ReactCodeGenerator.md):** Generates React components, TypeScript client classes, and C# controllers from entity classes.
- **[ReactCodeConverter](ReactCodeConverter.md):** Attempts to convert clipboard content from `.cshtml` to `.tsx`. Manual adjustments are required, but it accelerates migration from Signum.Web to Signum.React.

All generator classes are self-contained, easy to understand, and can be overridden for custom needs. Code generation respects the existing module structure and is organized by feature, not technical concern.

For a detailed tutorial on using these tools with a legacy database, see [Legacy Databases: Connecting to AdventureWorks](LegacyDatabase.AdventureWorks.md).
