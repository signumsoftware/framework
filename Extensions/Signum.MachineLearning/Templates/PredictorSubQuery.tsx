import * as React from 'react'
import { AutoLine, EntityLine, EntityTable, TextBoxLine } from '@framework/Lines'
import { FindOptions, ColumnOption } from '@framework/Search'
import { TypeContext } from '@framework/TypeContext'
import { PredictorSubQueryEntity, PredictorSubQueryColumnEmbedded, PredictorEntity, PredictorMainQueryEmbedded, PredictorMessage, PredictorSubQueryColumnUsage } from '../Signum.MachineLearning'
import { Finder } from '@framework/Finder'
import QueryTokenEmbeddedBuilder from '../../Signum.UserAssets/Templates/QueryTokenEmbeddedBuilder'
import FilterBuilderEmbedded from '../../Signum.UserAssets/Templates/FilterBuilderEmbedded';
import { UserAssetClient } from '../../Signum.UserAssets/UserAssetClient'
import { QueryDescription, SubTokensOptions } from '@framework/FindOptions'
import { initializeColumn } from './Predictor';
import { newMListElement } from '@framework/Signum.Entities';
import { is } from '@framework/Signum.Entities';
import { QueryTokenString } from '@framework/Reflection';
import { useForceUpdate } from '@framework/Hooks'
import { QueryFilterEmbedded, QueryTokenEmbedded } from '../../Signum.UserAssets/Signum.UserAssets.Queries'
import { LinkButton } from '@framework/Basics/LinkButton'

export default function PredictorSubQuery(p : { ctx: TypeContext<PredictorSubQueryEntity>, mainQuery: PredictorMainQueryEmbedded, mainQueryDescription: QueryDescription }): React.JSX.Element {
  const forceUpdate = useForceUpdate();
  function handleOnChange() {
    const e = p.ctx.value;
    e.filters = [];
    e.columns = [];
    forceUpdate();
  }

  function handlePreviewSubQuery(e: React.MouseEvent<any>) {

    var sq = p.ctx.value;

    Finder.getQueryDescription(sq.query!.key).then(sqd =>
      UserAssetClient.API.parseFilters({
        queryKey: sqd.queryKey,
        canAggregate: true,
        entity: undefined,
        filters: (getMainFilters() ?? []).concat(sq.filters).map(mle => UserAssetClient.Converter.toQueryFilterItem(mle.element))
      } as UserAssetClient.API.ParseFiltersRequest).then(filters => {
        var fo: FindOptions = {
          queryName: sq.query!.key,
          groupResults: true,
          filterOptions: filters.map(f => UserAssetClient.Converter.toFilterOption(f)),
          columnOptions: [{ token: QueryTokenString.count } as ColumnOption]
            .concat(sq.columns.map(mle => ({ token: mle.element.token && mle.element.token.tokenString, } as ColumnOption))),
          columnOptionsMode: "ReplaceAll",
        };

        Finder.exploreWindowsOpen(fo, e);
      }));
  }

  function getMainFilters() {
    const mq = p.mainQuery;
    const sq = p.ctx.value;
    if (is(mq.query, p.ctx.value.query))
      return mq.filters;

    var parentKey = sq.columns.singleOrNull(a => a.element.usage == "ParentKey");

    if (parentKey == null)
      return null;

    var t = parentKey.element.token;

    var prefix = t?.token && t.token.fullKey;

    if (prefix == null)
      return null;

    return p.mainQuery.filters.map(f => newMListElement(QueryFilterEmbedded.New(
      {
        token: f.element.token && QueryTokenEmbedded.New({ tokenString: prefix + "." + f.element.token.tokenString }),
        operation: f.element.operation,
        valueString: f.element.valueString,
      })));
  }

  function handleChangeUsage(ctx: TypeContext<PredictorSubQueryColumnEmbedded>) {
    var col = ctx.value;
    if (isInputOutput(ctx.value.usage)) {
      initializeColumn(p.ctx.findParent(PredictorEntity), col);
    } else {
      col.encoding = null!;
      col.nullHandling = null;
    }

    forceUpdate();
  }

  const ctx = p.ctx;
  const ctxxs = ctx.subCtx({ formSize: "xs" });
  const entity = ctx.value;
  const queryKey = entity.query && entity.query.key;

  const parentCtx = ctx.findParentCtx(PredictorEntity);

  var mq = parentCtx.value.mainQuery;

  var tokens = mq.groupResults ? mq.columns.map(mle => mle.element.token).filter(t => t != null && t.token != null && t.token.queryTokenType != "Aggregate").map(t => t!.token!.niceTypeName) :
    [p.mainQueryDescription.columns["Entity"].niceTypeName];

  var parentKeyColumns = ctx.value.columns.map(mle => mle.element).filter(col => col.usage == "ParentKey");

  var getParentKeyMessage = (col: PredictorSubQueryColumnEmbedded) => {
    var index = parentKeyColumns.indexOf(col);
    if (index == -1)
      return undefined;

    if (index < tokens.length)
      return PredictorMessage.ShouldBeOfType0.niceToString(tokens[index]);

    return PredictorMessage.TooManyParentKeys.niceToString();
  };


  return (
    <div>
      <TextBoxLine ctx={ctx.subCtx(f => f.name)} valueHtmlAttributes={{ onBlur: () => parentCtx.frame!.entityComponent!.forceUpdate() }} />
      <EntityLine ctx={ctx.subCtx(f => f.query)} remove={ctx.value.isNew} onChange={handleOnChange} />
      {queryKey &&
        <div>
        <FilterBuilderEmbedded ctx={ctxxs.subCtx(a => a.filters)} queryKey={queryKey}
          subTokenOptions={SubTokensOptions.CanAnyAll | SubTokensOptions.CanElement | SubTokensOptions.CanAggregate}
          />
          <EntityTable ctx={ctxxs.subCtx(e => e.columns)} columns={[
            {
              property: a => a.usage, template: colCtx => <AutoLine ctx={colCtx.subCtx(a => a.usage)} onChange={() => handleChangeUsage(colCtx)} />
            },
            {
              property: a => a.token,
              template: colCtx => <QueryTokenEmbeddedBuilder
                ctx={colCtx.subCtx(a => a.token)}
                queryKey={p.ctx.value.query!.key}
                subTokenOptions={SubTokensOptions.CanElement | SubTokensOptions.CanAggregate}
                onTokenChanged={() => handleChangeUsage(colCtx)}
                helpText={getParentKeyMessage(colCtx.value)}
              />,
              headerHtmlAttributes: { style: { width: "50%" } },
            },
            { property: a => a.encoding, template: colCtx => isInputOutput(colCtx.value.usage) && <AutoLine ctx={colCtx.subCtx(a => a.encoding)} /> },
            { property: a => a.nullHandling, template: colCtx => isInputOutput(colCtx.value.usage) && <AutoLine ctx={colCtx.subCtx(a => a.nullHandling)} /> },
          ]} />

          {ctx.value.query && <LinkButton title={undefined} onClick={handlePreviewSubQuery}>{PredictorMessage.Preview.niceToString()}</LinkButton>}
        </div>}
    </div>
  );
}

function isInputOutput(usage: PredictorSubQueryColumnUsage | undefined): boolean {
  return usage == "Input" || usage == "Output";
}
