import * as React from 'react'
import { Dic, classes, combineFunction } from '../Globals'
import * as RootCloseWrapper from 'react-overlays/lib/RootCloseWrapper'

export interface ContextMenuProps extends React.Props<ContextMenu>, React.HTMLAttributes {
    position: { pageX: number; pageY: number };
    onHide: () => void;
}

export default class ContextMenu extends React.Component<ContextMenuProps, {}> {
    render() {

        const { position, onHide } = this.props;
        const props = Dic.without(this.props, { position, onHide, ref: undefined });

        const style: React.CSSProperties = { left: position.pageX + "px", top: position.pageY + "px", zIndex: 999, display: "block", position: "absolute" };

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