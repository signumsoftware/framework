import { PredictorEntity } from "../Signum.Entities.MachineLearning";
import { Modal } from "react-bootstrap";
import * as React from "react";
import * as Navigator from "../../../../Framework/Signum.React/Scripts/Navigator";
import { IModalProps, openModal } from "../../../../Framework/Signum.React/Scripts/Modals";
import { API, PredictRequest, PredictColumn, PredictOutputTuple, PredictSubQueryHeader, PredictSubQueryTable } from "../PredictorClient";
import { Lite, Entity, NormalControlMessage, EntityControlMessage } from "../../../../Framework/Signum.React/Scripts/Signum.Entities";
import { StyleContext, FormGroup, TypeContext, EntityLine, EntityCombo, ValueLine } from "../../../../Framework/Signum.React/Scripts/Lines";
import { QueryToken } from "../../../../Framework/Signum.React/Scripts/FindOptions";
import { getTypeInfos } from "../../../../Framework/Signum.React/Scripts/Reflection";
import { IsByAll } from "../../../../Framework/Signum.React/Scripts/Reflection";
import { Dic } from "../../../../Framework/Signum.React/Scripts/Globals";
import { PropertyRoute } from "../../../../Framework/Signum.React/Scripts/Reflection";
import { Binding } from "../../../../Framework/Signum.React/Scripts/Reflection";
import { is } from "../../../../Framework/Signum.React/Scripts/Signum.Entities";
import { isLite } from "../../../../Framework/Signum.React/Scripts/Signum.Entities";


interface PredictModalProps extends IModalProps {
    initialPredict: PredictRequest;
    entity?: Lite<Entity>;
}

interface PredictModalState {
    show: boolean;
    predict: PredictRequest;
}

export class PredictModal extends React.Component<PredictModalProps, PredictModalState> {

    constructor(props: PredictModalProps) {
        super(props);
        this.state = { show: true, predict: props.initialPredict };
    }

    handleClosedClicked = () => {
        this.setState({ show: false });
    }

    handleOnExited = () => {
        this.props.onExited!(undefined);
    }

    handlePredictClick = () => {
        API.updatePredict(this.state.predict)
            .then(predict => this.setState({ predict: predict }))
            .done();
    }

    render() {

        const p = this.state.predict;
        var e = this.props.entity;

        var sctx = new StyleContext(undefined, {});

        return (
            <Modal onHide={this.handleClosedClicked} onExited={this.handleOnExited}
                show={this.state.show} className="message-modal">
                <Modal.Header closeButton={true}>
                    <h4 className={"modal-title"}>
                        {e && (<a href={Navigator.navigateRoute(e)} target="_blank" style={{ float: "right", marginRight: "20px" }}>{e.toStr}</a>)}
                        {p.predictor.toStr}<br />
                        <small>{PredictorEntity.niceName()}&nbsp;{p.predictor.id} (<a href={Navigator.navigateRoute(p.predictor)} target="_blank">{EntityControlMessage.View.niceToString()}</a>)</small>
                    </h4>
                </Modal.Header>
                <Modal.Body>
                    <div className="form-horizontal">
                        {p.columns.filter(c => c.usage == "Input").map((c, i) => <PredictLine key={i} sctx={sctx} hasOriginal={p.hasOriginal} column={c} onChange={this.handlePredictClick} />)}
                    </div>
                    {p.subQueries.map((c, i) => <PredictTable key={i} sctx={sctx} table={c} onChange={this.handlePredictClick}/>)}
                    <div className="form-horizontal">
                        {p.columns.filter(c => c.usage == "Output").map((c, i) => <PredictLine key={i} sctx={sctx} hasOriginal={p.hasOriginal} column={c} onChange={this.handlePredictClick} />)}
                    </div>
                </Modal.Body>
            </Modal>
        );
    }

    static show(predict: PredictRequest, entity: Lite<Entity> | undefined): Promise<void> {
        return openModal<undefined>(<PredictModal initialPredict={predict} entity={entity} />);
    }
}

interface PredictLineProps {
    column: PredictColumn;
    sctx: StyleContext;
    hasOriginal: boolean;
    onChange: () => void;
}

