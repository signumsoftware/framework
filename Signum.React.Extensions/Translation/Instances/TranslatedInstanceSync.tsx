import * as React from 'react'
import { Link } from 'react-router-dom'
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome'
import { Dic, softCast } from '@framework/Globals'
import { notifySuccess } from '@framework/Operations'
import * as CultureClient from '../CultureClient'
import { API, PropertyRouteConflic, TypeInstancesChanges, TranslationRecord, PropertyChange } from '../TranslatedInstanceClient'
import { TranslationMessage } from '../Signum.Entities.Translation'
import { RouteComponentProps } from "react-router";
import "../Translation.css"
import { useAPI, useForceUpdate, useAPIWithReload, useLock } from '@framework/Hooks'
import { EntityLink } from '@framework/Search'
import { DiffDocumentSimple } from '../../DiffLog/Templates/DiffDocument'
import TextArea from '@framework/Components/TextArea'
import { KeyCodes } from '@framework/Components'
import { getTypeInfo } from '@framework/Reflection'
import { useTitle } from '@framework/AppContext'
import { CultureInfoEntity } from '../../Basics/Signum.Entities.Basics'
import { TranslationMember, initialElementIf } from '../Code/TranslationTypeTable'
import { getToString, Lite } from '@framework/Signum.Entities'



export default function TranslatedInstanceSync(p: RouteComponentProps<{ type: string; culture: string; }>) {

  const type = p.match.params.type;
  const culture = p.match.params.culture;

  const cultures = useAPI(() => CultureClient.getCultures(null), []);
  const [isLocked, lock] = useLock();

  const [result, reloadResult] = useAPIWithReload(() => API.syncTranslatedInstance(type, culture), [type, culture]);

  function renderTable() {
    if (result == undefined || cultures == undefined)
      return undefined;

    if (Dic.getKeys(result).length == 0)
      return <strong> {TranslationMessage.NoResultsFound.niceToString()}</strong>;

    return (
      <div>
        <TranslatedInstances data={result} currentCulture={p.match.params.culture} cultures={cultures} />
        <input type="submit" value={TranslationMessage.Save.niceToString()} className="btn btn-primary mt-2" onClick={handleSave} disabled={isLocked} />
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

      return softCast<TranslationRecord>({
        lite: ins.instance,
        propertyRoute: pr,
        rowId: rowId,
        culture: culture,
        originalText: propChange.support[result!.masterCulture].original,
        translatedText: propChange.translatedText!
      });
    }).notNull());

    lock(() => API.saveTranslatedInstanceData(records, type, true, culture)
      .then(() => { reloadResult(); notifySuccess(); }));
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
          <h2> {TranslationMessage._0AlreadySynchronized.niceToString(getTypeInfo(type).niceName)}</h2>
        </div>
        {deletedTranslations}
        {result && result.totalInstances == 0 && <Link to={`~/translatedInstance/status`}>
          {TranslationMessage.BackToTranslationStatus.niceToString()}
        </Link>}
        
      </div>
    );
  }

  return (
    <div>
      <div className="mb-2">
        <h2><Link to="~/translatedInstance/status">{TranslationMessage.InstanceTranslations.niceToString()}</Link> {">"} {message}</h2>
      </div>
      {deletedTranslations}
      {result && result.totalInstances > 0 && renderTable()}
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

export function TranslatedInstances(p: { data: TypeInstancesChanges, cultures: { [culture: string]: Lite<CultureInfoEntity> }, currentCulture: string }) {

  return (
    <table id="results" style={{ width: "100%", margin: "0px" }} className="st">
      {p.data.instances.map(ins =>
        <React.Fragment key={ins.instance.id}>
          <thead>
            <tr>
              <th className="leftCell">{TranslationMessage.Instance.niceToString()}</th>
              <th className="titleCell"><EntityLink lite={ins.instance} /></th>
            </tr>
          </thead>
          <tbody>
            {
              Dic.getKeys(ins.routeConflicts).map(routeAndRowId => {
                var propertyRoute = routeAndRowId.tryBefore(";") ?? routeAndRowId;
                var propertyString = !routeAndRowId.contains(";") ? routeAndRowId : routeAndRowId.before(";").replace("/", "[" + routeAndRowId.after(";") + "].");

                var propChange = ins.routeConflicts[routeAndRowId];

                var isHtml = p.data.routes[propertyRoute] == "Html";


                return (
                  <React.Fragment key={routeAndRowId}>
                    <tr>
                      <th className="leftCell">{TranslationMessage.Property.niceToString()}</th>
                      <th>{propertyString}</th>
                    </tr>
                    {Dic.getKeys(propChange.support).map(c => {
                      var rc = propChange.support[c];
                      return (
                        <tr key={c}>
                          <td className="leftCell">{c}</td>
                          <td className="monospaceCell">
                            {rc.oldOriginal == null || rc.original == null || rc.oldOriginal == rc.original ?
                              <pre className="mb-0">{rc.original}</pre> :
                              <DiffDocumentSimple first={rc.oldOriginal} second={rc.original} />
                            }
                          </td>
                        </tr>
                      );
                    })}
                    <tr>
                      <td className="leftCell">{p.currentCulture}</td>
                      <td className="monospaceCell">
                        <TranslationProperty property={propChange} />
                      </td>
                    </tr>
                  </React.Fragment>
                );
              })
            }
          </tbody>
        </React.Fragment>
      )}
    </table>
  );
}


export function TranslationProperty({ property }: { property: PropertyChange }) {

  const [avoidCombo, setAvoidCombo] = React.useState(false);
  const forceUpdate = useForceUpdate();

  const handleOnTextArea = React.useCallback(function (ta: HTMLTextAreaElement | null) {
    ta && avoidCombo && ta.focus();
  }, [avoidCombo]);


  function handleOnChange(e: React.FormEvent<any>) {
    property.translatedText = TranslationMember.normalizeString((e.currentTarget as HTMLSelectElement).value)!;
    forceUpdate();
  }

  function handleAvoidCombo(e: React.FormEvent<any>) {
    e.preventDefault();
    setAvoidCombo(true);
  }

  function handleKeyDown(e: React.KeyboardEvent<any>) {
    if (e.keyCode == 32 || e.keyCode == 113) { //SPACE OR F2
      e.preventDefault();
      setAvoidCombo(true);
    }
  }

  var translations = Object.entries(property.support)
    .flatMap(([c, rc]) => rc.automaticTranslations.map(at => ({ culture: c, text: at.text, translatorName: at.translatorName }))
      .concat(rc.oldTranslation ? [{ culture: c, text: rc.oldTranslation, translatorName: "Previous translation" }] : [])
  );

  if (translations.length == 0 || avoidCombo)
    return (
      <TextArea style={{ height: "24px", width: "90%" }} minHeight="24px" value={property.translatedText ?? ""}
        onChange={e => { property.translatedText = e.currentTarget.value; forceUpdate(); }}
        onBlur={handleOnChange}
        innerRef={handleOnTextArea} />
    );

  return (
    <span>
      <select value={property.translatedText ?? ""} onChange={handleOnChange} onKeyDown={handleKeyDown}>
        {initialElementIf(property.translatedText == undefined).concat(
          translations.map(a => <option key={a.culture + a.translatorName} title={`from '${a.culture}' using ${a.translatorName}`} value={a.text}>{a.text}</option>))}
      </select>
        &nbsp;
      <a href="#" onClick={handleAvoidCombo}>{TranslationMessage.Edit.niceToString()}</a>
    </span>
  );
}
