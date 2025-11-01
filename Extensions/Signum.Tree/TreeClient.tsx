import * as React from 'react'
import { RouteObject } from 'react-router'
import { ajaxPost, ajaxGet } from '@framework/Services';
import { Navigator, EntitySettings } from '@framework/Navigator'
import * as AppContext from '@framework/AppContext'
import { Finder } from '@framework/Finder'
import { Operations, EntityOperationSettings } from '@framework/Operations'
import { PseudoType, QueryKey, Type, tryGetTypeInfo } from '@framework/Reflection'
import { getToString, Lite, liteKey } from '@framework/Signum.Entities'
import { TreeEntity, TreeOperation, MoveTreeModel, TreeMessage, UserTreePartEntity } from './Signum.Tree'
import TreeModal from './TreeModal'
import { FilterRequest, FilterOption, ColumnOption, ResultRow, FilterOptionParsed, ColumnOptionParsed, QueryDescription, ColumnRequest } from "@framework/FindOptions";
import { ImportComponent } from '@framework/ImportComponent'
import { getAllTypes, getTypeInfo } from "@framework/Reflection";
import { TypeInfo } from "@framework/Reflection";
import TreeButton from './TreeButton'
import { toLite } from "@framework/Signum.Entities";
import { ColumnOptionsMode, FindOptions, FindOptionsParsed, Pagination, ResultTable, SearchControlLoaded } from "@framework/Search";
import { LiteAutocompleteConfig } from '@framework/Lines';
import { QueryString } from '@framework/QueryString';
import { DashboardClient } from '../Signum.Dashboard/DashboardClient'
import { UserQueryClient } from '../Signum.UserQueries/UserQueryClient'
import { DisabledMixin } from '@framework/Signum.Basics';
import { Dic } from '../../Signum/React/Globals';

export namespace TreeClient {

  export function start(options: { routes: RouteObject[] }): void {
    options.routes.push({ path: "/tree/:typeName", element: <ImportComponent onImport={() => import("./TreePage")} /> });

    Navigator.addSettings(new EntitySettings(MoveTreeModel, e => import('./Templates/MoveTreeModel')));
    Navigator.addSettings(new EntitySettings(UserTreePartEntity, e => import('./Dashboard/Admin/UserTreePart')));

    Operations.addSettings(
      new EntityOperationSettings(TreeOperation.CreateChild, { contextual: { isVisible: ctx => ctx.context.container instanceof SearchControlLoaded } }),
      new EntityOperationSettings(TreeOperation.CreateNextSibling, { contextual: { isVisible: ctx => ctx.context.container instanceof SearchControlLoaded } }),
      new EntityOperationSettings(TreeOperation.Move, {
        onClick: ctx => moveModal(toLite(ctx.entity)).then(m => m && ctx.defaultClick(m)),
        contextual: { onClick: ctx => moveModal(ctx.context.lites[0]).then(m => m && ctx.defaultClick(m)) }
      }),
      new EntityOperationSettings(TreeOperation.Copy, {
        onClick: ctx => copyModal(toLite(ctx.entity)).then(m => {
          if (m) {
            ctx.onConstructFromSuccess = pack => { Operations.notifySuccess(); return Promise.resolve(); };
            ctx.defaultClick(m);
          }
        }),
        contextual: {
          onClick: ctx => copyModal(ctx.context.lites[0]).then(m => {
            if (m) {
              ctx.onConstructFromSuccess = pack => Operations.notifySuccess();
              ctx.defaultClick(m);
            }
          })
        }
      })
    );

    Finder.ButtonBarQuery.onButtonBarElements.push(ctx => {
      var ti = tryGetTypeInfo(ctx.findOptions.queryKey);

      if (
        !ti || !isTree(ti) ||
        !ctx.searchControl.props.showBarExtension ||
        !(ctx.searchControl.props.showBarExtensionOption?.showTreeButton ?? ctx.searchControl.props.largeToolbarButtons))
        return undefined;

      return { button: <TreeButton searchControl={ctx.searchControl} /> };
    });

    DashboardClient.registerRenderer(UserTreePartEntity, {
      component: () => import('./Dashboard/View/UserTreePart').then((a: any) => a.default),
      icon: () => ({ icon: ["far", "network-wired"], iconColor: "#B7950B" }),
      withPanel: c => true,
      getQueryNames: c => [c.userQuery?.query].notNull(),
      handleEditClick: !Navigator.isViewable(UserTreePartEntity) || Navigator.isReadOnly(UserTreePartEntity) ? undefined :
        (c, e, cdRef, ev) => {
          return Navigator.view(c.userQuery!).then(uq => Boolean(uq));
        },
      handleTitleClick:
        (c, e, cdRef, ev) => {
          ev.persist();
          UserQueryClient.Converter.toFindOptions(c.userQuery!, e)
            .then(cr => AppContext.pushOrOpenInTab(Finder.findOptionsPath(cr, { userQuery: liteKey(toLite(c.userQuery!)) }), ev))
        }
    });


  }



