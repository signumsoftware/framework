import * as React from "react"
import { Lite, Entity, liteKey, ModifiableEntity, getToString } from '../Signum.Entities';
import * as Navigator from '../Navigator';
import { Link } from 'react-router-dom';
import { StyleContext } from "../Lines";

export interface EntityLinkProps extends React.HTMLAttributes<HTMLAnchorElement> {
  lite: Lite<Entity>;
  inSearch?: boolean;
  inPlaceNavigation?: boolean;
  onNavigated?: (lite: Lite<Entity>) => void;
  getViewPromise?: (e: ModifiableEntity | null) => undefined | string | Navigator.ViewPromise<ModifiableEntity>;
  innerRef?: React.Ref<HTMLAnchorElement>;
}

export default function EntityLink(p: EntityLinkProps) {

  const { lite, inSearch, children, onNavigated, getViewPromise, inPlaceNavigation, ...htmlAtts } = p;

  if (!Navigator.isNavigable(lite.EntityType, undefined, p.inSearch || false))
    return <span data-entity={liteKey(lite)}>{p.children || getToString(lite)}</span>;


  return (
    <Link
      innerRef={p.innerRef as any}
      to={Navigator.navigateRoute(lite)}
      title={StyleContext.default.titleLabels ? p.title || getToString(lite) : undefined}
      onClick={handleClick}
      data-entity={liteKey(lite)}
      {...(htmlAtts as React.HTMLAttributes<HTMLAnchorElement>)}>
      {children || lite.toStr}
    </Link>
  );

  function handleClick(event: React.MouseEvent<any>) {

    event.preventDefault();
    const lite = p.lite;
    const s = Navigator.getSettings(lite.EntityType)
    const avoidPopup = s != undefined && s.avoidPopup;

    if (event.ctrlKey || event.button == 1 || avoidPopup && !p.inPlaceNavigation) {
      var vp = p.getViewPromise && p.getViewPromise(null);
      window.open(Navigator.navigateRoute(lite, vp && typeof vp == "string" ? vp : undefined));
      return;
    }

    if (p.inPlaceNavigation) {
      var vp = p.getViewPromise && p.getViewPromise(null);
      Navigator.history.push(Navigator.navigateRoute(lite, vp && typeof vp == "string" ? vp : undefined));
    } else {
      Navigator.navigate(lite, { getViewPromise: p.getViewPromise }).then(() => {
        p.onNavigated && p.onNavigated(lite);
      }).done();
    }
  }
}
