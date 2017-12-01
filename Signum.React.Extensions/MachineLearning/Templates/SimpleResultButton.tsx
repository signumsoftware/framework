import * as React from 'react'
import { BsStyle } from '../../../../Framework/Signum.React/Scripts/Operations';
import { classes } from '../../../../Framework/Signum.React/Scripts/Globals'
import * as Finder from '../../../../Framework/Signum.React/Scripts/Finder'
import * as numbro from 'numbro';
import { PredictorEntity, PredictSimpleResultEntity, PredictorColumnEmbedded } from '../Signum.Entities.MachineLearning';
import * as ChartClient from '../../Chart/ChartClient'
import { ChartRequest } from '../../Chart/Signum.Entities.Chart'
import { SubTokensOptions } from '../../../../Framework/Signum.React/Scripts/FindOptions';
import FilterBuilderEmbedded from './FilterBuilderEmbedded';
import { toQueryTokenEmbedded } from '../../UserAssets/UserAssetClient';
import { TypeReference } from '../../../../Framework/Signum.React/Scripts/Reflection';
import { TypeContext } from '../../../../Framework/Signum.React/Scripts/Lines';
import { FilterOptionParsed, FilterOption } from '../../../../Framework/Signum.React/Scripts/Search';
import { ChartOptions } from '../../Chart/ChartClient';
import { QueryToken } from '../../../../Framework/Signum.React/Scripts/FindOptions';

interface SimpleResultButtonProps {
    ctx: TypeContext<PredictorEntity>;
}

export default class SimpleResultButton extends React.Component<SimpleResultButtonProps> {

    render() {
        const p = this.props.ctx.value;

        var col = p.mainQuery.columns.single(a => a.element.usage == "Output");

        return (
            <div>
                <a href="#" className="btn btn-sm btn-info" onClick={this.handleOnClick} >
                    <i className="fa fa-line-chart" />&nbsp;
                    {col.element.encoding == "OneHot" ? "Confusion matrix" : "Regression Scatterplot"}
                </a>
            </div>
        );
    }

    handleOnClick = (e: React.MouseEvent<any>) => {
        e.preventDefault();

        this.getChartUrl()
            .then(url => window.open(url))
            .done();
    }

    async getChartUrl(): Promise<string> {
        const predictor = this.props.ctx.value;

        var outCol = predictor.mainQuery.columns.single(a => a.element.usage == "Output").element;
        var outToken = outCol.token!.token!;

        var qdb = await Finder.getQueryDescription(predictor.mainQuery.query!.key);

        function appendToken(token: QueryToken) {
            return `Target.(${(qdb.columns["Entity"].type.name)}).` + (token.fullKey.startsWith("Entity.") ? token.fullKey.after("Entity.") : token.fullKey);
        }

        var filterOptions = predictor.mainQuery.filters.map(fo => ({
            columnName: appendToken(fo.element.token!.token!),
            operation: fo.element.operation,
            value: fo.element.valueString
        }) as FilterOption);

        if (isCategorical(outCol))
            return ChartClient.Encoder.chartPath({
                queryName: PredictSimpleResultEntity,
                filterOptions: filterOptions,
                chartScript: "Punchcard",
                columnOptions: [
                    { columnName: appendToken(outToken) },
                    { columnName: "PredictedCategory", displayName: "Predicted " + outToken.niceName },
                    { columnName: "Count" },
                ],
            });
        else
            return ChartClient.Encoder.chartPath({
                queryName: PredictSimpleResultEntity,
                filterOptions: filterOptions,
                chartScript: "Scatterplot",
                columnOptions: [
                    { columnName: "Type" },
                    { columnName: appendToken(outToken) },
                    { columnName: "PredictedValue", displayName: "Predicted " + outToken.niceName },
                ],
            });
    }
}

function isCategorical(column: PredictorColumnEmbedded) {
    return column.encoding == "Codified" || column.encoding == "OneHot";
}