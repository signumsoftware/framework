import * as React from "react";
import { Driver,
driver } from "driver.js";
import "driver.js/dist/driver.css";
import { TourEntity, TourMessage } from "./Signum.Tour";
import { useAPI } from "@framework/Hooks";
import { TourClient, TourDTO } from "./TourClient";
import { Entity,
Lite } from "@framework/Signum.Entities";

export function TourButton(p: { target: Lite<Entity> }) {

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

    const steps = tour.steps.map(step => ({
      element: step.cssSelector || undefined,
      popover: step.cssSelector ? {
        title: step.title ?? undefined,
        description: step.description ?? undefined,
        side: step.side as any,
        align: step.align as any,
      } : {
        title: step.title ?? undefined,
        description: step.description ?? undefined,
      }
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
      overlayOpacity: 0.75,
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
