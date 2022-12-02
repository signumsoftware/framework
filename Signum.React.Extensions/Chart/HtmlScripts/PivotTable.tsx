import * as React from 'react'
import * as Navigator from '@framework/Navigator';
import * as Finder from '@framework/Finder';
import * as Constructor from '@framework/Constructor';
import * as ChartClient from '../ChartClient';
import { ChartColumn, ChartRow } from '../ChartClient';
import * as ChartUtils from '../D3Scripts/Components/ChartUtils';
import { Dic, softCast } from '@framework/Globals';
import InitialMessage from '../D3Scripts/Components/InitialMessage';
import { toNumberFormat } from '@framework/Reflection';
import './PivotTable.css'
import { Color } from '../../Basics/Color';
import { isLite, Lite, Entity, BooleanEnum } from '@framework/Signum.Entities';
import { FilterOptionParsed } from '@framework/Search';
import { QueryToken, FilterConditionOptionParsed, isFilterGroupOptionParsed, FilterGroupOption, FilterConditionOption, FilterOption, FindOptions } from '@framework/FindOptions';
import { ChartColumnType } from '../Signum.Entities.Chart';
import { EntityBaseController } from '@framework/Lines';
import { isBooleanOrFunctionOrNull } from '../../Dynamic/View/NodeUtils';
import { MultiValueLineController } from '../../../Signum.React/Scripts/Lines/MultiValueLine';
import { BigValueSearchCounter } from '../../Dashboard/View/UserQueryPart';
import { QueryString } from '../../../Signum.React/Scripts/QueryString';
import { QueryTokenMessage } from '../../../Signum.React/Scripts/Signum.Entities.DynamicQuery';

interface RowDictionary {
  [key: string]: { value: unknown, dicOrRows: RowDictionary | ChartRow[] };
}

class RowGroup {
  column: ChartColumn<unknown>;
  style: CellStyle;
  nextStyle?: CellStyle;
  value: unknown;

  subGroups?: RowGroup[];
  rows?: ChartRow[];

  parent?: RowGroup;

  constructor(style: CellStyle, nextStyle: CellStyle,  value: unknown, subGroups?: RowGroup[], rows?: ChartRow[]) {
    this.column = style.column!;
    this.style = style;
    this.nextStyle = nextStyle;
    this.value = value;
    this.subGroups = subGroups;
    if (this.subGroups)
      this.subGroups.forEach(a => a.parent = this);
    this.rows = rows;
  }

  getKey() {
    return this.column.getKey(this.value);
  }

  getNiceName() {
    if (this.column.token?.type.name == "boolean")
      return this.column.title + " = " +
        (this.value == true ? BooleanEnum.niceToString("True") :
          this.value == false ? BooleanEnum.niceToString("False") :
            "-" + QueryTokenMessage.Null.niceToString() + "-");

    if (this.value == null)
      return "-" + QueryTokenMessage.Null.niceToString() + "-";

    return this.column.getNiceName(this.value);
  }

  span(): number {

    var summary = this.nextStyle && this.nextStyle.subTotal == "yes" ||
      (this.style.placeholder == "empty" || this.style.placeholder == "filled") && this.nextStyle ? 1 : 0;

    var result = this.subGroups ? this.subGroups.sum(a => a.span()) :
      this.rows ? 1 :
        1;

    return result + summary;
  }

  getFilters(recursive: boolean): { col: ChartColumn<unknown>, val: unknown }[] {
    return [
      ...(recursive && this.parent ? this.parent.getFilters(recursive) : []),
      { col: this.column, val: this.value },
    ];
  }
}

type MultiNum = number | number[]; 

