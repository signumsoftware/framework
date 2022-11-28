import { DateTime, DurationUnit, Duration, DateTimeUnit } from "luxon"
import * as d3 from "d3"
import { ChartTable, ChartColumn, ChartRow } from "../../ChartClient"
import { parseLite } from "@framework/Signum.Entities"
import * as Navigator from '@framework/Navigator'
import { coalesce, Dic } from "@framework/Globals";
import { tryGetTypeInfo } from "@framework/Reflection";
import { ChartRequestModel } from "../../Signum.Entities.Chart";
import { isFilterGroupOption, isFilterGroupOptionParsed, FilterConditionOptionParsed, FilterOptionParsed, QueryToken, FilterConditionOption } from "@framework/FindOptions";
import { MemoRepository } from "./ReactChart";
import * as ColorUtils from "../../ColorPalette/ColorUtils"
import { colorInterpolators } from "../../ColorPalette/ColorUtils"



export function translate(x: number, y: number) {
  if (y == undefined)
    return 'translate(' + x + ')';

  return 'translate(' + x + ',' + y + ')';
}

export function scale(x: number, y: number) {
  if (y == undefined)
    return 'scale(' + x + ')';

  return 'scale(' + x + ',' + y + ')';
}

export function rotate(angle: number, x?: number, y?: number): string {
  if (x == undefined || y == undefined)
    return 'rotate(' + angle + ')';

  return 'rotate(' + angle + ',' + y + ',' + y + ')';
}

export function skewX(angle: number): string {
  return 'skewX(' + angle + ')';
}

export function skewY(angle: number): string {
  return 'skewY(' + angle + ')';
}

export function matrix(a: number, b: number, c: number, d: number, e: number, f: number): string {
  return 'matrix(' + a + ',' + b + ',' + c + ',' + d + ',' + e + ',' + f + ')';
}

export function scaleFor(column: ChartColumn<any>, values: number[], minRange: number, maxRange: number, scaleName: string | null | undefined): d3.ScaleContinuousNumeric<number, number> {

  if (scaleName == "ZeroMax") {

    let max = d3.max(values)!;
    if (max == 0) // To keep the color or 0 stable
      max = 1;

    return d3.scaleLinear()
      .domain([0, max])
      .range([minRange, maxRange])
      .nice();

  }

  if (scaleName == "MinMax") {
    if (column.type == "DateOnly" || column.type == "DateTime") {
      var dates = values.map(d => new Date(d));

      const scale = d3.scaleTime()
        .domain([d3.min(dates)!, d3.max(dates)!])
        .range([minRange, maxRange]);

      const f = function (d: string | Date) { return scale(typeof d == "string" ?  new Date(d) : d); } as any as d3.ScaleContinuousNumeric<number, number>;
      f.ticks = scale.ticks as any;
      f.tickFormat = scale.tickFormat as any;
      return f;
    }
    else if (column.type == "Time") {
      var dates = values.map(d => DateTime.fromFormat(d as any as string, "HH:mm:ss.u").toJSDate());

      const scale = d3.scaleTime()
        .domain([d3.min(dates)!, d3.max(dates)!])
        .range([minRange, maxRange]);

      const f = function (d: string | Date) { return scale(typeof d == "string" ? DateTime.fromFormat(d, "HH:mm:ss.u").toJSDate() : d); } as any as d3.ScaleContinuousNumeric<number, number>;
      f.ticks = scale.ticks as any;
      f.tickFormat = scale.tickFormat as any;
      return f;
    }
    else {
      return d3.scaleLinear()
        .domain([d3.min(values)!, d3.max(values)!])
        .range([minRange, maxRange])
        .nice();
    }
  }

  if (scaleName == "Log")
    return d3.scaleLog()
      .domain([d3.min(values)!, d3.max(values)!])
      .range([minRange, maxRange])
      .nice();

  if (scaleName == "Sqrt")
    return d3.scalePow().exponent(.5)
      .domain([d3.min(values)!, d3.max(values)!])
      .range([minRange, maxRange]);

  throw Error("Unexpected scale: " + scaleName);
}

