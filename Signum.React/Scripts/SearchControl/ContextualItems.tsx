
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
    markRows: (dictionary: MarkRowsDictionary) => void;
}

export interface MarkRowsDictionary {
    [liteKey: string]: string | { style: string, message: string };
}

export const onContextualItems: ((ctx: ContextualItemsContext) => Promise<MenuItemBlock>)[] = [];

export function renderContextualItems(ctx: ContextualItemsContext): Promise<React.ReactElement<any>[]> {

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




