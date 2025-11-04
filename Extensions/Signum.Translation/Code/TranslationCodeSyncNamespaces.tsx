import * as React from 'react'
import { Link, useLocation, useParams } from 'react-router-dom'
import { JavascriptMessage } from '@framework/Signum.Entities'
import { TranslationClient } from '../TranslationClient'
import { TranslationMessage } from '../Signum.Translation'
import "../Translation.css"
import { encodeDots, decodeDots } from './TranslationCodeStatus'
import { useAPI } from '@framework/Hooks'
import { AccessibleTable } from '../../../Signum/React/Basics/AccessibleTable'

export default function TranslationCodeSyncNamespaces(): React.JSX.Element {
  const params = useParams() as { culture: string; assembly: string; };
  const assembly = decodeDots(params.assembly);
  const culture = params.culture;

  const result = useAPI(() => TranslationClient.API.namespaceStatus(assembly, culture), [assembly, culture]);
  

  function renderTable() {
    if (result == undefined)
      return <strong>{JavascriptMessage.loading.niceToString()}</strong>;

    return (
      <AccessibleTable
        aria-label={`${TranslationMessage.Namespace.niceToString()} / ${TranslationMessage.NewTranslations.niceToString()}`}
        className="st table">
        <thead>
          <tr>
            <th> {TranslationMessage.Namespace.niceToString()} </th>
            <th> {TranslationMessage.NewTypes.niceToString()} </th>
            <th> {TranslationMessage.NewTranslations.niceToString()} </th>
          </tr>
        </thead>
        <tbody>
          <tr key={"All"}>
            <th>
              <Link to={`/translation/sync/${encodeDots(assembly)}/${culture}`}>
                {TranslationMessage.All.niceToString()}
              </Link>
            </th>
            <th> {result.sum(a => a.types)}</th>
            <th> {result.sum(a => a.translations)}</th>
          </tr>

          {result.map(stats =>
            <tr key={stats.namespace}>
              <td>
                <Link to={`/translation/sync/${encodeDots(assembly)}/${culture}/${encodeDots(stats.namespace)}`}>
                  {stats.namespace}
                </Link>
              </td>
              <th> {stats.types}</th>
              <th> {stats.translations}</th>
            </tr>
          )}
        </tbody>
      </AccessibleTable>
    );
  }
  if (result?.length == 0) {
    return (
      <div>
        <h2>{TranslationMessage._0AlreadySynchronized.niceToString(assembly)}</h2>
        <Link to={`/translation/status`}>
          {TranslationMessage.BackToTranslationStatus.niceToString()}
        </Link>
      </div>
    );
  }


  return (
    <div>
      <h2><Link to="/translation/status">{TranslationMessage.CodeTranslations.niceToString()}</Link> {">"} {TranslationMessage.Synchronize0In1.niceToString(assembly, culture)}</h2>
      {renderTable()}
    </div>
  );
}



