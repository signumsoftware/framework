import * as React from 'react'
import { FindOptions, FilterOption, isFilterGroupOption, FilterGroupOption, FilterConditionOption } from '../FindOptions'
import { ModifiableEntity } from '../Signum.Entities'
import { TypeContext } from '../TypeContext'
import { PropertyRoute } from '../Reflection'
import { ColumnOption, OrderOption, Pagination } from '../Search';
import { Tab } from 'react-bootstrap'

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

    const newChildren = React.Children.map(oldChildren, c => this.visitChild(c as React.ReactChild));

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

  visitElement(element: React.ReactElement<any>): React.ReactNode {

    if (this.predicate(element)) {

      var node = this.replacement(element);

      var validatedNode = React.Children.map(node, c => new ReactValidator().visitChild(c as React.ReactChild));

      return validatedNode;
    }

    return super.visitElement(element);
  }
}

export class ReactValidator extends ReactVisitor {
  visitElement(element: React.ReactElement<any>): React.ReactNode {

    if (!React.isValidElement(element))
      return <div className="alert alert-danger">Invalid react element: {JSON.stringify(element)}</div>;

    return super.visitElement(element);
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

    var pr = this.ctx.propertyRoute!.addLambda(propertyRoute);

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

  replaceAttributes<P>(propertyRoute: (entity: T) => any, newAttrs: Partial<P>): this {

    var pr = this.ctx.propertyRoute!.addLambda(propertyRoute);

    this.result = new ReplaceVisitor(
      e => hasPropertyRoute(e, pr),
      e => React.cloneElement(e, { ...newAttrs })
    ).visit(this.result);

    return this;
  }

  insertAfterLine(propertyRoute: (entity: T) => any, newElements: (ctx: TypeContext<T>) => (React.ReactElement<any> | undefined | false | null)[]): this {

    var pr = this.ctx.propertyRoute!.addLambda(propertyRoute);

    this.result = new ReplaceVisitor(
      e => hasPropertyRoute(e, pr),
      e => [e, ...newElements(this.previousTypeContext(e))]
    ).visit(this.result);

    return this;
  }

  insertBeforeLine(propertyRoute: (entity: T) => any, newElements: (ctx: TypeContext<T>) => (React.ReactElement<any> | undefined)[]): this {

    var pr = this.ctx.propertyRoute!.addLambda(propertyRoute);

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
    var pr = this.ctx.propertyRoute!.addLambda(propertyRoute);

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

  insertTabAfter(eventKey: string | number, ...newTabs: (React.ReactElement<any> | undefined | false | null)[]): this {
    this.result = new ReplaceVisitor(
      e => e.type == Tab && e.props.eventKey == eventKey,
      e => [e, ...newTabs])
      .visit(this.result);

    return this;
  }

  insertTabBefore(eventKey: string | number, ...newTabs: (React.ReactElement<any> | undefined | false | null)[]): this {
    this.result = new ReplaceVisitor(
      e => e.type == Tab && e.props.eventKey == eventKey,
      e => [...newTabs, e])
      .visit(this.result);

    return this;
  }
}

export function cloneFindOptions(fo: FindOptions): FindOptions {

  function cloneFilter(f: FilterOption): FilterOption {
    if (isFilterGroupOption(f))
      return ({
        groupOperation: f.groupOperation,
        token: f.token,
        filters: f.filters.map(f => f && cloneFilter(f)),
        pinned: f.pinned && { ...f.pinned }
      } as FilterGroupOption);
    else
      return ({
        token: f.token,
        operation: f.operation,
        value: f.value,
        frozen: f.frozen,
        pinned: f.pinned && { ...f.pinned }
      } as FilterConditionOption)
  }

  const pa = fo.pagination;
  return {
    queryName: fo.queryName,
    groupResults: fo.groupResults,
    filterOptions: fo.filterOptions && fo.filterOptions.map(f => f && cloneFilter(f)),
    orderOptions: fo.orderOptions && fo.orderOptions.map(o => o && ({ ...o })),
    columnOptions: fo.columnOptions && fo.columnOptions.map(c => c && ({ ...c })),
    columnOptionsMode: fo.columnOptionsMode,
    pagination: pa && { ...pa } as Pagination,
  };
}

export function hasPropertyRoute(e: React.ReactElement<any>, pr: PropertyRoute): boolean {
  const tc = e.props.ctx as TypeContext<any>;

  if (!tc)
    return false;

  return tc.propertyRoute != null && tc.propertyRoute.toString() == pr.toString();
}


