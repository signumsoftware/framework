
import * as React from 'react'
import { Modal, ModalProps, ModalClass, ButtonToolbar } from 'react-bootstrap'
import * as Finder from '../Finder'
import { openModal, IModalProps } from '../Modals';
import * as Navigator from '../Navigator';
import { classes, Dic } from '../Globals';
import { ServiceError, WebApiHttpError, ValidationError } from '../Services';
import { SearchMessage, JavascriptMessage, Lite, Entity, NormalWindowMessage } from '../Signum.Entities'
import { ExceptionEntity } from '../Signum.Entities.Basics'

require("!style!css!./Modals.css");
//http://codepen.io/m-e-conroy/pen/ALsdF
interface ErrorModalProps extends IModalProps {
    error: any;
}

export default class ErrorModal extends React.Component<ErrorModalProps, { showDetails?: boolean; show?: boolean; }>  {

    constructor(props: ErrorModalProps) {
        super(props);

        this.state = {
            show: true,
            showDetails: false,
        };
    }

    handleShowStackTrace = (e: React.MouseEvent) => {
        e.preventDefault();
        this.setState({ showDetails: !this.state.showDetails });
    }

    handleOnExited = () => {
        this.props.onExited!(undefined);
    }

    handleCloseClicked = () => {
        this.setState({ show: false });
    }

    render() {

        const e = this.props.error;

        const se = e instanceof ServiceError ? (e as ServiceError) : undefined;
        const ve = e instanceof ValidationError ? (e as ValidationError) : undefined;

        return (
            <Modal onHide={this.handleCloseClicked} show={this.state.show} onExited={this.handleOnExited}>
                <Modal.Header closeButton={true} className="dialog-header-error">
                    {se ? this.renderServiceTitle(se) :
                        ve ? this.renderValidationTitle(ve) :
                            this.renderTitle(e)}
                </Modal.Header>

                <Modal.Body>
                    {se ? this.renderServiceMessage(se) :
                        ve ? this.renderValidationeMessage(ve) :
                            this.renderMessage(e)}

                    {
                        se && se.httpError.StackTrace &&
                        <div>
                            <a href="#" onClick={this.handleShowStackTrace}>StackTrace</a>
                            {this.state.showDetails && <pre>{se.httpError.StackTrace}</pre>}
                        </div>
                    }
                </Modal.Body>

                <Modal.Footer>
                    <button className ="btn btn-primary sf-close-button sf-ok-button" onClick={this.handleCloseClicked}>
                        {JavascriptMessage.ok.niceToString() }</button>
                </Modal.Footer>
            </Modal>
        );
    }

    renderTitle(e: any) {
            return <h4 className="modal-title text-danger"><span className="glyphicon glyphicon-alert"></span> Error </h4>;
    }

    renderServiceTitle(se: ServiceError) {
        return (<h4 className="modal-title text-danger">
            <span className={classes("glyphicon", se.defaultIcon)}></span>&nbsp; <span>{se.httpError.ExceptionType}</span>
            ({
                Navigator.isViewable(ExceptionEntity) ?
                    <a href={Navigator.navigateRoute(ExceptionEntity, se.httpError.ExceptionID!)}>{se.httpError.ExceptionID}</a> :
                    <strong>{se.httpError.ExceptionID}</strong>
            })
        </h4>);
    }


    renderValidationTitle(ve: ValidationError) {
        return <h4 className="modal-title text-danger">
            <span className="glyphicon glyphicon-alert"></span> {NormalWindowMessage.ThereAreErrors.niceToString()}
        </h4>;
    }

    renderServiceMessage(se: ServiceError) {
        return (
            <div>
                {textDanger(se.httpError.Message)}
                {textDanger(se.httpError.ExceptionMessage)}
                {textDanger(se.httpError.MessageDetail)}
            </div>
        );
    }

    renderValidationeMessage(ve: ValidationError) {
        return (
            <div>
                {textDanger(Dic.getValues(ve.modelState).join("\n"))}
            </div>
        );
    }

    renderMessage(e: any) {
        return textDanger(e.message ? e.message : e);
    }


    static showError(error: any): Promise<void> {
        return openModal<void>(<ErrorModal error={error}/>);
    }
}


function textDanger(message: string | null | undefined): React.ReactFragment | null | undefined {

    if (typeof message == "string")
        return message.split("\n").map((s, i) => <p key={i} className="text-danger">{s}</p>);

    return message;
}



