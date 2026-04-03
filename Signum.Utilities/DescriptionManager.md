# DescriptionManager

This class is the responsible for localization of `Properties`, `Types` (singular and plural) and `Enums`. 

By localizing members and types, instead of plain strings in resource files, we can reuse more aggressively the same localized strings, and ensure consistency.

Simple messages that do not belong to any of this groups are also implemented as enums witch type-name ends with `Message` termination. We can use the different enum types for different controls/screens/reports that need to be localized. 

### DescriptionOptions & DescriptionOptionsAttribute

The enum `DescriptionOptions` determines, for each type, witch elements should be translated. 

```C#
[Flags]
public enum DescriptionOptions
{
    None = 0,

    Members = 1,
    Description = 2,
    PluralDescription = 4,
    Gender = 8,

    All = Members | Description | PluralDescription | Gender,
}
```

The the event `DefaultDescriptionOptions` (int `DescriptionManager`) functions can attached to give default values for `DescriptionOptions`.

```C#
public static event Func<Type, DescriptionOptions?> DefaultDescriptionOptions
```

By default this are the rules applied: 

*  **Enums ending with Message:** Members
*  **Symbols and SemiSymbols:** Members
*  **Enums in entity properties:** Members |  Description
*  **Enums ending with Query:** Members

Additionally, this behavior can be overridden using `DescriptionOptionsAttribute`.

For example `ModifiableEntity` has it, and is inherited for all the entities: 

```C#
[DescriptionOptions(DescriptionOptions.Members | DescriptionOptions.Description)]
public abstract class ModifiableEntity
{

}
```


## Nice methods

Once we know a piece of metadata is localized `DescriptionManager` exposes simple extension methods to get the string that should be shown to the user. 


### NiceToString (Enums)

Returns the user-friendly string representation of a enum value. 

```C#
public static string NiceToString(this Enum a)
```

It tries to find the value in the following order: 

1. The translated XML file for the current culture (es-ES)
2. The translated XML file for the current culture parent (es)
3. The translated XML file for the assembly default culture (en)
4. The DescriptionAttribute in the field
5. The `NiceName` of the field
 * If has `_`, just replace them by space
 * Otherwise, space the name by each uppercase change (`SpacePascal`)

Additionally, there's an overload for the common case of calling `FormatWith` after `NiceToString`

```C#
public static string NiceToString(this Enum a, params object[] args)
{
    return a.NiceToString().FormatWith(args);
}
```
Example: 

```C#
public enum RegisterUserMessage
{
    [Description("Username '{0}' is already in use"]
    Username0IsAlreadyInUse,
}

(...)

RegisterUserMessage.Username0IsAlreadyInUse.NiceToString(userName)
```

### NiceName

Properties and Types can also be localized, using the same order as NiceToString does. 

```C#
public static string NiceName(this PropertyInfo pi)
public static string NiceName(this Type type)
```

### NicePluralName

Types can also have a plural description, useful for query names and messages ('There are already invoices...')

```C#
public static string NicePluralName(this Type type)
```

A similar order as NiceToString is taken to get the plural name, with some variations: 

* The XML files can contain a `PluralDescription` attribute for rare cases (i.e. child, children)
* `PluralDescriptionAttribute` can be used to encode the rare cases in the the default assembly language. 
* An automatic pluralization will be made by `NaturalLanguageTools.Pluralizers` otherwise.


### Gender 

Even if English is fortunate enough not to have [grammatical gender](http://en.wikipedia.org/wiki/Grammatical_gender), many languages vary articles, pronouns or adjectives depending the gender of a related noun. 

```C#
public static char? GetGender(this Type type)
```

A similar order as NiceToString is taken to get the gender, with some variations: 

* The XML files can contain a `Gender` attribute for rare cases (i.e. child, children)
* `GenderAttribute` can be used to encode the rare cases in the the default assembly language. 
* An automatic pluralization will be made by `NaturalLanguageTools.GenderDetectors` otherwise.

### ForGenderAndNumber

Once you know the gender (and number) of the principal nouns of a sentence you can use this information to vary a string message that has already been prepared to be polymorphic: 

```C#
public static string ForGenderAndNumber(this string genderAwareText, char? gender = null, int? number = null)
```

Example 

```C#
int count = Database.Query<T>().Count();

"Se ha[n] encontrado [1m:un|1f:una|m:unos|f:unas] {0} eliminad[1m:o|1f:a|m:os|f:as]"
.ForGenderAndNumber( typeof(T).GetGender(), count)
.FormatWith(count == 1 ? typeof(T).NiceName(), typeof(T).NicePluralName());  
```
 

# Translation module

Signum.Utilities only contains the basic features to make an application localizable, you can write create and translate the XML files yourself but using **Translation module** in Signum.Extensions it's easier to translate them (with the help of Bing) and synchronize them. 