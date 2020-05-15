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
}

interface DimParameters {
  gradient: string,
  scale: string,
  textAlign: TextAlignProperty,
  verticalAlign: VerticalAlignProperty<string | number>
}


export default function renderPivotTable({ data, width, height, parameters, loading, onDrillDown, initialLoad }: ChartClient.ChartScriptProps): React.ReactElement<any> {

  if (data == null || data.rows.length == 0)
    return (
      <svg direction="ltr" width={width} height={height}>
        <InitialMessage data={data} x={width / 2} y={height / 2} loading={loading} />
      </svg>
    );

  function getDimParameters(columnName: string): DimParameters {
    return ({
      gradient: parameters["Gradient " + columnName],
      scale: parameters["Scale " + columnName],
      textAlign: parameters["text-align " + columnName] as TextAlignProperty,
      verticalAlign: parameters["vert-align " + columnName] as VerticalAlignProperty<string | number>,
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

  function sumValue(gor: GroupOrRows): number {
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


  function getCellStyle(values: number[], params: DimParameters): CellStyle {


    let color: ((num: number) => string) | undefined = undefined;
    if (params.scale && params.gradient != "none") {
      const scaleFunc = ChartUtils.scaleFor(valueColumn, values, 0, 1, params.scale);
      const gradient = ChartUtils.getColorInterpolation(params.gradient)!;
      color = (num: number) => gradient(scaleFunc(num));
    }
    return ({
      textAlign: params.textAlign,
      verticalAlign: params.verticalAlign,
      background: color
    }) as CellStyle;

  }

  const horStyles = horColsWitParams.map((cp, i) => getCellStyle(getLevelValues(horizontalGroups as RowGroup, i), cp.params));
  const vertStyles = vertColsWitParams.map((cp, i) => getCellStyle(getLevelValues(verticalGroups as RowGroup, i), cp.params));

  const valueStyle = getCellStyle(data.rows.map(a => valueColumn.getValue(a)), getDimParameters("Values"));

  const numbroFormat = toNumbroFormat(valueColumn.token?.format);

  function span(gor: GroupOrRows): number {
    if (Array.isArray(gor))
      return 1;

    return Dic.getValues(gor).sum(a => span(a.groupOrRows));
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

    const val = p.gor ? sumValue(p.gor) : 0;

    const link = p.gor == null ? null : <a href="#" onClick={e => handleClick(e)}>{numbro(val).format(numbroFormat)}</a>;

    const style: React.CSSProperties | undefined = p.style && {
      backgroundColor: p.style.background && p.style.background(val),
      textAlign: p.style.textAlign as TextAlignProperty,
      verticalAlign: p.style.verticalAlign as VerticalAlignProperty<string | number>,
    };

    if (!p.title) {
      return <td style={style}>{link}</td>;
    }

    return <th style={style} colSpan={p.colSpan} rowSpan={p.rowSpan}><span title={p.title}>{p.title} ({link})</span></th>;
  }

  return (
    <div className="table-responsive">
      <table className="table table-bordered pivot-table">
        <thead>
          <tr>
            <th scope="col" colSpan={Math.max(1, vertCols.length)} rowSpan={Math.max(1, horCols.length)}>#</th>
            {horCols.length == 0 ? <Cell gor={horizontalGroups} filters={[]} title={valueColumn.displayName} style={undefined} /> :
              Dic.getValues(horizontalGroups as RowGroup).map(grh0 =>
                <Cell key={horCols[0].getKey(grh0.value)} colSpan={span(grh0.groupOrRows)} style={horStyles[0]}
                  gor={grh0.groupOrRows} title={horCols[0].getNiceName(grh0.value)} filters={[{ col: horCols[0], val: grh0.value }]} />
              )
            }
          </tr>
          {horCols.length >= 2 &&
            <tr>
              {Dic.getValues(horizontalGroups as RowGroup).map(grh0 =>
                <React.Fragment key={horCols[0].getKey(grh0.value)} >
                  {Dic.getValues(grh0.groupOrRows as RowGroup).map(grh1 =>
                    <Cell key={horCols[1].getKey(grh1.value)} colSpan={span(grh1.groupOrRows)} style={horStyles[1]}
                      gor={grh1.groupOrRows} title={horCols[1].getNiceName(grh1.value)} filters={[
                        { col: horCols[0], val: grh0.value },
                        { col: horCols[1], val: grh1.value }]} />
                  )}
                </React.Fragment>
              )}
            </tr>
          }
          {horCols.length >= 3 &&
            <tr>
              {Dic.getValues(horizontalGroups as RowGroup).map(grh0 =>
                <React.Fragment key={horCols[0].getKey(grh0.value)} >
                  {Dic.getValues(grh0.groupOrRows as RowGroup).map(grh1 =>
                    <React.Fragment key={horCols[1].getKey(grh1.value)}>
                      {Dic.getValues(grh1.groupOrRows as RowGroup).map(grh2 =>
                        <Cell key={horCols[2].getKey(grh2.value)} colSpan={span(grh2.groupOrRows)} style={horStyles[2]}
                          gor={grh2.groupOrRows} title={horCols[2].getNiceName(grh2.value)} filters={[
                            { col: horCols[0], val: grh0.value },
                            { col: horCols[1], val: grh1.value },
                            { col: horCols[2], val: grh2.value }]} />
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
              Dic.getValues(verticalGroups as RowGroup).map((grv0, i) =>
                <tr key={vertCols[0].getKey(grv0.value)}>
                  <Cell style={vertStyles[0]} 
                    gor={grv0.groupOrRows} title={vertCols[0].getNiceName(grv0.value)} filters={[{ col: vertCols[0], val: grv0.value }]} />
                  {cells(grv0.groupOrRows as ChartRow[], [{ col: vertCols[0], val: grv0.value }])}
                </tr>
              ) :
              vertCols.length == 2 ?
                Dic.getValues(verticalGroups as RowGroup).map((grv0, i) =>
                  <React.Fragment key={vertCols[0].getKey(grv0.value)}>
                    {Dic.getValues(grv0.groupOrRows as RowGroup).map((grv1, j) =>
                      <tr key={vertCols[1].getKey(grv1.value)}>
                        {j == 0 &&
                          <Cell style={vertStyles[0]} rowSpan={span(grv0.groupOrRows)}
                            gor={grv0.groupOrRows} title={vertCols[0].getNiceName(grv0.value)} filters={[
                              { col: vertCols[0], val: grv0.value }
                            ]} />
                        }
                        <Cell style={vertStyles[1]}
                          gor={grv1.groupOrRows} title={vertCols[1].getNiceName(grv1.value)} filters={[
                            { col: vertCols[0], val: grv0.value },
                            { col: vertCols[1], val: grv1.value }
                          ]} />
                        {cells(grv1.groupOrRows as ChartRow[], [
                          { col: vertCols[0], val: grv0.value },
                          { col: vertCols[1], val: grv1.value }
                        ])}
                      </tr>
                    )}
                  </React.Fragment>) :
                vertCols.length == 3 ?
                  Dic.getValues(verticalGroups as RowGroup).map((grv0, i) =>
                    <React.Fragment key={vertCols[0].getKey(grv0.value)}>
                      {Dic.getValues(grv0.groupOrRows as RowGroup).map((grv1, j) =>
                        <React.Fragment key={vertCols[1].getKey(grv1.value)}>
                          {Dic.getValues(grv1.groupOrRows as RowGroup).map((grv2, k) =>
                            <tr key={vertCols[2].getKey(grv2.value)}>
                              {j == 0 && k == 0 &&
                                <Cell style={vertStyles[0]} rowSpan={span(grv0.groupOrRows)}
                                  gor={grv0.groupOrRows} title={vertCols[0].getNiceName(grv0.value)} filters={[
                                    { col: vertCols[0], val: grv0.value }
                                  ]} />
                              }
                              {k == 0 &&
                                <Cell style={vertStyles[1]} rowSpan={span(grv1.groupOrRows)}
                                  gor={grv1.groupOrRows} title={vertCols[1].getNiceName(grv1.value)} filters={[
                                    { col: vertCols[0], val: grv0.value },
                                    { col: vertCols[1], val: grv1.value }
                                  ]} />
                              }
                              <th scope="col">
                                <Cell style={vertStyles[2]}
                                  gor={grv2.groupOrRows} title={vertCols[2].getNiceName(grv2.value)} filters={[
                                  { col: vertCols[0], val: grv0.value },
                                  { col: vertCols[1], val: grv1.value },
                                  { col: vertCols[2], val: grv2.value },
                                ]} />
                              </th>
                              {cells(grv2.groupOrRows as ChartRow[], [
                                { col: vertCols[0], val: grv0.value },
                                { col: vertCols[1], val: grv1.value },
                                { col: vertCols[2], val: grv2.value },
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
          Dic.getValues(horizontalGroups as RowGroup).map(grh0 => {
            const grh0key = horCols[0].getKey(grh0.value);
            const gor0 = (gor as RowGroup)[grh0key]?.groupOrRows;
            return (
              <Cell key={grh0key} style={valueStyle}
                gor={gor0} filters={[...filters, { col: horCols[0], val: grh0.value }]} />
            );
          }) :
          horCols.length == 2 ?
            Dic.getValues(horizontalGroups as RowGroup).map(grh0 => {
              const grh0key = horCols[0].getKey(grh0.value);
              const gor0 = (gor as RowGroup)[grh0key]?.groupOrRows;
              return (
                <React.Fragment key={grh0key}>
                  {Dic.getValues(grh0.groupOrRows as RowGroup).map(grh1 => {
                    const grh1key = horCols[1].getKey(grh1.value);
                    const gor1 = gor0 && (gor0 as RowGroup)[grh1key]?.groupOrRows
                    return (
                      <Cell key={grh1key} style={valueStyle}
                        gor={gor1} filters={[
                          ...filters,
                          { col: horCols[0], val: grh0.value },
                          { col: horCols[1], val: grh1.value },
                        ]} />
                    );
                  })}
                </React.Fragment>
              );
            }) :
            horCols.length == 3 ?
              Dic.getValues(horizontalGroups as RowGroup).map(grh0 => {
                const grh0key = horCols[0].getKey(grh0.value);
                const gor0 = (gor as RowGroup)[grh0key]?.groupOrRows;
                return (
                  <React.Fragment key={grh0key}>
                    {Dic.getValues(grh0.groupOrRows as RowGroup).map(grh1 => {
                      const grh1key = horCols[1].getKey(grh1.value);
                      const gor1 = gor0 && (gor0 as RowGroup)[grh1key]?.groupOrRows
                      return (
                        <React.Fragment key={grh1key}>
                          {Dic.getValues(grh1.groupOrRows as RowGroup).map(grh2 => {
                            const grh2key = horCols[2].getKey(grh2.value);
                            const gor2 = gor1 && (gor1 as RowGroup)[grh2key]?.groupOrRows
                            return (
                              <Cell key={grh2key} style={valueStyle}
                                gor={gor2} filters={[
                                  ...filters,
                                  { col: horCols[0], val: grh0.value },
                                  { col: horCols[1], val: grh1.value },
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

