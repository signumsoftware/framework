import { PredictorEntity, PredictorState } from "../Signum.Entities.MachineLearning";
import * as React from "react";
import * as numbro from "numbro";
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome'
import * as Navigator from "@framework/Navigator";
import { IModalProps, openModal } from "@framework/Modals";
import { API, PredictRequest, PredictColumn, PredictOutputTuple, PredictSubQueryHeader, PredictSubQueryTable, AlternativePrediction } from "../PredictorClient";
import { Lite, Entity, NormalControlMessage, EntityControlMessage } from "@framework/Signum.Entities";
import { StyleContext, FormGroup, TypeContext, EntityLine, EntityCombo, ValueLine } from "@framework/Lines";
import { QueryToken } from "@framework/FindOptions";
import { getTypeInfos, ReadonlyBinding } from "@framework/Reflection";
import { IsByAll } from "@framework/Reflection";
import { Dic } from "@framework/Globals";
import { PropertyRoute } from "@framework/Reflection";
import { Binding } from "@framework/Reflection";
import { is } from "@framework/Signum.Entities";
import { isLite } from "@framework/Signum.Entities";
import { Modal } from "@framework/Components";
import { ModalHeaderButtons } from "@framework/Components/Modal";
import { NumericTextBox } from "@framework/Lines/ValueLine";
import { AbortableRequest } from "@framework/Services";


interface PredictModalProps extends IModalProps {
    initialPredict: PredictRequest;
    isClassification: boolean;
    entity?: Lite<Entity>;
}

interface PredictModalState {
    show: boolean;
    predict: PredictRequest;
    hasChanged: boolean;
}

export class PredictModal extends React.Component<PredictModalProps, PredictModalState> {

    constructor(props: PredictModalProps) {
        super(props);
        this.state = { show: true, predict: props.initialPredict, hasChanged: false };
    }

    handleOnClose = () => {
        this.setState({ show: false });
    }

    handleOnExited = () => {
        this.props.onExited!(undefined);
    }

    abortableUpdateRequest = new AbortableRequest((abortController, request: PredictRequest) => API.updatePredict(request));

    hangleOnChange = () => {
        this.setState({ hasChanged: true });
        this.abortableUpdateRequest.getData(this.state.predict)
            .then(predict => {
                this.setState({ predict: predict });
            })
            .done();
    }

    componentWillUnmount() {
        this.abortableUpdateRequest.abort();
    }

    render() {

        const p = this.state.predict;
        var e = this.props.entity;
   
        const hasChanged = this.state.hasChanged;

        var sctx = new StyleContext(undefined, {});

        return (
            <Modal onHide={this.handleOnClose} onExited={this.handleOnExited} show={this.state.show} className="message-modal" size="lg">
                <ModalHeaderButtons onClose={this.handleOnClose}>
                    <h4 className={"modal-title"}>
                        {e && (<a href={Navigator.navigateRoute(e)} target="_blank" style={{ float: "right", marginRight: "20px" }}>{e.toStr}</a>)}
                        {p.predictor.toStr}<br />
                        <small>{PredictorEntity.niceName()}&nbsp;{p.predictor.id} (<a href={Navigator.navigateRoute(p.predictor)} target="_blank">{EntityControlMessage.View.niceToString()}</a>)</small>
                    </h4>
                </ModalHeaderButtons>
                <div className="modal-body">
                    <div>
                        {p.columns.filter(c => c.usage == "Input").map((col, i) =>
                            <PredictLine key={i} sctx={sctx} hasOriginal={p.hasOriginal} hasChanged={hasChanged} binding={Binding.create(col, c => c.value)} usage={col.usage} token={col.token} onChange={this.hangleOnChange} />)}
                    </div>
                    {p.subQueries.map((c, i) => <PredictTable key={i} sctx={sctx} table={c} onChange={this.hangleOnChange} hasChanged={hasChanged} hasOriginal={p.hasOriginal} />)}
                    <div>
                        {p.columns.filter(c => c.usage == "Output").map((col, i) =>
                            <PredictLine key={i} sctx={sctx} hasOriginal={p.hasOriginal} hasChanged={hasChanged} binding={Binding.create(col, c => c.value)} usage={col.usage} token={col.token} onChange={this.hangleOnChange} />)}
                    </div>
                    {this.props.isClassification && <AlternativesCheckBox binding={Binding.create(p, p2 => p2.alternativesCount)} onChange={this.hangleOnChange} />}
                </div>
            </Modal>
        );
    }

