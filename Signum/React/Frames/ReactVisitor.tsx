import * as React from 'react'
import { FindOptions, FilterOption, FilterGroupOption, FilterConditionOption, isFilterGroup } from '../FindOptions'
import { ModifiableEntity } from '../Signum.Entities'
import { TypeContext } from '../TypeContext'
import { PropertyRoute } from '../Reflection'
import { ColumnOption, OrderOption, Pagination } from '../Search';
import { Tab, Tabs } from 'react-bootstrap'

export class ReactVisitor {
  visitChild(child: React.ReactNode): React.ReactNode {
    if (child == undefined)
      return child;

    if (typeof child == "string")
      return child;

    if (typeof child == "number")
      return child;

    if (typeof child == "boolean")
      return child;

    return this.visitElement(child as React.ReactElement);
  }

  visitElement(element: React.ReactElement): React.ReactNode {

    if (element.props.children == undefined || element.props.children.count == 0)
      return element;

    const oldChildren = React.Children.toArray(element.props.children);

    const newChildren = React.Children.map(oldChildren, c => this.visitChild(c as React.ReactNode | string | number | boolean | undefined | null));

    if (newChildren.length != oldChildren.length || newChildren.some((n, i) => n !== oldChildren[i]))
      return React.cloneElement(element, undefined, newChildren);

    return element;
  }

  visit(element: React.ReactNode): React.ReactNode {
    if (element == null)
      return element;

    const result = this.visitChild(element);

    if (Array.isArray(result))
      return React.createElement("div", {}, ...result);

    return result as React.ReactElement;
  }
}


export class ReplaceVisitor extends ReactVisitor {

  validator: ReactValidator = new ReactValidator();

  constructor(
    public predicate: (e: React.ReactElement) => boolean,
    public replacement: (e: React.ReactElement) => React.ReactNode) {
    super();
  }

  visitElement(element: React.ReactElement): React.ReactNode {

    if (this.predicate(element)) {

      var node = this.replacement(element);

      var validatedNode = React.Children.map(node, c => this.validator.visitChild(c));

      return validatedNode;
    }

    return super.visitElement(element);
  }
}

export class ReactValidator extends ReactVisitor {
  visitElement(element: React.ReactElement): React.ReactNode {

    if (!React.isValidElement(element))
      return <div className="alert alert-danger">Invalid react element: {JSON.stringify(element)}</div>;

    return super.visitElement(element);
  }
}

export class ViewReplacer<T extends ModifiableEntity> {

  constructor(
    public result: React.ReactNode,
    public ctx: TypeContext<T>,
    public originalFunction: Function | null,
  ) {
  }

  removeElement(filter: (e: React.ReactElement) => boolean): this {

    this.result = new ReplaceVisitor(
      e => filter(e),
      e => []
    ).visit(this.result);

    return this;
  }

  insertAfterElement(filter: (e: React.ReactElement) => boolean, newElements: (e: React.ReactElement) => (React.ReactElement | undefined | false | null)[]): this {

    this.result = new ReplaceVisitor(
      e => filter(e),
      e => [e, ...newElements(e)]
    ).visit(this.result);

    return this;
  }

  insertBeforeElement(filter: (e: React.ReactElement) => boolean, newElements: (e: React.ReactElement) => (React.ReactElement | undefined | false | null)[]): this {

    this.result = new ReplaceVisitor(
      e => filter(e),
      e => [...newElements(e), e]
    ).visit(this.result);

    return this;
  }

  replaceElement(filter: (e: React.ReactElement) => boolean, newElements: (e: React.ReactElement) => (React.ReactElement | undefined | false | null)[]): this {

    this.result = new ReplaceVisitor(
      e => filter(e),
      e => [...newElements(e)]
    ).visit(this.result);

    return this;
  }

  removeLine(propertyRoute: ((entity: T) => any) | PropertyRoute): this {

    var pr = propertyRoute instanceof PropertyRoute ? propertyRoute : this.ctx.propertyRoute!.addLambda(propertyRoute);

    this.result = new ReplaceVisitor(
      e => hasPropertyRoute(e, pr),
      e => []
    ).visit(this.result);

    return this;
  }

