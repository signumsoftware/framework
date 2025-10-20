import * as React from 'react';
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome';
import CopyButton from './CopyButton';

export function CopyHealthCheckButton(p: { name: string, healthCheckUrl: string, clickUrl: string }): React.ReactElement | null {
  return (
    <CopyButton
      getText={() => p.name + '$#$' + p.healthCheckUrl + "$#$" + p.clickUrl}
      title="Copy Health Check dashboard data"
      className="mx-1"
    >
      <span className="btn btn-sm btn-tertiary sf-pointer" style={{color: "var(--bs-secondary)", backgroundColor: "var(--bs-body-bg)", border: "1px solid var(--bs-border-color)"}}>
        <FontAwesomeIcon aria-hidden={true} icon="heart-pulse" /> Health Check Link
      </span>
    </CopyButton>
  );
}