  function moveModal(lite: Lite<TreeEntity>) {
    const s = settings[lite.EntityType];
    if (s?.createMoveModel)
      return s.createMoveModel(lite, {});
    else
      return Navigator.view(MoveTreeModel.New({ insertPlace: "LastNode" }), {
        title: TreeMessage.Move0.niceToString(getToString(lite)),
        modalSize: "md",
        extraProps: { lite },
      })
  }

  function copyModal(lite: Lite<TreeEntity>) {
    const s = settings[lite.EntityType];
    if (s?.createCopyModel)
      return s.createCopyModel(lite, {});
    else
      return Navigator.view(MoveTreeModel.New({ insertPlace: "LastNode" }), {
        title: TreeMessage.Copy0.niceToString(getToString(lite)),
        modalSize: "md",
        extraProps: { lite },
      });
  }


  export function treePath(to: TreeOptions, extra?: any): string {

    const fo = toFindOptions(to);
    const result = Finder.findOptionsPath(fo).replace("/find/", "/tree/");
    
    return result;
  }

  export function toFindOptions(to: TreeOptions): FindOptions {
    const fo = {
      queryName: to.typeName,
      filterOptions: to.filterOptions,
      columnOptions: to.columnOptions,
      columnOptionsMode: to.columnOptionsMode, 
    } as FindOptions;

    return fo;
  }

  export function toFindOptionsParsed(top: TreeOptionsParsed): FindOptionsParsed {

    const qs = Finder.getSettings(top.typeName);
    const fop = {
      queryKey: top.typeName,
      groupResults: false,
      filterOptions: top.filterOptions,
      columnOptions: top.columnOptions,
      orderOptions: [],
      pagination: qs?.pagination ?? Finder.Options.defaultPagination,
    } as FindOptionsParsed;

    return fop;
  }

  export function toTreeOptions(top: TreeOptionsParsed, qd: QueryDescription): TreeOptions {
    const fop = toFindOptionsParsed(top);
    const fo = Finder.toFindOptions(fop, qd, false);

    var to = {
      typeName: fo.queryName,
      filterOptions: fo.filterOptions,
      columnOptions: fo.columnOptions,
      columnOptionsMode: fo.columnOptionsMode,
    } as TreeOptions;

    return to;
  }

  export function parseTreeOptions(to: TreeOptions, qd: QueryDescription): Promise<TreeOptionsParsed> {
    const fo = toFindOptions(to);
    return Finder.parseFindOptions(fo, qd, false)
      .then(fop => {
        return {
          typeName: fop.queryKey,
          filterOptions: fop.filterOptions,
          columnOptions: fop.columnOptions,
        } as TreeOptionsParsed;
      });
  }

  export function parseTreeOptionsPath(queryName: PseudoType | QueryKey, query: any): TreeOptions {
    const fo = Finder.parseFindOptionsPath(queryName, query);
    const to = {
      typeName: fo.queryName,
      filterOptions: fo.filterOptions,
      columnOptions: fo.columnOptions,
      columnOptionsMode: fo.columnOptionsMode,
    } as TreeOptions;

    return to;
  }

  export function hideSiblingsAndIsDisabled(ti: TypeInfo): void {
    const type = new Type<TreeEntity>(ti.name);

    if (type.memberInfo(a => a.isSibling).notVisible == undefined)
      type.memberInfo(a => a.isSibling).notVisible = true;

    if (type.memberInfo(a => a.parentOrSibling).notVisible == undefined)
      type.memberInfo(a => a.parentOrSibling).notVisible = true;

    if (type.hasMixin(DisabledMixin)) {
      var mi = type.mixinMemberInfo(DisabledMixin, e => e.isDisabled);
      if (mi.notVisible == undefined)
        mi.notVisible = true;
    }
  }

  function getQuerySetting(typeName: string) {
    var qs = Finder.getSettings(typeName);
    if (!qs) {
      qs = { queryName: typeName };
      Finder.addSettings(qs);
    }
    return qs;
  }

  function getEntitySetting(typeName: string) {
    var es = Navigator.getSettings(typeName);
    if (!es) {
      es = new EntitySettings(typeName);
      Navigator.addSettings(es);
    }
    return es;
  }

  export function overrideOnFind(ti: TypeInfo): void {
    var qs = getQuerySetting(ti.name);

    if (!qs.onFind)
      qs.onFind = (fo, mo) => openTree({ typeName: ti.name, filterOptions: fo.filterOptions, columnOptions: fo.columnOptions, columnOptionsMode: fo.columnOptionsMode }, { title: mo?.title });
  }

