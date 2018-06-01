import * as React from 'react';
import { PopperContent, getTarget } from './PopperContent';
import PopperJS from "popper.js";
import { classes } from '../Globals';

interface PopoverProps {
    placement: PopperJS.Placement;
    target: string | (() => HTMLElement) | HTMLElement;
    container?: string | (() => HTMLElement) | HTMLElement;
    disabled?: boolean;
    hideArrow?: boolean;
    className?: string;
    innerClassName?: string;
    placementPrefix?: string;
    delay?: number | { show: number, hide: number };
    modifiers?: PopperJS.Modifiers;
    isOpen?: boolean;
    toggle?: () => void,
}

const DEFAULT_DELAYS = {
    show: 0,
    hide: 0,
};

export class Popover extends React.Component<PopoverProps> {

    static defaultProps = {
        isOpen: false,
        hideArrow: false,
        placement: 'right',
        placementPrefix: 'bs-popover',
        delay: DEFAULT_DELAYS,
        toggle: () => { },
    };

    _target?: HTMLElement;
    componentDidMount() {
        this._target = getTarget(this.props.target);
        this.handleProps();
    }

    componentDidUpdate() {
        this.handleProps();
    }

    componentWillUnmount() {
        this.clearShowTimeout();
        this.clearHideTimeout();
        this.removeTargetEvents();
    }

    _popover?: HTMLDivElement | null;
    getRef = (ref: HTMLDivElement | null) => {
        this._popover = ref;
    }

    _hideTimeout?: number;
    _showTimeout?: number;
    getDelay(key: "show" | "hide") {
        const { delay } = this.props;
        if (typeof delay === 'object') {
            return isNaN(delay[key]) ? DEFAULT_DELAYS[key] : delay[key];
        }
        return delay;
    }

    handleProps() {
        if (this.props.isOpen) {
            this.show();
        } else {
            this.hide();
        }
    }

    show = () => {
        this.clearHideTimeout();
        this.addTargetEvents();
        if (!this.props.isOpen) {
            this.clearShowTimeout();
            this._showTimeout = setTimeout(this.toggle, this.getDelay('show'));
        }
    }

    hide = () => {
        this.clearShowTimeout();
        this.removeTargetEvents();
        if (this.props.isOpen) {
            this.clearHideTimeout();
            this._hideTimeout = setTimeout(this.toggle, this.getDelay('hide'));
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
        if (e.target !== this._target && !this._target!.contains(e.target as HTMLElement) &&
            e.target !== this._popover && !(this._popover && this._popover.contains(e.target as HTMLElement))) {
            if (this._hideTimeout) {
                this.clearHideTimeout();
            }

            if (this.props.isOpen) {
                this.toggle!();
            }
        }
    }

    addTargetEvents = () => {
        document.addEventListener('click', this.handleDocumentClick, true);
        document.addEventListener('touchstart', this.handleDocumentClick, true);
    }

    removeTargetEvents = () => {
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

        const {
            placement,
            target,
            container,
            isOpen,
            disabled,
            className,
            innerClassName,
            toggle,
            placementPrefix,
            delay,
            modifiers,
            hideArrow,
            ...attributes
        } = this.props;

        const clss = classes(
            'popover-inner',
            this.props.innerClassName
        );

        const popperClasses = classes(
            'popover',
            'show',
            this.props.className
        );

        return (
            <PopperContent
                className={popperClasses}
                target={target}
                isOpen={isOpen}
                hideArrow={hideArrow}
                placement={placement}
                placementPrefix={placementPrefix}
                container={container}
                modifiers={modifiers}>
                <div {...attributes} className={clss} ref={this.getRef} />
            </PopperContent>
        );
    }
}