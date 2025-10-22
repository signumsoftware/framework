import * as React from 'react'
import { classes } from '@framework/Globals'
import { QueryToken, SubTokensOptions } from '@framework/FindOptions'
import { TypeContext, StyleContext } from '@framework/TypeContext'
import { tryGetTypeInfos, TypeInfo, isTypeEnum } from '@framework/Reflection'
import { Navigator } from '@framework/Navigator'
import { AutoLine, FormGroup, TextBoxLine } from '@framework/Lines'
import { ChartColumnEmbedded, ChartMessage, ChartColumnType, ChartParameterEmbedded } from '../Signum.Chart'
import { ChartClient } from '../ChartClient'
import { ColorPaletteClient } from '../ColorPalette/ColorPaletteClient'
import { JavascriptMessage, toLite } from '@framework/Signum.Entities';
import { useAPI, useAPIWithReload, useForceUpdate } from '@framework/Hooks'
import { ColumnParameters, Parameters } from './ChartBuilder'
import { IChartBase } from '../UserChart/Signum.Chart.UserChart'
import { ColorPaletteEntity } from '../ColorPalette/Signum.Chart.ColorPalette'
import QueryTokenEntityBuilder from '../../Signum.UserAssets/Templates/QueryTokenEmbeddedBuilder'

export interface ChartColumnProps {
  ctx: TypeContext<ChartColumnEmbedded>;
  columnIndex: number;
  scriptColumn: ChartClient.ChartScriptColumn;
  chartScript: ChartClient.ChartScript;
  chartBase: IChartBase;
  queryKey: string;
  onRedraw: () => void;
  parameterDic: { [name: string]: TypeContext<ChartParameterEmbedded> },
  onOrderChanged: (chartColumn: ChartColumnEmbedded, e: React.MouseEvent<any>) => void;
  onTokenChange: () => void;
}


export function ChartColumn(p: ChartColumnProps): React.JSX.Element {

  const forceUpdate = useForceUpdate();

  const [expanded, setExpanded] = React.useState<boolean>(false);

  function handleExpanded() {
    setExpanded(!expanded);
  }

  function handleDragOver(de: React.DragEvent<any>, ) {
    de.preventDefault();
    var txt = de.dataTransfer.getData("text");
    const cols = p.chartBase.columns;
    if (txt.startsWith("chartColumn_")) {
      var dropIndex = cols.findIndex(a => a.element == p.ctx.value);
      var dragIndex = parseInt(txt.after("chartColumn_"));
      if (dropIndex == dragIndex)
        de.dataTransfer.dropEffect = "none";
    }
  }

  function handleOnDrop(de: React.DragEvent<any>, ) {
    de.preventDefault();

    const cols = p.chartBase.columns;
    var txt = de.dataTransfer.getData("text");
    if (txt.startsWith("chartColumn_")) {

      var dropIndex = cols.findIndex(a => a.element == p.ctx.value);
      var dragIndex = parseInt(txt.after("chartColumn_"));

      if (dropIndex != dragIndex) {


        var dropToken = cols[dropIndex].element.token;
        var dragToken = cols[dragIndex].element.token;
        cols[dropIndex].element.token = dragToken;
        cols[dragIndex].element.token = dropToken;

        cols[dropIndex].element.modified = true;
        cols[dragIndex].element.modified = true;

        if (dragToken) dragToken.modified = true;
        if (dropToken) dropToken.modified = true;

        p.onTokenChange();
      }

    }
  }

  function handleDragStart(de: React.DragEvent<any>, ) {
    const dragIndex = p.chartBase.columns.findIndex(a => a.element == p.ctx.value);
    de.dataTransfer.setData('text', "chartColumn_" + dragIndex); //cannot be empty string
    de.dataTransfer.effectAllowed = "move";
  }


  function getColorPalettes() {
    const token = p.ctx.value.token;

    const t = token?.token!.type;

    if (t == undefined || Navigator.isReadOnly(ColorPaletteEntity))
      return [];

    if (!t.isLite && !isTypeEnum(t.name))
      return [];

    return tryGetTypeInfos(t);
  }

  function orderClassName(c: ChartColumnEmbedded) {
    if (c.orderByType == null || c.orderByIndex == null)
      return "";

    return (c.orderByType == "Ascending" ? "asc" : "desc") + (" l" + c.orderByIndex);
  }
  const sc = p.scriptColumn;
  const cb = p.chartBase;

  var subTokenOptions = SubTokensOptions.CanElement | SubTokensOptions.CanAggregate | (p.chartBase.chartTimeSeries ? SubTokensOptions.CanTimeSeries : 0);

  const ctx = p.ctx;

  const ctxBasic = ctx.subCtx({ formSize: "xs", formGroupStyle: "Basic" });

  var numParameters = p.chartScript.parameterGroups.flatMap(a => a.parameters).filter(a => a.columnIndex == p.columnIndex).length

  return (
    <>
      <tr className="sf-chart-token">
        <th
          draggable={true}
          onDragEnter={handleDragOver}
          onDragOver={handleDragOver}
          onDrop={handleOnDrop}
          onDragStart={handleDragStart}

          onClick={e => ctx.value.token && p.onOrderChanged(ctx.value, e)}
          style={{ whiteSpace: "nowrap", cursor: ctx.value.token ? "pointer" : undefined, userSelect: "none" }}>
          <span className={"sf-header-sort " + orderClassName(ctx.value)} />
          {sc.displayName + (sc.isOptional ? "?" : "")}
        </th>
        <td>
          <div className={classes("sf-query-token")}>
            <QueryTokenEntityBuilder
              ctx={ctx.subCtx(a => a.token, { formGroupStyle: "None" })}
              queryKey={p.queryKey}
              subTokenOptions={subTokenOptions} onTokenChanged={() => p.onTokenChange()} />
          </div>
          <span style={{
            color: ctx.value.token == null ? "#ddd" :
              ChartClient.isChartColumnType(ctx.value.token.token, sc.columnType) ? "#52b980" : "#ff7575",
            marginLeft: "10px",
            cursor: "default"
          }} title={getTitle(sc.columnType, ctx.value.token?.token)}>
            {ChartColumnType.niceToString(sc.columnType)}
          </span>
          <a
            role="button"
            tabIndex={0}
            title={ChartMessage.ToggleInfo.niceToString()}
            aria-label={ChartMessage.ToggleInfo.niceToString()}
            className={classes("sf-chart-token-config-trigger", numParameters > 0 && ctx.value.token && "fw-bold")}
            onClick={handleExpanded}
            onKeyDown={e => {
              if (e.key === "Enter" || e.key === " ") {
                e.preventDefault();
                handleExpanded();
              }
            }}>
            {ChartMessage.ToggleInfo.niceToString()} {numParameters > 0 && ctx.value.token && <span>({numParameters})</span>}
          </a>
        </td>
      </tr>
      {expanded && <tr className="sf-chart-token-config">
        <td></td>
        <td colSpan={1}>
          <div>
            <div className="row">
              <div className="col-sm-3">
                <TextBoxLine ctx={ctxBasic.subCtx(a => a.displayName)} valueHtmlAttributes={{ onBlur: p.onRedraw, placeholder: ctx.value.token?.token?.niceName }} />
              </div>
              <div className="col-sm-3">
                <TextBoxLine ctx={ctxBasic.subCtx(a => a.format)} valueHtmlAttributes={{ onBlur: p.onRedraw, placeholder: ctx.value.token?.token?.format }} />
              </div>
              {getColorPalettes().map((t, i) =>
                <div className="col-sm-3" key={i}>
                  {t && !t.noSchema && < ChartPaletteLink ctx={ctxBasic} type={t} refresh={forceUpdate} />}
                </div>)
              }
            </div>
            <ColumnParameters chart={p.chartBase} chartScript={p.chartScript} columnIndex={p.columnIndex} parameterDic={p.parameterDic} onRedraw={p.onRedraw} />
          </div>
        </td>
      </tr>
      }
    </>
  );
}

