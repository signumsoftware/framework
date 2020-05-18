import * as React from 'react'
import { TextAlignProperty, VerticalAlignProperty } from 'csstype'
import numbro from 'numbro'
import * as ChartClient from '../ChartClient';
import { ChartColumn, ChartRow } from '../ChartClient';
import * as ChartUtils from '../D3Scripts/Components/ChartUtils';
import { Dic } from '@framework/Globals';
import InitialMessage from '../D3Scripts/Components/InitialMessage';
import { toNumbroFormat } from '@framework/Reflection';
import './PivotTable.css'
import { Color } from '../../Basics/Color';

type GroupOrRows = RowGroup | ChartRow[];

interface RowGroup {
  [key: string]: { value: unknown, groupOrRows: GroupOrRows };
}


function multiGroups(rows: ChartRow[], columns: ChartColumn<unknown>[]): GroupOrRows {
  if (columns.length == 0)
    return rows;

  const [firstCol, ...otherCols] = columns;

  return rows.groupBy(r => firstCol.getValueKey(r))
    .toObject(gr => gr.key, gr => ({
      value: firstCol.getValue(gr.elements[0]),
      groupOrRows: multiGroups(gr.elements, otherCols)
    }));
}


interface CellStyle {
  textAlign: string;
  verticalAlign: string;
  background?: (number: number) => string;
  keys?: unknown[];
  order?: string;
  column?: ChartColumn<unknown>;
}

interface DimParameters {
  complete?: string,
  order?: string,
  gradient: string,
  scale: string,
  textAlign: TextAlignProperty,
  verticalAlign: VerticalAlignProperty<string>
}


