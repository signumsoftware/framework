import * as React from 'react'
import { Dic } from '../../../../Framework/Signum.React/Scripts/Globals'
import { MemberInfo, getTypeInfo } from '../../../../Framework/Signum.React/Scripts/Reflection'
import { DynamicValidationEntity } from '../Signum.Entities.Dynamic'
import { ValueLine, EntityLine, RenderEntity, EntityCombo, EntityList, EntityDetail, EntityStrip, EntityRepeater, EntityCheckboxList, EntityTabRepeater, TypeContext, ValueLineType, FormGroup } from '../../../../Framework/Signum.React/Scripts/Lines'
import { TypeEntity, PropertyRouteEntity } from '../../../../Framework/Signum.React/Scripts/Signum.Entities.Basics'
import * as Navigator from '../../../../Framework/Signum.React/Scripts/Navigator'
import { API } from '../DynamicValidationClient'
import CSharpScriptCode from './CSharpScriptCode'


export default class DynamicValidation extends React.Component<{ ctx: TypeContext<DynamicValidationEntity> }, void> {

    render() {
        var ctx = this.props.ctx;

        return (
            <div>
                <EntityLine ctx={ctx.subCtx(d => d.entityType)} onChange={() => { ctx.value.propertyRoute = null; this.forceUpdate();  } }/>
                <FormGroup ctx={ctx.subCtx(d => d.propertyRoute)}>
                    {ctx.value.entityType && <PropertyRouteCombo ctx={ctx.subCtx(d => d.propertyRoute)} type={ctx.value.entityType} onChange={() => this.forceUpdate()} />}
                </FormGroup>
                <ValueLine ctx={ctx.subCtx(d => d.name)}/>
                <ValueLine ctx={ctx.subCtx(d => d.isGlobalyEnabled)} inlineCheckbox={true} />
                {ctx.value.propertyRoute &&
                    <div>
                        <pre>{"string PropertyValidate(" + (!ctx.value.entityType ? "Entity" : ctx.value.entityType.cleanName) + " e, PropertyInfo pi)\n{"}</pre>
                        <CSharpScriptCode ctx={ctx.subCtx(d => d.eval)}/>
                        <pre>{"}"}</pre>
                    </div>}
            </div>
        );
    }
}

interface PropertyRouteLineProps {
    ctx: TypeContext<PropertyRouteEntity | undefined | null>;
    type: TypeEntity;
    onChange?: () => void;
}

class PropertyRouteCombo extends React.Component<PropertyRouteLineProps, void> {

    handleChange = (e: React.FormEvent) => {
        var currentValue = (e.currentTarget as HTMLSelectElement).value;
        this.props.ctx.value = currentValue ? PropertyRouteEntity.New(e => { e.path = currentValue; e.rootType = this.props.type; }) : null;
        this.forceUpdate();
        if (this.props.onChange)
            this.props.onChange();
    }

    render() {
        var ctx = this.props.ctx;

        var members = Dic.getValues(getTypeInfo(this.props.type.cleanName).members);

        return (
            <select className="form-control" value={ctx.value && ctx.value.path || ""}  onChange={this.handleChange} >
                {members.map(m =>
                    <option value={m.name}>{m.name}</option>
                )}
            </select>
            );  
    }
}
