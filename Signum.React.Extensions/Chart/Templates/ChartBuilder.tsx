
import * as React from 'react'
import { TypeContext, mlistItemContext } from '@framework/TypeContext'
import { is } from '@framework/Signum.Entities'
import { ValueLine, ValueLineProps, OptionItem } from '@framework/Lines'
import { ChartColumnEmbedded, IChartBase, ChartMessage, ChartParameterEmbedded, ChartRequestModel } from '../Signum.Entities.Chart'
import * as ChartClient from '../ChartClient'
import { ChartScript, ChartScriptParameter, EnumValueList } from '../ChartClient'
import { ChartColumn } from './ChartColumn'
import * as  ColorPaletteClient from '../ColorPalette/ColorPaletteClient'
import { ColorInterpolate, ColorScheme } from '../ColorPalette/ColorPaletteClient'
import { useForceUpdate, useAPI } from '@framework/Hooks'
import { UserState } from '../../Authorization/Signum.Entities.Authorization'
import { colorInterpolators, colorSchemes } from '../ColorPalette/ColorUtils'
import { Dic } from '@framework/Globals'

export interface ChartBuilderProps {
  ctx: TypeContext<IChartBase>; /*IChart*/
  queryKey: string;
  maxRowsReached?: boolean;
  onInvalidate: () => void;
  onTokenChange: () => void;
  onRedraw: () => void;
  onOrderChanged: () => void;
}

export default function ChartBuilder(p: ChartBuilderProps) {
  const forceUpdate = useForceUpdate();

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
    cc.format = null!;
    cc.modified = true;
    ChartClient.synchronizeColumns(chart, chartScript!);
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

  const chart = p.ctx.value;

  const chartScript = chartScripts?.single(cs => is(cs.symbol, chart.chartScript));

  var parameterDic = mlistItemContext(p.ctx.subCtx(c => c.parameters, { formSize: "xs", formGroupStyle: "Basic" })).toObject(a => a.value.name!);

  return (
    <div className="row sf-chart-builder gx-2">
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
          <div className="card-body">
            <ValueLine ctx={p.ctx.subCtx(a => a.maxRows)} formGroupStyle="Basic" formSize="xs" valueHtmlAttributes={{ className: p.maxRowsReached ? "text-danger fw-bold" : undefined }} />
          </div>
        </div>
      </div >
      <div className="col-lg-10">
        <div className="sf-chart-tokens card">
          <div className="card-header">
            <h6 className="card-title mb-0">{ChartMessage.ChartSettings.niceToString()}</h6>
          </div>
          <div className="card-body">
            <table className="table" style={{ marginBottom: "0px" }}>
              <thead>
                <tr>
                  <th className="sf-chart-token-narrow">
                    {ChartMessage.Dimension.niceToString()}
                  </th>
                  <th className="sf-chart-token-wide">
                    Token
                  </th>
                </tr>
              </thead>
              <tbody>
                {chartScript && mlistItemContext(p.ctx.subCtx(c => c.columns, { formSize: "xs" })).map((ctx, i) =>
                  <ChartColumn chartBase={chart} chartScript={chartScript} ctx={ctx} key={"C" + i} scriptColumn={chartScript!.columns[i]}
                    queryKey={p.queryKey} onTokenChange={() => handleTokenChange(ctx.value)}
                    onRedraw={handleOnRedraw}
                    onOrderChanged={handleOrderChart} columnIndex={i} parameterDic={parameterDic} />)
                }
              </tbody>
            </table>
          </div>
        </div>
        {chartScript && <Parameters chart={p.ctx.value} chartScript={chartScript} parameterDic={parameterDic} columnIndex={null} onRedraw={handleOnRedraw} />}
      </div>
    </div >
  );
}

