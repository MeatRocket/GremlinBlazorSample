using AngryMonkey.Cloud.GraphDB.Classes;
using Gremlin.Net.Driver;
using Gremlin.Net.Structure;
using Gremlin.Net.Structure.IO.GraphSON;
using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Cosmos.Serialization.HybridRow;
using Microsoft.Azure.Cosmos.Serialization.HybridRow.Schemas;
using Newtonsoft.Json.Linq;
using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
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

        #region Vertex

        public async Task AddVertex(string partitionkey, VertexRecord vertex)
        {
            StringBuilder builder = new($"g.addV('{vertex.Label}').property('id', '{vertex.ID}').property('PartitionKey', '{partitionkey}')");

            foreach (GraphRecordProperty propertyInfo in vertex.Properties.Where(key => !key.Name.Equals("ID", StringComparison.OrdinalIgnoreCase)))
                if (propertyInfo.Value != null)
                    builder.Append($".property('{propertyInfo.Name}', '{propertyInfo.Value}')");

            await Client.SubmitAsync<dynamic>(builder.ToString());
        }

        public async Task AddVertex<T>(string partitionkey, T vertex) where T : BaseVertexRecord
        {
            //StringBuilder builder = new($"g.addV('{vertex._VertexLabel}').property('id', '{vertex.ID}').property('PartitionKey', '{partitionkey}')");

            //foreach (PropertyInfo propertyInfo in vertex.GetType().GetProperties().Where(key => !key.Name.Equals("ID", StringComparison.OrdinalIgnoreCase)))
            //    if (propertyInfo.GetValue(vertex) != null)
            //        builder.Append($".property('{propertyInfo.Name}', '{propertyInfo.GetValue(vertex)}')");

            //await Client.SubmitAsync<dynamic>(builder.ToString());

            VertexRecord v = VertexRecord.ConvertToVertex(vertex);
            await AddVertex(partitionkey, v);
        }

        public async Task DeleteVertex(string partitionkey, Guid id)
        {
            await Client.SubmitAsync($"g.V(\"{id}\").has(\"PartitionKey\",\"{partitionkey}\").drop()");
        }

        //public async Task UpdateVertexProperty(string partitionkey, Guid id, string key ,string value)
        //{
        //    await Client.SubmitAsync<dynamic>($"g.V(\"{id}\").has(\"PartitionKey\",\"{partitionkey}\").property(\"{key}\",\"{value}\")");
        //}

        public async Task UpdateVertexProperties(string partitionkey, Guid id, List<GraphRecordProperty> properties)
        {
            StringBuilder builder = new($"g.V(\"{id}\").has(\"PartitionKey\",\"{partitionkey}\")");

            foreach (GraphRecordProperty property in properties)
            {
                if (property.Value != null)
                    builder.Append($".property(\"{property.Name}\",\"{property.Value}\")");
                else
                    await DeleteVertexProperty(partitionkey, id, property.Name);
            }

            await Client.SubmitAsync<dynamic>(builder.ToString());
        }

        public async Task UpdateVertexProperties<T>(string partitionkey, Guid id, T obj)
        {
            StringBuilder builder = new($"g.V(\"{id}\").has(\"PartitionKey\",\"{partitionkey}\")");

            dynamic properties = obj.GetType().GetProperties();

            foreach (var property in properties)
            {
                //Console.WriteLine($"Property Name :{property.Name}");
                //Console.WriteLine($"Property Value :{obj.GetType().GetProperty(property.Name).GetValue(obj)}");
                if ((obj.GetType().GetProperty(property.Name).GetValue(obj)).ToString() != "00000000-0000-0000-0000-000000000000")
                {
                    if (obj.GetType().GetProperty(property.Name).GetValue(obj) != null)
                        builder.Append($".property(\'{property.Name}\',\'{obj.GetType().GetProperty(property.Name).GetValue(obj)}')");
                    else
                        await DeleteVertexProperty(partitionkey, id, property.Name);
                }
            }

            await Client.SubmitAsync<dynamic>(builder.ToString());
        }

        public async Task<VertexRecord?> GetVertex(string partitionKey, Guid id)
        {
            ResultSet<dynamic> resultSet = await Client.SubmitAsync<dynamic>($"g.V(\"{id}\").has(\"PartitionKey\",\"{partitionKey}\")");

            return resultSet.ToList()[0] is Dictionary<string, object> r ? VertexRecord.Parse(r) : null;
        }

        public async Task<T?> GetVertex<T>(string partitionKey, Guid id) where T : BaseVertexRecord
        {
            VertexRecord? vertex = await GetVertex(partitionKey, id);

            return vertex?.Parse<T>();
        }

        public async Task<List<VertexRecord>> GetOutVertecies(string partitionKey, Guid id)
        {

            ResultSet<dynamic> resultSet = await Client.SubmitAsync<dynamic>($"g.V(\"{id}\").has(\"PartitionKey\",\"{partitionKey}\").inE().outV()");

            List<VertexRecord> vertexList = new();
            foreach (dynamic vertex in resultSet)
            {
                VertexRecord v = VertexRecord.Parse(vertex);
                vertexList.Add(v);
            }

            return vertexList;
        }

        public async Task<List<VertexRecord>> GetInVertecies(string partitionKey, Guid id)
        {
            List<VertexRecord> VRList = new();
            ResultSet<dynamic> resultSet = await Client.SubmitAsync<dynamic>($"g.V(\"{id}\").has(\"PartitionKey\",\"{partitionKey}\").outE().inV()");
            foreach (dynamic vertex in resultSet)
            {
                VertexRecord v = VertexRecord.Parse(vertex);
                VRList.Add(v);
            }
            return VRList;
        }

        public async Task<List<VertexRecord>?> FindVerticiesByAnyProperty<T>(T obj)
        {
            if (obj == null)
                return null;

            dynamic type = obj.GetType().GetProperties();
            StringBuilder query = new("g.V()");

            foreach (var item in type)
            {
                //Console.WriteLine($"Property Name :{item.Name}");
                //Console.WriteLine($"Property Value :{obj.GetType().GetProperty(item.Name).GetValue(obj)}");
                if ((obj.GetType().GetProperty(item.Name).GetValue(obj)).ToString() != "00000000-0000-0000-0000-000000000000" && obj.GetType().GetProperty(item.Name).GetValue(obj) != null)
                    query.Append($".has(\'{item.Name}\',\'{obj.GetType().GetProperty(item.Name).GetValue(obj)}')");
            }

            var resultSet = await Client.SubmitAsync<dynamic>(query.ToString());
            List<VertexRecord> VR = new();

            foreach (var vertex in resultSet)
            {
                VR.Add(VertexRecord.Parse(vertex));
            }

            var r = resultSet.ToList()[0] as Dictionary<string, object>;

            return VR;
        }
        public async Task DeleteVertexProperty(string partitionkey, Guid id, string propertyName)
        {
            await Client.SubmitAsync<dynamic>($"g.V(\"{id}\").has(\"PartitionKey\",\"{partitionkey}\").properties(\"{propertyName}\").drop()");
        }

        public async Task DeleteVertexProperties(string partitionkey, Guid id, List<GraphRecordProperty> properties)
        {
            foreach (GraphRecordProperty property in properties)
                await DeleteVertexProperty(partitionkey, id, property.Name);
        }

        public async Task DeleteVertexProperties<T>(string partitionkey, Guid id, T obj)
        {
            if (obj == null)
                return;

            dynamic type = obj.GetType().GetProperties();

            foreach (var item in type)
                if ((obj.GetType().GetProperty(item.Name).GetValue(obj)).ToString() != "00000000-0000-0000-0000-000000000000" && obj.GetType().GetProperty(item.Name).GetValue(obj) != null)
                    await DeleteVertexProperty(partitionkey, id, item.Name);
        }


        public async Task<List<VertexRecord>> GetVerticies(Guid vertexId, EdgesRequestOption? option = null)
        {
            ResultSet<dynamic> resultSet = await HelperMethod(vertexId, GraphReturnType.Vertex, option);

            return resultSet.Select(edge => (VertexRecord)VertexRecord.Parse(edge)).ToList();
        }

        #endregion

        #region Edge
        public async Task<ResultSet<dynamic>> HelperMethod(Guid vertexId, GraphReturnType returnType, EdgesRequestOption? option = null)
        {
            StringBuilder query = new($"g.V(\"{vertexId}\")");

            option ??= new();


            if (!string.IsNullOrEmpty(option.Label))
                query.Append($".haslabel(\"{option.Label}\")");

            switch (option.Direction)
            {
                case EdgeDirection.In:
                    query.Append(".inE()");
                    break;

                case EdgeDirection.Out:
                    query.Append(".outE()");
                    break;

                case EdgeDirection.Both:
                    query.Append(".bothE()");
                    break;
                default:
                    break;
            }

            switch (returnType)
            {
                case GraphReturnType.Vertex:
                    switch (option.Direction)
                    {
                        case EdgeDirection.In:
                            query.Append(".inV()");
                            break;

                        case EdgeDirection.Out:
                            query.Append(".outV()");
                            break;

                        case EdgeDirection.Both:
                            query.Append(".bothV()");
                            break;

                        default:
                            break;
                    }
                    break;
            }

            return await Client.SubmitAsync<dynamic>(query.ToString());
        }

        public async Task<List<EdgeRecord>> GetEdges(Guid vertexId, EdgesRequestOption? option = null)
        {
            ResultSet<dynamic> resultSet = await HelperMethod(vertexId, GraphReturnType.Edge, option);

            return resultSet.Select(edge => (EdgeRecord)EdgeRecord.Parse(edge)).ToList();
        }

        public async Task DeleteEdgeProperty(Guid id, string propertyName)
        {
            await Client.SubmitAsync<dynamic>($"g.E(\"{id}\").properties(\"{propertyName}\").drop()");
        }

        public async Task DeleteEdgeProperties(Guid id, List<GraphRecordProperty> properties)
        {
            foreach (GraphRecordProperty property in properties)
                await DeleteEdgeProperty(id, property.Name);
        }

        public async Task DeleteEdgeProperties<T>(Guid id, T obj)
        {

            if (obj == null)
                return;

            dynamic type = obj.GetType().GetProperties();

            foreach (var item in type)
                if (obj.GetType().GetProperty(item.Name).GetValue(obj).ToString() != "00000000-0000-0000-0000-000000000000" && obj.GetType().GetProperty(item.Name).GetValue(obj) != null)
                    await DeleteEdgeProperty(id, item.Name);

        }

        public async Task<EdgeRecord> GetEdgebyId(string id)
        {
            ResultSet<dynamic> resultSet = await Client.SubmitAsync<dynamic>($"g.E(\'{id}\')");

            Dictionary<string, object>? r = resultSet.ToList()[0] as Dictionary<string, object>;

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

        public async Task DeleteEdge(string partitionkey, Guid id)
        {
            await Client.SubmitAsync<dynamic>($"g.E(\"{id}\").drop()");
        }

        public async Task UpdateEdgeProperty(Guid id, string key, string value)
        {
            await Client.SubmitAsync<dynamic>($"g.E(\"{id}\").property(\"{key}\",\"{value}\")");
        }

        public async Task UpdateEdgeProperties<T>(Guid id, T obj)
        {
            StringBuilder builder = new($"g.E(\"{id}\")");

            dynamic properties = obj.GetType().GetProperties();

            foreach (var property in properties)
            {
                //Console.WriteLine($"Property Name :{property.Name}");
                //Console.WriteLine($"Property Value :{obj.GetType().GetProperty(property.Name).GetValue(obj)}");
                if ((obj.GetType().GetProperty(property.Name).GetValue(obj)).ToString() == "00000000-0000-0000-0000-000000000000")
                    continue;
                if (obj.GetType().GetProperty(property.Name).GetValue(obj) != null)
                    builder.Append($".property(\'{property.Name}\',\'{obj.GetType().GetProperty(property.Name).GetValue(obj)}')");
                else
                    await DeleteEdgeProperty(id, property.Name);

            }

            await Client.SubmitAsync<dynamic>(builder.ToString());
        }

        public async Task UpdateEdgeProperties(Guid id, List<GraphRecordProperty> properties)
        {
            StringBuilder builder = new($"g.E(\"{id}\")");

            foreach (GraphRecordProperty property in properties)
            {
                if (property.Value != null)
                    builder.Append($".property(\"{property.Name}\",\"{property.Value}\")");
                else
                    await DeleteEdgeProperty(id, property.Name);
            }

            await Client.SubmitAsync<dynamic>(builder.ToString());
        }

        #endregion

    }
}