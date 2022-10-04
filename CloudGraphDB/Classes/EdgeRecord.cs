using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace AngryMonkey.Cloud.GraphDB.Classes
{
    public record EdgeRecord : GraphRecord
    {
        private string inVLabel;
        private string outVLabel;
        private string inVID;
        private string outVID;

        public EdgeRecord(Guid id, string label, string inVLabel, string outVLabel, string inVID, string outVID) : base(id, label)  
        {
            this.inVLabel = inVLabel;
            this.outVLabel = outVLabel;
            this.inVID = inVID;
            this.outVID = outVID;
        }

        public List<GraphRecordProperty> Properties { get; set; } = new();

        public static EdgeRecord? Parse(dynamic result)
        {
            Dictionary<string, object>? resultproperties = result["properties"] as Dictionary<string, object>;

            EdgeRecord edgeRecord = new(new Guid(result["id"]), result["label"], result["inVLabel"], result["outVLabel"], result["inV"], result["outV"]);

            foreach (var property in resultproperties)
            {
                edgeRecord.Properties.Add(new()
                {
                    ID = property.Key.ToString(),
                    Value = property.Value.ToString()
                });
            }

            return edgeRecord;
        }

        public static T Parse<T>(dynamic result) where T : BaseEdgeRecord
        {
            EdgeRecord edgeRecord = Parse(result);

            T? obj = Activator.CreateInstance(typeof(T)) as T;

            obj.ID = edgeRecord.ID;

            foreach (GraphRecordProperty graphProperty in edgeRecord.Properties)
            {
                PropertyInfo? propertyInfo = typeof(T).GetProperty(graphProperty.ID);

                if (propertyInfo == null)
                    continue;

                propertyInfo.SetValue(obj, graphProperty.Value);
            }

            return obj;
        }
    }
}
