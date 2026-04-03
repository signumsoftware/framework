# Signum.Utilities.Csv class
--------------------

[CSV](http://en.wikipedia.org/wiki/Comma-separated_values#Specification) is a very simple file format for tabular data. Popular applications like Microsoft Excel can easily
open and save files in this format.

CSV files really shine as a way to generate tabular logs and integrate
data from other sources on your Loading Application, so
we decided to make a very lightweight CSV library in Signum.Utilities.


This library has two main functionalities, writing and reading CSV
files.


### Writing CSV Files

ToCSV method, as other utility methods here, uses public properties and
fields to define the columns.

```C#
//General overload that takes an stream.
public static void ToCsv<T>(this IEnumerable<T> collection, Stream stream, Encoding encoding = null, CultureInfo culture = null, bool writeHeaders = true, bool autoFlush = false,
   Func<CsvColumnInfo<T>, CultureInfo, Func<object, string>> toStringFactory = null)

//Overloads that returns a byte[], using a  MemoryStream. Useful for storing in the database or sending to server/client. 
public static byte[] ToCsvBytes<T>(this IEnumerable<T> collection, Encoding encoding = null, CultureInfo culture = null, bool writeHeaders = true, bool autoFlush = false,
   Func<CsvColumnInfo<T>, CultureInfo, Func<object, string>> toStringFactory = null)

//Overloads that takes a filename, using a FileStream to write the CSV. 
//Result: the fileName (to allow chaining)
public static string ToCsvFile<T>(this IEnumerable<T> collection, string fileName, Encoding encoding = null, CultureInfo culture = null, bool writeHeaders = true, bool autoFlush = false, bool append = false,
   Func<CsvColumnInfo<T>, CultureInfo, Func<object, string>> toStringFactory = null)
```

**Arguments:**
-   **encoding:** Used for writing the file. The default one is Windows-1252. You can see what is the current Encoding opening the
    Csv file in Visual Studio and using Advanced Save Options. 
-   **culture:** Used for ToString (DateTime, Numbers...). CultureInfo.CurrentCulture is the default.
-   **writeHeaders:** True to write the property names as column headers.
-   **toStringFactory:** Using this factory of functions, you can return a custom formatter for any specific column. 


Using a code very similar to the last example, but using ToCsv with a
filename instead:

```c#
new DirectoryInfo(@"C:\Users\Public\Pictures\Sample Pictures").GetFiles()
.Select(a=>new 
 {
   a.Name,  
   Size = a.Length.ToComputerSize(), 
   a.LastWriteTime, 
   a.CreationTime 
 })
.ToCSV("myFile.csv"); 
```

### Read CSV Files

Reading CSV files is a bit different. In order to get a `IEnumerable<T>`
that is filled with your CSV data you need a datasource (the file), and
the type (T in this case) that is used as a template to fill the data.

So the first thing is to define a type that allows us to read the file:

```c#
public class CSVFileLine
{
   public string Name; 
   public long Size; 
   public DateTime LastWriteTime; 
   public DateTime CreationTime; 
}
```

Then we just read the file like this:

```c#
var lines = Csv.ReadFile<CSVFileLine>("myFile.csv")
```

> **Note:** Take into account that the order eachf ield/property is declared, not the name, is used to parse each line of the CSV file.

There are many other overloads to use when you don't know the type

```c#
//General method that Parses a CSV file from a string IEnumerable
//skipFirstLine: True to avoid reading the first line (headers)
//IMPORTANT: Returns an IEnumerable and uses deferred execution, it's your responsability to keep the Stream open.
public static IEnumerable<T> ReadCVS<T>(this Stream stream, Encoding encoding, CultureInfo culture, bool skipFirst) where T : new()

//Simpler overload taking the fileName
public static List<T> ReadFile<T>(string fileName, Encoding encoding = null, CultureInfo culture = null, int skipLines = 1, bool trim = false, 
   Func<CsvColumnInfo<T>, CultureInfo, Func<string, object>> parserFactory = null) where T : new()

//Overload taking byte[], useful for reading CSV in entities or received from Client/Server 
public static List<T> ReadBytes<T>(byte[] data, Encoding encoding = null, CultureInfo culture = null, int skipLines = 1, bool trim = true,
   Func<CsvColumnInfo<T>, CultureInfo, Func<string, object>> parserFactory = null) where T : new()

//General overload taking a Stream
public static IEnumerable<T> ReadStream<T>(Stream stream, Encoding encoding = null, CultureInfo culture = null, int skipLines = 1, bool trim = true, 
    Func<CsvColumnInfo<T>, CultureInfo, Func<string, object>> parserFactory = null)


//General for parsing just one line
public static T ReadLine<T>(string csvLine, CultureInfo culture = null, bool trim = true, Func<CsvColumnInfo<T>, CultureInfo, Func<string, object>> parserFactory = null)
    where T : new()
```

**Writing-specific Arguments:**
-   **encoding:** Used for reading the file. The default one is Windows-1252. You can see what is the current Encoding opening the
    Csv file in Visual Studio and using Advanced Save Options. 
-   **culture:** Used for Parse (DateTime, Numbers...).  CultureInfo.CurrentCulture is the default.
-   **skipLines:** How many initial lines should be skipped, default is 1 (the headers).
-   **parserFactory:** Using this factory of functions, you can return a custom parser for any specific column. 
