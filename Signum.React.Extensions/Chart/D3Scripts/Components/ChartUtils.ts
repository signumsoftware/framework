import * as d3 from "d3"
import * as moment from "moment"
import * as d3sc from "d3-scale-chromatic";
import { ChartTable, ChartColumn, ChartRow } from "../../ChartClient"
import { parseLite } from "@framework/Signum.Entities"
import * as Navigator from '@framework/Navigator'
import { coalesce, Dic } from "@framework/Globals";
import { getTypeInfo } from "@framework/Reflection";



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

  if (scaleName == "ZeroMax")
    return d3.scaleLinear()
      .domain([0, d3.max(values)!])
      .range([minRange, maxRange])
      .nice();

  if (scaleName == "MinMax") {
    if (column.type == "Date" || column.type == "DateTime") {
      const scale = d3.scaleTime()
        .domain(values)
        .range([minRange, maxRange]);

      const f = function (d: string) { return scale(new Date(d)); } as any as d3.ScaleContinuousNumeric<number, number>;
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

export function completeValues(column: ChartColumn<unknown>, values: unknown[], completeValues: string | null | undefined, insertPoint: "Middle" | "Before" | "After"): unknown[] {
  
  if (completeValues == null || completeValues == "No")
    return values;

  if (column.type == "Lite" || column.type == "String")
    return values;

  const isAuto = completeValues == "Auto"

  if (column.type == "Date" || column.type == "DateTime") {

    const min = d3.min(values as string[]);
    const max = d3.max(values as string[]);

    if (min == undefined || max == undefined)
      return values; 

    const minMoment = moment(min);
    const maxMoment = moment(max);

    const lastPart = column.token!.fullKey.tryAfterLast('.');

    const unit: moment.unitOfTime.Base | null =
      lastPart == "SecondStart" ? "s" :
        lastPart == "MinuteStart" ? "m" :
          lastPart == "HourStart" ? "h" :
            lastPart == "Date" ? "d" :
              lastPart == "WeekStart" ? "w" :
                lastPart == "MonthStart" ? "M" :
                  null;

    if (unit == null)
      return values;

    const allValues: string[] = [];
    const limit = isAuto ? values.length * 2 : null;
    while (minMoment <= maxMoment) {

      if (limit != null && allValues.length > limit)
        return values;

      allValues.push(minMoment.format("YYYY-MM-DDTHH:mm:ss"));
      minMoment.add(unit, 1);
    }

    return complete(values, allValues, column, insertPoint);
  }

  if (column.type == "Enum") {

    var typeName = column.token!.type.name; 
    
    if (typeName == "boolean") {
      return complete(values, [false, true], column, insertPoint);
    }

    var typeInfo = getTypeInfo(column.token!.type.name);
    if (typeInfo == null)
      throw new Error("No Metadata found for " + typeName);

    var allValues = Dic.getValues(typeInfo.members).filter(a => !a.isIgnoredEnum).map(a => a.name);

    return complete(values, allValues, column, insertPoint);
  }

  if (column.type == "Integer" || column.type == "Real" || column.type == "RealGroupable") {

    const min = d3.min(values as number[]) as number | undefined;
    const max = d3.max(values as number[]) as number | undefined;

    if (min == undefined || max == undefined)
      return values;

    const lastPart = column.token!.fullKey.tryAfterLast('.');
    
    const step: number | null = lastPart != null && lastPart.startsWith("Step") ? parseFloat(lastPart.after("Step").replace("_", ".")) : 
        (column.type == "Integer" ? 1 : null);

    if (step == null)
      return values;

    const allValues: number[] = [];
    const limit = isAuto ? values.length * 2 : null;
    if (step < 1) {
      var inv = 1 / step;
      var v = min;
      while (v <= max) {

        if (limit != null && allValues.length > limit)
          return values;

        allValues.push(v);
        v = Math.round((v + step) * inv) / inv;
      }
    } else {
      var v = min;
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
    
    var oldValues = values.filter(a => !allValuesDic.hasOwnProperty(column.getKey(a)));

    return [...column.orderByType == "Descending" ? allValues.reverse() : allValues, ...oldValues];
  }
  else {
    const valuesDic = values.toObject(column.getKey);
    
    var newValues = allValues.filter(a => !valuesDic.hasOwnProperty(column.getKey(a)));

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

export function getColorInterpolation(interpolationName: string | undefined | null): ((value: number) => string) | undefined {
  switch (interpolationName) {
    case "YlGn": return d3sc.interpolateYlGn;
    case "YlGnBu": return d3sc.interpolateYlGnBu;
    case "GnBu": return d3sc.interpolateGnBu;
    case "BuGn": return d3sc.interpolateBuGn;
    case "PuBuGn": return d3sc.interpolatePuBuGn;
    case "PuBu": return d3sc.interpolatePuBu;
    case "BuPu": return d3sc.interpolateBuPu;
    case "RdPu": return d3sc.interpolateRdPu;
    case "PuRd": return d3sc.interpolatePuRd;
    case "OrRd": return d3sc.interpolateOrRd;
    case "YlOrRd": return d3sc.interpolateYlOrRd;
    case "YlOrBr": return d3sc.interpolateYlOrBr;
    case "Purples": return d3sc.interpolatePurples;
    case "Blues": return d3sc.interpolateBlues;
    case "Greens": return d3sc.interpolateGreens;
    case "Oranges": return d3sc.interpolateOranges;
    case "Reds": return d3sc.interpolateReds;
    case "Greys": return d3sc.interpolateGreys;
    case "PuOr": return d3sc.interpolatePuOr;
    case "BrBG": return d3sc.interpolateBrBG;
    case "PRGn": return d3sc.interpolatePRGn;
    case "PiYG": return d3sc.interpolatePiYG;
    case "RdBu": return d3sc.interpolateRdBu;
    case "RdGy": return d3sc.interpolateRdGy;
    case "RdYlBu": return d3sc.interpolateRdYlBu;
    case "Spectral": return d3sc.interpolateSpectral;
    case "RdYlGn": return d3sc.interpolateRdYlGn;
  }

  return undefined;
}

export function getColorScheme(schemeName: string | null | undefined, k: number | undefined = 11): ReadonlyArray<string> | undefined {
  switch (schemeName) {
    case "category10": return d3.schemeCategory10;
    case "accent": return d3sc.schemeAccent;
    case "dark2": return d3sc.schemeDark2;
    case "paired": return d3sc.schemePaired;
    case "pastel1": return d3sc.schemePastel1;
    case "pastel2": return d3sc.schemePastel2;
    case "set1": return d3sc.schemeSet1;
    case "set2": return d3sc.schemeSet2;
    case "set3": return d3sc.schemeSet3;
    case "BrBG[K]": return d3sc.schemeBrBG[k];
    case "PRGn[K]": return d3sc.schemePRGn[k];
    case "PiYG[K]": return d3sc.schemePiYG[k];
    case "PuOr[K]": return d3sc.schemePuOr[k];
    case "RdBu[K]": return d3sc.schemeRdBu[k];
    case "RdGy[K]": return d3sc.schemeRdGy[k];
    case "RdYlBu[K]": return d3sc.schemeRdYlBu[k];
    case "RdYlGn[K]": return d3sc.schemeRdYlGn[k];
    case "Spectral[K]": return d3sc.schemeSpectral[k];
    case "Blues[K]": return d3sc.schemeBlues[k];
    case "Greys[K]": return d3sc.schemeGreys[k];
    case "Oranges[K]": return d3sc.schemeOranges[k];
    case "Purples[K]": return d3sc.schemePurples[k];
    case "Reds[K]": return d3sc.schemeReds[k];
    case "BuGn[K]": return d3sc.schemeBuGn[k];
    case "BuPu[K]": return d3sc.schemeBuPu[k];
    case "OrRd[K]": return d3sc.schemeOrRd[k];
    case "PuBuGn[K]": return d3sc.schemePuBuGn[k];
    case "PuBu[K]": return d3sc.schemePuBu[k];
    case "PuRd[K]": return d3sc.schemePuRd[k];
    case "RdPu[K]": return d3sc.schemeRdPu[k];
    case "YlGnBu[K]": return d3sc.schemeYlGnBu[k];
    case "YlGn[K]": return d3sc.schemeYlGn[k];
    case "YlOrBr[K]": return d3sc.schemeYlOrBr[k];
    case "YlOrRd[K]": return d3sc.schemeYlOrRd[k];
  }

  return undefined;
}

interface CachedColorOrdinal {
  category: string;
  categorySteps: number;
  scale: d3.ScaleOrdinal<string, string>;
}

export function colorCategory(parameters: { [name: string]: string }, domain: string[]): d3.ScaleOrdinal<string, string> {

  const cacheKey = "_cachedColorOrdinal_"; 

  var category = parameters["ColorCategory"];
  var categorySteps = parseInt(parameters["ColorCategorySteps"]);
  if (parameters[cacheKey]) {
    const cached = parameters[cacheKey] as any as CachedColorOrdinal;

    if (cached.category == category && cached.categorySteps == categorySteps) {
      domain.forEach(a => cached.scale(a));
      return cached.scale;
    }
  }

  var scheme = getColorScheme(category, categorySteps);

  const newCached: CachedColorOrdinal = {
    category: category,
    categorySteps: categorySteps,
    scale: d3.scaleOrdinal(scheme).domain(domain),
  };

  parameters[cacheKey] = newCached as any;

  return newCached.scale;
}