function getMultiAggregator(columns: ChartColumn<number>[]): (values: MultiNum[]) => MultiNum {

  function getSingleAggregator(token: QueryToken): (values: MultiNum[], selector?: (mn: number[]) => number, countSelector?: (mn: number[]) => number) => number {
    if (token.queryTokenType != "Aggregate")
      return (vals, selector) => vals.sum(selector as (mn: MultiNum) => number);

    if (token.key.startsWith("Count"))
      return (vals, selector) => vals.sum(selector as (mn: MultiNum) => number);

    if (token.key == "Sum")
      return (vals, selector) => vals.sum(selector as (mn: MultiNum) => number);

    if (token.key == "Min")
      return (vals, selector) => vals.min(selector as (mn: MultiNum) => number)!;

    if (token.key == "Max")
      return (vals, selector) => vals.max(selector as (mn: MultiNum) => number)!;

    if (token.key == "Average")
      return (vals, selector, countSelector) => {
        var sel = selector as (mn: MultiNum) => number;
        var cnt = countSelector as (mn: MultiNum) => number;
        return vals.sum(mn => sel(mn) * cnt(mn)) / vals.sum(cnt)!
      }

    throw new Error('getSingleAggregator not implemented for ' + token.key);
  }

  function getCountNNSelectors(c: ChartColumn<number>) {
    if (c.token!.queryTokenType == "Aggregate" && c.token!.key == "Average") {
      const countNNColumn = columns.firstOrNull(s => s.token?.queryTokenType == "Aggregate" && s.token!.key == "CountNotNull" && s.token?.parent?.fullKey == c.token?.parent?.fullKey);

      if (countNNColumn == null)
        throw new Error(`Unable to combine values an Avg token (${c.token!.fullKey}) because no equivalent CountNotNull token found (${c.token?.parent?.fullKey + ".CountNotNull"})`);

      const index = columns.indexOf(countNNColumn);
      return (array: number[]) => array[index];
    }

    return undefined;
  };

  if (columns.length == 1) {
    const count0 = getCountNNSelectors(columns[0]); //Just for the error
    return getSingleAggregator(columns[0].token!) as (values: MultiNum[]) => MultiNum;
  }

  if (columns.length == 2) {
    const ag0 = getSingleAggregator(columns[0].token!); const count0 = getCountNNSelectors(columns[0]);
    const ag1 = getSingleAggregator(columns[1].token!); const count1 = getCountNNSelectors(columns[1]);

    return ((values: number[][]) => [
      ag0(values, a => a[0], count0),
      ag1(values, a => a[1], count1),
    ]) as (values: MultiNum[]) => MultiNum;
  }

  if (columns.length == 3) {
    const ag0 = getSingleAggregator(columns[0].token!); const count0 = getCountNNSelectors(columns[0]);
    const ag1 = getSingleAggregator(columns[1].token!); const count1 = getCountNNSelectors(columns[1]);
    const ag2 = getSingleAggregator(columns[2].token!); const count2 = getCountNNSelectors(columns[2]);

    return ((values: number[][]) => [
      ag0(values, a => a[0], count0),
      ag1(values, a => a[1], count1),
      ag2(values, a => a[2], count2),
    ]) as (values: MultiNum[]) => MultiNum;
  }

  if (columns.length == 4) {
    const ag0 = getSingleAggregator(columns[0].token!); const count0 = getCountNNSelectors(columns[0]);
    const ag1 = getSingleAggregator(columns[1].token!); const count1 = getCountNNSelectors(columns[1]);
    const ag2 = getSingleAggregator(columns[2].token!); const count2 = getCountNNSelectors(columns[2]);
    const ag3 = getSingleAggregator(columns[3].token!); const count3 = getCountNNSelectors(columns[3]);

    return ((values: number[][]) => [
      ag0(values, a => a[0], count0),
      ag1(values, a => a[1], count1),
      ag2(values, a => a[2], count2),
      ag3(values, a => a[3], count3),
    ]) as (values: MultiNum[]) => MultiNum;
  }

  throw new Error("Unexpected number of value columns " + columns.length);
}

function getMultiSelector(columns: ChartColumn<number>[]): (row: ChartRow) => MultiNum {

  if (columns.length == 1) {
    var c = columns[0];
    return c.getValue;
  }

  if (columns.length == 2) {
    var c0 = columns[0];
    var c1 = columns[1];
    return row => [
      c0.getValue(row),
      c1.getValue(row)
    ];
  }

  if (columns.length == 3) {
    var c0 = columns[0];
    var c1 = columns[1];
    var c2 = columns[2];
    return row => [
      c0.getValue(row),
      c1.getValue(row),
      c2.getValue(row)
    ];
  }

  if (columns.length == 4) {
    var c0 = columns[0];
    var c1 = columns[1];
    var c2 = columns[2];
    var c3 = columns[3];
    return row => [
      c0.getValue(row),
      c1.getValue(row),
      c2.getValue(row),
      c3.getValue(row),
    ];
  }

  throw new Error("Unexpected number of value columns " + columns.length);
}