  export function overrideAutocomplete(ti: TypeInfo): void {
    var es = getEntitySetting(ti.name);

    if (!es.autocomplete)
      es.autocomplete = fo => fo ? null : new LiteAutocompleteConfig((ac, str) => API.findTreeLiteLikeByName(ti.name, str, 5, ac));

    if (!es.autocompleteDelay)
      es.autocompleteDelay = 750;
  }

  export function overrideDefaultOrder(ti: TypeInfo): void {
    var qs = getQuerySetting(ti.name);

    if (!qs.defaultOrders) {
      qs.defaultOrders = [{ token: "FullName", orderType: "Ascending" }];
    }
  }

  export function isTree(t: TypeInfo): boolean {
    return (t.kind == "Entity" && t.operations && t.operations[TreeOperation.CreateNextSibling.key] != null) || false;
  }

  export function getAllTreeTypes(): TypeInfo[] {
    return getAllTypes().filter(t => isTree(t));
  }

  export function openTree(to: TreeOptions, options?: TreeModalOptions): Promise<Lite<TreeEntity> | undefined> {

    return import("./TreeModal")
      .then((TM: { default: typeof TreeModal }) => TM.default.open(to, options));
  }

  export interface TreeModalOptions {
    title?: React.ReactNode;
    excludedNodes?: Array<Lite<TreeEntity>>;
  }


  export interface TreeSettings<T extends TreeEntity> {
    createCopyModel?: (from: T | Lite<T>, dropConfig: Partial<MoveTreeModel>) => Promise<MoveTreeModel | undefined>;
    createMoveModel?: (from: T | Lite<T>, dropConfig: Partial<MoveTreeModel>) => Promise<MoveTreeModel | undefined>;
    dragTargetIsValid?: (draggedNode: TreeNode, targetNode: TreeNode | null) => Promise<boolean>;
  }

  export const settings: {
    [typeName: string]: TreeSettings<any>;
  } = {};

  export function register<T extends TreeEntity>(type: Type<T>, setting: TreeSettings<T>) : void {
    settings[type.typeName] = setting;
  }



  export function fixState(node: TreeNode): void {

    node.nodeState = node.childrenCount == 0 ? "Leaf" :
      node.loadedChildren.length == 0 ? "Collapsed" :
        node.childrenCount == node.loadedChildren.length ? "Expanded" : "Filtered";

    node.loadedChildren.forEach(n => fixState(n));
  }

  function fixNodes(nodes: Array<TreeNode>) {
    nodes.forEach(n => fixState(n));
    return nodes;
  }


  export namespace API {
    export function findTreeLiteLikeByName(typeName: string, subString: string, count: number, abortSignal?: AbortSignal): Promise<Array<Lite<TreeEntity>>> {
      return ajaxGet({ url: `/api/tree/findLiteLikeByName/${typeName}/${subString}/${count}`, signal: abortSignal });
    }

    export function findNodes(typeName: string, request: FindNodesRequest): Promise<FindNodesResponse> {
      return ajaxPost<FindNodesResponse>({ url: `/api/tree/findNodes/${typeName}` }, request).then(response => {
        fixNodes(response.nodes);
        return response;
      });
    }

    export function getNode(typeName: string, request: GetNodeRequest): Promise<TreeNode> {
      return ajaxPost<TreeNode>({ url: `/api/tree/getNode/${typeName}` }, request);
    }

    export interface FindNodesRequest {
      userFilters: Array<FilterRequest>;
      frozenFilters: Array<FilterRequest>;
      columns: Array<ColumnRequest>;
      expandedNodes: Array<Lite<TreeEntity>>;
      loadDescendants: boolean;
    }

    export interface FindNodesResponse {
      columns: string[];
      nodes: TreeNode[];
    }

    export interface GetNodeRequest {
      lite: Lite<TreeEntity>;
      columns: Array<ColumnRequest>;
    }
  }
}

export interface TreeOptions {
  typeName: string;
  filterOptions?: (FilterOption | null | undefined)[];
  columnOptions?: (ColumnOption | null | undefined)[];
  columnOptionsMode?: ColumnOptionsMode;
}

export interface TreeOptionsParsed {
  typeName: string;
  filterOptions: FilterOptionParsed[];
  columnOptions: ColumnOptionParsed[];
}

export type TreeNodeState = "Collapsed" | "Expanded" | "Filtered" | "Leaf";

export interface TreeNode {
  values: any[];
  lite: Lite<TreeEntity>;
  name: string;
  fullName: string;
  disabled: boolean;
  childrenCount: number;
  level: number;
  loadedChildren: Array<TreeNode>;
  nodeState: TreeNodeState;
}

declare module '@framework/SearchControl/SearchControlLoaded' {

  export interface ShowBarExtensionOption {
    showTreeButton?: boolean;
  }
}
