import * as React from 'react'
import { Dic } from '../Globals'
import { Binding, LambdaMemberType, getTypeInfos, EntityKind, KindOfType } from '../Reflection'
import { ModifiableEntity } from '../Signum.Entities'
import {  ValueLine, EntityLine, EntityCombo, EntityList, EntityDetail, EntityStrip, EntityRepeater, TypeContext, EntityCheckboxList } from '../Lines'

export default class DynamicComponent extends React.Component<{ ctx: TypeContext<ModifiableEntity> }, void> {

    render() {

        var subContexts = this.subContext(this.props.ctx).filter(m => m.propertyRoute.member.name != "Id");

        var components = subContexts.map(ctx => this.appropiateComponent(ctx));

        return React.createElement("div", null, ...components);
    }


    subContext(ctx: TypeContext<ModifiableEntity>): TypeContext<any>[] {

        var members = ctx.propertyRoute.subMembers();

        var result = Dic.map(members, (n, m) => new TypeContext<any>(ctx, null, ctx.propertyRoute.addMember({ name: n, type: LambdaMemberType.Member }), new Binding(n.firstLower(), ctx.value)));

        return result;
    }

    appropiateComponent(ctx: TypeContext<any>): React.ReactElement<any> {
        var tr = ctx.propertyRoute.typeReference();

        var tis = getTypeInfos(tr);
        var ti = tis.firstOrNull();

        if (tr.isCollection) {
            if (tr.isEmbedded || ti.entityKind == EntityKind.Part || ti.entityKind == EntityKind.SharedPart)
                return <EntityRepeater ctx={ctx}/>;
            else if (ti.isLowPopupation)
                return <EntityCheckboxList ctx ={ctx}/>;
            else
                return <EntityStrip ctx={ctx}/>;
        }

        if (ti) {
            if (ti.kind == KindOfType.Enum)
                return <ValueLine ctx={ctx}/>;

            if (ti.entityKind == EntityKind.Part || ti.entityKind == EntityKind.SharedPart)
                return <EntityDetail ctx={ctx} />;

            if (ti.isLowPopupation)
                return <EntityCombo ctx={ctx}/>;           

            return <EntityLine ctx={ctx}/>;
        }

        if (tr.isEmbedded)
            return <EntityDetail ctx={ctx} />;

        return <ValueLine ctx={ctx}/>;
    }

}