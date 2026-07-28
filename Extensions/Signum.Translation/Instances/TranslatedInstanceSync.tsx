import * as React from 'react'
import { Link } from 'react-router-dom'
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome'
import { Dic, softCast } from '@framework/Globals'
import { Operations } from '@framework/Operations'
import { CultureClient } from '@framework/Basics/CultureClient'
import { TranslatedInstanceClient } from '../TranslatedInstanceClient'
import { TranslationMessage } from '../Signum.Translation'
import { useParams, useLocation } from "react-router";
import "../Translation.css"
import { useAPI, useForceUpdate, useAPIWithReload, useLock } from '@framework/Hooks'
import { EntityLink } from '@framework/Search'
import { DiffDocumentSimple } from '../../Signum.DiffLog/Templates/DiffDocument'
import TextArea from '@framework/Components/TextArea'
import { KeyNames } from '@framework/Components'
import "../../Signum.DiffLog/Templates/DiffLog.css"
import { getTypeInfo } from '@framework/Reflection'
import { useTitle } from '@framework/AppContext'
import { TranslationMember, initialElementIf } from '../Code/TranslationTypeTable'
import { getToString, Lite } from '@framework/Signum.Entities'
import { CultureInfoEntity } from '@framework/Signum.Basics'
import { AccessibleRow, AccessibleTable } from '../../../Signum/React/Basics/AccessibleTable'
import { LinkButton } from '@framework/Basics/LinkButton'
import { TranslatedHtmlEditor, TranslatedHtmlViewer } from './TranslatedHtml'

export default function TranslatedInstanceSync(): React.JSX.Element {
  const params = useParams() as { type: string; culture: string; };

  const type = params.type;
  const culture = params.culture;

  const location = useLocation();
  const cultures = useAPI(() => CultureClient.getCultures(null), []);
  const [isLocked, lock] = useLock();
  const [applyFilter, setApplyFilter] = React.useState<boolean>(new URLSearchParams(location.search).get("applyFilter") != "false");

  const [result, reloadResult] = useAPIWithReload(() => TranslatedInstanceClient.API.syncTranslatedInstance(type, culture, applyFilter), [type, culture, applyFilter]);

  const filterToggle = (
    <label className="d-block mb-2 text-end">
      <input type="checkbox" checked={applyFilter} onChange={e => setApplyFilter(e.currentTarget.checked)} /> {TranslationMessage.OnlyRecommendedInstances.niceToString()}
    </label>
  );

  function renderTable() {
    if (result == undefined || cultures == undefined)
      return undefined;

    if (Dic.getKeys(result).length == 0)
      return <strong> {TranslationMessage.NoResultsFound.niceToString()}</strong>;

    return (
      <div>
        <TranslatedInstances data={result} currentCulture={params.culture} cultures={cultures} />
        <input type="submit" aria-label={TranslationMessage.Save.niceToString()} value={TranslationMessage.Save.niceToString()} className="btn btn-primary mt-2" onClick={handleSave} disabled={isLocked} />
      </div>
    );
  }

  function handleSave(e: React.FormEvent<any>) {
    e.preventDefault();
    const records = result!.instances.flatMap(ins => Dic.getKeys(ins.routeConflicts).map(k => {
      const pr = k.tryBefore(";") ?? k;
      const rowId = k.tryAfter(";");
      const propChange = ins.routeConflicts[k];
      if (propChange.translatedText == null)
        return;

      return softCast<TranslatedInstanceClient.TranslationRecord>({
        lite: ins.instance,
        propertyRoute: pr,
        rowId: rowId,
        culture: culture,
        originalText: propChange.support[result!.masterCulture].original,
        translatedText: propChange.translatedText!
      });
    }).notNull());

    lock(() => TranslatedInstanceClient.API.saveTranslatedInstanceData(records, type, true, culture)
      .then(() => { reloadResult(); Operations.notifySuccess(); }));
  }

  const message = result && result.totalInstances == 0 ? TranslationMessage._0AlreadySynchronized.niceToString(getTypeInfo(type).niceName) :
    TranslationMessage.Synchronize0In1.niceToString(getTypeInfo(type).niceName, cultures ? getToString(cultures[culture]) : culture) +
    (result && result.instances.length < result.totalInstances ? " [{0}/{1}]".formatWith(result.instances.length, result.totalInstances) : "");

  useTitle(message);

  var deletedTranslations = result?.deletedTranslations ? <p className="text-warning">{TranslationMessage._0OutdatedTranslationsFor1HaveBeenDeleted.niceToString(result?.deletedTranslations, getTypeInfo(type).niceName)}</p> : null;

  if (result && result.totalInstances == 0) {
    return (
      <div>
        <div className="mb-2">
          <h1 className="h2"> {TranslationMessage._0AlreadySynchronized.niceToString(getTypeInfo(type).niceName)}</h1>
        </div>
        {filterToggle}
        {deletedTranslations}
        {result && result.totalInstances == 0 && <Link to={`/translatedInstance/status`}>
          {TranslationMessage.BackToTranslationStatus.niceToString()}
        </Link>}

      </div>
    );
  }

  return (
    <div>
      <div className="mb-2">
        <h1 className="h2"><Link to="/translatedInstance/status">{TranslationMessage.InstanceTranslations.niceToString()}</Link> {">"} {message}</h1>
      </div>
      {filterToggle}
      {deletedTranslations}
      {result && result.totalInstances > 0 && renderTable()}
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
        aria-label={TranslationMessage.Search.niceToString()} placeholder={TranslationMessage.Search.niceToString()} value={tmpFilter} onChange={e => setTmpFilter(e.currentTarget.value)} onKeyDown={handleKeyDown} />
      <button className="btn btn-tertiary" type="submit" title={TranslationMessage.Search.niceToString()}>
        <FontAwesomeIcon role="img" icon="magnifying-glass" />
      </button>
    </form>
  );
}

