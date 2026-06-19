using System.Linq;

namespace Maui3DApp.Models;

// Произвольный многоугольник (вид сверху, мировые координаты X/Z).
// Прямоугольники тоже хранятся как многоугольники (4 вершины) — это
// позволяет в одной модели представить и обычные ступени/площадки,
// и треугольные/четырёхугольные забежные ступени.
public record StairShape(IReadOnlyList<(float X, float Z)> Points);

public static class StairsLayout
{
    // Делит count на нижний/верхний пролёт.
    // usable = count минус площадка (middle=0, -1) или забежные ступени (middle>0, -middle).
    // База: floor -> нижний, ceil -> верхний (при нечётном usable верхний получает на 1 больше).
    // diff сдвигает ступени с нижнего пролёта на верхний.
    private static (int countLower, int countUpper) SplitCounts(int count, int diff, int middle)
    {
        int consumed   = middle == 0 ? 1 : middle;
        int usable     = count - consumed;
        int baseLower  = usable / 2;              // floor для неотрицательных int
        int baseUpper  = usable - baseLower;        // ceil
        int countLower = Math.Max(0, baseLower - diff);
        int countUpper = Math.Max(0, baseUpper + diff);
        return (countLower, countUpper);
    }

    public static List<StairShape> Build(
        float w, float h, float d,
        int count, int mode, int diff, int middle)
    {
        var shapes = new List<StairShape>();

        void Add(float x, float z, float rw, float rd)
            => shapes.Add(new StairShape(new[]
            {
                (x, z), (x + rw, z), (x + rw, z + rd), (x, z + rd),
            }));

        void AddPoly(IEnumerable<(float X, float Z)> points)
            => shapes.Add(new StairShape(points.ToList()));

        if (mode == 0)
        {
            for (int i = 0; i < count; i++)
                Add(-w / 2, -i * d, w, d);
            return shapes;
        }

        if (mode == 1 || mode == 2)
        {
            int mirror = mode == 1 ? 1 : -1;

            float leftX  = -w / 2 * mirror - w / 2;
            float rightX =  w / 2 * mirror - w / 2;

            // Центральный блок: при middle>0 — subSteps=middle/2 забежных
            // ступеней на каждую сторону. При subSteps=1 это просто целый
            // квадрат (как было). При subSteps>1 — веер треугольных/
            // четырёхугольных ступеней из общего угла P (см. FanWedgePolygons).
            if (middle > 0)
            {
                int subSteps = middle / 2;
                if (subSteps == 1)
                {
                    Add(leftX,  -w / 2, w, w);
                    Add(rightX, -w / 2, w, w);
                }
                else
                {
                    float px = rightX; // общий угол P — общая внутренняя грань
                    float pz = -w / 2;
                    foreach (var poly in FanWedgePolygons(w, subSteps, -1, px, pz))
                        AddPoly(poly);
                    foreach (var poly in FanWedgePolygons(w, subSteps, 1, px, pz))
                        AddPoly(poly);
                }
            }
            else
            {
                Add(-w, -w / 2, 2 * w, w);
            }

            float startZ = -(w / 2 + d / 2);

            var (countLower, countUpper) = SplitCounts(count, diff, middle);

            for (int i = 0; i < countLower; i++)
                Add(leftX, startZ - i * d, w, d);

            for (int i = 0; i < countUpper; i++)
                Add(rightX, startZ - i * d, w, d);

            return shapes;
        }

        if (mode == 3 || mode == 4)
        {
            int sideSign = mode == 3 ? 1 : -1;

            float halfW = w / 2;
            float halfD = d / 2;

            // Центральный квадрат либо два winder-квадрата при middle=2.
            // turnExtent — насколько площадка/винты "съедают" место у каждого пролёта
            // (половина ширины ступени для обычной площадки, целая — для забежных).
            float turnExtent;
            if (middle == 2)
            {
                Add(-w / 2, -w, w, w);
                Add(sideSign > 0 ? 0 : -w, -w / 2, w, w);
                turnExtent = w;
            }
            else
            {
                Add(-w / 2, -w / 2, w, w);
                turnExtent = halfW;
            }

            var (countLower, countUpper) = SplitCounts(count, diff, middle);

            float backStartZ = -(turnExtent + halfD + d / 2);
            for (int i = 0; i < countLower; i++)
                Add(-w / 2, backStartZ - i * d, w, d);

            float sideStartX = sideSign * (turnExtent + d / 2) - d / 2;
            for (int i = 0; i < countUpper; i++)
                Add(sideStartX + sideSign * i * d, -w / 2, d, w);

            return shapes;
        }

        return shapes;
    }

    // Веер забежных ступеней одного winder-квадрата стороны w. Общий угол —
    // точка P = (px, pz). "Входная" сторона квадрата направлена по оси X со
    // знаком entrySign, "выходная" — всегда в +Z. Делит прямой угол (90°) на
    // subSteps равных секторов; если граница 45° (диагональ квадрата) попадает
    // строго внутрь сектора, добавляет внешний угол квадрата как 4-ю вершину
    // (получается четырёхугольник вместо треугольника).
    private static List<List<(float X, float Z)>> FanWedgePolygons(
        float w, int subSteps, int entrySign, float px, float pz)
    {
        var result = new List<List<(float X, float Z)>>();
        float angleStep = 90f / subSteps;

        (float u, float v) Boundary(float deg)
        {
            float rad = deg * (float)Math.PI / 180f;
            return deg <= 45f
                ? (w, w * (float)Math.Tan(rad))
                : (w / (float)Math.Tan(rad), w);
        }

        for (int i = 0; i < subSteps; i++)
        {
            float a0 = i * angleStep;
            float a1 = (i + 1) * angleStep;
            var b0 = Boundary(a0);
            var b1 = Boundary(a1);

            var local = new List<(float u, float v)> { (0, 0), b0 };
            if (a0 < 45f && a1 > 45f) local.Add((w, w)); // сектор накрывает внешний угол
            local.Add(b1);

            result.Add(local.Select(p => (px + entrySign * p.u, pz + p.v)).ToList());
        }
        return result;
    }
}