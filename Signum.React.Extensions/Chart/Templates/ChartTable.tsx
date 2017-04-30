import * as React from 'react'
import * as Finder from '../../../../Framework/Signum.React/Scripts/Finder'
import { ResultTable, FindOptions, FilterOption, QueryDescription, SubTokensOptions, QueryToken, QueryTokenType, ColumnOptionParsed, OrderOptionParsed, OrderType } from '../../../../Framework/Signum.React/Scripts/FindOptions'
import { ChartColumnEmbedded, ChartScriptColumnEmbedded, ChartScriptParameterEmbedded, ChartRequest, GroupByChart, ChartMessage,
   ChartColorEntity, ChartScriptEntity, ChartParameterEmbedded, ChartParameterType } from '../Signum.Entities.Chart'

export default class ChartTable extends React.Component<{ resultTable: ResultTable, chartRequest: ChartRequest, onRedraw: () => void }, void> {


    handleHeaderClick = (e: React.MouseEvent<any>) => {

        const tokenStr = (e.currentTarget as HTMLElement).getAttribute("data-column-name");

        const cr = this.props.chartRequest;

        const prev = cr.orderOptions.filter(a => a.token.fullKey == tokenStr).firstOrNull();

        if (prev != undefined) {
            prev.orderType = prev.orderType == "Ascending" as OrderType ? "Descending" : "Ascending";
            if (!e.shiftKey)
                cr.orderOptions = [prev];

        } else {

            const token = cr.columns.map(mle => mle.element.token!).filter(t => t && t.token!.fullKey == tokenStr).first("Column");

            const newOrder: OrderOptionParsed = { token: token.token!, orderType: "Ascending" };

            if (e.shiftKey)
                cr.orderOptions.push(newOrder);
            else
                cr.orderOptions = [newOrder];
        }

        this.props.onRedraw();
    }

    render() {

        const resultTable = this.props.resultTable;

        const chartRequest = this.props.chartRequest;
        
        const qs = Finder.getSettings(chartRequest.queryKey);

        const columns = chartRequest.columns.map(c => c.element).filter(cc => cc.token != undefined)
            .map(cc => ({ token: cc.token!.token, columnName: cc.displayName } as ColumnOptionParsed))
            .map(co => ({
                column: co,
                cellFormatter: (qs && qs.formatters && qs.formatters[co.token!.fullKey]) || Finder.formatRules.filter(a => a.isApplicable(co)).last("FormatRules").formatter(co),
                resultIndex: resultTable.columns.indexOf(co.token!.fullKey)
            }));

        return (
            <table className="sf-search-results table table-hover table-condensed">
                <thead>
                    <tr>
                        { !chartRequest.groupResults && <th></th> }
                        { columns.map((col, i) =>
                            <th key={i}  data-column-name={col.column.token!.fullKey}
                                onClick={this.handleHeaderClick}>
                                <span className={"sf-header-sort " + this.orderClassName(col.column)}/>
                                <span> {col.column.displayName || col.column.token!.niceName}</span>
                            </th>)}
                    </tr>
                </thead>
                <tbody>
                    {
                        resultTable.rows.map((row, i) =>
                            <tr key={i}>
                                {!chartRequest.groupResults && <td>{((qs && qs.entityFormatter) || Finder.entityFormatRules.filter(a => a.isApplicable(row)).last("EntityFormatRules").formatter)(row, resultTable.columns, undefined)}</td>}
                                {columns.map((c, j) =>
                                    <td key={j} className={c.cellFormatter && c.cellFormatter.cellClass}>
                                        {c.resultIndex == -1 || c.cellFormatter == undefined ? undefined : c.cellFormatter.formatter(row.columns[c.resultIndex]) }
                                    </td>)
                                }
                            </tr>
                        )
                    }
                </tbody>
            </table>

        );
    }

    orderClassName(column: ColumnOptionParsed) {

        if (column.token == undefined)
            return "";

        const orders = this.props.chartRequest.orderOptions;

        const o = orders.filter(a => a.token.fullKey == column.token!.fullKey).firstOrNull();
        if (o == undefined)
            return "";

        let asc = (o.orderType == "Ascending" as OrderType ? "asc" : "desc");

        if (orders.indexOf(o))
            asc += " l" + orders.indexOf(o);

        return asc;
    }
}




