import * as React from 'react'
import { Dic, classes, combineFunction, DomUtils } from '../Globals'
import * as RootCloseWrapper from 'react-overlays/lib/RootCloseWrapper'


export interface ContextMenuPosition {
    pageX: number;
    pageY: number;
    width: number; //Necessary for RTL
}

export interface ContextMenuProps extends React.Props<ContextMenu>, React.HTMLAttributes {
    position: ContextMenuPosition;
    onHide: () => void;
}

export default class ContextMenu extends React.Component<ContextMenuProps, {}> {

    static getPosition(e: React.MouseEvent, container: HTMLElement): ContextMenuPosition{

        const op = DomUtils.offsetParent(container);

        return ({
            pageX: e.pageX - (op ? op.getBoundingClientRect().left : 0),
            pageY: e.pageY - (op ? op.getBoundingClientRect().top : 0),
            width: (op ? op.getBoundingClientRect().width : window.innerWidth)
        });
    }




    render() {

        const { position, onHide } = this.props;
        const props = Dic.without(this.props, { position, onHide, ref: undefined });

        const style: React.CSSProperties = { zIndex: 999, display: "block", position: "absolute" };

        style.top = position.pageY + "px";
        if (document.body.className.contains("rtl-mode"))
            style.right = (position.width - position.pageX) + "px";
        else
            style.left = position.pageX + "px";

        const childrens = React.Children.map(this.props.children,
            (c: React.ReactElement<any>) => c && React.cloneElement(c, { "onSelect": combineFunction(c.props.onSelect, onHide) }));

        const ul = (
            <ul {...props as any}  className={classes(props.className, "dropdown-menu sf-context-menu") } style={style}>
                {childrens}
            </ul>
        );

        return <RootCloseWrapper onRootClose={onHide}>{ul}</RootCloseWrapper>;
    }
}