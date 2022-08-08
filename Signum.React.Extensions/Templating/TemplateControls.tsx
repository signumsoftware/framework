import * as React from 'react'
import { SubTokensOptions, QueryToken, hasAnyOrAll, FindOptions } from '@framework/FindOptions'
import { TemplateTokenMessage } from './Signum.Entities.Templating'
import QueryTokenBuilder from '@framework/SearchControl/QueryTokenBuilder'
import ValueLineModal from '../../Signum.React/Scripts/ValueLineModal'
import { UserChartEntity } from '../Chart/Signum.Entities.Chart'
import { useAPI } from '../../Signum.React/Scripts/Hooks'
import * as Navigator from '../../Signum.React/Scripts/Navigator'
import * as Finder from '../../Signum.React/Scripts/Finder'
import { UserQueryEntity } from '../UserQueries/Signum.Entities.UserQueries'
import { getTypeInfos } from '@framework/Reflection'
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome';

export interface TemplateControlsProps {
  queryKey: string;
  forHtml: boolean;
  widgetButtons?: boolean;
}

export default function TemplateControls(p: TemplateControlsProps) {

  const [currentToken, setCurrentToken] = React.useState<QueryToken | undefined>(undefined);
  const qd = useAPI(() => Finder.getQueryDescription(p.queryKey), [p.queryKey]);

  function renderButton(text: string, canClick: string | undefined, buildPattern: (key: string) => string) {
    return <input type="button" disabled={!!canClick} className="btn btn-light btn-sm sf-button"
      title={canClick} value={text}
      onClick={() => ValueLineModal.show({
        type: { name: "string" },
        initialValue: buildPattern(currentToken ? currentToken.fullKey : ""),
        title: "Template",
        message: "Copy to clipboard: Ctrl+C, ESC",
        initiallyFocused: true,
      })} />
  }

  function renderWidgetButton(text: React.ReactElement, getCode: () => Promise<string | undefined>) {
    return <button className="btn btn-light btn-sm sf-button"

      onClick={() =>
        getCode()
          .then(code =>
            code &&
            ValueLineModal.show({
              type: { name: "string" },
              valueLineType: "TextArea",
              initialValue: code,
              title: "Embedded Widget",
              message: "Make a similar-looking Chart or Table in Excel and copy it to Word or PowerPoint. Then add the following code in the Alternative Text to bind the data:",
              initiallyFocused: true,
            }))} >{text}</button>
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
    <div className="d-flex">
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
      {p.widgetButtons &&
        <div className="btn-group" style={{ marginLeft: "auto" }}>
          {UserChartEntity.tryTypeInfo() && renderWidgetButton(<><FontAwesomeIcon icon={"chart-bar"} color={"darkviolet"} className="icon" /> {UserChartEntity.niceName()}</>, () => Finder.find<UserChartEntity>({
            queryName: UserChartEntity,
            filterOptions: [{
              token: UserChartEntity.token(a => a.entity!.entityType!.entity!.cleanName),
              operation: "IsIn",
              value: [null, ...getTypeInfos(qd?.columns["Entity"].type!).map(a => a.name)]
            }]
          }).then(uc => uc && Navigator.API.fetch(uc).then(uce => {
            var text = "UserChart:" + uce.guid;

            if ((uce.chartScript.key.contains("Multi") || uce.chartScript.key.contains("Stacked")) && uce.columns[1].element.token != null /*Split*/)
              text += "\nPivot(0, 1, 2)";

            return text;
          })))}
          {
            UserQueryEntity.tryTypeInfo() && renderWidgetButton(<><FontAwesomeIcon icon={["far", "list-alt"]} color={"dodgerblue"} className="icon" /> {UserQueryEntity.niceName()}</>, () => Finder.find<UserChartEntity>({
              queryName: UserQueryEntity,
              filterOptions: [{
                token: UserQueryEntity.token(a => a.entity!.entityType!.entity!.cleanName),
                operation: "IsIn",
                value: [null, ...getTypeInfos(qd?.columns["Entity"].type!).map(a => a.name)]
              }]
            }).then(uc => uc && Navigator.API.fetch(uc).then(uce => "UserQuery:" + uce.guid)))
          }
        </div>
      }
    </div>
  );
}





