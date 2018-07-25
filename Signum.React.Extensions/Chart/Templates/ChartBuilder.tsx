
import * as React from 'react'
import { Dic, classes } from '@framework/Globals'
import * as Finder from '@framework/Finder'
import { Lite, toLite } from '@framework/Signum.Entities'
import { ResultTable, FindOptions, FilterOption, QueryDescription, SubTokensOptions, QueryToken, QueryTokenType } from '@framework/FindOptions'
import { TypeContext, FormGroupStyle, StyleOptions, StyleContext, mlistItemContext } from '@framework/TypeContext'
import { SearchMessage, JavascriptMessage, parseLite, is, liteKey } from '@framework/Signum.Entities'
import * as Navigator from '@framework/Navigator'
import { ValueLine, FormGroup, ValueLineProps, ValueLineType, OptionItem } from '@framework/Lines'
import { ChartColumnEmbedded, ChartScriptColumnEmbedded, ChartScriptParameterEmbedded, IChartBase, GroupByChart, ChartMessage, ChartColorEntity, ChartScriptEntity, ChartParameterEmbedded, ChartParameterType } from '../Signum.Entities.Chart'
import * as ChartClient from '../ChartClient'
import QueryTokenEntityBuilder from '../../UserAssets/Templates/QueryTokenEntityBuilder'
import { ChartColumn, ChartColumnInfo } from './ChartColumn'

export interface ChartBuilderProps {
    ctx: TypeContext<IChartBase>; /*IChart*/
    queryKey: string;
    onInvalidate: () => void;
    onTokenChange: () => void;
    onRedraw: () => void;
}

export interface ChartBuilderState {
    chartScripts?: ChartScriptEntity[][],
    expanded?: boolean[];
    colorPalettes?: string[];
}


export default class ChartBuilder extends React.Component<ChartBuilderProps, ChartBuilderState> {

    constructor(props: ChartBuilderProps) {
        super(props);

        this.state = { expanded: undefined };
    }

    componentWillMount() {

        ChartClient.getChartScripts().then(scripts =>
            this.setState({ chartScripts: scripts }))
            .done();

        ChartClient.getColorPalettes().then(colorPalettes =>
            this.setState({ colorPalettes: colorPalettes }))
            .done();

        const ctx = this.props.ctx;

        ChartClient.synchronizeColumns(ctx.value);
        this.setState({ expanded: Array.repeat(ctx.value.columns.length, false) });
    }

    chartTypeImgClass(script: ChartScriptEntity): string {
        const cb = this.props.ctx.value;

        let css = "sf-chart-img";

        if (!cb.columns.some(a => a.element.token != undefined && a.element.token.parseException != undefined) && ChartClient.isCompatibleWith(script, cb))
            css += " sf-chart-img-equiv";

        if (is(cb.chartScript, script)) {

            css += " sf-chart-img-curr";

            if (cb.chartScript!.script != script.script)
                css += " edited";
        }

        return css;
    }

    handleOnToggleInfo = (index: number) => {
        this.state.expanded![index] = !this.state.expanded![index];
        this.forceUpdate();
    }

    handleOnRedraw = () => {
        this.forceUpdate();
        this.props.onRedraw();
    }

    handleOnInvalidate = () => {
        this.forceUpdate();
        this.props.onInvalidate();
    }

    handleTokenChange = (cc: ChartColumnEmbedded) => {
        cc.displayName = undefined;
        this.forceUpdate();
        this.props.onTokenChange();
    }

    handleChartScriptOnClick = (cs: ChartScriptEntity) => {

        const chart = this.props.ctx.value;
        let compatible = ChartClient.isCompatibleWith(cs, chart)
        chart.chartScript = cs;
        ChartClient.synchronizeColumns(chart);
        chart.modified = true;

        if (!compatible)
            this.props.onInvalidate();
        else
            this.props.onRedraw();
    }

    render() {

        const chart = this.props.ctx.value;

        return (
            <div className="row sf-chart-builder">
                <div className="col-lg-2">
                    <div className="sf-chart-type card">
                        <div className="card-header">
                            <h6 className="card-title mb-0">{ChartScriptEntity.nicePluralName()}</h6>
                        </div>
                        <div className="card-body">
                            {this.state.chartScripts && this.state.expanded && this.state.chartScripts.flatMap(a => a).map((cs, i) =>
                                <div key={i} className={this.chartTypeImgClass(cs)} title={cs.toStr + "\r\n" + cs.columnsStructure} onClick={() => this.handleChartScriptOnClick(cs)}>
                                    <img src={"data:image/jpeg;base64," + (cs.icon && cs.icon.entity && cs.icon.entity.binaryFile)} />
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
                                        <th className="">
                                            {ChartMessage.Chart_Group.niceToString()}
                                        </th>
                                        <th className="sf-chart-token-wide">
                                            Token
                                        </th>
                                    </tr>
                                </thead>
                                <tbody>
                                    {this.state.expanded && mlistItemContext(this.props.ctx.subCtx(c => c.columns, { formSize: "ExtraSmall" })).flatMap((ctx, i) => [
                                        <ChartColumn chartBase={chart} ctx={ctx} key={"C" + i} scriptColumn={chart.chartScript!.columns[i].element} queryKey={this.props.queryKey}
                                            onToggleInfo={() => this.handleOnToggleInfo(i)} onGroupChange={this.handleOnInvalidate} onTokenChange={() => this.handleTokenChange(ctx.value)} />,
                                        this.state.expanded![i] && this.state.colorPalettes && <ChartColumnInfo ctx={ctx} key={"CI" + i} colorPalettes={this.state.colorPalettes} onRedraw={this.handleOnRedraw} />
                                    ])}
                                </tbody>
                            </table>
                        </div>
                    </div>
                    <fieldset className="sf-chart-parameters">
                        {
                            this.state.expanded && mlistItemContext(this.props.ctx.subCtx(c => c.parameters, { formSize: "ExtraSmall", formGroupStyle: "Basic" }))
                                .map((ctx, i) => this.getParameterValueLine(ctx, chart.chartScript.parameters[i].element))
                                .groupsOf(6).map((gr, j) =>
                                    <div className="row" key={j}>
                                        {gr.map((vl, i) => <div className="col-sm-2" key={i}>{vl}</div>)}
                                    </div>)
                        }
                    </fieldset>
                </div>
            </div >);
    }



    getParameterValueLine(ctx: TypeContext<ChartParameterEmbedded>, scriptParameter: ChartScriptParameterEmbedded) {

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

            const compatible = scriptParameter.enumValues.filter(a => a.typeFilter == undefined || tokenEntity == undefined || ChartClient.isChartColumnType(tokenEntity.token, a.typeFilter));
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
