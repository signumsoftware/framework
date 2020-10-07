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
import { EntityLink } from '../../../../Framework/Signum.React/Scripts/Search'
import { DiffDocumentSimple } from '../../DiffLog/Templates/DiffDocument'
import TextArea from '../../../../Framework/Signum.React/Scripts/Components/TextArea'
import { KeyCodes } from '../../../../Framework/Signum.React/Scripts/Components'
import { getTypeInfo } from '@framework/Reflection'
import { useTitle } from '../../../../Framework/Signum.React/Scripts/AppContext'
import { CultureInfoEntity } from '../../Basics/Signum.Entities.Basics'
import { TranslationMember, initialElementIf } from '../Code/TranslationTypeTable'
import { Lite } from '@framework/Signum.Entities'



export default function TranslatedInstanceSync(p: RouteComponentProps<{ type: string; culture: string; }>) {

  const type = p.match.params.type;
  const culture = p.match.params.culture;

  const cultures = useAPI(() => CultureClient.getCultures(true), []);
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

    lock(() => API.saveTranslatedInstanceData(records, type, culture)
      .then(() => { reloadResult(); notifySuccess(); }))
      .done();
  }

  const message = result && result.totalInstances == 0 ? TranslationMessage._0AlreadySynchronized.niceToString(getTypeInfo(type).niceName) :
    TranslationMessage.Synchronize0In1.niceToString(getTypeInfo(type).niceName, cultures ? cultures[culture].toStr : culture) +
    (result && result.instances.length < result.totalInstances ? " [{0}/{1}]".formatWith(result.instances.length, result.totalInstances) : "");

  useTitle(message);

  return (
    <div>
      <div className="mb-2">
        <h2>{message}</h2>
      </div>
      {result && result.totalInstances > 0 && renderTable()}
      {result && result.totalInstances == 0 && <Link to={`~/translatedInstance/status`}>
        {TranslationMessage.BackToTranslationStatus.niceToString()}
      </Link>}
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
      <div className="input-group-append">
        <button className="btn btn-outline-secondary" type="submit" title={TranslationMessage.Search.niceToString()}>
          <FontAwesomeIcon icon="search" />
        </button>
      </div>
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
                            {rc.diff ? <DiffDocumentSimple diff={rc.diff} /> : <pre>{rc.original}</pre>}
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
    property.translatedText = TranslationMember.normalizeString((e.currentTarget as HTMLSelectElement).value);
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

  if (Dic.getKeys(property.support).length == 0 || avoidCombo)
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
          Object.entries(property.support).map(([c, rc]) => <option key={c} value={rc.automaticTranslation}>{rc.automaticTranslation}</option>))}
      </select>
        &nbsp;
      <a href="#" onClick={handleAvoidCombo}>{TranslationMessage.Edit.niceToString()}</a>
    </span>
  );
}
