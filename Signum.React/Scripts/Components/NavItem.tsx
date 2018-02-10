import * as React from 'react';
import * as PropTypes from 'prop-types';
import { classes } from '../Globals';

interface NavItemProps extends React.HTMLAttributes<any> {
    tag?: React.ReactType;
    active?: boolean;
    className?: string;
}

export class NavItem extends React.Component<NavItemProps> {

    static defaultProps = {
        tag: 'li'
    };

    render() {
        const {
            className,
            active,
            tag,
            ...attributes
        } = this.props;

        const clss = classes(
            className,
            'nav-item',
            active && 'active'
        );

        const Tag = tag!;

        return (
            <Tag {...attributes} className={clss} />
        );
    }
}

export interface NavLinkProps {
    tag?: React.ReactType;
    innerRef?: (a: React.ReactElement<any> | null) => void;
    disabled?: boolean;
    active?: boolean;
    className?: string;
    onClick?: (e: React.MouseEvent<any>) => void;
    href?: string;
}


export class NavLink extends React.Component<NavLinkProps> {
    static defaultProps = {
        tag: 'a',
    };

    onClick = (e: React.MouseEvent<any>) => {
        if (this.props.disabled) {
            e.preventDefault();
            return;
        }

        if (this.props.href === '#') {
            e.preventDefault();
        }

        if (this.props.onClick) {
            this.props.onClick(e);
        }
    }

    render() {
        let {
            className,
            active,
            tag,
            innerRef,
            ...attributes
        } = this.props;

        const clss = classes(
            className,
            'nav-link',
            attributes.disabled && 'disabled',
            active && 'active',
        );

        const Tag = tag!;

        return (
            <Tag {...attributes} ref={innerRef} onClick={this.onClick} className={clss} />
        );
    }
}
