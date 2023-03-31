
import * as React from 'react'
import { Dic } from '../Globals'
import { FindOptionsParsed } from '../FindOptions'
import { TypeReference } from '../Reflection'
import { ValidationMessage } from '../Signum.Entities.Validation';
import { CollectionMessage } from '../Signum.External';

export default function GroupByMessage(p: { findOptions: FindOptionsParsed, mainType: TypeReference }) {
  const fo = p.findOptions;

  const tokensObj = fo.columnOptions.map(a => a.token)
    .concat(fo.orderOptions.map(a => a.token))
    .filter(a => a != undefined && a.queryTokenType != "Aggregate")
    .toObjectDistinct(a => a!.fullKey, a => a!);

  const tokens = Dic.getValues(tokensObj);

  const message = ValidationMessage.TheRowsAreBeingGroupedBy0.niceToString().formatHtml(
    tokens.map(a => <strong>{a.niceName}</strong>).joinCommaHtml(CollectionMessage.And.niceToString()));
  return (
    <div className="sf-search-message alert alert-info">
      {"Ʃ"}&nbsp;{message}
    </div>
  );
}



