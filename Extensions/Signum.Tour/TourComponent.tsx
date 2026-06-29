import * as React from "react";
import {
  Alignment,
  Driver,
  driver, 
  DriveStep,
  Side } from "driver.js";
import "driver.js/dist/driver.css";
import { TourEntity, TourMessage } from "./Signum.Tour";
import { TourTriggerSymbol } from "@framework/Signum.Basics";
import { useAPI } from "@framework/Hooks";
import { TourClient, TourDTO } from "./TourClient";
import { Entity,
  Lite,
  isLite,
  liteKey,
  toLite } from "@framework/Signum.Entities";
import { FontAwesomeIcon } from "@fortawesome/react-fontawesome";
import { faBiking } from "@fortawesome/free-solid-svg-icons";
import { getTypeName,
  PseudoType, 
  tryGetTypeInfo} from "@framework/Reflection";
import { LinkButton } from "@framework/Basics/LinkButton";
import { classes } from "@framework/Globals";
import { Navigator } from "@framework/Navigator";
import * as AppContext from "@framework/AppContext";
import { JSX } from "react/jsx-runtime";
import { micromark } from "micromark";

export function TourButton(p: { trigger: PseudoType | TourTriggerSymbol | Lite<Entity>; className?: string }): JSX.Element | null {
  const storageKey =
    isLite(p.trigger) ? `tour-viewed-${liteKey(p.trigger)}` :
    TourTriggerSymbol.isInstance(p.trigger) ? `tour-viewed-${p.trigger.key}` :
    `tour-viewed-${getTypeName(p.trigger)}`;

  const [hasViewed, setHasViewed] = React.useState(() => {
    return localStorage.getItem(storageKey) === "true";
  });

  const [tourRunId, setTourRunId] = React.useState(0);

  const tour = useAPI(() => {
    if (isLite(p.trigger)) {
      return TourClient.API.getTourByLite(p.trigger);
    } else if (TourTriggerSymbol.isInstance(p.trigger)) {
      return TourClient.API.getTourBySymbol(p.trigger.key);
    } else {
      return TourClient.API.getTourByEntity(getTypeName(p.trigger));
    }
  }, [p.trigger]);

  const driverRef = React.useRef<Driver | null>(null);

  const handleClick = () => {
    if (!hasViewed) {
      localStorage.setItem(storageKey, "true");
      setHasViewed(true);
    }

    // Increment to force remount (new key) and auto-start on every click
    setTourRunId(prev => prev + 1);
  };

  if (tour === undefined) {
    return null; // still loading
  }

  if (tour === null) {
    // No tour exists yet. Let an authorized user author one from here.


    // No tour yet: only offer authoring it to users that can create a TourEntity.
    if (!Navigator.isCreable(TourEntity, { isSearch: true }))
      return null;

    async function handleCreate() {
    
      const triggerLite: Lite<Entity> =
        isLite(p.trigger) ? p.trigger :
          TourTriggerSymbol.isInstance(p.trigger) ? toLite(p.trigger) :
            toLite((await Navigator.API.getType(getTypeName(p.trigger)))!);

      await Navigator.createInNewTab({ entity: TourEntity.New({ trigger: triggerLite }), canExecute: {} });
    }

    return (
      <LinkButton
        className={classes('sf-pointer nav-link', p.className)}
        onClick={handleCreate}
        title={TourMessage.CreateTour.niceToString()}
      >
        <span className="fa-layers fa-fw icon">
          <FontAwesomeIcon aria-hidden={true} icon={faBiking}  transform="flip-h" color="var(--bs-secondary)" />
          <FontAwesomeIcon aria-hidden={true} icon={["fas", "circle-plus"]} transform="shrink-7 down-4 left-6" color="var(--bs-success)" />
        </span>
      </LinkButton>
    );
  }

  const canEdit = !Navigator.isReadOnly(TourEntity);
  const handleEdit = () => window.open(AppContext.toAbsoluteUrl(Navigator.navigateRoute(tour.tour)));

  const handleClickOrEdit = (e: React.MouseEvent) => {
    if (canEdit && (e.ctrlKey || e.altKey)) {
      handleEdit();
    } else {
      handleClick();
    }
  };

  const editHint = canEdit ? ` (${TourMessage.EditTour.niceToString()}: Ctrl/Alt+Click)` : "";
  const title = (hasViewed ? TourMessage.ReplayTour.niceToString() : TourMessage.StartTour.niceToString()) + editHint;

  return (
    <>
      <LinkButton
        className={classes('sf-pointer nav-link', p.className)}
        onClick={handleClickOrEdit}
        title={title}
      >
        <span className={classes("fa-layers fa-fw icon", !hasViewed && "fa-beat")}>
          <FontAwesomeIcon icon={faBiking} transform="flip-h" />
          {canEdit && <FontAwesomeIcon aria-hidden={true} icon={["fas", "circle-arrow-right"]} transform="shrink-7 down-4 left-6" color="var(--bs-info)"  />}
        </span>
      </LinkButton>
      {tourRunId > 0 && <TourComponent key={tourRunId} tour={tour} autoStart={true} ref={driverRef} />}
    </>
  );
}

