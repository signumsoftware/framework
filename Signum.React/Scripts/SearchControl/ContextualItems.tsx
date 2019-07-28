import * as React from 'react'
import { QueryDescription, } from '../FindOptions'
import { Lite, Entity } from '../Signum.Entities'
import SearchControlLoaded from "./SearchControlLoaded";
import { DropdownItem } from '../Components';
import { StyleContext } from '../TypeContext';

export interface MenuItemBlock {
  header: string;
  menuItems: React.ReactElement<any>[];
}

export interface ContextualItemsContext<T extends Entity> {
  lites: Lite<T>[];
  queryDescription: QueryDescription;
  markRows: (dictionary: MarkedRowsDictionary) => void;
  container?: SearchControlLoaded | React.Component<any, any>;
  styleContext?: StyleContext;
}

export interface MarkedRowsDictionary {
  [liteKey: string]: string | MarkedRow;
}

export interface MarkedRow {
  className: string;
  message?: string;
}

export function clearContextualItems() {
  onContextualItems.clear();
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
        result.push(<DropdownItem divider />);

      if (block.header)
        result.push(<DropdownItem header>{block.header}</DropdownItem>);

      if (block.header)
        result.splice(result.length, 0, ...block.menuItems);
    });

    return result;
  });
}




