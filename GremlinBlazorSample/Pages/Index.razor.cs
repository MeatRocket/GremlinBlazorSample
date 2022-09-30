using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using System.Net.Http;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Routing;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.Web.Virtualization;
using Microsoft.JSInterop;
using GremlinBlazorSample;
using GremlinBlazorSample.Shared;
using Microsoft.Azure.Cosmos;
using Gremlin.Net.Driver;
using Gremlin.Net.Structure.IO.GraphSON;
using System.Net.WebSockets;
using Gremlin.Net.Structure.IO;
using Newtonsoft.Json;
using System.Collections;
using Microsoft.AspNetCore.Http;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.DataCollection;
using Microsoft.Azure.Cosmos.Serialization.HybridRow;
using static GremlinBlazorSample.Pages.Index;
using System.Reflection;

namespace GremlinBlazorSample.Pages
{
    public partial class Index
    {
        //private static string Host = "graphdbdemo.gremlin.cosmos.azure.com:443/";
        private static string DatabaseId = "DemoDB";
        private static string ContainerId = "People";
        private static string ConnectionString = "AccountEndpoint=https://mohammad-graph-test.documents.azure.com:443/;AccountKey=2wT4Iw5W7j2jKOzSzebJYKHFxVr8KJ7G39a025sKRdQIDEM9QhK5htbhKCaGaUKksdbc2JVQZrLFcy6UKMZ71Q==;ApiKind=Gremlin;";
        private static string Host = "mohammad-graph-test.gremlin.cosmos.azure.com:443/";
        private static string GremlinPK = "2wT4Iw5W7j2jKOzSzebJYKHFxVr8KJ7G39a025sKRdQIDEM9QhK5htbhKCaGaUKksdbc2JVQZrLFcy6UKMZ71Q==";
        protected Container Container { get; set; }
        string containerLink = "/dbs/" + DatabaseId + "/colls/" + ContainerId;


        public string GenerateID(string label)
        {
            Guid Id = new();
            return label + Id;
        }
        bool Started { get; set; } = false;

        private static bool EnableSSL
        {
            get
            {
                if (Environment.GetEnvironmentVariable("EnableSSL") == null)
                    return true;

                if (!bool.TryParse(Environment.GetEnvironmentVariable("EnableSSL"), out bool value))
                    throw new ArgumentException("Invalid env var: EnableSSL is not a boolean");

                return value;
            }
        }


        ConnectionPoolSettings connectionPoolSettings = new()
        {
            MaxInProcessPerConnection = 10,
            PoolSize = 30,
            ReconnectionAttempts = 3,
            ReconnectionBaseDelay = TimeSpan.FromMilliseconds(500)
        };


