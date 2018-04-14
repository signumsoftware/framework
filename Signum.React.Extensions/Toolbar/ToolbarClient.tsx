import * as React from 'react'
import { Route } from 'react-router'
import { Dic, classes } from '../../../Framework/Signum.React/Scripts/Globals';
import { ajaxPost, ajaxPostRaw, ajaxGet, saveFile } from '../../../Framework/Signum.React/Scripts/Services';
import { EntitySettings, ViewPromise } from '../../../Framework/Signum.React/Scripts/Navigator'
import * as Navigator from '../../../Framework/Signum.React/Scripts/Navigator'
import * as Finder from '../../../Framework/Signum.React/Scripts/Finder'
import { QueryEntity } from '../../../Framework/Signum.React/Scripts/Signum.Entities.Basics'
import { Lite, Entity, EntityPack, ExecuteSymbol, DeleteSymbol, ConstructSymbol_From, registerToString, JavascriptMessage, toLite } from '../../../Framework/Signum.React/Scripts/Signum.Entities'
import { EntityOperationSettings } from '../../../Framework/Signum.React/Scripts/Operations'
import { PseudoType, QueryKey, GraphExplorer, OperationType, Type, getTypeName  } from '../../../Framework/Signum.React/Scripts/Reflection'
import * as Operations from '../../../Framework/Signum.React/Scripts/Operations'
import { ToolbarEntity, ToolbarMenuEntity, ToolbarElementEmbedded, ToolbarElementType, ToolbarLocation } from './Signum.Entities.Toolbar'
import * as Constructor from '../../../Framework/Signum.React/Scripts/Constructor'
import * as UserAssetClient from '../UserAssets/UserAssetClient'

export function start(options: { routes: JSX.Element[] }, ...configs: ToolbarConfig<any>[]) {
    Navigator.addSettings(new EntitySettings(ToolbarEntity, t => import('./Templates/Toolbar')));
    Navigator.addSettings(new EntitySettings(ToolbarMenuEntity, t => import('./Templates/ToolbarMenu')));
    Navigator.addSettings(new EntitySettings(ToolbarElementEmbedded, t => import('./Templates/ToolbarElement')));   
    
    Finder.addSettings({ queryName: ToolbarEntity, defaultOrderColumn: "Priority", defaultOrderType: "Descending" });

    Constructor.registerConstructor(ToolbarElementEmbedded, tn => ToolbarElementEmbedded.New({ type: "Link" }));

    configs.forEach(c => registerConfig(c));
    
    UserAssetClient.start({ routes: options.routes });
    UserAssetClient.registerExportAssertLink(ToolbarEntity);
}



export abstract class ToolbarConfig<T extends Entity> {
    type: Type<T>;
    constructor(type: Type<T>) {
        this.type = type;
    }

    getIcon(element: ToolbarResponse<T>) {
        return ToolbarConfig.coloredIcon(element.iconName, element.iconColor);
    }

    static coloredIcon(className: string | null | undefined, color: string | null | undefined): React.ReactChild | null {
        if (!className || className.toLowerCase() == "none")
            return null;

        return <span className={"icon " + className} style={{ color: color || undefined }} />;
    }

    getLabel(element: ToolbarResponse<T>) {
        return element.label || element.content!.toStr;
    }
    
    abstract navigateTo(element: ToolbarResponse<T>): Promise<string>;


    handleNavigateClick(e: React.MouseEvent<any>, res: ToolbarResponse<any>) {

        var openWindow = e.ctrlKey || e.button == 1;
        e.persist();
        this.navigateTo(res).then(url => {
            Navigator.pushOrOpenInTab(url, e);
        }).done();
    }
}


export const configs: { [type: string]: ToolbarConfig<any> } = {};

export function registerConfig<T extends Entity>(config: ToolbarConfig<T>) {
    configs[config.type.typeName] = config;
}

export namespace API {
    export function getCurrentToolbar(location: ToolbarLocation): Promise<ToolbarResponse<any>> {
        return ajaxGet<ToolbarResponse<any>>({ url: `~/api/toolbar/current/${location}` });
    }
}

export interface ToolbarResponse<T extends Entity> {
    type: ToolbarElementType;
    iconName?: string;
    iconColor?: string;
    label?: string;
    content?: Lite<T>;
    url?: string;
    elements?: Array<ToolbarResponse<any>>;
    openInPopup?: boolean;
    autoRefreshPeriod?: number;
}