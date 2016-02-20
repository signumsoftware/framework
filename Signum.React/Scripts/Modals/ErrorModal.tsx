
import * as React from 'react'
import { Modal, ModalProps, ModalClass, ButtonToolbar } from 'react-bootstrap'
import * as Finder from '../Finder'
import { openModal, IModalProps } from '../Modals';
import * as Navigator from '../Navigator';
import { classes } from '../Globals';
import { ServiceError, WebApiHttpError } from '../Services';
import { SearchMessage, JavascriptMessage, Lite, Entity, Basics } from '../Signum.Entities'

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

    handleShowStackTrace = () => {
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
                    {se == null ? <h4 className="modal-title text-danger"><span className="glyphicon glyphicon-alert"></span> Error </h4> :
                        <h4 className="modal-title text-danger">
                            <span className={classes("glyphicon", se.defaultIcon) }></span>&nbsp;{se.serviceError.ExceptionType} ({
                                Navigator.isViewable(Basics.ExceptionEntity_Type) ?
                                    <a href={Navigator.navigateRoute(Basics.ExceptionEntity_Type, se.serviceError.MessageDetail) }>{se.serviceError.MessageDetail}</a> :
                                    <strong>{se.serviceError.MessageDetail}</strong>
                            })
                        </h4>
                    }
                </Modal.Header>

                <Modal.Body>
                    <p className="text-danger">
                        {se != null ? se.serviceError.ExceptionMessage :
                            e.message ? e.message : e }
                    </p>

                    {
                        se &&
                        <div>
                            <a href="#" onClick={this.handleShowStackTrace}>StackTrace</a>
                            {this.state.showDetails && <pre>{se.serviceError.StackTrace}</pre>}
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

    static showError(error: any): Promise<void> {
        return openModal<void>(<ErrorModal error={error}/>);
    }
}



