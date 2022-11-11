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
import { getTypeInfos, TypeReference } from '@framework/Reflection'
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome';
import { ajaxGet } from '../../Signum.React/Scripts/Services'
import { softCast } from '../../Signum.React/Scripts/Globals'

export interface TemplateControlsProps {
  queryKey: string;
  forHtml: boolean;
  widgetButtons?: boolean;
}

export default function TemplateControls(p: TemplateControlsProps) {

  const [currentToken, setCurrentToken] = React.useState<{ type: "Query", token?: QueryToken} | { type: "Global", expression?: GlobalVariable }>({ type: 'Query'});
  const qd = useAPI(() => Finder.getQueryDescription(p.queryKey), [p.queryKey]);

  function renderButton(text: string, canClick: string | undefined, buildPattern: (key: string) => string) {
    return <input type="button" disabled={!!canClick} className="btn btn-light btn-sm sf-button"
      title={canClick} value={text}
      onClick={() => ValueLineModal.show({
        type: { name: "string" },
        initialValue: buildPattern(
          currentToken.type == 'Query' ? (currentToken.token ? currentToken.token.fullKey : "") : (currentToken.expression ? ("g:" + currentToken.expression.key) : "")),
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


  function tokenHasAnyOrAll(): boolean {

    if (currentToken.type == 'Query')
      return hasAnyOrAll(currentToken.token)
    else {
      return false;
    }
  }

  function tokenIsCollection(): boolean {

    if (currentToken.type == 'Query')
      return Boolean(currentToken.token?.type.isCollection);

    return Boolean(currentToken.expression?.type.isCollection);
  }

  function canElement(): string | undefined {

    if (tokenIsCollection())
      return TemplateTokenMessage.YouCannotAddIfBlocksOnCollectionFields.niceToString();

    if (tokenHasAnyOrAll())
      return TemplateTokenMessage.YouCannotAddBlocksWithAllOrAny.niceToString();

    return undefined;
  }

  function canIf(): string | undefined {

    if (tokenIsCollection())
      return TemplateTokenMessage.YouCannotAddIfBlocksOnCollectionFields.niceToString();

    if (tokenHasAnyOrAll())
      return TemplateTokenMessage.YouCannotAddBlocksWithAllOrAny.niceToString();

    return undefined;
  }

  function canForeach(): string | undefined {

    if (tokenIsCollection())
      return TemplateTokenMessage.YouHaveToAddTheElementTokenToUseForeachOnCollectionFields.niceToString();

    if (tokenHasAnyOrAll())
      return TemplateTokenMessage.YouCannotAddBlocksWithAllOrAny.niceToString();

    return undefined;
  }

  function canAny() {

    if (tokenHasAnyOrAll())
      return TemplateTokenMessage.YouCannotAddBlocksWithAllOrAny.niceToString();

    return undefined;
  }
  const ct = currentToken;

  if (!p.queryKey)
    return null;

  return (
    <div className="d-flex">
      <select className="form-select form-select-sm w-auto" onChange={(e: React.FormEvent<any>) => setCurrentToken({ type: (e.currentTarget as HTMLSelectElement).value as "Query" | "Global" })} >
        <option value="Query">Query</option>
        <option value="Global">Global</option>
      </select>
      <span className="mx-1">:</span>
      <span className="rw-widget-sm">
        {ct.type == "Query" ? <QueryTokenBuilder queryToken={ct.token} queryKey={p.queryKey} onTokenChange={t => setCurrentToken({ type: "Query", token: t ?? undefined })} subTokenOptions={SubTokensOptions.CanAnyAll | SubTokensOptions.CanElement} readOnly={false} /> :
          <GlobalVariables onTokenChange={t => setCurrentToken({ type: 'Global', expression: t ?? undefined})} />}
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
            UserQueryEntity.tryTypeInfo() && renderWidgetButton(<><FontAwesomeIcon icon={["far", "rectangle-list"]} color={"dodgerblue"} className="icon" /> {UserQueryEntity.niceName()}</>, () => Finder.find<UserChartEntity>({
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


function GlobalVariables(p: { onTokenChange: (newToken: GlobalVariable | undefined) => void }) {
  var variableList = useAPI(signal => getGlobalVariables(), []);
  return (
    <select id="variables" className="form-select form-select-sm w-auto" onChange={(e: React.FormEvent<any>) => p.onTokenChange(variableList?.[parseInt((e.currentTarget as HTMLSelectElement).value)])}>
      {variableList?.map((v, i) => <option key={i} value={i}>{v.key}</option>)}
    </select>
    );
}

function getGlobalVariables(): Promise<Array<GlobalVariable>> {
  return ajaxGet({ url: `~/api/templating/getGlobalVariables` });
}

interface GlobalVariable {
  key: string;
  type: TypeReference;
}
