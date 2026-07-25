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
import "../../Signum.DiffLog/Templates/DiffLog.css"
import TextArea from '@framework/Components/TextArea'
import { KeyNames } from '@framework/Components'
import { useTitle } from '@framework/AppContext'
import { QueryString } from '@framework/QueryString'
import { getToString } from '@framework/Signum.Entities'
import { AccessibleRow, AccessibleTable } from '../../../Signum/React/Basics/AccessibleTable'
import { LinkButton } from '@framework/Basics/LinkButton'
import { TranslatedHtmlEditor, TranslatedHtmlViewer } from './TranslatedHtml'

export default function TranslationInstanceView(): React.JSX.Element {
  const params = useParams() as { type: string; culture?: string; };
  const location = useLocation();

  const type = params.type;
  const culture = params.culture;

  const cultures = useAPI(() => CultureClient.getCultures(null), []);
  const [isLocked, lock] = useLock();


  const [onlyNeutral, setOnlyNeutral] = React.useState<boolean>(true);

  const [applyFilter, setApplyFilter] = React.useState<boolean>(QueryString.parse(location.search).applyFilter != "false");

  const [filter, setFilter] = React.useState<string | undefined>(() => QueryString.parse(location.search).filter);

  const [result, reloadResult] = useAPIWithReload(() => filter == undefined ? Promise.resolve(undefined) : TranslatedInstanceClient.API.viewTranslatedInstanceData(type, culture, filter, applyFilter), [type, culture, filter, applyFilter]);

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
        <h1 className="h2"><Link to="/translatedInstance/status">{TranslationMessage.InstanceTranslations.niceToString()}</Link> {">"} {message}</h1>
        <TranslateSearchBox setFilter={setFilter} filter={filter ?? ""} />
        <label style={{ float: 'right' }} className="ms-3">
          <input type="checkbox" checked={applyFilter} onChange={e => setApplyFilter(e.currentTarget.checked)} /> {TranslationMessage.OnlyRecommendedInstances.niceToString()}
        </label>
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

  return (
    <AccessibleTable
      aria-label={TranslationMessage.TranslationsOverview.niceToString()}
      className="table st"
      mapCustomComponents={new Map<React.JSXElementConstructor<any>, string>([[TranslatedInstanceProperty, "tr"]])}
      multiselectable={false}
      key={ins.lite.id}>
      <thead>
        <tr>
          <th className="leftCell">{TranslationMessage.Instance.niceToString()}</th>
          <th className="titleCell"><EntityLink lite={ins.lite} /></th>
        </tr>
      </thead>
      <tbody>
        {Dic.getKeys(ins.master).map(entry =>
          <TranslatedInstanceProperty key={entry} entry={entry} ins={ins} cultures={p.cultures} currentCulture={p.currentCulture} data={p.data} />
        )}
      </tbody>

    </AccessibleTable>
    );
}

function TranslatedInstanceProperty(p: { entry: string, ins: TranslatedInstanceClient.TranslatedInstanceView, cultures: string[], currentCulture?: string | undefined, data: TranslatedInstanceClient.TranslatedInstanceViewType }): React.JSX.Element {

  const forceUpdate = useForceUpdate();
  const { entry, ins } = p;

  const propertyRoute = entry.tryBefore(";") ?? entry;
  const propertyString = !entry.contains(";") ? entry : entry.before(";").replace("/", "[" + entry.after(";") + "].");
  const isHtml = p.data.routes[propertyRoute] === "Html";

  const [rich, setRich] = React.useState(true);
  const showRich = isHtml && rich;

  const trans = ins.translations[entry];

  const rows: React.ReactElement[] = [
    <AccessibleRow key={`${entry}-header`}>
      <th className="leftCell">{TranslationMessage.Property.niceToString()}</th>
      <th>
        {propertyString}
        {isHtml && (
          <LinkButton className="ms-2 fw-normal" title={TranslationMessage.Edit.niceToString()} onClick={() => setRich(!rich)}>
            <FontAwesomeIcon aria-hidden={true} icon={showRich ? "code" : "align-left"} />
          </LinkButton>
        )}
      </th>
    </AccessibleRow>,
    <AccessibleRow key={`${entry}-master`}>
      <td className="leftCell"><em>{p.data.masterCulture}</em></td>
      <td className="monospaceCell">
        {showRich ? <TranslatedHtmlViewer text={ins.master[entry]} /> : (isHtml ? <pre className="translation-raw-html" style={{ whiteSpace: "pre-wrap" }}>{ins.master[entry]}</pre> : ins.master[entry])}
      </td>
    </AccessibleRow>
  ];

  p.cultures.forEach(c => {
    const pair = trans && trans[c];

    function handleChange(newValue: string) {
      let t = ins.translations[entry];
      if (!t)
        t = ins.translations[entry] = {};

      if (!t[c])
        t[c] = { originalText: ins.master[entry], newText: ins.master[entry], translatedText: newValue };
      else
        t[c].translatedText = newValue;

      forceUpdate();
    }

    if (pair && pair.originalText != null && pair.newText != null && pair.originalText !== pair.newText) {
      rows.push(
        <AccessibleRow key={`${entry}-${c}-diff`}>
          <td className="leftCell">{c} Diff</td>
          <td className="monospaceCell">
            <pre><DiffDocumentSimple first={pair.originalText} second={pair.newText} /></pre>
          </td>
        </AccessibleRow>
      );
    }

    const editable = p.currentCulture == null || p.currentCulture === c;

    rows.push(
      <AccessibleRow key={`${entry}-${c}`}>
        <td className="leftCell">{c}</td>
        <td className="monospaceCell">
          {editable ? (
            showRich ? <TranslatedHtmlEditor text={pair?.translatedText ?? ""} onChange={handleChange} /> : (
              <TextArea
                className={isHtml ? "translation-raw-html" : undefined}
                style={{ height: "24px", width: "90%" }}
                minHeight="24px"
                autoResize={true}
                value={pair?.translatedText ?? ""}
                onChange={e => handleChange(e.currentTarget.value)}
                onBlur={e => handleChange(e.currentTarget.value)}
              />
            )
          ) : (
            pair && (showRich ? <TranslatedHtmlViewer text={pair.translatedText} /> : (isHtml ? <pre className="translation-raw-html" style={{ whiteSpace: "pre-wrap" }}>{pair.translatedText}</pre> : pair.translatedText))
          )}
        </td>
      </AccessibleRow>
    );
  });

  return <>{rows}</>;
}