function getTitle(ct: ChartColumnType, token: QueryToken | undefined): string {

  const group = expandGroup(ct);

  const tokenType = token && ChartClient.getChartColumnType(token);

  if (group != null)
    return ChartMessage.TheSelectedTokenShouldBeEither.niceToString() + "\n" +
      group.map(a => " - " + ChartColumnType.niceToString(a) + (a == tokenType ? " ✔" : "")).join("\n");


  return ChartMessage.TheSelectedTokenShouldBeA0.niceToString(ChartColumnType.niceToString(ct)) + (ct == tokenType ? " ✔" : "");

}


function expandGroup(ct: ChartColumnType): ChartColumnType[] | undefined {
  switch (ct) {
    case "AnyGroupKey": return ["String", "Entity", "Enum", "Date", "Number", "RoundedNumber"];
    case "AnyNumber": return ["Number", "DecimalNumber", "RoundedNumber"];
    case "AnyNumberDateTime": return ["Number", "DecimalNumber", "RoundedNumber", "Date", "DateTime"];
    default: return undefined;
  }
}

export interface ChartPaletteLinkProps {
  type: TypeInfo;
  refresh: () => void;
  ctx: StyleContext;
}

export function ChartPaletteLink(p: ChartPaletteLinkProps): React.JSX.Element {

  const [palette, reload] = useAPIWithReload(() => ColorPaletteClient.getColorPalette(p.type.name), [p.type.name]);

  return (
    <FormGroup ctx={p.ctx} label={ChartMessage.ColorsFor0.niceToString(p.type.niceName)}>
      {() => palette === undefined ?
        <span className={p.ctx.formControlPlainTextClass}>
          {JavascriptMessage.loading.niceToString()}
        </span> :
        <a href="#" className={p.ctx.formControlPlainTextClass} onClick={async e => {
          e.preventDefault();
          if (palette)
            await Navigator.view(palette.lite);
          else {
            var t = await Navigator.API.getType(p.type.name)
            await Navigator.view(ColorPaletteEntity.New({
              type: t!
            }));
          }

          reload();
        }}>
          {palette ? ChartMessage.ViewPalette.niceToString() : ChartMessage.CreatePalette.niceToString()}
        </a>
      }
    </FormGroup>    
  );
}



