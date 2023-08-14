namespace RP.Network;

public static partial class ClientRPC
{
	[ClientRpc]
	public static void Disconnect()
	{
		Game.LocalClient.Kick();
	}
}
