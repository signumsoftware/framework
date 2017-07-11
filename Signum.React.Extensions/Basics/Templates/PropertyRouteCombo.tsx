import * as React from 'react'
import { Dic } from '../../../../Framework/Signum.React/Scripts/Globals'
import { TypeEntity, PropertyRouteEntity } from '../../../../Framework/Signum.React/Scripts/Signum.Entities.Basics'
import { ValueLine, EntityLine, RenderEntity, EntityCombo, EntityList, EntityDetail, EntityStrip, EntityRepeater, EntityCheckboxList, EntityTabRepeater, TypeContext, ValueLineType, FormGroup } from '../../../../Framework/Signum.React/Scripts/Lines'
import { getTypeInfo, MemberInfo } from "../../../../Framework/Signum.React/Scripts/Reflection";


export interface PropertyRouteComboProps {
    ctx: TypeContext<PropertyRouteEntity | undefined | null>;
    type: TypeEntity;
    filter?: (m: MemberInfo) => boolean;
    onChange?: () => void;
}

export default class PropertyRouteCombo extends React.Component<PropertyRouteComboProps> {

    static defaultProps: Partial<PropertyRouteComboProps> = {
        filter: a => a.name != "Id"
    };

    handleChange = (e: React.FormEvent<any>) => {
        var currentValue = (e.currentTarget as HTMLSelectElement).value;
        this.props.ctx.value = currentValue ? PropertyRouteEntity.New({ path : currentValue, rootType : this.props.type }) : null;
        this.forceUpdate();
        if (this.props.onChange)
            this.props.onChange();
    }

    render() {
        var ctx = this.props.ctx;

        var members = Dic.getValues(getTypeInfo(this.props.type.cleanName).members).filter(this.props.filter!);

        return (
            <select className="form-control" value={ctx.value && ctx.value.path || ""} onChange={this.handleChange} >
                <option value=""> - </option>
                {members.map(m =>
                    <option key={m.name} value={m.name}>{m.name}</option>
                )}
            </select>
        );;
    }
}
