import * as React from 'react'
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome'
import { FormGroup, FormControlReadonly, ValueLine, EntityTable, StyleContext, OptionItem, LineBaseProps } from '@framework/Lines'
import { ValueSearchControl } from '@framework/Search'
import { TypeContext } from '@framework/TypeContext'
import { NeuralNetworkSettingsEntity, PredictorEntity, PredictorColumnUsage, PredictorCodificationEntity, NeuralNetworkHidenLayerEmbedded, PredictorAlgorithmSymbol, NeuralNetworkLearner } from '../Signum.Entities.MachineLearning'
import { API } from '../PredictorClient';
import { is } from '@framework/Signum.Entities';
import { Popover } from '@framework/Components';

export default class NeuralNetworkSettings extends React.Component<{ ctx: TypeContext<NeuralNetworkSettingsEntity> }> {
  handlePredictionTypeChanged = () => {
    var nn = this.props.ctx.value;
    if (nn.predictionType == "Classification" || nn.predictionType == "MultiClassification") {
      nn.lossFunction = "CrossEntropyWithSoftmax";
      nn.evalErrorFunction = "ClassificationError";
    } else {
      nn.lossFunction = "SquaredError";
      nn.evalErrorFunction = "SquaredError";
    }
  }

  render() {
    const ctx = this.props.ctx;

    var p = ctx.findParent(PredictorEntity);

    const ctxb = ctx.subCtx({ formGroupStyle: "Basic" })
    const ctx6 = ctx.subCtx({ labelColumns: 8 })

    return (
      <div>
        <h4>{NeuralNetworkSettingsEntity.niceName()}</h4>
        {p.algorithm && <DeviceLine ctx={ctx.subCtx(a => a.device)} algorithm={p.algorithm} />}
        <ValueLine ctx={ctx.subCtx(a => a.predictionType)} onChange={this.handlePredictionTypeChanged} />
        {this.renderCount(ctx, p, "Input")}
        <EntityTable ctx={ctx.subCtx(a => a.hiddenLayers)} columns={EntityTable.typedColumns<NeuralNetworkHidenLayerEmbedded>([
          { property: a => a.size, headerHtmlAttributes: { style: { width: "33%" } } },
          { property: a => a.activation, headerHtmlAttributes: { style: { width: "33%" } } },
          { property: a => a.initializer, headerHtmlAttributes: { style: { width: "33%" } } },
        ])} />
        <div>
          <div className="row">
            <div className="col-sm-4">
              {this.renderCount(ctxb, p, "Output")}
            </div>
            <div className="col-sm-4">
              <ValueLine ctx={ctxb.subCtx(a => a.outputActivation)} />
            </div>
            <div className="col-sm-4">
              <ValueLine ctx={ctxb.subCtx(a => a.outputInitializer)} />
            </div>
          </div>
          <div className="row">
            <div className="col-sm-4">
            </div>
            <div className="col-sm-4">
              <ValueLine ctx={ctxb.subCtx(a => a.lossFunction)} />
            </div>
            <div className="col-sm-4">
              <ValueLine ctx={ctxb.subCtx(a => a.evalErrorFunction)} />
            </div>

          </div>
        </div>
        <hr />
        <div className="row">
          <div className="col-sm-6">
            <ValueLine ctx={ctx6.subCtx(a => a.learner)} onChange={this.handleLearnerChange} helpText={this.getHelpBlock(ctx.value.learner)} />
            <ValueLine ctx={ctx6.subCtx(a => a.learningRate)} />
            <ValueLine ctx={ctx6.subCtx(a => a.learningMomentum)} formGroupHtmlAttributes={hideFor(ctx6, "AdaDelta", "AdaGrad", "SGD")} />
            {withHelp(<ValueLine ctx={ctx6.subCtx(a => a.learningUnitGain)} formGroupHtmlAttributes={hideFor(ctx6, "AdaDelta", "AdaGrad", "SGD")} />, <p>true makes it stable (Loss = 1)<br />false diverge (Loss >> 1)</p>)}
            <ValueLine ctx={ctx6.subCtx(a => a.learningVarianceMomentum)} formGroupHtmlAttributes={hideFor(ctx6, "AdaDelta", "AdaGrad", "SGD", "MomentumSGD")} />
          </div>
          <div className="col-sm-6">
            <ValueLine ctx={ctx6.subCtx(a => a.minibatchSize)} />
            <ValueLine ctx={ctx6.subCtx(a => a.numMinibatches)} />
            <ValueLine ctx={ctx6.subCtx(a => a.bestResultFromLast)} />
            <ValueLine ctx={ctx6.subCtx(a => a.saveProgressEvery)} />
            <ValueLine ctx={ctx6.subCtx(a => a.saveValidationProgressEvery)} />
          </div>
        </div>
      </div>
    );
  }

  getHelpBlock = (learner: NeuralNetworkLearner | undefined) => {
    switch (learner) {
      case "AdaDelta": return "Did not work :S";
      case "AdaGrad": return "";
      case "Adam": return "";
      case "FSAdaGrad": return "";
      case "MomentumSGD": return "";
      case "RMSProp": return "";
      case "SGD": return "";
      default: throw new Error("Unexpected " + learner)
    }
  }