        public async Task Add()
        {

            Started = true;
            CosmosClient graphClient = new(ConnectionString);
            Database GraphDB = graphClient.GetDatabase(DatabaseId);

            Container = GraphDB.GetContainer(ContainerId);

            var webSocketConfiguration =
            new Action<ClientWebSocketOptions>(options =>
            {
                options.KeepAliveInterval = TimeSpan.FromSeconds(10);
            });

            GremlinServer gremlinServer = new(Host, graphClient.Endpoint.Port, enableSsl: EnableSSL, username: containerLink, password: GremlinPK);


            using GremlinClient gremlinClient = new(gremlinServer, new GraphSON2Reader(), new GraphSON2Writer(), GremlinClient.GraphSON2MimeType, connectionPoolSettings, webSocketConfiguration);
            try
            {
                //var resultSet = gremlinClient.SubmitAsync<dynamic>("g.addV('person').property('id', 'Ahmad').property('firstName', 'Ahmad').property('age', 44).property('person', 'pk')").Result;

                var resultSet = gremlinClient.SubmitAsync<dynamic>("g.V('thomas')").Result;

                //var resultSet = gremlinClient.SubmitAsync<dynamic>("g.V().has(\"age\",44)").Result;


                DisplayResultSet = string.Empty;
                DisplayResult = string.Empty;

                //foreach (var result in resultSet)
                //    foreach (var item in result)
                //    {
                //        if(item.Key != "properties")
                //        DisplayResult += $"Key: {item.Key} | Value: {item.Value} ||||";
                //        else
                //            foreach (var property in item.Value)
                //            {
                //                DisplayResult += $"@@ Property name: {property.Key} ! Property Value: {JsonConvert.SerializeObject(property.Value)} @@@";
                //            }
                //    }

                //GraphRecord record = GraphRecord.Parse(resultSet.ToList()[0]);

                var r = resultSet.ToList()[0] as Dictionary<string, object>;

                var properties = r["properties"] as Dictionary<string, object>;

                var age = properties["age"];

                var ageList = (age as IEnumerable<object>).ToList();

                var ageValues = ageList[0] as Dictionary<string, object>;

                VertexRecord record = VertexRecord.Parse(r);

                Person person = VertexRecord.Parse<Person>(r);

                VertexRecord record1 = VertexRecord.GetbyID("thomas");

                Person person1 = VertexRecord.GetbyID<Person>("thomas");

                Person TestSubject = new() { firstName = "hannah" };

                List<VertexRecord> vr = VertexRecord.FindVerticiesByAnyProperty<Person>(TestSubject);

                //Console.WriteLine(r["id"]);

                //Console.WriteLine($"ID :  {r["id"]}  | Label : {r["label"]}  |  Type : {r["type"]}");

                //Console.WriteLine(GenerateID("person"));


                DisplayResultSet += PrintStatusAttributes(resultSet.StatusAttributes);
            }
            catch (Exception e)
            {
                throw e;
            }
            StateHasChanged();

        }

        public class PropertyClass
        {
            public string id { get; set; }
            public string value { get; set; }
        }

        private static string PrintStatusAttributes(IReadOnlyDictionary<string, object> attributes)
        {
            return
            $"\tStatusAttributes:" +
            $"\t[\" Status Code :  {GetValueAsString(attributes, "x-ms-status-code")}\"]" +
            $"\t[\" Total Time: {GetValueAsString(attributes, "x-ms-total-server-time-ms")}\"]" +
            $"\t[\" RU Cost : {GetValueAsString(attributes, "x-ms-total-request-charge")}\"] ";
        }

        public static string GetValueAsString(IReadOnlyDictionary<string, object> dictionary, string key)
        {
            return JsonConvert.SerializeObject(GetValueOrDefault(dictionary, key));
        }

        public static object GetValueOrDefault(IReadOnlyDictionary<string, object> dictionary, string key)
        {
            if (dictionary.ContainsKey(key))
            {
                return dictionary[key];
            }

            return null;
        }


        public string DisplayResultSet { get; set; } = "";

        public string DisplayResult { get; set; } = "";

        public record VertexRecord
        {
            public string ID { get; set; }
            public string Label { get; set; }
            public string Type { get; set; }

            public List<VertexRecordProperty> Properties { get; set; }

            public static VertexRecord? Parse(dynamic result)
            {
                var resultproperties = result["properties"] as Dictionary<string, object>;
                VertexRecord graphRecord = new()
                {
                    ID = result["id"],
                    Label = result["label"],
                    Type = result["type"]
                };

                graphRecord.Properties = new();

                foreach (var property in resultproperties)
                {
                    var prop = (property.Value as IEnumerable<object>).ToList();
                    var value = prop[0] as Dictionary<string, object>;
                    var value1 = value["value"];
                    graphRecord.Properties.Add(new()
                    {
                        ID = property.Key.ToString(),
                        Value = value1.ToString()
                    });
                }

                return graphRecord;
            }

