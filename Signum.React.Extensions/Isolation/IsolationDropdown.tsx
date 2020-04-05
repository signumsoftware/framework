
import * as React from 'react'
import { getTypeInfo } from '@framework/Reflection'
import { JavascriptMessage, Lite, is } from '@framework/Signum.Entities'
import { NavDropdown, Dropdown } from 'react-bootstrap'
import { useAPI } from '@framework/Hooks';
import { LinkContainer } from '@framework/Components'
import { IsolationEntity, IsolationMessage } from './Signum.Entities.Isolation';
import * as IsolationClient from './IsolationClient';


export default function IsolationDropdown(props: {}) {

  var isolations = useAPI(signal => IsolationClient.API.isolations(), []);
  function handleSelect(c: Lite<IsolationEntity> | undefined) {
    IsolationClient.changeOverridenIsolation(c);
  }

  if (!isolations)
    return null;

  const current = IsolationClient.overridenIsolation;

  return (
    <NavDropdown id="cultureDropdown" data-current-isolation={current?.id} title={current?.toStr ?? IsolationMessage.GlobalMode.niceToString()} className="sf-isolation-dropdown" >
      <NavDropdown.Item data-isolation={name} disabled={is(undefined, current)} onClick={() => handleSelect(undefined)}>
        {IsolationMessage.GlobalMode.niceToString()}
      </NavDropdown.Item>
      <NavDropdown.Divider />
      {isolations.map((iso, i) =>
        <NavDropdown.Item key={i} data-isolation={name} disabled={is(iso, current)} onClick={() => handleSelect(iso)}>
          {iso.toStr}
        </NavDropdown.Item>
      )}
    </NavDropdown >
  );
}
