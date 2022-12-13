import * as React from 'react'
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome'
import { Dic, softCast } from '@framework/Globals'
import { notifySuccess } from '@framework/Operations'
import * as CultureClient from '../CultureClient'
import { API, TranslatedInstanceView, TranslatedInstanceViewType, TranslatedTypeSummary, TranslationRecord } from '../TranslatedInstanceClient'
import { TranslationMessage } from '../Signum.Entities.Translation'
import { RouteComponentProps } from "react-router";
import { Link} from "react-router-dom";
import "../Translation.css"
import { useAPI, useForceUpdate, useAPIWithReload, useLock } from '@framework/Hooks'
import { EntityLink } from '@framework/Search'
import { DiffDocumentSimple } from '../../DiffLog/Templates/DiffDocument'
import TextArea from '@framework/Components/TextArea'
import { KeyCodes } from '@framework/Components'
import { useTitle } from '@framework/AppContext'
import { QueryString } from '@framework/QueryString'
import { getToString } from '@framework/Signum.Entities'

export default function TranslationInstanceView(p: RouteComponentProps<{ type: string; culture?: string; }>) {

  const type = p.match.params.type;
  const culture = p.match.params.culture;

  const cultures = useAPI(() => CultureClient.getCultures(null), []);
  const [isLocked, lock] = useLock();


  const [onlyNeutral, setOnlyNeutral] = React.useState<boolean>(true);

  const [filter, setFilter] = React.useState<string | undefined>(() => QueryString.parse(p.location.search).filter);

  const [result, reloadResult] = useAPIWithReload(() => filter == undefined ? Promise.resolve(undefined) : API.viewTranslatedInstanceData(type, culture, filter), [type, culture, filter]);

  function renderTable() {
    if (result == undefined || cultures == undefined)
      return undefined;

    if (Dic.getKeys(result).length == 0)
      return <strong> {TranslationMessage.NoResultsFound.niceToString()}</strong>;

    const otherCultures = Dic.getKeys(cultures)
      .filter(a => a != result.masterCulture)
      .filter(a => !onlyNeutral || !a.contains("-"));


    return (
      <div>
        <TranslatedInstances data={result} currentCulture={p.match.params.culture} cultures={culture ? [culture] : otherCultures} />
        {result.instances.length > 0 && <input type="submit" value={TranslationMessage.Save.niceToString()} className="btn btn-primary mt-2" onClick={handleSave} disabled={isLocked} />}
      </div>
    );
  }

  function handleSave(e: React.FormEvent<any>) {
    e.preventDefault();
    const params = p.match.params;
    const records = result!.instances.flatMap(ins => Dic.getKeys(ins.translations).flatMap(k => {
      const pr = k.tryBefore(";") ?? k;
      const rowId = k.tryAfter(";");
      const cultures = ins.translations[k];
      return Dic.getKeys(cultures).filter(c => culture == null || culture == c).map(c => softCast<TranslationRecord>({
        lite: ins.lite,
        propertyRoute: pr,
        rowId: rowId,
        culture: c,
        originalText: cultures[c].originalText,
        translatedText: cultures[c].translatedText
      }));
    }));

    lock(() => API.saveTranslatedInstanceData(records, type, false, culture)
      .then(() => { reloadResult(); notifySuccess(); }));
  }

  const message = TranslationMessage.View0In1.niceToString(type,
    culture == undefined ? TranslationMessage.AllLanguages.niceToString() :
      cultures ? getToString(cultures[culture]) :
        culture);

  useTitle(message);

  return (
    <div>
      <div className="mb-2">
        <h2><Link to="~/translatedInstance/status">{TranslationMessage.InstanceTranslations.niceToString()}</Link> {">"} {message}</h2>
        <TranslateSearchBox setFilter={setFilter} filter={filter ?? ""} />
        {culture == null && <label style={{ float: 'right' }}>
          <input type="checkbox" checked={onlyNeutral} onChange={e => setOnlyNeutral(e.currentTarget.checked)} /> Only neutral cultures
        </label>
        }
        <em> {TranslationMessage.PressSearchForResults.niceToString()}</em>
      </div>
      {renderTable()}
    </div>
  );
}

