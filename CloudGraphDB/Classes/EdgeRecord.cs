using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace AngryMonkey.Cloud.GraphDB.Classes
{
	public record EdgeRecord
	{
		public Guid ID { get; set; }
		public string Label { get; set; }
		private VertexRecord In { get; set; }
		private VertexRecord Out { get; set; }

		public EdgeRecord(Guid id, string label, string inVLabel, string outVLabel, Guid inVID, Guid outVID)
		{
			ID = id;
			Label = label;

			In = new VertexRecord(inVID, inVLabel);
			Out = new VertexRecord(outVID, outVLabel);
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

        public T Parse<T>() where T : BaseVertexRecord
        {
            T? obj = Activator.CreateInstance(typeof(T)) as T;

            obj.ID = ID;

            foreach (GraphRecordProperty graphProperty in Properties)
            {
                PropertyInfo? propertyInfo = typeof(T).GetProperty(graphProperty.ID);

                if (propertyInfo != null)
                    propertyInfo.SetValue(obj, graphProperty.Value);

            }

            return obj;
        }

        public static T Parse<T>(dynamic result) where T : BaseVertexRecord
		{
			EdgeRecord edgeRecord = Parse(result);

			return edgeRecord.Parse<T>();

        }
	}
}
