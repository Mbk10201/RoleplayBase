using Sandbox.Diagnostics;
using Sandbox.Html;
using Sandbox.Razor;
using System;
using System.IO;

namespace RP.UI.Helpers;

/// <summary>
/// A panel that acts like a website. A single page is always visible
/// but it will cache other views that you visit, and allow forward/backward navigation.
/// </summary>
[Library( "navigator" )]
public class NavigatorPanel : Panel
{
	public Panel CurrentPanel => Current?.Panel;
	public string CurrentUrl => Current?.Url;
	public string CurrentQuery;
	public string DefaultUrl { get; set; }

	public Panel NavigatorCanvas { get; set; }

	protected class HistoryItem
	{
		public Panel Panel;
		public string Url;
	}

	/// <summary>
	/// Called after initialization
	/// </summary>
	protected override void OnParametersSet()
	{
		if ( DefaultUrl != null && CurrentUrl == null )
		{
			Navigate( DefaultUrl );
		}
	}

	internal void RemoveUrls( Func<string, bool> p )
	{
		var removes = Cache.Where( x => p( x.Url ) ).ToArray();

		foreach ( var remove in removes )
		{
			remove.Panel.Delete();
			Cache.Remove( remove );
		}
	}

	public override void OnTemplateSlot( INode element, string slotName, Panel panel )
	{
		if ( slotName == "navigator-canvas" )
		{
			NavigatorCanvas = panel;

			foreach ( var p in Cache )
			{
				p.Panel.Parent = NavigatorCanvas;
			}

			return;
		}

		base.OnTemplateSlot( element, slotName, panel );
	}

	protected List<HistoryItem> Cache = new();

	HistoryItem Current;
	Stack<HistoryItem> Back = new();
	Stack<HistoryItem> Forward = new();

	Dictionary<string, TypeDescription> destinations = new Dictionary<string, TypeDescription>();

	/// <summary>
	/// Instead of finding pages by attributes, we can fill them in manually here
	/// </summary>
	public void AddDestination( string url, Type type )
	{
		var td = TypeLibrary.GetType( type );
		Assert.NotNull( td );
		destinations[url] = td;
	}

	public void Navigate( string url, bool redirectToDefault = true )
	{
		string query = "";
		string originalUrl = url;
		bool foundPartial = false;

		if ( url?.Contains( '?' ) ?? false )
		{
			var qi = url.IndexOf( '?' );
			query = url.Substring( qi + 1 );
			url = url.Substring( 0, qi );

			//Log.Info( $"Query: {query}" );
			//Log.Info( $"Url: {url}" );
		}

		//
		// Find a NavigatorPanel that we're a child of
		//
		var parent = Ancestors.OfType<NavigatorPanel>().FirstOrDefault();

		//
		// Make url absolute by adding it to parent url
		//
		if ( url?.StartsWith( "~/" ) ?? false && parent != null )
		{
			url = $"{parent.CurrentUrl}/{url[2..]}";
		}

		if ( url == CurrentUrl )
		{
			ApplyQuery( query );
			return;
		}

		if ( NavigatorCanvas == null )
		{
			NavigatorCanvas = this;
		}

		var previousUrl = CurrentUrl;

		var target = FindTarget( url, parent?.CurrentUrl );

		if ( target.panelType == null )
		{
			if ( DefaultUrl != null && redirectToDefault )
			{
				Navigate( DefaultUrl, false );
				return;
			}

			NotFound( url );
			return;
		}

		var parts = target.url.Split( '/', StringSplitOptions.RemoveEmptyEntries );

		//
		// If the URl contains a *, it's a partial url. This expects the matching
		// page to have a NavigatorPanel in it - which will forfil the rest of the url
		//
		if ( target.url.Contains( '*' ) )
		{
			foundPartial = true;

			// the full url might be something like
			// game/blahblah/front
			// but the url of this navigator might be
			// game/{ident}/*
			// so change the url to 
			// game/blahblah
			// stripping off extra parts

			var partCount = parts.Count();
			var currentParts = url.Split( "/" ).Take( partCount );
			url = string.Join( "/", currentParts );
		}

		Forward.Clear();

		if ( Current != null )
		{
			Back.Push( Current );
			Current.Panel.AddClass( "hidden" );

			if ( Current.Panel is INavigable _nav )
			{
				_nav.OnNavigationClose();
			}

			Current = null;
		}

		var cached = Cache.FirstOrDefault( x => x.Url == url );
		if ( cached != null )
		{
			cached.Panel.RemoveClass( "hidden" );
			Current = cached;
			Current.Panel.Parent = NavigatorCanvas;
		}
		else
		{
			var panel = target.panelType.Create<Panel>();
			if ( panel  == null )
			{
				Log.Warning( $"Found a Route attribute - but we couldn't create the panel ({target.panelType})" );
				return;
			}
			panel.AddClass( "navigator-body" );

			Current = new HistoryItem { Panel = panel, Url = url };
			Current.Panel.Parent = NavigatorCanvas;

			foreach ( var (key, value) in ExtractProperties( parts, url ) )
			{
				panel.SetProperty( key, value );
			}

			Cache.Add( Current );
			StateHasChanged();
		}

		if ( Current == null ) return;

		//
		// If we're a partial url, find the child NavigatorPanel and
		// send it the rest of the url
		//
		if ( foundPartial )
		{
			var childNavigator = Current.Panel as NavigatorPanel;

			if ( childNavigator  == null )
				childNavigator = Current.Panel.Descendants.OfType<NavigatorPanel>().FirstOrDefault();

			//Log.Info( $"Telling childNavigator [{Current.Panel}] => [{childNavigator}] to go to {originalUrl}" );
			
			if ( childNavigator != null )
			{
				childNavigator.Navigate( originalUrl );
			}
		}

		if ( Current.Panel is INavigable nav )
		{
			nav.OnNavigationOpen();
		}

		ApplyQuery( query );
	}

