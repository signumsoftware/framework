import * as React from 'react'
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome'
import { Dic, softCast } from '@framework/Globals'
import { Operations } from '@framework/Operations'
import { CultureClient } from '@framework/Basics/CultureClient'
import { TranslatedInstanceClient } from '../TranslatedInstanceClient'
import { TranslationMessage } from '../Signum.Translation'
import { useLocation, useParams } from "react-router";
import { Link} from "react-router-dom";
import "../Translation.css"
import { useAPI, useForceUpdate, useAPIWithReload, useLock } from '@framework/Hooks'
import { EntityLink } from '@framework/Search'
import { DiffDocumentSimple } from '../../Signum.DiffLog/Templates/DiffDocument'
import TextArea from '@framework/Components/TextArea'
import { KeyNames } from '@framework/Components'
import { useTitle } from '@framework/AppContext'
import { QueryString } from '@framework/QueryString'
import { getToString } from '@framework/Signum.Entities'
import { AccessibleTable } from '../../../Signum/React/Basics/AccessibleTable'

export default function TranslationInstanceView(): React.JSX.Element {
  const params = useParams() as { type: string; culture?: string; };
  const location = useLocation();

  const type = params.type;
  const culture = params.culture;

  const cultures = useAPI(() => CultureClient.getCultures(null), []);
  const [isLocked, lock] = useLock();


  const [onlyNeutral, setOnlyNeutral] = React.useState<boolean>(true);

  const [filter, setFilter] = React.useState<string | undefined>(() => QueryString.parse(location.search).filter);

  const [result, reloadResult] = useAPIWithReload(() => filter == undefined ? Promise.resolve(undefined) : TranslatedInstanceClient.API.viewTranslatedInstanceData(type, culture, filter), [type, culture, filter]);

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
        <TranslatedInstances data={result} currentCulture={params.culture} cultures={culture ? [culture] : otherCultures} />
        {result.instances.length > 0 && <input type="submit" value={TranslationMessage.Save.niceToString()} className="btn btn-primary mt-2" onClick={handleSave} disabled={isLocked} />}
      </div>
    );
  }

  function handleSave(e: React.FormEvent<any>) {
    e.preventDefault();
    const records = result!.instances.flatMap(ins => Dic.getKeys(ins.translations).flatMap(k => {
      const pr = k.tryBefore(";") ?? k;
      const rowId = k.tryAfter(";");
      const cultures = ins.translations[k];
      return Dic.getKeys(cultures).filter(c => culture == null || culture == c).map(c => softCast<TranslatedInstanceClient.TranslationRecord>({
        lite: ins.lite,
        propertyRoute: pr,
        rowId: rowId,
        culture: c,
        originalText: cultures[c].newText ?? cultures[c].originalText,
        translatedText: cultures[c].translatedText
      }));
    }));

    lock(() => TranslatedInstanceClient.API.saveTranslatedInstanceData(records, type, false, culture)
      .then(() => { reloadResult(); Operations.notifySuccess(); }));
  }

  const message = TranslationMessage.View0In1.niceToString(type,
    culture == undefined ? TranslationMessage.AllLanguages.niceToString() :
      cultures ? getToString(cultures[culture]) :
        culture);

  useTitle(message);

  return (
    <div>
      <div className="mb-2">
        <h2><Link to="/translatedInstance/status">{TranslationMessage.InstanceTranslations.niceToString()}</Link> {">"} {message}</h2>
        <TranslateSearchBox setFilter={setFilter} filter={filter ?? ""} />
        {culture == null && <label style={{ float: 'right' }}>
          <input type="checkbox" checked={onlyNeutral} onChange={e => setOnlyNeutral(e.currentTarget.checked)} /> {TranslationMessage.OnlyNeutralCultures.niceToString()}
        </label>
        }
        <em> {TranslationMessage.PressSearchForResults.niceToString()}</em>
      </div>
      {renderTable()}
    </div>
  );
}

