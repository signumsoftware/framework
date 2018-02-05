import * as React from 'react'
import { Route } from 'react-router'
import { ajaxPost, ajaxGet } from '../../../Framework/Signum.React/Scripts/Services';
import { EntitySettings, ViewPromise } from '../../../Framework/Signum.React/Scripts/Navigator'
import { Entity, Lite } from '../../../Framework/Signum.React/Scripts/Signum.Entities'
import * as Navigator from '../../../Framework/Signum.React/Scripts/Navigator'
import * as Finder from '../../../Framework/Signum.React/Scripts/Finder'
import { EntityOperationSettings } from '../../../Framework/Signum.React/Scripts/Operations'
import * as Operations from '../../../Framework/Signum.React/Scripts/Operations'
import { CultureInfoEntity } from '../Basics/Signum.Entities.Basics'
import { reloadTypes } from '../../../Framework/Signum.React/Scripts/Reflection'
import { toLite } from '../../../Framework/Signum.React/Scripts/Signum.Entities';

export let currentCulture: CultureInfoEntity;


export const onCultureLoaded: Array<(culture: CultureInfoEntity) => void> = [];
export function loadCurrentCulture() : Promise<void> {
    return API.fetchCurrentCulture()
        .then(ci => {
            currentCulture = ci;
            onCultureLoaded.forEach(f => f(ci));
        }); 
}

export function changeCurrentCulture(newCulture: Lite<CultureInfoEntity>) {
    API.setCurrentCulture(newCulture)
        .then(c => {
            //if (!document.cookie.contains("language="))
                document.cookie = "language=" + c + ";path =" + Navigator.toAbsoluteUrl("~/");
        })
        .then(() => reloadTypes())
        .then(() => Finder.clearQueryDescriptionCache())
        .then(() => loadCurrentCulture())
        .then(() => Navigator.resetUI())
        .done();
}

let cachedCultures: Promise<CultureInfoEntity[]>;

export function getCultures(withHidden: boolean): Promise<{ [name: string]: Lite<CultureInfoEntity> }> {

    if (cachedCultures == null)
        cachedCultures = API.fetchCultures();
    
    return cachedCultures.then(list => {
        return list
            .filter(a => withHidden || !a.hidden)
            .toObject(a => a.name, a => toLite(a, false, a.nativeName!));
    });
}


export module API {
    export function fetchCultures(): Promise<CultureInfoEntity[]> {
        return ajaxGet<CultureInfoEntity[]>({ url: "~/api/culture/cultures", cache: "no-cache" });
    }

    export function fetchCurrentCulture(): Promise<CultureInfoEntity> {
        return ajaxGet<CultureInfoEntity>({ url: "~/api/culture/currentCulture", cache: "no-cache" });
    }

    export function setCurrentCulture(culture: Lite<CultureInfoEntity>): Promise<string> {
        return ajaxPost<string>({ url: "~/api/culture/currentCulture", cache: "no-cache", credentials: "include" }, culture);
    }
}

