import { describe, it, expect } from "vitest";
import { splitCounts, fanWedgePolygons, fanYIndex, buildRects } from "../Resources/Raw/stairsGeometry.js";

function expectPoint(actual, expected, precision = 10) {
    expect(actual[0]).toBeCloseTo(expected[0], precision);
    expect(actual[1]).toBeCloseTo(expected[1], precision);
}

// ─────────────────────────────────────────────
// splitCounts
// ─────────────────────────────────────────────

describe("splitCounts", () => {
    it("middle=0 (площадка) забирает 1 ступень из usable", () => {
        // count=9, diff=0, middle=0 -> consumed=1, usable=8 -> 4/4
        expect(splitCounts(9, 0, 0)).toEqual({ countLower: 4, countUpper: 4 });
    });

    it("нечётный usable отдаёт лишнюю ступень верхнему пролёту", () => {
        // count=10, diff=0, middle=0 -> consumed=1, usable=9 -> floor=4, ceil=5
        expect(splitCounts(10, 0, 0)).toEqual({ countLower: 4, countUpper: 5 });
    });

    it("middle=2 (два винта/площадки) забирает 2 ступени", () => {
        // count=10, diff=0, middle=2 -> usable=8 -> 4/4
        expect(splitCounts(10, 0, 2)).toEqual({ countLower: 4, countUpper: 4 });
    });

    it("middle=6 (веер, 3 ступени на сторону) забирает 6 ступеней", () => {
        // count=10, diff=0, middle=6 -> usable=4 -> 2/2
        expect(splitCounts(10, 0, 6)).toEqual({ countLower: 2, countUpper: 2 });
    });

    it("diff сдвигает ступени с нижнего пролёта на верхний", () => {
        // count=10, middle=0 -> usable=9 -> база 4/5, diff=2 -> 2/7
        expect(splitCounts(10, 2, 0)).toEqual({ countLower: 2, countUpper: 7 });
    });

    it("countLower не уходит в минус при большом diff", () => {
        // база 4/5, diff=10 -> countLower было бы -6, клампится к 0
        expect(splitCounts(10, 10, 0)).toEqual({ countLower: 0, countUpper: 15 });
    });
});

// ─────────────────────────────────────────────
// fanWedgePolygons
// ─────────────────────────────────────────────

describe("fanWedgePolygons", () => {
    it("subSteps=1 -> единственный сектор - это весь квадрат (4 вершины)", () => {
        const polys = fanWedgePolygons(10, 1, 1, 0, 0);
        expect(polys[0]).toHaveLength(4);
        expectPoint(polys[0][0], [0, 0]);
        expectPoint(polys[0][1], [10, 0]);
        expectPoint(polys[0][2], [10, 10]);
        expectPoint(polys[0][3], [0, 10]);
    });

    it("subSteps=2 -> граница ровно на 45°, оба сектора - треугольники без 4-й вершины", () => {
        // При чётном делении на 45° граница секторов совпадает с диагональю
        // квадрата ровно по стыку, а не "внутри" сектора — поэтому условие
        // (a0 < 45 && a1 > 45) не срабатывает ни для одного сектора.
        const polys = fanWedgePolygons(10, 2, 1, 0, 0);
        expect(polys[0]).toHaveLength(3);
        expect(polys[1]).toHaveLength(3);

        expectPoint(polys[0][0], [0, 0]);
        expectPoint(polys[0][1], [10, 0]);
        expectPoint(polys[0][2], [10, 10]);

        expectPoint(polys[1][0], [0, 0]);
        expectPoint(polys[1][1], [10, 10]);
        expectPoint(polys[1][2], [0, 10]);
    });

    it("subSteps=3 -> средний сектор пересекает диагональ и получает 4-ю вершину", () => {
        // Сектор 1 (30°-60°) строго накрывает границу 45°, поэтому должен
        // содержать внешний угол квадрата [w,w] как отдельную вершину.
        // Крайние секторы (0-30° и 60-90°) остаются треугольниками.
        const polys = fanWedgePolygons(10, 3, 1, 0, 0);
        expect(polys).toHaveLength(3);
        expect(polys[0]).toHaveLength(3); // треугольник
        expect(polys[1]).toHaveLength(4); // четырёхугольник — содержит диагональ
        expect(polys[2]).toHaveLength(3); // треугольник
        expect(polys[1]).toContainEqual([10, 10]); // угол квадрата присутствует
    });

    it("entrySign=-1 зеркалит координату X сектора", () => {
        const positive = fanWedgePolygons(10, 1, 1, 0, 0);
        const mirrored = fanWedgePolygons(10, 1, -1, 0, 0);
        // Z-координата (вторая) не меняется при смене entrySign
        expect(mirrored[0]).toHaveLength(4);
        positive[0].forEach((pt, i) => {
            expect(mirrored[0][i][0]).toBeCloseTo(-pt[0], 10);
            expect(mirrored[0][i][1]).toBeCloseTo(pt[1], 10);
        });
    });

    it("px/pz сдвигают все вершины на одинаковый офсет", () => {
        const base = fanWedgePolygons(10, 1, 1, 0, 0);
        const shifted = fanWedgePolygons(10, 1, 1, 100, -50);
        expect(shifted[0]).toEqual(
            base[0].map(([x, z]) => [x + 100, z - 50])
        );
    });
});

