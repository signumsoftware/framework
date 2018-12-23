import * as React from 'react'
import { UserQueryEntity, UserQueryMessage, QueryOrderEmbedded, QueryColumnEmbedded } from '../Signum.Entities.UserQueries'
import { FormGroup, ValueLine, EntityLine, EntityTable } from '@framework/Lines'
import * as Finder from '@framework/Finder'
import { SubTokensOptions } from '@framework/FindOptions'
import { getQueryNiceName } from '@framework/Reflection'
import { TypeContext } from '@framework/TypeContext'
import QueryTokenEmbeddedBuilder from '../../UserAssets/Templates/QueryTokenEmbeddedBuilder'
import FilterBuilderEmbedded from '../../UserAssets/Templates/FilterBuilderEmbedded';

const CurrentEntityKey = "[CurrentEntity]";

export default class UserQuery extends React.Component<{ ctx: TypeContext<UserQueryEntity> }> {
  handleGroupResultsChanged = () => {
    var uq = this.props.ctx.value;
    if (!uq.groupResults) {
      uq.filters = [];
      uq.columns = [];
      uq.orders = [];
    }

    this.forceUpdate();
  }

  render() {
    const query = this.props.ctx.value.query;
    const ctx = this.props.ctx;
    const ctxxs = ctx.subCtx({ formSize: "ExtraSmall" });

    const canAggregate = ctx.value.groupResults ? SubTokensOptions.CanAggregate : 0;

    return (
      <div>
        <EntityLine ctx={ctx.subCtx(e => e.owner)} />
        <ValueLine ctx={ctx.subCtx(e => e.displayName)} />
        <FormGroup ctx={ctx.subCtx(e => e.query)}>
          {
            query && (
              Finder.isFindable(query.key, true) ?
                <a className="form-control-static" href={Finder.findOptionsPath({ queryName: query.key })}>{getQueryNiceName(query.key)}</a> :
                <span>{getQueryNiceName(query.key)}</span>)
          }
        </FormGroup>

        {query &&
          (<div>
            <EntityLine ctx={ctx.subCtx(e => e.entityType)} onChange={() => this.forceUpdate()} />
            {
              this.props.ctx.value.entityType &&
              <div>
                <ValueLine ctx={ctx.subCtx(e => e.hideQuickLink)} />
                <p className="messageEntity col-sm-offset-2">
                  {UserQueryMessage.Use0ToFilterCurrentEntity.niceToString(CurrentEntityKey)}
                </p>
              </div>
            }
            <ValueLine ctx={ctx.subCtx(e => e.appendFilters)} />
            <ValueLine ctx={ctx.subCtx(e => e.groupResults)} />
            <div>
              <FilterBuilderEmbedded ctx={ctxxs.subCtx(e => e.filters)}
                subTokenOptions={SubTokensOptions.CanAnyAll | SubTokensOptions.CanElement | canAggregate}
                queryKey={ctxxs.value.query!.key}
                showUserFilters={true} />
              <ValueLine ctx={ctxxs.subCtx(e => e.columnsMode)} />
              <EntityTable ctx={ctxxs.subCtx(e => e.columns)} columns={EntityTable.typedColumns<QueryColumnEmbedded>([
                {
                  property: a => a.token,
                  template: ctx => <QueryTokenEmbeddedBuilder
                    ctx={ctx.subCtx(a => a.token, { formGroupStyle: "SrOnly" })}
                    queryKey={this.props.ctx.value.query!.key}
                    subTokenOptions={SubTokensOptions.CanElement | canAggregate} />
                },
                { property: a => a.displayName }
              ])} />
              <EntityTable ctx={ctxxs.subCtx(e => e.orders)} columns={EntityTable.typedColumns<QueryOrderEmbedded>([
                {
                  property: a => a.token,
                  template: ctx => <QueryTokenEmbeddedBuilder
                    ctx={ctx.subCtx(a => a.token, { formGroupStyle: "SrOnly" })}
                    queryKey={this.props.ctx.value.query!.key}
                    subTokenOptions={SubTokensOptions.CanElement | canAggregate} />
                },
                { property: a => a.orderType }
              ])} />
            </div>
            <div className="row">
              <div className="col-sm-6">
                <ValueLine ctx={ctxxs.subCtx(e => e.paginationMode, { labelColumns: { sm: 4 } })} />
              </div>
              <div className="col-sm-6">
                <ValueLine ctx={ctxxs.subCtx(e => e.elementsPerPage, { labelColumns: { sm: 4 } })} />
              </div>
            </div>
          </div>)
        }
      </div>
    );
  }
}