export function Parameters(props: {
  chartScript: ChartScript,
  chart: IChartBase,
  onRedraw?: () => void,
  parameterDic: { [name: string]: TypeContext<ChartParameterEmbedded> },
  columnIndex: number | null
}) {


  var groups = props.chartScript.parameterGroups
    .filter(gr => gr.parameters.some(param => param.columnIndex == props.columnIndex))
    .map((gr, i) =>
      <div className={props.columnIndex == null ? "col-sm-2" : "col-sm-3"} key={i} >
        {gr.name && < span style={{ color: "gray", textDecoration: "underline" }}>{gr.name}</span>}
        {gr.parameters
          .filter(sp => sp.columnIndex == props.columnIndex)
          .map((sp, j) => props.parameterDic[sp.name] ?
            <ParameterValueLine key={sp.name} ctx={props.parameterDic[sp.name]} scriptParameter={sp} chart={props.chart} onRedraw={props.onRedraw} /> :
            <p key={sp.name} className="text-danger">{sp.name}</p>)}
      </div>
    );

  if (groups.length == 0)
    return null;

  if (props.columnIndex == null)
    return (
      <fieldset className="sf-chart-parameters">
        <div className="row">
          {groups}
        </div>
      </fieldset>
    );
  else
    return (
      <div className="sf-chart-parameters">
        <div className="row">
          {groups}
        </div>
      </div>
    );
}

function ParameterValueLine({ ctx, scriptParameter, chart, onRedraw }: { ctx: TypeContext<ChartParameterEmbedded>, scriptParameter: ChartScriptParameter, onRedraw?: () => void, chart: IChartBase }) {


  if (scriptParameter.type == "Special") {
    var sp = scriptParameter.valueDefinition as ChartClient.SpecialParameter;

    if (sp.specialParameterType == "ColorCategory") {
      return <ValueLine ctx={ctx.subCtx(a => a.value)} label={scriptParameter.name} onChange={onRedraw}
        valueLineType="DropDownList"
        optionItems={Dic.getKeys(colorSchemes)}
        onRenderDropDownListItem={oi => <div style={{ display: "flex", alignItems: "center", userSelect: "none" }}>
          <ColorScheme colorScheme={oi.value} />
          {oi.label}
        </div>} />
    }

    if (sp.specialParameterType == "ColorInterpolate") {
      return <ValueLine ctx={ctx.subCtx(a => a.value)} label={scriptParameter.name} onChange={onRedraw}
        valueLineType="DropDownList"
        optionItems={Dic.getKeys(colorInterpolators).map(a => (ctx.value.value?.startsWith("-") ? "-" : "") + a)}
        onRenderDropDownListItem={oi => <div style={{ display: "flex", alignItems: "center", userSelect: "none" }}>
          <ColorInterpolate colorInterpolator={oi.value} />
          {oi.label}
        </div>}
        helpText={<label>
          <input type="checkbox" className="form-check me-2"
            checked={ctx.value.value?.startsWith("-")}
            onChange={e => {
              if (ctx.value.value)
                ctx.value.value = e.currentTarget.checked ? ("-" + ctx.value.value) : ctx.value.value.after("-");

              onRedraw?.();
            }} />
          Invert
        </label>}
      />
    }
  }

  const token = scriptParameter.columnIndex == undefined ? undefined :
    chart.columns[scriptParameter.columnIndex].element.token?.token;



  const vl: ValueLineProps = {
    ctx: ctx.subCtx(a => a.value),
    label: scriptParameter.name!,
  };

  if (scriptParameter.type == "Number" || scriptParameter.type == "String") {
    vl.valueLineType = "TextBox";
    vl.valueHtmlAttributes = { onBlur: onRedraw };
  }
  else if (scriptParameter.type == "Enum") {
    vl.valueLineType = "DropDownList";
    vl.type = { name: "string", isNotNullable: true };

    const compatible = (scriptParameter.valueDefinition as EnumValueList).filter(a => a.typeFilter == undefined || token == undefined || ChartClient.isChartColumnType(token, a.typeFilter));

    if (compatible.length <= 1)
      vl.ctx.styleOptions.readOnly = true;

    vl.optionItems = compatible.map(ev => ({
      value: ev.name,
      label: ev.name
    } as OptionItem));

    vl.valueHtmlAttributes = { size: null as any };
    vl.onChange = onRedraw;
  }

  if (ctx.value.value != ChartClient.defaultParameterValue(scriptParameter, token))
    vl.labelHtmlAttributes = { style: { fontWeight: "bold" } };

  return <ValueLine {...vl} />;
}

