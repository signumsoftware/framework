import * as React from 'react';
import * as PropTypes from 'prop-types';
import { BsSize, BsColor } from './Basic';
import { classes } from '../Globals';

export interface ButtonProps extends React.AnchorHTMLAttributes<any> {
    active?: boolean;
    block?: boolean;
    color?: BsColor;
    disabled?: boolean;
    outline?: boolean;
    tag?: React.ReactType,
    innerRef?: (e: HTMLElement | null) => void;
    onClick?: (e: React.MouseEvent<any>) => void;
    size?: BsSize;
    className?: string;
};



export class Button extends React.Component<ButtonProps> {

    static defaultProps = {
        color: 'secondary',
        tag: 'button',
    };

    constructor(props: ButtonProps) {
        super(props);

        this.onClick = this.onClick.bind(this);
    }

    onClick = (e: React.MouseEvent<any>) => {
        if (this.props.disabled) {
            e.preventDefault();
            return;
        }

        if (this.props.onClick) {
            this.props.onClick(e);
        }
    }

    render() {
        let {
            active,
            disabled,
            block,
            className,
            color,
            outline,
            size,
            tag,
            innerRef,
            ...attributes
        } = this.props;

        const clss = classes(
            className,
            'btn',
            `btn${outline ? '-outline' : ''}-${color}`,
            size && `btn-${size}`,
            block && 'btn-block',
            active && "active",
            disabled && "disabled"
        );

        let Tag = tag!;

        if (attributes.href && Tag === 'button') {
            Tag = 'a';
        }

        return (
            <Tag
                {...attributes}
                className={clss}
                ref={innerRef}
                onClick={this.onClick}
            />
        );
    }
}