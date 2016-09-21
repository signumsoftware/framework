﻿import * as React from 'react'
import { Tab } from 'react-bootstrap'
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

        if (this.predicate(element))
            return this.replacement(element);

        return super.visitElement(element);
    }    
}

export class ViewReplacer<T> {

    constructor(
        public result: React.ReactElement<any>,
        public ctx: TypeContext<T>
    ) {
    }


    remove(pr: (entity: T) => any): this {
        this.result = new ReplaceVisitor(
            e => hasPropertyRoute(e, this.ctx.propertyRoute.add(pr)),
            e => []).visit(this.result);

        return this;
    }

    insertAfter(pr: (entity: T) => any, ...newElements: React.ReactElement<any>[]): this {
        this.result = new ReplaceVisitor(
            e => hasPropertyRoute(e, this.ctx.propertyRoute.add(pr)),
            e => [e, ...newElements]).visit(this.result);

        return this;
    }

    insertBefore(pr: (entity: T) => any, ...newElements: React.ReactElement<any>[]): this {
        this.result = new ReplaceVisitor(
            e => hasPropertyRoute(e, this.ctx.propertyRoute.add(pr)),
            e => [...newElements, e]).visit(this.result);

        return this;
    }


    removeTab(eventKey: string): this {
        this.result = new ReplaceVisitor(
            e => e.type == Tab && e.props.eventKey == eventKey,
            e => []).visit(this.result);

        return this;
    }

    insertTabAfter(eventKey: string, ...newTabs: Tab[]): this {
        this.result = new ReplaceVisitor(
            e => e.type == Tab && e.props.eventKey == eventKey,
            e => [e, newTabs]).visit(this.result);

        return this;
    }

    insertTabBefore(eventKey: string, ...newTabs: Tab[]): this {
        this.result = new ReplaceVisitor(
            e => e.type == Tab && e.props.eventKey == eventKey,
            e => [newTabs, e]).visit(this.result);

        return this;
    }
}

export function hasPropertyRoute(e: React.ReactElement<any>, pr: PropertyRoute) {
    const tc = e.props.ctx as TypeContext<any>;

    return tc && tc.propertyRoute && tc.propertyRoute.toString() == pr.toString();
}


