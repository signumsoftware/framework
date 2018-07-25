
import * as React from 'react'
import * as Finder from '../Finder'
import { openModal, IModalProps } from '../Modals';
import * as Navigator from '../Navigator';
import { classes, Dic } from '../Globals';
import { ServiceError, WebApiHttpError, ValidationError } from '../Services';
import { SearchMessage, JavascriptMessage, Lite, Entity, NormalWindowMessage } from '../Signum.Entities'
import { ExceptionEntity } from '../Signum.Entities.Basics'
import "./Modals.css"
import { Modal } from '../Components';
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome';

//http://codepen.io/m-e-conroy/pen/ALsdF
interface ErrorModalProps extends IModalProps {
    error: any;
}

export default class ErrorModal extends React.Component<ErrorModalProps, { showDetails?: boolean; show: boolean; }>  {

    constructor(props: ErrorModalProps) {
        super(props);

        this.state = {
            show: true,
            showDetails: false,
        };
    }

    handleShowStackTrace = (e: React.MouseEvent<any>) => {
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
            <Modal show={this.state.show} onExited={this.handleOnExited} onHide={this.handleCloseClicked} size="lg">
                <div className="modal-header dialog-header-error text-danger">
                    <h5 className="modal-title"> {se ? this.renderServiceTitle(se) :
                        ve ? this.renderValidationTitle(ve) :
                            this.renderTitle(e)}</h5>
                    <button type="button" className="close" data-dismiss="modal" aria-label="Close" onClick={this.handleCloseClicked}>
                        <span aria-hidden="true">&times;</span>
                    </button>
                </div>

                <div className="modal-body">
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
                </div>

                <div className="modal-footer">
                    <button className ="btn btn-primary sf-close-button sf-ok-button" onClick={this.handleCloseClicked}>
                        {JavascriptMessage.ok.niceToString() }</button>
                </div>
            </Modal>
        );
    }

    renderTitle(e: any) {
        return (
            <span><FontAwesomeIcon icon="exclamation-triangle"/> Error </span>
        );
    }

    renderServiceTitle(se: ServiceError) {
        return (
            <span>

                <FontAwesomeIcon icon={se.defaultIcon} />&nbsp; <span>{se.httpError.ExceptionType}</span>
                ({
                    Navigator.isViewable(ExceptionEntity) ?
                        <a href={Navigator.navigateRoute(ExceptionEntity, se.httpError.ExceptionID!)}>{se.httpError.ExceptionID}</a> :
                        <strong>{se.httpError.ExceptionID}</strong>
                })
            </span>
        );
    }


    renderValidationTitle(ve: ValidationError) {
        return (
            <span>
                <FontAwesomeIcon icon="exclamation-triangle"/> {NormalWindowMessage.ThereAreErrors.niceToString()}
            </span>
        );
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



