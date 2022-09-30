using Gremlin.Net.Driver;
using Gremlin.Net.Structure.IO.GraphSON;
using Microsoft.Azure.Cosmos;
using System.Net.WebSockets;
using System.Reflection;
using System.Text;

namespace AngryMonkey.Cloud.GraphDB
{
	public class CloudGraphDbClient
	{
		public CosmosClient GraphClient { get; set; }
		public Database GraphDatabase { get; set; }
		public Container GraphContainer { get; set; }
		private GremlinServer Gremlin { get; set; }

		private string ContainerLink => $"/dbs/{GraphDatabase.Id}/colls/{GraphContainer.Id}";
		private static bool EnableSSL
		{
			get
			{
				if (Environment.GetEnvironmentVariable("EnableSSL") == null)
					return true;

				if (!bool.TryParse(Environment.GetEnvironmentVariable("EnableSSL"), out bool value))
					return false;

				return value;
			}
		}

		public CloudGraphDbClient(string connectionString, string databaseId, string containerId, string host, string gremlinkKey)
		{
			GraphClient = new(connectionString);
			GraphDatabase = GraphClient.GetDatabase(databaseId);
			GraphContainer = GraphDatabase.GetContainer(containerId);

			Gremlin = new(host, GraphClient.Endpoint.Port, enableSsl: EnableSSL, username: ContainerLink, password: gremlinkKey);
		}

		private GremlinClient Client
		{
			get
			{
				Action<ClientWebSocketOptions> webSocketConfiguration = new(options =>
				{
					options.KeepAliveInterval = TimeSpan.FromSeconds(10);
				});

				ConnectionPoolSettings connectionPoolSettings = new()
				{
					MaxInProcessPerConnection = 10,
					PoolSize = 30,
					ReconnectionAttempts = 3,
					ReconnectionBaseDelay = TimeSpan.FromMilliseconds(500)
				};

				GremlinClient gremlinClient = new(Gremlin, new GraphSON2Reader(), new GraphSON2Writer(), GremlinClient.GraphSON2MimeType, connectionPoolSettings, webSocketConfiguration);
				return gremlinClient;
			}
		}

		public async Task AddVertex<T>(T vertex) where T : BaseVertexRecord
		{
			StringBuilder builder = new($"g.addV('{vertex._VertexLabel}').property('id', '{vertex.ID}').property('PartitionKey', '{vertex._VertexPartitionKey}')");

			foreach (PropertyInfo propertyInfo in vertex.GetType().GetProperties().Where(key => !key.Name.Equals("ID", StringComparison.OrdinalIgnoreCase)))
				if (propertyInfo.GetValue(vertex) != null)
					builder.Append($".property('{propertyInfo.Name}', '{propertyInfo.GetValue(vertex)}')");

			await Client.SubmitAsync<dynamic>(builder.ToString());
		}
	}
}