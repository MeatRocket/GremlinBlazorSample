using System.Text.Json.Serialization;

namespace AngryMonkey.Cloud.GraphDB
{
	public class BaseGraphRecord
	{
		public Guid ID { get; set; }
		internal string _VertexLabel => this.GetType().Name;
		internal string _VertexPartitionKey => this.GetType().Name;
	}
}