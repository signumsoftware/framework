import * as React from 'react'
import { Popover, OverlayTrigger } from 'react-bootstrap'
import { TypeContext, AutoLine, EntityLine, EntityCombo, EntityList, EntityDetail, EntityStrip, EntityRepeater, EntityTable, FormGroup, MultiValueLine, ChangeEvent } from '@framework/Lines'
import { SearchControl, SearchValue, FilterOperation, OrderType, PaginationMode } from '@framework/Search'
import { toLite, Lite, JavascriptMessage } from '@framework/Signum.Entities';
import { DateTime } from 'luxon'
import { useAPI, useForceUpdate } from '@framework/Hooks'
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome'
import { RemoteEmailsClient } from './RemoteEmailsClient'
import { RemoteEmailRenderer } from './RemoteEmailMessage'
import { UserEntity, UserLiteModel } from '../../Signum.Authorization/Signum.Authorization';


export default function RemoteEmailPopover(p: { subject: string, user: Lite<UserEntity>, remoteEmailId: string, isRead: boolean }): React.JSX.Element {

  const [show, setShow] = React.useState(false);
  const handleOnMouseEnter = () => {
    setShow(true);
  }
  const handleOnMouseLeave = () => {
    setShow(false);
  }
  const ref = React.useRef(null);
  const popover = (
    <Popover id="popover-basic" style={{ "--bs-popover-max-width": "unset" } as React.CSSProperties} onMouseEnter={handleOnMouseEnter} onMouseLeave={handleOnMouseLeave}>
      <Popover.Header as="h3">{p.subject}</Popover.Header>
      <Popover.Body>
        <RemoteEmailSnippet user={p.user!} remoteEmailId={p.remoteEmailId} />
      </Popover.Body>
    </Popover>
  );

  return (
    <OverlayTrigger
      //trigger="hover"
      show={show} 
      placement="right" overlay={popover}>
      <span ref={ref} onMouseEnter={handleOnMouseEnter} onMouseLeave={handleOnMouseLeave}>
        <FontAwesomeIcon aria-hidden={true} icon={p.isRead ? ["far", "envelope-open"] : ["far", "envelope"] } className="me-1" />
      </span>
    </OverlayTrigger>
  );
}

export function RemoteEmailSnippet(p: {  user: Lite<UserEntity>, remoteEmailId: string }): React.JSX.Element {

  const model = p.user.model as UserLiteModel;

  const email = useAPI(() => RemoteEmailsClient.API.getRemoteEmail(model.oID!, p.remoteEmailId), [p.user, p.remoteEmailId]);

  return (
    <div style={{ minWidth: "500px" }}>
      {
        email == undefined ?
          <span>{JavascriptMessage.loading.niceToString()}</span> :
          <RemoteEmailRenderer remoteEmail={email} />
      }
    </div>
  );
}

