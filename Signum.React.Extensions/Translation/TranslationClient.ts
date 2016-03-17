import * as React from 'react'
import { Route } from 'react-router'
import { ajaxPost, ajaxGet } from '../../../Framework/Signum.React/Scripts/Services';
import { EntitySettings } from '../../../Framework/Signum.React/Scripts/Navigator'
import { Entity, Lite } from '../../../Framework/Signum.React/Scripts/Signum.Entities'
import * as Navigator from '../../../Framework/Signum.React/Scripts/Navigator'
import { EntityOperationSettings } from '../../../Framework/Signum.React/Scripts/Operations'
import * as Operations from '../../../Framework/Signum.React/Scripts/Operations'
import { CultureInfoEntity } from '../Basics/Signum.Entities.Basics'


export module Api {
    export function getCultures(): Promise<{ [name: string]: Lite<CultureInfoEntity> }> {
        return ajaxGet<{ [name: string]: Lite<CultureInfoEntity> }>({ url: "/api/translation/cultures", cache: "no-cache" });
    }

    export function getCurrentCulture(): Promise<Lite<CultureInfoEntity>> {
        return ajaxGet<Lite<CultureInfoEntity>>({ url: "/api/translation/currentCulture", cache: "no-cache" });
    }

    export function setCurrentCulture(culture: Lite<CultureInfoEntity>): Promise<void> {
        return ajaxPost<void>({ url: "/api/translation/currentCulture", cache: "no-cache" }, culture);
    }
}

