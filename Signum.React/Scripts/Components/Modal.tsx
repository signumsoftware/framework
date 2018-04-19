//Thanks to https://github.com/react-bootstrap/react-bootstrap

import * as React from 'react';
import * as PropTypes from 'prop-types';
import * as ReactDOM from 'react-dom';
import * as BaseModal from 'react-overlays/lib/Modal';
import isOverflowing from 'react-overlays/lib/utils/isOverflowing';

import { ModalFade } from './ModalFade';
import { classes } from '../Globals';
import { JavascriptMessage } from '../Signum.Entities';
import { BsSize, BsColor} from './index';
import { ErrorBoundary } from './ErrorBoundary';

export interface ModalProps extends ModelDialogProps  {

    /**
     * Include a backdrop component. Specify 'static' for a backdrop that doesn't
     * trigger an "onHide" when clicked.
     */
    backdrop?: 'static' | boolean;

    /**
     * Close the modal when escape key is pressed
     */
    keyboard?: boolean;

    /**
     * Open and close the Modal with a slide and fade animation.
     */
    animation?: boolean;

    /**
     * When `true` The modal will automatically shift focus to itself when it
     * opens, and replace it to the last focused element when it closes.
     * Generally this should never be set to false as it makes the Modal less
     * accessible to assistive technologies, like screen-readers.
     */
    autoFocus?: boolean;

    /**
     * When `true` The modal will prevent focus from leaving the Modal while
     * open. Consider leaving the default value here, as it is necessary to make
     * the Modal work well with assistive technologies, such as screen readers.
     */
    enforceFocus?: boolean;

    /**
     * When `true` The modal will restore focus to previously focused element once
     * modal is hidden
     */
    restoreFocus?: boolean;

    /**
     * When `true` The modal will show itself.
     */
    show: boolean;

    /**
     * A callback fired when the header closeButton or non-static backdrop is
     * clicked. Required if either are specified.
     */
    onHide: () => void;

    /**
     * Callback fired before the Modal transitions in
     */
    onEnter?: () => void;

    /**
     * Callback fired as the Modal begins to transition in
     */
    onEntering?: () => void;

    /**
     * Callback fired after the Modal finishes transitioning in
     */
    onEntered?: () => void;

    /**
     * Callback fired right before the Modal transitions out
     */
    onExit?: () => void;

    /**
     * Callback fired as the Modal begins to transition out
     */
    onExiting?: () => void;

    /**
     * Callback fired after the Modal finishes transitioning out
     */
    onExited?: () => void;

    /**
     * @private
     */
    container?: React.ReactInstance;

    className?: string;

    style?: React.CSSProperties;
};




/* eslint-disable no-use-before-define, react/no-multi-comp */
function DialogTransition(props : any) {
    return <ModalFade { ...props } timeout = { Modal.TRANSITION_DURATION } />;
}

function BackdropTransition(props : any) {
    return <ModalFade { ...props } timeout = { Modal.BACKDROP_TRANSITION_DURATION } />;
}


export interface ModalState {
    style: React.CSSProperties;
}
/* eslint-enable no-use-before-define */

export class Modal extends React.Component<ModalProps, ModalState> {

    static defaultProps = {
        ...BaseModal.defaultProps,
        animation: true,
    };
    
    static TRANSITION_DURATION = 300;
    static BACKDROP_TRANSITION_DURATION = 150;

    constructor(props: ModalProps, context: any) {
        super(props, context);
        
        this.state = {
            style: {}
        };
    }

    static childContextTypes = {
        $bs_modal: PropTypes.shape({
            onHide: PropTypes.func
        })
    };

    getChildContext() {
        return {
            $bs_modal: {
                onHide: this.props.onHide
            }
        };
    }

    componentWillUnmount() {
        // Clean up the listener if we need to.
        this.handleExited();
    }

    _modal?: BaseModal | null;
    setModalRef = (ref: BaseModal | null) => {
        this._modal = ref;
    }

    handleDialogClick = (e: React.MouseEvent<any>) => {
        if (e.target !== e.currentTarget) {
            return;
        }

        this.props.onHide();
    }

    handleEntering = () => {
        // FIXME: This should work even when animation is disabled.
        window.addEventListener("resize", this.handleWindowResize);
        this.updateStyle();
    }

    handleExited = () => {
        // FIXME: This should work even when animation is disabled.
        window.removeEventListener('resize', this.handleWindowResize);
    }

    handleWindowResize = () => {
        this.updateStyle();
    }

