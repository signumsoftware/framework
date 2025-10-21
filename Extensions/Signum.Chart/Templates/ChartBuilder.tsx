
import * as React from 'react'
import { TypeContext, mlistItemContext } from '@framework/TypeContext'
import { is } from '@framework/Signum.Entities'
import { ChartColumnEmbedded, ChartMessage, ChartParameterEmbedded, ChartTimeSeriesEmbedded } from '../Signum.Chart'
import { ChartClient } from '../ChartClient'
import { ChartColumn } from './ChartColumn'
import { ColorInterpolate, ColorScheme } from '../ColorPalette/ColorPaletteClient'
import { useForceUpdate, useAPI } from '@framework/Hooks'
import { colorInterpolators, colorSchemes } from '../ColorPalette/ColorUtils'
import { Dic } from '@framework/Globals'
import { IChartBase } from '../UserChart/Signum.Chart.UserChart'
import { EnumLine, FormGroup, NumberLine, TextBoxLine, TextBoxLineProps } from '@framework/Lines'
import { EnumLineProps, OptionItem } from '@framework/Lines/EnumLine'
import { Finder } from '@framework/Finder'
import { getTypeInfos, toNumberFormat } from '@framework/Reflection'
import { QueryDescription } from '@framework/FindOptions'
import { DateTime } from 'luxon'
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome'
import ChartTimeSeries from './ChartTimeSeries'
import { isDecimalKey, NumberBox } from '@framework/Lines/NumberLine'
import { AggregateFunction } from '@framework/Signum.DynamicQuery.Tokens'

export interface ChartBuilderProps {
  ctx: TypeContext<IChartBase>; /*IChart*/
  queryKey: string;
  maxRowsReached?: boolean;
  queryDescription: QueryDescription;
  onInvalidate: () => void;
  onTokenChange: () => void;
  onRedraw: () => void;
  onOrderChanged: () => void;
}

