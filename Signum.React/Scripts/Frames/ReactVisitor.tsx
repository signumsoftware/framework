import * as React from 'react'
import { Tab } from '../Components/Tabs'
import { Dic } from '../Globals'
import * as Navigator from '../Navigator'
import { ResultTable, FindOptions, FilterOption, QueryDescription, isFilterGroupOption, FilterGroupOption, FilterConditionOption } from '../FindOptions'
import { Entity, Lite, is, toLite, LiteMessage, getToString, EntityPack, ModelState, ModifiableEntity } from '../Signum.Entities'
import { TypeContext, StyleOptions, EntityFrame } from '../TypeContext'
import { getTypeInfo, TypeInfo, PropertyRoute, ReadonlyBinding, getTypeInfos } from '../Reflection'
import { ColumnOption, OrderOption, Pagination } from '../Search';
import { func } from 'prop-types';


export class ReactVisitor {
    
    visitChild(child: React.ReactChild): React.ReactNode {

        if (child == undefined)
            return child;

        if (typeof child == "string")
            return child;

        if (typeof child == "number")
            return child;

        if (typeof child == "boolean")
            return child;

        return this.visitElement(child as React.ReactElement<any>);
    }

    visitElement(element: React.ReactElement<any>): React.ReactNode {

        if (element.props.children == undefined || element.props.children.count == 0)
            return element;

        const oldChildren = React.Children.toArray(element.props.children);

        const newChildren = React.Children.map(oldChildren, c => this.visitChild(c)); 

        if (newChildren.length != oldChildren.length || newChildren.some((n, i) => n !== oldChildren[i]))
            return React.cloneElement(element, undefined, newChildren);

        return element;
    }

    visit(element: React.ReactElement<any>): React.ReactElement<any> {
        const result = this.visitElement(element);

        if (Array.isArray(result))
            return React.createElement("div", {}, ...result);

        return result as React.ReactElement<any>;
    }
}


export class ReplaceVisitor extends ReactVisitor {

    constructor(
        public predicate: (e: React.ReactElement<any>) => boolean,
        public replacement: (e: React.ReactElement<any>) => React.ReactNode) {
        super();
    }

    visitElement(element: React.ReactElement<any>) {

        if (this.predicate(element)) {

            var node = this.replacement(element);

            var validatedNode = React.Children.map(node, c => new ReactValidator().visitChild(c));

            return validatedNode;
        }

        return super.visitElement(element);
    }    
}

export class ReactValidator extends ReactVisitor {
    visitElement(element: React.ReactElement<any>) {

        var error = this.getError(element);
        if (error) {
            return <div className="alert alert-danger">{error}</div>;
        }

        return super.visitElement(element);
    }  

    static validTagRegex = /^[a-zA-Z][a-zA-Z:_\.\-\d]*$/

    getError(element: React.ReactElement<any>) {

        if (typeof element.type === 'function')
            return undefined;

        if (typeof element.type === 'string') {
            if (!ReactValidator.validTagRegex.exec(element.type))
                return "Invalid tag: " + element.type;

            return undefined;
        }

        return "React.createElement: type should not be null, undefined, boolean, or number. It should be a string (for DOM elements) or a ReactClass (for composite components).";
    }

}

export class ViewReplacer<T extends ModifiableEntity> {

    constructor(
        public result: React.ReactElement<any>,
        public ctx: TypeContext<T>
    ) {
    }

    removeElement(filter: (e: React.ReactElement<any>) => boolean): this {

        this.result = new ReplaceVisitor(
            e => filter(e),
            e => []
        ).visit(this.result);

        return this;
    }

    insertAfterElement(filter: (e: React.ReactElement<any>) => boolean, newElements: (e: React.ReactElement<any>) => (React.ReactElement<any> | undefined | false | null)[]): this {

        this.result = new ReplaceVisitor(
            e => filter(e),
            e => [e, ...newElements(e)]
        ).visit(this.result);

        return this;
    }

    insertBeforeElement(filter: (e: React.ReactElement<any>) => boolean, newElements: (e: React.ReactElement<any>) => (React.ReactElement<any> | undefined | false | null)[]): this {

        this.result = new ReplaceVisitor(
            e => filter(e),
            e => [...newElements(e), e]
        ).visit(this.result);

        return this;
    }

    replaceElement(filter: (e: React.ReactElement<any>) => boolean, newElements: (e: React.ReactElement<any>) => (React.ReactElement<any> | undefined | false | null)[]): this {

        this.result = new ReplaceVisitor(
            e => filter(e),
            e => [...newElements(e)]
        ).visit(this.result);

        return this;
    }

