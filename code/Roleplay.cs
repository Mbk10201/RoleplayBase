using Mbk.RoleplayAPI.Player;
using static Mbk.RoleplayAPI.RoleplayAPI;

namespace RP;

[Display( Name = "Roleplay Game" ), Category( "Roleplay" ), Icon( "sports_esports" )]
public partial class Roleplay : GameManager
{
	public static Roleplay Instance => Current as Roleplay;

	public Roleplay()
	{
	}

	public override void Shutdown()
	{
		foreach ( var player in Entity.All.OfType<RoleplayPlayer>() )
		{
			_ = player.Data.TryToSave();
		}
		base.Shutdown();
	}

	public override void ClientJoined( IClient client )
	{
		base.ClientJoined( client );

		Event.Run(OnClientJoined);

		var pawn = new RoleplayPlayer();
		pawn.CreatePawn( client );
	}

	public override void ClientDisconnect( IClient client, NetworkDisconnectionReason reason )
	{
		base.ClientDisconnect( client, reason );

		Event.Run( OnClientDisconnect );

		var pawn = client.Pawn as RoleplayPlayer;

		if(pawn is not null)
		{
			_ = pawn.Data.TryToSave();
		}
	}

	[ConCmd.Server]
	public static void Test()
	{
		var player = ConsoleSystem.Caller.Pawn as RoleplayPlayer;
		Log.Info( player.Data.Money );

		//PlayerAPI.SetMoney( player.Data, 500 );

		Log.Info( player.Data.Money );
	}
}
