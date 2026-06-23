using FluentAssertions;
using Maui3DApp.Models;
using Xunit;

namespace Maui3DApp.Tests;

/// <summary>
/// Тесты для StairsCalculator.Magic().
/// "Безопасные" значения параметров подобраны так, чтобы не попадать
/// ни в один особый диапазон A и не пересекаться с проверяемой логикой.
/// </summary>
public class StairsCalculatorTests
{
    private const double SafeA  = 3000; // вне всех диапазонов Diff/Middle
    private const double SafeB  = 900;  // B < 2000
    private const double SafeC  = 0;    // не используется в Magic
    private const double SafeH1 = 1750; // даёт Nh = 175, без клампа
    private const double SafeH2 = 0;    // не используется в Magic
    private const double SafeN  = 10;

    #region Nh — высота подступёнка (h1 / N, клампится в [170, 180])

    [Theory]
    [InlineData(1700, 10, 170)]   // нижняя граница диапазона — без клампа
    [InlineData(1800, 10, 180)]   // верхняя граница диапазона — без клампа
    [InlineData(1750, 10, 175)]   // середина диапазона
    [InlineData(1000, 10, 170)]   // ниже 170 -> клампится к 170
    [InlineData(2000, 10, 180)]   // выше 180 -> клампится к 180
    [InlineData(1690, 10, 170)]   // чуть ниже границы -> клампится
    [InlineData(1810, 10, 180)]   // чуть выше границы -> клампится
    public void Nh_IsClampedToValidRange(double h1, double n, double expectedNh)
    {
        var result = StairsCalculator.Magic(SafeA, SafeB, SafeC, h1, SafeH2, n);
        result.Nh.Should().Be(expectedNh);
    }

    [Fact]
    public void Nh_DivisionByZero_N_IsClampedTo180()
    {
        // h1 / 0 даёт +Infinity (double не бросает исключение при делении на 0),
        // но клампинг (Nh > 180 -> Nh = 180) отрабатывает и для Infinity,
        // поэтому результат безопасно ограничивается 180, а не "протекает"
        // как Infinity наружу. Полезное побочное свойство текущей реализации —
        // но явного guard на N = 0 в коде всё равно нет (если N придёт
        // отрицательным, например -5, тоже стоит проверить отдельно).
        var result = StairsCalculator.Magic(SafeA, SafeB, SafeC, 1750, SafeH2, 0);
        result.Nh.Should().Be(180);
    }

    #endregion

    #region SL — зависит от B

    [Theory]
    [InlineData(0,    0)]      // граничный случай: B = 0
    [InlineData(1500, 750)]    // B < 2000 -> B / 2
    [InlineData(1999, 999.5)]  // чуть меньше границы
    [InlineData(2000, 1000)]   // ровно граница — НЕ "< 2000" -> фиксированные 1000
    [InlineData(2500, 1000)]   // B > 2000 -> фиксированные 1000
    [InlineData(5000, 1000)]
    public void SL_DependsOnB(double b, double expectedSl)
    {
        var result = StairsCalculator.Magic(SafeA, b, SafeC, SafeH1, SafeH2, SafeN);
        result.SL.Should().Be(expectedSl);
    }

    #endregion

    #region Middle / Diff — зависят от A

    [Theory]
    // Диапазон 2750–2900 -> Diff = 2, Middle не трогается (остаётся 0)
    [InlineData(2750, 2, 0)]
    [InlineData(2800, 2, 0)]
    [InlineData(2900, 2, 0)]
    // Диапазон 2500–2650 -> Diff = 3, Middle = 2
    [InlineData(2500, 3, 2)]
    [InlineData(2575, 3, 2)]
    [InlineData(2650, 3, 2)]
    // Диапазон 2250–2400 -> Diff = 1, Middle = 6
    [InlineData(2250, 1, 6)]
    [InlineData(2300, 1, 6)]
    [InlineData(2400, 1, 6)]
    public void MiddleAndDiff_AreSetForKnownARanges(double a, int expectedDiff, int expectedMiddle)
    {
        var result = StairsCalculator.Magic(a, SafeB, SafeC, SafeH1, SafeH2, SafeN);
        result.Diff.Should().Be(expectedDiff);
        result.Middle.Should().Be(expectedMiddle);
    }

    [Theory]
    // Значения A, попадающие в "дырки" между диапазонами -> Diff = null, Middle = 0.
    // Если по бизнес-логике тут должно быть другое поведение — это баг,
    // а не особенность теста.
    [InlineData(0)]
    [InlineData(2000)]
    [InlineData(2249)]   // чуть ниже 2250
    [InlineData(2401)]   // чуть выше 2400, ниже 2500
    [InlineData(2499)]
    [InlineData(2651)]   // чуть выше 2650, ниже 2750
    [InlineData(2749)]
    [InlineData(2901)]   // чуть выше 2900
    [InlineData(5000)]
    public void MiddleAndDiff_AreDefaultForAOutsideKnownRanges(double a)
    {
        var result = StairsCalculator.Magic(a, SafeB, SafeC, SafeH1, SafeH2, SafeN);
        result.Diff.Should().BeNull();
        result.Middle.Should().Be(0);
    }

    #endregion
}
