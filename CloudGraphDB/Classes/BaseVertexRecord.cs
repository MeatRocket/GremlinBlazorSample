using System.Text.Json.Serialization;

namespace AngryMonkey.Cloud.GraphDB
{
	public class BaseVertexRecord
	{
		public Guid ID { get; set; }
		internal string _VertexLabel => this.GetType().Name;
	}
}