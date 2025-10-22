import * as React from "react";
import { TypeContext, FormGroup, AutoLine, AutoLineProps } from "../Lines";
import { SearchMessage, MList, newMListElement } from "../Signum.Entities";
import { mlistItemContext } from "../TypeContext";
import { FontAwesomeIcon } from "@fortawesome/react-fontawesome";
import { ErrorBoundary } from "../Components";
import { EntityBaseController } from "./EntityBase";
import { LineBaseProps, LineBaseController, useController, genericMemo } from "./LineBase";
import { KeyGenerator } from "../Globals";
import { MListElementBinding } from "../Reflection";

interface MultiValueLineProps<V> extends LineBaseProps<MList<V>> {
  onRenderItem?: (p: AutoLineProps) => React.ReactElement;
  onCreate?: () => Promise<any[] | any | undefined>;
  addValueText?: string;
  valueColumClass?: string;
  filterRows?: (ctxs: TypeContext<any /*T*/>[]) => TypeContext<any /*T*/>[];
  ref?: React.Ref<MultiValueLineController<V>>;
}

export class MultiValueLineController<V> extends LineBaseController<MultiValueLineProps<V>, MList<V>> {

  keyGenerator: KeyGenerator = new KeyGenerator();

  getDefaultProps(p: MultiValueLineProps<V>): void {
    if (p.ctx.value == undefined)
      p.ctx.value = [];

    p.valueColumClass = "col-sm-12";

    super.getDefaultProps(p);
  }

  handleDeleteValue = (index: number): void => {
    const list = this.props.ctx.value;
    list.removeAt(index);
    this.setValue(list);
  }

  handleAddValue = (e: React.MouseEvent<any>): void => {
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

  defaultCreate(): Promise<null> {
    return Promise.resolve(null);
  }

  getMListItemContext(ctx: TypeContext<MList<V>>): TypeContext<V>[] {
    var rows = mlistItemContext(ctx);

    if (this.props.filterRows)
      return this.props.filterRows(rows);

    return rows;
  }
}

export const MultiValueLine: <V>(props: MultiValueLineProps<V>) => React.ReactNode | null
  = genericMemo(function MultiValueLine<V>(props: MultiValueLineProps<V>) {

    const c = useController<MultiValueLineController<V>, MultiValueLineProps<V>, MList<V>>(MultiValueLineController<V>, props);
    const p = c.props;

    var renderItem = React.useMemo(() => {
      if (props.onRenderItem)
        return props.onRenderItem;

      var pr = c.props.ctx.propertyRoute?.addMember("Indexer", "", true);
      if (pr)
        return AutoLine.getComponentFactory(pr.typeReference(), pr);

      return null;
    }, [Boolean(p.onRenderItem), p.ctx.propertyPath]);

    if (c.isHidden)
      return null;


    const helpText = p.helpText && (typeof p.helpText == "function" ? p.helpText(c) : p.helpText);
    const helpTextOnTop = p.helpTextOnTop && (typeof p.helpTextOnTop == "function" ? p.helpTextOnTop(c) : p.helpTextOnTop);

    return (
      <FormGroup ctx={p.ctx} error={p.error} label={p.label} labelIcon={p.labelIcon}
        htmlAttributes={{ ...c.baseHtmlAttributes(), ...p.formGroupHtmlAttributes }}
        helpText={helpText}
        helpTextOnTop={helpTextOnTop}
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
                        onRenderItem={renderItem!}
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
  onRenderItem: (p: AutoLineProps) => React.ReactElement;
  valueColumClass: string;
}

export function MultiValueLineElement(props: MultiValueLineElementProps): React.ReactElement {
  const mctx = props.ctx;


  return (
    <div className="sf-multi-value-element">
      {!mctx.readOnly &&
        <a href="#"
          title={mctx.titleLabels ? SearchMessage.DeleteFilter.niceToString() : undefined}
          className="sf-line-button sf-remove"
          role="button"
          tabIndex={0}
          onClick={props.onRemove}>
          <FontAwesomeIcon aria-hidden={true} icon="xmark" />
        </a>
      }
      {React.cloneElement(props.onRenderItem({ ctx: mctx, mandatory: true })!)}
    </div>
  );
}



