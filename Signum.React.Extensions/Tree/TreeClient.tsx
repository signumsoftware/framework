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
import { FilterRequest } from "../../../Framework/Signum.React/Scripts/FindOptions";
import { ImportRoute } from "../../../Framework/Signum.React/Scripts/AsyncImport";
import { getAllTypes } from "../../../Framework/Signum.React/Scripts/Reflection";
import { TypeInfo } from "../../../Framework/Signum.React/Scripts/Reflection";

export function start(options: { routes: JSX.Element[] }) {
    options.routes.push(<ImportRoute path="~/tree/:typeName" onImportModule={() => _import("./TreePage")} />);

    Operations.addSettings(
        new EntityOperationSettings(TreeOperation.CreateChild, { isVisible: _ => false }),
        new EntityOperationSettings(TreeOperation.CreateNextSibling, { isVisible: _ => false }),
        new EntityOperationSettings(TreeOperation.Move, { isVisible: _ => false })
    );    
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
        Finder.addSettings({
            queryName: ti.name,
            onFind: (fo, mo) => openTree(ti.name, { title: mo && mo.title })
        });
    }
}

export function isTree(t: TypeInfo) {
    return t.kind == "Entity" && t.operations && t.operations[TreeOperation.CreateNextSibling.key];
}

export function getAllTreeTypes() {
    return getAllTypes().filter(t => isTree(t));
}

export function openTree<T extends TreeEntity>(type: Type<T>, options?: { title?: string }): Promise<Lite<T>>;
export function openTree(typeName: string, options?: { title?: string }): Promise<Lite<TreeEntity>>;
export function openTree(type: Type<TreeEntity> | string, options?: TreeModalOptions): Promise<Lite<TreeEntity>> {
    const typeName = type instanceof Type ? type.typeName : type;

    return _import("./TreeModal")
        .then((TM: { default: typeof TreeModal }) => TM.default.open(typeName, options));
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