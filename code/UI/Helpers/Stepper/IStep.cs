namespace RP.UI.Helpers;

public enum StapStatus
{
	LOCKED,
	FINISHED,
}

public interface IStep
{
	int Id { get; set; }
	string Identifier { get; set; }
	string Title { get; set; }
	string Slogan { get; set; }
	StapStatus Status { get; set; }
	bool IsLocked { get; set; }
	bool IsFirst { get; set; }
	bool IsLast { get; set; }
}