export function TranslateSearchBox(p: { filter: string, setFilter: (newFilter: string) => void }): React.JSX.Element {

  const [tmpFilter, setTmpFilter] = React.useState(p.filter);

  function handleSearch(e: React.FormEvent<any>) {
    e.preventDefault();
    p.setFilter(tmpFilter);
  }

  function handleKeyDown(e: React.KeyboardEvent<any>) {
    if (e.key == KeyNames.enter) {
      e.preventDefault();
      p.setFilter(tmpFilter);
    }
  }

  return (
    <form onSubmit={handleSearch} className="input-group">
      <input type="text" className="form-control"
        placeholder={TranslationMessage.Search.niceToString()} value={tmpFilter} onChange={e => setTmpFilter(e.currentTarget.value)} onKeyDown={handleKeyDown} />
      <button className="btn btn-tertiary" type="submit" title={TranslationMessage.Search.niceToString()}>
        <FontAwesomeIcon aria-hidden="true" icon="magnifying-glass" />
      </button>
    </form>
  );
}

export function TranslatedInstances(p: { data: TranslatedInstanceClient.TranslatedInstanceViewType, cultures: string[], currentCulture?: string | undefined }): React.JSX.Element {


  return (
    <div id="results">
      {p.data.instances.map(ins => <TranslatedInstance ins={ins} cultures={p.cultures} currentCulture={p.currentCulture} data={p.data} />)}
    </div>
  );
}

export function TranslatedInstance(p: { ins: TranslatedInstanceClient.TranslatedInstanceView, cultures: string[], currentCulture?: string | undefined, data: TranslatedInstanceClient.TranslatedInstanceViewType }): React.JSX.Element {

  const ins = p.ins;
  const forceUpdate = useForceUpdate();

  return (
    <AccessibleTable
      aria-label={TranslationMessage.TranslationsOverview.niceToString()}
      className="table st"
      multiselectable={false}
      key={ins.lite.id}>
      <thead>
        <tr>
          <th className="leftCell">{TranslationMessage.Instance.niceToString()}</th>
          <th className="titleCell"><EntityLink lite={ins.lite} /></th>
        </tr>
      </thead>
      <tbody>
        {
          Dic.getKeys(ins.master).flatMap(entry => {
            const propertyRoute = entry.tryBefore(";") ?? entry;
            const propertyString = !entry.contains(";") ? entry : entry.before(";").replace("/", "[" + entry.after(";") + "].");

            let trans = ins.translations[entry];
            const isHtml = p.data.routes[propertyRoute] === "Html";

            const rows: React.ReactElement[] = [
              <tr key={`${entry}-header`}>
                <th className="leftCell">{TranslationMessage.Property.niceToString()}</th>
                <th>{propertyString}</th>
              </tr>,
              <tr key={`${entry}-master`}>
                <td className="leftCell"><em>{p.data.masterCulture}</em></td>
                <td className="monospaceCell">
                  {isHtml ? <pre>{ins.master[entry]}</pre> : ins.master[entry]}
                </td>
              </tr>
            ];

            p.cultures.forEach(c => {
              const pair = trans && trans[c];

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

              if (pair && pair.originalText != null && pair.newText != null && pair.originalText !== pair.newText) {
                rows.push(
                  <tr key={`${entry}-${c}-diff`}>
                    <td className="leftCell">{c} Diff</td>
                    <td className="monospaceCell">
                      <pre><DiffDocumentSimple first={pair.originalText} second={pair.newText} /></pre>
                    </td>
                  </tr>
                );
              }

              rows.push(
                <tr key={`${entry}-${c}`}>
                  <td className="leftCell">{c}</td>
                  <td className="monospaceCell">
                    {p.currentCulture == null || p.currentCulture === c ? (
                      <TextArea
                        style={{ height: "24px", width: "90%" }}
                        minHeight="24px"
                        value={pair?.translatedText ?? ""}
                        onChange={handleChange}
                        onBlur={handleChange}
                      />
                    ) : (
                      pair && (isHtml ? <pre>{pair.translatedText}</pre> : pair.translatedText)
                    )}
                  </td>
                </tr>
              );
            });

            return rows;
          })
        }
      </tbody>

    </AccessibleTable>
    );
}
