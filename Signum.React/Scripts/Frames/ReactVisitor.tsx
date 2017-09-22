import * as React from 'react'
import { Tab } from '../Tabs'
import { Dic } from '../Globals'
import * as Navigator from '../Navigator'
import { ResultTable, FindOptions, FilterOption, QueryDescription } from '../FindOptions'
import { Entity, Lite, is, toLite, LiteMessage, getToString, EntityPack, ModelState, ModifiableEntity } from '../Signum.Entities'
import { TypeContext, StyleOptions, EntityFrame } from '../TypeContext'
import { getTypeInfo, TypeInfo, PropertyRoute, ReadonlyBinding, getTypeInfos } from '../Reflection'


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

export class ViewReplacer<T> {

    constructor(
        public result: React.ReactElement<any>,
        public ctx: TypeContext<T>
    ) {
    }


    remove(propertyRoute: (entity: T) => any): this {

        var pr = this.ctx.propertyRoute.add(propertyRoute);

        this.result = new ReplaceVisitor(
            e => hasPropertyRoute(e, pr),
            e => [])
            .visit(this.result);

        return this;
    }

    insertAfter(propertyRoute: (entity: T) => any, ...newElements: (React.ReactElement<any> | undefined)[]): this {

        var pr = this.ctx.propertyRoute.add(propertyRoute);

        this.result = new ReplaceVisitor(
            e => hasPropertyRoute(e, pr),
            e => [e, ...newElements])
            .visit(this.result);

        return this;
    }

    insertBefore(propertyRoute: (entity: T) => any, ...newElements: React.ReactElement<any>[]): this {

        var pr = this.ctx.propertyRoute.add(propertyRoute);

        this.result = new ReplaceVisitor(
            e => hasPropertyRoute(e, pr),
            e => [...newElements, e])
            .visit(this.result);

        return this;
    }


    removeTab(tabId: string | number): this {
        this.result = new ReplaceVisitor(
            e => e.type == Tab && e.props.eventKey == tabId,
            e => [])
            .visit(this.result);

        return this;
    }

    insertTabAfter(tabId: string | number, ...newTabs: Tab[]): this {
        this.result = new ReplaceVisitor(
            e => e.type == Tab && e.props.eventKey == tabId,
            e => [e, ...newTabs])
            .visit(this.result);

        return this;
    }

    insertTabBefore(tabId: string | number, ...newTabs: Tab[]): this {
        this.result = new ReplaceVisitor(
            e => e.type == Tab && e.props.eventKey == tabId,
            e => [...newTabs, e])
            .visit(this.result);

        return this;
    }
}

export function hasPropertyRoute(e: React.ReactElement<any>, pr: PropertyRoute) {
    const tc = e.props.ctx as TypeContext<any>;

    return tc && tc.propertyRoute && tc.propertyRoute.toString() == pr.toString();
}


