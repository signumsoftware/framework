import { PredictorEntity } from "MachineLearning/Signum.Entities.MachineLearning";
import { Modal } from "react-bootstrap";
import * as React from "react";
import { IModalProps, openModal } from "../../../../Framework/Signum.React/Scripts/Modals";
import { Lite } from "../../../../Framework/Signum.React/Scripts/Signum.Entities";
import { Predict } from "MachineLearning/PredictorClient";
import { Entity } from "../../../../Framework/Signum.React/Scripts/Signum.Entities";


interface PredictModalProps extends IModalProps {
    predictor: Lite<PredictorEntity>;
    predict: Predict;
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

    answer?: boolean;
    handleButtonClicked = (val: boolean) => {
        this.answer = val;
        this.setState({ show: false });
    }

    handleClosedClicked = () => {
        this.setState({ show: false });
    }

    handleOnExited = () => {
        this.props.onExited!(undefined);
    }

    render() {
        return (
            <Modal onHide={this.handleClosedClicked}
                show={this.state.show} className="message-modal">
                <Modal.Header closeButton={true}>
                    <h4 className={"modal-title"}>
                        Important Question
                    </h4>
                </Modal.Header>
                <Modal.Body>
                    {this.props.question}
                </Modal.Body>
                <Modal.Footer>
                    <div>
                        <button
                            className="btn btn-primary sf-close-button sf-ok-button"
                            onClick={() => this.handleButtonClicked(true)}
                            name="accept">
                            Yes
                        </button>
                        <button
                            className="btn btn-default sf-close-button sf-button"
                            onClick={() => this.handleButtonClicked(false)}
                            name="cancel">
                            No
                        </button>
                    </div>
                </Modal.Footer>
            </Modal>
        );
    }

    static show(predictor: Lite<PredictorEntity>, predict: Predict, entity?: Lite<Entity>): Promise<void> {
        return openModal<boolean | undefined>(<PredictModal predictor={predictor} predict={predict} entity={entity} />);
    }
}