export default class PredictLine extends React.Component<PredictLineProps> {
    render() {
        const column = this.props.column;
        if (column.usage == "Output" && this.props.hasOriginal) {

            var tuple = this.props.column.value as PredictOutputTuple;

            const pctx = new TypeContext<any>(this.props.sctx, { readOnly: true }, undefined as any, Binding.create(tuple, a => a.predicted));
            const octx = new TypeContext<any>(this.props.sctx, { readOnly: true }, undefined as any, Binding.create(tuple, a => a.original));

            var color = pctx.value == octx.value || isLite(pctx.value) && isLite(octx.value) && is(pctx.value, octx.value) ? "green" : "red";

            return (
                <FormGroup ctx={this.props.sctx} labelText={column.token.niceName} labelHtmlAttributes={{ title: fullNiceName(column.token) }}>
                    <PredictValue token={column.token} ctx={pctx} label={<i className="fa fa-lightbulb-o" style={{ color }}></i>} />
                    <PredictValue token={column.token} ctx={octx} label={<i className="fa fa-bullseye" style={{ color }}></i>} />
                </FormGroup>
            );
        } else {
            const ctx = new TypeContext<any>(this.props.sctx, { readOnly: column.usage == "Output" }, undefined as any, Binding.create(column, a => a.value));
            return (
                <FormGroup ctx={ctx} labelText={column.token.niceName} labelHtmlAttributes={{ title: fullNiceName(column.token) }}>
                    <PredictValue token={column.token} ctx={ctx} label={column.usage == "Output" ? <i className="fa fa-lightbulb-o"></i> : undefined} onChange={this.props.onChange} />
                </FormGroup>
            );
        }
    }
}


interface PredictTableProps {
    sctx: StyleContext;
    table: PredictSubQueryTable;
    onChange: () => void;
}

export class PredictTable extends React.Component<PredictTableProps> {
    render() {
        var { subQuery, columnHeaders, rows } = this.props.table;
        return (
            <div>
                <h4>{subQuery.toStr}</h4>
                <div style={{ maxHeight: "500px", overflowY: "scroll", marginBottom: "10px" }}>
                    <table className="table table-condensed">
                        <thead>
                            <tr>
                                {columnHeaders.map((he, i) => <th key={i} className={"header-" + he.headerType.toLowerCase()}>
                                    {he.headerType == "Key" && <i className="fa fa-key" style={{ marginRight: "10px" }}></i>}
                                    {he.token.niceName}
                                </th>)}
                            </tr>
                        </thead>
                        <tbody>
                            {
                                rows.map((row, j) => <tr key={j}>
                                    {row.map((v, i) => {
                                        var ch = columnHeaders[i];
                                        const ctx = new TypeContext<any>(this.props.sctx, { readOnly: ch.headerType == "Key" || ch.headerType == "Output" }, undefined as any, new Binding(row, i));
                                        return <td><PredictValue ctx={ctx} token={ch.token} onChange={this.props.onChange} /></td>;
                                    })}
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

        switch (token.filterType) {
            case "Lite":
                if (token.type.name == IsByAll || getTypeInfos(token.type).some(ti => !ti.isLowPopulation))
                    return <EntityLine ctx={ctx} type={token.type} create={false} labelText={label} onChange={this.handleValueChange} />;
                else
                    return <EntityCombo ctx={ctx} type={token.type} create={false} labelText={label} onChange={this.handleValueChange} />
            case "Enum":
                const ti = getTypeInfos(token.type).single();
                if (!ti)
                    throw new Error(`EnumType ${token.type.name} not found`);
                const members = Dic.getValues(ti.members).filter(a => !a.isIgnoredEnum);
                return <ValueLine ctx={ctx} type={token.type} formatText={token.format} unitText={token.unit} comboBoxItems={members} labelText={label} onChange={this.handleValueChange} />;
            default:
                return <ValueLine ctx={ctx} type={token.type} formatText={token.format} unitText={token.unit} labelText={label} onChange={this.handleValueChange} />;
        }
    }
}








