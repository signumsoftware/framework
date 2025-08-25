import * as React from 'react'
import { ModifiableEntity } from "../Signum.Entities";
import { TypeContext } from "../Lines";
import { ViewReplacer } from "./ReactVisitor";
import { ViewPromise } from '../Navigator';

export function ViewOverrider<T extends ModifiableEntity>(p: { ctx: TypeContext<T>, viewOverride?: (replacer: ViewReplacer<T>) => void, children: React.ReactNode }): React.ReactElement {

  var child = React.Children.only(p.children);

  if (!React.isValidElement(child))
    throw new Error("The child should be a react element");

  if (!p.viewOverride)
    return child as React.ReactElement;

  var component = child.type as React.ComponentClass<{ ctx: TypeContext<T> }> | React.FunctionComponent<{ ctx: TypeContext<T> }>;
  if (component.prototype.render) {
    throw new Error("ViewOverrider only works on Functional components");
  }

  var newFunc = ViewPromise.surroundFunctionComponent(component as React.FunctionComponent<{ ctx: TypeContext<T> }>, [{ override: p.viewOverride! }])
  return <div>{React.createElement(newFunc, child.props as any)}</div>;
}
