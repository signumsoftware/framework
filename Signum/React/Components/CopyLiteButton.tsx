import * as React from 'react';
import { Entity, NormalControlMessage, liteKey, toLite } from '../Signum.Entities';
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome';
import CopyButton from './CopyButton';

interface CopyLiteButtonProps {
  entity: Entity;
  className?: string;
}

export default function CopyLiteButton(p: CopyLiteButtonProps): React.ReactElement | null {
  if (p.entity.isNew)
    return null;

  return (
    <CopyButton
      getText={() => liteKey(toLite(p.entity))}
      className={p.className}
      title={NormalControlMessage.CopyEntityTypeAndIdForAutocomplete.niceToString()}
    >
      <FontAwesomeIcon aria-hidden={true} icon="copy" color="gray" />
    </CopyButton>
  );
}
