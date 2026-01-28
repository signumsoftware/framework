import * as React from 'react'
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome'
import { PredictorEntity, PredictSimpleResultEntity, DefaultColumnEncodings } from '../Signum.MachineLearning';
import { ChartClient } from '../../Signum.Chart/ChartClient'
import { TypeContext } from '@framework/Lines';
import { is } from '@framework/Signum.Entities';
import { QueryTokenString } from '@framework/Reflection';
import { toAbsoluteUrl } from '@framework/AppContext';
import { LinkButton } from '@framework/Basics/LinkButton';

interface SimpleResultButtonProps {
  ctx: TypeContext<PredictorEntity>;
}

export default function SimpleResultButton(p : SimpleResultButtonProps): React.JSX.Element {

  function handleOnClick(e: React.MouseEvent<any>) {
    window.open(toAbsoluteUrl(getChartUrl()));
  }

  function getChartUrl(): string {
    const predictor = p.ctx.value;

    var outCol = predictor.mainQuery.columns.single(a => a.element.usage == "Output").element;
    var outToken = outCol.token!.token!;


    if (is(outCol.encoding, DefaultColumnEncodings.OneHot))
      return ChartClient.Encoder.chartPath({
        queryName: PredictSimpleResultEntity,
        filterOptions: [{ token: PredictSimpleResultEntity.token(e => e.predictor), value: predictor }],
        chartScript: "Punchcard",
        columnOptions: [
          { token: PredictSimpleResultEntity.token(e => e.originalCategory), displayName: "Original " + outToken.niceName },
          { token: PredictSimpleResultEntity.token(e => e.predictedCategory), displayName: "Predicted " + outToken.niceName },
          { token: QueryTokenString.count },
        ],
      });
    else
      return ChartClient.Encoder.chartPath({
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
  const pred = p.ctx.value;
  var col = pred.mainQuery.columns.single(a => a.element.usage == "Output");

  return (
    <div>
      <LinkButton title={undefined} className="btn btn-sm btn-info" onClick={handleOnClick} >
        <FontAwesomeIcon icon="chart-line" />&nbsp;
        {is(col.element.encoding, DefaultColumnEncodings.OneHot) ? "Confusion matrix" : "Regression Scatterplot"}
      </LinkButton>
    </div>
  );
}
