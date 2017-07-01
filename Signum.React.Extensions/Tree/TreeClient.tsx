import * as React from 'react'
import { Route } from 'react-router'
import * as QueryString from 'query-string'
import { ajaxPost, ajaxGet } from '../../../Framework/Signum.React/Scripts/Services';
import { EntitySettings, ViewPromise } from '../../../Framework/Signum.React/Scripts/Navigator'
import * as Navigator from '../../../Framework/Signum.React/Scripts/Navigator'
import * as Finder from '../../../Framework/Signum.React/Scripts/Finder'
import { EntityOperationSettings } from '../../../Framework/Signum.React/Scripts/Operations'
import * as Operations from '../../../Framework/Signum.React/Scripts/Operations'

import { Type } from '../../../Framework/Signum.React/Scripts/Reflection'
import { getMixin, Lite, isLite } from '../../../Framework/Signum.React/Scripts/Signum.Entities'
import { UserEntity } from '../../../Extensions/Signum.React.Extensions/Authorization/Signum.Entities.Authorization'

import { ValueLine, EntityLine, EntityCombo, EntityList, EntityDetail, EntityStrip, EntityRepeater } from '../../../Framework/Signum.React/Scripts/Lines'
import { SearchMessage, JavascriptMessage, ExecuteSymbol, ConstructSymbol_From, ConstructSymbol_Simple, DeleteSymbol } from '../../../Framework/Signum.React/Scripts/Signum.Entities'
import { TreeEntity, TreeOperation } from './Signum.Entities.Tree'
import TreeModal from './TreeModal'
import { FilterRequest, FilterOption, FilterOptionParsed } from "../../../Framework/Signum.React/Scripts/FindOptions";
import { ImportRoute } from "../../../Framework/Signum.React/Scripts/AsyncImport";
import { getAllTypes, getTypeInfo } from "../../../Framework/Signum.React/Scripts/Reflection";
import { TypeInfo } from "../../../Framework/Signum.React/Scripts/Reflection";
import * as AuthClient from '../../../Extensions/Signum.React.Extensions/Authorization/AuthClient'
import TreeButton from './TreeButton'

export function start(options: { routes: JSX.Element[] }) {
    options.routes.push(<ImportRoute path="~/tree/:typeName" onImportModule={() => import("./TreePage")} />);

    Operations.addSettings(
        new EntityOperationSettings(TreeOperation.CreateChild, { isVisible: _ => false }),
        new EntityOperationSettings(TreeOperation.CreateNextSibling, { isVisible: _ => false }),
        new EntityOperationSettings(TreeOperation.Move, { isVisible: _ => false })
    );    

    Finder.ButtonBarQuery.onButtonBarElements.push(ctx => {
        var ti = getTypeInfo(ctx.findOptions.queryKey);

        if (!ctx.searchControl.props.showBarExtension || ti == null || !isTree(ti))
            return undefined;

        return <TreeButton searchControl={ctx.searchControl} />;
    });
}

export function treePath(typeName:string, filterOptions?: FilterOption[]): string {

    const query: any = {};
    if (filterOptions)
        Finder.Encoder.encodeFilters(query, filterOptions);

    return Navigator.history.createHref({ pathname: "~/tree/" + typeName, search: QueryString.stringify(query) });
}

export function hideSiblings(ti: TypeInfo) {
    const type = new Type<TreeEntity>(ti.name);
    if (type.memberInfo(a => a.isSibling).notVisible == undefined)
        type.memberInfo(a => a.isSibling).notVisible = true;
    if (type.memberInfo(a => a.parentOrSibling).notVisible == undefined)
        type.memberInfo(a => a.parentOrSibling).notVisible = true;
}

export function overrideOnFind(ti: TypeInfo) {
    var s = Finder.getSettings(ti.name);
    if (!s) {
        s = { queryName: ti.name };
        Finder.addSettings(s);
    }

    if (!s.onFind)
      s.onFind = (fo, mo) => openTree(ti.name, fo.filterOptions, { title: mo && mo.title });
}

export function isTree(t: TypeInfo) {
    return (t.kind == "Entity" && t.operations && t.operations[TreeOperation.CreateNextSibling.key] != null) || false;
}

export function getAllTreeTypes() {
    return getAllTypes().filter(t => isTree(t));
}

export function openTree<T extends TreeEntity>(type: Type<T>, filterOptions?: FilterOption[], options?: { title?: string }): Promise<Lite<T> | undefined>;
export function openTree(typeName: string, filterOptions?: FilterOption[],options?: TreeModalOptions): Promise<Lite<TreeEntity> | undefined>;
export function openTree(type: Type<TreeEntity> | string, filterOptions?: FilterOption[],options?: TreeModalOptions): Promise<Lite<TreeEntity> | undefined> {
    const typeName = type instanceof Type ? type.typeName : type;

    return import("./TreeModal")
        .then((TM: { default: typeof TreeModal }) => TM.default.open(typeName, filterOptions || [], options));
}

export interface TreeModalOptions {
    title?: string;
}


export interface TreeConfiguration<T extends TreeEntity> {
    hideSiblings: boolean,
    replaceEntityLine: boolean
}


export type TreeNodeState = "Collapsed" | "Expanded" | "Filtered" | "Leaf";

export interface TreeNode {

    lite: Lite<TreeEntity>;
    disabled: boolean;
    childrenCount: number;
    level: number;
    loadedChildren: Array<TreeNode>;
    nodeState: TreeNodeState;
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

    export function getChildren(lite: Lite<TreeEntity>): Promise<Array<TreeNode>>;
    export function getChildren(typeName: string, id: string): Promise<Array<TreeNode>>;
    export function getChildren(typeNameOrLite: string | Lite<TreeEntity>, id?: string): Promise<Array<TreeNode>> {
        if (isLite(typeNameOrLite))
            return ajaxGet<Array<TreeNode>>({ url: `~/api/tree/children/${typeNameOrLite.EntityType}/${typeNameOrLite.id != null ? typeNameOrLite.id : ""}` }).then(ns => fixNodes(ns));
        else
            return ajaxGet<Array<TreeNode>>({ url: `~/api/tree/children/${typeNameOrLite}/${id != null ? id : ""}` }).then(ns => fixNodes(ns));
    }

    export function getRoots(typeName: string): Promise<Array<TreeNode>> {
        return ajaxGet<Array<TreeNode>>({ url: `~/api/tree/roots/${typeName}` }).then(ns => fixNodes(ns));
    }

    export function findNodes(typeName: string, filters: FilterRequest[]): Promise<Array<TreeNode>> {
        return ajaxPost<Array<TreeNode>>({ url: `~/api/tree/findNodes/${typeName}` }, filters).then(ns => fixNodes(ns));
    }
}