import * as React from 'react'
import { Link, RouteComponentProps } from 'react-router-dom'
import { Dic } from '@framework/Globals'
import { JavascriptMessage } from '@framework/Signum.Entities'
import { API, TranslationFileStatus } from '../TranslationClient'
import { TranslationMessage } from '../Signum.Entities.Translation'
import "../Translation.css"
import { useAPI } from '@framework/Hooks'

export default function TranslationCodeStatus(p: RouteComponentProps<{}>) {

  const result = useAPI(() => API.status(), []);

  return (
    <div>
      <h2>{TranslationMessage.CodeTranslations.niceToString()}</h2>
      {result == undefined ? <strong>{JavascriptMessage.loading.niceToString()}</strong> :
        <TranslationTable result={result} />}
    </div>
  );
}


function TranslationTable({ result }: { result: TranslationFileStatus[] }) {
  const tree = result.groupBy(a => a.assembly)
    .toObject(gr => gr.key, gr => gr.elements.toObject(a => a.culture));

  const assemblies = Dic.getKeys(tree);
  const cultures = Dic.getKeys(tree[assemblies.first()]);

  return (
    <table className="st">
      <thead>
        <tr>
          <th></th>
          <th> {TranslationMessage.All.niceToString()} </th>
          {cultures.map(culture => <th key={culture}>{culture}</th>)}
        </tr>
      </thead>
      <tbody>
        {assemblies.map(assembly =>
          <tr key={assembly}>
            <th> {assembly}</th>
            <td>
              <Link to={`~/translation/view/${encodeDots(assembly)}`}>{TranslationMessage.View.niceToString()}</Link>
            </td>
            {cultures.map(culture =>
              <td key={culture}>
                <Link to={`~/translation/view/${encodeDots(assembly)}/${culture}`}>{TranslationMessage.View.niceToString()}</Link>
                <br />
                {
                  !tree[assembly][culture].isDefault &&
                  <Link to={`~/translation/syncNamespaces/${encodeDots(assembly)}/${culture}`} className={"status-" + tree[assembly][culture].status}>
                    {TranslationMessage.Sync.niceToString()}
                  </Link>
                }
              </td>
            )}
          </tr>
        )}
      </tbody>
    </table>
  );
}

export function encodeDots(value: string) {
  return value.replaceAll(".", "-");
}

export function decodeDots(value: string) {
  return value.replaceAll("-", ".");
}


