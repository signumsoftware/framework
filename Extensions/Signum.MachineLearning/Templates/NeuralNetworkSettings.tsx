import * as React from 'react'
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome'
import { FormGroup, FormControlReadonly, AutoLine, EntityTable, StyleContext, OptionItem, LineBaseProps } from '@framework/Lines'
import { SearchValue } from '@framework/Search'
import { TypeContext } from '@framework/TypeContext'
import { NeuralNetworkSettingsEntity, PredictorEntity, PredictorColumnUsage, PredictorCodificationEntity, NeuralNetworkHidenLayerEmbedded, PredictorAlgorithmSymbol, TensorFlowOptimizer } from '../Signum.MachineLearning'
import { PredictorClient } from '../PredictorClient';
import { is } from '@framework/Signum.Entities';
import { Popover, OverlayTrigger } from 'react-bootstrap';
import { useForceUpdate, useAPI } from '@framework/Hooks'

export default function NeuralNetworkSettings(p : { ctx: TypeContext<NeuralNetworkSettingsEntity> }): React.JSX.Element {
  const forceUpdate = useForceUpdate();
  function handlePredictionTypeChanged() {
    var nn = p.ctx.value;
    if (nn.predictionType == "Classification" || nn.predictionType == "MultiClassification") {
      nn.lossFunction = "softmax_cross_entropy_with_logits";
      nn.evalErrorFunction = "ClassificationError";
    } else {
      nn.lossFunction = "MeanSquaredError";
      nn.evalErrorFunction = "MeanSquaredError";
    }
  }


  function getHelpBlock(optimizer: TensorFlowOptimizer | undefined) {
    switch (optimizer) {
      case "Adam": return "";
      case "GradientDescentOptimizer": return "";
      default: throw new Error("Unexpected " + optimizer)
    }
  }

  //Values found letting a NN work for a night learning y = sin(x * 5), no idea if they work ok for other cases
  function handleOptimizerChange() {
    var nns = p.ctx.value;
    switch (nns.optimizer) {
      case "Adam":
        nns.learningRate = 0.01;
        break;
      case "GradientDescentOptimizer":
        nns.learningRate = 0.01;
        break;
      default:
    }

    forceUpdate();
  }

  function renderCount(ctx: StyleContext, p: PredictorEntity, usage: PredictorColumnUsage) {
    return (
      <FormGroup ctx={ctx} label={PredictorColumnUsage.niceToString(usage) + " columns"}>
        {inputId => p.state != "Trained" ? <FormControlReadonly id={inputId} ctx={ctx}>?</FormControlReadonly> : <SearchValue isBadge={true} isLink={true} findOptions={{
          queryName: PredictorCodificationEntity,
          filterOptions: [
            { token: PredictorCodificationEntity.token(e => e.predictor), value: p },
            { token: PredictorCodificationEntity.token(e => e.usage), value: usage }
          ]
        }} />}
      </FormGroup>
    );
  }
  const ctx = p.ctx;

  var pred = ctx.findParent(PredictorEntity);

  const ctxb = ctx.subCtx({ formGroupStyle: "Basic" })
  const ctx6 = ctx.subCtx({ labelColumns: 8 })

  return (
    <div>
      <h4>{NeuralNetworkSettingsEntity.niceName()}</h4>
      <AutoLine ctx={ctx.subCtx(a => a.predictionType)} onChange={handlePredictionTypeChanged} />
      {renderCount(ctx, pred, "Input")}
      <EntityTable ctx={ctx.subCtx(a => a.hiddenLayers)} columns={[
        { property: a => a.size, headerHtmlAttributes: { style: { width: "33%" } } },
        { property: a => a.activation, headerHtmlAttributes: { style: { width: "33%" } } },
        { property: a => a.initializer, headerHtmlAttributes: { style: { width: "33%" } } },
      ]} />
      <div>
        <div className="row">
          <div className="col-sm-4">
            {renderCount(ctxb, pred, "Output")}
          </div>
          <div className="col-sm-4">
            <AutoLine ctx={ctxb.subCtx(a => a.outputActivation)} />
          </div>
          <div className="col-sm-4">
            <AutoLine ctx={ctxb.subCtx(a => a.outputInitializer)} />
          </div>
        </div>
        <div className="row">
          <div className="col-sm-4">
          </div>
          <div className="col-sm-4">
            <AutoLine ctx={ctxb.subCtx(a => a.lossFunction)} />
          </div>
          <div className="col-sm-4">
            <AutoLine ctx={ctxb.subCtx(a => a.evalErrorFunction)} />
          </div>

        </div>
      </div>
      <hr />
      <div className="row">
        <div className="col-sm-6">
          <AutoLine ctx={ctx6.subCtx(a => a.optimizer)} onChange={handleOptimizerChange} helpText={getHelpBlock(ctx.value.optimizer)} />
          <AutoLine ctx={ctx6.subCtx(a => a.learningRate)} />
        </div>
        <div className="col-sm-6">
          <AutoLine ctx={ctx6.subCtx(a => a.minibatchSize)} />
          <AutoLine ctx={ctx6.subCtx(a => a.numMinibatches)} />
          <AutoLine ctx={ctx6.subCtx(a => a.bestResultFromLast)} />
          <AutoLine ctx={ctx6.subCtx(a => a.saveProgressEvery)} />
          <AutoLine ctx={ctx6.subCtx(a => a.saveValidationProgressEvery)} />
        </div>
      </div>
    </div>
  );
}

interface LabelWithHelpProps {
  ctx: TypeContext<LineBaseProps>;
  text: React.ReactNode;
}

export function LabelWithHelp(p: LabelWithHelpProps) {

    return (
      <OverlayTrigger overlay={
        <Popover id={p.ctx.prefix + "_popper"} placement="auto" key="p">
          <h3 className="popover-header">{p.ctx.niceName()}</h3>
          <div className="popover-body">{p.text}</div>
        </Popover>}>
        <span key="s">
          {p.ctx.niceName()} <FontAwesomeIcon icon="circle-question" />
        </span>
      </OverlayTrigger>
    );
}
