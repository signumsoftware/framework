import * as React from 'react'
import { FormGroup } from '@framework/Lines'
import { TypeContext } from '@framework/TypeContext'
import { QueryToken, SubTokensOptions } from '@framework/FindOptions'
import QueryTokenBuilder from '@framework/SearchControl/QueryTokenBuilder'
import { useForceUpdate } from '@framework/Hooks'
import { QueryTokenEmbedded } from '../Signum.UserAssets.Queries'

interface QueryTokenEmbeddedBuilderProps {
  ctx: TypeContext<QueryTokenEmbedded | null>;
  queryKey: string;
  subTokenOptions: SubTokensOptions;
  onTokenChanged?: (newToken: QueryToken | undefined) => void;
  helpText?: React.ReactChild;
}

export default function QueryTokenEmbeddedBuilder(p: QueryTokenEmbeddedBuilderProps) {
  const forceUpdate = useForceUpdate();
  function handleTokenChanged(newToken: QueryToken | undefined) {
    if (newToken == null)
      p.ctx.value = null;
    else
      p.ctx.value = QueryTokenEmbedded.New({
        tokenString: newToken.fullKey,
        token: newToken
      });

    if (p.onTokenChanged)
      p.onTokenChanged(newToken);

    forceUpdate();
  }

  const qte = p.ctx.value;

  const tokenBuilder = (
    <div className={p.ctx.rwWidgetClass}>
      <QueryTokenBuilder queryToken={qte?.token}
        onTokenChange={handleTokenChanged} queryKey={p.queryKey} subTokenOptions={p.subTokenOptions}
        readOnly={p.ctx.readOnly} />
    </div>
  );

  return (
    <FormGroup ctx={p.ctx} helpText={p.helpText}>
      {() => !qte || !qte.parseException ? tokenBuilder :
          <div>
            <code>{qte.tokenString}</code>
            <br />
            {tokenBuilder}
            <br />
            <p className="alert alert-danger">
              {qte.parseException}
            </p>
          </div>
      }
    </FormGroup>
  );
}

