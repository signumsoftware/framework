
import * as React from 'react'
import { TypeContext, mlistItemContext } from '@framework/TypeContext'
import { is } from '@framework/Signum.Entities'
import { ValueLine, ValueLineProps, OptionItem } from '@framework/Lines'
import { ChartColumnEmbedded, IChartBase, ChartMessage, ChartParameterEmbedded, ChartRequestModel } from '../Signum.Entities.Chart'
import * as ChartClient from '../ChartClient'
import { ChartScript, ChartScriptParameter, EnumValueList } from '../ChartClient'
import { ChartColumn } from './ChartColumn'
import { useForceUpdate, useAPI } from '@framework/Hooks'
import { UserState } from '../../Authorization/Signum.Entities.Authorization'

export interface ChartBuilderProps {
  ctx: TypeContext<IChartBase>; /*IChart*/
  queryKey: string;
  onInvalidate: () => void;
  onTokenChange: () => void;
  onRedraw: () => void;
  onOrderChanged: () => void;
}

export default function ChartBuilder(p: ChartBuilderProps) {
  const forceUpdate = useForceUpdate();

  const colorPalettes = useAPI(signal => ChartClient.getColorPalettes(), []);
  const chartScripts = useAPI(signal => ChartClient.getChartScripts(), []);

  function chartTypeImgClass(script: ChartScript): string {
    const cb = p.ctx.value;

    let css = "sf-chart-img";

    if (!cb.columns.some(a => a.element.token != undefined && a.element.token.parseException != undefined) && ChartClient.isCompatibleWith(script, cb))
      css += " sf-chart-img-equiv";

    if (is(cb.chartScript, script.symbol))
      css += " sf-chart-img-curr";

    return css;
  }

  function handleOnRedraw() {
    forceUpdate();
    p.onRedraw();
  }

  function handleTokenChange(cc: ChartColumnEmbedded) {
    cc.displayName = null!;
    cc.modified = true;
    forceUpdate();
    p.onTokenChange();
  }

  function handleChartScriptOnClick(cs: ChartScript) {
    const chart = p.ctx.value;
    let compatible = ChartClient.isCompatibleWith(cs, chart)
    chart.chartScript = cs.symbol;
    ChartClient.synchronizeColumns(chart, cs);
    chart.modified = true;

    if (!compatible)
      p.onInvalidate();
    else
      p.onRedraw();
  }

  function handleOrderChart(c: ChartColumnEmbedded, e: React.MouseEvent<any>) {
    ChartClient.handleOrderColumn(p.ctx.value, c, e.shiftKey);
    p.onOrderChanged();
  }


  function renderParameters(chartScript: ChartScript) {
    var parameterDic = mlistItemContext(p.ctx.subCtx(c => c.parameters, { formSize: "ExtraSmall", formGroupStyle: "Basic" })).toObject(a => a.value.name!);

    return (
      <fieldset className="sf-chart-parameters">
        <div className="row">
          {
            chartScript.parameterGroups.map((gr, i) =>
              <div className="col-sm-2" key={i}>
                <span style={{ color: "gray", textDecoration: "underline" }}>{gr.name}</span>
                {gr.parameters.map((p, j) => parameterDic[p.name] ? getParameterValueLine(parameterDic[p.name], p, j) : <p key={p.name} className="text-danger">{p.name}</p>)}
              </div>
            )
          }
        </div>
      </fieldset>
    );
  }

  function getParameterValueLine(ctx: TypeContext<ChartParameterEmbedded>, scriptParameter: ChartScriptParameter, j: number) {
    const chart = p.ctx.value;

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
    vl.onChange = handleOnRedraw;

    return <ValueLine key={j} {...vl} />;
  }

  const chart = p.ctx.value;

  const chartScript = chartScripts?.single(cs => is(cs.symbol, chart.chartScript));

  return (
    <div className="row sf-chart-builder">
      <div className="col-lg-2">
        <div className="sf-chart-type card">
          <div className="card-header">
            <h6 className="card-title mb-0">{ChartMessage.Chart.niceToString()}</h6>
          </div>
          <div className="card-body">
            {chartScripts?.map((cs, i) =>
              <div key={i} className={chartTypeImgClass(cs)} title={cs.symbol.key.after(".")} onClick={() => handleChartScriptOnClick(cs)}>
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
                {chartScript && colorPalettes && mlistItemContext(p.ctx.subCtx(c => c.columns, { formSize: "ExtraSmall" })).map((ctx, i) =>
                  <ChartColumn chartBase={chart} chartScript={chartScript} ctx={ctx} key={"C" + i} scriptColumn={chartScript!.columns[i]}
                    queryKey={p.queryKey} onTokenChange={() => handleTokenChange(ctx.value)}
                    onRedraw={handleOnRedraw}
                    onOrderChanged={handleOrderChart} colorPalettes={colorPalettes!} />)
                }
              </tbody>
            </table>
          </div>
        </div>
        {chartScript && renderParameters(chartScript)}
      </div>
    </div >
  );
}


