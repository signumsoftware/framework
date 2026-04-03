import * as React from 'react'
import { JavascriptMessage } from '@framework/Signum.Entities';
import { useAPI } from '@framework/Hooks';
import { WhatsNewClient } from "../WhatsNewClient";
import "./AllNewsPage.css"
import * as AppContext from "@framework/AppContext"
import { WhatsNewEntity, WhatsNewMessage } from '../Signum.WhatsNew';
import { HtmlViewer } from './WhatsNewHtmlEditor';
import { Link } from 'react-router-dom';
import { Navigator } from '@framework/Navigator';

export default function AllNews(): React.JSX.Element {
  const news: WhatsNewClient.WhatsNewFull[] | undefined = useAPI(() => WhatsNewClient.API.getAllNews().then(w => w), []);

  if (news == undefined)
    return <div>{JavascriptMessage.loading.niceToString()}</div>;

  return (
    <div>
      <h1 className="h2">{WhatsNewMessage.YourNews.niceToString()} {news && <span className="sf-news-notify-badge" style={{ marginTop: "6px", marginLeft: "3px", fontSize: "12px" }}>{news.length}</span>}
      </h1>
        <div className="mt-3">
        <div style={{ display: "flex", flexFlow: "wrap" }}>
          {news && news.orderByDescending(n => n.creationDate).map(wn =>
                  <WhatsNewPreviewPicture key={wn.whatsNew.id} news={wn} />
                )}
            </div>
          </div>
    </div>
  );
}

export function WhatsNewPreviewPicture(p: { news: WhatsNewClient.WhatsNewFull}): React.JSX.Element {

  const whatsnew = p.news;

  function handleClickPreviewPicture() {
    AppContext.navigate("/newspage/" + p.news.whatsNew.id);
  }


  //ignoring open tags other than img
  function HTMLSubstring(text: string ) {
    var substring = text.substring(0, 300);
    if (substring.contains("<img")) {
      var fullImageTag = substring.match(/(<img[^>] *)(\/>)/gmi);
      if (fullImageTag != undefined && fullImageTag.length >= 1) {
        return substring + "...";
      }
      else {
        return substring.substring(0, substring.indexOf("<img")) + "...";
      }
    }
    return substring + "...";
  }

  if (whatsnew == undefined)
    return <div>{JavascriptMessage.loading.niceToString()}</div>;

  return (
    <div key={whatsnew.whatsNew.id} style={{ position: "relative", cursor: "pointer", margin: "10px", }}>
      <div className={"card news-shadow"} style={{ width: "500px" }} key={whatsnew.whatsNew.id}>
        {whatsnew.previewPicture != undefined &&
          <Link
            to={"/newspage/" + p.news.whatsNew.id}
            onClick={() => { handleClickPreviewPicture(); }}
            aria-label={`${whatsnew.title} – ${WhatsNewMessage.ReadFurther.niceToString()}`}
            style={{ display: "inline-block", maxWidth: "10vw", marginLeft: 10 }}>
            <div className="preview-picture-card-box">
              <img alt={whatsnew.title} src={AppContext.toAbsoluteUrl("/api/whatsnew/previewPicture/" + whatsnew.whatsNew.id)} style={{ width: "100%", height: "auto" }} />
            </div>
          </Link>
        }
        <div className="card-body pt-2">
          <h2 className="card-title h5">{whatsnew.title}</h2>
          <small><HtmlViewer text={HTMLSubstring(whatsnew.description)} /></small>
          <br />
          <div style={{ display: "flex", justifyContent: "space-between"}}>
            <Link to={"/newspage/" + p.news.whatsNew.id}>{WhatsNewMessage.ReadFurther.niceToString()}</Link>
            {!Navigator.isReadOnly(WhatsNewEntity) && <small style={{ color: "#d50a30" }}> {(p.news.status == "Draft") ? p.news.status : undefined}</small>}
          </div>
          {(whatsnew.attachments > 0) && <Attachments news={whatsnew} />
          }
        </div>
      </div>
      <NewsBadge news={whatsnew} />
    </div>
  );
}

export function NewsBadge(p: { news: WhatsNewClient.WhatsNewFull }): React.JSX.Element {
  if (!p.news.read)
    return (
      <span className="sf-news-notify-badge" style={{ right: "0", top: "0" }}>{WhatsNewMessage.New.niceToString()}</span>
    );
  else {
    return (<div></div>);
  }
}

export function Attachments(p: { news: WhatsNewClient.WhatsNewFull }): React.JSX.Element {
  return (
    <div>
      <hr />
      <h3 className="h5">{WhatsNewMessage.Downloads.niceToString()} ({p.news.attachments.toString()})</h3>
    </div>
  );
}