function multiDictionary(rows: ChartRow[], columns: ChartColumn<unknown>[]): RowDictionary | ChartRow[] {
  if (columns.length == 0)
    return rows;

  const [firstCol, ...otherCols] = columns;

  return rows.groupBy(r => firstCol.getValueKey(r))
    .toObject(gr => gr.key, gr => ({
      value: firstCol.getValue(gr.elements[0]),
      dicOrRows: multiDictionary(gr.elements, otherCols)
    }));
}


interface CellStyle {
  cssStyle: React.CSSProperties | undefined;
  cssStyleDiv: React.CSSProperties | undefined;
  subTotal?: "no" | "yes";
  placeholder?: "no" | "empty" | "filled";
  background?: (key: any, number: number) => string | undefined;
  order?: string;
  _keys?: unknown[];
  _complete?: "No" | "Yes" | "FromFilters",
  column?: ChartColumn<unknown>;
  maxTextLength?: number;
  showCreateButton: boolean;
  showAggregateValues: boolean;
}

interface DimParameters {
  complete?: "No" | "Yes" | "Consistent" | "FromFilters",
  order?: string,
  gradient: string,
  scale: string,
  subTotal?: "no" | "yes"
  placeholder?: "no" | "empty" | "filled"
  cssStyle?: string,
  cssStyleDiv?: string,
  maxTextLength?: number,
  createButton?: "No" | "Yes"
  aggegrateValues?: "No" | "Yes"
}


