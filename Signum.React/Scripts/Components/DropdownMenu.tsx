import * as React from 'react';
import * as PropTypes from 'prop-types';
import { Popper } from 'react-popper';
import { classes } from '../Globals';

export interface DropdownMenuProps {
    tag: React.ReactType<any>;
    right: boolean;
    flip: boolean;
    className: string;
};


interface DropdownMenuContext {
    isOpen: boolean;
    dropup: boolean;
    inNavbar: boolean;
};

const noFlipModifier = { flip: { enabled: false } };

export class DropdownMenu extends React.Component<DropdownMenuProps> {

    static defaultProps = {
        tag: 'div',
        flip: true,
    };

    render() {
        const ctx = this.context as DropdownMenuContext;
        const { className, right, tag: Tag, flip, ...attrs } = this.props;
        const clss = classes(
            className,
            'dropdown-menu',
            right && 'dropdown-menu-right',
            ctx.isOpen && 'show',
        );


        if (ctx.isOpen && !ctx.inNavbar) {

            const position1 = ctx.dropup ? 'top' : 'bottom';
            const position2 = right ? 'end' : 'start';
            return (
                <Popper
                    placement={`${position1}-${position2}` as any}
                    component={Tag} modifiers={!flip ? noFlipModifier : undefined}
                />
            );
        }
        else {
            return (
                <Tag
                    tabIndex="-1"
                    role="menu"
                    {...attrs}
                    aria-hidden={!ctx.isOpen}
                    className={clss}
                />
            );
        }
    }
}
