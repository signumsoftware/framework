import * as React from 'react'
import { ValueLine } from '@framework/Lines'
import { TypeContext } from '@framework/TypeContext'
import { PredictorMetricsEmbedded, PredictorEntity } from '../Signum.Entities.MachineLearning'

export default class PredictorRegressionMetrics extends React.Component<{ ctx: TypeContext<PredictorEntity> }> {

    render() {
        const ctx = this.props.ctx.subCtx({ formGroupStyle: "SrOnly" });


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
                        {this.renderRow(ctx, a => a.loss)}
                        {this.renderRow(ctx, a => a.evaluation)}
                    </tbody>
                </table>
            </fieldset>
        );
    }

    renderRow(ctx: TypeContext<PredictorEntity>, property: (val: PredictorMetricsEmbedded) => number | null | undefined) {
        const ctxT = ctx.subCtx(a => a.resultTraining!);
        const ctxV = ctx.subCtx(a => a.resultValidation!);
        var unit = ctxT.subCtx(property).propertyRoute.member!.unit;

        return (
            <tr>
                <th>{ctxT.niceName(property)}{unit && " (" + unit + ")"}</th>
                <td><ValueLine ctx={ctxT.subCtx(property)} unitText="" /></td>
                <td><ValueLine ctx={ctxV.subCtx(property)} unitText="" /></td>
            </tr>
        );
    }
}
