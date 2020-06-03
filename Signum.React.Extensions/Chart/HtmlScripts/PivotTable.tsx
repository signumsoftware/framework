import * as React from 'react'
import { TextAlignProperty, VerticalAlignProperty } from 'csstype'
import numbro from 'numbro'
import * as Navigator from '@framework/Navigator';
import * as ChartClient from '../ChartClient';
import { ChartColumn, ChartRow } from '../ChartClient';
import * as ChartUtils from '../D3Scripts/Components/ChartUtils';
import { Dic, softCast } from '@framework/Globals';
import InitialMessage from '../D3Scripts/Components/InitialMessage';
import { toNumbroFormat } from '@framework/Reflection';
import './PivotTable.css'
import { Color } from '../../Basics/Color';
import { isLite, Lite, Entity, BooleanEnum } from '@framework/Signum.Entities';
import { FilterOptionParsed } from '../../../../Framework/Signum.React/Scripts/Search';
import { QueryToken, FilterConditionOptionParsed, isFilterGroupOptionParsed, FilterGroupOption, FilterConditionOption, FilterOption } from '@framework/FindOptions';
import { ChartColumnType } from '../Signum.Entities.Chart';

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
            "null");

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

  getFilters(): { col: ChartColumn<unknown>, val: unknown }[] {
    return [
      ...(this.parent?.getFilters() ?? []),
      { col: this.column, val: this.value },
    ];
  }
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
  subTotal?: "no" | "yes";
  placeholder?: "no" | "empty" | "filled";
  background?: (number: number) => string;
  order?: string;
  _keys?: unknown[];
  _complete?: "No" | "Yes" | "FromFilters",
  column?: ChartColumn<unknown>;
  maxTextLength?: number;
}

interface DimParameters {
  complete?: "No" | "Yes" | "Consistent" | "FromFilters",
  order?: string,
  gradient: string,
  scale: string,
  subTotal?: "no" | "yes"
  placeholder?: "no" | "empty" | "filled"
  cssStyle: string,
  maxTextLength?: number,
}


