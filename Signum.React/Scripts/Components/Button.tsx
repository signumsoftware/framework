import * as React from 'react';
import * as PropTypes from 'prop-types';
import { BsSize } from './Basic';
import { classes } from '../Globals';

interface ButtonProps extends React.AnchorHTMLAttributes<any> {
    active?: boolean;
    block?: boolean;
    color?: string;
    disabled?: boolean;
    outline?: boolean;
    tag?: React.ReactType,
    innerRef?: (e: React.ReactElement<any> | null) => void;
    onClick?: (e: React.MouseEvent<any>) => void;
    size?: BsSize;
    className?: string;
};

const defaultProps = {
    color: 'secondary',
    tag: 'button',
};

export class Button extends React.Component<ButtonProps> {
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
                type={(Tag === 'button' && attributes.onClick) ? 'button' : undefined}
                {...attributes}
                className={clss}
                ref={innerRef}
                onClick={this.onClick}
            />
        );
    }
}