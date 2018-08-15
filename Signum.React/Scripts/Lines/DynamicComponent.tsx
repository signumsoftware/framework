import * as React from 'react'
import { Dic } from '../Globals'
import { getTypeInfos } from '../Reflection'
import { ModifiableEntity } from '../Signum.Entities'
import * as Navigator from '../Navigator'
import { ViewReplacer } from '../Frames/ReactVisitor'
import { ValueLine, EntityLine, EntityCombo, EntityDetail, EntityStrip, TypeContext, EntityCheckboxList, EnumCheckboxList, EntityTable } from '../Lines'
import { Type } from '../Reflection';

export default class DynamicComponent extends React.Component<{ ctx: TypeContext<ModifiableEntity>, viewName?: string }> {

    render() {

        const subContexts = this.subContext(this.props.ctx);

        const components = subContexts.map(ctx => DynamicComponent.getAppropiateComponent(ctx)).filter(a => !!a).map(a => a!);

        const result = React.createElement("div", undefined, ...components);

        const es = Navigator.getSettings(this.props.ctx.value.Type);

        var vos = es && es.viewOverrides && es.viewOverrides.filter(a => a.viewName == this.props.viewName); //Should user viewDispatcher.getViewOverrides promise instead

        if (vos && vos.length) {
            const replacer = new ViewReplacer(result, this.props.ctx);
            vos.forEach(vo => vo.override(replacer));
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

    static customTypeComponent: {
        [typeName: string]: (ctx: TypeContext<any>) => React.ReactElement<any> | null | undefined | "continue";
    } = {};

    static customPropertyComponent: {
        [propertyRoute: string]: (ctx: TypeContext<any>) => React.ReactElement<any> | null | undefined;
    } = {};

    static registerCustomPropertyComponent<T extends ModifiableEntity, V>(type: Type<T>, property: (e: T) => V, component: (ctx: TypeContext<any>) => React.ReactElement<any> | undefined)
    {
        DynamicComponent.customPropertyComponent[type.propertyRoute(property).toString()] = component;
    }

    static getAppropiateComponent(ctx: TypeContext<any>): React.ReactElement<any> | undefined {
        const mi = ctx.propertyRoute.member!;

        if (mi.name == "Id" || mi.notVisible == true)
            return undefined;

        const ccProp = DynamicComponent.customPropertyComponent[ctx.propertyRoute.toString()];
        if (ccProp) {
            return ccProp(ctx) || undefined;
        }

        const tr = ctx.propertyRoute.typeReference();        
        const ccType = DynamicComponent.customTypeComponent[tr.name];
        if (ccType) {
            var result = ccType(ctx);
            if (result != "continue")
                return result || undefined;
        }
        
        let tis = getTypeInfos(tr);
        if (tis.length == 1 && tis[0] == undefined)
            tis = []; 

        if (tr.isCollection) {
            if (tr.name == "[ALL]")
                return <EntityStrip ctx={ctx} />;

            if (tis.length) {
                if (tis.length == 1 && tis.first().kind == "Enum")
                    return <EnumCheckboxList ctx={ctx} />;

                if (tis.every(t => t.entityKind == "Part" || t.entityKind == "SharedPart"))
                    return <EntityTable ctx={ctx} />;

                if (tis.every(t => t.isLowPopulation == true))
                    return <EntityCheckboxList ctx={ctx} />;

                return <EntityStrip ctx={ctx} />;
            }

            if (tr.isEmbedded)
                return <EntityTable ctx={ctx} />;

            return undefined; 

        } else {

            if (tr.name == "[ALL]")
                return <EntityLine ctx={ctx} />;

            if (tis.length) {
                if (tis.length == 1 && tis.first().kind == "Enum")
                    return <ValueLine ctx={ctx} />;

                if (tis.every(t => t.entityKind == "Part" || t.entityKind == "SharedPart"))
                    return <EntityDetail ctx={ctx} />;

                if (tis.every(t => t.isLowPopulation == true))
                    return <EntityCombo ctx={ctx} />;

                return <EntityLine ctx={ctx} />;
            }

            if (tr.isEmbedded)
                return <EntityDetail ctx={ctx} />;

            if (ValueLine.getValueLineType(tr) == "Checkbox")
                return <ValueLine ctx={ctx} inlineCheckbox="block" />;

            if (ValueLine.getValueLineType(tr) != undefined)
                return <ValueLine ctx={ctx} />;

            return undefined;
        }
    }
}


(DynamicComponent.prototype.render as any).withViewOverrides = true;