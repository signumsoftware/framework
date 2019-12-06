import * as React from 'react'
import { SubTokensOptions, QueryToken, hasAnyOrAll } from '@framework/FindOptions'
import { TemplateTokenMessage } from './Signum.Entities.Templating'
import QueryTokenBuilder from '@framework/SearchControl/QueryTokenBuilder'

export interface TemplateControlsProps {
  queryKey: string;
  onInsert: (newCode: string) => void;
  forHtml: boolean
}

export default function TemplateControls(p: TemplateControlsProps) {

  const [currentToken, setCurrentToken] = React.useState<QueryToken | undefined>(undefined)

  function renderButton(text: string, canClick: string | undefined, buildPattern: (key: string) => string) {
    return <input type="button" disabled={!!canClick} className="btn btn-light btn-sm sf-button"
      title={canClick} value={text}
      onClick={() => p.onInsert(buildPattern(currentToken ? currentToken.fullKey : ""))} />;
  }


  function canElement(): string | undefined {
    let token = currentToken;

    if (token == undefined)
      return TemplateTokenMessage.NoColumnSelected.niceToString();

    if (token.type.isCollection)
      return TemplateTokenMessage.YouCannotAddIfBlocksOnCollectionFields.niceToString();

    if (hasAnyOrAll(token))
      return TemplateTokenMessage.YouCannotAddBlocksWithAllOrAny.niceToString();

    return undefined;
  }

  function canIf(): string | undefined {
    let token = currentToken;

    if (token == undefined)
      return TemplateTokenMessage.NoColumnSelected.niceToString();

    if (token.type.isCollection)
      return TemplateTokenMessage.YouCannotAddIfBlocksOnCollectionFields.niceToString();

    if (hasAnyOrAll(token))
      return TemplateTokenMessage.YouCannotAddBlocksWithAllOrAny.niceToString();

    return undefined;
  }

  function canForeach(): string | undefined {

    let token = currentToken;

    if (token == undefined)
      return TemplateTokenMessage.NoColumnSelected.niceToString();

    if (token.type.isCollection)
      return TemplateTokenMessage.YouHaveToAddTheElementTokenToUseForeachOnCollectionFields.niceToString();

    if (token.key != "Element" || token.parent == undefined || !token.parent.type.isCollection)
      return TemplateTokenMessage.YouCanOnlyAddForeachBlocksWithCollectionFields.niceToString();

    if (hasAnyOrAll(token))
      return TemplateTokenMessage.YouCannotAddBlocksWithAllOrAny.niceToString();

    return undefined;
  }

  function canAny() {
    let token = currentToken;

    if (token == undefined)
      return TemplateTokenMessage.NoColumnSelected.niceToString();

    if (hasAnyOrAll(token))
      return TemplateTokenMessage.YouCannotAddBlocksWithAllOrAny.niceToString();

    return undefined;
  }
  const ct = currentToken;

  if (!p.queryKey)
    return null;

  return (
    <div>
      <span className="rw-widget-sm">
        <QueryTokenBuilder queryToken={ct} queryKey={p.queryKey} onTokenChange={t => setCurrentToken(t ?? undefined)} subTokenOptions={SubTokensOptions.CanAnyAll | SubTokensOptions.CanElement} readOnly={false} />
      </span>
      <div className="btn-group" style={{ marginLeft: "10px" }}>
        {renderButton(TemplateTokenMessage.Insert.niceToString(), canElement(), token => `@[${token}]`)}
        {renderButton("if", canIf(), token => p.forHtml ?
          `<!--@if[${token}]--> <!--@else--> <!--@endif-->` :
          `@if[${token}] @else @endif`)}
        {renderButton("foreach", canForeach(), token => p.forHtml ?
          `<!--@foreach[${token}]--> <!--@endforeach-->` :
          `@foreach[${token}] @endforeach`)}
        {renderButton("any", canElement(), token => p.forHtml ?
          `<!--@any[${token}]--> <!--@notany--> <!--@endany-->` :
          `@any[${token}] @notany @endany`)}
      </div>
    </div>
  );
}





