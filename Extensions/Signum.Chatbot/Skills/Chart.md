Creating a chart in Signum Framework is very similar to creating a FindOptions, so most of the concepts are shared. 

After you have identified the root query, and the columns you want to display, you need select the best matching type of chart (ChartScript).

## Get ChartScripts

Call the tool `getChartScripts` to get the list of available ChartScripts.

Each chart has a key and a list of columns it requires. 

```ts
interface ChatScript
{
    key: string;
    columns: ChartScriptColumn[];
}

interface ChartScriptColumn
{
    displayName: string;
    isOptional: boolean;
    columnType: ChartColumnType;
}

type ChartColumnType = 
    "Number" | "DecimalNumber" | "Date" | "DateTime" | "String" | "Entity" | "Enum" | "RoundedNumber" | "Time" |
    "AnyGroupKey" | "AnyNumber" | "AnyNumberDateTime" | "AllTypes";
```

The `columnType` is a bitwise enum, so for example, if a chart script requires:
* `AnyGroupKey` you can provide column of type `RoundedNumber`, `Number`, `Date`, `String`, `Entity` or `Enum`.
* `AnyNumber` you can provide column of type `RoundedNumber`, `Number` or `DecimalNumber`,
* `AnyNumberDateTime` you can provide `RoundedNumber`, `Number`, `DecimalNumber`, `Date`, `DateTime` or `Time`.
* `AnyType` you can provide any column.

## Preparing ChartOptions

A ChartOptions is very similar to a FindOptions:

```TS
export interface ChartOptions {
  queryName: string;
  chartScript: string;
  filterOptions?: FilterOption[];
  chartColumnOptions: ChartColumnOption[];
}
```

The `chartScript` is the key of the ChatScript you want to use. 

The number of `chartColumnOptions` should be identical to the number of columns required by the ChartScript. 

```TS
export interface ChartColumnOption {
  scriptColumnName: string;
  token?: string;
  orderByIndex?: string;
  orderByType?: OrderType;
}
```

Each `chartColumnOptions[i].scriptColumnName` should have the `columns[i].name` of the selected ChartScript.

The `token` is the token you want to use for that column. It could be empty if `columns[i].isOptional` is true.

Optionally you can specify `orderByIndex` and `orderByType` to sort the results by that column: 
* `orderByType` can be "Ascending" or "Descending" 
* `orderByIndex` should be a positive number that indicates the priority of the order (1 for first, 2 for second, etc).