    updateStyle() {

        const dialogNode = this._modal!.getDialogElement();
        const dialogHeight = dialogNode.scrollHeight;

        const document = ownerDocument(dialogNode);
        const bodyIsOverflowing = isOverflowing(
            this.props.container && ReactDOM.findDOMNode(this.props.container) as HTMLElement || document.body
        );
        const modalIsOverflowing =
            dialogHeight > document.documentElement.clientHeight;

        this.setState({
            style: {
                paddingRight:
                    bodyIsOverflowing && !modalIsOverflowing
                        ? getScrollbarSize()
                        : undefined,
                paddingLeft:
                    !bodyIsOverflowing && modalIsOverflowing
                        ? getScrollbarSize()
                        : undefined
            }
        });
    }

    render() {
        const {
            backdrop,
            animation,
            show,
            className,
            style,
            children, // Just in case this get added to BaseModal propTypes.
            onEntering,
            onExited,
            size,
            color,
            dialogClassName,
            ...dialogProps
        } = this.props;


        const inClassName = show && !animation && 'show';

        return (
            <BaseModal
                {...dialogProps}
                ref={this.setModalRef}
                show={show}
                containerClassName="modal-open"
                transition={animation ? DialogTransition : undefined}
                backdrop={backdrop}
                backdropTransition={animation ? BackdropTransition : undefined}
                backdropClassName={classes(
                    "modal-backdrop",
                    inClassName
                )}
                onEntering={() => {
                    onEntering && onEntering();
                    this.handleEntering();
                }}
                onExited={() => {
                    onExited && onExited();
                    this.handleExited();
                }}>
                <ModalDialog
                    color={color}
                    size={size}
                    dialogClassName={dialogClassName}
                    style={{ ...this.state.style, ...style }}
                    className={classes(className, inClassName)}
                    onClick={backdrop == true ? this.handleDialogClick : undefined}
                >
                    <ErrorBoundary>
                        {children}
                    </ErrorBoundary>
                </ModalDialog>
            </BaseModal>
        );
    }
}

function ownerDocument(node?: HTMLElement) {
    return (node && node.ownerDocument) || document;
}

let size: number | undefined;
function getScrollbarSize(recalc?: boolean) {
    if ((!size && size !== 0) || recalc) {
        var scrollDiv = document.createElement('div');

        scrollDiv.style.position = 'absolute';
        scrollDiv.style.top = '-9999px';
        scrollDiv.style.width = '50px';
        scrollDiv.style.height = '50px';
        scrollDiv.style.overflow = 'scroll';

        document.body.appendChild(scrollDiv);
        size = scrollDiv.offsetWidth - scrollDiv.clientWidth;
        document.body.removeChild(scrollDiv);
    }

    return size
}




interface ModelDialogProps extends React.HTMLAttributes<any> {
    /**
     * A css class to apply to the Modal dialog DOM node.
     */
    dialogClassName?: string;
    className?: string;
    style?: React.CSSProperties;

    size?: BsSize;
    color?: BsColor;
}

class ModalDialog extends React.Component<ModelDialogProps> {


    render() {
        const {
            dialogClassName,
            className,
            style,
            children,
            size,
            color,
            ...elementProps
        } = this.props;

        return (
            <div
                {...elementProps}
                tabIndex={-1}
                role="dialog"
                style={{ display: 'block', ...style }}
                className={classes(className, "modal")}>
                <div className={classes(
                    dialogClassName,
                    "modal-dialog",
                    color && "modal-" + color,
                    size && "modal-" + size)}>
                    <div className="modal-content" role="document">
                        {children}
                    </div>
                </div>
            </div>
        );
    }
}

interface ModalHeaderButtonsProps {
    onClose?: () => void;
    onOk?: () => void;
    okDisabled?: boolean;
    onCancel?: () => void;
    closeBeforeTitle?: boolean;
    htmlAttributes?: React.HTMLAttributes<HTMLDivElement>;
}

export class ModalHeaderButtons extends React.Component<ModalHeaderButtonsProps> {
    render() {
        const p = this.props;
        var close = this.props.onClose &&
            <button type="button" className="close" aria-label="Close" onClick={this.props.onClose}>
                <span aria-hidden="true">×</span>
            </button>;

        return (
            <div className="modal-header" {...p.htmlAttributes}>
                {p.closeBeforeTitle && close}
                <h4 className="modal-title" >
                    {this.props.children}
                </h4>
                {!p.closeBeforeTitle && close}
                {(this.props.onCancel || this.props.onOk) &&
                    <div className="btn-toolbar" style={{ flexWrap: "nowrap" }}>
                        {this.props.onOk && <button className="btn btn-primary sf-entity-button sf-close-button sf-ok-button" disabled={this.props.okDisabled} onClick={this.props.onOk}>
                            {JavascriptMessage.ok.niceToString()}
                        </button>
                        }
                        {this.props.onCancel && <button className="btn btn-light sf-entity-button sf-close-button sf-cancel-button" onClick={this.props.onCancel}>
                            {JavascriptMessage.cancel.niceToString()}
                        </button>
                        }
                    </div>
                }
            </div>
        );
    }
}



