import * as React from 'react'
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome'
import { PredictorEntity, PredictSimpleResultEntity, DefaultColumnEncodings } from '../Signum.Entities.MachineLearning';
import * as ChartClient from '../../Chart/ChartClient'
import { TypeContext } from '@framework/Lines';
import { is } from '@framework/Signum.Entities';
import { QueryTokenString } from '@framework/Reflection';

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
          <FontAwesomeIcon icon="chart-line" />&nbsp;
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
      return await ChartClient.Encoder.chartPath({
        queryName: PredictSimpleResultEntity,
        filterOptions: [{ token: PredictSimpleResultEntity.token(e => e.predictor), value: predictor }],
        chartScript: "Punchcard",
        columnOptions: [
          { token: PredictSimpleResultEntity.token(e => e.originalCategory), displayName: "Original " + outToken.niceName },
          { token: PredictSimpleResultEntity.token(e => e.predictedCategory), displayName: "Predicted " + outToken.niceName },
          { token: QueryTokenString.count() },
        ],
      });
    else
      return await  ChartClient.Encoder.chartPath({
        queryName: PredictSimpleResultEntity,
        filterOptions: [{ token: PredictSimpleResultEntity.token(e => e.predictor), value: predictor }],
        chartScript: "Scatterplot",
        columnOptions: [
          { token: PredictSimpleResultEntity.token(e => e.type) },
          { token: PredictSimpleResultEntity.token(e => e.originalValue), displayName: "Original " + outToken.niceName },
          { token: PredictSimpleResultEntity.token(e => e.predictedValue), displayName: "Predicted " + outToken.niceName },
        ],
      });
  }
}
