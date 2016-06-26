import * as React from 'react'
import { Route } from 'react-router'
import { ajaxPost, ajaxGet } from '../../../Framework/Signum.React/Scripts/Services';
import { EntitySettings } from '../../../Framework/Signum.React/Scripts/Navigator'
import { Entity, Lite } from '../../../Framework/Signum.React/Scripts/Signum.Entities'
import * as Navigator from '../../../Framework/Signum.React/Scripts/Navigator'
import { EntityOperationSettings } from '../../../Framework/Signum.React/Scripts/Operations'
import * as Operations from '../../../Framework/Signum.React/Scripts/Operations'
import { CultureInfoEntity } from '../Basics/Signum.Entities.Basics'


export let currentCulture: CultureInfoEntity;

export function loadCurrentCulture() : Promise<void> {
    return API.fetchCurrentCulture()
        .then(ci => { currentCulture = ci; }); 
}

export const onCultureChanged: Array<(culture: CultureInfoEntity) => void> = [];
export function changeCurrentCulture(newCulture: Lite<CultureInfoEntity>) {
    API.setCurrentCulture(newCulture)
        .then(() => API.fetchCurrentCulture())
        .then(ci => {
            currentCulture = ci;
            onCultureChanged.forEach(f => f(ci));
        }).done();
}

export module API {
    export function fetchCultures(): Promise<{ [name: string]: Lite<CultureInfoEntity> }> {
        return ajaxGet<{ [name: string]: Lite<CultureInfoEntity> }>({ url: "~/api/translation/cultures", cache: "no-cache" });
    }

    export function fetchCurrentCulture(): Promise<CultureInfoEntity> {
        return ajaxGet<CultureInfoEntity>({ url: "~/api/translation/currentCulture", cache: "no-cache" });
    }

    export function setCurrentCulture(culture: Lite<CultureInfoEntity>): Promise<void> {
        return ajaxPost<void>({ url: "~/api/translation/currentCulture", cache: "no-cache" }, culture);
    }
}

