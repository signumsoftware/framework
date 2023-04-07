import * as React from 'react'
import { useLocation, Location } from 'react-router'
import * as AppContext from '@framework/AppContext'
import * as ToolbarClient from '../ToolbarClient'
import { ToolbarConfig } from "../ToolbarClient";
import '@framework/Frames/MenuIcons.css'
import './Toolbar.css'
import { Nav } from 'react-bootstrap';
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome'
import { useAPI, useUpdatedRef } from '@framework/Hooks'
import { QueryString } from '@framework/QueryString'
import { parseIcon } from '../../Basics/Templates/IconTypeahead'
import { SidebarMode  } from '../SidebarContainer'
import { isActive } from '@framework/FindOptions';
import { classes, Dic } from '@framework/Globals';
import { urlVariables } from '../../Dashboard/UrlVariables';
import { inferActive, isCompatibleWithUrl, renderNavItem } from './ToolbarRenderer';


export default function ToolbarTopRenderer(): React.ReactElement | null {
  const response = useAPI(() => ToolbarClient.API.getCurrentToolbar("Top"), []);
  const responseRef = useUpdatedRef(response);

  const [refresh, setRefresh] = React.useState(false);
  const [active, setActive] = React.useState<ToolbarClient.ToolbarResponse<any> | null>(null);
  const activeRef = useUpdatedRef(active);

  function changeActive(location: Location) {
    var query = QueryString.parse(location.search);
    if (responseRef.current) {
      if (activeRef.current && isCompatibleWithUrl(activeRef.current, location, query)) {
        return;
      }

      var newActive = inferActive(responseRef.current, location, query);
      setActive(newActive?.response ?? null);
    }
  }
  const location = useLocation();
  React.useEffect(() => {
    if (response != null)
      changeActive(location);
  }, [response, location]);

  function handleRefresh() {
    return setTimeout(() => setRefresh(!refresh), 500)
  }

  return (
     <div className={classes("nav navbar-nav")}>
        {response && response.elements && response.elements.map((res: ToolbarClient.ToolbarResponse<any>, i: number) => renderNavItem(res, active, i, handleRefresh))}
      </div>
  );
}
