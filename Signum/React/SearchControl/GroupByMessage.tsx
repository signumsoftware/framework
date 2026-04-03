
import * as React from 'react'
import { Dic } from '../Globals'
import { FindOptionsParsed } from '../FindOptions'
import { TypeReference, getQueryNiceName } from '../Reflection'
import { ValidationMessage } from '../Signum.Entities.Validation';
import { CollectionMessage } from '../Signum.External';
import { VisualTipIcon } from '../Basics/VisualTipIcon';
import { SearchVisualTip } from '../Signum.Basics';
import { GroupHelp } from './SearchControlVisualTips';

export default function GroupByMessage(p: { findOptions: FindOptionsParsed, mainType: TypeReference}): React.ReactElement {
  const fo = p.findOptions;

  const tokensObj = fo.columnOptions.map(a => a.token)
    .concat(fo.orderOptions.map(a => a.token))
    .filter(a => a != undefined && a.queryTokenType != "Aggregate")
    .toObjectDistinct(a => a!.fullKey, a => a!);

  const tokens = Dic.getValues(tokensObj);

  const message = ValidationMessage.EachRowRepresentsAGroupOf0WithSame1.niceToString().formatHtml(getQueryNiceName(fo.queryKey),
    tokens.map(a => <strong>{a.niceName}</strong>).joinCommaHtml(CollectionMessage.And.niceToString()));
  return (
    <div className="sf-search-message alert alert-info">
      {"Ʃ"}&nbsp;{message}
      <VisualTipIcon visualTip={SearchVisualTip.GroupHelp} content={props => <GroupHelp injected={props } />} /> 
    </div>
  );
}



