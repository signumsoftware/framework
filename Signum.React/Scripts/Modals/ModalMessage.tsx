
import * as React from 'react'
import { Modal, ModalProps, ModalClass, ButtonToolbar, Sizes } from 'react-bootstrap'
import * as Finder from '../Finder'
import { openModal, IModalProps } from '../Modals';
import * as Navigator from '../Navigator';
import { classes, Dic } from '../Globals';
import { SearchMessage, JavascriptMessage, Lite, Entity, NormalWindowMessage, BooleanEnum } from '../Signum.Entities'

require("!style!css!./Modals.css");

export type ModalMessageStyle = "success" | "info" | "warning" | "error";

export type ModalMessageIcon = "info" | "question" | "warning" | "error" | "success";

export type ModalMessageButtons = "ok" | "ok_cancel" | "yes_no" | "yes_no_cancel";

export type ModalMessageResult = "ok" | "cancel" | "yes" | "no";

interface ModalMessageProps extends React.Props<ModalMessage>, IModalProps {
    title: React.ReactChild;
    message: React.ReactChild;
    defaultStyle?: ModalMessageStyle;
    buttons: ModalMessageButtons;
    icon?: ModalMessageIcon;
    customIcon?: string;
}

export default class ModalMessage extends React.Component<ModalMessageProps, { show: boolean }> {

    constructor(props: ModalMessageProps) {
        super(props);

        this.state = { show: true };
    }

    selectedValue?: ModalMessageResult;
    handleButtonClicked = (val: ModalMessageResult) => {
        this.selectedValue = val;
        this.setState({ show: false });
    }

    handleCancelClicked = () => {
        this.setState({ show: false });
    }

    handleOnExited = () => {
        this.props.onExited!(this.selectedValue);
    }

    renderButtons = (buttons: ModalMessageButtons) => {
        switch (buttons) {
            case "ok":
                return (
                    <button
                        className="btn btn-primary sf-close-button sf-ok-button"
                        onClick={() => this.handleButtonClicked("ok")}
                        name="accept">
                        {JavascriptMessage.ok.niceToString()}
                    </button>);
            case "ok_cancel":
                return (
                    <div>
                        <button
                            className="btn btn-primary sf-close-button sf-ok-button"
                            onClick={() => this.handleButtonClicked("ok")}
                            name="accept">
                            {JavascriptMessage.ok.niceToString()}
                        </button>
                        <button
                            className="btn btn-default sf-close-button sf-button"
                            onClick={() => this.handleButtonClicked("cancel")}
                            name="cancel">
                            {JavascriptMessage.cancel.niceToString()}
                        </button>
                    </div>);
            case "yes_no":
                return (
                    <div>
                        <button
                            className="btn btn-primary sf-close-button sf-ok-button"
                            onClick={() => this.handleButtonClicked("yes")}
                            name="yes">
                            {BooleanEnum.niceName("True")}
                        </button>
                        <button
                            className="btn btn-default sf-close-button sf-button"
                            onClick={() => this.handleButtonClicked("no")}
                            name="no">
                            {BooleanEnum.niceName("False")}
                        </button>
                    </div>);
            case "yes_no_cancel":
                return (
                    <div>
                        <button
                            className="btn btn-primary sf-close-button sf-ok-button"
                            onClick={() => this.handleButtonClicked("yes")}
                            name="yes">
                            {BooleanEnum.niceName("True")}
                        </button>
                        <button
                            className="btn btn-default sf-close-button sf-button"
                            onClick={() => this.handleButtonClicked("no")}
                            name="no">
                            {BooleanEnum.niceName("False")}
                        </button>
                        <button
                            className="btn btn-default sf-close-button sf-button"
                            onClick={() => this.handleButtonClicked("cancel")}
                            name="cancel">
                            {JavascriptMessage.cancel.niceToString()}
                        </button>
                    </div>);
        }
    }

    getIcon = () => {
        var icon: string | undefined;

        if (this.props.customIcon)
            icon = this.props.customIcon;

        if (this.props.icon) {
            switch (this.props.icon) {
                case "info":
                    icon = "glyphicon glyphicon-info-sign";
                    break;
                case "error":
                    icon = "glyphicon glyphicon-alert";
                    break;
                case "question":
                    icon = "glyphicon glyphicon-question-sign";
                    break;
                case "success":
                    icon = "glyphicon glyphicon-ok-sign";
                    break;
                case "warning":
                    icon = "glyphicon glyphicon-exclamation-sign";
                    break;
            }
        }

        return icon;
    }

    renderTitle = () => {
        var icon = this.getIcon();

        var iconSpan = icon && <span className={icon}></span>;

        return (
            <h4 className={"modal-title"}>
                {iconSpan && iconSpan}{iconSpan && <span>&nbsp;&nbsp;</span>}{this.props.title}
            </h4>
            );
    }

    render() {
        return (
            <Modal onHide={this.handleCancelClicked} show={this.state.show} onExited={this.handleOnExited}>
                <Modal.Header closeButton={true} className={dialogHeaderClass(this.props.defaultStyle)}>
                    {this.renderTitle()}
                </Modal.Header>
                <Modal.Body>
                    {renderText(this.props.message, this.props.defaultStyle)}
                </Modal.Body>
                <Modal.Footer>
                    {this.renderButtons(this.props.buttons)}
                </Modal.Footer>
            </Modal>
        );
    }

    static show(options: ModalMessageProps): Promise<ModalMessageResult | undefined> {
        return openModal<ModalMessageResult>(
            <ModalMessage
                title={options.title}
                message={options.message}
                buttons={options.buttons}
                icon={options.icon}
                customIcon={options.customIcon}
                defaultStyle={options.defaultStyle}
            />
        );
    }
}

function dialogHeaderClass(style: ModalMessageStyle | undefined) {
    switch (style) {
        case "success":
            return "dialog-header-success";
        case "info":
            return "dialog-header-info";
        case "warning":
            return "dialog-header-warning";
        case "error":
            return "dialog-header-error";
        default:
            return "bg-primary";
    }
}

function dialogTextClass(style?: ModalMessageStyle) {
    switch (style) {
        case "success":
            return "text-success";
        case "info":
            return "text-info";
        case "warning":
            return "text-warning";
        case "error":
            return "text-danger";
        default:
            return "text-primary";
    }
}


function renderText(message: React.ReactChild | null | undefined, style?: ModalMessageStyle): React.ReactFragment | null | undefined {
    if (typeof message == "string")
        return message.split("\n").map((p, i) => <p key={i} className={dialogTextClass(style)}>{p}</p>);

    return message;
}


