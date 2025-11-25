import * as React from 'react'
import { FindOptions, QueryToken } from '@framework/FindOptions'
import { getQueryKey } from '@framework/Reflection'
import { JavascriptMessage, toLite, liteKey, translated } from '@framework/Signum.Entities'
import { SearchValue, SearchValueController } from '@framework/Search'
import { UserQueryClient } from '../../UserQueryClient'
import { classes, getColorContrasColorBWByHex, softCast } from '@framework/Globals';
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome';
import { Finder } from '@framework/Finder'
import { useAPI, useForceUpdate, useVersion } from '@framework/Hooks'
import { parseIcon } from '@framework/Components/IconTypeahead'
import { DashboardPinnedFilters } from '../../../Signum.Dashboard/View/DashboardFilterController'
import { BigValuePartEntity, UserQueryEntity } from '../../Signum.UserQueries'
import { UserAssetClient } from '../../../Signum.UserAssets/UserAssetClient'
import { BigValueClient } from '../../BigValueClient'
import * as AppContext from "@framework/AppContext"
import { toAbsoluteUrl } from '@framework/AppContext'
import { ToolbarUrl } from '../../../Signum.Toolbar/ToolbarUrl'
import { ToolbarClient } from '../../../Signum.Toolbar/ToolbarClient'
import { selectSubEntity } from '../../UserQueryToolbarConfig'
import { PanelPartContentProps } from '../../../Signum.Dashboard/DashboardClient'


export interface UserQueryPartHandler {
  findOptions: FindOptions;
  refresh: () => void;
}

export default function BigValuePart(p: PanelPartContentProps<BigValuePartEntity>): React.JSX.Element {

  let fo = useAPI(signal => p.content.userQuery == null ? null : UserQueryClient.Converter.toFindOptions(p.content.userQuery, p.entity), [p.content.userQuery, p.entity && liteKey(p.entity)]);

  let valueToken = p.content.valueToken && UserAssetClient.getToken(p.content.valueToken);

  const [version, updateVersion] = useVersion();

  React.useEffect(() => {

    if (fo) {
      var dashboardPinnedFilters = fo.filterOptions?.filter(a => a?.dashboardBehaviour == "PromoteToDasboardPinnedFilter") ?? [];

      if (dashboardPinnedFilters.length) {
        Finder.getQueryDescription(fo.queryName)
          .then(qd => Finder.parseFilterOptions(dashboardPinnedFilters, fo!.groupResults ?? false, qd))
          .then(fops => {
            p.dashboardController.setPinnedFilter(new DashboardPinnedFilters(p.partEmbedded, getQueryKey(fo!.queryName), fops));
            p.dashboardController.registerInvalidations(p.partEmbedded, () => updateVersion());
          });
      } else {
        p.dashboardController.clearPinnesFilter(p.partEmbedded);
        p.dashboardController.registerInvalidations(p.partEmbedded, () => updateVersion());
      }
    }
  }, [fo, p.partEmbedded]);

  const cachedQuery = p.content.userQuery && p.cachedQueries[liteKey(toLite(p.content.userQuery))];

  if (p.content.userQuery == null)
    fo = {
      queryName: p.dashboardController.dashboard.entityType!.model as string,
      filterOptions: [{ token: "Entity", value: p.entity }]
    };




  const vsc = React.useRef<SearchValueController>(null);
  const forceUpdate = useForceUpdate();


  if (!fo)
    return <span>{JavascriptMessage.loading.niceToString()}</span>;

  if (p.dashboardController.isLoading)
    return <span>{JavascriptMessage.loading.niceToString()}...</span>;

  const foExpanded = p.dashboardController.applyToFindOptions(p.partEmbedded, fo);

  p.customDataRef.current = softCast<UserQueryPartHandler>({
    findOptions: foExpanded,
    refresh: updateVersion,
  });

  //const clickable = p.content.userQuery != null;
  const clickable = p.content.userQuery != null && (p.content.isClickable ?? true)
  const customColor = p.partEmbedded.customColor;

  async function handleNavigate(e: React.MouseEvent) {
    if (p.content.customUrl) {
      let url = p.content.customUrl;

      if (ToolbarUrl.hasSubEntity(url)) {

        if (p.content.userQuery == null)
          throw new Error("SubEntity (:id2, :type2, :key2) can only be used when a UserQuery is defined");

        const subEntity = await selectSubEntity(p.content.userQuery, p.entity);
        if (subEntity == null)
          return;

        url = ToolbarUrl.replaceSubEntity(url, subEntity)
      }

      url = ToolbarUrl.replaceVariables(url)

      if (p.entity)
        url = ToolbarUrl.replaceEntity(url, p.entity)

      if (ToolbarUrl.isExternalLink(url))
        window.open(url);
      else
        AppContext.pushOrOpenInTab(url, e);
    } else {
      const url = await UserQueryClient.getUserQueryUrl(p.content.userQuery!, p.entity);
      AppContext.navigate(url);

    }
  }

  var custom = p.content.customBigValue ? BigValueClient.renderCustomBigValue(p.content.customBigValue, { content: p.content, entity: p.entity, value: vsc.current?.value }) : null; 

  function renderCardContent() {
    return (
      <>
        <div className="dashboard-flex">
          <div className="left">
            <h3>
              <SearchValue ref={vsc} findOptions={foExpanded} isLink={false} isBadge={false} deps={p.deps}
                onValueChange={forceUpdate}
                onInitialValueLoaded={forceUpdate}
                searchControlProps={{ extraOptions: { userQuery: toLite(p.content.userQuery) } }}
                valueToken={valueToken ?? undefined}
                onRender={custom?.value == null ? undefined : v => custom?.value}
              />
              {/*customRequest={p.cachedQuery && ((req, fop, token) => p.cachedQuery!.then(cq => executeQueryValueCached(req, fop, token, cq)))}*/}
            </h3>

          </div>
          <div className="right">
            {p.partEmbedded.iconName &&
              <FontAwesomeIcon role="img" icon={parseIcon(p.partEmbedded.iconName)!} color={p.partEmbedded.iconColor ?? undefined} size="2x" />}
          </div>
        </div>
        <h2 className="medium h3">{
          custom?.message ?? (translated(p.partEmbedded, a => a.title) ||
            (p.content.userQuery ? translated(p.content.userQuery, a => a.displayName) : valueToken?.niceName))

        }</h2>
      </>
    );
  }

  return (
    <div className={classes("card", "border-tertiary shadow-sm mb-3 w-100", "o-hidden")}
      style={{
        backgroundColor: customColor ?? undefined,
        color: Boolean(customColor) ? getColorContrasColorBWByHex(customColor!) : "var(--bs-body-color)"
      }}>
      {clickable ? (
        <button
          type="button"
          onClick={p.content.navigate ? handleNavigate : e => vsc.current!.handleClick(e)}
          className="card-body border-0 bg-transparent text-start w-100"
          style={{
            backgroundColor: customColor ?? undefined,
            color: p.partEmbedded.titleColor ?? (customColor ? getColorContrasColorBWByHex(customColor!) : "var(--bs-body-color)"),
          }}>
          {renderCardContent()}
        </button>
      ) : (
        <div
          className="card-body"
          style={{
            backgroundColor: customColor ?? undefined,
            color: p.partEmbedded.titleColor ?? (customColor ? getColorContrasColorBWByHex(customColor!) : "var(--bs-body-color)"),
          }}>
          {renderCardContent()}
        </div>
      )}
    </div>
  );

 
}
