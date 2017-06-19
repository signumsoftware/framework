import * as React from 'react'
import { Dic, classes, combineFunction, DomUtils } from '../Globals'
import * as RootCloseWrapper from 'react-overlays/lib/RootCloseWrapper'


export interface ContextMenuPosition {
    pageX: number;
    pageY: number;
    width: number; //Necessary for RTL
}

export interface ContextMenuProps extends React.Props<ContextMenu>, React.HTMLAttributes<HTMLUListElement> {
    position: ContextMenuPosition;
    onHide: () => void;
}

export default class ContextMenu extends React.Component<ContextMenuProps, {}> {

    static getPosition(e: React.MouseEvent<any>, container: HTMLElement): ContextMenuPosition{

        const op = DomUtils.offsetParent(container);
        var result = ({
            pageX: e.pageX - (op ? op.offsetLeft : 0),
            pageY: e.pageY - (op ? op.offsetTop : 0),
            width: (op ? op.offsetWidth : window.innerWidth)
        });

        return result;
    }




    render() {
        
        const { position, onHide, ref, ...props } = this.props;

        const style: React.CSSProperties = { zIndex: 999, display: "block", position: "absolute" };

        style.top = position.pageY + "px";
        if (document.body.className.contains("rtl-mode"))
            style.right = (position.width - position.pageX) + "px";
        else
            style.left = position.pageX + "px";

        const childrens = React.Children.map(this.props.children,
            (c: React.ReactElement<any>) => c && React.cloneElement(c, { "onSelect": combineFunction(c.props.onSelect, onHide) }));

        const ul = (
            <ul {...props as any} className={classes(props.className, "dropdown-menu sf-context-menu") } style={style}>
                {childrens}
            </ul>
        );

        return <RootCloseWrapper onRootClose={onHide}>{ul}</RootCloseWrapper>;
    }
}