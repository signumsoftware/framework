import * as React from 'react';
import * as PropTypes from 'prop-types';
import PopperJS from "popper.js";
import { PopperContent, getTarget } from './PopperContent';
import { classes } from '../Globals';

interface UncontrolledTooltipProps extends React.HTMLAttributes<HTMLDivElement> {
    placement: PopperJS.Placement;
    target: string | (() => HTMLElement) | HTMLElement;
    container?: string | (() => HTMLElement) | HTMLElement;
    disabled?: boolean;
    hideArrow?: boolean;
    className?: string;
    innerClassName?: string;
    autohide?: boolean;
    placementPrefix?: string;
    delay?: number | { show: number, hide: number };
    modifiers?: PopperJS.Modifiers;
};

interface TooltipProps extends UncontrolledTooltipProps {
    isOpen?: boolean;
    toggle?: () => void,
}


export class Tooltip extends React.Component<TooltipProps> {

    static DEFAULT_DELAYS = {
        show: 0,
        hide: 250
    };

    static defaultProps = {
        isOpen: false,
        placement: 'top',
        placementPrefix: 'bs-tooltip',
        delay: Tooltip.DEFAULT_DELAYS,
        autohide: true,
        hideArrow: false,
        toggle: function () { }
    };


    constructor(props: TooltipProps) {
        super(props);
    }

    _target!: HTMLElement;
    componentDidMount() {
        this._target = getTarget(this.props.target);
        this.addTargetEvents();
    }

    componentWillUnmount() {
        this.removeTargetEvents();
    }

    _hideTimeout?: number;
    _showTimeout?: number;
    onMouseOverTooltip = () => {
        if (this._hideTimeout) {
            this.clearHideTimeout();
        }
        this._showTimeout = setTimeout(this.show, this.getDelay('show'));
    }

    onMouseLeaveTooltip = () => {
        if (this._showTimeout) {
            this.clearShowTimeout();
        }
        this._hideTimeout = setTimeout(this.hide, this.getDelay('hide'));
    }

    onMouseOverTooltipContent = () => {
        if (this.props.autohide) {
            return;
        }
        if (this._hideTimeout) {
            this.clearHideTimeout();
        }
    }

    onMouseLeaveTooltipContent = () => {
        if (this.props.autohide) {
            return;
        }
        if (this._showTimeout) {
            this.clearShowTimeout();
        }
        this._hideTimeout = setTimeout(this.hide, this.getDelay('hide'));
    }

    getDelay(key : "show" | "hide") {
        const { delay } = this.props;
        if (typeof delay === 'object') {
            return isNaN(delay[key]) ? Tooltip.DEFAULT_DELAYS[key] : delay[key];
        }
        return delay;
    }

    show = () => {
        if (!this.props.isOpen) {
            this.clearShowTimeout();
            this.toggle();
        }
    }

    hide = () => {
        if (this.props.isOpen) {
            this.clearHideTimeout();
            this.toggle();
        }
    }

    clearShowTimeout() {
        clearTimeout(this._showTimeout!);
        this._showTimeout = undefined;
    }

    clearHideTimeout() {
        clearTimeout(this._hideTimeout!);
        this._hideTimeout = undefined;
    }

    handleDocumentClick = (e: MouseEvent | TouchEvent) => {
        if (e.target === this._target || this._target!.contains(e.target as HTMLElement)) {
            if (this._hideTimeout) {
                this.clearHideTimeout();
            }

            if (this.props.isOpen) {
                this.toggle();
            }
        }
    }

    addTargetEvents = () => {
        this._target!.addEventListener('mouseover', this.onMouseOverTooltip, true);
        this._target!.addEventListener('mouseout', this.onMouseLeaveTooltip, true);
        document.addEventListener('click', this.handleDocumentClick, true);
        document.addEventListener('touchstart', this.handleDocumentClick, true);
    }

    removeTargetEvents = ()  => {
        this._target!.removeEventListener('mouseover', this.onMouseOverTooltip, true);
        this._target!.removeEventListener('mouseout', this.onMouseLeaveTooltip, true);
        document.removeEventListener('click', this.handleDocumentClick, true);
        document.removeEventListener('touchstart', this.handleDocumentClick, true);
    }

    toggle = () => {
        if (this.props.disabled) {
            return;
        }

        return this.props.toggle!();
    }

    render() {
        if (!this.props.isOpen) {
            return null;
        }

        const { placement,
            target,
            container,
            isOpen,
            disabled,
            className,
            innerClassName,
            toggle,
            autohide,
            placementPrefix,
            delay,
            modifiers,
            hideArrow,
            ...attributes
        } = this.props;
        

        return (
            <PopperContent
                className={classes('tooltip', 'show', className)}
                target={target}
                isOpen={isOpen}
                placement={placement}
                hideArrow={hideArrow}
                placementPrefix={placementPrefix}
                container={container}
                modifiers={modifiers}
            >
                <div
                    {...attributes}
                    className={classes('tooltip-inner', innerClassName)}
                    onMouseOver={this.onMouseOverTooltipContent}
                    onMouseLeave={this.onMouseLeaveTooltipContent}
                />
            </PopperContent>
        );
    }
}

export class UncontrolledTooltip extends React.Component<UncontrolledTooltipProps, { isOpen: boolean }> {
    constructor(props: TooltipProps) {
        super(props);

        this.state = { isOpen: false };
    }

    toggle = () => {
        this.setState({ isOpen: !this.state.isOpen });
    }

    render() {
        return <Tooltip isOpen={this.state.isOpen} toggle={this.toggle} {...this.props} />;
    }
}