import * as React from "react";
import { driver } from "driver.js";
import "driver.js/dist/driver.css";
import { TourEntity, TourMessage } from "./Signum.Tour";
import { useAPI } from "@framework/Hooks";
import { TourClient, TourDTO } from "./TourClient";

export interface TourHelpers {
  start: () => void;
  destroy: () => void;
  next: () => void;
  previous: () => void;
  moveTo: (index: number) => void;
}

interface TourComponentProps {
  tour: TourEntity | TourDTO;
  autoStart?: boolean;
}

export const TourComponent = React.forwardRef<TourHelpers, TourComponentProps>(
  function TourComponent({ tour, autoStart = true }, ref) {
    const driverRef = React.useRef<any>(null);

    React.useImperativeHandle(ref, () => ({
      start: () => driverRef.current?.drive(),
      destroy: () => driverRef.current?.destroy(),
      next: () => driverRef.current?.moveNext(),
      previous: () => driverRef.current?.movePrevious(),
      moveTo: (index: number) => driverRef.current?.moveTo(index),
    }), []);

    React.useEffect(() => {
      if (!tour) return;

      // Check if it's a TourDTO or TourEntity
      const isTourDTO = 'guid' in tour && !('Type' in tour);
      
      const steps = isTourDTO 
        ? (tour as TourDTO).steps.map(step => ({
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
          }))
        : (tour as TourEntity).steps.map((step: any) => {
            // Legacy path for TourEntity - find first CSSSelector type
            const cssSelector = step.cssSteps?.find((cs: any) => cs.type === "CSSSelector")?.cssSelector || undefined;
            
            return {
              element: cssSelector,
              popover: cssSelector ? {
                title: step.title ?? undefined,
                description: step.description ?? undefined,
                side: step.side?.toLowerCase() as any,
                align: step.align?.toLowerCase() as any,
              } : {
                title: step.title ?? undefined,
                description: step.description ?? undefined,
              }
            };
          });

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

      return () => driverObj.destroy();
    }, [tour, autoStart]);

    return null;
  }
);

export default TourComponent;