export default function renderPivotTable({ data, width, height, parameters, loading, onDrillDown, initialLoad, chartRequest }: ChartClient.ChartScriptProps): React.ReactElement<any> {

  if (data == null || data.rows.length == 0)
    return (
      <svg direction="ltr" width={width} height={height}>
        <InitialMessage data={data} x={width / 2} y={height / 2} loading={loading} />
      </svg>
    );

  function getDimParameters(columnName: string): DimParameters {
    return ({
      complete: parameters["Complete " + columnName],
      order: parameters["Order " + columnName],
      gradient: parameters["Gradient " + columnName],
      scale: parameters["Scale " + columnName],
      textAlign: parameters["text-align " + columnName] as TextAlignProperty,
      verticalAlign: parameters["vert-align " + columnName] as VerticalAlignProperty<string>,
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

  const horizontalGroups = multiGroups(data.rows, horCols);
  const verticalGroups = multiGroups(data.rows, vertCols);

  function sumValue(gor: GroupOrRows | undefined): number {

    if (gor == undefined)
      return 0;

    if (Array.isArray(gor))
      return gor.sum(a => valueColumn.getValue(a));

    return Dic.getValues(gor).sum(group2 => sumValue(group2.groupOrRows));
  }

  function getLevelValues(gor: RowGroup, level: number): number[] {
    if (level == 0)
      return Dic.getValues(gor).map(a => sumValue(a.groupOrRows));
    else
      return Dic.getValues(gor).flatMap(a => getLevelValues(a.groupOrRows as RowGroup, level - 1));
  }


  function getCellStyle(values: number[], params: DimParameters, column?: ChartColumn<unknown>): CellStyle {

    let color: ((num: number) => string) | undefined = undefined;
    if (params.scale && params.gradient != "None") {
      const scaleFunc = ChartUtils.scaleFor(valueColumn, values, 0, 1, params.scale);
      const gradient = ChartUtils.getColorInterpolation(params.gradient)!;
      color = (num: number) => gradient(scaleFunc(num));
    }

    let keys: unknown[] | undefined = undefined;
    if (column && params.complete != "No") {
      keys = data!.rows.map(row => column.getValue(row)).distinctBy(val => column.getKey(val));

      if (params.complete != "Consistent")
        keys = ChartUtils.completeValues(column, keys, params.complete, chartRequest, ChartUtils.insertPoint(column, valueColumn));
    }

    return ({
      textAlign: params.textAlign,
      verticalAlign: params.verticalAlign,
      background: color,

      column: column,
      order: params.order,
      keys: keys,
    });
  }

  const horStyles = horColsWitParams.map((cp, i) => getCellStyle(getLevelValues(horizontalGroups as RowGroup, i), cp.params, cp.col));
  const vertStyles = vertColsWitParams.map((cp, i) => getCellStyle(getLevelValues(horizontalGroups as RowGroup, i), cp.params, cp.col));

  const valueStyle = getCellStyle(data.rows.map(a => valueColumn.getValue(a)), getDimParameters("Values"));

  const numbroFormat = toNumbroFormat(valueColumn.token?.format);

  function span(gor: GroupOrRows | undefined, styles: CellStyle[], index: number): number {
    var st = styles[index + 1];
    if (st == null)
      return 1;

    if (st.keys)
      return st.keys.sum(k => span(gor && (gor as RowGroup)[st.column!.getKey(k)]?.groupOrRows, styles, index + 1));

    if (gor == null)
      return 1;

    return Dic.getValues(gor as RowGroup).sum(a => span(a.groupOrRows, styles, index + 1));
  }


  function Cell(p:
    {
      gor: GroupOrRows | undefined,
      filters: { col: ChartColumn<unknown>, val: unknown }[],
      title?: string,
      style: CellStyle | undefined,
      colSpan?: number;
      rowSpan?: number;
    }
  ) {

    function handleClick(e: React.MouseEvent<HTMLAnchorElement>) {
      onDrillDown({
        ...p.filters.toObject(a => a.col.name, a => a.val),
      });
    }

    const val = sumValue(p.gor);

    const link = p.gor == null ? null : <a href="#" onClick={e => handleClick(e)}>{numbro(val).format(numbroFormat)}</a>;

    var color = p.style && p.style.background && p.style.background(val);

    const style: React.CSSProperties | undefined = p.style && {
      backgroundColor: color,
      color: color != null ? Color.parse(color).lerp(0.5, Color.parse(color).opositePole()).toString() : undefined,
      textAlign: p.style?.textAlign as TextAlignProperty,
      verticalAlign: p.style?.verticalAlign as VerticalAlignProperty<string | number>,
    };

    if (p.title == null) {
      return <td style={style}>{link}</td>;
    }

    return (
      <th style={style} colSpan={p.colSpan} rowSpan={p.rowSpan}>{
        link ? <span title={p.title}>{p.title} ({link})</span> :
          <span title={p.title}>{p.title}</span>
      }</th>
    );
  }

  function getValues(group: RowGroup | undefined, style: CellStyle): ({ value: unknown, groupOrRows?: GroupOrRows } | undefined)[] {

    const col = style.column!;
    let keys = style.keys ?? (group == null ? null : Dic.getValues(group).map(a => a.value));

    if (keys == null)
      return [undefined];

    keys = orderKeys(keys, style.order!, col, group);

    return keys.map(val => ({ value: val, groupOrRows: group && group[col.getKey(val)]?.groupOrRows }));
  }

  function orderKeys(keys: unknown[], order: string, col: ChartColumn<unknown>, group: RowGroup | undefined) {
    switch (order) {
      case "Ascending": return keys.orderBy(a => a);
      case "AscendingToStr": return keys.orderBy(a => col.getNiceName(a));
      case "AscendingKey": return keys.orderBy(a => col.getKey(a));
      case "AscendingSumValues": return keys.orderBy(a => group && sumValue(group[col.getKey(a)]?.groupOrRows));
      case "Descending": return keys.orderByDescending(a => a);
      case "DescendingToStr": return keys.orderByDescending(a => col.getNiceName(a));
      case "DescendingKey": return keys.orderByDescending(a => col.getKey(a));
      case "DescendingSumValues": return keys.orderByDescending(a => group && sumValue(group[col.getKey(a)]?.groupOrRows));
      case "None": return keys;
      default: return keys;
    }
  }

  return (
    <div className="table-responsive" style={{ maxWidth: width }}>
      <table className="table table-bordered pivot-table">
        <thead>
          <tr>
            <td scope="col" colSpan={Math.max(1, vertCols.length)} rowSpan={Math.max(1, horCols.length)} style={{ border: "0px" }}></td>
            {horCols.length == 0 ? <Cell gor={horizontalGroups} filters={[]} title={valueColumn.displayName} style={undefined} /> :
              getValues(horizontalGroups as RowGroup, horStyles[0]).map(grh0 =>
                grh0 && <Cell key={horCols[0].getKey(grh0.value)} colSpan={span(grh0.groupOrRows, horStyles, 0)} style={horStyles[0]}
                  gor={grh0.groupOrRows} title={horCols[0].getNiceName(grh0.value)} filters={[{ col: horCols[0], val: grh0.value }]} />
              )
            }
          </tr>
          {horCols.length >= 2 &&
            <tr>
              {getValues(horizontalGroups as RowGroup, horStyles[0]).map(grh0 =>
                <React.Fragment key={horCols[0].getKey(grh0!.value)} >
                  {getValues(grh0!.groupOrRows as RowGroup, horStyles[1]).map(grh1 =>
                    grh1 && <Cell key={horCols[1].getKey(grh1.value)} colSpan={span(grh1.groupOrRows, horStyles, 1)} style={horStyles[1]}
                      gor={grh1.groupOrRows} title={horCols[1].getNiceName(grh1.value)} filters={[
                        { col: horCols[0], val: grh0!.value },
                        { col: horCols[1], val: grh1.value }]} />
                  )}
                </React.Fragment>
              )}
            </tr>
          }
          {horCols.length >= 3 &&
            <tr>
              {getValues(horizontalGroups as RowGroup, horStyles[0]).map(grh0 =>
                <React.Fragment key={horCols[0].getKey(grh0!.value)} >
                  {getValues(grh0!.groupOrRows as RowGroup, horStyles[1]).map(grh1 =>
                    <React.Fragment key={horCols[1].getKey(grh1?.value)}>
                      {getValues(grh1?.groupOrRows as RowGroup, horStyles[2]).map(grh2 =>
                        <Cell key={horCols[2].getKey(grh2?.value)} colSpan={span(grh2?.groupOrRows, horStyles, 2)} style={horStyles[2]}
                          gor={grh2?.groupOrRows} title={grh2 == null ? "": horCols[2].getNiceName(grh2.value)} filters={ grh2 == null ? [] : [
                            { col: horCols[0], val: grh0!.value },
                            { col: horCols[1], val: grh1!.value },
                            { col: horCols[2], val: grh2!.value }]} />
                      )}
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
              <Cell style={undefined}
                gor={verticalGroups} title={valueColumn.displayName} filters={[]} />
              {cells(verticalGroups as ChartRow[], [])}
            </tr> :
            vertCols.length == 1 ?
              getValues(verticalGroups as RowGroup, vertStyles[0]).map((grv0, i) =>
                <tr key={vertCols[0].getKey(grv0!.value)}>
                  <Cell style={vertStyles[0]}
                    gor={grv0!.groupOrRows} title={vertCols[0].getNiceName(grv0!.value)} filters={[{ col: vertCols[0], val: grv0!.value }]} />
                  {cells(grv0!.groupOrRows as ChartRow[], [{ col: vertCols[0], val: grv0!.value }])}
                </tr>
              ) :
              vertCols.length == 2 ?
                getValues(verticalGroups as RowGroup, vertStyles[0]).map((grv0, i) =>
                  <React.Fragment key={vertCols[0].getKey(grv0!.value)}>
                    {getValues(grv0!.groupOrRows as RowGroup, vertStyles[1]).map((grv1, j) =>
                      <tr key={vertCols[1].getKey(grv1?.value)}>
                        {j == 0 &&
                          <Cell style={vertStyles[0]} rowSpan={span(grv0!.groupOrRows, vertStyles, 0)}
                            gor={grv0!.groupOrRows} title={vertCols[0].getNiceName(grv0!.value)} filters={[
                              { col: vertCols[0], val: grv0!.value }
                            ]} />
                        }
                        <Cell style={vertStyles[1]}
                          gor={grv1?.groupOrRows} title={grv1 == null ? "" : vertCols[1].getNiceName(grv1.value)} filters={grv1 == null ? [] : [
                            { col: vertCols[0], val: grv0!.value },
                            { col: vertCols[1], val: grv1!.value }
                          ]} />
                        {cells(grv1?.groupOrRows as ChartRow[] ?? [], grv1 == null ? [] : [
                          { col: vertCols[0], val: grv0!.value },
                          { col: vertCols[1], val: grv1!.value }
                        ])}
                      </tr>
                    )}
                  </React.Fragment>) :
                vertCols.length == 3 ?
                  getValues(verticalGroups as RowGroup, vertStyles[0]).map((grv0, i) =>
                    <React.Fragment key={vertCols[0].getKey(grv0!.value)}>
                      {getValues(grv0!.groupOrRows as RowGroup, vertStyles[1]).map((grv1, j) =>
                        <React.Fragment key={vertCols[1].getKey(grv1?.value)}>
                          {getValues(grv1?.groupOrRows as RowGroup, vertStyles[2]).map((grv2, k) =>
                            <tr key={vertCols[2].getKey(grv2?.value)}>
                              {j == 0 && k == 0 &&
                                <Cell style={vertStyles[0]} rowSpan={span(grv0!.groupOrRows, vertStyles, 0)}
                                  gor={grv0!.groupOrRows} title={vertCols[0].getNiceName(grv0!.value)} filters={[
                                    { col: vertCols[0], val: grv0!.value }
                                  ]} />
                              }
                              {k == 0 &&
                                <Cell style={vertStyles[1]} rowSpan={span(grv1?.groupOrRows, vertStyles, 1)}
                                  gor={grv1?.groupOrRows} title={grv1 == null ? "" : vertCols[1].getNiceName(grv1.value)} filters={grv1 == null ? [] : [
                                    { col: vertCols[0], val: grv0!.value },
                                    { col: vertCols[1], val: grv1.value }
                                  ]} />
                              }
                              <th scope="col">
                                <Cell style={vertStyles[2]}
                                  gor={grv2?.groupOrRows} title={grv2 == null ? "" : vertCols[2].getNiceName(grv2.value)} filters={grv2 == null ? [] : [
                                    { col: vertCols[0], val: grv0!.value },
                                    { col: vertCols[1], val: grv1!.value },
                                    { col: vertCols[2], val: grv2!.value },
                                  ]} />
                              </th>
                              {cells(grv2?.groupOrRows as ChartRow[] ?? [], grv2 == null ? [] : [
                                { col: vertCols[0], val: grv0!.value },
                                { col: vertCols[1], val: grv1!.value },
                                { col: vertCols[2], val: grv2!.value },
                              ])}
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

  function cells(rows: ChartRow[], filters: { col: ChartColumn<unknown>, val: unknown }[]) {

    const gor = multiGroups(rows, horCols);

    return (
      horCols.length == 0 ?
        <Cell gor={gor} style={valueStyle} filters={filters} /> :
        horCols.length == 1 ?
          getValues(horizontalGroups as RowGroup, horStyles[0]).map(grh0 => {
            const grh0key = horCols[0].getKey(grh0!.value);
            const gor0 = (gor as RowGroup)[grh0key]?.groupOrRows;
            return (
              <Cell key={grh0key} style={valueStyle}
                gor={gor0} filters={[...filters, { col: horCols[0], val: grh0!.value }]} />
            );
          }) :
          horCols.length == 2 ?
            getValues(horizontalGroups as RowGroup, horStyles[0]).map(grh0 => {
              const grh0key = horCols[0].getKey(grh0!.value);
              const gor0 = (gor as RowGroup)[grh0key]?.groupOrRows;
              return (
                <React.Fragment key={grh0key}>
                  {getValues(grh0!.groupOrRows as RowGroup, horStyles[1]).map(grh1 => {
                    const grh1key = horCols[1].getKey(grh1?.value);
                    const gor1 = gor0 && (gor0 as RowGroup)[grh1key]?.groupOrRows
                    return (
                      <Cell key={grh1key} style={valueStyle}
                        gor={gor1} filters={grh1 == null ? [] : [
                          ...filters,
                          { col: horCols[0], val: grh0!.value },
                          { col: horCols[1], val: grh1.value },
                        ]} />
                    );
                  })}
                </React.Fragment>
              );
            }) :
            horCols.length == 3 ?
              getValues(horizontalGroups as RowGroup, horStyles[0]).map(grh0 => {
                const grh0key = horCols[0].getKey(grh0!.value);
                const gor0 = (gor as RowGroup)[grh0key]?.groupOrRows;
                return (
                  <React.Fragment key={grh0key}>
                    {getValues(grh0!.groupOrRows as RowGroup, horStyles[1]).map(grh1 => {
                      const grh1key = horCols[1].getKey(grh1?.value);
                      const gor1 = gor0 && (gor0 as RowGroup)[grh1key]?.groupOrRows
                      return (
                        <React.Fragment key={grh1key}>
                          {getValues(grh1?.groupOrRows as RowGroup, horStyles[2]).map(grh2 => {
                            const grh2key = horCols[2].getKey(grh2?.value);
                            const gor2 = gor1 && (gor1 as RowGroup)[grh2key]?.groupOrRows
                            return (
                              <Cell key={grh2key} style={valueStyle}
                                gor={gor2} filters={grh2 == null ? [] : [
                                  ...filters,
                                  { col: horCols[0], val: grh0!.value },
                                  { col: horCols[1], val: grh1!.value },
                                  { col: horCols[2], val: grh2.value },
                                ]} />
                            );
                          })}
                        </React.Fragment>
                      );
                    })}
                  </React.Fragment>
                );
              }) :
              null
    );
  }
}

