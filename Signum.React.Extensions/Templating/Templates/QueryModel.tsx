import * as React from 'react'
import { SearchControl } from '@framework/Search'
import { TypeContext } from '@framework/TypeContext'
import { QueryModelMessage, QueryModel } from '../../Templating/Signum.Entities.Templating'
import { SearchControlHandler } from '../../../../Framework/Signum.React/Scripts/SearchControl/SearchControl';

interface QueryModelComponentProps {
  ctx: TypeContext<QueryModel>
}

export default function QueryModelComponent(p : QueryModelComponentProps){
  function handleOnSearch() {
    const qr = searchControl.current!.searchControlLoaded!.getQueryRequest();
    const model = p.ctx.value;
    model.filters = qr.filters;
    model.orders = qr.orders;
    model.pagination = qr.pagination;
    model.modified = true;
  }

  var searchControl = React.useRef<SearchControlHandler>(null);
  const ctx = p.ctx;
  return (
    <div>
      <p>{QueryModelMessage.ConfigureYourQueryAndPressSearchBeforeOk.niceToString()}</p>
      <SearchControl ref={searchControl}
        hideButtonBar={true}
        showContextMenu={fo => "Basic"}
        allowSelection={false}
        findOptions={{ queryName: ctx.value.queryKey }}
        onSearch={handleOnSearch} />
    </div>
  );
}