export function TranslatedInstances(p: { data: TranslatedInstanceClient.TypeInstancesChanges, cultures: { [culture: string]: Lite<CultureInfoEntity> }, currentCulture: string }): React.JSX.Element {

  return (
    <>
      {p.data.instances.map(ins =>
        <React.Fragment key={ins.instance.id}>
          <AccessibleTable
            aria-label={TranslationMessage.TranslationsOverview.niceToString()}
            className="table st"
            mapCustomComponents={new Map<React.JSXElementConstructor<any>, string>([[SyncPropertyRows, "tr"], [AccessibleRow, "tr"]])}
            multiselectable={false}
            id="results"
            style={{ width: "100%", margin: "0px" }}>
            <thead>
              <tr>
                <th className="leftCell">{TranslationMessage.Instance.niceToString()}</th>
                <th className="titleCell"><EntityLink lite={ins.instance} /></th>
              </tr>
            </thead>
            <tbody>
              {Dic.getKeys(ins.routeConflicts).map(routeAndRowId =>
                <SyncPropertyRows key={routeAndRowId} routeAndRowId={routeAndRowId} conflict={ins.routeConflicts[routeAndRowId]} data={p.data} currentCulture={p.currentCulture} />
              )}
            </tbody>
          </AccessibleTable>
        </React.Fragment>
      )}
    </>
  );
}

function SyncPropertyRows(p: { routeAndRowId: string, conflict: TranslatedInstanceClient.PropertyChange, data: TranslatedInstanceClient.TypeInstancesChanges, currentCulture: string }): React.JSX.Element {

  const { routeAndRowId, conflict } = p;

  const propertyRoute = routeAndRowId.tryBefore(";") ?? routeAndRowId;
  const propertyString = !routeAndRowId.contains(";") ? routeAndRowId : routeAndRowId.before(";").replace("/", "[" + routeAndRowId.after(";") + "].");
  const isHtml = p.data.routes[propertyRoute] === "Html";

  const [richView, setRichView] = React.useState(true);
  const [richEdit, setRichEdit] = React.useState(true);
  const [avoidCombo, setAvoidCombo] = React.useState(false);

  // The automatic-translations combo takes priority; the edit toggle is hidden while it is visible.
  const canShowSelect = Dic.getKeys(conflict.support).some(c => conflict.support[c].automaticTranslations.length > 0 || conflict.support[c].oldTranslation != null);
  const comboVisible = canShowSelect && !avoidCombo;

  function toggleButton(value: boolean, setValue: (v: boolean) => void) {
    return (
      <LinkButton className="me-1 fw-normal" title={value ? "show code" : "show preview"} onClick={() => setValue(!value)}>
        <FontAwesomeIcon role="img" icon={value ? "code" : "eye"} />
      </LinkButton>
    );
  }

  const rows: React.ReactElement[] = [
    <AccessibleRow key={routeAndRowId + "-main"}>
      <th className="leftCell">{TranslationMessage.Property.niceToString()}</th>
      <th>{propertyString}</th>
    </AccessibleRow>
  ];

  Dic.getKeys(conflict.support).forEach(c => {
    const rc = conflict.support[c];
    const showDiff = !(rc.oldOriginal == null || rc.original == null || rc.oldOriginal == rc.original);
    const showRich = isHtml && richView && !showDiff;
    rows.push(
      <AccessibleRow key={routeAndRowId + "-" + c}>
        <td className="leftCell">
          {isHtml && !showDiff && toggleButton(richView, setRichView)}
          {c}
        </td>
        <td className="monospaceCell">
          {showDiff
            ? <DiffDocumentSimple first={rc.oldOriginal!} second={rc.original} />
            : (showRich ? <TranslatedHtmlViewer text={rc.original} /> : <pre className={isHtml ? "mb-0 translation-raw-html" : "mb-0"} style={{ whiteSpace: "pre-wrap" }}>{rc.original}</pre>)
          }
        </td>
      </AccessibleRow>
    );
  });

  rows.push(
    <AccessibleRow key={routeAndRowId + "-translation"}>
      <td className="leftCell">
        {isHtml && !comboVisible && toggleButton(richEdit, setRichEdit)}
        {p.currentCulture}
      </td>
      <td className="monospaceCell">
        <TranslationProperty property={conflict} isHtml={isHtml} richEdit={richEdit} avoidCombo={avoidCombo} setAvoidCombo={setAvoidCombo} />
      </td>
    </AccessibleRow>
  );

  return <>{rows}</>;
}


