import * as React from 'react'
import { RouteObject } from 'react-router'
import { ajaxPost, ajaxGet } from '@framework/Services';
import { Navigator, EntitySettings } from '@framework/Navigator'
import * as AppContext from '@framework/AppContext'
import { Finder } from '@framework/Finder'
import { Operations, EntityOperationSettings } from '@framework/Operations'
import { Type, tryGetTypeInfo } from '@framework/Reflection'
import { getToString, Lite, liteKey } from '@framework/Signum.Entities'
import { TreeEntity, TreeOperation, MoveTreeModel, TreeMessage, UserTreePartEntity } from './Signum.Tree'
import TreeModal from './TreeModal'
import { FilterRequest, FilterOption } from "@framework/FindOptions";
import { ImportComponent } from '@framework/ImportComponent'
import { getAllTypes, getTypeInfo } from "@framework/Reflection";
import { TypeInfo } from "@framework/Reflection";
import TreeButton from './TreeButton'
import { toLite } from "@framework/Signum.Entities";
import { SearchControlLoaded } from "@framework/Search";
import { LiteAutocompleteConfig } from '@framework/Lines';
import { QueryString } from '@framework/QueryString';
import { DashboardClient } from '../Signum.Dashboard/DashboardClient'
import { UserQueryClient } from '../Signum.UserQueries/UserQueryClient'
import { DisabledMixin } from '@framework/Signum.Basics';

export namespace TreeClient {
  
export function start(options: { routes: RouteObject[] }) {
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
    defaultIcon: () => ({ icon: ["far", "network-wired"], iconColor: "#B7950B" }),
    withPanel: c => true,
    getQueryNames: c => [c.userQuery?.query].notNull(),
    handleEditClick: !Navigator.isViewable(UserTreePartEntity) || Navigator.isReadOnly(UserTreePartEntity) ? undefined :
      (c, e, cdRef, ev) => {
        ev.preventDefault();
        return Navigator.view(c.userQuery!).then(uq => Boolean(uq));
      },
    handleTitleClick:
      (c, e, cdRef, ev) => {
        ev.preventDefault();
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


export function treePath(typeName: string, filterOptions?: (FilterOption | null | undefined)[]): string {

  const query: any = {};
  if (filterOptions)
    Finder.Encoder.encodeFilters(query, filterOptions.notNull());

  return "/tree/" + typeName + "?" + QueryString.stringify(query);
}

export function hideSiblingsAndIsDisabled(ti: TypeInfo) {
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

export function overrideOnFind(ti: TypeInfo) {
  var qs = getQuerySetting(ti.name);

  if (!qs.onFind)
    qs.onFind = (fo, mo) => openTree(ti.name, fo.filterOptions, { title: mo?.title });
}

export function overrideAutocomplete(ti: TypeInfo) {
  var es = getEntitySetting(ti.name);

  if (!es.autocomplete)
    es.autocomplete = fo => fo ? null : new LiteAutocompleteConfig((ac, str) => API.findTreeLiteLikeByName(ti.name, str, 5, ac));

  if (!es.autocompleteDelay)
    es.autocompleteDelay = 750;
}

export function overrideDefaultOrder(ti: TypeInfo) {
  var qs = getQuerySetting(ti.name);

  if (!qs.defaultOrders) {
    qs.defaultOrders = [{ token: "FullName", orderType: "Ascending" }];
  }
}

export function isTree(t: TypeInfo) {
  return (t.kind == "Entity" && t.operations && t.operations[TreeOperation.CreateNextSibling.key] != null) || false;
}

export function getAllTreeTypes() {
  return getAllTypes().filter(t => isTree(t));
}

export function openTree<T extends TreeEntity>(type: Type<T>, filterOptions?: (FilterOption | null | undefined)[], options?: TreeModalOptions): Promise<Lite<T> | undefined>;
export function openTree(typeName: string, filterOptions?: (FilterOption | null | undefined)[], options?: TreeModalOptions): Promise<Lite<TreeEntity> | undefined>;
export function openTree(type: Type<TreeEntity> | string, filterOptions?: (FilterOption | null | undefined)[], options?: TreeModalOptions): Promise<Lite<TreeEntity> | undefined> {
  const typeName = type instanceof Type ? type.typeName : type;

  return import("./TreeModal")
    .then((TM: { default: typeof TreeModal }) => TM.default.open(typeName, filterOptions?.notNull() ?? [], options));
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

export function register<T extends TreeEntity>(type: Type<T>, setting: TreeSettings<T>) {
  settings[type.typeName] = setting;
}



export function fixState(node: TreeNode) {

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

  export function findNodes(typeName: string, request: FindNodesRequest): Promise<Array<TreeNode>> {
    return ajaxPost<Array<TreeNode>>({ url: `/api/tree/findNodes/${typeName}` }, request).then(ns => fixNodes(ns));
  }

  export interface FindNodesRequest {
    userFilters: Array<FilterRequest>;
    frozenFilters: Array<FilterRequest>;
    expandedNodes: Array<Lite<TreeEntity>>;
    loadDescendants: boolean;
  }
}

}

export type TreeNodeState = "Collapsed" | "Expanded" | "Filtered" | "Leaf";

export interface TreeNode {
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
