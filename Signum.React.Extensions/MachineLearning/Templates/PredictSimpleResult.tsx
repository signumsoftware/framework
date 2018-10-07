import * as React from 'react'
import * as OrderUtils from '@framework/Frames/OrderUtils'
import * as Navigator from '@framework/Navigator'
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome'
import { ValueLine, EntityLine, IRenderButtons } from '@framework/Lines'
import { TypeContext, ButtonsContext } from '@framework/TypeContext'
import { PredictSimpleResultEntity, PredictorMessage } from '../Signum.Entities.MachineLearning'
import { predict } from '../PredictorClient';

export default class PredictSimpleResult extends React.Component<{ ctx: TypeContext<PredictSimpleResultEntity> }> implements IRenderButtons {

    handleClick = () => {

        var psr = this.props.ctx.value;

        Navigator.API.fetchAndForget(psr.predictor!).then(p => {

            if (!p.mainQuery.groupResults) {
                predict(p, { "Entity": psr.target }).done();
            } else {

                var fullKeys = p.mainQuery.columns.map(mle => mle.element.token!.tokenString!);

                var values = [psr.key0, psr.key1, psr.key2];

                var obj = fullKeys.map((fk, i) => ({ tokenString: fk, value: values[i] })).toObject(a => a.tokenString, a => a.value);

                predict(p, obj).done();
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
        return [OrderUtils.setOrder(10000, <button className="btn btn-info" onClick={this.handleClick}><FontAwesomeIcon icon={["far", "lightbulb"]} />&nbsp;{PredictorMessage.Predict.niceToString()}</button >)];
    }
}
