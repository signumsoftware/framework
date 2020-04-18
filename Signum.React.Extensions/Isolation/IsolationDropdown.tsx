
import * as React from 'react'
import { Lite, is } from '@framework/Signum.Entities'
import { NavDropdown } from 'react-bootstrap'
import { useAPI } from '@framework/Hooks';
import { IsolationEntity, IsolationMessage } from './Signum.Entities.Isolation';
import * as IsolationClient from './IsolationClient';


export default function IsolationDropdown(props: {}) {

  var isolations = useAPI(signal => IsolationClient.API.isolations(), []);
  function handleSelect(c: Lite<IsolationEntity> | undefined) {
    IsolationClient.changeOverridenIsolation(c);
  }

  if (!isolations)
    return null;

  const { current, title } = IsolationClient.getOverridenIsolation();

  return (
    <NavDropdown id="cultureDropdown" data-current-isolation={current?.id} title={title} className="sf-isolation-dropdown" >
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
