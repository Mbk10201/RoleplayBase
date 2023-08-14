namespace RP.UI.Helpers;

[Library( "stepper" )]
public class StepperPanel : Panel
{
	protected List<HistoryItem> Cache = new();
	public HistoryItem CurrentStep { get; set; }
	Stack<HistoryItem> Back = new();
	Stack<HistoryItem> Forward = new();
	public Panel StepperCanvas { get; set; }

	public class HistoryItem
	{
		public IStep StepRef { get; set; } = null;
		public Panel Panel;
	}

	public override void OnTemplateSlot( INode element, string slotName, Panel panel )
	{
		if ( slotName == "stepper-canvas" )
		{
			StepperCanvas = panel;
			return;
		}

		base.OnTemplateSlot( element, slotName, panel );
	}

	public void Navigate( string name )
	{
		/*if( CurrentStep is not null && CurrentStep.StepRef.IsLast)
		{
			OnStepsFinished();
			return;
		}*/
		
		var selection = StepperTargetAttribute.FindValidTarget( name );

		if (selection == null ) {
			NotFound( name );
			return;
		}

		if( selection.IsLocked )
		{
			Locked( name );
			return;
		}

		if ( StepperCanvas == null )
		{
			Log.Info( "Make Canvas This" );
			StepperCanvas = this;
		}

		PlaySound( "ui.itemselect" );
		Forward.Clear();

		/// <summary>
		/// Get the current step, we check if valid, if so we store it on the previous page cache
		/// and after we destroy the current
		/// </summary>
		if ( CurrentStep != null )
		{
			Back.Push( CurrentStep );
			CurrentStep.Panel.AddClass( "hidden" );
			CurrentStep = null;
		}

		/// <summary>
		/// We check if the wanted step exist on cache, if not we initialise it on the cache and 
		/// create is panel reference
		/// </summary>
		var cached = Cache.FirstOrDefault( x => x.StepRef.Identifier == name );
		if ( cached != null )
		{
			cached.Panel.RemoveClass( "hidden" );
			CurrentStep = cached;
			CurrentStep.Panel.Parent = StepperCanvas;
		}
		else
		{
			var panel = TypeLibrary.Create<Panel>( selection.TargetType );
			panel.AddClass( "navigator-body" );

			CurrentStep = new HistoryItem { StepRef = selection, Panel = panel };
			CurrentStep.Panel.Parent = StepperCanvas;

			Cache.Add( CurrentStep );
		}


		if ( CurrentStep == null ) return;

		OnStepSwitch( CurrentStep.StepRef );
		var previousUrl = CurrentStep.StepRef.Identifier;
		CurrentStep.Panel.SetProperty( "referrer", previousUrl );
	}

	public virtual void OnStepSwitch( IStep step )
	{

	}

	/// <summary>
	/// Navigate to a URL
	/// </summary>
	[PanelEvent]
	public bool NavigateEvent( string name )
	{
		Navigate( name );
		return false;
	}

	internal bool CurrentStepMatches( string name )
	{
		return CurrentStep.StepRef?.Identifier == name;
	}

	protected virtual void NotFound( string name )
	{
		if ( name == null ) return;
		Log.Warning( $"Stepper step Not Found: {name}" );
	}

	protected virtual void Locked( string name )
	{
		if ( name == null ) return;
		Log.Info( Game.LocalClient );
		//NotificationSystem.Warning( "This step is locked !" );
	}

	/// <summary>
	/// 
	/// </summary>
	[PanelEvent( "navigate_return" )]
	public bool NavigateReturnEvent()
	{
		if ( !Back.TryPop( out var result ) )
			return true;

		Switch( result );
		return false;
	}

	protected override void OnBack( PanelEvent e )
	{
		if ( GoBack() )
		{
			e.StopPropagation();
		}
	}

	protected override void OnForward( PanelEvent e )
	{
		if ( GoForward() )
		{
			e.StopPropagation();
		}
	}

	public virtual bool GoBack()
	{
		if ( !Back.TryPop( out var result ) )
		{
			// TODO - only play this sound if we didn't pass to a parent
			PlaySound( "ui.navigate.deny" );
			return false;
		}

		if ( !Cache.Contains( result ) )
		{
			return GoBack();
		}

		PlaySound( "ui.navigate.back" );

		if ( CurrentStep != null )
			Forward.Push( CurrentStep );

		Switch( result );
		return true;
	}

	public virtual bool GoForward()
	{
		if ( !Forward.TryPop( out var result ) )
		{
			PlaySound( "ui.navigate.deny" );
			return false;
		}

		if ( !Cache.Contains( result ) )
		{
			return GoForward();
		}

		PlaySound( "ui.navigate.forward" );

		if ( CurrentStep != null )
			Back.Push( CurrentStep );

		Switch( result );
		return true;
	}

	public virtual async Task OnStepsFinished()
	{
		await ShowLoader( 1500 );
	}

	public async void GoBackStep()
	{
		var selection = StepperTargetAttribute.FindValidTarget( CurrentStep.StepRef.Id - 1 );
		Navigate( selection.Identifier );
	}

	public async void GoNextStep()
	{
		if ( CurrentStep.StepRef.IsLast )
		{
			await OnStepsFinished();
			return;
		}

		var selection = StepperTargetAttribute.FindValidTarget( CurrentStep.StepRef.Id + 1 );
		if ( selection.IsLocked)
		{
			selection.IsLocked = false;
			//NotificationSystem.Success("You have unlocked a new stap, congrats !");
			await ShowLoader( 1500 );
		}
		Navigate( selection.Identifier );
	}

	public async Task ShowLoader(int ms = 2500)
	{
		var tmp = StepperCanvas.AddChild<StepperLoader>( );
		await GameTask.Delay( ms );
		tmp.Delete();
	}

	void Switch( HistoryItem item )
	{
		if ( CurrentStep == item ) return;

		CurrentStep?.Panel.AddClass( "hidden" );
		CurrentStep = null;

		CurrentStep = item;
		CurrentStep?.Panel.RemoveClass( "hidden" );
	}
}
