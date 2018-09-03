import * as React from 'react'
import * as Finder from '@framework/Finder'
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome'
import { PredictorEntity, PredictSimpleResultEntity, DefaultColumnEncodings } from '../Signum.Entities.MachineLearning';
import * as ChartClient from '../../Chart/ChartClient'
import { TypeContext } from '@framework/Lines';
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