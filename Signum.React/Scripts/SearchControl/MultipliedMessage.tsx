
import * as React from 'react'
import { Dic } from '../Globals'
import { FindOptionsParsed, QueryToken, getTokenParents, isFilterGroupOptionParsed } from '../FindOptions'
import { ValidationMessage, External } from '../Signum.Entities'
import { tryGetTypeInfos, TypeReference, getTypeInfos } from '../Reflection'
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome';
import { FilterOptionParsed } from '../Search';

export default function MultipliedMessage(p: { findOptions: FindOptionsParsed, mainType: TypeReference }) {
  const fops = p.findOptions;

  function getFilterTokens(fop: FilterOptionParsed): (QueryToken | undefined)[] {
    if (isFilterGroupOptionParsed(fop))
      return [fop.token, ...fop.filters.flatMap(f => getFilterTokens(f))];
    else
      return [fop.operation == undefined ? undefined : fop.token]
  }

  const tokensObj = fops.columnOptions.map(a => a.token)
    .concat(fops.filterOptions.flatMap(fo => getFilterTokens(fo)))
    .concat(fops.orderOptions.map(a => a.token))
    .filter(a => a != undefined)
    .flatMap(a => {
      var parts = getTokenParents(a); 

      var toArrayIndex = parts.findIndex(a => a.queryTokenType == "ToArray");
      if (toArrayIndex == -1)
        return parts;

      return parts.slice(0, toArrayIndex);
    })
    .filter(a => a.queryTokenType == "Element")
    .toObjectDistinct(a => a.fullKey);

  const tokens = Dic.getValues(tokensObj);

  if (tokens.length == 0)
    return null;

  const message = ValidationMessage.TheNumberOf0IsBeingMultipliedBy1.niceToString().formatHtml(
    getTypeInfos(p.mainType).map(a => a.nicePluralName).joinComma(External.CollectionMessage.And.niceToString()),
    tokens.map(a => <strong>{a.parent!.niceName}</strong>).joinCommaHtml(External.CollectionMessage.And.niceToString()))

  return (
    <div className="sf-search-message alert alert-warning">
      <FontAwesomeIcon icon="triangle-exclamation" />&nbsp;{message}
    </div>
  );
}



