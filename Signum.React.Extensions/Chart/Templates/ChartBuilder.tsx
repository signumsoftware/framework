
import * as React from 'react'
import { TypeContext, mlistItemContext } from '@framework/TypeContext'
import { is } from '@framework/Signum.Entities'
import { ValueLine, ValueLineProps, OptionItem } from '@framework/Lines'
import { ChartColumnEmbedded, IChartBase, ChartMessage, ChartParameterEmbedded, ChartRequestModel } from '../Signum.Entities.Chart'
import * as ChartClient from '../ChartClient'
import { ChartScript, ChartScriptParameter, EnumValueList } from '../ChartClient'
import { ChartColumn } from './ChartColumn'

export interface ChartBuilderProps {
  ctx: TypeContext<IChartBase>; /*IChart*/
  queryKey: string;
  onInvalidate: () => void;
  onTokenChange: () => void;
  onRedraw: () => void;
  onOrderChanged: () => void;
  
}

export interface ChartBuilderState {
  chartScripts?: ChartScript[],
  colorPalettes?: string[];
}


export default class ChartBuilder extends React.Component<ChartBuilderProps, ChartBuilderState> {

  constructor(props: ChartBuilderProps) {
    super(props);
    this.state = {};
  }

  componentWillMount() {
    const ctx = this.props.ctx;

    ChartClient.getChartScripts().then(scripts => {
      this.setState({ chartScripts: scripts });
      ChartClient.synchronizeColumns(ctx.value, scripts.single(a => is(a.symbol, ctx.value.chartScript)));
    }).done();

    ChartClient.getColorPalettes().then(colorPalettes =>
      this.setState({ colorPalettes: colorPalettes }))
      .done();
  }

  chartTypeImgClass(script: ChartScript): string {
    const cb = this.props.ctx.value;

    let css = "sf-chart-img";

    if (!cb.columns.some(a => a.element.token != undefined && a.element.token.parseException != undefined) && ChartClient.isCompatibleWith(script, cb))
      css += " sf-chart-img-equiv";

    if (is(cb.chartScript, script.symbol))
      css += " sf-chart-img-curr";

    return css;
  }

  handleOnRedraw = () => {
    this.forceUpdate();
    this.props.onRedraw();
  }

  handleTokenChange = (cc: ChartColumnEmbedded) => {
    cc.displayName = undefined;
    this.forceUpdate();
    this.props.onTokenChange();
  }

  handleChartScriptOnClick = (cs: ChartScript) => {

    const chart = this.props.ctx.value;
    let compatible = ChartClient.isCompatibleWith(cs, chart)
    chart.chartScript = cs.symbol;
    ChartClient.synchronizeColumns(chart, cs);
    chart.modified = true;

    if (!compatible)
      this.props.onInvalidate();
    else
      this.props.onRedraw();
  }

  handleOrderChart = (c: ChartColumnEmbedded, e: React.MouseEvent<any>) => {
    ChartClient.handleOrderColumn(this.props.ctx.value, c, e.shiftKey);
    this.props.onOrderChanged();
  }

  render() {

    const chart = this.props.ctx.value;

    const chartScript = this.state.chartScripts && this.state.chartScripts.single(cs => is(cs.symbol, chart.chartScript));

    return (
      <div className="row sf-chart-builder">
        <div className="col-lg-2">
          <div className="sf-chart-type card">
            <div className="card-header">
              <h6 className="card-title mb-0">{ChartMessage.Chart.niceToString()}</h6>
            </div>
            <div className="card-body">
              {this.state.chartScripts && this.state.chartScripts.map((cs, i) =>
                <div key={i} className={this.chartTypeImgClass(cs)} title={cs.symbol.key.after(".") + "\r\n" + cs.columnStructure} onClick={() => this.handleChartScriptOnClick(cs)}>
                  <img src={"data:image/jpeg;base64," + (cs.icon && cs.icon.bytes)} />
                </div>)}
            </div>
          </div>
        </div >
        <div className="col-lg-10">
          <div className="sf-chart-tokens card">
            <div className="card-header">
              <h6 className="card-title mb-0">{ChartMessage.Chart_ChartSettings.niceToString()}</h6>
            </div>
            <div className="card-body">
              <table className="table" style={{ marginBottom: "0px" }}>
                <thead>
                  <tr>
                    <th className="sf-chart-token-narrow">
                      {ChartMessage.Chart_Dimension.niceToString()}
                    </th>
                    <th className="sf-chart-token-wide">
                      Token
                    </th>
                  </tr>
                </thead>
                <tbody>
                  {chartScript && this.state.colorPalettes && mlistItemContext(this.props.ctx.subCtx(c => c.columns, { formSize: "ExtraSmall" })).map((ctx, i) =>
                    <ChartColumn chartBase={chart} chartScript={chartScript} ctx={ctx} key={"C" + i} scriptColumn={chartScript!.columns[i]}
                      queryKey={this.props.queryKey} onTokenChange={() => this.handleTokenChange(ctx.value)}
                      onRedraw={this.handleOnRedraw}
                      onOrderChanged={this.handleOrderChart} colorPalettes={this.state.colorPalettes!} />)
                  }
                </tbody>
              </table>
            </div>
          </div>
          <fieldset className="sf-chart-parameters">
            {
              chartScript && mlistItemContext(this.props.ctx.subCtx(c => c.parameters, { formSize: "ExtraSmall", formGroupStyle: "Basic" }))
                .map((ctx, i) => this.getParameterValueLine(ctx, chartScript.parameters[i]))
                .groupsOf(6).map((gr, j) =>
                  <div className="row" key={j}>
                    {gr.map((vl, i) => <div className="col-sm-2" key={i}>{vl}</div>)}
                  </div>)
            }
          </fieldset>
        </div>
      </div >);
  }
  
  getParameterValueLine(ctx: TypeContext<ChartParameterEmbedded>, scriptParameter: ChartScriptParameter) {

    const chart = this.props.ctx.value;

    const vl: ValueLineProps = {
      ctx: ctx.subCtx(a => a.value, { labelColumns: { sm: 6 } }),
      labelText: scriptParameter.name!,
    };

    if (scriptParameter.type == "Number" || scriptParameter.type == "String") {
      vl.valueLineType = "TextBox";
    }
    else if (scriptParameter.type == "Enum") {
      vl.valueLineType = "ComboBox";

      const tokenEntity = scriptParameter.columnIndex == undefined ? undefined : chart.columns[scriptParameter.columnIndex].element.token;

      const compatible = (scriptParameter.valueDefinition as EnumValueList).filter(a => a.typeFilter == undefined || tokenEntity == undefined || ChartClient.isChartColumnType(tokenEntity.token, a.typeFilter));
      if (compatible.length <= 1)
        vl.ctx.styleOptions.readOnly = true;

      vl.comboBoxItems = compatible.map(ev => ({
        value: ev.name,
        label: ev.name
      } as OptionItem));

      vl.valueHtmlAttributes = { size: null as any };
    }
    vl.onChange = this.handleOnRedraw;

    return <ValueLine {...vl} />;
  }

}


