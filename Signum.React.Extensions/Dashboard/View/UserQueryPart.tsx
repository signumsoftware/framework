
import * as React from 'react'
import { FindOptions } from '@framework/FindOptions'
import { getQueryNiceName } from '@framework/Reflection'
import { Entity, Lite, is, JavascriptMessage } from '@framework/Signum.Entities'
import { SearchControl, ValueSearchControl } from '@framework/Search'
import * as UserQueryClient from '../../UserQueries/UserQueryClient'
import { UserQueryPartEntity, PanelPartEmbedded, PanelStyle } from '../Signum.Entities.Dashboard'
import { classes } from '@framework/Globals';
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome';
import { parseIcon } from '../Admin/Dashboard';
import { useAPI } from '../../../../Framework/Signum.React/Scripts/Hooks'
import { PanelPartContentProps } from '../DashboardClient'

export default function UserQueryPart(p: PanelPartContentProps<UserQueryPartEntity>) {

  const fo = useAPI(signal => UserQueryClient.Converter.toFindOptions(p.part.userQuery, p.entity), [p.part.userQuery, p.entity]);

  if (!fo)
    return <span>{JavascriptMessage.loading.niceToString()}</span>;

  if (p.part.renderMode == "BigValue") {
    return <BigValueSearchCounter
      findOptions={fo}
      text={p.partEmbedded.title ?? undefined}
      style={p.partEmbedded.style}
      iconName={p.partEmbedded.iconName ?? undefined}
      iconColor={p.partEmbedded.iconColor ?? undefined}
    />;
  }

  return (
    <SearchControl
      findOptions={fo}
      showHeader={"PinnedFilters"}
      showFooter={p.part.showFooter}
      allowSelection={p.part.allowSelection} />
  );
}


interface BigValueBadgeProps {
  findOptions: FindOptions;
  text?: string;
  style: PanelStyle;
  iconName?: string;
  iconColor?: string;
}

export function BigValueSearchCounter(p: BigValueBadgeProps) {

  const isRTL = React.useMemo(() => document.body.classList.contains("rtl"), []);

  const vsc = React.useRef<ValueSearchControl>(null);

  return (
    <div className={classes(
      "card",
      p.style != "Light" && p.style != "Secondary" && "text-white",
      "bg-" + p.style.toLowerCase(),
      "o-hidden"
    )}>
      <div className={classes("card-body", "bg-" + p.style.toLowerCase())} onClick={e => vsc.current!.handleClick(e)} style={{ cursor: "pointer" }}>
        <div className="row">
          <div className="col-3">
            {p.iconName &&
              <FontAwesomeIcon icon={parseIcon(p.iconName)!} color={p.iconColor} size="4x" />}
          </div>
          <div className={classes("col-9 flip", isRTL ? "text-left" : "text-right")}>
            <h1>
              <ValueSearchControl ref={vsc} findOptions={p.findOptions} isLink={false} isBadge={false} />
            </h1>
          </div>
        </div>
        <div className={classes("flip", isRTL ? "text-left" : "text-right")}>
          <h6 className="large">{p.text ?? getQueryNiceName(p.findOptions.queryName)}</h6>
        </div>
      </div>
    </div>
  );
}





