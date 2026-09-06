import { HubConnectionBuilder, type HubConnection } from "@microsoft/signalr";

// Constroi a conexão com o hub /hubs/community. O hub usa o parâmetro
// `communityId` da query para incluir a conexão no grupo da comunidade.
export function buildCommunityHub(
  communityId: string,
  token: string | null
): HubConnection {
  return new HubConnectionBuilder()
    .withUrl(`/hubs/community?communityId=${encodeURIComponent(communityId)}`, {
      accessTokenFactory: () => token ?? "",
    })
    .withAutomaticReconnect()
    .build();
}
