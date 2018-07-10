import * as React from 'react';
import * as PropTypes from 'prop-types';
import { Reference, ReferenceChildrenProps, PopperChildrenProps, RefHandler } from 'react-popper';
import { classes } from '../Globals';
import { Button, ButtonProps } from './Button';
import { BsColor } from '.';
import { color } from 'd3';

interface DropdownToggleProps extends React.AnchorHTMLAttributes<any> {
    caret?: boolean;
    color?: BsColor;
    className?: string;
    disabled?: boolean;
    onClick?: (e: React.MouseEvent<any>) => void;
    'aria-haspopup'?: boolean;
    split?: boolean;
    nav?: boolean;
    tag?: React.ReactType<any>;
}

export class DropdownToggle extends React.Component<DropdownToggleProps> {

    static defaultProps = {
        'aria-haspopup': true,
        color: 'secondary',
    };

    constructor(props: DropdownToggleProps) {
        super(props);

        this.onClick = this.onClick.bind(this);
    }

    static contextTypes = {
        isOpen: PropTypes.bool.isRequired,
        toggle: PropTypes.func.isRequired,
        inNavbar: PropTypes.bool.isRequired,
    };

    onClick = (e: React.MouseEvent<any>) => {
        if (this.props.disabled) {
            e.preventDefault();
            return;
        }

        if (this.props.nav) {
            e.preventDefault();
        }

        if (this.props.onClick) {
            this.props.onClick(e);
        }

        this.context.toggle(e);
    }

    render() {

        if (this.context.inNavbar)
            return this.renderContent(this.props, undefined);
        
        return (
            <Reference>
                {p => this.renderContent(this.props, p.ref)}
            </Reference>
        );
    }

    renderContent(p: DropdownToggleProps, ref: RefHandler | undefined) {

        const clss = classes(
            p.className,
            (p.caret || p.split) && 'dropdown-toggle',
            p.split && 'dropdown-toggle-split',
            p.nav && 'nav-link'
        );


        const children = p.children || <span className="sr-only">Toggle Dropdown</span>;

        if (p.nav && !p.tag) {
            return (
                <a href="#" ref={ref} className={clss} aria-expanded={this.context.isOpen} onClick={this.onClick}>
                    {children}
                </a>
            );
        } else if (!p.tag) {
            return (
                <Button color={p.color} className={clss} innerRef={ref} aria-expanded={this.context.isOpen} onClick={this.onClick}>
                    {children}
                </Button>
            );
        } else {
            const Tag = p.tag;

            return (
                <Tag ref={ref} className={clss} aria-expanded={this.context.isOpen} onClick={this.onClick}>
                    {children}
                </Tag>
            );
        }
    }
}