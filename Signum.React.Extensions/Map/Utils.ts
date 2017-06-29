import * as d3 from "d3"

export interface Point {
    x?: number; //Realy not nullable, but d3.d.ts
    y?: number; //Realy not nullable, but d3.d.ts
}


export interface Rectangle extends Point {
    width: number;
    height: number;
}

export function colorScale(max: number): d3.ScaleLinear<string, string> {
    return d3.scaleLinear<string>()
        .domain([0, max / 4, max])
        .range(["green", "gold", "red"]);

}

export function colorScaleSqr(max: number): d3.ScalePower<string, string>{
    return d3.scaleSqrt<string>()
        .domain([0, max / 4, max])
        .range(["green", "gold", "red"]);

}


export function calculatePoint(rectangle: Rectangle, point: Point): Point {

    const vector = { x: point.x! - rectangle.x!, y: point.y! - rectangle.y! };

    const v2 = { x: rectangle.width / 2, y: rectangle.height / 2 };

    const ratio = getRatio(vector, v2);

    return { x: rectangle.x! + vector.x * (ratio || 0), y: rectangle.y! + vector.y * (ratio || 0) };
}


function getRatio(vOut: Point, vIn: Point) {

    const vOut2 = { x: vOut.x, y: vOut.y };

    if (vOut2.x! < 0)
        vOut2.x = -vOut2.x!;

    if (vOut2.y! < 0)
        vOut2.y = -vOut2.y!;

    if (vOut2.x == 0 && vOut2.y == 0)
        return undefined;

    if (vOut2.x == 0)
        return vIn.y! / vOut2.y!;

    if (vOut2.y == 0)
        return vIn.x! / vOut2.x!;

    return Math.min(vIn.x! / vOut2.x!, vIn.y! / vOut2.y!);
}



export function wrap(textElement: SVGTextElement, width: number) {
    const text = d3.select<SVGTextElement, void>(textElement);
    const words: string[] = text.text().split(/\s+/).reverse();
    let word: string;

    let line: string[] = [];
    let tspan = text.text(null).append("tspan")
        .attr("x", 0)
        .attr("dy", "1.2em");

    while (word = words.pop()!) {
        line.push(word);
        tspan.text(line.join(" "));
        if ((<SVGTSpanElement>tspan.node()).getComputedTextLength() > width && line.length > 1) {
            line.pop();
            tspan.text(line.join(" "));
            line = [word];
            tspan = text.append("tspan")
                .attr("x", 0)
                .attr("dy", "1.2em").text(word);
        }
    }
}

export function forceBoundingBox<T extends d3.SimulationNodeDatum>(width : number = 0, height: number = 0) {
    var nodes : T[];
    
    function gravityDim(v: number, min: number, max: number, alpha: number): number {

        const minF = min + 100;
        const maxF = max - 100;

        const dist =
            maxF < v ? maxF - v :
                v < minF ? minF - v :
                    ((max - min)/2 - v) / 50;

        return dist * alpha * 0.4;
    }

    function force(alpha: number) {
        nodes.forEach(n => {
            n.vx = n.vx! + gravityDim(n.x!, 0, width, alpha);
            n.vy = n.vx! + gravityDim(n.y!, 0, height, alpha);
        });
    }

    (force as any).initialize = function (_ : T[]) {
        nodes = _;
    };

    return force;
}
