import * as React from 'react'
import { ValueLine } from '@framework/Lines'
import { TypeContext } from '@framework/TypeContext'
import { PredictorRegressionMetricsEmbedded, PredictorEntity } from '../Signum.Entities.MachineLearning'

export default function PredictorRegressionMetrics(p : { ctx: TypeContext<PredictorEntity> }){

  function renderRow(ctx: TypeContext<PredictorEntity>, property: (val: PredictorRegressionMetricsEmbedded) => number | null | undefined) {
    const ctxT = ctx.subCtx(a => a.regressionTraining!);
    const ctxV = ctx.subCtx(a => a.regressionValidation!);
    var unit = ctxT.subCtx(property).propertyRoute!.member!.unit;

    return (
      <tr>
        <th>{ctxT.niceName(property)}{unit && " (" + unit + ")"}</th>
        <td><ValueLine ctx={ctxT.subCtx(property)} unit="" /></td>
        <td><ValueLine ctx={ctxV.subCtx(property)} unit="" /></td>
      </tr>
    );
  }
  const ctx = p.ctx.subCtx({ formGroupStyle: "SrOnly" });

  return (
    <fieldset>
      <legend>Regression</legend>
      <table className="table table-sm" style={{ width: "initial" }}>
        <thead>
          <tr>
            <th></th>
            <th>Training</th>
            <th>Validation</th>
          </tr>
        </thead>
        <tbody>
          {renderRow(ctx, a => a.meanError)}
          {renderRow(ctx, a => a.meanAbsoluteError)}
          {renderRow(ctx, a => a.meanSquaredError)}
          {renderRow(ctx, a => a.rootMeanSquareError)}
          {renderRow(ctx, a => a.meanPercentageError)}
          {renderRow(ctx, a => a.meanAbsolutePercentageError)}
        </tbody>
      </table>
    </fieldset>
  );
}
