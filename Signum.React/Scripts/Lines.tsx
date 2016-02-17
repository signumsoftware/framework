import * as React from 'react'
import { TypeContext, StyleContext, StyleOptions, FormGroupStyle } from './TypeContext'
import { PropertyRouteType, MemberInfo, getTypeInfo, TypeInfo} from './Reflection'
import { ModifiableEntity, EntityPack, ModelState } from './Signum.Entities'
import * as Navigator from './Navigator'
import { ViewReplacer } from  './Frames/ReactVisitor'

import { FormGroup, FormGroupProps, FormControlStatic, FormControlStaticProps, LineBase, LineBaseProps, Tasks} from './Lines/LineBase'
export { FormGroup, FormGroupProps, FormControlStatic, FormControlStaticProps, LineBase, LineBaseProps, Tasks};

import { ValueLine, ValueLineType, ValueLineProps } from './Lines/ValueLine'
export { ValueLine, ValueLineType, ValueLineProps};

import { EntityBase, EntityBaseProps } from  './Lines/EntityBase'
export { EntityBase, EntityBaseProps };

import { EntityLine, EntityLineProps } from  './Lines/EntityLine'
export { EntityLine, EntityLineProps };

import { EntityCombo, EntityComboProps } from  './Lines/EntityCombo'
export { EntityCombo, EntityComboProps };

import { EntityListBase, EntityListBaseProps } from  './Lines/EntityListBase'
export { EntityListBase, EntityListBaseProps };


import { EntityList, EntityListProps } from  './Lines/EntityList'
export { EntityList, EntityListProps };

export interface EntityFrame<T extends ModifiableEntity> {
    onReload: (pack: EntityPack<T>) => void;
    setError: (modelState: ModelState) => void;
    onClose: () => void;
}


export interface EntityComponentProps<T extends ModifiableEntity> {
    ctx: TypeContext<T>;
    frame: EntityFrame<T>;
}

export abstract class EntityComponent<T extends ModifiableEntity> extends React.Component<EntityComponentProps<T>, {}>{

    get entity() {
        return this.props.ctx.value;
    }

    subCtx(styleOptions?: StyleOptions): TypeContext<T>
    subCtx<R>(property: (val: T) => R, styleOptions?: StyleOptions): TypeContext<R>
    subCtx(propertyOrStyleOptions: ((val: T) => any) | StyleOptions, styleOptions?: StyleOptions): TypeContext<any> {
        if (typeof propertyOrStyleOptions != "function")
            return this.props.ctx.subCtx(propertyOrStyleOptions as StyleOptions);
        else
            return this.props.ctx.subCtx(propertyOrStyleOptions as ((val: T) => any), styleOptions);
    }

    niceName(property: (val: T) => any) {
        return this.props.ctx.niceName(property);
    }

    render() {

        var result = this.renderEntity();

        var ctx = this.props.ctx;

        var es = Navigator.getSettings(ctx.value.Type);

        if (es && es.viewOverrides && es.viewOverrides.length) {
            var replacer = new ViewReplacer(result, ctx);
            es.viewOverrides.forEach(vo => vo(replacer));
            return replacer.result;
        }

        return result;
    }

    abstract renderEntity(): React.ReactElement<any>;
}

Tasks.push(taskSetNiceName);
export function taskSetNiceName(lineBase: LineBase<any, any>, state: LineBaseProps) {
    if (!state.labelText &&
        state.ctx.propertyRoute &&
        state.ctx.propertyRoute.propertyRouteType == PropertyRouteType.Field) {
        state.labelText = state.ctx.propertyRoute.member.niceName;
    }
}

Tasks.push(taskSetUnit);
export function taskSetUnit(lineBase: LineBase<any, any>, state: LineBaseProps) {
    if (lineBase instanceof ValueLine) {
        const vProps = state as ValueLineProps;

        if (!vProps.unitText &&
            state.ctx.propertyRoute &&
            state.ctx.propertyRoute.propertyRouteType == PropertyRouteType.Field) {
            vProps.unitText = state.ctx.propertyRoute.member.unit;
        }
    }
}

Tasks.push(taskSetFormat);
export function taskSetFormat(lineBase: LineBase<any, any>, state: LineBaseProps) {
    if (lineBase instanceof ValueLine) {
        const vProps = state as ValueLineProps;

        if (!vProps.formatText &&
            state.ctx.propertyRoute &&
            state.ctx.propertyRoute.propertyRouteType == PropertyRouteType.Field) {
            vProps.formatText = state.ctx.propertyRoute.member.format;
        }
    }
}

Tasks.push(taskSetReadOnly);
export function taskSetReadOnly(lineBase: LineBase<any, any>, state: LineBaseProps) {
    if (!state.ctx.readOnly &&
        state.ctx.propertyRoute &&
        state.ctx.propertyRoute.propertyRouteType == PropertyRouteType.Field &&
        state.ctx.propertyRoute.member.isReadOnly) {
        state.ctx.readOnly = true;
    }
}