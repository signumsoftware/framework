import * as React from 'react'
import * as Finder from '../../../../Framework/Signum.React/Scripts/Finder'
import { ResultTable, FindOptions, FilterOption, QueryDescription, SubTokensOptions, QueryToken, QueryTokenType, ColumnOption, OrderOption, OrderType } from '../../../../Framework/Signum.React/Scripts/FindOptions'
import { ChartColumnEntity, ChartScriptColumnEntity, ChartScriptParameterEntity, ChartRequest, GroupByChart, ChartMessage,
   ChartColorEntity, ChartScriptEntity, ChartParameterEntity, ChartParameterType } from '../Signum.Entities.Chart'

export default class ChartTable extends React.Component<{ resultTable: ResultTable, chartRequest: ChartRequest, onRedraw: () => void }, void> {


    handleHeaderClick = (e: React.MouseEvent) => {

        const tokenStr = (e.currentTarget as HTMLElement).getAttribute("data-column-name");

        var cr = this.props.chartRequest;

        const prev = cr.orderOptions.filter(a => a.token.fullKey == tokenStr).firstOrNull();

        if (prev != null) {
            prev.orderType = prev.orderType == "Ascending" as OrderType ? "Descending" : "Ascending";
            if (!e.shiftKey)
                cr.orderOptions = [prev];

        } else {

            const token = cr.columns.map(mle => mle.element.token).filter(t => t && t.token.fullKey == tokenStr).first("Column");

            const newOrder: OrderOption = { token: token.token, orderType: "Ascending", columnName: token.token.fullKey };

            if (e.shiftKey)
                cr.orderOptions.push(newOrder);
            else
                cr.orderOptions = [newOrder];
        }

        this.props.onRedraw();
    }

    render() {

        var resultTable = this.props.resultTable;

        var chartRequest = this.props.chartRequest;
        
        var qs = Finder.getQuerySettings(chartRequest.queryKey);

        const columns = chartRequest.columns.map(c => c.element).filter(cc => cc.token != null)
            .map(cc => ({ token: cc.token.token, columnName: cc.displayName } as ColumnOption))
            .map(co => ({
                column: co,
                cellFormatter: (qs && qs.formatters && qs.formatters[co.token.fullKey]) || Finder.formatRules.filter(a => a.isApplicable(co)).last("FormatRules").formatter(co),
                resultIndex: resultTable.columns.indexOf(co.token.fullKey)
            }));

        return (
            <table className="sf-search-results table table-hover table-condensed">
                <thead>
                    <tr>
                        { !chartRequest.groupResults && <th></th> }
                        { columns.map((col, i) =>
                            <th key={i}  data-column-name={col.column.token.fullKey}
                                onClick={this.handleHeaderClick}>
                                <span className={"sf-header-sort " + this.orderClassName(col.column) }/>
                                <span> { col.column.displayName || col.column.token.niceName }</span>
                            </th>) }
                    </tr>
                </thead>
                <tbody>
                    {
                        resultTable.rows.map((row, i) =>
                            <tr key={i}>
                                { !chartRequest.groupResults && <td>{ ((qs && qs.entityFormatter) || Finder.entityFormatRules.filter(a => a.isApplicable(row)).last("EntityFormatRules").formatter)(row) }</td> }
                                { columns.map((c, j) =>
                                    <td key={j} style={{ textAlign: c.cellFormatter && c.cellFormatter.textAllign }}>
                                        {c.resultIndex == -1 || c.cellFormatter == null ? null : c.cellFormatter.formatter(row.columns[c.resultIndex]) }
                                    </td>)
                                }
                            </tr>
                        )
                    }
                </tbody>
            </table>

        );
    }

    orderClassName(column: ColumnOption) {

        if (column.token == null)
            return "";

        const orders = this.props.chartRequest.orderOptions;

        const o = orders.filter(a => a.token.fullKey == column.token.fullKey).firstOrNull();
        if (o == null)
            return "";

        let asc = (o.orderType == "Ascending" as OrderType ? "asc" : "desc");

        if (orders.indexOf(o))
            asc += " l" + orders.indexOf(o);

        return asc;
    }
}




