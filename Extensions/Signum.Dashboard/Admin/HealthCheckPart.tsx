
import * as React from 'react'
import { EntityTable } from '@framework/Lines'
import { TypeContext } from '@framework/TypeContext'
import { HealthCheckElementEmbedded, HealthCheckPartEntity } from '../Signum.Dashboard'
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome'
import { EntityControlMessage, newMListElement } from '@framework/Signum.Entities'
import { useForceUpdate } from '../../../Signum/React/Hooks'
import ErrorModal from '@framework/Modals/ErrorModal'
import { LinkButton } from '@framework/Basics/LinkButton'

export default function HealthCheckPart(p: { ctx: TypeContext<HealthCheckPartEntity> }): React.JSX.Element {
  const forceUpdate = useForceUpdate();
  const ctx = p.ctx.subCtx({ formGroupStyle: "SrOnly", placeholderLabels: true });

  return (
    <div>
      <EntityTable ctx={ctx.subCtx(p => p.items)} avoidFieldSet="h6" createAsLink={c =>
        <div>
          <LinkButton title={c.props.ctx.titleLabels ? EntityControlMessage.Create.niceToString() : undefined}
            className="sf-line-button sf-create"
            onClick={c.handleCreateClick}>
            <FontAwesomeIcon icon="plus" className="sf-create" />&nbsp;{EntityControlMessage.Create.niceToString()}
          </LinkButton>

          <LinkButton
            title={undefined}
            className="sf-line-button sf-create ms-4"
            onClick={async e => {
              var clipboard = await navigator.clipboard.readText();
              var data = clipboard.split('$#$');
              if (data.length != 3) {
                ErrorModal({ error: 'Clipboard data is not compatible with dashboard data!' });
                return;
              }
              var newItem = newMListElement(HealthCheckElementEmbedded.New({ title: data[0], checkURL: data[1], navigateURL: data[2] }));
              ctx.value.items.push(newItem);
              forceUpdate();
            }}>
            <FontAwesomeIcon aria-hidden={true} icon="heart-pulse" color="gray" /> Paste Health Check Link
          </LinkButton>
        </div>

      } />
    </div>
  );
}
