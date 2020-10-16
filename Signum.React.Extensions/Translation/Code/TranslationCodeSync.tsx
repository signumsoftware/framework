import * as React from 'react'
import { RouteComponentProps } from 'react-router'
import { Dic } from '@framework/Globals'
import { notifySuccess } from '@framework/Operations'
import { Lite } from '@framework/Signum.Entities'
import * as CultureClient from '../CultureClient'
import { API, AssemblyResult } from '../TranslationClient'
import { CultureInfoEntity } from '../../Basics/Signum.Entities.Basics'
import { TranslationMessage } from '../Signum.Entities.Translation'
import { TranslationTypeTable } from './TranslationTypeTable'
import { Link } from "react-router-dom";
import "../Translation.css"
import { decodeDots } from './TranslationCodeStatus'
import { useAPI, useAPIWithReload } from '@framework/Hooks'
import { useTitle } from '../../../../Framework/Signum.React/Scripts/AppContext'

export default function TranslationCodeSync(p: RouteComponentProps<{ culture: string; assembly: string; namespace?: string; }>) {
  const cultures = useAPI(() => CultureClient.getCultures(true), []);
  const assembly = decodeDots(p.match.params.assembly);
  const culture = p.match.params.culture;
  const namespace = p.match.params.namespace && decodeDots(p.match.params.namespace);

  const [result, reloadResult] = useAPIWithReload(() => API.sync(assembly, culture, namespace), [assembly, culture, namespace]);  

  var message = result?.totalTypes == 0 ? TranslationMessage._0AlreadySynchronized.niceToString(namespace ?? assembly) :
    TranslationMessage.Synchronize0In1.niceToString(namespace ?? assembly, cultures ? cultures[culture].toStr : culture) +
    (result ? ` [${Dic.getKeys(result.types).length}/${result.totalTypes}]` : " …");

  useTitle(message);

  function handleSave() {
    API.save(assembly, culture ?? "", result!)
      .then(() => notifySuccess())
      .then(() => reloadResult())
      .done();
  }

  return (
    <div>
      <h2>{message}</h2>
      <br />
      {result && result.totalTypes > 0 && <SyncTable result={result} onSave={handleSave} currentCulture={culture} />}
      {result && result.totalTypes == 0 && <Link to={`~/translation/status`}>
        {TranslationMessage.BackToTranslationStatus.niceToString()}
      </Link>}
    </div>
  );
}

function SyncTable({ result, onSave, currentCulture }: { result: AssemblyResult, onSave: () => void, currentCulture: string }) {

  return (
    <div>
      {Dic.getValues(result.types)
        .map(type => <TranslationTypeTable key={type.type} type={type} result={result} currentCulture={currentCulture} />)}
      <button className="btn btn-primary" onClick={onSave}>{TranslationMessage.Save.niceToString()}</button>
    </div>
  );
}
