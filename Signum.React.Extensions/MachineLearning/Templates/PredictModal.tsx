import { PredictorEntity } from "MachineLearning/Signum.Entities.MachineLearning";
import { Modal } from "react-bootstrap";
import * as React from "react";
import * as Navigator from "../../../../Framework/Signum.React/Scripts/Navigator";
import { IModalProps, openModal } from "../../../../Framework/Signum.React/Scripts/Modals";
import { Lite } from "../../../../Framework/Signum.React/Scripts/Signum.Entities";
import { PredictRequest } from "MachineLearning/PredictorClient";
import { Entity } from "../../../../Framework/Signum.React/Scripts/Signum.Entities";
import { NormalControlMessage } from "../../../../Framework/Signum.React/Scripts/Signum.Entities";
import { EntityControlMessage } from "../../../../Framework/Signum.React/Scripts/Signum.Entities";


interface PredictModalProps extends IModalProps {
    predict: PredictRequest;
    entity?: Lite<Entity>;
}

interface PredictModalState {
    show: boolean;
}

class PredictModal extends React.Component<PredictModalProps, PredictModalState> {

    constructor(props: PredictModalProps) {
        super(props);
        this.state = { show: true };
    }

    handleClosedClicked = () => {
        this.setState({ show: false });
    }

    handleOnExited = () => {
        this.props.onExited!(undefined);
    }



    render() {

        const p = this.props.predict;

        return (
            <Modal onHide={this.handleClosedClicked} onExited={this.handleOnExited}
                show={this.state.show} className="message-modal">
                <Modal.Header closeButton={true}>
                    <h4 className={"modal-title"}>
                        {p.predictor.toStr}
                        <small>{PredictorEntity.niceName()}&nbsp;{p.predictor.id} (<a href={Navigator.navigateRoute(p.predictor)}>{EntityControlMessage.View.niceToString()}</a>)</small>
                    </h4>
                </Modal.Header>
                <Modal.Body>
                    
                </Modal.Body>
                <Modal.Footer>
                    <div>
                        <button
                            className="btn btn-primary sf-close-button sf-ok-button"
                            onClick={() => this.handleButtonClicked(true)}
                            name="accept">
                            Predict
                        </button>
                    </div>
                </Modal.Footer>
            </Modal>
        );
    }

    static show(predictor: Lite<PredictorEntity>, predict: predictor: Lite<PredictorEntity>): Promise<void> {
        return openModal<boolean | undefined>(<PredictModal predictor={predictor} predict={predict} entity={entity} />);
    }
}

