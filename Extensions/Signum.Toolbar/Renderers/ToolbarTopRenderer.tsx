import * as React from 'react'
import { useLocation, Location } from 'react-router'
import { ToolbarClient, ToolbarResponse } from '../ToolbarClient'
import '@framework/Frames/MenuIcons.css'
import './Toolbar.css'
import { useAPI, useUpdatedRef } from '@framework/Hooks'
import { QueryString } from '@framework/QueryString'
import { classes } from '@framework/Globals';
import { inferActive, isCompatibleWithUrl, renderNavItem } from './ToolbarRenderer';
import { InferActiveResponse, ToolbarContext } from '../ToolbarConfig'


export default function ToolbarTopRenderer(): React.ReactElement | null {
  const response = useAPI(() => ToolbarClient.API.getCurrentToolbar("Top"), []);
  const responseRef = useUpdatedRef(response);

  const [refresh, setRefresh] = React.useState(false);
  const [active, setActive] = React.useState<InferActiveResponse | null>(null);
  const activeRef = useUpdatedRef(active);

  function changeActive(location: Location) {
    var query = QueryString.parse(location.search);
    if (responseRef.current) {
      if (activeRef.current && isCompatibleWithUrl(activeRef.current.response, location, query, undefined)) {
        return;
      }

      var newActive = inferActive(responseRef.current, location, query);
      setActive(newActive ?? null);
    }
  }
  const location = useLocation();
  React.useEffect(() => {
    if (response != null)
      changeActive(location);
  }, [response, location]);

  function handleRefresh() {
    return window.setTimeout(() => setRefresh(!refresh), 500)
  }

  const ctx: ToolbarContext = {
    active: active,
    onRefresh: handleRefresh
  };

  return (
    <div className={classes("nav navbar-nav")}>
      {response && response.elements && response.elements.map((res: ToolbarResponse<any>, i: number) => renderNavItem(res, i, ctx, null))}
    </div>
  );
}
