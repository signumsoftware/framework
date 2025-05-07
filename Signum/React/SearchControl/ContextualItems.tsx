import * as React from 'react'
import { QueryDescription, } from '../FindOptions'
import { Navigator } from '../Navigator'
import { Lite, Entity } from '../Signum.Entities'
import SearchControlLoaded from "./SearchControlLoaded";
import { StyleContext } from '../TypeContext';
import { Dropdown } from 'react-bootstrap';


export interface SearchableMenuItem  {
  fullText: string; //used for filtering
  menu : React.ReactElement<any>;
}

export type ContextualMenuItem = React.ReactElement<any> | SearchableMenuItem;

export interface MenuItemBlock {
  header: string;
  menuItems: ContextualMenuItem[];
}

export interface ContextMenuPack {
  items: ContextualMenuItem[],
  showSearch: boolean;
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
  status: "Error" | "Warning" | "Success" | "Muted";
  message?: string;
}

export function clearContextualItems() {
  onContextualItems.clear();
}

export const onContextualItems: ((ctx: ContextualItemsContext<Entity>) => Promise<MenuItemBlock | undefined> | undefined)[] = [];

export function renderContextualItems(ctx: ContextualItemsContext<Entity>): Promise<ContextMenuPack> {

  const blockPromises = onContextualItems.map(func => func(ctx));
  return Promise.all(blockPromises).then(blocks => {

    const items: ContextualMenuItem[] = []
    blocks.forEach(block => {

      if (block == undefined || block.menuItems == undefined || block.menuItems.length == 0)
        return;

      if (items.length)
        items.push(<Dropdown.Divider />);

      if (block.header)
        items.push(<Dropdown.Header>{block.header}</Dropdown.Header>);

      if (block.header)
        items.splice(items.length, 0, ...block.menuItems);
    });

    const showSearchFunc = ctx.lites[0] && Navigator.getSettings(ctx.lites[0].EntityType)?.showContextualSearchBox;
    const showSearch = Boolean(showSearchFunc && showSearchFunc(ctx, blocks.notNull()));

    return ({ items, showSearch });
  });
}