export function insertPoint(keyColumn: ChartColumn<any>, valueColumn: ChartColumn<any>) : "Middle" | "Before" | "After" {

  if (valueColumn.orderByIndex != null && (keyColumn.orderByIndex == null || valueColumn.orderByIndex < keyColumn.orderByIndex)) {
    if (valueColumn.orderByType == "Ascending")
      return "Before";
    else
      return "After";
  } else {
    return "Middle"; 
  }
}


export function completeValues(column: ChartColumn<unknown>, values: unknown[], completeValues: string | null | undefined, filterOptions: FilterOptionParsed[], insertPoint: "Middle" | "Before" | "After"): unknown[] {
  if (completeValues == null || completeValues == "No")
    return values;

  function normalizeToken(qt: QueryToken): { normalized: QueryToken, lastPart?: QueryToken } {
    if ((qt.type.name == "DateOnly" || qt.type.name == "DateTime") &&
      qt.parent && (qt.parent.type.name == "DateOnly" || qt.parent.type.name == "DateTime"))
      switch (qt.key) {
        case "SecondStart":
        case "MinuteStart":
        case "HourStart":
        case "Date":
        case "WeekStart":
        case "MonthStart":
        case "MonthStart":
          return {
            normalized: qt.parent,
            lastPart : qt,
          };
      }

    return {
      normalized: qt,
      lastPart: undefined
    };
  }

  function durationUnit(lastPart: string): DateTimeUnit {
    switch (lastPart) {
      case "SecondStart": return "second";
      case "MinuteStart": return "minute";
      case "HourStart": return "hour";
      case "Date": return "day";
      case "WeekStart": return "week";
      case "MonthStart": return "month";
      default: throw new Error("Unexpected " + lastPart);
    }
  }

  function tryCeil(date: string | null | undefined, unit: DateTimeUnit): DateTime | undefined {
    if (date == null)
      return undefined;

    return ceil(DateTime.fromISO(date), unit);
  }


  function ceil(date: DateTime, unit: DateTimeUnit) {

    if (date.toMillis() == date.startOf(unit).toMillis())
      return date;

    return date.startOf(unit).plus({ [unit]: 1 });
  }

  function tryFloor(date: string | null | undefined, unit: DateTimeUnit): DateTime | undefined {
    if (date == null)
      return undefined;

    return floor(DateTime.fromISO(date), unit);
  }

  function floor(date: DateTime, unit: DateTimeUnit) {

    return date.startOf(unit);
  }

  function withoutEntity(fullKey: string) {
    if (fullKey.startsWith("Entity."))
      return fullKey.after("Entity.");

    return fullKey;
  }

  const columnNomalized = normalizeToken(column.token!);  

  const matchingFilters = column.token && (completeValues == "FromFilters" || completeValues == "Auto") ?
    (filterOptions.filter(f => !isFilterGroupOptionParsed(f)) as FilterConditionOptionParsed[])
      .filter(f => f.token && withoutEntity(normalizeToken(f.token).normalized.fullKey) == withoutEntity(columnNomalized.normalized.fullKey)) :
    [];

  if (completeValues == "FromFilters" && matchingFilters.length == 0)
    return values;

  const isAuto = completeValues == "Auto";

  const isInFilter = matchingFilters.firstOrNull(a => a.operation == "IsIn");

  if (isInFilter)
    return complete(values, isInFilter.value as unknown[], column, insertPoint);

  if (column.type == "Lite" || column.type == "String")
    return values;

  if (column.type == "DateOnly" || column.type == "DateTime") {

    const unit: DurationUnit | null = columnNomalized.lastPart != null ? durationUnit(columnNomalized.lastPart.key) :
      columnNomalized.normalized.type.name == "DateOnly" ? "day" : null;

    if (unit == null)
      return values;


    const min = d3.max(matchingFilters.filter(a => a.operation == "GreaterThan" || a.operation == "GreaterThanOrEqual" || a.operation == "EqualTo")
      .map(f => {
        const pair = normalizeToken(f.token!);

        const value = DateTime.fromISO(f.value);

        //Date.MonthStart >  1.4.2000
        //             Min-> 1.5.2000
        //Date.MonthStart >= 1.4.2000
        //             Min-> 1.4.2000
        //Date.MonthStart == 1.4.2000
        //             Min-> 1.4.2000

        var filterUnit = pair.lastPart != null ? durationUnit(pair.lastPart.key) :
          f.token?.type.name == "DateOnly" ? "day" : null;

        const newValue = filterUnit == null ? value :
          f.operation == "GreaterThan" ? floor(value, filterUnit).plus({ [filterUnit]: 1 }) : floor(value, filterUnit);

        return floor(newValue, unit).toISO();
      }).notNull()) ?? tryFloor(d3.min(values as string[]), unit)?.toISO();

    const max = d3.min(matchingFilters.filter(a => a.operation == "LessThan" || a.operation == "LessThanOrEqual" || a.operation == "EqualTo")
      .map(f => {
        const pair = normalizeToken(f.token!);
        let value = DateTime.fromISO(f.value);

        //Date.MonthStart <  1.4.2000
        //             Max   1.4.2000
        //Date.MonthStart <= 1.4.2000
        //                   1.5.2000
        //Date.MonthStart == 1.4.2000
        //             Max   1.5.2000

        var filterUnit = pair.lastPart != null ? durationUnit(pair.lastPart.key) :
          f.token?.type.name == "DateOnly" ? "day" : null;

        const newValue = filterUnit == null ? value :
          f.operation == "LessThan" ? ceil(value, filterUnit) : floor(value, filterUnit).plus({ [filterUnit]: 1 });

        return ceil(newValue, unit).toISO();
      }).notNull()) ?? tryCeil(d3.max(values as string[]), unit)?.toISO();


    if (min == undefined  || max == undefined)
      return values;

    var isServerUtc = values.some(a => (a as string).endsWith("Z"));

    const minDate = DateTime.fromISO(min, { zone: isServerUtc ? "utc" : undefined }).startOf(unit); //Needed to fix offset issues after UTC conversion
    const maxDate = DateTime.fromISO(max, { zone: isServerUtc ? "utc" : undefined }).startOf(unit); //Needed to fix offset issues after UTC conversion
    let date = minDate;

    const allValues: string[] = [];
    const limit = isAuto ? values.length * 2 : null;
    while (date < maxDate) {

      if (limit != null && allValues.length > limit)
        return values;

      allValues.push(column.token!.type.name == "DateOnly" ? date.toISODate() : date.toISO(({ suppressMilliseconds: true })));
      date = date.plus({ [unit]: 1 });
    }

    return complete(values, allValues, column, insertPoint);
  }

  if (column.type == "Enum") {

    const typeName = column.token!.type.name; 
    
    if (typeName == "boolean") {
      return complete(values, [false, true], column, insertPoint);
    }

    const typeInfo = tryGetTypeInfo(column.token!.type.name);
    if (typeInfo == null)
      throw new Error("No Metadata found for " + typeName);

    const allValues = Dic.getValues(typeInfo.members).filter(a => !a.isIgnoredEnum).map(a => a.name);

    return complete(values, allValues, column, insertPoint);
  }

  if (column.type == "Integer" || column.type == "Real" || column.type == "RealGroupable") {

    const lastPart = column.token!.fullKey.tryAfterLast('.');

    const step: number | null = lastPart != null && lastPart.startsWith("Step") ? parseFloat(lastPart.after("Step").replace("_", ".")) :
      (column.type == "Integer" ? 1 : null);

    if (step == null)
      return values;

    const minFilter = matchingFilters.firstOrNull(a => a.operation == "GreaterThan" || a.operation == "GreaterThanOrEqual");
    const min = minFilter == null ? d3.min(values as number[]) :
      minFilter.operation == "GreaterThan" ? minFilter.value as number + step :
        minFilter.operation == "GreaterThanOrEqual" ? minFilter.value : undefined;

    const maxFilter = matchingFilters.firstOrNull(a => a.operation == "LessThan" || a.operation == "LessThanOrEqual");
    const max = maxFilter == null ? d3.max(values as number[]) :
      maxFilter.operation == "LessThan" ? maxFilter.value as number - step :
        maxFilter.operation == "LessThanOrEqual" ? maxFilter.value : undefined; 

    if (min == undefined || max == undefined)
      return values;

    const allValues: number[] = [];
    const limit = isAuto ? values.length * 2 : null;
    if (step < 1) {
      const inv = 1 / step;
      let v = min;
      while (v <= max) {

        if (limit != null && allValues.length > limit)
          return values;

        allValues.push(v);
        v = Math.round((v + step) * inv) / inv;
      }
    } else {
      let v = min;
      while (v <= max) {
        if (limit != null && allValues.length > limit)
          return values;

        allValues.push(v);
        v += step;
      }
    }
    return complete(values, allValues, column, insertPoint);
  }

  return values;
}

