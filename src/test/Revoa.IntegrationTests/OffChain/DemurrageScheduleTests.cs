using FluentAssertions;
using Revoa.Demurrage.Application.Services;
using Xunit;

namespace Revoa.IntegrationTests.OffChain;

// Aritmética pura do scheduler de demurrage (Fase 4): a janela mensal (dia 1º 03:00 UTC) e
// o reajuste IPCA da taxa. Sem Mongo/HTTP — data e fórmula cravadas para o scheduler nunca
// "marcar para ontem" nem reajustar fora dos clamps.
public class DemurrageScheduleTests
{
    private static readonly TimeSpan Zero = TimeSpan.Zero;

    [Theory]
    [InlineData(9, false)]
    [InlineData(2, false)]
    [InlineData(12, false)]
    [InlineData(1, true)]
    [InlineData(4, true)]
    [InlineData(7, true)]
    [InlineData(10, true)]
    public void Quarter_start_months_are_jan_apr_jul_oct(int month, bool expected)
    {
        DemurrageSchedule.IsQuarterStart(month).Should().Be(expected);
    }

    [Fact]
    public void Next_run_from_mid_month_is_first_of_next_month()
    {
        var now = new DateTimeOffset(2026, 9, 15, 10, 0, 0, Zero);

        var next = DemurrageSchedule.NextMonthlyRunUtc(now);

        next.Should().Be(new DateTimeOffset(2026, 10, 1, 3, 0, 0, Zero));
    }

    [Fact]
    public void Next_run_after_window_on_first_is_next_month()
    {
        var now = new DateTimeOffset(2026, 9, 1, 4, 0, 0, Zero);

        var next = DemurrageSchedule.NextMonthlyRunUtc(now);

        next.Should().Be(new DateTimeOffset(2026, 10, 1, 3, 0, 0, Zero));
    }

    [Fact]
    public void Next_run_before_window_on_first_is_today()
    {
        var now = new DateTimeOffset(2026, 9, 1, 2, 59, 0, Zero);

        var next = DemurrageSchedule.NextMonthlyRunUtc(now);

        next.Should().Be(new DateTimeOffset(2026, 9, 1, 3, 0, 0, Zero));
    }

    [Fact]
    public void Next_run_rolls_over_the_year()
    {
        var now = new DateTimeOffset(2026, 12, 31, 23, 0, 0, Zero);

        var next = DemurrageSchedule.NextMonthlyRunUtc(now);

        next.Should().Be(new DateTimeOffset(2027, 1, 1, 3, 0, 0, Zero));
    }

    [Fact]
    public void Rate_adjuster_scales_by_ipca_and_rounds()
    {
        DemurrageRateAdjuster.Apply(50, 1.5).Should().Be(51); // 50,75 → 51
        DemurrageRateAdjuster.Apply(50, -20).Should().Be(40); // deflação reduz a taxa
        DemurrageRateAdjuster.Apply(50, 0).Should().Be(50);
    }

    [Fact]
    public void Rate_adjuster_clamps_to_sane_bounds()
    {
        DemurrageRateAdjuster.Apply(495, 2).Should().Be(DemurrageRateAdjuster.MaxBps); // 504,9 → 500
        DemurrageRateAdjuster.Apply(10, -5).Should().Be(DemurrageRateAdjuster.MinBps); // 9,5 → 10
    }
}