  replaceFindOptions(filter: (findOptions: FindOptions, e: React.ReactElement) => boolean, modifier: (clone: FindOptions) => void): this {
    this.result = new ReplaceVisitor(
      e => e.props.findOptions && filter(e.props.findOptions, e),
      e => {
        var clone = cloneFindOptions(e.props.findOptions);
        modifier(clone);
        return React.cloneElement(e, { findOptions: clone });
      }
    ).visit(this.result);

    return this;
  }

  replaceAttributes<P>(propertyRoute: ((entity: T) => any) | PropertyRoute, newAttrs: Partial<P>): this {

    var pr = propertyRoute instanceof PropertyRoute ? propertyRoute : this.ctx.propertyRoute!.addLambda(propertyRoute);

    this.result = new ReplaceVisitor(
      e => hasPropertyRoute(e, pr),
      e => React.cloneElement(e, { ...newAttrs })
    ).visit(this.result);

    return this;
  }

  insertAfterLine(propertyRoute: ((entity: T) => any) | PropertyRoute, newElements: (ctx: TypeContext<T>) => (React.ReactElement | undefined | false | null)[]): this {

    var pr = propertyRoute instanceof PropertyRoute ? propertyRoute : this.ctx.propertyRoute!.addLambda(propertyRoute);

    this.result = new ReplaceVisitor(
      e => hasPropertyRoute(e, pr),
      e => [e, ...newElements(this.previousTypeContext(e))]
    ).visit(this.result);

    return this;
  }

  insertBeforeLine(propertyRoute: ((entity: T) => any) | PropertyRoute, newElements: (ctx: TypeContext<T>) => (React.ReactElement | undefined)[]): this {

    var pr = propertyRoute instanceof PropertyRoute ? propertyRoute : this.ctx.propertyRoute!.addLambda(propertyRoute);

    this.result = new ReplaceVisitor(
      e => hasPropertyRoute(e, pr),
      e => [...newElements(this.previousTypeContext(e)), e]
    ).visit(this.result);

    return this;
  }

  previousTypeContext(e: React.ReactElement) {
    var ctx = e.props.ctx as TypeContext<any>;

    var parentCtx = ctx.findParentCtx(this.ctx.value.Type);

    return parentCtx as TypeContext<T>;
  }

  replaceLine(propertyRoute: ((entity: T) => any) | PropertyRoute, newElements: (e: React.ReactElement) => (React.ReactElement | undefined)[]): this {
    var pr = propertyRoute instanceof PropertyRoute ? propertyRoute : this.ctx.propertyRoute!.addLambda(propertyRoute);

    this.result = new ReplaceVisitor(
      e => hasPropertyRoute(e, pr),
      e => newElements(e),
    ).visit(this.result);

    return this;
  }

  removeTab(eventKey: string): this {
    this.result = new ReplaceVisitor(
      e => e.type == Tab && e.props.eventKey == eventKey,
      e => [])
      .visit(this.result);

    return this;
  }

  addTab(tabsId: string, ...newTabs: (React.ReactElement | undefined | false | null)[]): this {
    this.result = new ReplaceVisitor(
      e => e.type == Tabs && e.props.id == tabsId,
      e => [React.cloneElement(e, { children: [...React.Children.toArray(e.props.children), ...newTabs] })])
      .visit(this.result);

    return this;
  }

  insertTabAfter(eventKey: string | number, ...newTabs: (React.ReactElement | undefined | false | null)[]): this {
    this.result = new ReplaceVisitor(
      e => e.type == Tab && e.props.eventKey == eventKey,
      e => [e, ...newTabs])
      .visit(this.result);

    return this;
  }

  insertTabBefore(eventKey: string | number, ...newTabs: (React.ReactElement | undefined | false | null)[]): this {
    this.result = new ReplaceVisitor(
      e => e.type == Tab && e.props.eventKey == eventKey,
      e => [...newTabs, e])
      .visit(this.result);

    return this;
  }
}

export function cloneFindOptions(fo: FindOptions): FindOptions {

  function cloneFilter(f: FilterOption): FilterOption {
    if (isFilterGroup(f))
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

export function hasPropertyRoute(e: React.ReactElement, pr: PropertyRoute): boolean {
  const tc = e.props.ctx as TypeContext<any>;

  if (!tc)
    return false;

  return tc.propertyRoute != null && tc.propertyRoute.toString() == pr.toString();
}


