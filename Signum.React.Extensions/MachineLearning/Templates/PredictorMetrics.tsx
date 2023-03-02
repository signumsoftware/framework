import * as React from 'react'
import { ValueLine } from '@framework/Lines'
import { TypeContext } from '@framework/TypeContext'
import { PredictorMetricsEmbedded, PredictorEntity } from '../Signum.Entities.MachineLearning'

export default function PredictorRegressionMetrics(p : { ctx: TypeContext<PredictorEntity> }){

  function renderRow(ctx: TypeContext<PredictorEntity>, property: (val: PredictorMetricsEmbedded) => number | null | undefined) {
    const ctxT = ctx.subCtx(a => a.resultTraining!);
    const ctxV = ctx.subCtx(a => a.resultValidation!);
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
      <legend>Last results</legend>
      <table className="table table-sm" style={{ width: "initial" }}>
        <thead>
          <tr>
            <th></th>
            <th>Training</th>
            <th>Validation</th>
          </tr>
        </thead>
        <tbody>
          {renderRow(ctx, a => a.loss)}
          {renderRow(ctx, a => a.accuracy)}
        </tbody>
      </table>
    </fieldset>
  );
}
