import * as React from 'react'
import { Dic } from '../Globals'
import { Binding, LambdaMemberType, getTypeInfos, EntityKind, KindOfType } from '../Reflection'
import { ModifiableEntity } from '../Signum.Entities'
import * as Navigator from '../Navigator'
import { ViewReplacer } from '../Frames/ReactVisitor'
import {  ValueLine, EntityLine, EntityCombo, EntityList, EntityDetail, EntityStrip, EntityRepeater, TypeContext, EntityCheckboxList } from '../Lines'

export default class DynamicComponent extends React.Component<{ ctx: TypeContext<ModifiableEntity> }, void> {

    render() {

        const subContexts = this.subContext(this.props.ctx).filter(m => m.propertyRoute.member!.name != "Id");

        const components = subContexts.map(ctx => DynamicComponent.appropiateComponent(ctx));

        const result = React.createElement("div", undefined, ...components);

        const es = Navigator.getSettings(this.props.ctx.value.Type);
        
        if (es && es.viewOverrides && es.viewOverrides.length) {
            const replacer = new ViewReplacer(result, this.props.ctx);
            es.viewOverrides.forEach(vo => vo(replacer));
            return replacer.result;
        } else {
            return result;
        }   
    }


    subContext(ctx: TypeContext<ModifiableEntity>): TypeContext<any>[] {

        const members = ctx.propertyRoute.subMembers();

        const result = Dic.map(members, (n, m) => new TypeContext<any>(ctx, undefined, ctx.propertyRoute.addMember({ name: n, type: LambdaMemberType.Member }), new Binding(ctx.value, n.firstLower())));

        return result;
    }

    static specificComponents: {
        [typeName: string]: (ctx: TypeContext<any>) => React.ReactElement<any> | undefined;
    } = {};

    static appropiateComponent = (ctx: TypeContext<any>): React.ReactElement<any> => {
        const tr = ctx.propertyRoute.typeReference();        
    
        const sc = DynamicComponent.specificComponents[tr.name];
        if (sc) {
            const result = sc(ctx);
            if (result)
                return result;
        }
        
        const tis = getTypeInfos(tr);
        const ti = tis.firstOrNull();

        if (tr.isCollection) {
            if (tr.isEmbedded || ti!.entityKind == EntityKind.Part || ti!.entityKind == EntityKind.SharedPart)
                return <EntityRepeater ctx={ctx}/>;
            else if (ti!.isLowPopulation)
                return <EntityCheckboxList ctx ={ctx}/>;
            else
                return <EntityStrip ctx={ctx}/>;
        }

        if (tr.name == "[ALL]")
            return <EntityLine ctx={ctx}/>;

        if (ti) {
            if (ti.kind == KindOfType.Enum)
                return <ValueLine ctx={ctx}/>;

            if (ti.entityKind == EntityKind.Part || ti.entityKind == EntityKind.SharedPart)
                return <EntityDetail ctx={ctx} />;

            if (ti.isLowPopulation)
                return <EntityCombo ctx={ctx}/>;           

            return <EntityLine ctx={ctx}/>;
        }

        if (tr.isEmbedded)
            return <EntityDetail ctx={ctx} />;

        return <ValueLine ctx={ctx}/>;
    }

}