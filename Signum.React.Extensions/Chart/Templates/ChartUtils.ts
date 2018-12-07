import * as d3 from "d3"
import * as moment from "moment"
import * as d3sc from "d3-scale-chromatic";
import { ChartTable, ChartColumn, ChartRow } from "../ChartClient"
import { parseLite } from "@framework/Signum.Entities"
import * as Navigator from '@framework/Navigator'
import { coalesce, Dic } from "../../../../Framework/Signum.React/Scripts/Globals";
import { getTypeInfo } from "@framework/Reflection";

export function getNavigateRoute(liteData: string) {
  var lite = parseLite(liteData);
  return Navigator.navigateRoute(lite);
}

export function navigateEntity(liteData: string) {
  var lite = parseLite(liteData);
  window.open(Navigator.navigateRoute(lite));
}

((d3.select(document) as any).__proto__ as d3.Selection<any, any, any, any>).enterData = function (this: d3.Selection<any, any, any, any>, data: any, tag: string, cssClass: string) {
  return this.selectAll(tag + "." + cssClass).data(data)
    .enter().append("svg:" + tag)
    .attr("class", cssClass);
};

declare module "d3-selection" {
  interface Selection<GElement extends d3.BaseType, Datum, PElement extends d3.BaseType, PDatum> {
    enterData<NElement extends d3.BaseType, NDatum = NDatum>(data: NDatum[], tag: string, cssClass: string): Selection<NElement, NDatum, GElement, Datum>;
    enterData<NElement extends d3.BaseType, NDatum = NDatum>(data: (data: Datum) => NDatum[], tag: string, cssClass: string): Selection<NElement, NDatum, GElement, Datum>;
  }
}

export function ellipsis(elem: SVGTextElement, width: number, padding?: number, ellipsisSymbol?: string) {

  if (ellipsisSymbol == undefined)
    ellipsisSymbol = '…';

  if (padding)
    width -= padding * 2;

  const self = d3.select(elem);
  let textLength = (<any>self.node()).getComputedTextLength();
  let text = self.text();
  while (textLength > width && text.length > 0) {
    text = text.slice(0, -1);
    while (text[text.length - 1] == ' ' && text.length > 0)
      text = text.slice(0, -1);
    self.text(text + ellipsisSymbol);
    textLength = (<any>self.node()).getComputedTextLength();
  }
}

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

