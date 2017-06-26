import * as React from 'react'
import { Dic } from '../Globals'
import { Binding, LambdaMemberType, getTypeInfos, EntityKind, KindOfType } from '../Reflection'
import { ModifiableEntity } from '../Signum.Entities'
import * as Navigator from '../Navigator'
import { ViewReplacer } from '../Frames/ReactVisitor'
import { ValueLine, EntityLine, EntityCombo, EntityList, EntityDetail, EntityStrip, EntityRepeater, TypeContext, EntityCheckboxList, EntityTable } from '../Lines'

export default class DynamicComponent extends React.Component<{ ctx: TypeContext<ModifiableEntity> }, void> {

    render() {

        const subContexts = this.subContext(this.props.ctx);

        const components = subContexts.map(ctx => DynamicComponent.getAppropiateComponent(ctx)).filter(a => !!a).map(a => a!);

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

        const result = Dic.map(members, (field, m) => ctx.subCtx(field));

        return result;
    }

    static specificComponents: {
        [typeName: string]: (ctx: TypeContext<any>) => React.ReactElement<any> | undefined;
    } = {};

    static getAppropiateComponent(ctx: TypeContext<any>): React.ReactElement<any> | undefined {
        const mi = ctx.propertyRoute.member!;

        if (mi.name == "Id" || mi.notVisible == true)
            return undefined;

        const tr = ctx.propertyRoute.typeReference();        

        const sc = DynamicComponent.specificComponents[tr.name];
        if (sc) {
            const result = sc(ctx);
            if (result)
                return result;
        }
        
        let tis = getTypeInfos(tr);

        if (tis.length == 1 && tis[0] == undefined)
            tis = []; 

        if (tr.isCollection) {
            if (tr.name == "[ALL]")
                return <EntityStrip ctx={ctx} />;

            if (tr.isEmbedded || tis.every(t => t.entityKind == "Part" || t.entityKind == "SharedPart"))
                return <EntityTable ctx={ctx}/>;
            else if (tis.every(t => t.isLowPopulation == true))
                return <EntityCheckboxList ctx ={ctx}/>;
            else
                return <EntityStrip ctx={ctx}/>;
        }

        if (tr.name == "[ALL]")
            return <EntityLine ctx={ctx}/>;

        if (tis.length) {
            if (tis.length == 1 && tis.first().kind == "Enum")
                return <ValueLine ctx={ctx}/>;

            if (tis.every(t => t.entityKind == "Part" || t.entityKind == "SharedPart"))
                return <EntityDetail ctx={ctx} />;

            if (tis.every(t => t.isLowPopulation == true))
                return <EntityCombo ctx={ctx}/>;           

            return <EntityLine ctx={ctx}/>;
        }

        if (tr.isEmbedded)
            return <EntityDetail ctx={ctx} />;

        if (ValueLine.getValueLineType(tr) != undefined)
            return <ValueLine ctx={ctx} />;

        return undefined;
    }

}