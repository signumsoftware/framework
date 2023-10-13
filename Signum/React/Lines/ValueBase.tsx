import * as React from 'react';
import { Dic } from '../Globals';
import { LineBaseController, LineBaseProps, setRefProp, useInitiallyFocused } from '../Lines/LineBase';
import { getTimeMachineIcon } from './TimeMachineIcon';

export interface ValueBaseProps<C> extends LineBaseProps {
  format?: string;
  unit?: string;
  valueHtmlAttributes?: React.AllHTMLAttributes<any>;
  extraButtons?: (vl: C) => React.ReactNode;
  extraButtonsBefore?: (vl: C) => React.ReactNode;
  initiallyFocused?: boolean | number;
  valueRef?: React.Ref<HTMLElement>;
}

export class ValueBaseController<T extends ValueBaseProps<any>> extends LineBaseController<T> {

  inputElement!: React.RefObject<HTMLElement>;
  init(p: T) {
      super.init(p);

      this.inputElement = React.useRef<HTMLElement>(null);
      useInitiallyFocused(this.props.initiallyFocused, this.inputElement);
  }

  setRefs = (node: HTMLElement | null) => {
      setRefProp(this.props.valueRef, node);
      (this.inputElement as React.MutableRefObject<HTMLElement | null>).current = node;
  };

  overrideProps(state: T, overridenProps: T) {

      const valueHtmlAttributes = { ...state.valueHtmlAttributes, ...Dic.simplify(overridenProps.valueHtmlAttributes) };
      super.overrideProps(state, overridenProps);
      state.valueHtmlAttributes = valueHtmlAttributes;
  }


  withItemGroup(input: JSX.Element, preExtraButton?: JSX.Element): JSX.Element {

      if (!this.props.extraButtons && !preExtraButton) {
          return <>
              {getTimeMachineIcon({ ctx: this.props.ctx })}
              {input}
          </>;
      }

      return (
          <div className={this.props.ctx.inputGroupClass}>
              {getTimeMachineIcon({ ctx: this.props.ctx })}
              {this.props.extraButtonsBefore && this.props.extraButtonsBefore(this)}
              {input}
              {preExtraButton}
              {this.props.extraButtons && this.props.extraButtons(this)}
          </div>
      );
  }

  getPlaceholder(): string | undefined {
      const p = this.props;
      return p.valueHtmlAttributes?.placeholder ??
          ((p.ctx.placeholderLabels || p.ctx.formGroupStyle == "FloatingLabel") ? asString(p.label) :
              undefined);
  }
  static autoFixString(str: string | null | undefined, autoTrim: boolean, autoNull: boolean): string | null | undefined {

    if (autoTrim)
      str = str?.trim();

    return str == "" && autoNull ? null : str;
  }

}

export function asString(reactChild: React.ReactNode | undefined): string | undefined {
  if (typeof reactChild == "string")
    return reactChild as string;

  return undefined;
}