// ─────────────────────────────────────────────
// fanYIndex — регрессия на баг с перепутанными высотами
// ─────────────────────────────────────────────

describe("fanYIndex", () => {
    it("без reverseHeight ступень i=0 - самая нижняя", () => {
        expect(fanYIndex(3, 0, false)).toBe(0);
        expect(fanYIndex(3, 1, false)).toBe(1);
        expect(fanYIndex(3, 2, false)).toBe(2);
    });

    it("с reverseHeight=true порядок переворачивается (i=0 - самая верхняя)", () => {
        // Это та самая логика, из-за которой раньше высоты веера во второй
        // группе П-образной лестницы шли в обратном порядке.
        expect(fanYIndex(3, 0, true)).toBe(2);
        expect(fanYIndex(3, 1, true)).toBe(1);
        expect(fanYIndex(3, 2, true)).toBe(0);
    });

    it("сумма индексов по группе всегда покрывает 0..subSteps-1 без дублей", () => {
        const subSteps = 4;
        for (const reverse of [false, true]) {
            const indices = Array.from({ length: subSteps }, (_, i) => fanYIndex(subSteps, i, reverse));
            expect([...indices].sort((a, b) => a - b)).toEqual([0, 1, 2, 3]);
        }
    });
});

// ─────────────────────────────────────────────
// buildRects — регрессия на баг с overlap startZ
// ─────────────────────────────────────────────

describe("buildRects", () => {
    it("mode=0: одна цепочка прямоугольников, первый помечен '1'", () => {
        const rects = buildRects({ w: 80, h: 20, d: 30, count: 4, mode: 0, diff: 0, middle: 0 });
        expect(rects).toHaveLength(4);

        expect(rects[0].label).toBe("1");

        expect(rects[0].x).toBeCloseTo(-40);
        expect(rects[0].z).toBeCloseTo(0);

        expect(rects[1].z).toBeCloseTo(-30);
        expect(rects[2].z).toBeCloseTo(-60);
        expect(rects[3].z).toBeCloseTo(-90);
    });

    it("mode=1, middle=0: пролёты примыкают к площадке, но не пересекают её", () => {
        const rects = buildRects({ w: 80, h: 20, d: 30, count: 5, mode: 1, diff: 0, middle: 0 });

        expect(rects).toEqual([
            { x: -80, z: -40,  w: 160, d: 80, label: "c" },
            { x: -80, z: -70,  w: 80,  d: 30, label: "" },
            { x: -80, z: -100, w: 80,  d: 30, label: "" },
            { x: 0,   z: -70,  w: 80,  d: 30, label: "" },
            { x: 0,   z: -100, w: 80,  d: 30, label: "" },
        ]);

        // Регрессия на баг с overlap: дальняя грань первой ступени пролёта
        // (z + d) должна ровно совпадать с ближней гранью площадки (z),
        // а не заходить за неё.
        const platform = rects[0];
        const firstLowerStep = rects[1];
        expect(firstLowerStep.z + firstLowerStep.d).toBe(platform.z);
    });

    it("mode=3, middle=0: пролёты примыкают к площадке и по Z, и по X, без пересечения", () => {
        const rects = buildRects({ w: 80, h: 20, d: 30, count: 5, mode: 3, diff: 0, middle: 0 });

        expect(rects).toEqual([
            { x: -40, z: -40,  w: 80, d: 80, label: "c" },
            { x: -40, z: -70,  w: 80, d: 30, label: "" },
            { x: -40, z: -100, w: 80, d: 30, label: "" },
            { x: -70, z: -40,  w: 30, d: 80, label: "" },
            { x: -100,z: -40,  w: 30, d: 80, label: "" },
        ]);

        const platform = rects[0];
        const backStep = rects[1];
        const sideStep = rects[3];

        // Задний пролёт примыкает по Z
        expect(backStep.z + backStep.d).toBe(platform.z);
        // Боковой пролёт примыкает по X
        expect(sideStep.x + sideStep.w).toBe(platform.x);
    });

    it("mode=1, middle=6 (веер): центральная фигура состоит из 2*subSteps многоугольников", () => {
        const rects = buildRects({ w: 80, h: 20, d: 30, count: 10, mode: 1, diff: 0, middle: 6 });
        const fanPieces = rects.filter(r => r.points);
        expect(fanPieces).toHaveLength(6); // 2 группы * 3 сектора
    });
});