  //Values found letting a NN work for a night learning y = sin(x * 5), no idea if they work ok for other cases
  handleLearnerChange = () => {
    var nns = this.props.ctx.value;
    switch (nns.learner) {
      case "Adam":
        nns.learningRate = 1;
        nns.learningMomentum = 0.1;
        nns.learningVarianceMomentum = 0.1;
        nns.learningUnitGain = false;
        break;
      case "AdaDelta":
        nns.learningRate = 1;
        nns.learningMomentum = nns.learningVarianceMomentum = nns.learningUnitGain = null;
        break;
      case "AdaGrad":
        nns.learningRate = 0.1;
        nns.learningMomentum = nns.learningVarianceMomentum = nns.learningUnitGain = null;
        break;
      case "FSAdaGrad":
        nns.learningRate = 0.1;
        nns.learningMomentum = 0.01;
        nns.learningVarianceMomentum = 1;
        nns.learningUnitGain = false;
        break;
      case "MomentumSGD":
        nns.learningRate = 0.1;
        nns.learningMomentum = 0.01;
        nns.learningVarianceMomentum = 0.001;
        nns.learningUnitGain = false;
        break;
      case "RMSProp":
        nns.learningRate = 0.1;
        nns.learningMomentum = 0.01;
        nns.learningVarianceMomentum = 1;
        nns.learningUnitGain = false;
        break;
      case "SGD":
        nns.learningRate = 0.1;
        nns.learningMomentum = nns.learningVarianceMomentum = nns.learningUnitGain = null;
        break;
      default:
    }

    this.forceUpdate();
  }

  renderCount(ctx: StyleContext, p: PredictorEntity, usage: PredictorColumnUsage) {
    return (
      <FormGroup ctx={ctx} labelText={PredictorColumnUsage.niceToString(usage) + " columns"}>
        {p.state != "Trained" ? <FormControlReadonly ctx={ctx}>?</FormControlReadonly> : <ValueSearchControl isBadge={true} isLink={true} findOptions={{
          queryName: PredictorCodificationEntity,
          parentToken: PredictorCodificationEntity.token(e => e.predictor),
          parentValue: p,
          filterOptions: [
            { token: PredictorCodificationEntity.token(e => e.usage), value: usage }
          ]
        }} />}
      </FormGroup>
    );
  }
}

function withHelp(element: React.ReactElement<LineBaseProps>, text: React.ReactNode): React.ReactElement<any> {
  var ctx = element.props.ctx;

  var label = <LabelWithHelp ctx={ctx} text={text} />;

  return React.cloneElement(element, { labelText: label } as LineBaseProps);
}


interface LabelWithHelpProps {
  ctx: TypeContext<LineBaseProps>;
  text: React.ReactNode;
}

interface LabelWithHelpState {
  isOpen?: boolean
}

export class LabelWithHelp extends React.Component<LabelWithHelpProps, LabelWithHelpState> {

  constructor(props: LabelWithHelpProps) {
    super(props);
    this.state = {};
  }

  toggle = () => {
    this.setState({ isOpen: !this.state.isOpen });
  }

  span?: HTMLSpanElement | null;
  render() {
    const ctx = this.props.ctx;
    return [
      <span ref={r => this.span = r} onClick={this.toggle} key="s">
        {ctx.niceName()} <FontAwesomeIcon icon="question-circle" />
      </span>,
      <Popover placement="auto" target={() => this.span!} toggle={this.toggle} isOpen={this.state.isOpen} key="p">
        <h3 className="popover-header">{ctx.niceName()}</h3>
        <div className="popover-body">{this.props.text}</div>
      </Popover>
    ];
  }
}

function hideFor(ctx: TypeContext<NeuralNetworkSettingsEntity>, ...learners: NeuralNetworkLearner[]): React.HTMLAttributes<any> | undefined {
  return ctx.value.learner && learners.contains(ctx.value.learner) ? ({ style: { opacity: 0.5 } }) : undefined;
}

interface DeviceLineProps {
  ctx: TypeContext<string | null | undefined>;
  algorithm: PredictorAlgorithmSymbol;
}

interface DeviceLineState {
  devices?: string[];
}

export class DeviceLine extends React.Component<DeviceLineProps, DeviceLineState> {

  constructor(props: DeviceLineProps) {
    super(props);
    this.state = {};
  }

  componentWillMount() {
    this.loadData();
  }

  componentWillReceiveProps(newProps: DeviceLineProps) {
    if (!is(newProps.algorithm, this.props.algorithm))
      this.loadData();
  }

  loadData() {
    API.availableDevices(this.props.algorithm)
      .then(devices => this.setState({ devices }))
      .done();
  }

  render() {
    const ctx = this.props.ctx;
    return (
      <ValueLine ctx={ctx} comboBoxItems={(this.state.devices || []).map(a => ({ label: a, value: a }) as OptionItem)} valueLineType={"ComboBox"} valueHtmlAttributes={{ size: 1 }} />
    );
  }
}
