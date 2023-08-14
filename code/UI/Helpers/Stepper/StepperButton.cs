namespace RP.UI.Helpers;

/// <summary>
/// A button that will navigate to an href but also have .active class if href is active
/// </summary>
[Library( "steplink" )]
public class StepperButton : Button
{
	StepperPanel Stepper;
	public string Identifier { get; set; }

	public override void OnParentChanged()
	{
		base.OnParentChanged();

		Stepper = Ancestors.OfType<StepperPanel>().FirstOrDefault();
	}

	public override void SetProperty( string name, string value )
	{
		base.SetProperty( name, value );

		if ( name == "identifier" )
		{
			Identifier = value;
		}
	}

	protected override void OnMouseDown( MousePanelEvent e )
	{
		if ( e.Button == "mouseleft" )
		{
			CreateEvent( "navigate", Identifier );
			e.StopPropagation();
		}
	}
	 
	public override void Tick()
	{
		base.Tick();

		var active = Stepper?.CurrentStepMatches( Identifier ) ?? false;
		SetClass( "active", active );
	}
}
