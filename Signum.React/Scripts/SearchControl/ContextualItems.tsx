
import * as React from 'react'
import { MenuItem, Overlay } from 'react-bootstrap'
import { Dic, classes, combineFunction } from '../Globals'
import { QueryDescription, } from '../FindOptions'
import { SearchMessage, JavascriptMessage, Lite, Entity } from '../Signum.Entities'
import * as RootCloseWrapper from 'react-overlays/lib/RootCloseWrapper'


export interface MenuItemBlock {
    header: string;
    menuItems: React.ReactElement<any>[];
}

export interface ContextualItemsContext {
    lites: Lite<Entity>[];
    queryDescription: QueryDescription;
}

export const onContextualItems: ((ctx: ContextualItemsContext) => Promise<MenuItemBlock>)[] = [];

export function getContextualItems(ctx: ContextualItemsContext): Promise<React.ReactElement<any>[]> {

    const blockPromises = onContextualItems.map(func => func(ctx));

    return Promise.all(blockPromises).then(blocks => {

        const result: React.ReactElement<any>[] = []
        blocks.forEach(block => {

            if (block == null || block.menuItems == null || block.menuItems.length == 0)
                return;

            if (result.length)
                result.push(<MenuItem divider/>);

            if (block.header)
                result.push(<MenuItem header>{block.header}</MenuItem>);

            if (block.header)
                result.splice(result.length, 0, ...block.menuItems);
        });

        return result;
    });
}


export interface ContextMenuProps extends React.Props<ContextMenu>, React.HTMLAttributes {
    position: { pageX: number; pageY: number };
    onHide: () => void;
}

export class ContextMenu extends React.Component<ContextMenuProps, {}> {
    render() {

        const { position } = this.props;
        const props = Dic.without(this.props, { position, ref: null });

        const style: React.CSSProperties = { left: position.pageX + "px", top: position.pageY + "px", zIndex: 9999, display: "block", position: "absolute" };

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

