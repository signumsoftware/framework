import * as React from 'react'
import { classes, Dic } from '../Globals'
import { TypeContext, StyleOptions } from '../TypeContext'
import { TypeReference } from '../Reflection'
import { ValidationMessage } from '../Signum.Entities.Validation'
import { useForceUpdate } from '../Hooks'

export interface ChangeEvent {
  newValue: any;
  oldValue: any;
  originalEvent?: React.SyntheticEvent;
}

export interface LineBaseProps<V = unknown> extends StyleOptions {
  ctx: TypeContext<V>;
  unit?: string;
  format?: string;
  type?: TypeReference;
  label?: React.ReactNode;
  labelIcon?: React.ReactNode;
  visible?: boolean;
  hideIfNull?: boolean;
  onChange?: (e: ChangeEvent) => void;
  error?: string | null;
  resetValidationError?: (val: any) => string | undefined;
  extraButtons?: (c: LineBaseController<any, V>) => React.ReactNode;
  extraButtonsBefore?: (c: LineBaseController<any, V>) => React.ReactNode;
  labelHtmlAttributes?: React.LabelHTMLAttributes<HTMLLabelElement>;
  formGroupHtmlAttributes?: React.HTMLAttributes<any>;
  helpText?: React.ReactNode | null | ((c: LineBaseController<any, V>) => React.ReactNode | null);
  helpTextOnTop?: React.ReactNode | null | ((c: LineBaseController<any, V>) => React.ReactNode | null);
  mandatory?: boolean | "warning";
}

export function useController<C extends LineBaseController<P, V>, P extends LineBaseProps<V> & { ref?: React.Ref<C> }, V>(controllerType: new () => C, props: P): C {
  var controller = React.useMemo<C>(() => new controllerType(), []);
  controller.init(props);
  React.useImperativeHandle(props.ref, () => controller, []);
  return controller;
}

export class LineBaseController<P extends LineBaseProps<V>, V> {

  static propEquals<V>(prev: LineBaseProps<V>, next: LineBaseProps<V>): boolean {
    if (next.extraButtons || prev.extraButtons)
      return false;

    if (next.extraButtonsBefore || prev.extraButtonsBefore)
      return false;

    if (next.labelIcon || prev.labelIcon)
      return false;

    if (Dic.equals(prev, next, true))
      return true; //For Debugging

    return false;
  }

  props!: P;
  forceUpdate!: () => void;
  changes!: number;
  setChanges!: (changes: React.SetStateAction<number>) => void;

  init(p: P): void {
    this.props = this.expandProps(p);
    this.forceUpdate = useForceUpdate();
    [this.changes, this.setChanges] = React.useState(0);
  }

  setValue(val: V, event?: React.SyntheticEvent): void {
    var oldValue = this.props.ctx.value;
    this.props.ctx.value = val;
    this.setChanges(c => c + 1);
    this.validate();
    this.forceUpdate();
    if (this.props.onChange)
      this.props.onChange({ oldValue: oldValue, newValue: val, originalEvent: event });
  }

  getError(): string | null | undefined {
    return this.props.error == undefined ? this.props.ctx.error : this.props.error;
  }

  getErrorClass(extraClasses?: "border"): string | undefined {

    return this.getError() ? classes("has-error", extraClasses) : undefined;
  }

  errorAttributes(): React.HTMLAttributes<any> | undefined {

    const error = this.getError();

    if (!error)
      return undefined;

    return {
      title: error,
      "data-error-path": this.props.ctx.prefix
    } as any;
  }

  validate(): void {
    const error = this.props.resetValidationError ? this.props.resetValidationError(this.props.ctx.value) : this.defaultResetValidationError(this.props.ctx.value);
    this.props.ctx.error = error;
    if (this.props.ctx.frame)
      this.props.ctx.frame.revalidate();
  }

  defaultResetValidationError(val: V): string | undefined {
    if (this.props.type!.isNotNullable && val == undefined)
      return ValidationMessage._0IsNotSet.niceToString(this.props.ctx.niceName());

    return undefined;
  }

  expandProps(props: P): P {

    const { type, ctx,
      readonlyAsPlainText, formSize, formGroupStyle, labelColumns, placeholderLabels, readOnly, valueColumns,
      ...otherProps
    } = props as LineBaseProps;

    const so: StyleOptions = { readonlyAsPlainText, formSize, formGroupStyle, labelColumns, placeholderLabels, readOnly, valueColumns };

    const p = { ctx: ctx.subCtx(so), type: (type ?? ctx.propertyRoute?.typeReference()) } as LineBaseProps as P;

    this.getDefaultProps(p);
    this.overrideProps(p, otherProps as P);
    runTasks(this as any, p as any, props as any);

    return p;
  }

  overrideProps(p: P, overridenProps: P): void {
    const labelHtmlAttributes = { ...p.labelHtmlAttributes, ...Dic.simplify(overridenProps.labelHtmlAttributes) };
    Dic.assign(p, Dic.simplify(overridenProps))
    p.labelHtmlAttributes = labelHtmlAttributes;
  }