export default function renderPivotTable({ data, width, height, parameters, loading, onDrillDown, initialLoad, chartRequest, onReload, dashboardFilter }: ChartClient.ChartScriptProps): React.ReactElement<any> {

  if (data == null)
    return (
      <svg direction="ltr" width={width} height={height}>
        <InitialMessage data={data} x={width / 2} y={height / 2} loading={loading} />
      </svg>
    );

  function getDimParameters(columnName: string): DimParameters {
    return ({
      complete: parameters["Complete " + columnName] as "No" | "Yes" | "Consistent" | "FromFilters",
      createButton: parameters["Show Create Button " + columnName] as "No" | "Yes",
      aggegrateValues: parameters["Show Aggregate Values " + columnName] as "No" | "Yes",
      order: parameters["Order " + columnName],
      gradient: parameters["Gradient " + columnName],
      scale: parameters["Scale " + columnName],
      placeholder: parameters["Placeholder " + columnName] as "no" | "empty" | "filled",
      subTotal: parameters["SubTotal " + columnName] as "no" | "yes",
      cssStyle: parameters["CSS Style " + columnName],
      cssStyleDiv: parameters["CSS Style (div) " + columnName],
      maxTextLength: parseInt(parameters["Max Text Length " + columnName]) || undefined,
    });
  }

  const horColsWitParams = [
    { col: data.columns.c0!, params: getDimParameters("Horizontal Axis") },
    { col: data.columns.c1!, params: getDimParameters("Horizontal Axis (2)") },
    { col: data.columns.c2!, params: getDimParameters("Horizontal Axis (3)") },
    { col: data.columns.c3!, params: getDimParameters("Horizontal Axis (4)") },
  ].filter(p => p.col != null);

  const vertColsWitParams = [
    { col: data.columns.c4!, params: getDimParameters("Vertical Axis") },
    { col: data.columns.c5!, params: getDimParameters("Vertical Axis (2)") },
    { col: data.columns.c6!, params: getDimParameters("Vertical Axis (3)") },
    { col: data.columns.c7!, params: getDimParameters("Vertical Axis (4)") },
  ].filter(p => p.col != null);

  const valueColumns = [
    data.columns.c8! as ChartColumn<number>,
    data.columns.c9! as ChartColumn<number>,
    data.columns.c10! as ChartColumn<number>,
    data.columns.c11! as ChartColumn<number>,
  ].notNull();

  var multiValueFormat = parameters["Multi-Value Format"] ||
      valueColumns.map((c, i) => "{" + i + "}").join(" - ");

  const horCols = horColsWitParams.map(a => a.col);
  const vertCols = vertColsWitParams.map(a => a.col);

  const horizontalDic = multiDictionary(data.rows, horCols);
  const verticalDic = multiDictionary(data.rows, vertCols);

  var aggregate = (
    horColsWitParams.some(a => a.params.aggegrateValues == "Yes") ||
    vertColsWitParams.some(a => a.params.aggegrateValues == "Yes")
  ) ? getMultiAggregator(valueColumns) : null;

  var selector = getMultiSelector(valueColumns);

  var firstValue: (array: MultiNum) => number =
    valueColumns.length == 1 ? num => num as number :
      array => (array as number[])[0];

  function sumValue(dor: RowDictionary | RowGroup | ChartRow[] | undefined): MultiNum {

    if (dor == undefined)
      return 0;

    if (Array.isArray(dor)) {
      if (dor.length == 1)
        return selector(dor[0]);

      return aggregate!(dor.map(row => selector(row)));
    }

    if (dor instanceof RowGroup)
      return dor.subGroups ? aggregate!(dor.subGroups.map(a => sumValue(a))) :
        dor.rows ? sumValue(dor.rows) :
          0;

    return aggregate!(Dic.getValues(dor).map(group2 => sumValue(group2.dicOrRows)));
  }

  function getLevelValues(dor: RowDictionary, level: number): MultiNum[] {
    if (level == 0)
      return Dic.getValues(dor).map(a => sumValue(a.dicOrRows));
    else
      return Dic.getValues(dor).flatMap(a => getLevelValues(a.dicOrRows as RowDictionary, level - 1));
  }

  function getCellStyle(values: ()=> number[], params: DimParameters, column?: ChartColumn<unknown>): CellStyle {

    let background: ((key: unknown, num: number) => string | undefined) | undefined = undefined;
    if (params.gradient == "EntityPalette" && column != null) {
      background = (key: unknown, num: number) => column.getColor(key) ?? undefined;
    }
    else if (params.scale && params.gradient != "None") {
      const scaleFunc = ChartUtils.scaleFor(valueColumns[0], values(), 0, 1, params.scale);
      const gradient = ChartUtils.getColorInterpolation(params.gradient)!;
      background = (key: unknown, num: number) => num == null ? "white" : gradient(scaleFunc(num)!);
    }

    return ({
      cssStyle: parseCssStyle(params.cssStyle),
      cssStyleDiv: parseCssStyle(params.cssStyleDiv),
      placeholder: params.placeholder,
      subTotal: params.subTotal,
      background: background,
      maxTextLength: params.maxTextLength,
      column: column,
      order: params.order,
      _keys: column && params.complete == "Consistent" ? data!.rows.map(row => column.getValue(row)).distinctBy(val => column.getKey(val)) : undefined,
      _complete: params.complete == "Consistent" ? undefined : params.complete,
      showCreateButton: params.createButton == "Yes",
      showAggregateValues: params.aggegrateValues == "Yes",
    });
  }

  function parseCssStyle(cssStyle: string | undefined): React.CSSProperties | undefined {
    if (!cssStyle)
      return undefined;
    try {

      return cssStyle.split(";").filter(a => Boolean(a)).toObject(a => {
        var name = a.before(":").trim();
        var camelCased = name.replace(/-([a-z])/g, function (g) { return g[1].toUpperCase(); });
        return camelCased;
      }, a => a.after(":").trim());
    } catch (e) {
      throw new Error(`Invalid CSS Style "${cssStyle}": ${(e as Error).message ?? e}`);
    }
  }

  function getRowGroups(gor: RowDictionary | ChartRow[] | undefined, styles: CellStyle[], level: number, filters: FilterConditionOptionParsed[]): RowGroup[] | ChartRow[] | undefined {
    if (Array.isArray(gor)) {
      if (styles.length == level)
        return gor;

      throw new Error("Unexpected Array in variable 'gor' at this level");
    }

    if (gor == undefined && styles.length == level) {
      return gor;
    }

    const style = styles[level];

    const col = style.column!;

    let keys = style._keys;
    if (!keys) {
      const currentValues = gor ? Dic.getValues(gor).map(a => a.value) : [];
      const allFilters = [...baseFilters, ...filters];
      const insertPoint = ChartUtils.insertPoint(col, valueColumns[0]);
      keys = ChartUtils.completeValues(col, currentValues, style._complete!, allFilters, insertPoint);
      keys = orderKeys(keys, style.order!, col, gor);
    }

    return keys.map(val => {

      const gr = styles.length < level + 1 ? undefined : getRowGroups(gor && (gor as RowDictionary)[col.getKey(val)]?.dicOrRows, styles, level + 1, [
        ...filters,
        { token: col.token!, operation: "EqualTo", value: val, frozen: false }
      ]);

      return new RowGroup(style, styles[level + 1], val,
        level < styles.length - 1 ? gr as RowGroup[] : undefined,
        level == styles.length - 1 ? gr as ChartRow[] : undefined);
    });
  }

  const horStyles = horColsWitParams.map((cp, i) => getCellStyle(() => getLevelValues(horizontalDic as RowDictionary, i).map(firstValue), cp.params, cp.col));
  const vertStyles = vertColsWitParams.map((cp, i) => getCellStyle(() => getLevelValues(verticalDic as RowDictionary, i).map(firstValue), cp.params, cp.col));


  const baseFilters = chartRequest.filterOptions.filter(fo => !isFilterGroupOptionParsed(fo)) as FilterConditionOptionParsed[];
  const horizontalGroups = getRowGroups(horizontalDic, horStyles, 0, []);
  const verticalGroups = getRowGroups(verticalDic, vertStyles, 0, []);

  const valueStyle = getCellStyle(() => data.rows.map(row => firstValue(selector(row))), getDimParameters("Value"));

  const cellFormatter = getCellFormatter(multiValueFormat, valueColumns);

  const typeName = chartRequest.queryKey;

  const isCreable = Navigator.isCreable(typeName, { isSearch: true });

  const detector = dashboardFilter?.getActiveDetector(chartRequest);

  function Cell(p:
    {
      gor: RowDictionary | RowGroup | ChartRow[] | undefined,
      filters?: { col: ChartColumn<unknown>, val: unknown }[],
      title?: string,
      style?: CellStyle,
      colSpan?: number,
      rowSpan?: number,
      indent?: number,
      isSummary?: number,
    }
  ) {

    var gr = p.gor instanceof RowGroup ? p.gor : undefined;
    var style = p.style ?? gr?.style;

    function handleNumberClick(e: React.MouseEvent<HTMLAnchorElement>) {
      e.preventDefault();

      if (Array.isArray(p.gor) && p.gor.length == 1 && p.gor[0].entity != null) {
        Navigator.view(p.gor[0].entity as Lite<Entity>)
          .then(() => onReload && onReload());
        return;
      }

      var filters = p.filters ?? gr?.getFilters(true);

      if (filters == null)
        throw new Error("Unexpected no filters");

      onDrillDown(filters.toObject(a => a.col.name, a => a.val), e);
    }

    var lite = gr && isLite(gr.value) ? gr.value : undefined;

    let multiVal: MultiNum |undefined ;

    const link = (p.gor == null || style == null || style.showAggregateValues == false) ? null : <a href="#" onClick={e => handleNumberClick(e)}>{cellFormatter(multiVal ??= sumValue(p.gor))}</a>;

    var color =
      p.isSummary == 4 ? "rgb(228, 228, 228)" :
        p.isSummary == 3 ? "rgb(236, 236, 236)" :
          p.isSummary == 2 ? "rgb(241, 241, 241)" :
            p.isSummary == 1 ? "#f8f8f8" :
              style && style.background && style.background(gr?.value, firstValue(multiVal ??= sumValue(p.gor)));

    

    let cssStyle: React.CSSProperties | undefined = style && {
      backgroundColor: color,
      color:
        p.isSummary == 4 ? "rgb(66, 66, 66)" :
          p.isSummary == 3 ? "rgb(97, 97, 97)" :
            p.isSummary == 2 ? "rgb(115, 115, 115)" :
              p.isSummary == 1 ? "rgb(191, 191, 191)" :
                color != null ? Color.parse(color).lerp(0.5, Color.parse(color).opositePole()).toString() : undefined,
      paddingLeft: p.indent ? (p.indent * 30) + "px" : undefined,
      textAlign: p.indent != undefined ? "left" : "center",
      fontWeight: p.isSummary ? "bold" : undefined,
      ...style?.cssStyle,
    };


    if (detector != null) {
      var active = detector((p.filters ?? gr?.getFilters(true))!.toObject(a => a.col.name, a => a.val) as ChartRow);
      cssStyle = {
        ...cssStyle,
        color: active ? "black" : cssStyle?.color,
        opacity: !active ? .5 : cssStyle?.opacity,
        fontWeight: active ? "bold" : cssStyle?.fontWeight
      };
    }

    var createLink = p.style?.showCreateButton && isCreable && <a className="sf-create-cell" href="#" onClick={handleCreateClick}>{EntityBaseController.createIcon}</a>;

    function handleCreateClick(e: React.MouseEvent) {
      e.preventDefault()
      var filters = p.filters ?? gr?.getFilters(true);

      if (filters == null)
        throw new Error("Unexpected no filters");

      var fop = [
        ...filters.map(f => softCast<FilterOptionParsed>({ token: f.col.token!, operation: "EqualTo", value: f.val, frozen: false })),
        ...chartRequest.filterOptions,
      ];

      Finder.getPropsFromFilters(typeName, fop)
        .then(props => Constructor.construct(typeName, props))
        .then(e => e && Navigator.view(e))
        .then(() => onReload && onReload());
    }

    var title = p.title ?? (p.gor instanceof RowGroup ? p.gor.getNiceName() : undefined);
    if (title == null) {
      if (style?.cssStyleDiv)
        return (
          <td style={cssStyle}>
            <div style={style.cssStyleDiv}>
              {link}
              {createLink}
            </div>
          </td>
        );
      else
        return (
          <td style={cssStyle}>
            {link}
            {createLink}
          </td>
        );
    }

    function handleLiteClick(e: React.MouseEvent) {
      e.preventDefault();
      Navigator.view(lite as Lite<Entity>)
        .then(() => onReload && onReload());
    }

    var etcTitle = style && style.maxTextLength ? title.etc(style.maxTextLength) : title;

    var titleElement = isLite(lite) ?
      <a href="#" onClick={handleLiteClick} title={title}>{etcTitle}</a> :
      <span title={title}>{etcTitle}</span>

    if (style?.cssStyleDiv)
      return (
        <th style={cssStyle} colSpan={p.colSpan} rowSpan={p.rowSpan}>
          <div style={style.cssStyleDiv}>
            {titleElement}
            {link && <span> ({link})</span>}
            {createLink}
          </div>
        </th>
      );
    else
      return (
        <th style={cssStyle} colSpan={p.colSpan} rowSpan={p.rowSpan}>
          {titleElement}
          {link && <span> ({link})</span>}
          {createLink}
        </th>
      );
  }

  function orderKeys(keys: unknown[], order: string, col: ChartColumn<unknown>, group: RowDictionary | undefined) {
    switch (order) {
      case "Ascending": return keys.orderBy(a => a);
      case "AscendingToStr": return keys.orderBy(a => col.getNiceName(a));
      case "AscendingKey": return keys.orderBy(a => col.getKey(a));
      case "AscendingSumValues": return keys.orderBy(a => group && sumValue(group[col.getKey(a)]?.dicOrRows));
      case "Descending": return keys.orderByDescending(a => a);
      case "DescendingToStr": return keys.orderByDescending(a => col.getNiceName(a));
      case "DescendingKey": return keys.orderByDescending(a => col.getKey(a));
      case "DescendingSumValues": return keys.orderByDescending(a => group && sumValue(group[col.getKey(a)]?.dicOrRows));
      case "None": return keys;
      default: return keys;
    }
  }

  function verColSpan(minCol: number) {
    return vertStyles.filter((a, i) => i >= minCol && a.column && a.placeholder == "no" && vertStyles[i + 1]?.column).length + 1;
  }

  function allCells(group: RowGroup | ChartRow[] | undefined): ChartRow[] {
    if (group == undefined)
      return [];

    if (Array.isArray(group))
      return group;

    if (group.rows)
      return group.rows;

    if (group.subGroups)
      return group.subGroups.flatMap(gr => allCells(gr));

    return [];
  }

  const empty: (RowGroup | undefined)[] = [undefined];

  return (
    <div className="table-responsive" style={{ maxWidth: width }}>
      <table className="table table-bordered pivot-table">
        <thead>
          <tr>
            <td scope="col" colSpan={verColSpan(0)} rowSpan={Math.max(1, horCols.length)} style={{ border: "0px" }}></td>
            {horCols.length == 0 ? <Cell gor={horizontalGroups as ChartRow[]} title={valueColumns[0].displayName} /> :
              <HeaderGroupControl grhList={horizontalGroups as RowGroup[]} level={0} targetLevel={0}/>
            }
          </tr>
          {horCols.length >= 2 &&
            <tr>
              <HeaderGroupControl grhList={horizontalGroups as RowGroup[]} level={0} targetLevel={1} />
            </tr>
          }
          {horCols.length >= 3 &&
            <tr>
              <HeaderGroupControl grhList={horizontalGroups as RowGroup[]} level={0} targetLevel={2} />
            </tr>
          }
          {horCols.length >= 4 &&
            <tr>
              <HeaderGroupControl grhList={horizontalGroups as RowGroup[]} level={0} targetLevel={3} />
            </tr>
          }
        </thead>

        <tbody>
          {
            vertCols.length == 0 ?
              <tr>
                <Cell style={undefined} gor={verticalGroups as ChartRow[]} title={valueColumns[0].displayName} />
                {cells(verticalGroups as ChartRow[], [])}
              </tr> :
              (verticalGroups as RowGroup[]).map(grv => <RowGroupControl key={grv.getKey()} grv={grv} level={0} indent={0} />)
          }
        </tbody>
      </table>
    </div >
  );

  function HeaderGroupControl({ grhList, level, targetLevel }: { grhList: RowGroup[], level: number, targetLevel: number }) {
    return (
      <>
        {
          level == targetLevel ?
            grhList.map(grh => <Cell key={grh.getKey()} colSpan={grh.span()} gor={grh} />) :
            grhList.map(grh => <HeaderGroupControl key={grh.getKey()} grhList={grh.subGroups!} level={level + 1} targetLevel={targetLevel} />)
        }
        {level == targetLevel && horStyles[level].subTotal == "yes" && <Cell style={horStyles[level]}
          gor={undefined} title="Σ" isSummary={(horStyles.length - level)} rowSpan={horStyles.length } />}
      </>
    );
  }

  function RowGroupControl({ grv, level, placeholderCell, indent }: { grv: RowGroup | undefined, level: number, indent: number, placeholderCell?: React.ReactNode }) {

    if (level == vertCols.length - 1)
      return (
        <tr>
          {placeholderCell}
          <Cell gor={grv} indent={indent} colSpan={verColSpan(level)} />
          {cells(grv?.rows ?? [], grv?.getFilters(true) ?? [])}
        </tr>
      );
    else
      return (
        <React.Fragment>
          {vertStyles[level].placeholder != "no" && <tr>
            {placeholderCell}
            <Cell gor={grv} indent={indent} colSpan={verColSpan(level)} />
            {vertStyles[level].placeholder == "filled" && cells(allCells(grv), grv?.getFilters(true) ?? [], 1)}
          </tr>}
          {(grv?.subGroups ?? empty).map((grvNext, i) => <RowGroupControl key={grvNext?.getKey()}
            grv={grvNext}
            level={level + 1}
            indent={vertStyles[level].placeholder == "no" ? 0 : indent + 1}
            placeholderCell={i == 0 && vertStyles[level].placeholder == "no" ?
              <>
                {placeholderCell}
                <Cell gor={grv} indent={indent} rowSpan={grv?.span()} />
              </> : undefined} />)}
        </React.Fragment>
      );
  }
 

  function cells(rows: ChartRow[], filters: { col: ChartColumn<unknown>, val: unknown }[], isSummary?: number) {

    const gor = multiDictionary(rows, horCols);

    return (
      horCols.length == 0 ?
        <Cell gor={gor as ChartRow[]} style={valueStyle} filters={filters} isSummary={isSummary} /> :
        <CellGroup gor={gor} grhList={horizontalGroups as RowGroup[]} filters={filters} level={0} isSummary={isSummary} />
    );
  }

  function CellGroup({ gor, grhList, level, filters, isSummary, }: { gor: RowDictionary | ChartRow[] | undefined, grhList: (RowGroup | undefined)[], level: number, filters: { col: ChartColumn<unknown>, val: unknown }[], isSummary?: number }) {

    var isLast = level == horCols.length - 1;

    return (
      <>
        {
          grhList.map(grh => {

            var newFilters = [...filters, ...grh?.getFilters(false) ?? []];

            var grNext = gor && grh && (gor as RowDictionary)[grh.getKey()]?.dicOrRows;
            if (isLast)
              return <Cell key={grh?.getKey()} style={valueStyle} isSummary={isSummary} gor={grNext} filters={newFilters} />;
            else
              return <CellGroup key={grh?.getKey()} gor={grNext} filters={newFilters} grhList={grh?.subGroups ?? empty} level={level + 1} isSummary={isSummary} />;
          })
        }
        {horStyles[level].subTotal == "yes" && <Cell isSummary={(isSummary ?? 0) + (horStyles.length - level)} style={valueStyle}
          gor={gor} filters={filters} />}
      </>
    );
  }

 
}


