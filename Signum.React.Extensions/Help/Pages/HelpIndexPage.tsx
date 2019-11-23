import * as React from 'react'
import { RouteComponentProps, Link } from 'react-router-dom'
import * as numbro from 'numbro'
import * as Navigator from '@framework/Navigator'
import EntityLink from '@framework/SearchControl/EntityLink'
import { API, Urls } from '../HelpClient'
import { SearchControl } from '@framework/Search';
import { useAPI, useTitle } from '../../../../Framework/Signum.React/Scripts/Hooks';
import { HelpMessage, NamespaceHelpEntity, AppendixHelpEntity } from '../Signum.Entities.Help';
import { getTypeInfo } from '@framework/Reflection';
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome';


export default function HelpIndexPage(p: RouteComponentProps<{}>) {

  useTitle(HelpMessage.Help.niceToString());

  var index = useAPI(() => API.index(), []);

  return (
    <div id="entityContent">
      <h1 className="display-6">{HelpMessage.Help.niceToString()}</h1>
      {/*  <form id="form-search-big">
        <div class="input-group">
          <input type="text" class="form-control" placeholder="@HelpSearchMessage.Search.NiceToString()" name="q" />
          <div class="input-group-btn">
            <button class="btn btn-default" type="submit"><i class="glyphicon glyphicon-search"></i></button>
          </div>
        </div>
      </form>*/}

      {index && <div>
        <ul className="responsive-columns">
          {index.namespaces.map(nh =>
            <li className="mb-4" key={nh.namespace}>
              <h4 className="display-7">
                <Link to={Urls.namespaceUrl(nh.namespace)}>{nh.title}</Link>
                {nh.before && <small> {HelpMessage.In0.niceToString(nh.before)}</small>}
              </h4>
              <ul>
                {nh.allowedTypes.map(ei => <li key={ei.cleanName}><Link to={Urls.typeUrl(ei.cleanName)} >{getTypeInfo(ei.cleanName).niceName}</Link></li>)}
              </ul>
            </li>
          )}
        </ul>

        <h3 className="display-6">
          {HelpMessage.Appendices.niceToString()}
          {Navigator.isCreable(AppendixHelpEntity, true, true) && <Link to={Urls.appendixUrl(null)} style={{ fontSize: "20px" }}><FontAwesomeIcon icon="plus" className="ml-2" /></Link>}
        </h3>
        <ul className="responsive-columns">
          {index.appendices.map(ap => <li key={ap.uniqueName}><Link to={Urls.appendixUrl(ap.uniqueName)} >{ap.title}</Link></li>)}
        </ul>
      </div>
      }
    </div>
  );
}