  getDefaultProps(p: P): void {
  }


  baseHtmlAttributes(): React.HTMLAttributes<any> {
    return {
      'data-property-path': this.props.ctx.propertyPath,
      'data-changes': this.changes,
    } as any;
  }

  baseAriaAttributes(): React.AriaAttributes {
    const p = this.props;

    const ids: string[] = [];
    if (p.helpText) ids.push(this.props.ctx.getUniqueId("help"));
    if (this.getError()) ids.push(this.props.ctx.getUniqueId("error"));

    return {
      "aria-readonly": p.ctx.readOnly || undefined,
      "aria-describedby": ids.length ? ids.join(" ") : undefined
    };
  }

  extendedAriaAttributes(): React.AriaAttributes {
    return {
      ...this.baseAriaAttributes(),
      "aria-required": this.mandatoryClass ? true : this.props.mandatory ? true : false,
      "aria-invalid": !!this.getError() || undefined
    };
  }


  get mandatoryClass(): "sf-mandatory-warning" | "sf-mandatory" | null {

    if (this.props.mandatory && !this.props.readOnly) {
      const val = this.props.ctx.value;
      if (val == null || val === "" || Array.isArray(val) && val.length == 0) {
        if (this.props.mandatory == "warning")
          return "sf-mandatory-warning";
        else
          return "sf-mandatory";
      }
    }

    return null;
  }

  get isHidden(): boolean | undefined {
    return this.props.type == null || this.props.visible == false || this.props.ctx.binding.getIsHidden() || this.props.hideIfNull && (this.props.ctx.value == undefined || this.props.ctx.value == "");
  }
}

export function setRefProp(propRef: React.Ref<HTMLElement> | undefined, node: HTMLElement | null): void {
  if (propRef) {
    if (typeof propRef == "function")
      propRef(node);
    else
      (propRef as React.MutableRefObject<HTMLElement | null>).current = node;
  }
}

export function useInitiallyFocused(initiallyFocused: boolean | number | undefined, inputElement: React.RefObject<HTMLElement | null>): void {
  React.useEffect(() => {
    if (initiallyFocused) {
      window.setTimeout(() => {
        let element = inputElement?.current;
        if (element) {
          if (element instanceof HTMLInputElement)
            element.setSelectionRange(0, element.value.length);
          else if (element instanceof HTMLTextAreaElement)
            element.setSelectionRange(0, element.value.length);
          element.focus();
        }
      }, initiallyFocused == true ? 0 : initiallyFocused as number);
    }

  }, []);
}

export function genericMemo<T, P = {}>(render: (props: P) => React.ReactNode | null, propsAreEqual?: (prevProps: P, nextProps: P) => boolean): (props: P) => React.ReactNode | null {
  return React.memo(render, propsAreEqual) as any;
}

export const tasks: ((lineBase: LineBaseController<LineBaseProps, unknown>, state: LineBaseProps, originalProps: LineBaseProps) => void)[] = [];

export function runTasks(lineBase: LineBaseController<LineBaseProps, unknown>, state: LineBaseProps, originalProps: LineBaseProps): void {
  tasks.forEach(t => t(lineBase, state, originalProps));
}

tasks.push(taskSetNiceName);
export function taskSetNiceName(lineBase: LineBaseController<LineBaseProps, unknown>, state: LineBaseProps): void {
  if (state.label === undefined &&
    state.ctx.propertyRoute &&
    state.ctx.propertyRoute.propertyRouteType == "Field") {
    state.label = state.ctx.propertyRoute.member!.niceName;
  }
}

tasks.push(taskSetReadOnlyProperty);
export function taskSetReadOnlyProperty(lineBase: LineBaseController<LineBaseProps, unknown>, state: LineBaseProps): void {
  if (state.ctx.styleOptions.readOnly === undefined && !state.ctx.readOnly &&
    state.ctx.propertyRoute &&
    state.ctx.propertyRoute.propertyRouteType == "Field" &&
    state.ctx.propertyRoute.member!.isReadOnly) {
    state.ctx.readOnly = true;
  }
}

tasks.push(taskSetReadOnly);
export function taskSetReadOnly(lineBase: LineBaseController<LineBaseProps, unknown>, state: LineBaseProps): void {
  if (state.ctx.styleOptions.readOnly === undefined && !state.ctx.readOnly &&
    state.ctx.binding.getIsReadonly()) {
    state.ctx.readOnly = true;
  }
}

tasks.push(taskSetMandatory);
export function taskSetMandatory(lineBase: LineBaseController<LineBaseProps, unknown>, state: LineBaseProps): void {
  if (state.ctx.propertyRoute && state.mandatory == undefined &&
    state.ctx.propertyRoute.propertyRouteType == "Field" &&
    state.ctx.propertyRoute.member!.required) {
    state.mandatory = true;
  }
}
