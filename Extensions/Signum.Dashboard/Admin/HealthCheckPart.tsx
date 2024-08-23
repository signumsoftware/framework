
import * as React from 'react'
import { EntityTable } from '@framework/Lines'
import { TypeContext } from '@framework/TypeContext'
import { HealthCheckElementEmbedded, HealthCheckPartEntity } from '../Signum.Dashboard'
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome'
import { newMListElement } from '@framework/Signum.Entities'
import { useForceUpdate } from '../../../Signum/React/Hooks'
import ErrorModal from '@framework/Modals/ErrorModal'

export default function HealthCheckPart(p: { ctx: TypeContext<HealthCheckPartEntity> }): React.JSX.Element {
  const forceUpdate = useForceUpdate();
  const ctx = p.ctx.subCtx({ formGroupStyle: "SrOnly", placeholderLabels: true });

  return (
    <div>
      <a href='#' className="btn btn-sm btn-light text-dark sf-pointer mx-1"
        onClick={async e => {
          e.preventDefault();
          var clipboard = await navigator.clipboard.readText();
          var data = clipboard.split('$#$');
          if (data.length != 3){
            ErrorModal({error: 'Clipboard data is not compatible with dashboard data!'});
            return ;
          }
          var newItem = newMListElement(HealthCheckElementEmbedded.New({ title: data[0], checkURL: data[1], navigateURL: data[2] }));
          ctx.value.items.push(newItem);
          forceUpdate();
        }}
        title="Paste Health Check dashboard data">
        <FontAwesomeIcon icon="heart-pulse" color="gray" />
      </a>
      <EntityTable ctx={ctx.subCtx(p => p.items)} />
    </div>
  );
}
