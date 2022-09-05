
import * as React from 'react'
import { Lite, is, getToString } from '@framework/Signum.Entities'
import { NavDropdown } from 'react-bootstrap'
import { useAPI } from '@framework/Hooks';
import { IsolationEntity, IsolationMessage } from './Signum.Entities.Isolation';
import * as IsolationClient from './IsolationClient';

export default function IsolationDropdown(props: {}) {

  var isolations = useAPI(signal => IsolationClient.API.isolations(), []);
  function handleSelect(e: React.MouseEvent, c: Lite<IsolationEntity> | undefined) {
    IsolationClient.changeOverridenIsolation(e, c);
  }

  if (!isolations)
    return null;

  const current = IsolationClient.getOverridenIsolation();

  return (
    <NavDropdown id="isolationDropdown" data-current-isolation={current?.id} title={current ? getToString(current) : <strong className="text-danger">{IsolationMessage.GlobalMode.niceToString()}</strong> } className="sf-isolation-dropdown" >
      <NavDropdown.Item data-isolation={name} disabled={is(undefined, current)} onClick={e => handleSelect(e, undefined)}>
        {IsolationMessage.GlobalMode.niceToString()}
      </NavDropdown.Item>
      <NavDropdown.Divider />
      {isolations.map((iso, i) =>
        <NavDropdown.Item key={i} data-isolation={name} disabled={is(iso, current)} onClick={e => handleSelect(e, iso)}>
          {getToString(iso)}
        </NavDropdown.Item>
      )}
    </NavDropdown >
  );
}
