using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using System.Net.Http;
using Microsoft.Azure.Cosmos;
using Gremlin.Net.Driver;
using Gremlin.Net.Structure.IO.GraphSON;
using System.Net.WebSockets;
using Newtonsoft.Json;
using System.Reflection;
using AngryMonkey.Cloud.GraphDB;
using AngryMonkey.Cloud.GraphDB.Classes;
using System.Text;

namespace GremlinBlazorSample.Pages
{
	public partial class Index
	{
		//private static string Host = "graphdbdemo.gremlin.cosmos.azure.com:443/";
		private static string DatabaseId = "DemoDB";
		private static string ContainerId = "AngryGraph";
		private static string ConnectionString = "AccountEndpoint=https://mohammad-graph-test.documents.azure.com:443/;AccountKey=2wT4Iw5W7j2jKOzSzebJYKHFxVr8KJ7G39a025sKRdQIDEM9QhK5htbhKCaGaUKksdbc2JVQZrLFcy6UKMZ71Q==;ApiKind=Gremlin;";
		private static string Host = "mohammad-graph-test.gremlin.cosmos.azure.com:443/";
		private static string GremlinPK = "2wT4Iw5W7j2jKOzSzebJYKHFxVr8KJ7G39a025sKRdQIDEM9QhK5htbhKCaGaUKksdbc2JVQZrLFcy6UKMZ71Q==";
		protected Container Container { get; set; }
		string containerLink = "/dbs/" + DatabaseId + "/colls/" + ContainerId;

		//public string GenerateID(string label)
		//{
		//    Guid Id = new();
		//    return label + Id;
		//}
		//bool Started { get; set; } = false;

		//private static bool EnableSSL
		//{
		//    get
		//    {
		//        if (Environment.GetEnvironmentVariable("EnableSSL") == null)
		//            return true;

		//        if (!bool.TryParse(Environment.GetEnvironmentVariable("EnableSSL"), out bool value))
		//            throw new ArgumentException("Invalid env var: EnableSSL is not a boolean");

		//        return value;
		//    }
		//}

		//ConnectionPoolSettings connectionPoolSettings = new()
		//{
		//    MaxInProcessPerConnection = 10,
		//    PoolSize = 30,
		//    ReconnectionAttempts = 3,
		//    ReconnectionBaseDelay = TimeSpan.FromMilliseconds(500)
		//};

		public class Brand : BaseVertexRecord
		{
			public string year { get; set; }
			public string Salary { get; set; }
		}

		public async Task Add()
		{
			CloudGraphDbClient cloudGraph = new(ConnectionString, DatabaseId, ContainerId, Host, GremlinPK);
			//         Brand brand = await cloudGraph.GetVertex<Brand>("", Guid.Empty);

			//         VertexRecord vertex = await cloudGraph.GetVertex("", Guid.Empty);

			////cloudGraph.EdgeTest();
			Guid g = new("d3da1c85-a272-4c3a-8a8f-3c738b97ed2d");

			List<EdgeRecord> EGL = await cloudGraph.GetEdges(g, new EdgesRequestOption()
			{
				Direction = EdgeDirection.In
			});

			var e = EGL;

			//         List<GraphRecordProperty> properties = new()
			//         {
			//             new(){ID = "year", Value = "5000"},
			//             new(){ID = "Salary", Value = "6000"},
			//             new(){ID = "HAHA", Value = "HEHE"}
			//         };

			//         Brand b = new() { year = "123", Salary = "222" };
			//         await cloudGraph.UpdateEdgeProperties<Brand>(g, b);
			//VertexRecord v = await cloudGraph.GetVertex("Brand", g);

			//EdgeRecord e = cloudGraph.GetEdgebyID("95831405-b710-4c1c-981f-555c9c762145");

			//await cloudGraph.AddVertex(new Brand()
			//{
			//    ID = g,
			//    Name = "XBOXx",
			//    Country = "Test"
			//});

			//Console.WriteLine();

			////List<GraphRecordProperty> gp = new()
			////{
			////    new(){ID = "HAHA", Value="Hehe"},
			////    new(){ID = "Country", Value="Hawai"}
			////};
			//Brand b = new()
			//{
			//    Name = "Apple",
			//    Country = "Canada"
			//};
			//cloudGraph.UpdateVertexProperty<Brand>(g,"Brand", b);
			//VertexRecord vertex1 = await cloudGraph.GetVertex("Brand",g);

			//List <VertexRecord> vertexRecords = cloudGraph.FindVerticiesByAnyProperty(new Brand()
			//{
			//	Name = "Microsoft"
			//         });

			//EdgeRecord edge1 = cloudGraph.GetEdgebyID("95831405-b710-4c1c-981f-555c9c762145");


		}

		//private static string PrintStatusAttributes(IReadOnlyDictionary<string, object> attributes)
		//{
		//    return
		//    $"\tStatusAttributes:" +
		//    $"\t[\" Status Code :  {GetValueAsString(attributes, "x-ms-status-code")}\"]" +
		//    $"\t[\" Total Time: {GetValueAsString(attributes, "x-ms-total-server-time-ms")}\"]" +
		//    $"\t[\" RU Cost : {GetValueAsString(attributes, "x-ms-total-request-charge")}\"] ";
		//}

		//public static string GetValueAsString(IReadOnlyDictionary<string, object> dictionary, string key)
		//{
		//    return JsonConvert.SerializeObject(GetValueOrDefault(dictionary, key));
		//}

		//public static object GetValueOrDefault(IReadOnlyDictionary<string, object> dictionary, string key)
		//{
		//    if (dictionary.ContainsKey(key))
		//    {
		//        return dictionary[key];
		//    }

		//    return null;
		//}

	}
}