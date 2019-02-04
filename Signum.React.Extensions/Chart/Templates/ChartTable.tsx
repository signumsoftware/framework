import * as React from 'react'
import * as Finder from '@framework/Finder'
import * as Navigator from '@framework/Navigator'
import { ResultTable, ColumnOptionParsed, OrderOptionParsed, OrderType, ResultRow, hasAggregate, ColumnOption, FilterOptionParsed, withoutAggregateAndPinned } from '@framework/FindOptions'
import { ChartRequestModel, ChartColumnEmbedded } from '../Signum.Entities.Chart'
import * as ChartClient from '../ChartClient'
import { toFilterOptions } from '@framework/Finder';
import { ChartRow } from '../ChartClient';
import { ChartColumn } from './ChartColumn';

interface ChartTableProps {
  resultTable: ResultTable;
  chartRequest: ChartRequestModel;
  lastChartRequest: ChartRequestModel;
  onOrderChanged: () => void;
}

export default class ChartTableComponent extends React.Component<ChartTableProps> {

  handleHeaderClick = (e: React.MouseEvent<any>, col: ColumnOptionParsed) => {

    var chartCol = this.props.chartRequest.columns.map(mle => mle.element)
      .firstOrNull(a => a.token != null && a.token.token != null && a.token.token.fullKey == col.token!.fullKey);

    if (chartCol) {
      ChartClient.handleOrderColumn(this.props.chartRequest, chartCol, e.shiftKey);
      this.props.onOrderChanged();
    }
  }

  render() {

    const resultTable = this.props.resultTable;

    const chartRequest = this.props.chartRequest;

    const qs = Finder.getSettings(chartRequest.queryKey);

    const columns = chartRequest.columns.map(c => c.element).filter(cc => cc.token != undefined)
      .map(cc => ({ token: cc.token!.token, displayName: cc.displayName } as ColumnOptionParsed))
      .map(co => ({
        column: co,
        cellFormatter: (qs && qs.formatters && qs.formatters[co.token!.fullKey]) || Finder.formatRules.filter(a => a.isApplicable(co, undefined)).last("FormatRules").formatter(co),
        resultIndex: resultTable.columns.indexOf(co.token!.fullKey)
      }));


    const ctx: Finder.CellFormatterContext = {
      refresh: undefined
    }

    var hasEntity = ChartClient.hasAggregates(chartRequest);

    return (
      <table className="sf-search-results table table-hover table-sm">
        <thead>
          <tr>
            {hasEntity && <th></th>}
            {columns.map((col, i) =>
              <th key={i} data-column-name={col.column.token!.fullKey}
                onClick={e=>this.handleHeaderClick(e, col.column)}>
                <span className={"sf-header-sort " + this.orderClassName(col.column)} />
                <span> {col.column.displayName || col.column.token!.niceName}</span>
              </th>)}
          </tr>
        </thead>
        <tbody>
          {
            resultTable.rows.map((row, i) =>
              <tr key={i} onDoubleClick={e => this.handleOnDoubleClick(e, row)}>
                {hasEntity && <td>{((qs && qs.entityFormatter) || Finder.entityFormatRules.filter(a => a.isApplicable(row, undefined)).last("EntityFormatRules").formatter)(row, resultTable.columns, undefined)}</td>}
                {columns.map((c, j) =>
                  <td key={j} className={c.cellFormatter && c.cellFormatter.cellClass}>
                    {c.resultIndex == -1 || c.cellFormatter == undefined ? undefined : c.cellFormatter.formatter(row.columns[c.resultIndex], ctx)}
                  </td>)
                }
              </tr>
            )
          }
        </tbody>
      </table>

    );
  }

  handleOnDoubleClick = (e: React.MouseEvent<HTMLTableRowElement>, row: ResultRow) => {


    const lcr = this.props.lastChartRequest!;

    if (row.entity) {

      window.open(Navigator.navigateRoute(row.entity!));

    } else {

      const filters = lcr.filterOptions.map(f => withoutAggregateAndPinned(f)!).filter(Boolean);
      const columns: ColumnOption[] = [];

      lcr.columns.filter(a => a.element.token).map((a, i) => {

        const t = a.element.token!.token!;

        if (!hasAggregate(t)) {
          filters.push({
            token: t,
            operation: "EqualTo",
            value: row.columns[i],
            frozen: false
          } as FilterOptionParsed);
        }

        if (t.parent != undefined) //Avoid Count and simple Columns that are already added
        {
          var col = t.queryTokenType == "Aggregate" ? t.parent : t

          if (col.parent)
            columns.push({
              token: col.fullKey
            });
        }
      });

      window.open(Finder.findOptionsPath({
        queryName: lcr.queryKey,
        filterOptions: toFilterOptions(filters),
        columnOptions: columns,
      }));
    }
  }

  orderClassName(column: ColumnOptionParsed) {

    if (column.token == undefined)
      return "";

    const columns = this.props.chartRequest.columns;

    const c = columns.filter(a => a.element.token != null && a.element.token!.token!.fullKey == column.token!.fullKey).firstOrNull();
    if (c == undefined || c.element.orderByType == null)
      return "";

    return (c.element.orderByType == "Ascending" ? "asc" : "desc") + (" l" + c.element.orderByIndex);
  }
}




