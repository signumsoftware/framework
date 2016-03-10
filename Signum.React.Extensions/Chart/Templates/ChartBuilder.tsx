
import * as React from 'react'
import { DropdownButton, MenuItem, } from 'react-bootstrap'
import { Dic, classes } from '../../../../Framework/Signum.React/Scripts/Globals'
import * as Finder from '../../../../Framework/Signum.React/Scripts/Finder'
import { Lite, toLite } from '../../../../Framework/Signum.React/Scripts/Signum.Entities'
import { ResultTable, FindOptions, FilterOption, QueryDescription, SubTokensOptions, QueryToken, QueryTokenType } from '../../../../Framework/Signum.React/Scripts/FindOptions'
import { TypeContext, FormGroupSize, FormGroupStyle, StyleOptions, StyleContext, mlistItemContext } from '../../../../Framework/Signum.React/Scripts/TypeContext'
import { SearchMessage, JavascriptMessage, parseLite, is, liteKey } from '../../../../Framework/Signum.React/Scripts/Signum.Entities'
import * as Navigator from '../../../../Framework/Signum.React/Scripts/Navigator'
import { ValueLine, FormGroup, ValueLineProps, ValueLineType } from '../../../../Framework/Signum.React/Scripts/Lines'
import { ChartColumnEntity, ChartScriptColumnEntity, ChartScriptParameterEntity, IChartBase, GroupByChart, ChartMessage, ChartColorEntity_Type, ChartScriptEntity, ChartScriptEntity_Type, ChartParameterEntity, ChartParameterType } from '../Signum.Entities.Chart'
import * as ChartClient from '../ChartClient'
import QueryTokenEntityBuilder from '../../UserAssets/Templates/QueryTokenEntityBuilder'
import { ChartColumn, ChartColumnInfo }from './ChartColumn'

export interface ChartBuilderProps {
    ctx: TypeContext<IChartBase>; /*IChart*/
    queryKey: string;
    onInvalidate: () => void;
    onRedraw: () => void;
}

export interface ChartBuilderState {
    chartScripts?: ChartScriptEntity[][],
    expanded?: boolean[];
    colorPalettes?: string[];
}


export default class ChartBuilder extends React.Component<ChartBuilderProps, ChartBuilderState> {

    constructor(props) {
        super(props);

        this.state = { expanded: null };
    }

    componentWillMount() {

        ChartClient.getChartScripts().then(scripts =>
            this.setState({ chartScripts: scripts }))
            .done();

        ChartClient.getColorPalettes().then(colorPalettes =>
            this.setState({ colorPalettes: colorPalettes }))
            .done();

        var ctx = this.props.ctx;
        ChartClient.API.setChartScript(ctx.value, ctx.value.chartScript).then(() => {
            this.setState({ expanded: Array.repeat(ctx.value.columns.length, false) });
        }).done();
    }

    chartTypeImgClass(script: ChartScriptEntity): string {
        var cb = this.props.ctx.value;

        var css = "sf-chart-img";

        if (!cb.columns.some(a => a.element.token != null && a.element.token.parseException != null) && ChartClient.isCompatibleWith(script, cb))
            css += " sf-chart-img-equiv";

        if (is(cb.chartScript, script))
            css += " sf-chart-img-curr";

        return css;
    }