function complete(values: unknown[], allValues: unknown[], column: ChartColumn<unknown>, insertPoint: "Middle" | "Before" | "After"): any[] {
  
  if (insertPoint == "Middle") {

    const allValuesDic = allValues.toObject(column.getKey);
    
    const oldValues = values.filter(a => !allValuesDic.hasOwnProperty(column.getKey(a)));

    return [...column.orderByType == "Descending" ? allValues.reverse() : allValues, ...oldValues];
  }
  else {
    const valuesDic = values.toObject(column.getKey);
    
    const newValues = allValues.filter(a => !valuesDic.hasOwnProperty(column.getKey(a)));

    if (insertPoint == "Before")
      return [...newValues, ...values];
    else if (insertPoint == "After") //Descending
      return [...values, ...newValues];
  } 

  throw new Error();
}

export function getStackOffset(curveName: string): ((series: d3.Series<any, any>, order: number[]) => void) | undefined {
  switch (curveName) {
    case "zero": return d3.stackOffsetNone;
    case "expand": return d3.stackOffsetExpand;
    case "silhouette": return d3.stackOffsetSilhouette;
    case "wiggle": return d3.stackOffsetWiggle;
  }

  return undefined;
}



export function getStackOrder(schemeName: string): ((series: d3.Series<any, any>) => number[]) | undefined {
  switch (schemeName) {
    case "none": return d3.stackOrderNone;
    case "ascending": return d3.stackOrderAscending;
    case "descending": return d3.stackOrderDescending;
    case "insideOut": return d3.stackOrderInsideOut;
    case "reverse": return d3.stackOrderReverse;
  }

  return undefined;
}


