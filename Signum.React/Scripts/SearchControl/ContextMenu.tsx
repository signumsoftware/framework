import * as React from 'react'
import { Dic, classes, combineFunction } from '../Globals'
import * as RootCloseWrapper from 'react-overlays/lib/RootCloseWrapper'

export interface ContextMenuProps extends React.Props<ContextMenu>, React.HTMLAttributes {
    position: { pageX: number; pageY: number };
    onHide: () => void;
}

export class ContextMenu extends React.Component<ContextMenuProps, {}> {
    render() {

        const { position } = this.props;
        const props = Dic.without(this.props, { position, ref: null });

        const style: React.CSSProperties = { left: position.pageX + "px", top: position.pageY + "px", zIndex: 999, display: "block", position: "absolute" };

        var childrens = React.Children.map(this.props.children,
            (c: React.ReactElement<any>) => React.cloneElement(c, { "onSelect": combineFunction(c.props.onSelect, this.props.onHide) }));

        const ul = (
            <ul {...props as any}  className={classes(props.className, "dropdown-menu sf-context-menu") } style={style}>
                {childrens}
            </ul>
        );

        return <RootCloseWrapper onRootClose={this.props.onHide}>{ul}</RootCloseWrapper>;
    }
}