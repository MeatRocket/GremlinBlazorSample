using AngryMonkey.Cloud.GraphDB.Classes;
using System.Net.WebSockets;
using System.Reflection;

namespace AngryMonkey.Cloud.GraphDB
{
	public partial record VertexRecord
	{
		public Guid ID { get; set; }
		public string Label { get; set; }

		public VertexRecord(Guid id, string label)
		{
			ID = id;
			Label = label;
		}

		internal string PartitionKeyValue => Label;

		public List<GraphRecordProperty> Properties { get; set; } = new();

        public static VertexRecord ConvertToVertex<T>(T res) where T : BaseVertexRecord
        {
			VertexRecord vertexRecord = new(res.ID, res._VertexLabel);

            dynamic properties = res.GetType().GetProperties();

			foreach (var property in properties)
			{
				if (property.Name == "ID")
					continue;

                vertexRecord.Properties.Add(new() {
                    //Name = res.GetType().GetProperty(property.Name).ToString(),

                    Name = property.Name,
                    Value = res.GetType().GetProperty(property.Name).GetValue(res)
				});
            }

            return vertexRecord;
        }

        public static VertexRecord? Parse(dynamic result)
		{
			Dictionary<string, object>? resultproperties = result["properties"] ;

			VertexRecord vertexRecord = new(new Guid(result["id"]), result["label"]);

			foreach (var property in resultproperties)
			{
				List<object> prop = (property.Value as IEnumerable<object>).ToList();
				Dictionary<string, object>? value = prop[0] as Dictionary<string, object>;
				object value1 = value["value"];

				vertexRecord.Properties.Add(new()
				{
					Name = property.Key.ToString(),
					Value = value1.ToString()
				});
			}

			return vertexRecord;
		}

		public T Parse<T>() where T : BaseVertexRecord
		{
			T? obj = Activator.CreateInstance(typeof(T)) as T;

			obj.ID = ID;

			foreach (GraphRecordProperty graphProperty in Properties)
			{
				PropertyInfo? propertyInfo = typeof(T).GetProperty(graphProperty.Name);

				if (propertyInfo != null)
                    propertyInfo.SetValue(obj, graphProperty.Value);
                
            }

			return obj;
		}

		public static T Parse<T>(dynamic result) where T : BaseVertexRecord
		{
			VertexRecord vertexRecord = Parse(result);
			return vertexRecord.Parse<T>();
		}
	}
}