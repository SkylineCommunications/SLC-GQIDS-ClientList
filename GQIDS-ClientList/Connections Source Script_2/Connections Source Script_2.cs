using System;
using System.Collections.Generic;
using System.Linq;
using Skyline.DataMiner.Analytics.GenericInterface;
using Skyline.DataMiner.Net.Messages;

[GQIMetaData(Name = "Connections")]
public class ConnectionSource : IGQIDataSource, IGQIOnInit
{
	private readonly RecordDefinition<LoginInfoResponseMessage> _recordDefinition;

	private GQIDMS _dms;

	public ConnectionSource()
	{
		_recordDefinition = ForProperties(new IPropertyDefinition<LoginInfoResponseMessage>[]
		{
			ForString<LoginInfoResponseMessage>("Name", connection => connection.Name),
			ForString<LoginInfoResponseMessage>("Full name", connection => connection.FullName),
			ForString<LoginInfoResponseMessage>("Friendly name", connection => connection.FriendlyName),
			ForString<LoginInfoResponseMessage>("Connection id", connection => $"{connection.ConnectionID}"),
			ForDateTime<LoginInfoResponseMessage>("Connect time", connection => connection.ConnectTime.ToUniversalTime()),
			ForString<LoginInfoResponseMessage>("Attributes", connection => $"{connection.ConnectionAttributes}"),
		});
	}

	public OnInitOutputArgs OnInit(OnInitInputArgs args)
	{
		_dms = args.DMS;
		return default;
	}

	public GQIColumn[] GetColumns()
	{
		return _recordDefinition.GetColumns();
	}

	public GQIPage GetNextPage(GetNextPageInputArgs args)
	{
		var connections = GetConnections();
		var rows = connections.Select(_recordDefinition.CreateRow);
		return new GQIPage(rows.ToArray()) { HasNextPage = false };
	}

	private IEnumerable<LoginInfoResponseMessage> GetConnections()
	{
		var request = new GetInfoMessage(InfoType.ClientList);
		var responses = _dms.SendMessages(request);
		return responses.OfType<LoginInfoResponseMessage>();
	}

	private PropertyDefinition<TSource, string> ForString<TSource>(string name, Func<TSource, string> accessor)
	{
		var column = new GQIStringColumn(name);
		return new PropertyDefinition<TSource, string>(column, accessor);
	}

	private PropertyDefinition<TSource, DateTime> ForDateTime<TSource>(string name, Func<TSource, DateTime> accessor)
	{
		var column = new GQIDateTimeColumn(name);
		return new PropertyDefinition<TSource, DateTime>(column, accessor);
	}

	private RecordDefinition<TSource> ForProperties<TSource>(IReadOnlyList<IPropertyDefinition<TSource>> properties)
	{
		return new RecordDefinition<TSource>(properties);
	}

	private interface IPropertyDefinition<TSource>
	{
		GQIColumn Column { get; }
		GQICell CreateCell(TSource source);
	}

	private class PropertyDefinition<TSource, TColumn> : IPropertyDefinition<TSource>
	{
		public GQIColumn Column => _column;
		private readonly GQIColumn<TColumn> _column;
		private readonly Func<TSource, TColumn> _accessor;

		public PropertyDefinition(GQIColumn<TColumn> column, Func<TSource, TColumn> accessor)
		{
			_column = column;
			_accessor = accessor;
		}

		public GQICell CreateCell(TSource source)
		{
			var value = _accessor(source);
			return new GQICell { Value = value };
		}
	}

	private class RecordDefinition<TSource>
	{
		private readonly IReadOnlyList<IPropertyDefinition<TSource>> _properties;

		public RecordDefinition(IReadOnlyList<IPropertyDefinition<TSource>> properties)
		{
			_properties = properties;
		}

		public GQIColumn[] GetColumns()
		{
			var columns = _properties.Select(property => property.Column);
			return columns.ToArray();
		}

		public GQIRow CreateRow(TSource source)
		{
			var cells = _properties.Select(property => property.CreateCell(source));
			return new GQIRow(cells.ToArray());
		}
	}
}