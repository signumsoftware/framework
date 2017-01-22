
declare namespace BPMN {

    interface Options {
        container?: HTMLElement;
        width?: number | string;
        height?: number | string;
        moddleExtensions?: any;
        modules?: any[];
        additionalModules?: any[];
        keyboard?: {
            bindTo?: Node;
        }
    }

    interface SaveOptions {
        format?: boolean;
        preamble?: boolean;
    }

    interface Definitions {
    }

    interface Event {

    }

    interface DoubleClickEvent extends Event {
        element: DiElement;
        gfx: Gfx;
        originalEvent: MouseEvent;

        type: string;
        stopPropagation(): void;
        preventDefault(): void;
    }

    interface AddClickEvent extends Event {
        element: DiElement;
    }

    interface PasteEvent extends Event {
        createdElements: { [oldId: string]: CreatedElement };
        descriptor: Descriptor;
    }

    interface CreatedElement {
        descriptor: Descriptor;
        element: DiElement;
    }

    interface Descriptor {
        id: string;
        name: string;
        type: string;
    }

    interface DiElement {
        attachers: any[];
        businessObject: ModdleElement;
        type: string;
        id: string;
        host: any;
        parent: DiElement;
        label: DiElement;
        colapsed: boolean;
        hidden: boolean;
        width: number;
        height: number;
        x: number;
        y: number;
    }

    interface ModdleElement {
        id: string;
        parent: ModdleElement;
        di: DiElement;
        name: string;
        $type: string;
        lanes: ModdleElement[];
    }

    interface ConnectionModdleElemnet extends ModdleElement {
        sourceRef: ModdleElement;
        targetRef: ModdleElement;
    }

    interface Gfx {
    }

    interface DiModule {
        get(elementId: string): BPMN.DiElement;
    }
}

declare module 'bpmn-js/lib/Viewer' {

    class Viewer {
        constructor(options: BPMN.Options)
        importXML(xml: string, done: (error: string, warning: string[]) => void): void;
        saveXML(options: BPMN.SaveOptions, done: (error: string, xml: string) => void): void;
        saveSVG(options: BPMN.SaveOptions, done: (error: string, svgStr: string) => void): void;
        importDefinitions(definition: BPMN.Definitions, done: (error: string) => void): void;
        getModules(): void;
        destroy(): void;
        on(event: string, callback: (obj: BPMN.Event) => void, target?: BPMN.DiElement): void;
        on(event: string, priority: number, callback: (obj: BPMN.Event) => void, target?: BPMN.DiElement): void;
        off(event: string, callback: () => void): void;
        get(module: string): BPMN.DiModule;
        _emit(event: string, element: Object): void;
    }

    export = Viewer;
}

declare module 'bpmn-js/lib/Modeler' {
    import Viewer = require("bpmn-js/lib/Viewer");

    class Modeler extends Viewer {
        createDiagram(done: (error: string, warning: string[]) => void): void;
    }

    export = Modeler
}


declare module 'bpmn-js' {
    import Viewer = require("bpmn-js/lib/Viewer");
    export = Viewer;
}