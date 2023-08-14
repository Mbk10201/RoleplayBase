namespace RP.UI.Helpers;

[Library( "button" ), StyleSheet]
public class Button : Panel
{
	/// <summary>
	/// The <see cref="Label"/> that displays <see cref="Text"/>.
	/// </summary>
	protected Label TextLabel;

	/// <summary>
	/// The <see cref="IconPanel"/> that displays <see cref="Icon"/>.
	/// </summary>
	protected IconPanel IconPanel;

	protected bool Disabled;

	public Button()
	{
		AddClass( "button" );
	}

	/// <summary>
	/// Text for the button.
	/// </summary>
	public string Text
	{
		get => TextLabel?.Text;
		set
		{
			TextLabel ??= Add.Label( value, "button-label" );
			TextLabel.Text = value;
		}
	}

	/// <summary>
	/// Deletes the <see cref="Text"/>.
	/// </summary>
	public void DeleteText()
	{
		if ( TextLabel == null ) return;

		TextLabel?.Delete();
		TextLabel = null;
	}

	/// <summary>
	/// Icon for the button.
	/// </summary>
	public string Icon
	{
		get => IconPanel?.Text;
		set
		{
			if ( string.IsNullOrEmpty( value ) )
			{
				DeleteIcon();
				return;
			}

			IconPanel ??= Add.Icon( value );
			IconPanel.Text = value;
			SetClass( "has-icon", IconPanel != null );
		}
	}

	/// <summary>
	/// Deletes the <see cref="Icon"/>.
	/// </summary>
	public void DeleteIcon()
	{
		if ( IconPanel == null ) return;

		IconPanel?.Delete();
		IconPanel = null;
		RemoveClass( "has-icon" );
	}

	/// <summary>
	/// Set the text for the button. Calls <c>Text = value</c>
	/// </summary>
	public virtual void SetText( string text )
	{
		Text = text;
	}

	/// <summary>
	/// Imitate the button being clicked.
	/// </summary>
	public void Click()
	{
		CreateEvent( new MousePanelEvent( "onclick", this, "mouseleft" ) );
	}

	protected override void OnClick( MousePanelEvent e )
	{
		return;
		if (Disabled)
			return;

		Log.Info( "OnClick()" );
		
		base.OnClick( e );
	}

	public override void SetProperty( string name, string value )
	{
		switch ( name )
		{
			case "disabled":
			{
				Log.Info( Disabled );
				Disabled = value.ToBool();
				SetClass( "disabled", value.ToBool() );
				return;
			}
		}

		base.SetProperty( name, value );
	}

	public override void SetContent( string value )
	{
		SetText( value?.Trim() ?? "" );
	}
}
