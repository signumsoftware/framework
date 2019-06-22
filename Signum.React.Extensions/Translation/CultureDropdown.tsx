import * as React from 'react'
import { Dic } from '@framework/Globals';
import { Lite, is } from '@framework/Signum.Entities'
import { CultureInfoEntity } from '../Basics/Signum.Entities.Basics'
import * as CultureClient from './CultureClient'
import { UncontrolledDropdown, DropdownToggle, DropdownMenu, DropdownItem } from '@framework/Components';
import { useAPI } from '../../../Framework/Signum.React/Scripts/Hooks';


export default function CultureDropdown(props: {}) {

  var cultures = useAPI(undefined, [], signal => CultureClient.getCultures(false));
  function handleSelect(c: Lite<CultureInfoEntity>) {
    CultureClient.changeCurrentCulture(c);
  }

  if (!cultures)
    return null;

  const current = CultureClient.currentCulture;

  const pair = Dic.map(cultures, (name, c) => ({ name, c })).filter(p => is(p.c, current)).singleOrNull();

  return (
    <UncontrolledDropdown id="cultureDropdown" data-culture={pair && pair.name} nav inNavbar>
      <DropdownToggle nav caret>
        {current.nativeName}
      </DropdownToggle>
      <DropdownMenu right>
        {Dic.map(cultures, (name, c, i) =>
          <DropdownItem key={i} data-culture={name} disabled={is(c, current)} onClick={() => handleSelect(c)}>
            {c.toStr}
          </DropdownItem>
        )}
      </DropdownMenu>
    </UncontrolledDropdown>
  );
}
