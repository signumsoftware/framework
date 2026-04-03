# NaturalLanguageTools

### SpacePascal

Returns a “StringLikeThis” into a normal “String Like This”. By default
preserves uppercase in English but it doesn't in other languages like
Spanish.

```C#
public static string SpacePascal(this string pascalStr)
public static string SpacePascal(this string pascalStr, bool preserveUppercase)
```

Example: 
```C#
"FirstName".SpacePascal(); // running on a en-US thread, returns "First Name"
"FirstName".SpacePascal(false); // retursn "First name"
"PrimerApellido".SpacePascal(); // running on a es-ES thread, returns "Primer apellido"
```

### NiceName

If the string has '\_', splits by '\_', otherwise uses SpacePascal. Used
for default description for field, types and properties.

`
public static string NiceName(this string memberName)
`

`
"FirstName".NiceName(); // returns "First Name"
"First_Name".NiceName(); // returns "First Name"
`