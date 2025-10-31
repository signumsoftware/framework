import * as React from 'react'
import { useParams } from 'react-router'
import { Dic } from '@framework/Globals'
import { Operations } from '@framework/Operations'
import { getToString } from '@framework/Signum.Entities'
import { TranslationClient } from '../TranslationClient'
import { TranslationMessage } from '../Signum.Translation'
import { TranslationTypeTable } from './TranslationTypeTable'
import { Link } from "react-router-dom";
import "../Translation.css"
import { decodeDots, encodeDots } from './TranslationCodeStatus'
import { useAPI, useAPIWithReload } from '@framework/Hooks'
import { useTitle } from '@framework/AppContext'
import { CultureClient } from '@framework/Basics/CultureClient'

export default function TranslationCodeSync(): React.JSX.Element {
  const params = useParams() as { culture: string; assembly: string; namespace?: string; };
  const cultures = useAPI(() => CultureClient.getCultures(null), []);
  const assembly = decodeDots(params.assembly);
  const culture = params.culture;
  const namespace = params.namespace && decodeDots(params.namespace);

  const [result, reloadResult] = useAPIWithReload(() => TranslationClient.API.sync(assembly, culture, namespace), [assembly, culture, namespace]);  

  var message = result?.totalTypes == 0 ? TranslationMessage._0AlreadySynchronized.niceToString(namespace ?? assembly) :
    TranslationMessage.Synchronize0In1.niceToString(namespace ?? assembly, cultures ? getToString(cultures[culture]) : culture) +
    (result ? ` [${Dic.getKeys(result.types).length}/${result.totalTypes}]` : " …");

  useTitle(message);

  function handleSave() {
    TranslationClient.API.save(assembly, culture ?? "", result!)
      .then(() => Operations.notifySuccess())
      .then(() => reloadResult());
  }

  return (
    <div>
      <h2><Link to="/translation/status">{TranslationMessage.CodeTranslations.niceToString()}</Link> {">"} {message}</h2>
      <br />
      {result && result.totalTypes > 0 && <SyncTable result={result} onSave={handleSave} currentCulture={culture} />}
      {result && result.totalTypes == 0 && <Link to={`/translation/syncNamespaces/${encodeDots(assembly)}/${culture}`}>
        {TranslationMessage.BackToSyncAssembly0.niceToString(assembly)}
      </Link>}
    </div>
  );
}

function SyncTable({ result, onSave, currentCulture }: { result: TranslationClient.AssemblyResult, onSave: () => void, currentCulture: string }) {

  return (
    <div>
      {Dic.getValues(result.types)
        .map(type => <TranslationTypeTable key={type.type} type={type} result={result} currentCulture={currentCulture} />)}
      <button type="button" className="btn btn-primary" onClick={onSave}>{TranslationMessage.Save.niceToString()}</button>
    </div>
  );
}