export function TranslateSearchBox(p: { filter: string, setFilter: (newFilter: string) => void }) {

  const [tmpFilter, setTmpFilter] = React.useState(p.filter);

  function handleSearch(e: React.FormEvent<any>) {
    e.preventDefault();
    p.setFilter(tmpFilter);
  }

  function handleKeyDown(e: React.KeyboardEvent<any>) {
    if (e.keyCode == KeyCodes.enter) {
      e.preventDefault();
      p.setFilter(tmpFilter);
    }
  }

  return (
    <form onSubmit={handleSearch} className="input-group">
      <input type="text" className="form-control"
        placeholder={TranslationMessage.Search.niceToString()} value={tmpFilter} onChange={e => setTmpFilter(e.currentTarget.value)} onKeyDown={handleKeyDown} />
      <button className="btn btn-outline-secondary" type="submit" title={TranslationMessage.Search.niceToString()}>
        <FontAwesomeIcon icon="magnifying-glass" />
      </button>
    </form>
  );
}

export function TranslatedInstances(p: { data: TranslatedInstanceViewType, cultures: string[], currentCulture?: string | undefined }) {


  return (
    <table id="results" style={{ width: "100%", margin: "0px" }} className="st">
      {p.data.instances.map(ins => <TranslatedInstance ins={ins} cultures={p.cultures} currentCulture={p.currentCulture} data={p.data} />)}
    </table>
  );
}

export function TranslatedInstance(p: { ins: TranslatedInstanceView, cultures: string[], currentCulture?: string | undefined, data: TranslatedInstanceViewType }) {

  const ins = p.ins;
  const forceUpdate = useForceUpdate();

  return (
    <React.Fragment key={ins.lite.id}>
      <thead>
        <tr>
          <th className="leftCell">{TranslationMessage.Instance.niceToString()}</th>
          <th className="titleCell"><EntityLink lite={ins.lite} /></th>
        </tr>
      </thead>
      <tbody>
        {
          Dic.getKeys(ins.master).map(entry => {
            var propertyRoute = entry.tryBefore(";") ?? entry;
            var propertyString = !entry.contains(";") ? entry : entry.before(";").replace("/", "[" + entry.after(";") + "].");

            var trans = ins.translations[entry];

            var isHtml = p.data.routes[propertyRoute] == "Html";

            return (
              <React.Fragment key={entry}>
                <tr>
                  <th className="leftCell">{TranslationMessage.Property.niceToString()}</th>
                  <th>{propertyString}</th>
                </tr>
                <tr>
                  <td className="leftCell"><em>{p.data.masterCulture}</em></td>
                  <td className="monospaceCell">
                    {isHtml ?
                      <pre>{ins.master[entry]}</pre> :
                      ins.master[entry]
                    }
                  </td>
                </tr>
                {p.cultures.map(c => {

                  var pair = trans && trans[c];

                  function handleChange(e: React.ChangeEvent<HTMLTextAreaElement>) {
                    if (!pair) {
                      if (!trans)
                        trans = ins.translations[entry] = {};

                      trans[c] = {
                        originalText: ins.master[entry],
                        newText: ins.master[entry],
                        translatedText: e.currentTarget.value
                      };
                    } else {
                      pair.translatedText = e.currentTarget.value;
                    }
                    forceUpdate();
                  }

                  return (
                    <React.Fragment key={c}>
                      {pair != null && pair.originalText != null && pair.newText != null &&
                        pair.originalText != pair.newText && <tr>
                        <td className="leftCell">{c} Diff</td>
                          <td className="monospaceCell">
                            <pre><DiffDocumentSimple first={pair.originalText} second={pair.newText} /></pre>
                        </td>
                      </tr>
                      }
                      <tr>
                        <td className="leftCell">{c}</td>
                        <td className="monospaceCell">
                          {p.currentCulture == null || p.currentCulture == c ?
                            <TextArea style={{ height: "24px", width: "90%" }} minHeight="24px" value={pair?.translatedText ?? ""}
                              onChange={handleChange}
                              onBlur={handleChange} /> :

                            pair && (
                              isHtml ?
                                <pre>{pair.translatedText}</pre> :
                                pair.translatedText)
                          }
                        </td>
                      </tr>
                    </React.Fragment>
                  );
                })}
              </React.Fragment>
            );

          })
        }
      </tbody>
    </React.Fragment>
    
    );
}
