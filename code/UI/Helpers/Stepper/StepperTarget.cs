namespace RP.UI.Helpers;

/// <summary>
/// Mark a Panel with this class for it to be navigatable
/// </summary>
public class StepperTargetAttribute : Attribute, ITypeAttribute, IStep
{
	public static List<StepperTargetAttribute> GetAll => TypeLibrary.GetAttributes<StepperTargetAttribute>().ToList();
	
	public string Identifier { get; set; }
	public string Title { get; set; }
	public string Slogan { get; set; }
	public Type TargetType
	{
		get; set;
	}
	public StapStatus Status { get; set; }
	public int Id { get; set; }
	public bool IsLocked { get; set; }
	public bool IsFirst { get; set; }
	public bool IsLast { get; set; }

	public StepperTargetAttribute( int id, bool IsFirst, bool _IsLast, string identifier, string title, string slogan = "" )
	{
		Id = id;
		Identifier = identifier;
		Title = title;
		Slogan = slogan;
		if(!IsFirst)
			IsLocked = true;
		IsLast = _IsLast;
	}

	public static StepperTargetAttribute FindValidTarget( string identifier )
	{
		return GetAll
				.Where( x => x.Identifier == identifier )
				.FirstOrDefault();
	}

	public static StepperTargetAttribute FindValidTarget( int id )
	{
		return GetAll
				.Where( x => x.Id == id )
				.FirstOrDefault();
	}
}
