import * as React from 'react'
import { classes } from '../../../../Framework/Signum.React/Scripts/Globals'
import { FormGroup, FormControlStatic, ValueLine, ValueLineType, EntityLine, EntityCombo, EntityList, EntityRepeater, EntityTable } from '../../../../Framework/Signum.React/Scripts/Lines'
import { SearchControl } from '../../../../Framework/Signum.React/Scripts/Search'
import { TypeContext, FormGroupStyle } from '../../../../Framework/Signum.React/Scripts/TypeContext'
import FileLine from '../../Files/FileLine'
import { PredictorRegressionMetricsEmbedded } from '../Signum.Entities.MachineLearning'
import * as Finder from '../../../../Framework/Signum.React/Scripts/Finder'
import { getQueryNiceName } from '../../../../Framework/Signum.React/Scripts/Reflection'
import QueryTokenEntityBuilder from '../../UserAssets/Templates/QueryTokenEntityBuilder'
import { QueryTokenEmbedded } from '../../UserAssets/Signum.Entities.UserAssets'
import { QueryFilterEmbedded } from '../../UserQueries/Signum.Entities.UserQueries'
import { QueryDescription, SubTokensOptions } from '../../../../Framework/Signum.React/Scripts/FindOptions'
import { API } from '../PredictorClient';
import FilterBuilderEmbedded from './FilterBuilderEmbedded';
import { TypeReference } from '../../../../Framework/Signum.React/Scripts/Reflection';

export default class PredictorRegressionMetrics extends React.Component<{ ctx: TypeContext<PredictorRegressionMetricsEmbedded> }> {

    render() {
        const ctx = this.props.ctx.subCtx({ formGroupStyle: "Basic" });


        return (
            <div className="form-vertical">
                <div className="col-sm-2">
                    <ValueLine ctx={ctx.subCtx(a => a.signed)} />
                </div>

                <div className="col-sm-2">
                    <ValueLine ctx={ctx.subCtx(a => a.absolute)} />
                </div>

                <div className="col-sm-2">
                    <ValueLine ctx={ctx.subCtx(a => a.deviation)} />
                </div>

                <div className="col-sm-2">
                    <ValueLine ctx={ctx.subCtx(a => a.percentageSigned)} />
                </div>

                <div className="col-sm-2">
                    <ValueLine ctx={ctx.subCtx(a => a.percentageAbsolute)} />
                </div>

                <div className="col-sm-2">
                    <ValueLine ctx={ctx.subCtx(a => a.percentageDeviation)} />
                </div>

            </div>
        );
    }
}
