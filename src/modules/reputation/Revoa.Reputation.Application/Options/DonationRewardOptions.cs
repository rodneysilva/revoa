namespace Revoa.Reputation.Application.Options;

// Parâmetros admin-configuráveis da recompensa multi-eixo de doação/voluntariado.
// Seção "DonationReward" do appsettings. Reajustada trimestralmente pelo IPCA (Tokenomia).
public class DonationRewardOptions
{
    public const string SectionName = "DonationReward";

    // Bônus em RVM creditado ao doador via faucet (mint). 0 desativa o bônus.
    public long BonusRvm { get; set; } = 2;

    // Pontos de reputação creditados ao score do doador.
    public long ReputationPoints { get; set; } = 10;

    // Pontos de ajuda (moeda social de ajuda mútua) creditados ao doador.
    public long HelpPoints { get; set; } = 1;
}
