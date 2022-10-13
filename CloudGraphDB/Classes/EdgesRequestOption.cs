using AngryMonkey.Cloud.GraphDB.Classes;

namespace AngryMonkey.Cloud.GraphDB
{
	public class EdgesRequestOption
	{
		public EdgeDirection Direction { get; set; } = EdgeDirection.Both;
		public string? Label { get; set; } = null;
		public int Page { get; set; } = 1;
		public int CountPerPage { get; set; } = int.MaxValue;
	}
}