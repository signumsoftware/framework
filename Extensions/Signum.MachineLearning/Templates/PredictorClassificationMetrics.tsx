import * as React from 'react'
import { AutoLine } from '@framework/Lines'
import { TypeContext } from '@framework/TypeContext'
import { PredictorClassificationMetricsEmbedded, PredictorEntity } from '../Signum.MachineLearning'

export default function PredictorClassificationMetrics(p : { ctx: TypeContext<PredictorEntity> }){

  function renderRow(ctx: TypeContext<PredictorEntity>, property: (val: PredictorClassificationMetricsEmbedded) => number | null | undefined) {
    const ctxT = ctx.subCtx(a => a.classificationTraining!);
    const ctxV = ctx.subCtx(a => a.classificationValidation!);

    return (
      <tr>
        <th>{ctxT.niceName(property)}</th>
        <td><AutoLine ctx={ctxT.subCtx(property)} /></td>
        <td><AutoLine ctx={ctxV.subCtx(property)} /></td>
      </tr>
    );
  }
  const ctx = p.ctx.subCtx({ formGroupStyle: "SrOnly" });

  return (
    <fieldset>
      <legend>Classification</legend>
      <table className="table table-sm">
        <thead>
          <tr>
            <th></th>
            <th>Training</th>
            <th>Validation</th>
          </tr>
        </thead>
        <tbody>
          {renderRow(ctx, a => a.totalCount)}
          {renderRow(ctx, a => a.missCount)}
          {renderRow(ctx, a => a.missRate)}
        </tbody>
      </table>
    </fieldset>
  );
}
