import * as React from 'react'
import { classes } from '@framework/Globals'
import * as Finder from '@framework/Finder'
import * as numbro from 'numbro';
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome'
import { PredictorEntity, PredictSimpleResultEntity, PredictorColumnEmbedded, DefaultColumnEncodings } from '../Signum.Entities.MachineLearning';
import * as ChartClient from '../../Chart/ChartClient'
import { ChartRequest } from '../../Chart/Signum.Entities.Chart'
import { SubTokensOptions } from '@framework/FindOptions';
import FilterBuilderEmbedded from './FilterBuilderEmbedded';
import { toQueryTokenEmbedded } from '../../UserAssets/UserAssetClient';
import { TypeReference } from '@framework/Reflection';
import { TypeContext } from '@framework/Lines';
import { FilterOptionParsed, FilterOption } from '@framework/Search';
import { ChartOptions } from '../../Chart/ChartClient';
import { QueryToken } from '@framework/FindOptions';
import { is } from '@framework/Signum.Entities';

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
                    <FontAwesomeIcon icon="chart-line"/>&nbsp;
                    {is(col.element.encoding, DefaultColumnEncodings.OneHot) ? "Confusion matrix" : "Regression Scatterplot"}
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

        if (is(outCol.encoding, DefaultColumnEncodings.OneHot))
            return ChartClient.Encoder.chartPath({
                queryName: PredictSimpleResultEntity,
                filterOptions: [{ token: "Predictor", value: predictor }],
                chartScript: "Punchcard",
                columnOptions: [
                    { token: "OriginalCategory", displayName: "Original " + outToken.niceName },
                    { token: "PredictedCategory", displayName: "Predicted " + outToken.niceName },
                    { token: "Count" },
                ],
            });
        else
            return ChartClient.Encoder.chartPath({
                queryName: PredictSimpleResultEntity,
                filterOptions: [{ token: "Predictor", value: predictor }],
                chartScript: "Scatterplot",
                columnOptions: [
                    { token: "Type" },
                    { token: "OriginalValue", displayName: "Original " + outToken.niceName },
                    { token: "PredictedValue", displayName: "Predicted " + outToken.niceName },
                ],
            });
    }
}