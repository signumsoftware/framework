
import * as React from 'react'
import * as Finder from '../Finder'
import { openModal, IModalProps } from '../Modals';
import * as Navigator from '../Navigator';
import { classes, Dic } from '../Globals';
import { SearchMessage, JavascriptMessage, Lite, Entity, NormalWindowMessage, BooleanEnum } from '../Signum.Entities'

import "./Modals.css"
import { Modal } from '../Components';

export type MessageModalStyle = "success" | "info" | "warning" | "error";

export type MessageModalIcon = "info" | "question" | "warning" | "error" | "success";

export type MessageModalButtons = "ok" | "ok_cancel" | "yes_no" | "yes_no_cancel";

export type MessageModalResult = "ok" | "cancel" | "yes" | "no";

interface MessageModalProps extends React.Props<MessageModal>, IModalProps {
    title: React.ReactChild;
    message: React.ReactChild;
    style?: MessageModalStyle;
    buttons: MessageModalButtons;
    icon?: MessageModalIcon;
    customIcon?: string;
}

export default class MessageModal extends React.Component<MessageModalProps, { show: boolean }> {

    constructor(props: MessageModalProps) {
        super(props);

        this.state = { show: true };
    }

    selectedValue?: MessageModalResult;

    handleButtonClicked = (val: MessageModalResult) => {
        this.selectedValue = val;
        this.setState({ show: false });
    }

    handleCancelClicked = () => {
        this.setState({ show: false });
    }

    handleOnExited = () => {
        this.props.onExited!(this.selectedValue);
    }

    renderButtons = (buttons: MessageModalButtons) => {
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
                    <div className="btn-toolbar">
                        <button
                            className="btn btn-primary sf-close-button sf-ok-button"
                            onClick={() => this.handleButtonClicked("ok")}
                            name="accept">
                            {JavascriptMessage.ok.niceToString()}
                        </button>
                        <button
                            className="btn btn-secondary sf-close-button sf-button"
                            onClick={() => this.handleButtonClicked("cancel")}
                            name="cancel">
                            {JavascriptMessage.cancel.niceToString()}
                        </button>
                    </div>);
            case "yes_no":
                return (
                    <div className="btn-toolbar">
                        <button
                            className="btn btn-primary sf-close-button sf-yes-button"
                            onClick={() => this.handleButtonClicked("yes")}
                            name="yes">
                            {BooleanEnum.niceToString("True")}
                        </button>
                        <button
                            className="btn btn-secondary sf-close-button sf-no-button"
                            onClick={() => this.handleButtonClicked("no")}
                            name="no">
                            {BooleanEnum.niceToString("False")}
                        </button>
                    </div>);
            case "yes_no_cancel":
                return (
                    <div className="btn-toolbar">
                        <button
                            className="btn btn-primary sf-close-button sf-yes-button"
                            onClick={() => this.handleButtonClicked("yes")}
                            name="yes">
                            {BooleanEnum.niceToString("True")}
                        </button>
                        <button
                            className="btn btn-secondary sf-close-button sf-no-button"
                            onClick={() => this.handleButtonClicked("no")}
                            name="no">
                            {BooleanEnum.niceToString("False")}
                        </button>
                        <button
                            className="btn btn-secondary sf-close-button sf-cancel-button"
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
                    icon = "fa fa-info-circle";
                    break;
                case "error":
                    icon = "fa fa-exclamation-circle";
                    break;
                case "question":
                    icon = "fa fa-question-circle";
                    break;
                case "success":
                    icon = "fa fa-check-circle";
                    break;
                case "warning":
                    icon = "fa fa-exclamation-triangle";
                    break;
            }
        }

        return icon;
    }

    renderTitle = () => {
        var icon = this.getIcon();

        var iconSpan = icon && <span className={icon}></span>;

        return (
            <span>
                {iconSpan && iconSpan}{iconSpan && <span>&nbsp;&nbsp;</span>}{this.props.title}
            </span>
            );
    }

    render() {
        return (
            <Modal show={this.state.show} onExited={this.handleOnExited} className="message-modal" onHide={this.handleCancelClicked} autoFocus={true}>
                <div className={classes("modal-header", dialogHeaderClass(this.props.style))}>
                    {this.renderTitle()}
                </div>
                <div className="modal-body">
                    {renderText(this.props.message, this.props.style)}
                </div>
                <div className="modal-footer">
                     {this.renderButtons(this.props.buttons)}
                </div>
            </Modal>
        );
    }

    static show(options: MessageModalProps): Promise<MessageModalResult | undefined> {
        return openModal<MessageModalResult>(
            <MessageModal
                title={options.title}
                message={options.message}
                buttons={options.buttons}
                icon={options.icon}
                customIcon={options.customIcon}
                style={options.style}
            />
        );
    }

    static showError(message: string, title?: string): Promise<undefined> {
        return this.show({ buttons: "ok", icon: "error", style: "error", title: title || JavascriptMessage.error.niceToString(), message: message })
            .then(() => undefined);
    }
}

function dialogHeaderClass(style: MessageModalStyle | undefined) {
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

function dialogTextClass(style?: MessageModalStyle) {

    return undefined;

    //switch (style) {
    //    case "success":
    //        return "text-success";
    //    case "info":
    //        return "text-info";
    //    case "warning":
    //        return "text-warning";
    //    case "error":
    //        return "text-danger";
    //    default:
    //        return "text-primary";
    //}
}


function renderText(message: React.ReactChild | null | undefined, style?: MessageModalStyle): React.ReactFragment | null | undefined {
    if (typeof message == "string")
        return message.split("\n").map((p, i) => <p key={i} className={dialogTextClass(style)}>{p}</p>);

    return message;
}