    static show(predict: PredictRequest, entity: Lite<Entity> | undefined, isClassification: boolean): Promise<void> {
        return openModal<undefined>(<PredictModal initialPredict={predict} entity={entity} isClassification={isClassification} />);
    }
}


export class AlternativesCheckBox extends React.Component<{ binding: Binding<number | null>, onChange : ()=> void }> {
    render() {
        var val = this.props.binding.getValue();
        return (
            <label><input type="checkbox" checked={val != null} onChange={() => this.setValue(val == null ? 5 : null)} /> Show <NumericTextBox value={val} onChange={n => this.setValue(n)} validateKey={ValueLine.isNumber} /> alternative predictions </label>
        );
    }

    setValue(val: number | null) {
        this.props.binding.setValue(val);
        this.props.onChange();
    }
}



interface PredictLineProps {
    binding: Binding<any>;
    token: QueryToken;
    usage: "Key" | "Input" | "Output"
    sctx: StyleContext;
    hasOriginal: boolean;
    hasChanged: boolean;
    onChange: () => void;
}

export default class PredictLine extends React.Component<PredictLineProps> {

    render() {
        const p = this.props;
        return (
            <FormGroup ctx={this.props.sctx} labelText={p.token.niceName} labelHtmlAttributes={{ title: fullNiceName(p.token) }}>
                {this.renderValue()}
            </FormGroup>
        );
    }

    renderValue() {
        const p = this.props;
        if (p.usage == "Output") {
            if (this.props.hasOriginal) {
                var tuple = p.binding.getValue() as PredictOutputTuple;

                const octx = new TypeContext<any>(this.props.sctx, { readOnly: true }, undefined as any, Binding.create(tuple, a => a.original));
                const pctx = new TypeContext<any>(this.props.sctx, { readOnly: true }, undefined as any, Binding.create(tuple, a => a.predicted));
                
                return (
                    <div>
                        <div style={{ opacity: this.props.hasChanged ? 0.5 : 1 }}>
                            <PredictValue token={p.token} ctx={octx} label={<FontAwesomeIcon icon="bullseye" />} />
                        </div>
                        {this.renderValueOrMultivalue(pctx, octx.value)}
                    </div>
                );
            }
            else {
                const ctx = new TypeContext<any>(this.props.sctx, { readOnly: true }, undefined as any, p.binding);
                return this.renderValueOrMultivalue(ctx, null);

            }
        } else if (p.usage == "Input") {
            const ctx = new TypeContext<any>(this.props.sctx, undefined, undefined as any, p.binding);
            return (<PredictValue token={p.token} ctx={ctx} onChange={this.props.onChange}/>);
        } else throw new Error("unexpected Usage");
    }

    renderValueOrMultivalue(pctx: TypeContext<any>, originalValue: any) {
        if (!Array.isArray(pctx.value)) {
            return <PredictValue token={this.props.token} ctx={pctx} label={<FontAwesomeIcon icon={["far", "lightbulb"]} color={this.getColor(pctx.value, originalValue)}/>} />
        } else {
            const predictions = pctx.value as AlternativePrediction[];

            return (
                <div>
                    {predictions.map((a, i) => <PredictValue key={i} token={this.props.token}
                        ctx={new TypeContext<any>(this.props.sctx, { readOnly: true }, undefined as any, new ReadonlyBinding(a.Value, this.props.sctx + "_" + i))}
                        label={<i style={{ color: this.getColor(a.Value, originalValue) }}>{numbro(a.Probability).format("0.00 %")}</i>}
                        labelHtmlAttributes={{ style: { textAlign: "right" } }}
                    />)}
                </div>
            );
        }
    }

