import { ajaxPost, ajaxGet } from '../Services';
import { Lite } from '../Signum.Entities'
import * as AppContext from '../AppContext'
import { CultureInfoEntity } from '../Signum.Basics'
import { reloadTypes } from '../Reflection'
import { toLite } from '../Signum.Entities';



export namespace CultureClient {

  let currentCulture: CultureInfoEntity;

  export function getCurrentCulture() : CultureInfoEntity {
    return currentCulture;
  }

  export const onCultureLoaded: Array<(culture: CultureInfoEntity) => void> = [];
  export function loadCurrentCulture(): Promise<void> {
    return API.fetchCurrentCulture()
      .then(ci => {
        currentCulture = ci;
        AppContext.setCurrentCulture(ci.name);
        // SC 3.1.1: the document language has to be the language the application actually renders in.
        // Index.cshtml stamps lang from the server's CurrentUICulture, which is negotiated from the request
        // (Accept-Language) and can differ from the signed-in user's stored culture — a German browser with
        // an English account produced <html lang="de-DE"> over an English interface, and nothing corrected
        // it, because lang was only ever set when the user actively switched culture.
        // Setting it here covers both paths: startup, and changeCurrentCulture, which routes through this.
        document.documentElement.setAttribute("lang", ci.name ?? "en");
        onCultureLoaded.forEach(f => f(ci));
      });
  }
  
  let onCultureChanged: (previousCulture: Lite<CultureInfoEntity>, newCulture: Lite<CultureInfoEntity>) => void = (pci, nci) => { };
  export function setOnCultureChanged(onChanged: (previousCulture: Lite<CultureInfoEntity>, newCulture: Lite<CultureInfoEntity>) => void): void {
    onCultureChanged = onChanged;
  }
  
  export function changeCurrentCulture(newCulture: Lite<CultureInfoEntity>): void {
    const previousCulture = currentCulture;
    API.setCurrentCulture(newCulture)
      // lang is now set inside loadCurrentCulture, so the separate call that used to follow here is gone.
      .then(() => loadCurrentCulture())
      .then(() => reloadTypes())
      .then(() => AppContext.resetUI())
      .then(() => onCultureChanged(toLite(previousCulture), newCulture));
  }
  
  let cachedCultures: Promise<CultureInfoEntity[]>;
  
  export function getCultures(isNeutral: boolean | null): Promise<{ [name: string]: Lite<CultureInfoEntity> }> {
    if (cachedCultures == null)
      cachedCultures = API.fetchCultures();
  
    return cachedCultures.then(list => {
      return list
        .filter(a => isNeutral == null || isNeutral == !a.name.contains("-"))
        .toObject(a => a.name, a => toLite(a, false, a.nativeName!));
    });
  }
  
  export namespace API {
    export function fetchCultures(): Promise<CultureInfoEntity[]> {
      return ajaxGet({ url: "/api/culture/cultures" });
    }
  
    export function fetchCurrentCulture(): Promise<CultureInfoEntity> {
      return ajaxGet({ url: "/api/culture/currentCulture" });
    }
  
    export function setCurrentCulture(culture: Lite<CultureInfoEntity>): Promise<string> {
      return ajaxPost({ url: "/api/culture/setCurrentCulture" }, culture);
    }
  }
}