export function TranslationProperty({ property, isHtml, richEdit, avoidCombo, setAvoidCombo }: { property: TranslatedInstanceClient.PropertyChange, isHtml?: boolean, richEdit?: boolean, avoidCombo?: boolean, setAvoidCombo?: (v: boolean) => void }): React.JSX.Element {

  const forceUpdate = useForceUpdate();

  const handleOnTextArea = React.useCallback(function (ta: HTMLTextAreaElement | null) {
    ta && avoidCombo && ta.focus();
  }, [avoidCombo]);


  function handleOnChange(e: React.FormEvent<any>) {
    property.translatedText = TranslationMember.normalizeString((e.currentTarget as HTMLSelectElement).value)!;
    forceUpdate();
  }

  function handleAvoidCombo(e: React.FormEvent<any>) {
    setAvoidCombo?.(true);
  }

  function handleKeyDown(e: React.KeyboardEvent<any>) {
    if (e.key == KeyNames.space || e.key == "F2") {
      e.preventDefault();
      setAvoidCombo?.(true);
    }
  }

  var translations = Object.entries(property.support)
    .flatMap(([c, rc]) => rc.automaticTranslations.map(at => ({ culture: c, text: at.text, translatorName: at.translatorName }))
      .concat(rc.oldTranslation ? [{ culture: c, text: rc.oldTranslation, translatorName: "Previous translation" }] : [])
    );
  return (translations.length === 0 || avoidCombo) ? (
    isHtml && richEdit ? (
      <TranslatedHtmlEditor text={property.translatedText} onChange={v => { property.translatedText = v; forceUpdate(); }} />
    ) : <>
      <label className="sr-only" htmlFor={`translatedText-${property.translatedText}`}> {TranslationMessage.Description.niceToString()}</label>
      <TextArea
        id={`translatedText-${property.translatedText}`}
        aria-label={TranslationMessage.Description.niceToString()}
        className={isHtml ? "translation-raw-html" : undefined}
        style={{ height: "24px", width: "90%" }}
        minHeight="24px"
        autoResize={true}
        value={property.translatedText ?? ""}
        onChange={e => { property.translatedText = e.currentTarget.value; forceUpdate(); }}
        onBlur={handleOnChange}
        innerRef={handleOnTextArea}
      />
    </>
  )
    :
    <span>
      <label className="sr-only" htmlFor={`translationSelect-${property.translatedText}`}>
        {TranslationMessage.Description.niceToString()}
      </label>
      <select
        id={`translationSelect-${property.translatedText}`}
        aria-label={TranslationMessage.Description.niceToString()}
        style={{ maxWidth: "70vw" }}
        value={property.translatedText ?? ""}
        onChange={handleOnChange}
        onKeyDown={handleKeyDown}>
        {initialElementIf(property.translatedText == undefined).concat(
          translations.map(a =>
            <option key={a.culture + a.translatorName} title={TranslationMessage.From0using1_.niceToString(a.culture, a.translatorName)} value={a.text}>
              {a.text}
            </option>
          )
        )}
      </select>
      &nbsp;
      <LinkButton title={undefined} onClick={handleAvoidCombo} aria-label={TranslationMessage.Edit.niceToString()}>{TranslationMessage.Edit.niceToString()}</LinkButton>
    </span>
}
