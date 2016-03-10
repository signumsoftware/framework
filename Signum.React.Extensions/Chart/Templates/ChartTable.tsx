import * as React from 'react'
import * as Finder from '../../../../Framework/Signum.React/Scripts/Finder'
import { ResultTable, FindOptions, FilterOption, QueryDescription, SubTokensOptions, QueryToken, QueryTokenType, ColumnOption } from '../../../../Framework/Signum.React/Scripts/FindOptions'
import { ChartColumnEntity, ChartScriptColumnEntity, ChartScriptParameterEntity, ChartRequest, GroupByChart, ChartMessage,
   ChartColorEntity_Type, ChartScriptEntity, ChartScriptEntity_Type, ChartParameterEntity, ChartParameterType } from '../Signum.Entities.Chart'

export default class ChartTable extends React.Component<{ resultTable: ResultTable, chartRequest: ChartRequest}, void> {

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
                        { columns.map((col, i) => <th key={i}>{col.column.displayName || col.column.token.niceName}</th>) }
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


    
}




