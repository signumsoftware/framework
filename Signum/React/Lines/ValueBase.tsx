import * as React from 'react';
import { Dic } from '../Globals';
import { LineBaseController, LineBaseProps, setRefProp, tasks, useInitiallyFocused } from '../Lines/LineBase';
import { getTimeMachineIcon } from './TimeMachineIcon';

export interface ValueBaseProps<V = any> extends LineBaseProps<V> {
  format?: string;
  unit?: string;
  valueHtmlAttributes?: React.AllHTMLAttributes<any>;
  initiallyFocused?: boolean | number;
  valueRef?: React.Ref<HTMLElement>;
}

export class ValueBaseController<T extends ValueBaseProps<V>, V> extends LineBaseController<T, V> {

  inputElement!: React.RefObject<HTMLElement | null>;
  init(p: T): void {
    super.init(p);

    this.inputElement = React.useRef<HTMLElement>(null);
    useInitiallyFocused(this.props.initiallyFocused, this.inputElement);
  }

  setRefs = (node: HTMLElement | null): void => {
      setRefProp(this.props.valueRef, node);
      (this.inputElement as React.MutableRefObject<HTMLElement | null>).current = node;
  };

  assertType(tagName: string, types: string[]): void {
    if (!types.contains(this.props.type!.name))
      throw new Error(`Invalid type '${this.props.type?.name}'' in ${tagName} for ${this.props.ctx.propertyPath ?? this.props.ctx.prefix}`)
  }

  overrideProps(state: T, overridenProps: T): void {

      const valueHtmlAttributes = { ...state.valueHtmlAttributes, ...Dic.simplify(overridenProps.valueHtmlAttributes) };
      super.overrideProps(state, overridenProps);
      state.valueHtmlAttributes = valueHtmlAttributes;
  }

  withItemGroup(input: React.ReactElement, preExtraButton?: React.ReactElement, vertical?: boolean): React.ReactElement {

    if (!this.props.unit && !this.props.extraButtons && !this.props.extraButtonsBefore && !preExtraButton) {
        return (
          <>
            {getTimeMachineIcon({ ctx: this.props.ctx })}
            {input}
          </>
        );
    }

    if (vertical) {
      return (
        <div className="d-flex">
          {getTimeMachineIcon({ ctx: this.props.ctx })}
          {this.props.extraButtonsBefore && <div className={this.props.ctx.inputGroupVerticalClass("before")}>{this.props.extraButtonsBefore(this)}</div>}
          {input}
          {this.props.unit && <span className={this.props.ctx.readonlyAsPlainText ? undefined : "input-group-text"}>{this.props.unit}</span>}
          {preExtraButton}
          {this.props.extraButtons && <div className={this.props.ctx.inputGroupVerticalClass("after")}>{this.props.extraButtons(this)}</div>}
        </div>
      );
    } else {
      return (
        <div className={this.props.ctx.inputGroupClass}>
          {getTimeMachineIcon({ ctx: this.props.ctx })}
          {this.props.extraButtonsBefore && this.props.extraButtonsBefore(this)}
          {input}
          {this.props.unit && <span className={this.props.ctx.readonlyAsPlainText ? undefined : "input-group-text"}>{this.props.unit}</span>}
          {preExtraButton}
          {this.props.extraButtons && this.props.extraButtons(this)}
        </div>
      );
    }
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

tasks.push(taskSetFormat);
export function taskSetFormat(lineBase: LineBaseController<LineBaseProps, unknown>, state: LineBaseProps): void {
  if (lineBase instanceof ValueBaseController) {
    const vProps = state as ValueBaseProps<unknown>;

    if (!vProps.format &&
      state.ctx.propertyRoute &&
      state.ctx.propertyRoute.propertyRouteType == "Field") {
      vProps.format = state.ctx.propertyRoute.member!.format;
    }
  }
}

tasks.push(taskSetUnit);
export function taskSetUnit(lineBase: LineBaseController<LineBaseProps, unknown>, state: LineBaseProps): void {

  if (lineBase instanceof ValueBaseController) {
    const vProps = state as ValueBaseProps<any>;

    if (vProps.unit === undefined &&
      state.ctx.propertyRoute &&
      state.ctx.propertyRoute.propertyRouteType == "Field") {
      vProps.unit = state.ctx.propertyRoute.member!.unit;
    }
  }
}
