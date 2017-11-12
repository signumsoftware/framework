import * as React from 'react'
import { classes } from '../../../../Framework/Signum.React/Scripts/Globals'
import { FormGroup, FormControlStatic, ValueLine, ValueLineType, EntityLine, EntityCombo, EntityList, EntityRepeater, EntityTable } from '../../../../Framework/Signum.React/Scripts/Lines'
import { SearchControl } from '../../../../Framework/Signum.React/Scripts/Search'
import { TypeContext, FormGroupStyle } from '../../../../Framework/Signum.React/Scripts/TypeContext'
import FileLine from '../../Files/FileLine'
import { PredictorStatsEmbedded } from '../Signum.Entities.MachineLearning'
import * as Finder from '../../../../Framework/Signum.React/Scripts/Finder'
import { getQueryNiceName } from '../../../../Framework/Signum.React/Scripts/Reflection'
import QueryTokenEntityBuilder from '../../UserAssets/Templates/QueryTokenEntityBuilder'
import { QueryTokenEmbedded } from '../../UserAssets/Signum.Entities.UserAssets'
import { QueryFilterEmbedded } from '../../UserQueries/Signum.Entities.UserQueries'
import { QueryDescription, SubTokensOptions } from '../../../../Framework/Signum.React/Scripts/FindOptions'
import { API } from '../PredictorClient';
import FilterBuilderEmbedded from './FilterBuilderEmbedded';
import { TypeReference } from '../../../../Framework/Signum.React/Scripts/Reflection';

export default class PredictorStats extends React.Component<{ ctx: TypeContext<PredictorStatsEmbedded> }> {
    
    render() {
        const ctx = this.props.ctx.subCtx({ formGroupStyle: "Basic" });


        return (
            <div className="form-vertical">
                <ValueLine ctx={ctx.subCtx(a => a.mean)} />
                <ValueLine ctx={ctx.subCtx(a => a.standartDeviation)} />
                <ValueLine ctx={ctx.subCtx(a => a.variance)} />
                <ValueLine ctx={ctx.subCtx(a => a.errorCount)} />
                <ValueLine ctx={ctx.subCtx(a => a.totalCount)} />
            </div>
        );
    }
}
