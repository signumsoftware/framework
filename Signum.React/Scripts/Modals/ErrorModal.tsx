
import * as React from 'react'
import { Modal, ModalProps, ModalClass, ButtonToolbar } from 'react-bootstrap'
import * as Finder from '../Finder'
import { openModal, IModalProps } from '../Modals';
import * as Navigator from '../Navigator';
import { classes } from '../Globals';
import { ServiceError, WebApiHttpError } from '../Services';
import { SearchMessage, JavascriptMessage, Lite, Entity } from '../Signum.Entities'
import { ExceptionEntity_Type } from '../Signum.Entities.Basics'

require("!style!css!./Modals.css");
//http://codepen.io/m-e-conroy/pen/ALsdF
interface ErrorModalProps extends IModalProps {
    error: any;
}

export default class ErrorModal extends React.Component<ErrorModalProps, { showDetails?: boolean; show?: boolean; }>  {

    constructor(props) {
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
        this.props.onExited(null);
    }

    handleCloseClicked = () => {
        this.setState({ show: false });
    }

    render() {

        var e = this.props.error;

        var se = e instanceof ServiceError ? (e as ServiceError) : null;

        return (
            <Modal onHide={this.handleCloseClicked} show={this.state.show} onExited={this.handleOnExited}>
                <Modal.Header closeButton={true} className="dialog-header-error">
                    {this.renderTitle(e, se) }
                </Modal.Header>

                <Modal.Body>
                    { this.renderMessage(e, se) }

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

    renderTitle(e: any, se: ServiceError) {
        if (se == null || se.httpError.ExceptionType == null)
            return <h4 className="modal-title text-danger"><span className="glyphicon glyphicon-alert"></span> Error </h4>;

        return (<h4 className="modal-title text-danger">
            <span className={classes("glyphicon", se.defaultIcon) }></span>&nbsp; <span>{se.httpError.ExceptionType }</span>
            ({
                Navigator.isViewable(ExceptionEntity_Type) ?
                    <a href={Navigator.navigateRoute(ExceptionEntity_Type, se.httpError.ExceptionID) }>{se.httpError.ExceptionID}</a> :
                    <strong>{se.httpError.ExceptionID}</strong>
            })
        </h4>);
    }

    renderMessage(e: any, se: ServiceError) {
        if (se == null)
            return <p className="text-danger"> { e.message ? e.message : e }</p>;

        return (
            <div>
                {se.httpError.Message && <p className="text-danger">{se.httpError.Message}</p>}
                {se.httpError.ExceptionMessage && <p className="text-danger">{se.httpError.ExceptionMessage}</p>}
                {se.httpError.MessageDetail && <p className="text-danger">{se.httpError.MessageDetail}</p>}
            </div>
        );
    }

    static showError(error: any): Promise<void> {
        return openModal<void>(<ErrorModal error={error}/>);
    }
}