    handleOnToggleInfo = (index) => {
        this.state.expanded[index] = !this.state.expanded[index];
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

    handleChartScriptOnClick = (cs: ChartScriptEntity) => {

        var chart = this.props.ctx.value; 

        var isCompatible = ChartClient.isCompatibleWith(cs, chart);

        ChartClient.API.setChartScript(chart, cs)
            .then(() => {
                this.forceUpdate();

                if (isCompatible)
                    this.props.onRedraw();
                else
                    this.props.onInvalidate();

            }).done();
    }

    render() {

        var chart = this.props.ctx.value;

        return (
            <div className="row sf-chart-builder">
                <div className="col-lg-2">
                    <div className="sf-chart-type panel panel-default">
                        <div className="panel-heading">
                            <h3 className="panel-title">{ChartScriptEntity_Type.nicePluralName() }</h3>
                        </div>
                        <div className="panel-body">
                            {this.state.chartScripts && this.state.expanded && this.state.chartScripts.flatMap(a => a).map((cs, i) =>
                                <div key={i} className={this.chartTypeImgClass(cs) } title={cs.toStr + "\r\n" + cs.columnsStructure} onClick={() => this.handleChartScriptOnClick(cs)}>
                                    <img src={"data:image/jpeg;base64," + (cs.icon && cs.icon.entity && cs.icon.entity.binaryFile) }/>
                                </div>) }
                        </div>
                    </div>
                </div >
                <div className="col-lg-10">
                    <div className="sf-chart-tokens panel panel-default">
                        <div className="panel-heading">
                            <h3 className="panel-title">{ChartMessage.Chart_ChartSettings.niceToString() }</h3>
                        </div>
                        <div className="panel-body">
                            <table className="table" style={{ marginBottom: "0px" }}>
                                <thead>
                                    <tr>
                                        <th className="sf-chart-token-narrow">
                                            { ChartMessage.Chart_Dimension.niceToString() }
                                        </th>
                                        <th className="">
                                            { ChartMessage.Chart_Group.niceToString() }
                                        </th>
                                        <th className="sf-chart-token-wide">
                                            Token
                                        </th>
                                    </tr>
                                </thead>
                                <tbody>
                                    { this.state.expanded && mlistItemContext(this.props.ctx.subCtx(c => c.columns, { formGroupSize: FormGroupSize.ExtraSmall })).flatMap((ctx, i) => [
                                        <ChartColumn chartBase={chart} ctx={ctx} key={"C" + i} scriptColumn={chart.chartScript.columns[i].element} queryKey={this.props.queryKey}
                                            onToggleInfo={() => this.handleOnToggleInfo(i) } onInvalidate={this.handleOnInvalidate } />,
                                        this.state.expanded[i] && <ChartColumnInfo ctx= { ctx } key= { "CI" + i } colorPalettes={this.state.colorPalettes} onRedraw={this.handleOnRedraw} />
                                    ]) }
                                </tbody>
                            </table>
                        </div>
                    </div>
                    <div className="sf-chart-parameters panel panel-default">
                        <div className="panel-body form-vertical">
                            {
                                this.state.expanded && mlistItemContext(this.props.ctx.subCtx(c => c.parameters, { formGroupStyle: FormGroupStyle.Basic, formGroupSize: FormGroupSize.ExtraSmall }))
                                    .map((ctx, i) => this.getParameterValueLine(ctx, chart.chartScript.parameters[i].element))
                                    .groupsOf(6).map((gr, j) =>
                                        <div className="row" key={j}>
                                            {gr.map((vl, i) => <div className="col-sm-2" key={i}>{vl}</div>) }
                                        </div>)
                            }
                        </div>
                    </div>
                </div>
            </div >);
    }



    getParameterValueLine(ctx: TypeContext<ChartParameterEntity>, scriptParameter: ChartScriptParameterEntity) {

        var chart = this.props.ctx.value;

        var vl: ValueLineProps = {
            ctx: ctx.subCtx(a => a.value, { labelColumns: { sm: 6 } }),
            labelText: scriptParameter.name,
        };

        if (scriptParameter.type == ChartParameterType.Number || scriptParameter.type == ChartParameterType.String) {
            vl.valueLineType = ValueLineType.TextBox;
        }
        else if (scriptParameter.type == ChartParameterType.Enum) {
            vl.valueLineType = ValueLineType.Enum;

            var tokenEntity = scriptParameter.columnIndex == null ? null : chart.columns[scriptParameter.columnIndex].element.token;

            var compatible = scriptParameter.enumValues.filter(a => a.typeFilter == null || tokenEntity != null && ChartClient.isChartColumnType(tokenEntity.token, a.typeFilter));
            if (compatible.length <= 1)
                vl.ctx.styleOptions.readOnly = true;

            vl.comboBoxItems = compatible.map(ev => ({ name: ev.name, niceName: ev.name }));
        }

        vl.onChange = this.handleOnRedraw;

        return <ValueLine {...vl} />;
    }

}
