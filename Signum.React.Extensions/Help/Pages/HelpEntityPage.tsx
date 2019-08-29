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


export default function HelpEntityPage(p: RouteComponentProps<{}>) {

  useTitle(HelpMessage.Help.niceToString());

  var index = useAPI(undefined, [], () => API.index());

  return (
    <div id="entityContent">
      <h1 className="centered">{HelpMessage.Help.niceToString()}</h1>
      {/*  <form id="form-search-big">
        <div class="input-group">
          <input type="text" class="form-control" placeholder="@HelpSearchMessage.Search.NiceToString()" name="q" />
          <div class="input-group-btn">
            <button class="btn btn-default" type="submit"><i class="glyphicon glyphicon-search"></i></button>
          </div>
        </div>
      </form>*/}
    </div>
  );
}



