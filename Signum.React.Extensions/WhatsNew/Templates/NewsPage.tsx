import * as React from 'react'
import { JavascriptMessage } from '@framework/Signum.Entities';
import { WhatsNewEntity, WhatsNewMessage } from '../Signum.Entities.WhatsNew';
import { useAPI } from '../../../../Framework/Signum.React/Scripts/Hooks';
import { API, WhatsNewFull } from "../WhatsNewClient";
import "./NewsPage.css"
import * as AppContext from "@framework/AppContext"
import { FilePathEmbedded } from '../../../../Framework/Signum.React.Extensions/Files/Signum.Entities.Files';
import { downloadFile } from '../../../../Framework/Signum.React.Extensions/Files/FileDownloader';
import * as Services from '@framework/Services'
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome';
import { RouteComponentProps } from "react-router-dom";

export default function NewsPage(p: RouteComponentProps<{ newsId: string }>) {
  const whatsnew = useAPI(() => API.newsPage(p.match.params.newsId).then(w => w), [p.match.params.newsId]);

  if (whatsnew == undefined)
    return <div>{JavascriptMessage.loading.niceToString()}</div>;

    return (
    <div key={whatsnew.whatsNew.id} style = {{ position: "relative", cursor: "pointer", margin: "10px", }}>
      <div className={"whatsnewbody"} key={whatsnew.whatsNew.id}>
        {whatsnew.previewPicture != undefined && <img src={AppContext.toAbsoluteUrl("~/api/whatsnew/previewPicture/" + whatsnew.whatsNew.id)} className={"headerpicture"} />}
        <div className={"news pt-2"}>
          <h3 className={"news-title"}>{whatsnew.title}</h3>
            <div className={"news-text"}>{whatsnew.description}</div>
            {(whatsnew.attachments) && <Attachments news={whatsnew} />
          }
        </div>
      </div>
    </div>
  );
}

export function Attachments(p: { news: WhatsNewFull }) {
  const attachments = useAPI(() => API.attachments(p.news.whatsNew.id!).then(w => w), [p.news]);

  function downloadBase64(e: React.MouseEvent<any>, binaryFile: string, fileName: string) {
    e.preventDefault();

    const blob = Services.b64toBlob(binaryFile);

    Services.saveFileBlob(blob, fileName);
  };

  function handleDownload(e: React.MouseEvent, file: FilePathEmbedded) {
    e.preventDefault();
    if (file.binaryFile) {
      downloadBase64(e, file.binaryFile, file.fileName!);
    }
    else {
      downloadFile(file).then(res => res.blob()).then(blob => {
        Services.saveFileBlob(blob, file.fileName);
      });
    }
  }

  if (attachments != undefined) {
    return (
      <div>
        <hr />
        <h5>{WhatsNewMessage.Downloads.niceToString()} ({p.news.attachments.toString()})</h5>
        {attachments!.attachment.map(gr =>
          <div className="mt-3" key={gr.element.entityId}>
            <a href={gr.element.suffix} style={{ margin: "0 10px 0 0" }} onClick={(e) => handleDownload(e, gr.element)}>{gr.element.toStr}</a><FontAwesomeIcon icon="download" />
          </div>)}
      </div>
    );
  }
  else {
    return (<div></div>);
  }
}
