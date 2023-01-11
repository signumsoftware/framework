import * as React from 'react'
import { Dic } from '../Globals'
import { TypeContext, StyleOptions } from '../TypeContext'
import { TypeReference } from '../Reflection'
import { ValidationMessage } from '../Signum.Entities'
import { useForceUpdate } from '../Hooks'

export interface ChangeEvent {
  newValue: any;
  oldValue: any;
  originalEvent?: React.SyntheticEvent; 
}

export interface LineBaseProps extends StyleOptions {
  ctx: TypeContext<any>;
  type?: TypeReference;
  label?: React.ReactNode;
  visible?: boolean;
  hideIfNull?: boolean;
  onChange?: (e: ChangeEvent) => void;
  onValidate?: (val: any) => string;
  labelHtmlAttributes?: React.LabelHTMLAttributes<HTMLLabelElement>;
  formGroupHtmlAttributes?: React.HTMLAttributes<any>;
  helpText?: React.ReactNode | null;
  mandatory?: boolean | "warning";
}


export function useController<C extends LineBaseController<P>, P extends LineBaseProps>(controllerType: new () => C, props: P, ref: React.Ref<C>) : C {
  var controller = React.useMemo<C>(()=> new controllerType(), []);
  controller.init(props);
  React.useImperativeHandle(ref, () => controller, []);
  return controller;
}

export class LineBaseController<P extends LineBaseProps> {

  static propEquals(prevProps: LineBaseProps, nextProps: LineBaseProps) {
    if (Dic.equals(prevProps, nextProps, true))
      return true; //For Debugging

    return false;
  }

  props!: P;
  forceUpdate!: () => void;
  changes!: number;
  setChanges!: (changes: React.SetStateAction<number>) => void;

  init(p: P) {
    this.props = this.expandProps(p);
    this.forceUpdate = useForceUpdate();
    [this.changes, this.setChanges] = React.useState(0);
  }

  setValue(val: any, event?: React.SyntheticEvent) {
    var oldValue = this.props.ctx.value;
    this.props.ctx.value = val;
    this.setChanges(c => c + 1);
    this.validate();
    this.forceUpdate();
    if (this.props.onChange)
      this.props.onChange({ oldValue: oldValue, newValue: val, originalEvent: event });
  }

  validate() {
    const error = this.props.onValidate ? this.props.onValidate(this.props.ctx.value) : this.defaultValidate(this.props.ctx.value);
    this.props.ctx.error = error;
    if (this.props.ctx.frame)
      this.props.ctx.frame.revalidate();
  }

  defaultValidate(val: any) {
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
    runTasks(this as any as LineBaseController<LineBaseProps>, p, props);

    return p;
  }

  overrideProps(p: P, overridenProps: P) {
    const labelHtmlAttributes = { ...p.labelHtmlAttributes, ...Dic.simplify(overridenProps.labelHtmlAttributes) };
    Dic.assign(p, Dic.simplify(overridenProps))
    p.labelHtmlAttributes = labelHtmlAttributes;
  }

  getDefaultProps(p: P) {
  }


  baseHtmlAttributes(): React.HTMLAttributes<any> {
    return {
      'data-property-path': this.props.ctx.propertyPath,
      'data-changes': this.changes
    } as any;
  }


  get mandatoryClass() {

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

  get isHidden() {
    return this.props.type == null || this.props.visible == false || this.props.hideIfNull && this.props.ctx.value == undefined;
  }
}

export const tasks: ((lineBase: LineBaseController<LineBaseProps>, state: LineBaseProps, originalProps: LineBaseProps) => void)[] = [];

export function runTasks(lineBase: LineBaseController<LineBaseProps>, state: LineBaseProps, originalProps: LineBaseProps) {
  tasks.forEach(t => t(lineBase, state, originalProps));
}

tasks.push(taskSetNiceName);
export function taskSetNiceName(lineBase: LineBaseController<any>, state: LineBaseProps) {
  if (!state.label &&
    state.ctx.propertyRoute &&
    state.ctx.propertyRoute.propertyRouteType == "Field") {
    state.label = state.ctx.propertyRoute.member!.niceName;
  }
}

tasks.push(taskSetReadOnlyProperty);
export function taskSetReadOnlyProperty(lineBase: LineBaseController<any>, state: LineBaseProps) {
  if (state.ctx.styleOptions.readOnly === undefined && !state.ctx.readOnly && 
    state.ctx.propertyRoute &&
    state.ctx.propertyRoute.propertyRouteType == "Field" &&
    state.ctx.propertyRoute.member!.isReadOnly) {
    state.ctx.readOnly = true;
  }
}

tasks.push(taskSetReadOnly);
export function taskSetReadOnly(lineBase: LineBaseController<any>, state: LineBaseProps) {
  if (state.ctx.styleOptions.readOnly === undefined && !state.ctx.readOnly &&
    state.ctx.binding.getIsReadonly()) {
    state.ctx.readOnly = true;
  }
}

tasks.push(taskSetMandatory);
export function taskSetMandatory(lineBase: LineBaseController<any>, state: LineBaseProps) {
  if (state.ctx.propertyRoute && state.mandatory == undefined &&
    state.ctx.propertyRoute.propertyRouteType == "Field" &&
    state.ctx.propertyRoute.member!.required) {
    state.mandatory = true;
  }
}
