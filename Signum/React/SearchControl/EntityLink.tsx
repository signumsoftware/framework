import * as React from "react"
import { Lite, Entity, liteKey, ModifiableEntity, getToString } from '../Signum.Entities';
import * as AppContext from '../AppContext';
import { Navigator, ViewPromise } from '../Navigator';
import { Link } from 'react-router-dom';
import { StyleContext } from "../Lines";
import { classes } from "../Globals";

export interface EntityLinkProps extends React.HTMLAttributes<HTMLAnchorElement> {
  lite: Lite<Entity>;
  inSearch?: "main" | "related";
  inPlaceNavigation?: boolean;
  hideIfNotViable?: boolean;
  onNavigated?: (lite: Lite<Entity>) => void;
  getViewPromise?: (e: ModifiableEntity | null) => undefined | string | ViewPromise<ModifiableEntity>;
  innerRef?: React.Ref<HTMLAnchorElement>;
  stopPropagation?: boolean;
  extraProps?: any;
  extraQuery?: string;
  shy?: boolean
}

export default function EntityLink(p: EntityLinkProps): React.ReactElement | null {

  const { lite, inSearch, children, onNavigated, getViewPromise, inPlaceNavigation, shy, ...htmlAtts } = p;

  const settings = Navigator.getSettings(p.lite.EntityType);

  if (!Navigator.isViewable(lite, { isSearch: p.inSearch })){
    if (p.hideIfNotViable)
      return null;

    return <span data-entity={liteKey(lite)} className={settings?.allowWrapEntityLink ? undefined : "try-no-wrap"}>{p.children ?? Navigator.renderLite(lite)}</span>;
  }

  return (
    <Link
      ref={p.innerRef as any}
      to={Navigator.navigateRoute(lite)}
      title={StyleContext.default.titleLabels ? p.title ?? getToString(lite) : undefined}
      data-entity={liteKey(lite)}
      className={classes(settings?.allowWrapEntityLink ? undefined : "try-no-wrap", shy ? "sf-shy-link" : null)}
      {...(htmlAtts as React.HTMLAttributes<HTMLAnchorElement>)}
      onClick={handleClick}
    >
      {children ?? Navigator.renderLite(lite)}
    </Link>
  );

  function handleClick(event: React.MouseEvent<any>) {
    if (p.stopPropagation)
      event.stopPropagation();
    event.preventDefault();
    p.onClick?.call(event.currentTarget, event);

    const lite = p.lite;
    const s = Navigator.getSettings(lite.EntityType)
    const avoidPopup = s != undefined && s.avoidPopup;

    if (event.ctrlKey || event.button == 1 || avoidPopup && !p.inPlaceNavigation) {
      var vp = p.getViewPromise && p.getViewPromise(null);
      window.open(AppContext.toAbsoluteUrl(Navigator.navigateRoute(lite, vp && typeof vp == "string" ? vp : undefined) + (p.extraQuery ?? "")));
      return;
    }

    if (p.inPlaceNavigation) {
      var vp = p.getViewPromise && p.getViewPromise(null);
      AppContext.navigate(Navigator.navigateRoute(lite, vp && typeof vp == "string" ? vp : undefined) + (p.extraQuery ?? ""));
    } else {
      Navigator.view(lite, { getViewPromise: p.getViewPromise, buttons: "close", extraProps: p.extraProps }).then(() => {
        p.onNavigated && p.onNavigated(lite);
      });
    }
  }
}
