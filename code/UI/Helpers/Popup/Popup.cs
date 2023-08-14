namespace RP.UI.Helpers;

public partial class Popup : Panel
{
	// For keyboard navigation
	public Panel PopupSource { get; set; }
	public Panel SelectedChild { get; set; }

	public PositionMode Position { get; set; }
	public float PopupSourceOffset { get; set; }

	public enum PositionMode
	{
		Left,
		LeftBottom,
		AboveLeft,
		BelowLeft,
		BelowCenter,
		BelowStretch
	}

	public Popup()
	{
		
	}

	public Popup( Panel sourcePanel, PositionMode position, float offset )
	{
		SetPositioning( sourcePanel, position, offset );
	}

	public void SetPositioning( Panel sourcePanel, PositionMode position, float offset )
	{
		Parent = sourcePanel.FindPopupPanel();
		PopupSource = sourcePanel;
		Position = position;
		PopupSourceOffset = offset;

		AllPopups.Add( this );
		AddClass( "popup-panel" );
		PositionMe();

		switch ( Position )
		{
			case PositionMode.Left:
				AddClass( "left" );
				break;

			case PositionMode.LeftBottom:
				AddClass( "left-bottom" );
				break;

			case PositionMode.AboveLeft:
				AddClass( "above-left" );
				break;

			case PositionMode.BelowLeft:
				AddClass( "below-left" );
				break;

			case PositionMode.BelowCenter:
				AddClass( "below-center" );
				break;

			case PositionMode.BelowStretch:
				AddClass( "below-stretch" );
				break;
		}
	}

	public override void OnDeleted()
	{
		base.OnDeleted();

		AllPopups.Remove( this );
	}


	protected Panel Header;
	protected Label TitleLabel;
	protected IconPanel IconPanel;

	void CreateHeader()
	{
		if ( Header != null ) return;

		Header = Add.Panel( "header" );

		IconPanel = Header.Add.Icon( null );
		TitleLabel = Header.Add.Label( null, "title" );
	}


	public string Title
	{
		get => TitleLabel?.Text;
		set
		{
			CreateHeader();
			TitleLabel.Text = value;
		}
	}

	public string Icon
	{
		get => IconPanel?.Text;
		set
		{
			CreateHeader();
			IconPanel.Text = value;
		}
	}

	/// <summary>
	/// Closes all panels, marks this one as a success and closes it.
	/// </summary>
	public void Success()
	{
		AddClass( "success" );
		Popup.CloseAll();
	}

	/// <summary>
	/// Closes all panels, marks this one as a failure and closes it.
	/// </summary>
	public void Failure()
	{
		AddClass( "failure" );
		Popup.CloseAll();
	}

	public Panel AddOption( string text, Action action = null )
	{
		return Add.Button( text, () =>
		{
			CloseAll();
			action?.Invoke();
		} );
	}

	public Panel AddOption( string text, string icon, Action action = null )
	{
		return Add.ButtonWithIcon( text, icon, null, () =>
		{
			CloseAll();
			action?.Invoke();
		} );
	}

	public void MoveSelection( int dir )
	{
		var currentIndex = GetChildIndex( SelectedChild );

		if ( currentIndex >= 0 ) currentIndex += dir;
		else if ( currentIndex < 0 ) currentIndex = dir == 1 ? 0 : -1;

		SelectedChild?.SetClass( "active", false );
		SelectedChild = GetChild( currentIndex, true );
		SelectedChild?.SetClass( "active", true );
	}

	public override void Tick()
	{
		base.Tick();

		PositionMe();
	}

	public override void OnLayout( ref Rect layoutRect )
	{
		var padding = 10;
		var h = Screen.Height - padding;
		var w = Screen.Width - padding;

		if ( layoutRect.Bottom > h )
		{
			layoutRect.Top -= layoutRect.Bottom - h;
			layoutRect.Bottom -= layoutRect.Bottom - h;
		}

		if ( layoutRect.Right > w )
		{
			layoutRect.Left -= layoutRect.Right - w;
			layoutRect.Right -= layoutRect.Right - w;
		}
	}

	void PositionMe()
	{
		var rect = PopupSource.Box.Rect * PopupSource.ScaleFromScreen;

		var w = Screen.Width * PopupSource.ScaleFromScreen;
		var h = Screen.Height * PopupSource.ScaleFromScreen;


		Style.MaxHeight = Screen.Height - 50;

		switch ( Position )
		{
			case PositionMode.Left:
				{
					Style.Left = null;
					Style.Right = ((w - rect.Left) + PopupSourceOffset);
					Style.Top = rect.Top + rect.Height * 0.5f;
					Style.BackgroundColor = Color.Red;
					break;
				}				
			case PositionMode.LeftBottom:
				{
					Style.Left = null;
					Style.Right = ((w - rect.Left) + PopupSourceOffset);
					Style.Top = null;
					Style.Bottom = (h - rect.Bottom);
					Style.BackgroundColor = Color.Red;
					break;
				}
				
			case PositionMode.AboveLeft:
				{
					Style.Left = rect.Left;
					Style.Bottom = (Parent.Box.Rect * Parent.ScaleFromScreen).Height - rect.Top + PopupSourceOffset;
					Style.BackgroundColor = Color.Red;
					break;
				}

			case PositionMode.BelowLeft:
				{
					Style.Left = rect.Left;
					Style.Top = rect.Bottom + PopupSourceOffset;
					break;
				}

			case PositionMode.BelowCenter:
				{
					Style.Left = rect.Center.x; // centering is done via styles
					Style.Top = rect.Bottom + PopupSourceOffset;
					break;
				}

			case PositionMode.BelowStretch:
				{
					Style.Left = rect.Left;
					Style.Width = rect.Width;
					Style.Top = rect.Bottom + PopupSourceOffset;
					break;
				}
		}

		Style.Dirty();
	}

}
