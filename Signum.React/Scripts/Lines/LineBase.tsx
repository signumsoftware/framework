
import * as React from 'react'
import * as moment from 'moment'
import { classes, Dic, addClass } from '../Globals'
import { TypeContext, StyleContext, StyleOptions, FormGroupStyle } from '../TypeContext'
import { PropertyRouteType, MemberInfo, getTypeInfo, TypeInfo, TypeReference } from '../Reflection'
import { ValidationMessage } from '../Signum.Entities'


export interface ChangeEvent {
    newValue: any;
    oldValue: any;
}

export interface LineBaseProps extends StyleOptions {
    ctx: TypeContext<any>;
    type?: TypeReference;
    labelText?: React.ReactChild;
    visible?: boolean;
    hideIfNull?: boolean;
    onChange?: (e: ChangeEvent) => void;
    onValidate?: (val: any) => string;
    labelHtmlAttributes?: React.LabelHTMLAttributes<HTMLLabelElement>;
    formGroupHtmlAttributes?: React.HTMLAttributes<any>;
    helpText?: React.ReactChild;
}

export abstract class LineBase<P extends LineBaseProps, S extends LineBaseProps> extends React.Component<P, S> {

    constructor(props: P) {
        super(props);

        this.state = this.calculateState(props);
    }

    shouldComponentUpdate(nextProps: LineBaseProps, nextState: LineBaseProps) {
        if (Dic.equals(this.state, nextState, true))
            return false; //For Debugging

        return true;
    }

    componentWillReceiveProps(nextProps: P, nextContext: any) {
        const newState = this.calculateState(nextProps);

        Dic.getKeys(this.state).forEach(k => {
            if (!(newState as any).hasOwnProperty(k))
                (newState as any)[k] = undefined;
        });

        this.setState(newState);
    }

    changes = 0;
    setValue(val: any) {
        var oldValue = this.state.ctx.value;
        this.state.ctx.value = val;
        this.changes++;
        this.validate();
        this.forceUpdate();
        if (this.state.onChange)
            this.state.onChange({ oldValue: oldValue, newValue: val });
    }

    validate() {
        const error = this.state.onValidate ? this.state.onValidate(this.state.ctx.value) : this.defaultValidate(this.state.ctx.value);
        this.state.ctx.error = error;
        if (this.state.ctx.frame)
            this.state.ctx.frame.revalidate();
    }

    defaultValidate(val: any) {
        if (this.state.type!.isNotNullable && val == undefined)
            return ValidationMessage._0IsNotSet.niceToString(this.state.labelText);

        return undefined;
    }

    render() {

        if (this.state.visible == false || this.state.hideIfNull && this.state.ctx.value == undefined)
            return null;

        return this.renderInternal();
    }

    calculateState(props: P): S {

        const { type, ctx,
            readonlyAsPlainText, formSize, formGroupStyle, labelColumns, placeholderLabels, readOnly, valueColumns,
            ...otherProps
        } = props as LineBaseProps;

        const so: StyleOptions = { readonlyAsPlainText, formSize, formGroupStyle, labelColumns, placeholderLabels, readOnly, valueColumns };

        const state = { ctx: ctx.subCtx(so), type: (type || ctx.propertyRoute.typeReference() ) } as LineBaseProps as S;

        this.calculateDefaultState(state);
        runTasks(this as any as LineBase<LineBaseProps, LineBaseProps>, state);

        this.overrideProps(state, otherProps as S);
        return state;
    }

    overrideProps(state: S, overridenProps: S) {
        const labelHtmlAttributes = { ...state.labelHtmlAttributes, ...Dic.simplify(overridenProps.labelHtmlAttributes) };
        Dic.assign(state, Dic.simplify(overridenProps))
        state.labelHtmlAttributes = labelHtmlAttributes;
    }

    baseHtmlAttributes(): React.HTMLAttributes<any> {
        return {
            'data-property-path': this.state.ctx.propertyPath,
            'data-changes': this.changes
        } as any;
    }

    calculateDefaultState(state: S) {
    }

    abstract renderInternal(): JSX.Element | null;
}


export const tasks: ((lineBase: LineBase<LineBaseProps, LineBaseProps>, state: LineBaseProps) => void)[] = [];

export function runTasks(lineBase: LineBase<LineBaseProps, LineBaseProps>, state: LineBaseProps) {
    tasks.forEach(t => t(lineBase, state));
}