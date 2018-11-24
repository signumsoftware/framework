import * as React from 'react'
import { classes } from '@framework/Globals'
import { SubTokensOptions } from '@framework/FindOptions'
import { TypeContext, StyleContext } from '@framework/TypeContext'
import { getTypeInfos, TypeInfo, isTypeEnum } from '@framework/Reflection'
import * as Navigator from '@framework/Navigator'
import { ValueLine, FormGroup } from '@framework/Lines'
import { ChartColumnEmbedded, IChartBase, ChartMessage, ChartColorEntity } from '../Signum.Entities.Chart'
import * as ChartClient from '../ChartClient'
import { ChartScriptColumn, ChartScript } from '../ChartClient'
import QueryTokenEntityBuilder from '../../UserAssets/Templates/QueryTokenEntityBuilder'

export interface ChartColumnProps {
  ctx: TypeContext<ChartColumnEmbedded>;
  scriptColumn: ChartScriptColumn;
  chartScript: ChartScript;
  chartBase: IChartBase;
  queryKey: string;
  colorPalettes: string[];
  onRedraw: () => void;
  onOrderChanged: (chartColumn: ChartColumnEmbedded, e: React.MouseEvent<any>) => void;
  onTokenChange: () => void;
}


export class ChartColumn extends React.Component<ChartColumnProps, { expanded: boolean }> {

  constructor(props: ChartColumnProps) {
    super(props);
    this.state = { expanded: false };
  }

  handleExpanded = () => {
    this.setState({ expanded: !this.state.expanded });
  }

  render() {

    const sc = this.props.scriptColumn;
    const cb = this.props.chartBase;

    const subTokenOptions = SubTokensOptions.CanElement | SubTokensOptions.CanAggregate;

    const ctx = this.props.ctx;

    return (
      <>
        <tr className="sf-chart-token">
          <th onClick={e => ctx.value.token && this.props.onOrderChanged(ctx.value, e)}
            style={{ whiteSpace: "nowrap", cursor: ctx.value.token ? "pointer" : undefined, userSelect: "none" }}>
            <span className={"sf-header-sort " + this.orderClassName(ctx.value)} />
            {sc.displayName + (sc.isOptional ? "?" : "")}
          </th>
          <td>
            <div className={classes("sf-query-token")}>
              <QueryTokenEntityBuilder
                ctx={ctx.subCtx(a => a.token, { formGroupStyle: "None" })}
                queryKey={this.props.queryKey}
                subTokenOptions={subTokenOptions} onTokenChanged={() => this.props.onTokenChange()} />
            </div>
            <a className="sf-chart-token-config-trigger" onClick={this.handleExpanded}>{ChartMessage.Chart_ToggleInfo.niceToString()} </a>
          </td>
        </tr>
        {this.state.expanded && <tr className="sf-chart-token-config">
          <td></td>
          <td colSpan={1}>
            <div>
              <div className="row">
                <div className="col-sm-4">
                  <ValueLine ctx={ctx.subCtx(a => a.displayName, { formSize: "Small", formGroupStyle: "Basic" })} onTextboxBlur={this.props.onRedraw} />
                </div>
                {this.getColorPalettes().map((t, i) =>
                  <div className="col-sm-4" key={i}>
                    <ChartPaletteLink ctx={ctx} type={t} currentPalettes={this.props.colorPalettes} />
                  </div>)
                }
              </div>
            </div>
          </td>
        </tr>
        }
      </>
    );
  }

  getColorPalettes() {
    const token = this.props.ctx.value.token;

    const t = token && token.token!.type;

    if (t == undefined || Navigator.isReadOnly(ChartColorEntity))
      return [];

    if (!t.isLite && !isTypeEnum(t.name))
      return [];

    return getTypeInfos(t);
  }

  orderClassName(c: ChartColumnEmbedded) {

    if (c.orderByType == null || c.orderByIndex == null)
      return "";

    return (c.orderByType == "Ascending" ? "asc" : "desc") + (" l" + c.orderByIndex);
  }
}

export interface ChartPaletteLinkProps {
  type: TypeInfo;
  currentPalettes: string[];
  ctx: StyleContext;
}

export const ChartPaletteLink = (props: ChartPaletteLinkProps) =>
  <FormGroup ctx={props.ctx as any}
    labelText={ChartMessage.ColorsFor0.niceToString(props.type.niceName)}>
    <a href={"/chartColors/" + props.type.name} className="form-control">
      {props.currentPalettes.contains(props.type.name) ? ChartMessage.ViewPalette.niceToString() : ChartMessage.CreatePalette.niceToString()}
    </a>
  </FormGroup>;