    removeLine(propertyRoute: (entity: T) => any): this {

        var pr = this.ctx.propertyRoute.addLambda(propertyRoute);

        this.result = new ReplaceVisitor(
            e => hasPropertyRoute(e, pr),
            e => []
        ).visit(this.result);

        return this;
    }

    replaceFindOptions(filter: (findOptions: FindOptions) => boolean, modifier: (clone: FindOptions) => void) {
        this.result = new ReplaceVisitor(
            e => e.props.findOptions && filter(e.props.findOptions),
            e => {
                var clone = cloneFindOptions(e.props.findOptions);
                modifier(clone);
                return React.cloneElement(e, { findOptions: clone });
            }
        ).visit(this.result);

        return this;
    }



    insertAfterLine(propertyRoute: (entity: T) => any, newElements: (ctx: TypeContext<T>) => (React.ReactElement<any> | undefined | false | null)[]): this {

        var pr = this.ctx.propertyRoute.addLambda(propertyRoute);

        this.result = new ReplaceVisitor(
            e => hasPropertyRoute(e, pr),
            e => [e, ...newElements(this.previousTypeContext(e))]
        ).visit(this.result);

        return this;
    }

    insertBeforeLine(propertyRoute: (entity: T) => any, newElements: (ctx: TypeContext<T>) => (React.ReactElement<any> | undefined)[]): this {

        var pr = this.ctx.propertyRoute.addLambda(propertyRoute);

        this.result = new ReplaceVisitor(
            e => hasPropertyRoute(e, pr),
            e => [...newElements(this.previousTypeContext(e)), e]
        ).visit(this.result);

        return this;
    }

    previousTypeContext(e: React.ReactElement<any>) {
        var ctx = e.props.ctx as TypeContext<any>;

        var parentCtx = ctx.findParentCtx(this.ctx.value.Type);

        return parentCtx as TypeContext<T>;
    }

    replaceLine(propertyRoute: (entity: T) => any, newElements: (e: React.ReactElement<any>) => (React.ReactElement<any> | undefined)[]) {
        var pr = this.ctx.propertyRoute.addLambda(propertyRoute);

        this.result = new ReplaceVisitor(
            e => hasPropertyRoute(e, pr),
            e => newElements(e),
        ).visit(this.result);

        return this;
    }

    

    removeTab(tabId: string | number): this {
        this.result = new ReplaceVisitor(
            e => e.type == Tab && e.props.eventKey == tabId,
            e => [])
            .visit(this.result);

        return this;
    }

    insertTabAfter(tabId: string | number, ...newTabs: (Tab | undefined | false | null)[]): this {
        this.result = new ReplaceVisitor(
            e => e.type == Tab && e.props.eventKey == tabId,
            e => [e, ...newTabs])
            .visit(this.result);

        return this;
    }

    insertTabBefore(tabId: string | number, ...newTabs: (Tab | undefined | false | null)[]): this {
        this.result = new ReplaceVisitor(
            e => e.type == Tab && e.props.eventKey == tabId,
            e => [...newTabs, e])
            .visit(this.result);

        return this;
    }
}

export function cloneFindOptions(fo: FindOptions): FindOptions{

    function cloneFilter(f: FilterOption): FilterOption {
        if (isFilterGroupOption(f))
            return ({
                groupOperation: f.groupOperation,
                token: f.token,
                filters: f.filters.map(_ => cloneFilter(_))
            } as FilterGroupOption);
        else
            return ({
                token: f.token,
                operation: f.operation,
                value: f.value,
                frozen: f.frozen,
            } as FilterConditionOption)
    }

    const pa = fo.pagination;
    return {
        queryName: fo.queryName,
        groupResults: fo.groupResults,
        parentToken: fo.parentToken,
        parentValue: fo.parentValue,
        filterOptions: fo.filterOptions && fo.filterOptions.map(f => cloneFilter(f)),
        orderOptions: fo.orderOptions && fo.orderOptions.map(o => ({ token: o.token, orderType: o.orderType } as OrderOption)),
        columnOptions: fo.columnOptions && fo.columnOptions.map(c => ({ token: c.token, displayName: c.displayName } as ColumnOption)),
        columnOptionsMode: fo.columnOptionsMode,
        pagination: pa && { mode: pa.mode, elementsPerPage: pa.elementsPerPage, currentPage: pa.currentPage, } as Pagination,
    };
}

export function hasPropertyRoute(e: React.ReactElement<any>, pr: PropertyRoute) {
    const tc = e.props.ctx as TypeContext<any>;

    if (!tc)
        return false;

    return tc.propertyRoute && tc.propertyRoute.toString() == pr.toString();
}


