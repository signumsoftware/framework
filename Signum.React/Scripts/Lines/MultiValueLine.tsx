import * as React from "react";
import { TypeContext, FormGroup } from "../Lines";
import { SearchMessage, MList, newMListElement } from "../Signum.Entities";
import { mlistItemContext } from "../TypeContext";
import { FontAwesomeIcon } from "@fortawesome/react-fontawesome";
import DynamicComponent, { getAppropiateComponent, getAppropiateComponentFactory } from "./DynamicComponent";
import { ErrorBoundary } from "../Components";
import { EntityBaseController } from "./EntityBase";
import { LineBaseProps, LineBaseController, useController } from "./LineBase";
import { KeyGenerator } from "../Globals";
import { MListElementBinding } from "../Reflection";

interface MultiValueLineProps extends LineBaseProps {
  ctx: TypeContext<MList<any>>;
  onRenderItem?: (ctx: TypeContext<any>) => React.ReactElement<any>;
  onCreate?: () => Promise<any[] | any | undefined>;
  addValueText?: string;
}

export class MultiValueLineController extends LineBaseController<MultiValueLineProps> {

  keyGenerator = new KeyGenerator();

  getDefaultProps(p: MultiValueLineProps) {
    if (p.ctx.value == undefined)
      p.ctx.value = [];

    super.getDefaultProps(p);
  }

  handleDeleteValue = (index: number) => {
    const list = this.props.ctx.value;
    list.removeAt(index);
    this.setValue(list);
  }

  handleAddValue = (e: React.MouseEvent<any>) => {
    e.preventDefault();
    const list = this.props.ctx.value;
    const newValuePromise = this.props.onCreate == null ? this.defaultCreate() : this.props.onCreate();

    newValuePromise.then(v => {
      if (v === undefined)
        return;

      if (Array.isArray(v)) {
        list.push(...v.map(e => newMListElement(e)));
      }
      else {
        list.push(newMListElement(v));
      }

      this.setValue(list);
    }).done();
  }

  defaultCreate() {
    return Promise.resolve(null);
  }
}

export const MultiValueLine = React.forwardRef(function MultiValueLine(props: MultiValueLineProps, ref: React.Ref<MultiValueLineController>) {
  const c = useController(MultiValueLineController, props, ref);
  const p = c.props;
  const list = p.ctx.value;

  if (c.isHidden)
    return null;



  return (
    <FormGroup ctx={p.ctx} labelText={p.labelText}
      htmlAttributes={{ ...c.baseHtmlAttributes(), ...p.formGroupHtmlAttributes }}
      helpText={p.helpText}
      labelHtmlAttributes={p.labelHtmlAttributes}>
      <table className="sf-multi-value">
        <tbody>
          {
            mlistItemContext(p.ctx.subCtx({ formGroupStyle: "None" })).map((mlec, i) =>
              (<ErrorBoundary key={c.keyGenerator.getKey((mlec.binding as MListElementBinding<any>).getMListElement())}>
                <MultiValueLineElement
                  ctx={mlec}
                  onRemove={e => { e.preventDefault(); c.handleDeleteValue(i); }}
                  onRenderItem={p.onRenderItem} />
              </ErrorBoundary>))
          }
          <tr >
            <td colSpan={4}>
              {!p.ctx.readOnly &&
                <a href="#" title={p.ctx.titleLabels ? p.addValueText ?? SearchMessage.AddValue.niceToString() : undefined}
                  className="sf-line-button sf-create"
                  onClick={c.handleAddValue}>
                  {EntityBaseController.createIcon}&nbsp;{p.addValueText ?? SearchMessage.AddValue.niceToString()}
                </a>}
            </td>
          </tr>
        </tbody>
      </table>
    </FormGroup>
  );
});

export interface MultiValueLineElementProps {
  ctx: TypeContext<any>;
  onRemove: (event: React.MouseEvent<any>) => void;
  onRenderItem?: (ctx: TypeContext<any>) => React.ReactElement<any>;
}

export function MultiValueLineElement(props: MultiValueLineElementProps) {
  const ctx = props.ctx;

  var renderItem = props.onRenderItem ?? getAppropiateComponentFactory(ctx.propertyRoute!)

  return (
    <tr>
      <td className="px-2">
        {!ctx.readOnly &&
          <a href="#" title={ctx.titleLabels ? SearchMessage.DeleteFilter.niceToString() : undefined}
            className="sf-line-button sf-remove"
            onClick={props.onRemove}>
            <FontAwesomeIcon icon="times" />
          </a>}
      </td>
      <td className="w-100">
        {renderItem(ctx)}
      </td>
    </tr>
  );
}



