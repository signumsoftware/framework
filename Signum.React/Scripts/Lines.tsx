import * as React from 'react'
import { TypeContext, StyleContext, StyleOptions, FormGroupStyle, FormGroupSize } from './TypeContext'
import { PropertyRouteType, MemberInfo, getTypeInfo, TypeInfo, PropertyRoute } from './Reflection'
import { ModifiableEntity, EntityPack, ModelState } from './Signum.Entities'
import * as Navigator from './Navigator'
import { ViewReplacer } from  './Frames/ReactVisitor'

export { PropertyRoute };

import { FormGroup, FormGroupProps, FormControlStatic, FormControlStaticProps, LineBase, LineBaseProps, Tasks} from './Lines/LineBase'
export { FormGroup, FormGroupProps, FormControlStatic, FormControlStaticProps, LineBase, LineBaseProps, Tasks};

import { ValueLine, ValueLineType, ValueLineProps } from './Lines/ValueLine'
export { ValueLine, ValueLineType, ValueLineProps};

import { RenderEntity } from  './Lines/RenderEntity'
export { RenderEntity };

import { EntityBase } from  './Lines/EntityBase'
export { EntityBase };

import { EntityLine } from  './Lines/EntityLine'
export { EntityLine };

import { EntityCombo } from  './Lines/EntityCombo'
export { EntityCombo };

import { EntityDetail } from  './Lines/EntityDetail'
export { EntityDetail };

import { EntityListBase } from  './Lines/EntityListBase'
export { EntityListBase };

import { EntityList } from  './Lines/EntityList'
export { EntityList };

import { EntityRepeater } from  './Lines/EntityRepeater'
export { EntityRepeater };

import { EntityTabRepeater } from  './Lines/EntityTabRepeater'
export { EntityTabRepeater };

import { EntityStrip } from  './Lines/EntityStrip'
export { EntityStrip };

import { EntityCheckboxList } from  './Lines/EntityCheckBoxList'
export { EntityCheckboxList };

export { TypeContext, StyleContext, StyleOptions, FormGroupStyle, FormGroupSize }; 

export interface EntityFrame<T extends ModifiableEntity> {
    onReload: (pack: EntityPack<T>) => void;
    setError: (modelState: ModelState, initialPrefix?: string) => void;
    onClose: () => void;
}

export interface EntityComponentProps<T extends ModifiableEntity> {
    ctx: TypeContext<T>;
    frame?: EntityFrame<T>;
}

export abstract class EntityComponentWithState<T extends ModifiableEntity, S> extends React.Component<EntityComponentProps<T>, S>{

    get entity() {
        return this.props.ctx.value;
    }

    render() {
        var result = this.renderEntity();
        result = Navigator.applyViewOverrides(this.props.ctx, result);
        return result;
    }

    abstract renderEntity(): React.ReactElement<any>;
}

export abstract class EntityComponent<T extends ModifiableEntity> extends EntityComponentWithState<T, void> {

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

export let maxValueLineSize = 100; 

Tasks.push(taskSetHtmlProperties);
export function taskSetHtmlProperties(lineBase: LineBase<any, any>, state: LineBaseProps) {
    var vl = lineBase instanceof ValueLine ? lineBase as ValueLine : null;
    var pr = state.ctx.propertyRoute;
    var vlp = state as ValueLineProps;
    if (vl  && pr && pr.propertyRouteType == PropertyRouteType.Field) {
        if (pr.member.maxLength != null) {

            if (!vlp.valueHtmlProps)
                vlp.valueHtmlProps = {};

            vlp.valueHtmlProps.maxLength = pr.member.maxLength;

            vlp.valueHtmlProps.size = maxValueLineSize == null ? pr.member.maxLength : Math.min(maxValueLineSize, pr.member.maxLength);
        }

        if (pr.member.isMultiline)
            vlp.valueLineType = ValueLineType.TextArea;
    }
}