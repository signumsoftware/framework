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

export default function TranslationCodeSync(p: RouteComponentProps<{ culture: string; assembly: string; namespace?: string; }>) {
  const cultures = useAPI(() => CultureClient.getCultures(true), []);
  const assembly = decodeDots(p.match.params.assembly);
  const culture = p.match.params.culture;
  const namespace = p.match.params.namespace && decodeDots(p.match.params.namespace);

  const [result, reloadResult] = useAPIWithReload(() => API.sync(assembly, culture, namespace), [assembly, culture, namespace]);  
    
  if (result?.totalTypes == 0) {
    return (
      <div>
        <h2>{TranslationMessage._0AlreadySynchronized.niceToString(namespace ?? assembly)}</h2>
        <Link to={`~/translation/status`}>
          {TranslationMessage.BackToTranslationStatus.niceToString()}
        </Link>
      </div>
    );
  }

  function handleSave() {
    API.save(assembly, culture ?? "", result!)
      .then(() => notifySuccess())
      .then(() => reloadResult())
      .done();
  }

  let message = TranslationMessage.Synchronize0In1.niceToString(namespace ?? assembly,
    cultures ? cultures[culture].toStr : culture);

  if (result) {
    message += ` [${Dic.getKeys(result.types).length}/${result.totalTypes}]`;
  }

  return (
    <div>
      <h2>{message}</h2>
      <br />
      {result && <SyncTable result={result} onSave={handleSave} currentCulture={culture} />}
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