export default function ChartBuilder(p: ChartBuilderProps): React.JSX.Element {
  const forceUpdate = useForceUpdate();

  const chartScripts = useAPI(signal => ChartClient.getChartScripts(), []);

  function chartTypeImgClass(script: ChartClient.ChartScript): string {
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

  function handleChartScriptOnClick(cs: ChartClient.ChartScript) {
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
  const qs = Finder.getSettings(p.queryKey);
  
  const tis = getTypeInfos(p.queryDescription.columns["Entity"].type);

  return (<>
      {(qs?.allowSystemTime ?? tis.some(a => a.isSystemVersioned == true)) && <div className='d-flex align-items-center mb-3' style={{minHeight: 34}}>
        <label>
          <input className='me-1' type={'checkbox'} defaultChecked={chart.chartTimeSeries != null}
            onChange={e => {
              
              if(e.target.checked) {
                if(!chart.chartTimeSeries)
                  chart.chartTimeSeries = ChartTimeSeriesEmbedded.New({
                    timeSeriesStep: 1,
                    timeSeriesUnit: 'Month',
                    startDate: DateTime.now().startOf('year').toISODate(),
                    endDate: DateTime.now().toISODate(),
                    splitQueries: true,
                  });
              } else {
                chart.chartTimeSeries = null;
              }
              chart.modified = true;
              forceUpdate();
            }} 
          />
          Time machine
          <FontAwesomeIcon aria-hidden={true} className='mx-1' icon='clock-rotate-left' />
        </label>
        {chart.chartTimeSeries && <ChartTimeSeries chartTimeSeries={chart.chartTimeSeries} chartBase={p.ctx.value} onChange={handleOnRedraw}/>}
      </div>}
    <div className="row sf-chart-builder gx-2">
      <div className="col-lg-2">
        <div className="sf-chart-type bg-body rounded shadow-sm border-0 p-2">
          <h6 className="mb-3">{ChartMessage.ChartType.niceToString()}</h6>
          {chartScripts?.map((cs, i) =>
            <button
              key={i}
              type="button"
              className={`sf-chart-button ${chartTypeImgClass(cs)}`}
              title={cs.symbol.key.after(".")}
              onClick={() => handleChartScriptOnClick(cs)}>
              
                <img src={"data:image/jpeg;base64," + (cs.icon && cs.icon.bytes)} alt={cs.icon.fileName} />
              </button>)}
            <NumberLine ctx={p.ctx.subCtx(a => a.maxRows)} formGroupStyle="Basic" formSize="xs" valueHtmlAttributes={{ className: p.maxRowsReached ? "text-danger fw-bold" : undefined }} />
        </div>
      </div >
      <div className="col-lg-10">
        <div className="sf-chart-tokens bg-body rounded shadow-sm border-0 p-2">
            <h6>{ChartMessage.ChartSettings.niceToString()}</h6>
          <div>
            <table className="table table-borderless" style={{ marginBottom: "-1px" }}>
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
        {chartScript && <Parameters chart={p.ctx.value} chartScript={chartScript} parameterDic={parameterDic} onRedraw={handleOnRedraw} />}
      </div>
    </div >
  </>
  );
}

export function Parameters(props: {
  chartScript: ChartClient.ChartScript,
  chart: IChartBase,
  onRedraw?: () => void,
  parameterDic: { [name: string]: TypeContext<ChartParameterEmbedded> },
}): React.JSX.Element | null {


  var groups = props.chartScript.parameterGroups
    .filter(gr => gr.parameters.some(param => param.columnIndex == null))
    .map((gr, i) =>
      <div className={"col-sm-2"} key={i} >
        {gr.name && < span style={{ color: "gray", textDecoration: "underline" }}>{gr.name}</span>}
        {gr.parameters
          .filter(a => a.columnIndex == null)
          .map((sp, j) => props.parameterDic[sp.name] ?
            <ParameterValueLine key={sp.name} ctx={props.parameterDic[sp.name]} scriptParameter={sp} chart={props.chart} onRedraw={props.onRedraw} /> :
            <p key={sp.name} className="text-danger">{sp.name} ({sp.displayName})</p>)}
      </div>
    );

  if (groups.length == 0)
    return null;

  return (
    <fieldset className="sf-chart-parameters bg-body rounded shadow-sm border-0 my-1 p-2">
      <div className="row">
        {groups}
      </div>
    </fieldset>
  );
}

export function ColumnParameters(props: {
  chartScript: ChartClient.ChartScript,
  chart: IChartBase,
  onRedraw?: () => void,
  parameterDic: { [name: string]: TypeContext<ChartParameterEmbedded> },
  columnIndex: number
}): React.JSX.Element | null {


  var groups = props.chartScript.parameterGroups
    .filter(gr => gr.parameters.some(param => param.columnIndex == props.columnIndex))
    .map((gr, i) =>
      <div key={i} >
        {gr.name && < div style={{ color: "gray", textDecoration: "underline" }}>{gr.name}</div>}
        <div className="row">
          {gr.parameters
            .filter(a => a.columnIndex == props.columnIndex)
            .map((sp, j) =>
              <div className={"col-sm-3"} key={sp.name}>
                {props.parameterDic[sp.name] ?
                  <ParameterValueLine key={sp.name} ctx={props.parameterDic[sp.name]} scriptParameter={sp} chart={props.chart} onRedraw={props.onRedraw} /> :
                  <p key={sp.name} className="text-danger">{sp.name} ({sp.displayName})</p>
                }
              </div>
            )}
        </div>
      </div>
    );

  if (groups.length == 0)
    return null;

  return (
    <div className="sf-chart-parameters">
        {groups}
    </div>
  );
}

function ParameterValueLine({ ctx, scriptParameter, chart, onRedraw }: { ctx: TypeContext<ChartParameterEmbedded>, scriptParameter: ChartClient.ChartScriptParameter, onRedraw?: () => void, chart: IChartBase }) {

  if (scriptParameter.type == "Special") {
    var sp = scriptParameter.valueDefinition as ChartClient.SpecialParameter;

    if (sp.specialParameterType == "ColorCategory") {
      return (
        <EnumLine ctx={ctx.subCtx(a => a.value)} label={scriptParameter.displayName} onChange={onRedraw}
          optionItems={Dic.getKeys(colorSchemes)}
          onRenderDropDownListItem={oi => <div style={{ display: "flex", alignItems: "center", userSelect: "none" }}>
            <ColorScheme colorScheme={oi.value} />
            {oi.label}
          </div>} />
      );
    }

    if (sp.specialParameterType == "ColorInterpolate") {
      return (
        <EnumLine ctx={ctx.subCtx(a => a.value)} label={scriptParameter.displayName} onChange={onRedraw}
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
      );
    }

    throw new Error("Unexpected SpecialParameterType = " + sp.specialParameterType);
  }

  const token = scriptParameter.columnIndex == undefined ? undefined :
    chart.columns[scriptParameter.columnIndex].element.token?.token;

  if (scriptParameter.type == "Number" || scriptParameter.type == "String") {
    const tbl: TextBoxLineProps = {
      ctx: ctx.subCtx(a => a.value),
      label: scriptParameter.displayName!,
    };
    tbl.valueHtmlAttributes = { onBlur: onRedraw };
    if (ctx.value.value != ChartClient.defaultParameterValue(scriptParameter, token))
      tbl.labelHtmlAttributes = { style: { fontWeight: "bold" } };
    return <TextBoxLine {...tbl} />;
  }
  else if (scriptParameter.type == "Enum") {
    const el: EnumLineProps<string | null> = {
      ctx: ctx.subCtx(a => a.value),
      label: scriptParameter.displayName!,
    };
    el.type = { name: "string", isNotNullable: true };

    const values = (scriptParameter.valueDefinition as ChartClient.EnumValueList);

    if (values.length <= 1)
      el.ctx.styleOptions.readOnly = true;

    el.optionItems = values.map(ev => ({
      value: ev,
      label: ev
    } as OptionItem));

    el.valueHtmlAttributes = { size: null as any };
    el.onChange = onRedraw;
    if (ctx.value.value != ChartClient.defaultParameterValue(scriptParameter, token))
      el.labelHtmlAttributes = { style: { fontWeight: "bold" } };
    return <EnumLine {...el} />;
  }
  else if (scriptParameter.type == "Scala") {

    return <Scala ctx={ctx} chart={chart} scriptParameter={scriptParameter} onRedraw={onRedraw} />;
  }
  else {
    throw new Error("Unexpected Type = " + scriptParameter.type);
  }
}

export function Scala(p: { ctx: TypeContext<ChartParameterEmbedded>, scriptParameter: ChartClient.ChartScriptParameter, onRedraw?: () => void, chart: IChartBase }): React.ReactElement {


  const { ctx, scriptParameter, onRedraw, chart } = p;

  const token = scriptParameter.columnIndex == undefined ? undefined :
    chart.columns[scriptParameter.columnIndex].element.token?.token;

  const scala = p.scriptParameter.valueDefinition as ChartClient.Scala;

  const compatible = Object.entries(scala.standardScalas).filter(([value, columnType]) => columnType == undefined || token == undefined || ChartClient.isChartColumnType(token, columnType))
    .map(([value, columnType]) => value);


  const format = toNumberFormat(token?.format);

  function numberLine(part: string | null | undefined, buildPart: (newNumber: number | null) => string, label: string) {


    return <FormGroup label={label} ctx={ctx}>{id => <div className={p.ctx.inputGroupClass}>
      <NumberBox formControlClass={p.ctx.formControlClass} value={part ? (parseFloat(part) ?? null) : null}
        format={format}
        validateKey={isDecimalKey}
        onChange={newValue => {
          ctx.value.value = buildPart(newValue);
          p.onRedraw?.();
        }}
      />
      {token?.unit && <span className={p.ctx.readonlyAsPlainText ? undefined : "input-group-text"}>{token?.unit}</span>}
    </div>
    }</FormGroup>;
  }

  return (
    <div>
      <FormGroup ctx={ctx} label={scriptParameter.displayName}
        labelHtmlAttributes={{ style: { fontWeight: ctx.value.value != ChartClient.defaultParameterValue(scriptParameter, token) ? "bold" : undefined } }}>
        {id => <select id={id} className={p.ctx.formSelectClass} value={ctx.value.value?.contains("...") ? "Custom" : (ctx.value.value ?? undefined)}
          onChange={o => {
            ctx.value.value = o.currentTarget.value == "Custom" ? "0...100" : o.currentTarget.value;
            ctx.value.modified = true;
            p.onRedraw?.();
          }}>
          {compatible.map(a => <option key={a}>{a}</option>)}
          {scala.custom && <option>Custom</option>}
      </select>
      }
      </FormGroup>

      {ctx.value.value?.contains("...") && < div className="row">
        <div className="col-sm-6">
          {numberLine(ctx.value.value.before("..."), newValue => (newValue?.toString() ?? "") + "..." + ctx.value.value!.after("..."), AggregateFunction.niceToString("Min") + " " + token?.niceName)}
        </div>
        <div className="col-sm-6">
          {numberLine(ctx.value.value.after("..."), newValue => ctx.value.value!.before("...") + "..." + (newValue?.toString() ?? ""), AggregateFunction.niceToString("Max") + " " + token?.niceName)}
        </div>
      </div>
      }

    </div>
  );
}
