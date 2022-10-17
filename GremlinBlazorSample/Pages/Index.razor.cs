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

        private string? VertexLabel;
        private string? Response;
        private string? VertexPK;
        private string? VertexID;
        private string? EdgePK, EdgeID;
        private string VertexOutID, VertexInID;
        private string? Val1, Val2, Val3, Val4;
        private VertexRecord? Vertex;
        private static List<GraphRecordProperty> Properties = new()
        {
            new()
        };
        IQueryable<GraphRecordProperty>? PropertiesRecords;

        private List<GraphRecordProperty> UpdateProperties = new()
        {
            new()
        };

        public class Knows
        {
            public string Name { get; set; }
            public string Employees { get; set; }
        }

        public class Brand : BaseVertexRecord
        {
            public string Name { get; set; }
            public string Employees { get; set; }
        }

        public class Person : BaseVertexRecord
        {
            public string Knows { get; set; }
            public string Loves { get; set; }

            public string Kills { get; set; }
        }

        public void AddProperty(List<GraphRecordProperty> properties)
        {
            properties.Add(new());
        }

        //public async Task Add()
        //{
        //    CloudGraphDbClient cloudGraph = new(ConnectionString, DatabaseId, ContainerId, Host, GremlinPK);
        //    Guid g = Guid.NewGuid();

        //    Brand b = new Brand()
        //    {
        //        ID = g,
        //        Salary = "10mil",
        //        Year = "5000"
        //    };

        //    await cloudGraph.AddVertex("Black", b);

        //}

        public async Task CreateVertex()
        {
            CloudGraphDbClient cloudGraph = new(ConnectionString, DatabaseId, ContainerId, Host, GremlinPK);
            Guid g = Guid.NewGuid();
            VertexRecord v = new VertexRecord(g, VertexLabel);

            v.Properties = Properties;

            try
            {
                await cloudGraph.AddVertex(VertexLabel, v);
                Response = "Success!";
            }
            catch (Exception)
            {
                Response = "An Error Accured";
                throw;
            }
        }

        public void CastToGrid()
        {

        }

        public async Task DeleteVertex()
        {
            try
            {
                CloudGraphDbClient cloudGraph = new(ConnectionString, DatabaseId, ContainerId, Host, GremlinPK);
                await cloudGraph.DeleteVertex(VertexPK, Guid.Parse(VertexID));
                Response = "Deleted Successfully";
            }
            catch (Exception)
            {
                Response = "an error accured";
                throw;
            }
        }

        public async Task UpdateVertexProperties()
        {
            try
            {
                CloudGraphDbClient cloudGraph = new(ConnectionString, DatabaseId, ContainerId, Host, GremlinPK);
                await cloudGraph.UpdateVertexProperties(VertexPK, Guid.Parse(VertexID), UpdateProperties);
                Response = "Updated Successfully";
            }
            catch (Exception)
            {
                Response = "an error accured";
                throw;
            }
        }

        public async Task FindVertex()
        {
            try
            {
                CloudGraphDbClient cloudGraph = new(ConnectionString, DatabaseId, ContainerId, Host, GremlinPK);
                Vertex = await cloudGraph.GetVertex(VertexPK, Guid.Parse(VertexID));
                Response = "Vertex Found";
            }
            catch (Exception)
            {
                Response = "an error accured";
                throw;
            }
        }

        public async Task CreateBrand()
        {
            Guid g = Guid.NewGuid();
            try
            {
                CloudGraphDbClient cloudGraph = new(ConnectionString, DatabaseId, ContainerId, Host, GremlinPK);
                await cloudGraph.AddVertex<Brand>("Brand", new() { ID = g, Name = Val1, Employees = Val2 });
                Response = "Brand Created";
            }
            catch (Exception)
            {
                Response = "an error accured";
                throw;
            }
        }

        //public async Task CreatePerson()
        //{
        //    Guid g = Guid.NewGuid();
        //    try
        //    {
        //        CloudGraphDbClient cloudGraph = new(ConnectionString, DatabaseId, ContainerId, Host, GremlinPK);
        //        await cloudGraph.AddVertex<Person>("Person", new() { FirstName = Val1, ID = g, LastName = Val2, Age = Val3 });
        //        Response = "Person Created !";
        //    }
        //    catch (Exception)
        //    {
        //        Response = "an error accured";
        //        throw;
        //    }
        //}



        public async Task CreateEdge()
        {
            Guid g = Guid.NewGuid();

            try
            {
                CloudGraphDbClient cloudGraph = new(ConnectionString, DatabaseId, ContainerId, Host, GremlinPK);
                await cloudGraph.AddEdge<Person>(new Person() { Knows = Val1, Kills = Val2, Loves = Val3, ID = g }, Guid.Parse(VertexInID), Guid.Parse(VertexOutID));
                Response = "Edge Created !";
            }
            catch (Exception)
            {
                Response = "an error accured";
                throw;
            }
        }

        public async Task DeleteEdge()
        {
            try
            {
                CloudGraphDbClient cloudGraph = new(ConnectionString, DatabaseId, ContainerId, Host, GremlinPK);
                await cloudGraph.DeleteEdge(Guid.Parse(EdgeID));
                Response = "Edge Deleted !";
            }
            catch (Exception)
            {
                Response = "an error accured";

            }

        }
    }
}