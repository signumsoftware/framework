import * as React from 'react'
import { JavascriptMessage, toLite } from '@framework/Signum.Entities';
import { WhatsNewEntity, WhatsNewLogEntity, WhatsNewMessage } from '../Signum.Entities.WhatsNew';
import { useAPI } from '../../../../Framework/Signum.React/Scripts/Hooks';
import { API, WhatsNewFull } from "../WhatsNewClient";
import "./NewsPage.css"
import * as AppContext from "@framework/AppContext"
import { FilePathEmbedded } from '../../../../Framework/Signum.React.Extensions/Files/Signum.Entities.Files';
import { downloadFile } from '../../../../Framework/Signum.React.Extensions/Files/FileDownloader';
import * as Services from '@framework/Services'
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome';
import { RouteComponentProps } from "react-router-dom";
import WhatsNewHtmlEditor, { HtmlViewer } from './WhatsNewHtmlEditor';
import { Binding } from '../../../Signum.React/Scripts/Lines';
import { Link } from 'react-router-dom';
import * as Navigator from '@framework/Navigator'
import EntityLink from '../../../Signum.React/Scripts/SearchControl/EntityLink';

export default function NewsPage(p: RouteComponentProps<{ newsId: string }>) {

  const [refreshValue, setRefreshValue] = React.useState<number>(0);
  const whatsnew = useAPI(() => API.newsPage(p.match.params.newsId).then(w => {
    Navigator.raiseEntityChanged(WhatsNewLogEntity);
    return w;
  }), [p.match.params.newsId, refreshValue]);

  if (whatsnew == undefined)
    return <div>{JavascriptMessage.loading.niceToString()}</div>;

  return (
    <div key={whatsnew.whatsNew.id} style={{ position: "relative", margin: "10px", }}>
      <div style={{ display: "flex", justifyContent: "space-between" }}>
        <Link to={"~/news/"} style={{ textDecoration: "none" }}> <FontAwesomeIcon icon={"angles-left"} /> {WhatsNewMessage.BackToOverview.niceToString()}</Link>
        {!Navigator.isReadOnly(WhatsNewEntity) && <small className="ms-2 lead"><EntityLink lite={whatsnew.whatsNew} onNavigated={() => setRefreshValue(a => a + 1)}><FontAwesomeIcon icon="pen-to-square" /></EntityLink></small>}
      </div>


      <div className={"whatsnewbody"} key={whatsnew.whatsNew.id}>
        {whatsnew.previewPicture != undefined && <img src={AppContext.toAbsoluteUrl("~/api/whatsnew/previewPicture/" + whatsnew.whatsNew.id)} className={"headerpicture headerpicture-shadow"} />}
        <div className={"news pt-2"}>
          <h3 className={"news-title"}>{whatsnew.title} {!Navigator.isReadOnly(WhatsNewEntity) && <small style={{ color: "#d50a30" }}>{(whatsnew.status == "Draft") ? whatsnew.status : undefined}</small>}</h3>
            <HtmlViewer text={whatsnew.description} />
        </div>
      </div>
    </div>
  );
}
