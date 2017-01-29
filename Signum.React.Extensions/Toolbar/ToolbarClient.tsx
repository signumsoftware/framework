import * as React from 'react'
import { Route } from 'react-router'
import { Dic, classes } from '../../../Framework/Signum.React/Scripts/Globals';
import { Button, OverlayTrigger, Tooltip, MenuItem,  } from "react-bootstrap"
import { ajaxPost, ajaxPostRaw, ajaxGet, saveFile } from '../../../Framework/Signum.React/Scripts/Services';
import { EntitySettings, ViewPromise } from '../../../Framework/Signum.React/Scripts/Navigator'
import * as Navigator from '../../../Framework/Signum.React/Scripts/Navigator'
import * as Finder from '../../../Framework/Signum.React/Scripts/Finder'
import { QueryEntity } from '../../../Framework/Signum.React/Scripts/Signum.Entities.Basics'
import { Lite, Entity, EntityPack, ExecuteSymbol, DeleteSymbol, ConstructSymbol_From, registerToString, JavascriptMessage, toLite } from '../../../Framework/Signum.React/Scripts/Signum.Entities'
import { EntityOperationSettings } from '../../../Framework/Signum.React/Scripts/Operations'
import { PseudoType, QueryKey, GraphExplorer, OperationType, Type, getTypeName  } from '../../../Framework/Signum.React/Scripts/Reflection'
import * as Operations from '../../../Framework/Signum.React/Scripts/Operations'
import * as ContextualOperations from '../../../Framework/Signum.React/Scripts/Operations/ContextualOperations'
import { ToolbarEntity, ToolbarMenuEntity, ToolbarElementEntity, ToolbarElementType } from './Signum.Entities.Toolbar'
import * as Constructor from '../../../Framework/Signum.React/Scripts/Constructor'

export function start(...configs: ToolbarConfig<any>[]) {
    Navigator.addSettings(new EntitySettings(ToolbarEntity, t => new ViewPromise(resolve => require(['./Templates/Toolbar'], resolve))));
    Navigator.addSettings(new EntitySettings(ToolbarMenuEntity, t => new ViewPromise(resolve => require(['./Templates/ToolbarMenu'], resolve))));
    Navigator.addSettings(new EntitySettings(ToolbarElementEntity, t => new ViewPromise(resolve => require(['./Templates/ToolbarElement'], resolve))));   


    Finder.addSettings({ queryName: ToolbarEntity, defaultOrderColumn: "Priority", defaultOrderType: "Descending" });

    Constructor.registerConstructor(ToolbarElementEntity, tn => ToolbarElementEntity.New({ type : "Link" }));

    configs.forEach(c => registerConfig(c));
}



export abstract class ToolbarConfig<T extends Entity> {
    type: Type<T>;
    getIcon(element: ToolbarResponse<T>) {
        return this.coloredIcon(element.iconName, element.iconColor);
    }

    coloredIcon(className: string | null | undefined, color: string | null | undefined): React.ReactChild | null {
        if (!className || className.toLowerCase() == "none")
            return null;

        return <span className={"icon " + className} style={{ color: color, }} />;
    }

    getLabel(element: ToolbarResponse<T>) {
        return element.label || element.lite!.toStr;
    }
    
    abstract navigateTo(element: ToolbarResponse<T>): Promise<string>;


    handleNavigateClick(e: React.MouseEvent<any>, res: ToolbarResponse<any>) {

        var openWindow = e.ctrlKey || e.button == 1;

        this.navigateTo(res).then(url => {
            if (openWindow)
                window.open(url);
            else
                Navigator.currentHistory.push(url);
        }).done();
    }
}


export const configs: { [type: string]: ToolbarConfig<any> } = {};

export function registerConfig<T extends Entity>(config: ToolbarConfig<T>) {
    configs[config.type.typeName] = config;
}

export namespace API {
    export function getCurrentToolbar(): Promise<ToolbarResponse<any>> {
        return ajaxGet < ToolbarResponse<any>>({ url: `~/api/toolbar/current` });
    }
}

export interface ToolbarResponse<T extends Entity> {
    type: ToolbarElementType;
    iconName?: string;
    iconColor?: string;
    label?: string;
    lite?: Lite<T>;
    elements?: Array<ToolbarResponse<any>>;
}