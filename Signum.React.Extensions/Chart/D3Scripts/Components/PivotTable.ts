import * as d3 from "d3";
import { ChartTable, ChartColumn, ChartRow } from "../../ChartClient";

export function toPivotTable(data: ChartTable,
  col0: ChartColumn<unknown>, /*Employee*/
  usedCols: ChartColumn<number>[]): PivotTable {

  var rows = data.rows
    .map((r) => ({
      rowValue: col0.getValue(r),
      values: usedCols.toObject(cn => cn.name, (cn): PivotValue => ({
        rowClick: r,
        value: cn.getValue(r),
        valueNiceName: cn.getValueNiceName(r),
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
            rowClick: r,
            value: colValue.getValue(r),
            valueNiceName: colValue.getValueNiceName(r),
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
  valueNiceName: string;
  valueTitle: string;
}
