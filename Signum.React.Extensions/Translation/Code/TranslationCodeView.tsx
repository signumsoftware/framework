import * as React from 'react'
import { Link, RouteComponentProps } from 'react-router-dom'
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome'
import { Dic } from '@framework/Globals'
import { notifySuccess } from '@framework/Operations'
import { getToString, Lite } from '@framework/Signum.Entities'
import * as CultureClient from '../CultureClient'
import { API, AssemblyResult } from '../TranslationClient'
import { CultureInfoEntity } from '../../Basics/Signum.Entities.Basics'
import { TranslationMessage } from '../Signum.Entities.Translation'
import { TranslationTypeTable } from './TranslationTypeTable'
import "../Translation.css"
import { decodeDots } from './TranslationCodeStatus'
import { useAPI } from '@framework/Hooks'
import { useTitle } from '@framework/AppContext'
import { QueryString } from '@framework/QueryString'

export default function TranslationCodeView(p: RouteComponentProps<{ culture: string; assembly: string }>) {

  const assembly = decodeDots(p.match.params.assembly);
  const culture = p.match.params.culture;

  const cultures = useAPI(() => CultureClient.getCultures(null), []);

  const [filter, setFilter] = React.useState(() => QueryString.parse(p.location.search).filter);

  const result = useAPI(() => filter == "" ? Promise.resolve(undefined) : API.retrieve(assembly, culture ?? "", filter), [assembly, culture, filter]);

  function renderTable() {
    if (result == undefined)
      return undefined;

    if (Dic.getKeys(result).length == 0)
      return <strong> {TranslationMessage.NoResultsFound.niceToString()}</strong>;

    return (
      <div>
        {Dic.getValues(result.types).map(type => <TranslationTypeTable key={type.type} type={type} result={result} currentCulture={p.match.params.culture} />)}
        <input type="submit" value={TranslationMessage.Save.niceToString()} className="btn btn-primary" onClick={handleSave} />
      </div>
    );
  }

  function handleSave(e: React.FormEvent<any>) {
    e.preventDefault();
    const params = p.match.params;
    API.save(decodeDots(params.assembly), params.culture ?? "", result!).then(() => notifySuccess());
  }

  const message = TranslationMessage.View0In1.niceToString(decodeDots(assembly),
    culture == undefined ? TranslationMessage.AllLanguages.niceToString() :
      cultures ? getToString(cultures[culture]) :
        culture);

  useTitle(message);

  return (
    <div>
      <h2><Link to="~/translation/status">{TranslationMessage.CodeTranslations.niceToString()}</Link> {">"} {message}</h2>
      <TranslateSearchBox setFilter={setFilter} filter={filter} />
      <em> {TranslationMessage.PressSearchForResults.niceToString()}</em>
      <br />
      {renderTable()}
    </div>
  );
}

export function TranslateSearchBox(p: { filter: string, setFilter: (newFilter: string) => void }){

  const [tmpFilter, setTmpFilter] = React.useState(p.filter);

  function handleSearch(e: React.FormEvent<any>) {
    e.preventDefault();
    p.setFilter(tmpFilter);
  }

  return (
    <form onSubmit={handleSearch} className="input-group">
      <input type="text" className="form-control"
        placeholder={TranslationMessage.Search.niceToString()} value={tmpFilter} onChange={e => setTmpFilter(e.currentTarget.value)} />
      <button className="btn btn-outline-secondary" type="submit" title={TranslationMessage.Search.niceToString()}>
        <FontAwesomeIcon icon="magnifying-glass" />
      </button>
    </form>
  );
}
