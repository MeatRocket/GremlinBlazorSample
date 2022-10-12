using AngryMonkey.Cloud.GraphDB.Classes;
using Gremlin.Net.Driver;
using Gremlin.Net.Structure.IO.GraphSON;
using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Cosmos.Serialization.HybridRow;
using Newtonsoft.Json.Linq;
using System.Drawing.Drawing2D;
using System.Net.WebSockets;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

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

        public async Task DeleteVertex(Guid id, string pk)
        {
            StringBuilder builder = new($"g.V(\"{id}\").has(\"PartitionKey\",\"{pk}\").drop()");

            await Client.SubmitAsync<dynamic>(builder.ToString());
        }

        public async Task UpdateVertexProperty(Guid id, string pk, string key ,string value)
        {
            StringBuilder builder = new($"g.V(\"{id}\").has(\"PartitionKey\",\"{pk}\").property(\"{key}\",\"{value}\")");

            await Client.SubmitAsync<dynamic>(builder.ToString());
        }

		public async Task UpdateVertexProperties(Guid id, string pk, List<GraphRecordProperty> properties)
		{
            StringBuilder builder = new($"g.V(\"{id}\").has(\"PartitionKey\",\"{pk}\")");

			foreach (GraphRecordProperty property in properties)
			{
				builder.Append($".property(\"{property.ID}\",\"{property.Value}\")");
            }

            await Client.SubmitAsync<dynamic>(builder.ToString());
        }

        public async Task UpdateVertexProperties<T>(Guid id, string pk, T obj)
        {
            StringBuilder builder = new($"g.V(\"{id}\").has(\"PartitionKey\",\"{pk}\")");

            dynamic properties = obj.GetType().GetProperties();

            foreach (var property in properties)
            {
                Console.WriteLine($"Property Name :{property.Name}");
                Console.WriteLine($"Property Value :{obj.GetType().GetProperty(property.Name).GetValue(obj)}");
                if ((obj.GetType().GetProperty(property.Name).GetValue(obj)).ToString() != "00000000-0000-0000-0000-000000000000" && obj.GetType().GetProperty(property.Name).GetValue(obj) != null)
                    builder.Append($".property(\'{property.Name}\',\'{obj.GetType().GetProperty(property.Name).GetValue(obj)}')");
            }

            await Client.SubmitAsync<dynamic>(builder.ToString());
        }

        public async Task<VertexRecord?> GetVertex(string partitionKey, Guid id)
		{
			StringBuilder builder = new($"g.V(\'{id}\')");

			ResultSet<dynamic> resultSet = await Client.SubmitAsync<dynamic>(builder.ToString());

			Dictionary<string, object>? r = resultSet.ToList()[0] as Dictionary<string, object>;

			return r != null ? VertexRecord.Parse(r) : null;
		}

		public async Task<T?> GetVertex<T>(string partitionKey, Guid id) where T : BaseVertexRecord
		{
			VertexRecord? vertex = await GetVertex(partitionKey, id);

			return vertex?.Parse<T>();
		}

		public async Task<List<VertexRecord>?> FindVerticiesByAnyProperty<T>(T obj)
		{
			if (obj == null)
				return null;

			dynamic type = obj.GetType().GetProperties();
			StringBuilder query = new("g.V()");

			foreach (var item in type)
			{
				Console.WriteLine($"Property Name :{item.Name}");
				Console.WriteLine($"Property Value :{obj.GetType().GetProperty(item.Name).GetValue(obj)}");
				if ((obj.GetType().GetProperty(item.Name).GetValue(obj)).ToString() != "00000000-0000-0000-0000-000000000000" && obj.GetType().GetProperty(item.Name).GetValue(obj) != null)
					query.Append($".has(\'{item.Name}\',\'{obj.GetType().GetProperty(item.Name).GetValue(obj)}')");
			}

			var resultSet =await Client.SubmitAsync<dynamic>(query.ToString());
			List<VertexRecord> VR = new();

			foreach (var vertex in resultSet)
			{
				VR.Add(VertexRecord.Parse(vertex));
			}

			var r = resultSet.ToList()[0] as Dictionary<string, object>;

			return VR;
		}

		public async Task<EdgeRecord> GetEdgebyID(string id)
		{
			StringBuilder builder = new($"g.E(\'{id}\')");
			var resultSet =await Client.SubmitAsync<dynamic>(builder.ToString());
			var r = resultSet.ToList()[0] as Dictionary<string, object>;

			return EdgeRecord.Parse(r);
		}

        public void AddEdge<T>(T edgeProperties, Guid inVID, Guid outVID) where T : BaseVertexRecord
		{
			StringBuilder builder = new($"g.V(\"{outVID}\").as('a').V(\"{inVID}\").as('b').addE(\"{edgeProperties._VertexLabel}\").from('a').to('b')");

            foreach (PropertyInfo propertyInfo in edgeProperties.GetType().GetProperties().Where(key => !key.Name.Equals("ID", StringComparison.OrdinalIgnoreCase)))
				if (propertyInfo.GetValue(edgeProperties) != null)
					builder.Append($".property('{propertyInfo.Name}', '{propertyInfo.GetValue(edgeProperties)}')");

			Client.SubmitAsync<dynamic>(builder.ToString());
		}

        public async Task DeleteEdge(Guid id, string pk)
        {
            StringBuilder builder = new($"g.E(\"{id}\").drop()");

            await Client.SubmitAsync<dynamic>(builder.ToString());
        }

        public async Task UpdateEdgeProperty(Guid id, string key, string value)
        {
            StringBuilder builder = new($"g.E(\"{id}\").property(\"{key}\",\"{value}\")");

            await Client.SubmitAsync<dynamic>(builder.ToString());
        }

        public async Task UpdateEdgeProperties<T>(Guid id, T obj)
        {
            StringBuilder builder = new($"g.E(\"{id}\")");

            dynamic properties = obj.GetType().GetProperties();

            foreach (var property in properties)
            {
                Console.WriteLine($"Property Name :{property.Name}");
                Console.WriteLine($"Property Value :{obj.GetType().GetProperty(property.Name).GetValue(obj)}");
                if ((obj.GetType().GetProperty(property.Name).GetValue(obj)).ToString() != "00000000-0000-0000-0000-000000000000" && obj.GetType().GetProperty(property.Name).GetValue(obj) != null)
                    builder.Append($".property(\'{property.Name}\',\'{obj.GetType().GetProperty(property.Name).GetValue(obj)}')");
            }

            await Client.SubmitAsync<dynamic>(builder.ToString());
        }


        public async Task UpdateEdgeProperties(Guid id, string pk, List<GraphRecordProperty> properties)
        {
            StringBuilder builder = new($"g.E(\"{id}\")");

            foreach (GraphRecordProperty property in properties)
            {
                builder.Append($".property(\"{property.ID}\",\"{property.Value}\")");
            }

            await Client.SubmitAsync<dynamic>(builder.ToString());
        }

        public class Brand : BaseVertexRecord
        {
            public string Name { get; set; }
            public string Country { get; set; }
        }

        public async void EdgeTest()
		{
            dynamic r = await Client.SubmitAsync<dynamic>("g.V(\"9b0a078a-415b-4ff5-8024-b5a7aa4c0e4c\")");
            //dynamic r = await Client.SubmitAsync<dynamic>("g.V(\"9b0a078a-415b-4ff5-8024-b5a7aa4c0e4c\")");
            //foreach (var item in r)
            //{
            //             Brand b = VertexRecord.Parse<Brand>(item);
            //         }
        }

	}
}