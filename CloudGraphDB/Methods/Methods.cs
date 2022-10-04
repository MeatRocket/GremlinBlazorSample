using AngryMonkey.Cloud.GraphDB.Classes;
using Gremlin.Net.Driver;
using Gremlin.Net.Structure.IO.GraphSON;
using Microsoft.Azure.Cosmos;
using System.Drawing.Drawing2D;
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

        public async Task AddVertex<T>(T vertex) where T : BaseGraphRecord
        {
            StringBuilder builder = new($"g.addV('{vertex._VertexLabel}').property('id', '{vertex.ID}').property('PartitionKey', '{vertex._VertexPartitionKey}')");

            foreach (PropertyInfo propertyInfo in vertex.GetType().GetProperties().Where(key => !key.Name.Equals("ID", StringComparison.OrdinalIgnoreCase)))
                if (propertyInfo.GetValue(vertex) != null)
                    builder.Append($".property('{propertyInfo.Name}', '{propertyInfo.GetValue(vertex)}')");

            await Client.SubmitAsync<dynamic>(builder.ToString());
        }


        public VertexRecord GetVertexbyID(string id)
        {
            StringBuilder builder = new($"g.V(\'{id}\')");

            var resultSet = Client.SubmitAsync<dynamic>(builder.ToString()).Result;

            var r = resultSet.ToList()[0] as Dictionary<string, object>;

            return VertexRecord.Parse(r);
        }

        //needs to be checked

        //public T GetbyID<T>(string id)
        //{
        //    StringBuilder builder = new($"g.V(\'{id}\')");

        //    var resultSet = Client.SubmitAsync<dynamic>(builder.ToString()).Result;

        //    Dictionary<string, object> r = resultSet.ToList()[0] as Dictionary<string, object>;

        //    return VertexRecord.Parse<r>(r);
        //}

        public List<VertexRecord>? FindVerticiesByAnyProperty<T>(T obj)
        {
            if (obj == null)
                return null;

            dynamic type = obj.GetType().GetProperties();
            StringBuilder query = new("g.V()");

            foreach (var item in type)
            {
                Console.WriteLine($"Property Name :{item.Name}");
                Console.WriteLine($"Property Value :{obj.GetType().GetProperty(item.Name).GetValue(obj)}");
                if ( (obj.GetType().GetProperty(item.Name).GetValue(obj)).ToString() != "00000000-0000-0000-0000-000000000000" && obj.GetType().GetProperty(item.Name).GetValue(obj) != null)
                    query.Append($".has(\'{item.Name}\',\'{obj.GetType().GetProperty(item.Name).GetValue(obj)}')");
            }

            var resultSet = Client.SubmitAsync<dynamic>(query.ToString()).Result;
            List<VertexRecord> VR = new();

            foreach (var vertex in resultSet)
            {
                VR.Add(VertexRecord.Parse(vertex));
            }

            var r = resultSet.ToList()[0] as Dictionary<string, object>;

            return VR;
        }


        public EdgeRecord GetEdgebyID(string id)
        {
            StringBuilder builder = new($"g.E(\'{id}\')");
            var resultSet = Client.SubmitAsync<dynamic>(builder.ToString()).Result;
            var r = resultSet.ToList()[0] as Dictionary<string, object>;

            return EdgeRecord.Parse(r);
        }

        public void AddEdge<T>(T vertex,string inVID, string outVID) where T : BaseGraphRecord
        {
            StringBuilder builder = new($"g.addV('{vertex._VertexLabel}').property('id', '{vertex.ID}').property('PartitionKey', '{vertex._VertexPartitionKey}')");

            foreach (PropertyInfo propertyInfo in vertex.GetType().GetProperties().Where(key => !key.Name.Equals("ID", StringComparison.OrdinalIgnoreCase)))
                if (propertyInfo.GetValue(vertex) != null)
                    builder.Append($".property('{propertyInfo.Name}', '{propertyInfo.GetValue(vertex)}')");

             Client.SubmitAsync<dynamic>(builder.ToString());
        }


        public dynamic EdgeTest()
        {
            return Client.SubmitAsync<dynamic>("g.V(\"d3da1c85-a272-4c3a-8a8f-3c738b97ed2d\")").Result;
        }

    }
}