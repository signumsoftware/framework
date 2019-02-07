import { ajaxPost, ajaxGet } from '@framework/Services';
import { Lite } from '@framework/Signum.Entities'
import * as Navigator from '@framework/Navigator'
import * as Finder from '@framework/Finder'
import { CultureInfoEntity } from '../Basics/Signum.Entities.Basics'
import { reloadTypes } from '@framework/Reflection'
import { toLite } from '@framework/Signum.Entities';

export let currentCulture: CultureInfoEntity;

export const onCultureLoaded: Array<(culture: CultureInfoEntity) => void> = [];
export function loadCurrentCulture(): Promise<void> {
  return API.fetchCurrentCulture()
    .then(ci => {
      currentCulture = ci;
      onCultureLoaded.forEach(f => f(ci));
    });
}

export function changeCurrentCulture(newCulture: Lite<CultureInfoEntity>) {
  API.setCurrentCulture(newCulture)
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
    return ajaxGet<CultureInfoEntity[]>({ url: "~/api/culture/cultures" });
  }

  export function fetchCurrentCulture(): Promise<CultureInfoEntity> {
    return ajaxGet<CultureInfoEntity>({ url: "~/api/culture/currentCulture" });
  }

  export function setCurrentCulture(culture: Lite<CultureInfoEntity>): Promise<string> {
    return ajaxPost<string>({ url: "~/api/culture/setCurrentCulture" }, culture);
  }
}

