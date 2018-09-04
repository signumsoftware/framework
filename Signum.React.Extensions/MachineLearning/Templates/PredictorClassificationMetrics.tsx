import * as React from 'react'
import { ValueLine } from '@framework/Lines'
import { TypeContext } from '@framework/TypeContext'
import { PredictorClassificationMetricsEmbedded, PredictorEntity } from '../Signum.Entities.MachineLearning'

export default class PredictorClassificationMetrics extends React.Component<{ ctx: TypeContext<PredictorEntity> }> {

    render() {
        const ctx = this.props.ctx.subCtx({ formGroupStyle: "SrOnly" });
        
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
                        {this.renderRow(ctx, a => a.totalCount)}
                        {this.renderRow(ctx, a => a.missCount)}
                        {this.renderRow(ctx, a => a.missRate)}
                    </tbody>
                </table>
            </fieldset>
        );
    }

    renderRow(ctx: TypeContext<PredictorEntity>, property: (val: PredictorClassificationMetricsEmbedded) => number | null | undefined) {
        const ctxT = ctx.subCtx(a => a.classificationTraining!);
        const ctxV = ctx.subCtx(a => a.classificationValidation!);

        return (
            <tr>
                <th>{ctxT.niceName(property)}</th>
                <td><ValueLine ctx={ctxT.subCtx(property)} /></td>
                <td><ValueLine ctx={ctxV.subCtx(property)} /></td>
            </tr>
        );
    }
}
