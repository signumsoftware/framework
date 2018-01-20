import * as React from 'react'
import * as OrderUtils from '../../../../Framework/Signum.React/Scripts/Frames/OrderUtils'
import { classes } from '../../../../Framework/Signum.React/Scripts/Globals'
import * as Navigator from '../../../../Framework/Signum.React/Scripts/Navigator'
import { FormGroup, FormControlReadonly, ValueLine, ValueLineType, EntityLine, EntityCombo, EntityList, EntityRepeater, EntityTable, EntityDetail, IRenderButtons } from '../../../../Framework/Signum.React/Scripts/Lines'
import { SearchControl } from '../../../../Framework/Signum.React/Scripts/Search'
import { TypeContext, FormGroupStyle, ButtonsContext } from '../../../../Framework/Signum.React/Scripts/TypeContext'
import FileLine from '../../Files/FileLine'
import { PredictSimpleResultEntity, PredictorMessage } from '../Signum.Entities.MachineLearning'
import * as Finder from '../../../../Framework/Signum.React/Scripts/Finder'
import { getQueryNiceName } from '../../../../Framework/Signum.React/Scripts/Reflection'
import QueryTokenEntityBuilder from '../../UserAssets/Templates/QueryTokenEntityBuilder'
import { QueryTokenEmbedded } from '../../UserAssets/Signum.Entities.UserAssets'
import { QueryFilterEmbedded } from '../../UserQueries/Signum.Entities.UserQueries'
import { QueryDescription, SubTokensOptions } from '../../../../Framework/Signum.React/Scripts/FindOptions'
import { API, predict } from '../PredictorClient';
import FilterBuilderEmbedded from './FilterBuilderEmbedded';
import { TypeReference } from '../../../../Framework/Signum.React/Scripts/Reflection';
import { toLite } from '../../../../Framework/Signum.React/Scripts/Signum.Entities';

export default class PredictSimpleResult extends React.Component<{ ctx: TypeContext<PredictSimpleResultEntity> }> implements IRenderButtons {

    handleClick = () => {

        var psr = this.props.ctx.value;

        Navigator.API.fetchAndForget(psr.predictor!).then(p => {

            if (!p.mainQuery.groupResults) {
                predict(toLite(p), { "Entity": psr.target }).done();
            } else {

                var fullKeys = p.mainQuery.columns.map(mle => mle.element.token!.tokenString!);

                var values = [psr.key0, psr.key1, psr.key2];

                var obj = fullKeys.map((fk, i) => ({ tokenString: fk, value: values[i] })).toObject(a => a.tokenString, a => a.value);

                predict(toLite(p), obj).done();
            };
        });
    }

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

    renderButtons(ctx: ButtonsContext): (React.ReactElement<any> | undefined)[] {
        return [OrderUtils.setOrder(10000, <button className="btn btn-info" onClick={this.handleClick}><i className="fa fa-lightbulb-o"></i>&nbsp;{PredictorMessage.Predict.niceToString()}</button >)];
    }
}
