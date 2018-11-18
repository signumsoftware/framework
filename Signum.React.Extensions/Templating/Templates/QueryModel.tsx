import * as React from 'react'
import { SearchControl } from '@framework/Search'
import { TypeContext } from '@framework/TypeContext'
import { QueryModelMessage, QueryModel } from '../../Templating/Signum.Entities.Templating'

interface QueryModelComponentProps {
  ctx: TypeContext<QueryModel>
}

export default class QueryModelComponent extends React.Component<QueryModelComponentProps> {
  handleOnSearch = () => {
    const qr = this.searchControl.searchControlLoaded!.getQueryRequest();
    const model = this.props.ctx.value;
    model.filters = qr.filters;
    model.orders = qr.orders;
    model.pagination = qr.pagination;
    model.modified = true;
  }

  searchControl!: SearchControl;
  render() {
    const ctx = this.props.ctx;
    return (
      <div>
        <p>{QueryModelMessage.ConfigureYourQueryAndPressSearchBeforeOk.niceToString()}</p>
        <SearchControl ref={sc => this.searchControl = sc!}
          hideButtonBar={true}
          showContextMenu="Basic"
          allowSelection={false}
          findOptions={{ queryName: ctx.value.queryKey }}
          onSearch={this.handleOnSearch} />
      </div>
    );
  }
}