	/// <summary>
	/// Find a panel that can be created for this url
	/// </summary>
	private (string url, TypeDescription panelType ) FindTarget( string url, string currentUrl )
	{
		foreach( var destination in destinations )
		{
			if ( !DoesUrlMatch( url, destination.Key ) )
				continue;

			return (destination.Key, destination.Value);
		}

		var attr = RouteAttribute.FindValidTarget( url, currentUrl );
		if ( attr == null ) return default;

		return (attr.Value.Attribute.Url, attr.Value.Type);
	}

	static bool DoesUrlMatch( string url, string target )
	{
		if ( string.IsNullOrEmpty( url ) ) return false;

		if ( url.Contains( '?' ) )
		{
			url = url[..url.IndexOf( '?' )];
		}

		var a = url.Split( '/', StringSplitOptions.RemoveEmptyEntries );
		var parts = target.Split( '/', StringSplitOptions.RemoveEmptyEntries );

		for ( int i = 0; i < parts.Length || i < a.Length; i++ )
		{
			var left = i < a.Length ? a[i] : null;
			var right = i < parts.Length ? parts[i] : null;

			if ( right == "*" )
				return true;

			if ( !TestPart( left, right ) )
				return false;
		}

		return true;
	}

	static bool TestPart( string part, string ours )
	{
		// this is a variable
		if ( ours != null && ours.StartsWith( '{' ) && ours.EndsWith( '}' ) )
			return true;

		return part == ours;
	}

	public IEnumerable<(string key, string value)> ExtractProperties( string[] parts, string url )
	{
		var a = url.Split( '/', StringSplitOptions.RemoveEmptyEntries );

		for ( int i = 0; i < parts.Length; i++ )
		{
			if ( !parts[i].StartsWith( '{' ) ) continue;
			if ( !parts[i].EndsWith( '}' ) ) continue;

			var key = parts[i][1..^1].Trim( '?' );

			if ( i < a.Length )
			{
				yield return (key, a[i]);
			}
			else
			{
				yield return (key, null);
			}
		}
	}

	void ApplyQuery( string query )
	{
		if ( string.IsNullOrWhiteSpace( query ) )
			return;

		var parts = System.Web.HttpUtility.ParseQueryString( query );
		foreach ( var key in parts.AllKeys )
		{
			Current.Panel.SetProperty( key, parts.Get( key ) );
		}
	}

	protected virtual void NotFound( string url )
	{
		if ( url == null ) return;
		Log.Warning( $"Url Not Found: {url}" );
	}

	internal bool CurrentUrlMatches( string url )
	{
		if ( url != null && url.StartsWith( "~" ) )
			return CurrentUrl?.EndsWith( url[1..] ) ?? false;

		if ( CurrentUrl == null )
			return url == null;

		return CurrentUrl.WildcardMatch( url );
	}

	public override void SetProperty( string name, string value )
	{
		base.SetProperty( name, value );

		if ( name == "default" )
			DefaultUrl = value;
	}

	/// <summary>
	/// Navigate to a URL
	/// </summary>
	[PanelEvent]
	public bool NavigateEvent( string url )
	{
		Navigate( url );
		return false;
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

	public virtual bool GoBackUntilNot( string wildcard )
	{
		if ( GoBack() )
		{
			if ( !Current.Url.WildcardMatch( wildcard ) )
				return true;

			return GoBackUntilNot( wildcard );
		}

		return false;
	}

	public virtual bool GoBack()
	{
		if ( !Back.TryPop( out var result ) )
		{
			// TODO - only play this sound if we didn't pass to a parent
			PlaySound( "ui.navigate.deny" );
			return false;
		}

		if ( !Cache.Contains( result ) || result.Panel == null )
		{
			return GoBack();
		}

		PlaySound( "ui.navigate.back" );

		if ( Current != null )
			Forward.Push( Current );

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

		if ( !Cache.Contains( result ) || result.Panel == null )
		{
			return GoForward();
		}

		PlaySound( "ui.navigate.forward" );

		if ( Current != null )
			Back.Push( Current );

		Switch( result );
		return true;
	}

	void Switch( HistoryItem item )
	{
		if ( Current == item ) return;

		if ( Current?.Panel is INavigable fromNav )
		{
			fromNav.OnNavigationClose();
		}

		Current?.Panel.AddClass( "hidden" );
		Current = null;

		Current = item;
		Current?.Panel.RemoveClass( "hidden" );

		if ( Current?.Panel is INavigable toNav )
		{
			toNav.OnNavigationClose();
		}
	}

	public interface INavigable
	{
		public virtual void OnNavigationOpen() { }
		public virtual void OnNavigationClose() { }
	}
}


public static class NavigationExtensions
{
	public static void Navigate( this Panel panel, string url )
	{
		panel.AncestorsAndSelf.OfType<NavigatorPanel>().First().Navigate( url );
	}
}