function waitForElement(selector: string, timeout: number = 5000): Promise<Element> {
  return new Promise((resolve, reject) => {
    if (document.querySelector(selector)) {
      return resolve(document.querySelector(selector)!);
    }
    
    const observer = new MutationObserver(() => {
      if (document.querySelector(selector)) {
        observer.disconnect();
        resolve(document.querySelector(selector)!);
      }
    });
    
    observer.observe(document.body, {
      childList: true,
      subtree: true
    });
    
    setTimeout(() => {
      observer.disconnect();
      reject(new Error('Element not found: ' + selector));
    }, timeout);
  });
}

export function TourComponent({ tour, autoStart = true, ref }: {
  tour: TourDTO;
  autoStart?: boolean;
  ref?: React.Ref<Driver | null>;
}) {
  const driverRef = React.useRef<Driver | null>(null);

  React.useImperativeHandle(ref, () => driverRef.current!);

  React.useEffect(() => {
    if (!tour) return;

    // Check if it's a TourDTO or TourEntity

    const steps = tour.steps.map<DriveStep>((step, i, steps) => ({
      element: step.cssSelector || undefined,
      popover: step.cssSelector ? {
        title: step.title ?? undefined,
        description: step.description ? micromark(step.description) : undefined,
        side: step.side as Side,
        align: step.align as Alignment,
        onPopoverRender: async (popover, opts) => {
          if (step.click === "OnLoad") {
            if (step.cssSelector) {
              var elem = await waitForElement(step.cssSelector);
              (elem as HTMLButtonElement).click();
            }
          }
        },
        onNextClick: async e => {
          if (step.click === "OnNext") {
            (e as HTMLButtonElement).click();
            var nextStep = steps[i + 1];
            if (nextStep?.cssSelector)
              await waitForElement(nextStep?.cssSelector);
          }

          driverObj.moveNext();
        },
      } : {
        title: step.title ?? undefined,
        description: step.description ? micromark(step.description) : undefined,
      },
    }));

    const driverObj = driver({
      steps,
      showProgress: tour.showProgress,
      animate: tour.animate,
      showButtons: [
        "next",
        "previous",
        tour.showCloseButton ? "close" : null
      ].filter(Boolean) as any,

      nextBtnText: TourMessage.Next.niceToString(),
      prevBtnText: TourMessage.Previous.niceToString(),
      doneBtnText: TourMessage.Done.niceToString(),

      overlayColor: "black",
      overlayOpacity: 0.3,
      stagePadding: 10,
      stageRadius: 5,
      popoverOffset: 10,
      allowClose: true,
    });

    driverRef.current = driverObj;

    if (autoStart) {
      driverObj.drive();
    }

    return () => {
      driverObj.destroy();
      driverRef.current = null;
    };
  }, [tour, autoStart]);

  return null;
}

export default TourComponent;