            public static VertexRecord GetbyID(string id)
            {
                string query = $"g.V(\'{id}\')";


                //establishing connection
                CosmosClient graphClient = new(ConnectionString);
                Database GraphDB = graphClient.GetDatabase(DatabaseId);

                var webSocketConfiguration =
                new Action<ClientWebSocketOptions>(options =>
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

                GremlinServer gremlinServer = new(Host, graphClient.Endpoint.Port, enableSsl: EnableSSL, username: "/dbs/DemoDB/colls/People", password: GremlinPK);

                using GremlinClient gremlinClient = new(gremlinServer, new GraphSON2Reader(), new GraphSON2Writer(), GremlinClient.GraphSON2MimeType, connectionPoolSettings, webSocketConfiguration);

                var resultSet = gremlinClient.SubmitAsync<dynamic>(query).Result;

                var r = resultSet.ToList()[0] as Dictionary<string, object>;

                return Parse(r);
            }

            public static T GetbyID<T>(string id)
            {
                string query = $"g.V(\'{id}\')";


                //establishing connection
                CosmosClient graphClient = new(ConnectionString);
                Database GraphDB = graphClient.GetDatabase(DatabaseId);

                var webSocketConfiguration =
                new Action<ClientWebSocketOptions>(options =>
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

                GremlinServer gremlinServer = new(Host, graphClient.Endpoint.Port, enableSsl: EnableSSL, username: "/dbs/DemoDB/colls/People", password: GremlinPK);

                using GremlinClient gremlinClient = new(gremlinServer, new GraphSON2Reader(), new GraphSON2Writer(), GremlinClient.GraphSON2MimeType, connectionPoolSettings, webSocketConfiguration);

                var resultSet = gremlinClient.SubmitAsync<dynamic>(query).Result;

                var r = resultSet.ToList()[0] as Dictionary<string, object>;

                return Parse<T>(r);
            }

            public static T Parse<T>(dynamic result)
            {
                VertexRecord graphRecord = Parse(result);

                T obj = (T)Activator.CreateInstance(typeof(T));

                foreach (VertexRecordProperty graphProperty in graphRecord.Properties)
                {
                    PropertyInfo? propertyInfo = typeof(T).GetProperty(graphProperty.ID);

                    if (propertyInfo == null)
                        continue;

                    propertyInfo.SetValue(obj, graphProperty.Value);
                }

                return obj;
            }

            public static List<VertexRecord> FindVerticiesByAnyProperty<T>(T obj)
            {

                dynamic type = obj.GetType().GetProperties();
                string query = "g.V()";

                //string query = $"g.V().has(\'{key}\',\'{value}\')";

                foreach (var item in type)
                {
                    //if(obj.GetType().GetProperty(item.Name).GetValue(obj)!=null)
                    //    if(obj.GetType().GetProperty(item.Name).GetValue(obj).GetType()==)
                    //    query += $".has(\"{item.Name}\",{obj.GetType().GetProperty(item.Name).GetValue(obj)})";
                    Console.WriteLine($"Property Name :{item.Name}");
                    Console.WriteLine($"Property Value :{obj.GetType().GetProperty(item.Name).GetValue(obj)}");
                }
                Console.WriteLine(query);

                //establishing connection
                //break here
                CosmosClient graphClient = new(ConnectionString);
                Database GraphDB = graphClient.GetDatabase(DatabaseId);

                var webSocketConfiguration =
                new Action<ClientWebSocketOptions>(options =>
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

                GremlinServer gremlinServer = new(Host, graphClient.Endpoint.Port, enableSsl: EnableSSL, username: "/dbs/DemoDB/colls/People", password: GremlinPK);

                using GremlinClient gremlinClient = new(gremlinServer, new GraphSON2Reader(), new GraphSON2Writer(), GremlinClient.GraphSON2MimeType, connectionPoolSettings, webSocketConfiguration);

                var resultSet = gremlinClient.SubmitAsync<dynamic>("").Result;

                var r = resultSet.ToList()[0] as Dictionary<string, object>;

                List<VertexRecord> VR = new();

                return VR;
            }
        }

        public class Person
        {
            public string firstName { get; set; }
            public string age { get; set; }
        }

        public class VertexRecordProperty
        {
            public string ID { get; set; }
            public string Value { get; set; }
        }
    }
}