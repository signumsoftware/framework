import * as React from "react"
import { Router, Route, Redirect, IndexRoute } from "react-router"
import { Button, OverlayTrigger, Tooltip, MenuItem, DropdownButton } from "react-bootstrap"
import { IEntity, Lite, Entity, ModifiableEntity, EmbeddedEntity, LiteMessage,
    OperationSymbol, ConstructSymbol_From, ConstructSymbol_FromMany, ConstructSymbol_Simple, ExecuteSymbol, DeleteSymbol, OperationMessage } from '../Signum.Entities';
import { PropertyRoute, PseudoType, EntityKind, TypeInfo, IType, Type, getTypeInfo, OperationInfo, OperationType  } from '../Reflection';
import * as Navigator from '../Navigator';
import { EntityComponent }  from '../Lines';
import { operationInfos, getSettings, EntityOperationSettings, EntityOperationContext, EntityOperationGroup, CreateGroup } from '../Operations'


export function getButtonBarElements(ctx: Navigator.ButtonsContext): Array<React.ReactChild> {
    const ti = getTypeInfo(ctx.pack.entity.Type);

    if (ti == null)
        return null;

    const operations = operationInfos(ti)
        .filter(oi => isEntityOperation(oi.operationType) && oi.allowNew || !ctx.pack.entity.isNew)
        .map(oi => {
            const eos = getSettings(oi.key) as EntityOperationSettings<Entity>;

            const eoc: EntityOperationContext<Entity> = {
                entity: ctx.pack.entity,
                component: ctx.component as any as EntityComponent<Entity>,
                canExecute: ctx.pack.canExecute[oi.key],
                operationInfo: oi,
                settings: eos
            };

            if (eos && eos.isVisible ? eos.isVisible(eoc) : ctx.showOperations)
                if (eoc.settings == null || !eoc.settings.hideOnCanExecute || eoc.canExecute == null)
                    return eoc;

            return null;
        })
        .filter(eoc => eoc != null);

    var groups = operations.groupByArray(eoc => {

        const group = getDefaultGroup(eoc);

        if (group == null)
            return "";

        return group.key;
    });


    var result = groups.flatMap((gr, i) => {
        if (gr.key == "") {
            return gr.elements.map((eoc, j) => ({
                order: eoc.settings && eoc.settings.order,
                button: createDefaultButton(eoc, null, false, i + "-" + j)
            }));
        } else {
            var group = getDefaultGroup(gr.elements[0]);

            return [{
                order: group.order,
                button: (
                    <DropdownButton title={group.text} data-key={group.key} key={i}>
                        { gr.elements
                            .orderBy(a => a.settings && a.settings.order)
                            .map((eoc, j) => createDefaultButton(eoc, group, true, j))
                        }
                    </DropdownButton>
                )
            }];
        }
    });

    return result.orderBy(a => a.order).map(a => a.button);
}

function getDefaultGroup(eoc: EntityOperationContext<Entity>) {
    if (eoc.settings != null && eoc.settings.group !== undefined) {
        return eoc.settings.group; //maybe null 
    }

    if (eoc.operationInfo.operationType == OperationType.ConstructorFrom)
        return CreateGroup;

    return null;
}

function createDefaultButton(eoc: EntityOperationContext<Entity>, group: EntityOperationGroup, asMenuItem: boolean, key: any) {

    var text = eoc.settings && eoc.settings.text ? eoc.settings.text() :
        group && group.simplifyName ? group.simplifyName(eoc.operationInfo.niceName) :
            eoc.operationInfo.niceName;

    var bsStyle = eoc.settings && eoc.settings.style || autoStyleFunction(eoc.operationInfo);

    var onClick = eoc.settings && eoc.settings.onClick ? () => eoc.settings.onClick(eoc) : () => defaultClick(eoc);

    var btn = !asMenuItem ?
        <Button bsStyle={bsStyle} disabled={!!eoc.canExecute} onClick={onClick} data-operation={eoc.operationInfo.key} key={key}>{text}</Button> :
        <MenuItem className={"btn-" + bsStyle} disabled={!!eoc.canExecute} onClick={onClick} data-operation={eoc.operationInfo.key} key={key}>{text}</MenuItem>;

    if (!eoc.canExecute)
        return btn;

    const tooltip = <Tooltip>{eoc.canExecute}</Tooltip>;

    return <OverlayTrigger placement="bottom" overlay={tooltip}>{btn}</OverlayTrigger>;
}

function defaultClick(eoc: EntityOperationContext<Entity>) {

}

export function autoStyleFunction(oi: OperationInfo) {
    return oi.operationType == OperationType.Delete ? "danger" :
        oi.operationType == OperationType.Execute && oi.key.endsWith(".Save") ? "primary" : "default";
}


function isEntityOperation(operationType: OperationType) {
    return operationType == OperationType.ConstructorFrom ||
        operationType == OperationType.Execute ||
        operationType == OperationType.Delete;
}