export function getCurveByName(curveName: string): d3.CurveFactoryLineOnly | undefined {
  switch (curveName) {
    case "basis": return d3.curveBasis;
    case "bundle": return d3.curveBundle.beta(0.5);
    case "cardinal": return d3.curveCardinal;
    case "catmull-rom": return d3.curveCatmullRom;
    case "linear": return d3.curveLinear;
    case "monotone": return d3.curveMonotoneX;
    case "natural": return d3.curveNatural;
    case "step": return d3.curveStep;
    case "step-after": return d3.curveStepAfter;
    case "step-before": return d3.curveStepBefore;
  }

  return undefined;
}

export function colorCategory(parameters: { [name: string]: string }, domain: string[], memo: MemoRepository, memoKey?: string, deps?: []): d3.ScaleOrdinal<string, string> {

  var category = parameters["ColorCategory"];
  var categorySteps = parseInt(parameters["ColorCategorySteps"]);

  return memo.memo<d3.ScaleOrdinal<string, string>>(memoKey ?? "colorCategory", [category, categorySteps, ...(deps ?? [])], () => {

    var scheme = ColorUtils.colorSchemes[category];
    var scale = d3.scaleOrdinal(scheme);
    domain.forEach(a => scale(a));
    return scale;
  });
}

export function getColorInterpolation(interpolationName: string | undefined | null): ((value: number) => string) | undefined {

  return ColorUtils.getColorInterpolation(interpolationName);
}




