
import * as React from 'react'
import { MenuItem, Overlay } from 'react-bootstrap'
import { Dic, classes, combineFunction } from '../Globals'
import { QueryDescription, } from '../FindOptions'
import { SearchMessage, JavascriptMessage, Lite, Entity } from '../Signum.Entities'
import * as RootCloseWrapper from 'react-overlays/lib/RootCloseWrapper'
import SearchControlLoaded from "./SearchControlLoaded";


export interface MenuItemBlock {
    header: string;
    menuItems: React.ReactElement<any>[];
}

export interface ContextualItemsContext<T extends Entity> {
    lites: Lite<T>[];
    queryDescription: QueryDescription;
    markRows: (dictionary: MarkedRowsDictionary) => void;
    searchControl: SearchControlLoaded;
}

export interface MarkedRowsDictionary {
    [liteKey: string]: string | MarkedRow;
}

export interface MarkedRow {
    className: string;
    message?: string;
}

export const onContextualItems: ((ctx: ContextualItemsContext<Entity>) => Promise<MenuItemBlock | undefined> | undefined)[] = [];

export function renderContextualItems(ctx: ContextualItemsContext<Entity>): Promise<React.ReactElement<any>[]> {

    const blockPromises = onContextualItems.map(func => func(ctx));

    return Promise.all(blockPromises).then(blocks => {

        const result: React.ReactElement<any>[] = []
        blocks.forEach(block => {

            if (block == undefined || block.menuItems == undefined || block.menuItems.length == 0)
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




