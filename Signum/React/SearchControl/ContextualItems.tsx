import * as React from 'react'
import { QueryDescription, } from '../FindOptions'
import { Lite, Entity } from '../Signum.Entities'
import SearchControlLoaded from "./SearchControlLoaded";
import { StyleContext } from '../TypeContext';
import { Dropdown } from 'react-bootstrap';
import { softCast } from '../Globals';
import ErrorModal from '../Modals/ErrorModal';

export interface MenuItemBlock {
  header: string;
  menuItems: React.ReactElement[];
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

export function clearContextualItems(): void {
  onContextualItems.clear();
}

export const onContextualItems: ((ctx: ContextualItemsContext<Entity>) => Promise<MenuItemBlock | undefined> | undefined)[] = [];

export function renderContextualItems(ctx: ContextualItemsContext<Entity>): Promise<React.ReactElement[]> {

  const blockPromises = onContextualItems.map(func => func(ctx)?.catch(a => ({ error: a, func })));

  return Promise.all(blockPromises).then(blocks => {

    const result: React.ReactElement[] = []
    blocks.forEach(block => {

      if (block == undefined)
        return;

      if ("error" in block) {
        result.push(
          <Dropdown className="text-danger" onClick={() => ErrorModal.showErrorModal(block.error)}>
            Error in {block.func.name}
          </Dropdown >
        );
        return;
      }
        
      if (block.menuItems == undefined || block.menuItems.length == 0)
        return;

      if (result.length)
        result.push(<Dropdown.Divider />);

      if (block.header)
        result.push(<Dropdown.Header>{block.header}</Dropdown.Header>);

      if (block.header)
        result.splice(result.length, 0, ...block.menuItems);
    });

    return result;
  });
}