function getCellFormatter(multiValueFormat: string, valueColumns: ChartColumn<number>[]): (v: MultiNum) => string{

  var fmtWith = compileFormatter(multiValueFormat);

  if (valueColumns.length == 1) {
    const f0 = valueColumns[0];
    return n => fmtWith(f0.getNiceName(n as number));
  }

  if (valueColumns.length == 2) {
    const f0 = valueColumns[0];
    const f1 = valueColumns[1];
    return n => fmtWith(
      f0.getNiceName((n as number[])[0]),
      f1.getNiceName((n as number[])[1]),
    );
  }

  if (valueColumns.length == 3) {
    const f0 = valueColumns[0];
    const f1 = valueColumns[1];
    const f2 = valueColumns[2];
    return n => fmtWith(
      f0.getNiceName((n as number[])[0]),
      f1.getNiceName((n as number[])[1]),
      f2.getNiceName((n as number[])[2]),
    );
  }

  if (valueColumns.length == 4) {
    const f0 = valueColumns[0];
    const f1 = valueColumns[1];
    const f2 = valueColumns[2];
    const f3 = valueColumns[3];
    return n => fmtWith(
      f0.getNiceName((n as number[])[0]),
      f1.getNiceName((n as number[])[1]),
      f2.getNiceName((n as number[])[2]),
      f3.getNiceName((n as number[])[3]),
    );
  }

  throw new Error("Unexpected " + valueColumns.length);
}

function compileFormatter(format: string): (...args: string[]) => string {
  var matches = [...format.matchAll(/\{(?<num>\d)\}/g)];

  if (matches.length == 0)
    return () => format;

  var args = matches.map((m, i) => "a" + i);

  var exp = JSON.stringify(format.substring(0, matches[0].index));
  for (var i = 0; i < matches.length; i++) {
    var m = matches[i];

    exp += " + a" + m.groups!["num"];
    var endIndex = m.index! + m[0]!.length;

    var nextStartIndex = matches[i + 1]?.index ?? format.length;

    exp += " + " + JSON.stringify(format.substring(endIndex, nextStartIndex));
  }


  return new Function(...args, "return " +  exp + ";") as (...args: string[]) => string;
}
