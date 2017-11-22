import { PredictorEntity } from "MachineLearning/Signum.Entities.MachineLearning";
import { Modal } from "react-bootstrap";
import * as React from "react";
import * as Navigator from "../../../../Framework/Signum.React/Scripts/Navigator";
import { IModalProps, openModal } from "../../../../Framework/Signum.React/Scripts/Modals";
import { API, PredictRequest, PredictColumn, PredictOutputTuple, PredictSubQueryHeader, PredictSubQueryTable } from "MachineLearning/PredictorClient";
import { Lite, Entity, NormalControlMessage, EntityControlMessage } from "../../../../Framework/Signum.React/Scripts/Signum.Entities";
import { StyleContext, FormGroup } from "../../../../Framework/Signum.React/Scripts/Lines";
import { QueryToken } from "../../../../Framework/Signum.React/Scripts/FindOptions";


interface PredictModalProps extends IModalProps {
    originalPredict: PredictRequest;
    entity?: Lite<Entity>;
}

interface PredictModalState {
    show: boolean;
    predict: PredictRequest;
}

export class PredictModal extends React.Component<PredictModalProps, PredictModalState> {

    constructor(props: PredictModalProps) {
        super(props);
        this.state = { show: true, predict: props.originalPredict };
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

        return (
            <Modal onHide={this.handleClosedClicked} onExited={this.handleOnExited}
                show={this.state.show} className="message-modal">
                <Modal.Header closeButton={true}>
                    <h4 className={"modal-title"}>
                        {p.predictor.toStr}
                        <small>{PredictorEntity.niceName()}&nbsp;{p.predictor.id} (<a href={Navigator.navigateRoute(p.predictor)} target="_blank">{EntityControlMessage.View.niceToString()}</a>)</small>
                        {e && (<a href={Navigator.navigateRoute(e)} target="_blank">{e.toStr}</a>)}
                    </h4>
                </Modal.Header>
                <Modal.Body>
                    {p.columns.map((c, i) => <PredictLine key={i} ctx={} />)}
                </Modal.Body>
                <Modal.Footer>
                    <div>
                        <button
                            className="btn btn-primary sf-close-button sf-ok-button"
                            onClick={() => this.handlePredictClick(true)}
                            name="accept">
                            Predict
                        </button>
                    </div>
                </Modal.Footer>
            </Modal>
        );
    }

    static show(predict: PredictRequest, entity: Lite<Entity>): Promise<void> {
        return openModal<undefined>(<PredictModal predict={predict} entity={entity} />);
    }
}


interface PredictLineProps {
    column: PredictColumn;
    ctx: StyleContext;
}

export default class PredictLine extends React.Component<PredictLineProps> {
    render() {
        const ctx = this.props.ctx;
        const col = this.props.column;
        return (
            <FormGroup ctx={ctx} labelText={col.token.niceName} labelHtmlAttributes={{ title: fullNiceName(col.token) }}>
            </FormGroup>
        );
    }

    
}

function fullNiceName(token: QueryToken): string {

    var rec = (token.parent ? fullNiceName(token.parent) + "." : "");
    
    return rec + `[${token.niceName}]`;
}


