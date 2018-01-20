
import * as React from 'react'
import { Dic, classes } from '../../../../Framework/Signum.React/Scripts/Globals'
import * as Finder from '../../../../Framework/Signum.React/Scripts/Finder'
import { Lite, toLite } from '../../../../Framework/Signum.React/Scripts/Signum.Entities'
import { ResultTable, FindOptions, FilterOption, QueryDescription, SubTokensOptions, QueryToken } from '../../../../Framework/Signum.React/Scripts/FindOptions'
import { TypeContext, FormGroupStyle, StyleOptions, StyleContext } from '../../../../Framework/Signum.React/Scripts/TypeContext'
import { SearchMessage, JavascriptMessage, parseLite, is } from '../../../../Framework/Signum.React/Scripts/Signum.Entities'
import { getTypeInfos, TypeInfo, isTypeEnum } from '../../../../Framework/Signum.React/Scripts/Reflection'
import * as Navigator from '../../../../Framework/Signum.React/Scripts/Navigator'
import { ValueLine, FormGroup } from '../../../../Framework/Signum.React/Scripts/Lines'
import { ChartColumnEmbedded, ChartScriptColumnEmbedded, IChartBase, GroupByChart, ChartMessage, ChartColorEntity } from '../Signum.Entities.Chart'
import * as ChartClient from '../ChartClient'
import QueryTokenEntityBuilder from '../../UserAssets/Templates/QueryTokenEntityBuilder'

export interface ChartColumnProps {
    ctx: TypeContext<ChartColumnEmbedded>;
    scriptColumn: ChartScriptColumnEmbedded;
    chartBase: IChartBase;
    queryKey: string;
    onToggleInfo: () => void;
    onTokenChange: () => void;
    onGroupChange: () => void;
}


export class ChartColumn extends React.Component<ChartColumnProps, {}> {

    constructor(props: ChartColumnProps) {
        super(props);
    }

    handleExpanded = () => {
        this.props.onToggleInfo();
    }

    handleGroupChecked = (e: React.FormEvent<any>) => {

        this.props.chartBase.groupResults = (e.currentTarget as HTMLInputElement).checked;
        ChartClient.synchronizeColumns(this.props.chartBase);

        this.props.onGroupChange();
    }

    render() {

        const sc = this.props.scriptColumn;
        const cb = this.props.chartBase;

        const groupVisible = this.props.chartBase.chartScript.groupBy != "Never" && sc.isGroupKey;

        const groupResults = cb.groupResults == undefined ? true : cb.groupResults;

        const subTokenOptions = SubTokensOptions.CanElement | (groupResults && !sc.isGroupKey ? SubTokensOptions.CanAggregate : 0)

        return (
            <tr className="sf-chart-token">
                <th>{sc.displayName + (sc.isOptional ? "?" : "")}</th>
                <td style={{ textAlign: "center" }}>
                    {groupVisible && <input type="checkbox" checked={cb.groupResults} className="sf-chart-group-trigger" disabled={cb.chartScript.groupBy == "Always"} onChange={this.handleGroupChecked} />}
                </td>
                <td>
                    <div className={classes("sf-query-token")}>
                        <QueryTokenEntityBuilder
                            ctx={this.props.ctx.subCtx(a => a.token, { formGroupStyle: "None" })}
                            queryKey={this.props.queryKey}
                            subTokenOptions={subTokenOptions} onTokenChanged={() => this.props.onTokenChange()} />
                    </div>
                    <a className="sf-chart-token-config-trigger" onClick={this.handleExpanded}>{ChartMessage.Chart_ToggleInfo.niceToString()} </a>
                </td>
            </tr>
        );
    }
}


export interface ChartColumnInfoProps {
    ctx: TypeContext<ChartColumnEmbedded>;
    onRedraw: () => void;
    colorPalettes: string[];
}

export class ChartColumnInfo extends React.Component<ChartColumnInfoProps> {

    getColorPalettes() {
        const token = this.props.ctx.value.token;

        const t = token && token.token!.type;

        if (t == undefined || Navigator.isReadOnly(ChartColorEntity))
            return [];

        if (!t.isLite && !isTypeEnum(t.name))
            return [];

        return getTypeInfos(t);
    }

    render() {

        const ctx = this.props.ctx.subCtx({ formSize: "Small", formGroupStyle: "Basic" });



        return (
            <tr className="sf-chart-token-config">
                <td></td>
                <td></td>
                <td colSpan={1}>
                    <div>
                        <div className="row">
                            <div className="col-sm-4">
                                <ValueLine ctx={ctx.subCtx(a => a.displayName)} onTextboxBlur={this.props.onRedraw} />
                            </div>
                            {this.getColorPalettes().map((t, i) =>
                                <div className="col-sm-4" key={i}>
                                    <ChartLink ctx={this.props.ctx} type={t} currentPalettes={this.props.colorPalettes} />
                                </div>)
                            }
                        </div>
                    </div>
                </td>
            </tr>
        );
    }


}


export interface ChartLinkProps {
    type: TypeInfo;
    currentPalettes: string[];
    ctx: StyleContext;
}

export const ChartLink = (props: ChartLinkProps) =>
    <FormGroup ctx={props.ctx as any}
        labelText={ChartMessage.ColorsFor0.niceToString(props.type.niceName)}>
        <a href={"/chartColors/" + props.type.name} className="form-control">
            {props.currentPalettes.contains(props.type.name) ? ChartMessage.ViewPalette.niceToString() : ChartMessage.CreatePalette.niceToString()}
        </a>
    </FormGroup>;



