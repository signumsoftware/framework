import * as React from 'react'
import { Navigator } from '@framework/Navigator'
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome'
import { AutoLine, EntityLine, IRenderButtons } from '@framework/Lines'
import { TypeContext, ButtonsContext, ButtonBarElement } from '@framework/TypeContext'
import { PredictSimpleResultEntity, PredictorMessage } from '../Signum.MachineLearning'
import { PredictorClient } from '../PredictorClient';

export default class PredictSimpleResult extends React.Component<{ ctx: TypeContext<PredictSimpleResultEntity> }> implements IRenderButtons {
  handleClick = () : void => {
    var psr = this.props.ctx.value;
    Navigator.API.fetch(psr.predictor!).then(p => {
      if (!p.mainQuery.groupResults) {
        PredictorClient.predict(p, { "Entity": psr.target });
      } else {

        var fullKeys = p.mainQuery.columns.map(mle => mle.element.token!.tokenString!);

        var values = [psr.key0, psr.key1, psr.key2];

        var obj = fullKeys.map((fk, i) => ({ tokenString: fk, value: values[i] })).toObject(a => a.tokenString, a => a.value);

        PredictorClient.predict(p, obj);
      };
    });
  }

  override render(): React.ReactElement {
    const ctx = this.props.ctx;

    return (
      <div>
        <EntityLine ctx={ctx.subCtx(a => a.predictor)} />
        <AutoLine ctx={ctx.subCtx(a => a.type)} />
        <EntityLine ctx={ctx.subCtx(a => a.target)} hideIfNull={true} />
        <AutoLine ctx={ctx.subCtx(a => a.key0)} hideIfNull={true} />
        <AutoLine ctx={ctx.subCtx(a => a.key1)} hideIfNull={true} />
        <AutoLine ctx={ctx.subCtx(a => a.key2)} hideIfNull={true} />
        <AutoLine ctx={ctx.subCtx(a => a.originalValue)} hideIfNull={true} />
        <AutoLine ctx={ctx.subCtx(a => a.predictedValue)} hideIfNull={true} />
        <AutoLine ctx={ctx.subCtx(a => a.originalCategory)} hideIfNull={true} />
        <AutoLine ctx={ctx.subCtx(a => a.predictedCategory)} hideIfNull={true} />
      </div>
    );
  }

  renderButtons(ctx: ButtonsContext): (ButtonBarElement | undefined)[] {
    return [{
      order: 10000,
      button: <button className="btn btn-info" onClick={this.handleClick}><FontAwesomeIcon icon={["far", "lightbulb"]} />&nbsp;{PredictorMessage.Predict.niceToString()}</button >
    }];
  }
}