export default function renderPivotTable({ data, width, height, parameters, loading, onDrillDown, initialLoad, chartRequest }: ChartClient.ChartScriptProps): React.ReactElement<any> {

  if (data == null)
    return (
      <svg direction="ltr" width={width} height={height}>
        <InitialMessage data={data} x={width / 2} y={height / 2} loading={loading} />
      </svg>
    );

  function getDimParameters(columnName: string): DimParameters {
    return ({
      complete: parameters["Complete " + columnName] as "No" | "Yes" | "Consistent" | "FromFilters",
      order: parameters["Order " + columnName],
      gradient: parameters["Gradient " + columnName],
      scale: parameters["Scale " + columnName],
      placeholder: parameters["Placeholder " + columnName] as "no" | "empty" | "filled",
      subTotal: parameters["SubTotal " + columnName] as "no" | "yes",
      cssStyle: parameters["CSS Style " + columnName],
      maxTextLength: parseInt(parameters["Max Text Length " + columnName]) || undefined,
    });
  }

  const horColsWitParams = [
    { col: data.columns.c0!, params: getDimParameters("Horizontal Axis") },
    { col: data.columns.c1!, params: getDimParameters("Horizontal Axis (2)") },
    { col: data.columns.c2!, params: getDimParameters("Horizontal Axis (3)") },
  ].filter(p => p.col != null);

  const vertColsWitParams = [
    { col: data.columns.c3!, params: getDimParameters("Vertical Axis") },
    { col: data.columns.c4!, params: getDimParameters("Vertical Axis (2)") },
    { col: data.columns.c5!, params: getDimParameters("Vertical Axis (3)") },
  ].filter(p => p.col != null);

  const valueColumn = data.columns.c6! as ChartColumn<number>;

  const horCols = horColsWitParams.map(a => a.col);
  const vertCols = vertColsWitParams.map(a => a.col);

  const horizontalDic = multiDictionary(data.rows, horCols);
  const verticalDic = multiDictionary(data.rows, vertCols);

  function sumValue(dor: RowDictionary | RowGroup | ChartRow[] | undefined): number {

    if (dor == undefined)
      return 0;

    if (Array.isArray(dor))
      return dor.sum(a => valueColumn.getValue(a));

    if (dor instanceof RowGroup)
      return dor.subGroups ? dor.subGroups.sum(a => sumValue(a)) :
        dor.rows ? sumValue(dor.rows) :
          0;

    return Dic.getValues(dor).sum(group2 => sumValue(group2.dicOrRows));
  }

  function getLevelValues(dor: RowDictionary, level: number): number[] {
    if (level == 0)
      return Dic.getValues(dor).map(a => sumValue(a.dicOrRows));
    else
      return Dic.getValues(dor).flatMap(a => getLevelValues(a.dicOrRows as RowDictionary, level - 1));
  }

  function getCellStyle(values: number[], params: DimParameters, column?: ChartColumn<unknown>): CellStyle {

    let color: ((num: number) => string) | undefined = undefined;
    if (params.scale && params.gradient != "None") {
      const scaleFunc = ChartUtils.scaleFor(valueColumn, values, 0, 1, params.scale);
      const gradient = ChartUtils.getColorInterpolation(params.gradient)!;
      color = (num: number) => gradient(scaleFunc(num));
    }

    return ({
      cssStyle: parseCssStyle(params.cssStyle),
      placeholder: params.placeholder,
      subTotal: params.subTotal,
      background: color,
      maxTextLength: params.maxTextLength,
      column: column,
      order: params.order,
      _keys: column && params.complete == "Consistent" ? data!.rows.map(row => column.getValue(row)).distinctBy(val => column.getKey(val)) : undefined,
      _complete: params.complete == "Consistent" ? undefined : params.complete,
    });
  }

  function parseCssStyle(cssStyle: string | undefined): React.CSSProperties | undefined  {
    if (!cssStyle)
      return undefined;
    try {

      return cssStyle.split(";").filter(a => Boolean(a)).toObject(a => {
        var name = a.before(":");
        var camelCased = name.replace(/-([a-z])/g, function (g) { return g[1].toUpperCase(); });
        return camelCased;
      }, a => a.after(":"));
    } catch (e) {
      throw new Error(`Invalid CSS Style "${cssStyle}": ${(e as Error).message ?? e}`);
    }
  }

  function getRowGroups(gor: RowDictionary | ChartRow[] | undefined, styles: CellStyle[], level: number, filters: FilterConditionOptionParsed[]): RowGroup[] | ChartRow[] {
    if (Array.isArray(gor)) {
      if (styles.length == level)
        return gor;

      throw new Error("Unexpected Array in variable 'gor' at this level");
    }

    const style = styles[level];

    const col = style.column!;

    let keys = style._keys;
    if (!keys) {
      const currentValues = gor ? Dic.getValues(gor).map(a => a.value) : [];
      const allFilters = [...baseFilters, ...filters];
      const insertPoint = ChartUtils.insertPoint(col, valueColumn);
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

  const horStyles = horColsWitParams.map((cp, i) => getCellStyle(getLevelValues(horizontalDic as RowDictionary, i), cp.params, cp.col));
  const vertStyles = vertColsWitParams.map((cp, i) => getCellStyle(getLevelValues(verticalDic as RowDictionary, i), cp.params, cp.col));


  const baseFilters = chartRequest.filterOptions.filter(fo => !isFilterGroupOptionParsed(fo)) as FilterConditionOptionParsed[];
  const horizontalGroups = getRowGroups(horizontalDic, horStyles, 0, []);
  const verticalGroups = getRowGroups(verticalDic, vertStyles, 0, []);

  const valueStyle = getCellStyle(data.rows.map(a => valueColumn.getValue(a)), getDimParameters("Values"));

  const numbroFormat = toNumbroFormat(valueColumn.token?.format);

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

    function handleMumberClick(e: React.MouseEvent<HTMLAnchorElement>) {
      e.preventDefault();
      var filters = p.filters ?? gr?.getFilters();

      if (filters == null)
        throw new Error("Unexpected no filters");

      onDrillDown({
        ...filters.toObject(a => a.col.name, a => a.val),
      }, e);
    }

    var lite = gr && isLite(gr.value) ? gr.value : undefined;

    const val = sumValue(p.gor);

    const link = p.gor == null ? null : <a href="#" onClick={e => handleMumberClick(e)}>{numbro(val).format(numbroFormat)}</a>;

    var color =
      p.isSummary == 4 ? "rgb(228, 228, 228)" :
        p.isSummary == 3 ? "rgb(236, 236, 236)" :
          p.isSummary == 2 ? "rgb(241, 241, 241)" :
            p.isSummary == 1 ? "#f8f8f8" :
              style && style.background && style.background(val);

    const cssStyle: React.CSSProperties | undefined = style && {
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
      ...style?.cssStyle
    };

    var title = p.title ?? (p.gor instanceof RowGroup ? p.gor.getNiceName() : undefined);

    if (title == null) {
      return <td style={cssStyle}>{link}</td>;
    }

    function handleLiteClick(e: React.MouseEvent) {
      e.preventDefault();
      Navigator.navigate(lite as Lite<Entity>).done();
    }

    var etcTitle = style && style.maxTextLength ? title.etc(style.maxTextLength) : title;

    var titleElement = isLite(lite) ?
      <a href="#" onClick={handleLiteClick} title={title}>{etcTitle}</a> :
      <span title={title}>{etcTitle}</span>

    return (
      <th style={cssStyle} colSpan={p.colSpan} rowSpan={p.rowSpan}>
        {titleElement}
        {link && <span> ({link})</span>}
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
            {horCols.length == 0 ? <Cell gor={horizontalGroups as ChartRow[]} title={valueColumn.displayName} /> :
              (horizontalGroups as RowGroup[]).map(grh0 => grh0 && <Cell key={grh0.getKey()} colSpan={grh0.span()} gor={grh0} />)
            }
          </tr>
          {horCols.length >= 2 &&
            <tr>
              {(horizontalGroups as RowGroup[]).map(grh0 =>
                <React.Fragment key={grh0.getKey()} >
                  {(grh0.subGroups ?? empty).map(grh1 =>
                    <Cell key={grh1?.getKey()} colSpan={grh1?.span() ?? 1} gor={grh1} />)}
                  {horStyles[1].subTotal == "yes" && <Cell style={horStyles[1]}
                    gor={undefined} title="Σ" isSummary={horStyles[2]?.subTotal == "yes" ? 2 : 1} rowSpan={2} />}
                </React.Fragment>
              )}
            </tr>
          }
          {horCols.length >= 3 &&
            <tr>
              {(horizontalGroups as RowGroup[]).map(grh0 =>
                <React.Fragment key={grh0.getKey()} >
                  {(grh0.subGroups ?? empty).map(grh1 =>
                    <React.Fragment key={grh1?.getKey()}>
                      {(grh1?.subGroups ?? empty).map(grh2 =>
                        <Cell key={grh2?.getKey()} colSpan={grh2?.span() ?? 1} gor={grh2} />
                      )}
                      {horStyles[2].subTotal == "yes" && <Cell style={horStyles[2]}
                        gor={undefined} title="Σ" isSummary={1} />}
                    </React.Fragment>
                  )}
                </React.Fragment>
              )}
            </tr>
          }
        </thead>

        <tbody>
          {vertCols.length == 0 ?
            <tr>
              <Cell style={undefined} gor={verticalGroups as ChartRow[]} title={valueColumn.displayName} />
              {cells(verticalGroups as ChartRow[], [])}
            </tr> :
            vertCols.length == 1 ?
              (verticalGroups as RowGroup[]).map((grv0) =>
                <tr key={grv0.getKey()}>
                  <Cell gor={grv0} indent={0} />
                  {cells(grv0.rows!, grv0.getFilters())}
                </tr>
              ) :
              vertCols.length == 2 ?
                (verticalGroups as RowGroup[]).map((grv0, i) =>
                  <React.Fragment key={grv0.getKey()}>
                    {vertStyles[0].placeholder != "no" && <tr>
                      <Cell gor={grv0} indent={0}/>
                      {vertStyles[0].placeholder == "filled" && cells(allCells(grv0), grv0.getFilters(), 1)}
                    </tr>}
                    {(grv0.subGroups ?? empty).map((grv1, j) =>
                      <tr key={grv1?.getKey()}>
                        {j == 0 && vertStyles[0].placeholder == "no" &&
                          <Cell gor={grv0} indent={0} rowSpan={grv0.span()} />
                        }
                        <Cell gor={grv1} indent={vertStyles[0].placeholder != "no" ? 1 : 0} />
                        {cells(grv1?.rows ?? [], grv1?.getFilters() ?? [])}
                      </tr>
                    )}
                  </React.Fragment>) :
                vertCols.length == 3 ?
                  (verticalGroups as RowGroup[]).map((grv0, i) =>
                    <React.Fragment key={grv0.getKey()}>
                      {vertStyles[0].placeholder != "no" && <tr>
                        <Cell gor={grv0} indent={0} colSpan={verColSpan(0)} />
                        {vertStyles[0].placeholder == "filled" && cells(allCells(grv0), grv0.getFilters(), 2)}
                      </tr>}
                      {(grv0.subGroups ?? empty).map((grv1, j) =>
                        <React.Fragment key={grv1?.getKey()}>
                          {vertStyles[1].placeholder != "no" && <tr>
                            {j == 0 && vertStyles[0].placeholder == "no" &&
                              <Cell gor={grv0} indent={0} rowSpan={grv0.span()} />
                            }
                            <Cell gor={grv1} indent={vertStyles[0].placeholder != "no" ? 1 : 0} />
                            {vertStyles[1].placeholder == "filled" && cells(allCells(grv1), grv1?.getFilters() ?? [], 1)}
                          </tr>}
                          {(grv1?.subGroups ?? empty).map((grv2, k) =>
                            <tr key={grv2?.getKey()}>
                              {j == 0 && k == 0 && vertStyles[0].placeholder == "no" && vertStyles[1].placeholder == "no" &&
                                <Cell gor={grv0} indent={0} rowSpan={grv0.span()} />
                              }
                              {k == 0 && vertStyles[1].placeholder == "no" &&
                                <Cell gor={grv1} indent={(vertStyles[0].placeholder != "no" ? 1 : 0)} rowSpan={grv1?.span() ?? 1} />
                              }
                              <Cell gor={grv2} indent={(vertStyles[1].placeholder != "no" ? (vertStyles[0].placeholder != "no" ? 2 : 1) : 0)} />
                              {cells(grv2?.rows ?? [], grv2?.getFilters() ?? [])}
                            </tr>
                          )}
                        </React.Fragment>
                      )}
                    </React.Fragment>
                  ) :
                  null}
        </tbody>
      </table>
    </div >
  );

  function cells(rows: ChartRow[], filters: { col: ChartColumn<unknown>, val: unknown }[], isSummary?: number) {

    const gor = multiDictionary(rows, horCols);

    return (
      horCols.length == 0 ?
        <Cell gor={gor as ChartRow[]} style={valueStyle} filters={filters} isSummary={isSummary} /> :
        horCols.length == 1 ?
          (horizontalGroups as RowGroup[]).map(grh0 => {
            const gor0 = (gor as RowDictionary)[grh0.getKey()]?.dicOrRows;
            return (
              <Cell key={grh0.getKey()} style={valueStyle} isSummary={isSummary}
                gor={gor0} filters={[...filters, ...grh0.getFilters()]} />
            );
          }) :
          horCols.length == 2 ?
            (horizontalGroups as RowGroup[]).map(grh0 => {
              const gor0 = (gor as RowDictionary)[grh0.getKey()]?.dicOrRows;
              return (
                <React.Fragment key={grh0.getKey()}>
                  {(grh0.subGroups ?? empty).map(grh1 => {
                    const gor1 = gor0 && grh1 && (gor0 as RowDictionary)[grh1.getKey()]?.dicOrRows
                    return (
                      <Cell key={grh1?.getKey()} style={valueStyle} isSummary={isSummary}
                        gor={gor1} filters={[
                          ...filters,
                          ...(grh1?.getFilters() ?? [])
                        ]} />
                    );
                  })}
                  {horStyles[1].subTotal == "yes" && <Cell isSummary={(isSummary ?? 0) + 1} style={valueStyle}
                    gor={gor0} filters={[
                      ...filters,
                      ...grh0.getFilters()
                    ]} />}
                </React.Fragment>
              );
            }) :
            horCols.length == 3 ?
              (horizontalGroups as RowGroup[]).map(grh0 => {
                const gor0 = (gor as RowDictionary)[grh0.getKey()]?.dicOrRows;
                return (
                  <React.Fragment key={grh0.getKey()}>
                    {(grh0.subGroups ?? empty).map(grh1 => {
                      const gor1 = gor0 && grh1 && (gor0 as RowDictionary)[grh1.getKey()]?.dicOrRows
                      return (
                        <React.Fragment key={grh1?.getKey()}>
                          {(grh1?.subGroups ?? empty).map(grh2 => {
                            const grh2key = horCols[2].getKey(grh2?.value);
                            const gor2 = gor1 && (gor1 as RowDictionary)[grh2key]?.dicOrRows
                            return (
                              <Cell key={grh2key} style={valueStyle} isSummary={isSummary}
                                gor={gor2} filters={[
                                  ...filters,
                                  ...grh2?.getFilters() ?? []
                                ]} />
                            );
                          })}
                          {horStyles[2].subTotal == "yes" && <Cell isSummary={(isSummary ?? 0) + 1} style={valueStyle}
                            gor={gor1} filters={[
                              ...filters,
                              ...grh1?.getFilters() ?? []
                            ]} />}
                        </React.Fragment>
                      );
                    })}
                    {horStyles[1].subTotal == "yes" && <Cell isSummary={(isSummary ?? 0) + (horStyles[2]?.subTotal == "yes" ? 2 : 1)} style={valueStyle}
                      gor={gor0} filters={[
                        ...filters,
                        ...grh0?.getFilters() ?? []
                      ]} />}
                  </React.Fragment>
                );
              }) :
              null
    );
  }
}

