import * as React from 'react';
import * as PropTypes from 'prop-types';
import { classes } from '../Globals';

export interface DropdownItemProps extends React.AnchorHTMLAttributes<any> {
    active?: boolean;
    disabled?: boolean;
    divider?: boolean;
    tag?: React.ReactType<any>;
    header?: boolean;
    onClick?: (e: React.MouseEvent<any>) => void;
    className?: string;
    toggle?: boolean;
    innerRef?: (r: HTMLElement | null) => void;
};

export class DropdownItem extends React.Component<DropdownItemProps> {

    static contextTypes = {
        toggle: PropTypes.func
    };

    static defaultProps = {
        tag: 'button',
        toggle: true
    };

    constructor(props: DropdownItemProps) {
        super(props);

        this.onClick = this.onClick.bind(this);
        this.getTabIndex = this.getTabIndex.bind(this);
    }

    onClick = (e: React.MouseEvent<any>) => {
        if (this.props.disabled || this.props.header || this.props.divider) {
            e.preventDefault();
            return;
        }

        if (this.props.onClick) {
            this.props.onClick(e);
        }

        if (this.props.toggle) {
            this.context.toggle(e);
        }
    }

    getTabIndex() {
        if (this.props.disabled || this.props.header || this.props.divider) {
            return '-1';
        }

        return '0';
    }

    render() {
        const tabIndex = this.getTabIndex();
        let {
            className,
            tag,
            header,
            active,
            toggle,
            divider,
            innerRef,
            disabled,
            ...props } = this.props;

        const clss = classes(
            className,
            disabled && 'disabled',
            !divider && !header && 'dropdown-item',
            active && 'active',
            header && 'dropdown-header',
            divider && 'dropdown-divider'
        );

        let Tag = tag!;

        if (Tag === 'button') {
            if (header) {
                Tag = 'h6';
            } else if (divider) {
                Tag = 'div';
            } else if (props.href) {
                Tag = 'a';
            }
        }

        return (
            <Tag
                ref={innerRef}
                type={(Tag === 'button' && (props.onClick || this.props.toggle)) ? 'button' : undefined}
                {...props}
                tabIndex={tabIndex}
                className={clss}
                onClick={this.onClick}
            />
        );
    }
}