import * as React from 'react'
import { Operations } from '@framework/Operations'
import { useRootClose } from '@restart/ui'
import { Finder } from '@framework/Finder'
import { is, JavascriptMessage, toLite } from '@framework/Signum.Entities'
import { Toast, Button, ButtonGroup } from 'react-bootstrap'
import { DateTime } from 'luxon'
import { useAPI, useAPIWithReload, useForceUpdate, useUpdatedRef } from '@framework/Hooks';
import { Navigator } from '@framework/Navigator'
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome'
import { WhatsNewClient } from '../WhatsNewClient'
import "./WhatsNewDropdown.css"
import { Link } from 'react-router-dom';
import { classes, Dic } from '@framework/Globals'
import MessageModal from '@framework/Modals/MessageModal'
import { WhatsNewEntity, WhatsNewLogEntity, WhatsNewMessage, WhatsNewOperation, WhatsNewState } from '../Signum.WhatsNew'
import * as AppContext from "@framework/AppContext"
import { HtmlViewer } from '../Templates/WhatsNewHtmlEditor'
import { LinkButton } from '../../../Signum/React/Basics/LinkButton'

export default function WhatsNewDropdown(): React.JSX.Element | null {

  if (!Navigator.isViewable(WhatsNewEntity))
    return null;

  return <WhatsNewDropdownImp />;
}

function WhatsNewDropdownImp() {

  const forceUpdate = useForceUpdate();
  const [isOpen, setIsOpen] = React.useState<boolean>(false);

  const showNews = 3; 
  
  const isOpenRef = useUpdatedRef(isOpen);

  var [countResult, reloadCount] = useAPIWithReload<WhatsNewClient.NumWhatsNews>(() => WhatsNewClient.API.myNewsCount().then(res => {
    if (isOpenRef.current) {
      WhatsNewClient.API.myNews()
        .then(als => {
          setNews(als);
        });
    }

    return res;
  }), [], { avoidReset: true });

  Navigator.useEntityChanged(WhatsNewLogEntity, () => reloadCount(), []);

  const [whatsNew, setNews] = React.useState<WhatsNewClient.WhatsNewShort[] | undefined>(undefined);

  function handleOnToggle() {

    if (!isOpen) {
      WhatsNewClient.API.myNews()
        .then(wn => setNews(wn));
    }

    setIsOpen(!isOpen);
  }

  function handleClickAll() {
    setIsOpen(false);
    AppContext.navigate("/news/");
  }

  function handleOnCloseNews(toRemove: WhatsNewClient.WhatsNewShort[]) {

    //Optimistic
    let wasClosed = false;
    if (whatsNew) {
      whatsNew.extract(a => toRemove.some(r => is(r.whatsNew, a.whatsNew)));
      if (whatsNew.length == 0) {
        setIsOpen(false);
        wasClosed = true;
      }
    }
    if (countResult)
      countResult.numWhatsNews -= 1;
    forceUpdate();

    WhatsNewClient.API.setNewsLogRead(toRemove.map(r => r.whatsNew)).then(res => {

      // Pesimistic
      WhatsNewClient.API.myNews()
        .then(wn => {
          if (wasClosed && wn.length > 0)
            setIsOpen(true);

          setNews(wn);
        });

      reloadCount();
    }), [toRemove];
  }

  var newsInOrder = whatsNew == null ? null : whatsNew.orderByDescending(w => w.creationDate);

  var divRef = React.useRef<HTMLDivElement>(null);

  useRootClose(divRef as any, () => setIsOpen(false), { disabled: !isOpen });

  return (
    <>
      <button
        className="nav-link sf-bell-container"
        onClick={handleOnToggle}
        style={{ border: 0, backgroundColor: 'var(--bs-transparent)' }}
        title={(countResult ? WhatsNewEntity.niceCount(countResult.numWhatsNews) : WhatsNewEntity.nicePluralName())}      >
        <FontAwesomeIcon aria-hidden={true} icon="bullhorn"
          className={classes("sf-newspaper", isOpen && "open", countResult && countResult.numWhatsNews > 0 && "active")}
        />
        {countResult && countResult.numWhatsNews > 0 && <span className="badge bg-danger badge-pill sf-news-badge">{countResult.numWhatsNews}</span>}
      </button>
      {isOpen && <div className="sf-news-toasts mt-2" ref={divRef} style={{
        backdropFilter: "blur(10px)",
        transition: "transform .4s ease" }}>
        {newsInOrder == null ? <Toast> <Toast.Body>{JavascriptMessage.loading.niceToString()}</Toast.Body></Toast> :

          <>
            {newsInOrder.length == 0 && <Toast><Toast.Body>{WhatsNewMessage.YouDoNotHaveAnyUnreadNews.niceToString()}</Toast.Body></Toast>}

            {
              newsInOrder.filter((gr, i) => i < showNews)
                .map(a => <WhatsNewToast whatsnew={a} key={a.whatsNew.id} onClose={handleOnCloseNews} refresh={reloadCount} setIsOpen={setIsOpen} />)
            }
            {
              newsInOrder.length > showNews &&
              <Toast onClose={() => handleOnCloseNews(whatsNew!.map(a => a))}>
                <Toast.Header>
                    <small>{WhatsNewMessage.CloseAll.niceToString()}</small>
                </Toast.Header>
              </Toast>
            }
            <Toast>
              <Toast.Body style={{ textAlign: "center" }}>
                <LinkButton title={undefined} style={{ color: "var(--bs-primary)" }}  onClick={() => handleClickAll()}>{WhatsNewMessage.AllMyNews.niceToString()}</LinkButton>
              </Toast.Body>
            </Toast>
          </>
        }
      </div>}
    </>
  );
}

