import * as React from 'react'
import { Dic } from '@framework/Globals';
import { Lite, is } from '@framework/Signum.Entities'
import { CultureInfoEntity } from '../Basics/Signum.Entities.Basics'
import * as CultureClient from './CultureClient'
import { NavDropdown } from 'react-bootstrap';
import { useAPI } from '@framework/Hooks';


export default function CultureDropdown(props: {}) {

  var cultures = useAPI(signal => CultureClient.getCultures(false), []);
  function handleSelect(c: Lite<CultureInfoEntity>) {
    CultureClient.changeCurrentCulture(c);
  }

  if (!cultures)
    return null;

  const current = CultureClient.currentCulture;

  const pair = Dic.map(cultures, (name, c) => ({ name, c })).filter(p => is(p.c, current)).singleOrNull();

  return (
    <NavDropdown id="cultureDropdown" data-culture={pair?.name} title={current.nativeName} className="sf-culture-dropdown">
      {Dic.map(cultures, (name, c, i) =>
        <NavDropdown.Item key={i} data-culture={name} disabled={is(c, current)} onClick={() => handleSelect(c)}>
          {c.toStr}
        </NavDropdown.Item>
      )}
    </NavDropdown >
  );
}
