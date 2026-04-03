import * as React from 'react'
import { Link, useLocation, useParams } from 'react-router-dom'
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome'
import { Dic } from '@framework/Globals'
import { Operations } from '@framework/Operations'
import { getToString } from '@framework/Signum.Entities'
import { CultureClient } from '@framework/Basics/CultureClient'
import { TranslationClient } from '../TranslationClient'
import { TranslationMessage } from '../Signum.Translation'
import { TranslationTypeTable } from './TranslationTypeTable'
import "../Translation.css"
import { decodeDots } from './TranslationCodeStatus'
import { useAPI } from '@framework/Hooks'
import { useTitle } from '@framework/AppContext'
import { QueryString } from '@framework/QueryString'

export default function TranslationCodeView(): React.JSX.Element {
  const params = useParams() as { culture: string; assembly: string };
  const location = useLocation();

  const assembly = decodeDots(params.assembly);
  const culture = params.culture;

  const cultures = useAPI(() => CultureClient.getCultures(null), []);

  const [filter, setFilter] = React.useState(() => QueryString.parse(location.search).filter);

  const result = useAPI(() => filter == "" ? Promise.resolve(undefined) : TranslationClient.API.retrieve(assembly, culture ?? "", filter), [assembly, culture, filter]);

  function renderTable() {
    if (result == undefined)
      return undefined;

    if (Dic.getKeys(result).length == 0)
      return <strong> {TranslationMessage.NoResultsFound.niceToString()}</strong>;

    return (
      <div>
        {Dic.getValues(result.types).map(type => <TranslationTypeTable key={type.type} type={type} result={result} currentCulture={params.culture} />)}
        <input type="submit" value={TranslationMessage.Save.niceToString()} className="btn btn-primary" onClick={handleSave} />
      </div>
    );
  }

  function handleSave(e: React.FormEvent<any>) {
    e.preventDefault();
    TranslationClient.API.save(decodeDots(params.assembly), params.culture ?? "", result!).then(() => Operations.notifySuccess());
  }

  const message = TranslationMessage.View0In1.niceToString(decodeDots(assembly),
    culture == undefined ? TranslationMessage.AllLanguages.niceToString() :
      cultures ? getToString(cultures[culture]) :
        culture);

  useTitle(message);

  return (
    <div>
      <h1 className="h2"><Link to="/translation/status">{TranslationMessage.CodeTranslations.niceToString()}</Link> {">"} {message}</h1>
      <TranslateSearchBox setFilter={setFilter} filter={filter} />
      <em> {TranslationMessage.PressSearchForResults.niceToString()}</em>
      <br />
      {renderTable()}
    </div>
  );
}

export function TranslateSearchBox(p: { filter: string, setFilter: (newFilter: string) => void }): React.JSX.Element{

  const [tmpFilter, setTmpFilter] = React.useState(p.filter);

  function handleSearch(e: React.FormEvent<any>) {
    e.preventDefault();
    p.setFilter(tmpFilter);
  }

  return (
    <form onSubmit={handleSearch} className="input-group">
      <input type="text" className="form-control"
        placeholder={TranslationMessage.Search.niceToString()} value={tmpFilter} onChange={e => setTmpFilter(e.currentTarget.value)} />
      <button className="btn btn-tertiary" type="submit" title={TranslationMessage.Search.niceToString()}>
        <FontAwesomeIcon aria-hidden={true} icon="magnifying-glass" />
      </button>
    </form>
  );
}
