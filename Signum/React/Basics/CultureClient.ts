import { ajaxPost, ajaxGet } from '../Services';
import { Lite } from '../Signum.Entities'
import * as AppContext from '../AppContext'
import { CultureInfoEntity } from '../Signum.Basics'
import { reloadTypes } from '../Reflection'
import { toLite } from '../Signum.Entities';

export namespace CultureClient {
  
  export let currentCulture: CultureInfoEntity;
  
  export const onCultureLoaded: Array<(culture: CultureInfoEntity) => void> = [];
  export function loadCurrentCulture(): Promise<void> {
    return API.fetchCurrentCulture()
      .then(ci => {
        currentCulture = ci;
        AppContext.setCurrentCulture(ci.name);
        onCultureLoaded.forEach(f => f(ci));
      });
  }
  
  export let onCultureChanged: (previousCulture: Lite<CultureInfoEntity>, newCulture: Lite<CultureInfoEntity>) => void = (pci, nci) => { };
  export function setOnCultureChanged(onChanged: (previousCulture: Lite<CultureInfoEntity>, newCulture: Lite<CultureInfoEntity>) => void): void {
    onCultureChanged = onChanged;
  }
  
  export function changeCurrentCulture(newCulture: Lite<CultureInfoEntity>): void {
    const previousCulture = currentCulture;
    API.setCurrentCulture(newCulture)
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