    getColor(predicted: any, original : any) {
        return !this.props.hasOriginal ? undefined :
            predicted == original || isLite(predicted) && isLite(original) && is(predicted, original) ? "green" : "red";
    }
}


interface PredictTableProps {
    sctx: StyleContext;
    table: PredictSubQueryTable;
    hasChanged: boolean;
    hasOriginal: boolean;
    onChange: () => void;
}

export class PredictTable extends React.Component<PredictTableProps> {
    render() {
        var p = this.props;
        var { subQuery, columnHeaders, rows } = this.props.table;
        var sctx = new StyleContext(this.props.sctx, { formGroupStyle: "SrOnly" });
        return (
            <div>
                <h4>{subQuery.toStr}</h4>
                <div style={{ maxHeight: "500px", overflowY: "scroll", marginBottom: "10px" }}>
                    <table className="table table-sm">
                        <thead>
                            <tr >
                                {
                                    columnHeaders.map((he, i) => <th key={i} className={"header-" + he.headerType.toLowerCase()} title={fullNiceName(he.token)}>
                                        {he.headerType == "Key" && <FontAwesomeIcon icon="key" style={{ marginRight: "10px" }}/>}
                                        {he.token.niceName}
                                    </th>)
                                }
                            </tr>
                        </thead>
                        <tbody>
                            {
                                rows.map((row, j) => <tr key={j}>
                                    {
                                        row.map((v, i) => {
                                            var ch = columnHeaders[i];
                                            return (
                                                <td>
                                                    <PredictLine sctx={sctx} token={ch.token} binding={new Binding(row, i)}
                                                        usage={ch.headerType} hasChanged={p.hasChanged} hasOriginal={p.hasOriginal} onChange={this.props.onChange} />
                                                </td>
                                            );
                                        })
                                    }
                                </tr>)
                            }
                        </tbody>
                    </table>
                </div>
            </div>
        );
    }
}


function fullNiceName(token: QueryToken): string {

    var rec = (token.parent ? fullNiceName(token.parent) + "." : "");

    return rec + `[${token.niceName}]`;
}

interface PredictValueProps {
    token: QueryToken;
    ctx: TypeContext<any>;
    onChange?: () => void;
    label?: React.ReactElement<any>;
    labelHtmlAttributes?: React.LabelHTMLAttributes<HTMLLabelElement>;
}

export class PredictValue extends React.Component<PredictValueProps> {

    handleValueChange = () => {
        if (this.props.onChange)
            this.props.onChange();
    }

    render() {

        const ctx = this.props.ctx.subCtx({ labelColumns: 1 });
        const token = this.props.token;
        const label = this.props.label;
        const lha = this.props.labelHtmlAttributes;

        switch (token.filterType) {
            case "Lite":
                if (token.type.name == IsByAll || getTypeInfos(token.type).some(ti => !ti.isLowPopulation))
                    return <EntityLine ctx={ctx} type={token.type} create={false} labelText={label} labelHtmlAttributes={lha} onChange={this.handleValueChange} />;
                else
                    return <EntityCombo ctx={ctx} type={token.type} create={false} labelText={label} labelHtmlAttributes={lha} onChange={this.handleValueChange} />
            case "Enum":
                const ti = getTypeInfos(token.type).single();
                if (!ti)
                    throw new Error(`EnumType ${token.type.name} not found`);
                const members = Dic.getValues(ti.members).filter(a => !a.isIgnoredEnum);
                return <ValueLine ctx={ctx} type={token.type} formatText={token.format} unitText={token.unit} labelHtmlAttributes={lha} labelText={label} onChange={this.handleValueChange} comboBoxItems={members} />;
            default:
                return <ValueLine ctx={ctx} type={token.type} formatText={token.format} unitText={token.unit} labelHtmlAttributes={lha} labelText={label} onChange={this.handleValueChange} />;
        }
    }
}








