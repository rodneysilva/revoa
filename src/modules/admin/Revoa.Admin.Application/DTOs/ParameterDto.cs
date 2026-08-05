namespace Revoa.Admin.Application.DTOs;

// Visão de um parâmetro de sistema para o dashboard admin (UF-30).
//   Key:   identificador do parâmetro (ex.: "DonationReward.BonusRvm").
//   Label: rótulo amigável em pt-BR exibido no dashboard.
//   Type:  tipo de entrada para o front ("long" | "int" | "decimal" | "bool").
//   Value: valor efetivo ATUAL (do store ou do default) — number/bool pronto p/ o input.
public sealed record ParameterDto(string Key, string Label, string Type, object? Value);
