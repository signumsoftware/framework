import * as React from 'react'
import { classes } from '../../../../Framework/Signum.React/Scripts/Globals'
import { FormGroup, FormControlReadonly, ValueLine, ValueLineType, EntityLine, EntityCombo, EntityList, EntityRepeater, EntityTable } from '../../../../Framework/Signum.React/Scripts/Lines'
import { SearchControl } from '../../../../Framework/Signum.React/Scripts/Search'
import { TypeContext, FormGroupStyle } from '../../../../Framework/Signum.React/Scripts/TypeContext'
import FileLine from '../../Files/FileLine'
import { PredictorRegressionMetricsEmbedded, PredictorEntity } from '../Signum.Entities.MachineLearning'
import * as Finder from '../../../../Framework/Signum.React/Scripts/Finder'
import { getQueryNiceName } from '../../../../Framework/Signum.React/Scripts/Reflection'
import QueryTokenEntityBuilder from '../../UserAssets/Templates/QueryTokenEntityBuilder'
import { QueryTokenEmbedded } from '../../UserAssets/Signum.Entities.UserAssets'
import { QueryFilterEmbedded } from '../../UserQueries/Signum.Entities.UserQueries'
import { QueryDescription, SubTokensOptions } from '../../../../Framework/Signum.React/Scripts/FindOptions'
import { API } from '../PredictorClient';
import FilterBuilderEmbedded from './FilterBuilderEmbedded';
import { TypeReference } from '../../../../Framework/Signum.React/Scripts/Reflection';

export default class PredictorRegressionMetrics extends React.Component<{ ctx: TypeContext<PredictorEntity> }> {

    render() {
        const ctx = this.props.ctx.subCtx({ formGroupStyle: "SrOnly" });


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
                        {this.renderRow(ctx, a => a.meanError)}
                        {this.renderRow(ctx, a => a.meanAbsoluteError)}
                        {this.renderRow(ctx, a => a.meanSquaredError)}
                        {this.renderRow(ctx, a => a.rootMeanSquareError)}
                        {this.renderRow(ctx, a => a.meanPercentageError)}
                        {this.renderRow(ctx, a => a.meanAbsolutePercentageError)}
                    </tbody>
                </table>
            </fieldset>
        );
    }

    renderRow(ctx: TypeContext<PredictorEntity>, property: (val: PredictorRegressionMetricsEmbedded) => number | null | undefined) {
        const ctxT = ctx.subCtx(a => a.regressionTraining!);
        const ctxV = ctx.subCtx(a => a.regressionValidation!);
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
