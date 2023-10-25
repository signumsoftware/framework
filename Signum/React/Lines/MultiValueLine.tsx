import * as React from "react";
import { TypeContext, FormGroup, AutoLine, AutoLineProps } from "../Lines";
import { SearchMessage, MList, newMListElement } from "../Signum.Entities";
import { mlistItemContext } from "../TypeContext";
import { FontAwesomeIcon } from "@fortawesome/react-fontawesome";
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
  valueColumClass?: string;
  filterRows?: (ctxs: TypeContext<any /*T*/>[]) => TypeContext<any /*T*/>[];
}

export class MultiValueLineController extends LineBaseController<MultiValueLineProps> {

  keyGenerator = new KeyGenerator();

  getDefaultProps(p: MultiValueLineProps) {
    if (p.ctx.value == undefined)
      p.ctx.value = [];

    p.valueColumClass = "col-sm-12";

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
    });
  }

  defaultCreate() {
    return Promise.resolve(null);
  }

  getMListItemContext<T>(ctx: TypeContext<MList<T>>): TypeContext<T>[] {
    var rows = mlistItemContext(ctx);

    if (this.props.filterRows)
      return this.props.filterRows(rows);

    return rows;
  }
}

export const MultiValueLine = React.forwardRef(function MultiValueLine(props: MultiValueLineProps, ref: React.Ref<MultiValueLineController>) {
  const c = useController(MultiValueLineController, props, ref);
  const p = c.props;
  const list = p.ctx.value;

  if (c.isHidden)
    return null;

  return (
    <FormGroup ctx={p.ctx} label={p.label} labelIcon={p.labelIcon}
      htmlAttributes={{ ...c.baseHtmlAttributes(), ...p.formGroupHtmlAttributes }}
      helpText={p.helpText}
      labelHtmlAttributes={p.labelHtmlAttributes}>
      {inputId => <>
        <div className="row">
          {
          c.getMListItemContext(p.ctx.subCtx({ formGroupStyle: "None" })).map((mlec, i) => {
              return (

                <ErrorBoundary key={c.keyGenerator.getKey((mlec.binding as MListElementBinding<any>).getMListElement())}>
                  <div className={p.valueColumClass!} >
                    <MultiValueLineElement
                      ctx={mlec}
                      onRemove={e => { e.preventDefault(); c.handleDeleteValue(i); }}
                      onRenderItem={p.onRenderItem}
                      valueColumClass={p.valueColumClass!} />
                  </div>
                </ErrorBoundary>

              );
            })
          }
        </div>
        {!p.ctx.readOnly &&
          <a href="#" title={p.ctx.titleLabels ? p.addValueText ?? SearchMessage.AddValue.niceToString() : undefined}
            className="sf-line-button sf-create"
            onClick={c.handleAddValue}>
            {EntityBaseController.getCreateIcon()}&nbsp;{p.addValueText ?? SearchMessage.AddValue.niceToString()}
          </a>}

      </>}
    </FormGroup>
  );
});

export interface MultiValueLineElementProps {
  ctx: TypeContext<any>;
  onRemove: (event: React.MouseEvent<any>) => void;
  onRenderItem?: (ctx: TypeContext<any>) => React.ReactElement<any>;
  valueColumClass: string;
}

export function MultiValueLineElement(props: MultiValueLineElementProps) {
  const mctx = props.ctx;

  var renderItem = props.onRenderItem ?? AutoLine.getComponentFactory(mctx.propertyRoute!.typeReference, mctx.propertyRoute!)
  return (
    <div style={{ display: "flex", alignItems: "center", marginBottom: "2px" }}>
      {!mctx.readOnly &&
        <a href="#" title={mctx.titleLabels ? SearchMessage.DeleteFilter.niceToString() : undefined}
          className="sf-line-button sf-remove"
          onClick={props.onRemove}>
          <FontAwesomeIcon icon="xmark" />
        </a>
      }
      {React.cloneElement(renderItem({ ctx: mctx, mandatory: true} as any)!)}
    </div>
  );
}



