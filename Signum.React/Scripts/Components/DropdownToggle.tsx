import * as React from 'react';
import * as PropTypes from 'prop-types';
import { Target } from 'react-popper';
import { classes } from '../Globals';
import { Button, ButtonProps } from './Button';
import { BsColor } from '.';

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

    static  contextTypes = {
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
        const { className, color, caret, split, nav, tag, ...props } = this.props;
        const clss = classes(
            className,
            (caret || split) && 'dropdown-toggle',
            split && 'dropdown-toggle-split',
            nav && 'nav-link'
        );
        const children = props.children || <span className="sr-only">Toggle Dropdown</span>;

        let Tag = tag!;
      
        if (nav && !tag) {
            Tag = 'a';
            props.href = '#';
        } else if (!tag) {
            Tag = Button;
            (props as ButtonProps).color = color;
        } else {
            Tag = tag;
        }

        if (this.context.inNavbar) {
            return (
                <Tag
                    {...props}
                    className={clss}
                    onClick={this.onClick}
                    aria-expanded={this.context.isOpen}
                    children={children}
                />
            );
        }

        return (
            <Target
                {...props}
                className={clss}
                component={Tag}
                onClick={this.onClick}
                aria-expanded={this.context.isOpen}
                children={children}
            />
        );
    }
}