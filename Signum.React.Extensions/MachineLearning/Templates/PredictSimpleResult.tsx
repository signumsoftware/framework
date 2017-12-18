import * as React from 'react'
import { classes } from '../../../../Framework/Signum.React/Scripts/Globals'
import { FormGroup, FormControlStatic, ValueLine, ValueLineType, EntityLine, EntityCombo, EntityList, EntityRepeater, EntityTable, EntityDetail } from '../../../../Framework/Signum.React/Scripts/Lines'
import { SearchControl } from '../../../../Framework/Signum.React/Scripts/Search'
import { TypeContext, FormGroupStyle } from '../../../../Framework/Signum.React/Scripts/TypeContext'
import FileLine from '../../Files/FileLine'
import { PredictSimpleResultEntity } from '../Signum.Entities.MachineLearning'
import * as Finder from '../../../../Framework/Signum.React/Scripts/Finder'
import { getQueryNiceName } from '../../../../Framework/Signum.React/Scripts/Reflection'
import QueryTokenEntityBuilder from '../../UserAssets/Templates/QueryTokenEntityBuilder'
import { QueryTokenEmbedded } from '../../UserAssets/Signum.Entities.UserAssets'
import { QueryFilterEmbedded } from '../../UserQueries/Signum.Entities.UserQueries'
import { QueryDescription, SubTokensOptions } from '../../../../Framework/Signum.React/Scripts/FindOptions'
import { API } from '../PredictorClient';
import FilterBuilderEmbedded from './FilterBuilderEmbedded';
import { TypeReference } from '../../../../Framework/Signum.React/Scripts/Reflection';

export default class PredictSimpleResult extends React.Component<{ ctx: TypeContext<PredictSimpleResultEntity> }> {

    render() {
        const ctx = this.props.ctx;

        return (
            <div>
                <EntityLine ctx={ctx.subCtx(a => a.predictor)} />
                <ValueLine ctx={ctx.subCtx(a => a.type)} />
                <EntityLine ctx={ctx.subCtx(a => a.target)} hideIfNull={true} />
                <ValueLine ctx={ctx.subCtx(a => a.key0)} hideIfNull={true}/>
                <ValueLine ctx={ctx.subCtx(a => a.key1)} hideIfNull={true}/>
                <ValueLine ctx={ctx.subCtx(a => a.key2)} hideIfNull={true} />
                <ValueLine ctx={ctx.subCtx(a => a.originalValue)} hideIfNull={true} />
                <ValueLine ctx={ctx.subCtx(a => a.predictedValue)} hideIfNull={true}/>
                <ValueLine ctx={ctx.subCtx(a => a.originalCategory)} hideIfNull={true}/>
                <ValueLine ctx={ctx.subCtx(a => a.predictedCategory)} hideIfNull={true}/>
            </div>
        );
    }
}
