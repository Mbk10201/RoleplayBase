using System.Threading.Tasks;

namespace RP.UI.Helpers;

/// <summary>
/// A panel that will navigate to an href but also have .active class if href is active
/// </summary>
[ClassName( "a" )]
[Alias( "navlink" )]
public class Anchor : Panel
{
	NavigatorPanel Navigator;
	public string HRef { get; set; }
	public string Match { get; set; }

	public override void OnParentChanged()
	{
		base.OnParentChanged();

		Navigator = Ancestors.OfType<NavigatorPanel>().FirstOrDefault();
	}

	protected override void OnClick( MousePanelEvent e )
	{
		if ( e.Button == "mouseleft" )
		{
			CreateEvent( "navigate", HRef );
		}
	}

	public override void Tick()
	{
		base.Tick();
		var active = Navigator?.CurrentUrlMatches( Match ?? HRef ) ?? false;
		SetClass( "active", active );
	}
}
