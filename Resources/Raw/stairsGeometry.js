// stairsGeometry.js
//
// Общая чистая геометрия для забежных (винтовых) ступеней и 2D-схемы
// лестницы. НЕ зависит ни от Three.js, ни от DOM/canvas — поэтому может
// быть протестирована напрямую через Vitest, без браузера и без WebView.
//
// Подключается как обычный <script src="stairsGeometry.js"></script>
// (НЕ type="module") — функции попадают в глобальную область видимости,
// ровно как раньше, когда были инлайн внутри cube.html/stairs2d.html.
// Дополнительно отдаётся через module.exports для Node/Vitest.

// Делит count на нижний/верхний пролёт.
// usable = count минус площадка (middle=0 -> -1) или забежные ступени (-middle).
// База: floor -> нижний, ceil -> верхний (при нечётном usable верхний получает на 1 больше).
// diff сдвигает ступени с нижнего пролёта на верхний.
function splitCounts(count, diff, middle) {
    const consumed = middle === 0 ? 1 : middle;
    const usable = count - consumed;
    const baseLower = Math.floor(usable / 2);
    const baseUpper = Math.ceil(usable / 2);
    return {
        countLower: Math.max(0, baseLower - diff),
        countUpper: Math.max(0, baseUpper + diff),
    };
}

// Веер забежных ступеней одного winder-квадрата стороны w. Общий угол —
// точка P = (px, pz). "Входная" сторона квадрата направлена по оси X со
// знаком entrySign, "выходная" — всегда в +Z. Делит прямой угол (90°) на
// subSteps равных секторов; если граница 45° (диагональ квадрата) попадает
// строго внутрь сектора (а не точно на стык секторов), добавляет внешний
// угол квадрата как 4-ю вершину (получается четырёхугольник вместо
// треугольника). Возвращает массив многоугольников в мировых координатах:
// [ [[x,z],[x,z],...], ... ] — по одному многоугольнику на сектор.
//
// Раньше этот алгоритм был продублирован в cube.html (внутри addFanSteps)
// и в stairs2d.html. Теперь это единственное место с этой математикой —
// обе сцены (3D и 2D) используют одну и ту же функцию.
function fanWedgePolygons(w, subSteps, entrySign, px, pz) {
    const angleStep = 90 / subSteps;
    const boundary = (deg) => {
        const rad = deg * Math.PI / 180;
        return deg <= 45
            ? { u: w, v: w * Math.tan(rad) }
            : { u: w / Math.tan(rad), v: w };
    };
    const polys = [];
    for (let i = 0; i < subSteps; i++) {
        const a0 = i * angleStep;
        const a1 = (i + 1) * angleStep;
        const b0 = boundary(a0);
        const b1 = boundary(a1);
        const pts = [[0, 0], [b0.u, b0.v]];
        if (a0 < 45 && a1 > 45) pts.push([w, w]); // сектор строго накрывает диагональ
        pts.push([b1.u, b1.v]);
        polys.push(pts.map(([u, v]) => [px + entrySign * u, pz + v]));
    }
    return polys;
}

// Индекс по высоте (Y) для i-й ступени веера из subSteps штук.
// reverseHeight=false: ступень i=0 — самая нижняя в группе.
// reverseHeight=true:  порядок переворачивается — i=0 становится самой
// верхней. Используется, когда вторая группа веера "поднимается" в
// обратном направлении относительно обхода секторов (см. вторую вызов
// addFanSteps в П-образной лестнице в cube.html).
//
// Именно эта строка раньше была источником бага с перепутанными высотами
// ступеней веера в П-образной лестнице — тест ниже фиксирует правильное
// поведение как регрессию.
function fanYIndex(subSteps, i, reverseHeight) {
    return reverseHeight ? (subSteps - 1 - i) : i;
}

/**
 * Строит плоский (2D, вид сверху) список фигур лестницы для заданных
 * параметров. Каждая фигура — либо прямоугольник {x,z,w,d,label}, либо
 * многоугольник {points:[[x,z],...],label} (для забежных вееров).
 * Чистая функция: не трогает canvas/DOM, только геометрия — поэтому
 * тестируется напрямую без рендеринга.
 */
function buildRects({ w, h, d, count, mode, diff, middle }) {
    const rects = [];

    const add = (x, z, rw, rd, label) =>
        rects.push({ x, z, w: rw, d: rd, label });

    const addPoly = (points, label) =>
        rects.push({ points, label });

    if (mode === 0) {
        for (let i = 0; i < count; i++) {
            add(-w / 2, -i * d, w, d, i === 0 ? "1" : "");
        }
        return rects;
    }

    if (mode === 1 || mode === 2) {
        const mirror = mode === 1 ? 1 : -1;
        const leftX  = -w / 2 * mirror - w / 2;
        const rightX =  w / 2 * mirror - w / 2;

        if (middle > 0) {
            const subSteps = middle / 2;
            if (subSteps === 1) {
                add(leftX,  -w / 2, w, w, "c1");
                add(rightX, -w / 2, w, w, "c2");
            } else {
                const Px = 0;
                const Pz = -w / 2;
                fanWedgePolygons(w, subSteps, -mirror, Px, Pz).forEach((pts, i) => addPoly(pts, "c1_" + i));
                fanWedgePolygons(w, subSteps, mirror, Px, Pz).forEach((pts, i) => addPoly(pts, "c2_" + i));
            }
        } else {
            add(-w, -w / 2, 2 * w, w, "c");
        }

        const startZ = -(w / 2 + d);
        const { countLower, countUpper } = splitCounts(count, diff, middle);

        for (let i = 0; i < countLower; i++) {
            add(leftX, startZ - i * d, w, d, "");
        }
        for (let i = 0; i < countUpper; i++) {
            add(rightX, startZ - i * d, w, d, "");
        }
        return rects;
    }

    if (mode === 3 || mode === 4) {
        const sideSign = mode === 3 ? 1 : -1;
        const halfW = w / 2;
        const halfD = d / 2;

        let turnExtent;
        if (middle === 2) {
            add(-w / 2, -w, w, w, "c1");
            add(sideSign > 0 ? 0 : -w, -w / 2, w, w, "c2");
            turnExtent = w;
        } else if (middle === 3) {
            const Px = sideSign > 0 ? halfW : -halfW;
            const Pz = -halfW;
            const entrySign = -sideSign;
            fanWedgePolygons(w, 3, entrySign, Px, Pz)
                .forEach((pts, i) => addPoly(pts, (sideSign > 0 ? "c1_" : "c2_") + i));
            turnExtent = halfW;
        } else {
            add(-w / 2, -w / 2, w, w, "c");
            turnExtent = halfW;
        }

        const { countLower, countUpper } = splitCounts(count, diff, middle);

        const backStartZ = -(turnExtent + halfD + d / 2);
        for (let i = 0; i < countLower; i++) {
            add(-w / 2, backStartZ - i * d, w, d, "");
        }

        const sideStartX = sideSign * (turnExtent + d / 2) - d / 2;
        for (let i = 0; i < countUpper; i++) {
            add(sideStartX + sideSign * i * d, -w / 2, d, w, "");
        }
        return rects;
    }

    return rects;
}

if (typeof module !== "undefined" && module.exports) {
    module.exports = { splitCounts, fanWedgePolygons, fanYIndex, buildRects };
}