export function WhatsNewToast(p: { whatsnew: WhatsNewClient.WhatsNewShort, onClose: (e: WhatsNewClient.WhatsNewShort[]) => void, refresh: () => void, className?: string; setIsOpen: (isOpen: boolean) => void }): React.JSX.Element
{
  //ignoring open tags other than img
  function HTMLSubstring(text: string) {
    var substring = text.substring(0, 100);
    substring = substring.replace("<p>", "");
    substring = substring.replace("</p>", "");
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

  function handleClickPreviewPicture(e: React.MouseEvent) {
    e.preventDefault();
    p.setIsOpen(false);
    AppContext.navigate("/newspage/" + p.whatsnew.whatsNew.id);
  }

  return (
    <Toast onClose={() => p.onClose([p.whatsnew])} className={p.className} aria-atomic={true}>
      <Toast.Header closeLabel={WhatsNewMessage.Close0WhatsNew.niceToString(p.whatsnew.title)}>
        <strong className="me-auto" role="heading" aria-level={3}>{p.whatsnew.title} {!Navigator.isReadOnly(WhatsNewEntity) && <small style={{ color: "var(--bs-danger)" }}>{(p.whatsnew.status == "Draft") ? p.whatsnew.status : undefined}</small>}</strong>
        <small>{DateTime.fromISO(p.whatsnew.creationDate!).toRelative()}</small>
      </Toast.Header>
      <Toast.Body style={{ whiteSpace: "pre-wrap" }}>
        <Link
          to={"/newspage/" + p.whatsnew.whatsNew.id}
          onClick={e => { p.onClose([p.whatsnew]); handleClickPreviewPicture(e); }}
          aria-label={`${p.whatsnew.title} – ${WhatsNewMessage.ReadFurther.niceToString()}`}
          style={{ display: "inline-block", maxWidth: "10vw", marginLeft: 10 }}>
          <img
            src={AppContext.toAbsoluteUrl("/api/whatsnew/previewPicture/" + p.whatsnew.whatsNew.id)}
            alt={p.whatsnew.title}
            style={{
              maxHeight: "30vh",
              maxWidth: "100%",
              borderRadius: 4,
              display: "block",
            }}
          />
        </Link>
        <HtmlViewer text={HTMLSubstring(p.whatsnew.description)} />
        <br />
        <Link
          onClick={e => { p.onClose([p.whatsnew]); handleClickPreviewPicture(e) }}
          to={"/newspage/" + p.whatsnew.whatsNew.id}
          aria-label={WhatsNewMessage.ReadFurther.niceToString()}>
            {WhatsNewMessage.ReadFurther.niceToString()}
        </Link>
      </Toast.Body>
    </Toast>
  );
}

export declare namespace WhatsNewToast {
    export var icons: {
        [alertTypeKey: string]: React.ReactNode
    }
}

WhatsNewToast.icons = {} as { [alertTypeKey: string]: React.ReactNode };
