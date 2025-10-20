import * as React from 'react';
import { Entity, NormalControlMessage } from '../Signum.Entities';
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome';
import { Navigator } from '../Navigator';
import * as AppContext from '../AppContext';
import CopyButton from './CopyButton';

interface CopyLinkButtonProps {
  entity: Entity;
  className?: string;
}

export default function CopyLinkButton(p: CopyLinkButtonProps): React.ReactElement | null {
  if (p.entity.isNew)
    return null;

  return (
    <CopyButton
      getText={() => window.location.origin + AppContext.toAbsoluteUrl(Navigator.navigateRoute(p.entity))}
      className={p.className}
      title={NormalControlMessage.CopyEntityUrl.niceToString()}
    >
      <FontAwesomeIcon aria-hidden={true} icon="link" color="gray" />
    </CopyButton>
  );
}
