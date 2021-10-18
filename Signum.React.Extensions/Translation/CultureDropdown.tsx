import * as React from 'react'
import { Dic } from '@framework/Globals';
import { Lite, is } from '@framework/Signum.Entities'
import { CultureInfoEntity } from '../Basics/Signum.Entities.Basics'
import * as CultureClient from './CultureClient'
import { NavDropdown } from 'react-bootstrap';
import { useAPI } from '@framework/Hooks';


export default function CultureDropdown(p: { fullName?: boolean }) {

  var cultures = useAPI(signal => CultureClient.getCultures(false), []);

  if (!cultures)
    return null;

  const current = CultureClient.currentCulture;

  const pair = Dic.map(cultures, (name, c) => ({ name, c })).singleOrNull(p => is(p.c, current));

  function handleSelect(c: Lite<CultureInfoEntity>) {
    CultureClient.changeCurrentCulture(c);
  }



  return (
    <NavDropdown id="cultureDropdown" data-culture={current.name} title={p.fullName ? current.nativeName : simplifyName(current.nativeName)} className="sf-culture-dropdown">
      {Dic.map(cultures, (name, c, i) =>
        <NavDropdown.Item key={i} data-culture={name} disabled={is(c, current)} onClick={() => handleSelect(c)}>
          {p.fullName ? c.toStr : simplifyName(c.toStr!)}
        </NavDropdown.Item>
      )}
    </NavDropdown >
  );
}

function simplifyName(name: string) {
  return name.tryBefore("(")?.trim() ?? name;
}