export function insertPoint(column: ChartColumn<any>, valueColumn: ChartColumn<any>) : "Middle" | "Before" | "After" {

  if ((valueColumn.orderByIndex || 0) > (column.orderByIndex || 0)) {
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

    var allValues = Dic.getValues(getTypeInfo(column.token!.type.name).members).filter(a => !a.isIgnoredEnum).map(a => a.name);

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

    return [...allValues, ...oldValues];
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

export function rule(object: any, totalSize?: number): Rule {
  return new Rule(object, totalSize);
}

export class Rule {

  private sizes: { [key: string]: number } = {};
  private starts: { [key: string]: number } = {};
  private ends: { [key: string]: number } = {};

  totalSize: number;

  constructor(object: any, totalSize?: number) {

    let fixed = 0;
    let proportional = 0;
    for (const p in object) {
      const value = object[p];
      if (typeof value === 'number')
        fixed += value;
      else if (Rule.isStar(value))
        proportional += Rule.getStar(value);
      else
        throw new Error("values should be numbers or *");
    }

    if (!totalSize) {
      if (proportional)
        throw new Error("totalSize is mandatory if * is used");

      totalSize = fixed;
    }

    this.totalSize = totalSize;

    const remaining = totalSize - fixed;
    const star = proportional <= 0 ? 0 : remaining / proportional;

    for (const p in object) {
      const value = object[p];
      if (typeof value === 'number')
        this.sizes[p] = value;
      else if (Rule.isStar(value))
        this.sizes[p] = Rule.getStar(value) * star;
    }

    let acum = 0;

    for (const p in this.sizes) {
      this.starts[p] = acum;
      acum += this.sizes[p];
      this.ends[p] = acum;
    }
  }

  static isStar(val: string) {
    return typeof val === 'string' && val[val.length - 1] == '*';
  }

  static getStar(val: string) {
    if (val === '*')
      return 1;

    return parseFloat(val.substring(0, val.length - 1));
  }


  size(name: string) {
    return this.sizes[name];
  }

  start(name: string) {
    return this.starts[name];
  }

  end(name: string) {
    return this.ends[name];
  }

  middle(name: string) {
    return this.starts[name] + this.sizes[name] / 2;
  }

  debugX(chart: d3.Selection<any, any, any, any>) {

    const keys = d3.keys(this.sizes);

    //paint x-axis rule
    chart.append('svg:g').attr('class', 'x-rule-tick')
      .enterData(keys, 'line', 'x-rule-tick')
      .attr('x1', d => this.ends[d])
      .attr('x2', d => this.ends[d])
      .attr('y1', 0)
      .attr('y2', 10000)
      .style('stroke-width', 2)
      .style('stroke', 'Pink');

    //paint y-axis rule labels
    chart.append('svg:g').attr('class', 'x-axis-rule-label')
      .enterData(keys, 'text', 'x-axis-rule-label')
      .attr('transform', (d, i) => {
        return translate(this.starts[d] + this.sizes[d] / 2 - 5, 10 + 100 * (i % 3)) +
          rotate(90);
      })
      .attr('fill', 'DeepPink')
      .text(d => d);
  }

  debugY(chart: d3.Selection<any, any, any, any>) {

    const keys = d3.keys(this.sizes);

    //paint y-axis rule
    chart.append('svg:g').attr('class', 'y-rule-tick')
      .enterData(keys, 'line', 'y-rule-tick')
      .attr('x1', 0)
      .attr('x2', 10000)
      .attr('y1', d => this.ends[d])
      .attr('y2', d => this.ends[d])
      .style('stroke-width', 2)
      .style('stroke', 'Violet');

    //paint y-axis rule labels
    chart.append('svg:g').attr('class', 'y-axis-rule-label')
      .enterData(keys, 'text', 'y-axis-rule-label')
      .attr('transform', (d, i) => translate(100 * (i % 3), this.starts[d] + this.sizes[d] / 2 + 4))
      .attr('fill', 'DarkViolet')
      .text(d => d);
  }
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



export function stratifyTokens(
  data: ChartTable,
  keyColumn: ChartColumn<unknown>, /*Employee*/
  keyColumnParent?: ChartColumn<unknown>, /*Employee.ReportsTo*/):
  d3.HierarchyNode<ChartRow | Folder | Root> {


  const folders = data.rows
    .filter(r => keyColumnParent != null && keyColumnParent.getValue(r) != null)
    .map(r => ({ folder: keyColumnParent!.getValue(r) }) as Folder)
    .toObjectDistinct(r => keyColumnParent!.getKey(r.folder));

  const root: Root = { isRoot: true };

  const NullConst = "- Null -";


  const dic = data.rows.filter(r => keyColumn.getValue(r) != null).toObjectDistinct(r => keyColumn.getValueKey(r));

  const getParent = (d: ChartRow | Folder | Root) => {
    if ((d as Root).isRoot)
      return null;

    if ((d as Folder).folder) {
      const r = dic[keyColumnParent!.getKey((d as Folder).folder)];

      if (!r)
        return root;

      const parentValue = keyColumnParent!.getValue(r);
      if (parentValue == null)
        return root;  //Either null

      return folders[keyColumnParent!.getKey(parentValue)]; // Parent folder
    }

    var keyVal = keyColumn.getValue(d as ChartRow);

    if (keyVal) {
      const r = d as ChartRow;

      var fold = folders[keyColumn.getKey(keyVal)];
      if (fold)
        return fold; //My folder

      if (keyColumnParent) {

        const parentValue = keyColumnParent.getValue(r);

        const parentFolder = parentValue && folders[keyColumnParent.getKey(parentValue)];

        if (parentFolder)
          return folders[keyColumnParent.getKey(parentFolder.folder)]; //only parent
      }

      return root; //No key an no parent
    }

    throw new Error("Unexpected " + JSON.stringify(d))
  };

  var getKey = (r: ChartRow | Folder | Root) => {

    if ((r as Root).isRoot)
      return "#Root";

    if ((r as Folder).folder)
      return "F#" + keyColumnParent!.getKey((r as Folder).folder);

    const cr = (r as ChartRow);

    if (keyColumn.getValue(cr) != null)
      return keyColumn.getKey(cr);

    return NullConst;
  }

  var rootNode = d3.stratify<ChartRow | Folder | Root>()
    .id(getKey)
    .parentId(r => {
      var parent = getParent(r);
      return parent ? getKey(parent) : null
    })([root, ...Object.values(folders), ...data.rows]);

  return rootNode

}

export interface Folder {
  folder: unknown;
}

export function isFolder(obj: any): obj is Folder {
  return (obj as Folder).folder !== undefined;
}

export interface Root {
  isRoot: true;
}

export function isRoot(obj: any): obj is Root {
  return (obj as Root).isRoot;
}


export function toPivotTable(data: ChartTable,
  col0: ChartColumn<unknown>, /*Employee*/
  usedCols: ChartColumn<number>[]): PivotTable {

  var rows = data.rows
    .map((r) => ({
      rowValue: col0.getValue(r),
      values: usedCols.toObject(cn => cn.name, (cn): PivotValue => ({
        rowClick: r,
        value: cn.getValue(r),
        valueTitle: `${col0.getValueNiceName(r)}, ${cn.title}: ${cn.getValueNiceName(r)}`
      }))
    } as PivotRow));

  var title = usedCols.map(c => c.title).join(" | ");

  return {
    title,
    columns: d3.values(usedCols.toObject(c => c.name, c => ({
      color: null,
      key: c.name,
      niceName: c.title,
    } as PivotColumn))),
    rows,
  };
}

export function groupedPivotTable(data: ChartTable,
  col0: ChartColumn<unknown>, /*Employee*/
  colSplit: ChartColumn<unknown>,
  colValue: ChartColumn<number>): PivotTable {

  var columns = d3.values(data.rows.map(r => colSplit.getValue(r)).toObjectDistinct(v => colSplit.getKey(v), v => ({
    niceName: colSplit.getNiceName(v),
    color: colSplit.getColor(v),
    key: colSplit.getKey(v),
  }) as PivotColumn));

  var rows = data.rows.groupBy(r => "k" + col0.getValueKey(r))
    .map(gr => {

      var rowValue = col0.getValue(gr.elements[0]);
      return {
        rowValue: rowValue,
        values: gr.elements.toObject(
          r => colSplit.getValueKey(r),
          (r): PivotValue => ({
            value: colValue.getValue(r),
            rowClick: r,
            valueTitle: `${col0.getNiceName(rowValue)}, ${colSplit.getValueNiceName(r)}: ${colValue.getValueNiceName(r)}`
          })),
      } as PivotRow;
    });

  var title = data.columns.c2!.title + " / " + data.columns.c1!.title;

  return {
    title,
    columns,
    rows,
  } as PivotTable;
}

export interface PivotTable {
  title: string;
  columns: PivotColumn[];
  rows: PivotRow[];
}

export interface PivotColumn {
  key: string;
  color?: string | null;
  niceName?: string | null;
}

export interface PivotRow {
  rowValue: unknown;
  values: { [key: string /*| number*/]: PivotValue };
}

export interface PivotValue {
  rowClick: ChartRow;
  value: number;
  valueTitle: string;
}
