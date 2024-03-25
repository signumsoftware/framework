import * as React from 'react'
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome'
import { RestApiKeyEntity, RestApiKeyMessage } from '../Signum.Rest'
import { TypeContext, AutoLine, EntityLine, TextBoxLine } from "@framework/Lines";
import { classes } from "@framework/Globals";
import * as RestApiKeyClient from "../RestApiKeyClient";
import { useForceUpdate } from '@framework/Hooks';

export default function RestApiKeyComponent(p : { ctx: TypeContext<RestApiKeyEntity> }){

  const forceUpdate = useForceUpdate();

  function generateApiKey(e: React.MouseEvent<any>) {
    e.preventDefault();
    RestApiKeyClient.API.generateRestApiKey()
      .then(key => {
        p.ctx.value.apiKey = key;
        p.ctx.value.modified = true;
        forceUpdate();
      });
  }

  const ctx = p.ctx;
  return (
    <div>
      <EntityLine ctx={ctx.subCtx(e => e.user)} />
      <TextBoxLine ctx={ctx.subCtx(e => e.apiKey)}
        extraButtons={vl =>
          <a href="#" className={classes("sf-line-button", "sf-view", "btn input-group-text")}            
            onClick={generateApiKey}>
            <FontAwesomeIcon icon="key" title={RestApiKeyMessage.GenerateApiKey.niceToString()}/>
          </a>} />
    </div>
  );
}

