import * as React from 'react'
import { Dic } from '@framework/Globals'
import { TypeEntity, PropertyRouteEntity } from '@framework/Signum.Entities.Basics'
import { ValueLine, EntityLine, RenderEntity, EntityCombo, EntityList, EntityDetail, EntityStrip, EntityRepeater, EntityCheckboxList, EntityTabRepeater, TypeContext, ValueLineType, FormGroup } from '@framework/Lines'
import { getTypeInfo, MemberInfo, PropertyRoute } from "@framework/Reflection";


export interface PropertyRouteComboProps {
    ctx: TypeContext<PropertyRouteEntity | undefined | null>;
    type: TypeEntity;
    filter?: (m: MemberInfo) => boolean;
    routes?: PropertyRoute[]; 
    onChange?: () => void;
}

export default class PropertyRouteCombo extends React.Component<PropertyRouteComboProps> {

    static defaultProps: Partial<PropertyRouteComboProps> = {
        filter: a => a.name != "Id"
    };

    handleChange = (e: React.FormEvent<any>) => {
        var currentValue = (e.currentTarget as HTMLSelectElement).value;
        this.props.ctx.value = currentValue ? PropertyRouteEntity.New({ path: currentValue, rootType: this.props.type }) : null;
        this.forceUpdate();
        if (this.props.onChange)
            this.props.onChange();
    }

    render() {
        var ctx = this.props.ctx;

        var routes = this.props.routes || Dic.getValues(getTypeInfo(this.props.type.cleanName).members).filter(this.props.filter!).map(mi => PropertyRoute.parse(this.props.type.cleanName, mi.name)) ;

        return (
            <select className={ctx.formControlClass} value={ctx.value && ctx.value.path || ""} onChange={this.handleChange} >
                <option value=""> - </option>
                {routes.map(r => r.propertyPath()).map(path =>
                    <option key={path} value={path}>{path}</option>
                )}
            </select>
        );;
    }
}
