import * as React from 'react';
import * as PropTypes from 'prop-types';
import { Popper } from 'react-popper';
import { classes } from '../Globals';

export interface DropdownMenuProps extends React.HTMLAttributes<any> {
    tag?: React.ReactType<any>;
    right?: boolean;
    flip?: boolean;
    className?: string;
};


interface DropdownMenuContext {
    isOpen: boolean;
    direction: "up" | "down" | "left" | "right";
    inNavbar: boolean;
};

const noFlipModifier = { flip: { enabled: false } };

const directionPositionMap = {
    up: 'top',
    left: 'left',
    right: 'right',
    down: 'bottom',
};

export class DropdownMenu extends React.Component<DropdownMenuProps> {

    static contextTypes = {
        isOpen: PropTypes.bool.isRequired,
        direction: PropTypes.oneOf(['up', 'down', 'left', 'right']).isRequired,
        inNavbar: PropTypes.bool.isRequired,
    };

    static defaultProps = {
        tag: 'div',
        flip: true,
    };

    render() {
        const context = this.context as DropdownMenuContext;
        let { className, right, tag, flip, ...attrs } = this.props;

        if (document.body.classList.contains("rtl"))
            right = !right;

        const clss = classes(
            className,
            'dropdown-menu',
            right && 'dropdown-menu-right',
            context.isOpen && 'show',
        );

        
        let Tag = tag!;

        if (context.isOpen && !context.inNavbar) {
            Tag = Popper;

            const position1 = directionPositionMap[context.direction] || 'bottom';
            const position2 = right ? 'end' : 'start';
            (attrs as any).placement = `${position1}-${position2}`;
            (attrs as any).component = tag;
            (attrs as any).modifiers = !flip ? noFlipModifier : undefined;
        }

        return (
            <Tag
                tabIndex="-1"
                role="menu"
                {...attrs}
                aria-hidden={!context.isOpen}
                className={clss}
            />
        );
    }
}
