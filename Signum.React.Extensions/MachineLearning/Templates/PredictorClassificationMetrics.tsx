import * as React from 'react'
import { classes } from '@framework/Globals'
import { FormGroup, FormControlReadonly, ValueLine, ValueLineType, EntityLine, EntityCombo, EntityList, EntityRepeater, EntityTable } from '@framework/Lines'
import { SearchControl } from '@framework/Search'
import { TypeContext, FormGroupStyle } from '@framework/TypeContext'
import FileLine from '../../Files/FileLine'
import { PredictorClassificationMetricsEmbedded, PredictorEntity } from '../Signum.Entities.MachineLearning'
import * as Finder from '@framework/Finder'
import { getQueryNiceName } from '@framework/Reflection'
import QueryTokenEntityBuilder from '../../UserAssets/Templates/QueryTokenEntityBuilder'
import { QueryTokenEmbedded } from '../../UserAssets/Signum.Entities.UserAssets'
import { QueryFilterEmbedded } from '../../UserQueries/Signum.Entities.UserQueries'
import { QueryDescription, SubTokensOptions } from '@framework/FindOptions'
import { API } from '../PredictorClient';
import FilterBuilderEmbedded from './FilterBuilderEmbedded';
import { TypeReference } from '@framework/Reflection';

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
