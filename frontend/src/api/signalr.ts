import { HubConnectionBuilder, type HubConnection } from "@microsoft/signalr";

// Constroi a conexão com o hub /hubs/community. O hub usa o parâmetro
// `comunidadeId` da query para incluir a conexão no grupo da comunidade.
export function buildCommunityHub(
  comunidadeId: string,
  token: string | null
): HubConnection {
  return new HubConnectionBuilder()
    .withUrl(`/hubs/community?comunidadeId=${encodeURIComponent(comunidadeId)}`, {
      accessTokenFactory: () => token ?? "",
    })
    .withAutomaticReconnect()
    .build();
}
