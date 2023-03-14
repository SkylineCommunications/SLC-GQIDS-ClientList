using System;
using System.Collections.Generic;
using System.Linq;
using Skyline.DataMiner.Analytics.GenericInterface;
using Skyline.DataMiner.Net.Messages;

[GQIMetaData(Name = "Connections")]
public class ConnectionSource : IGQIDataSource, IGQIInputArguments, IGQIOnInit
{
	private GQIBooleanArgument _resolveSubscriptions = new GQIBooleanArgument("Resolve Subscriptions") { DefaultValue = false };

	private bool resolveSubscriptions;
	private GQIDMS _dms;

	private List<GQIColumn> _columns;

	public OnInitOutputArgs OnInit(OnInitInputArgs args)
	{
		_dms = args.DMS;
		return default;
	}

	public GQIArgument[] GetInputArguments()
	{
		return new GQIArgument[] { _resolveSubscriptions };
	}

	public OnArgumentsProcessedOutputArgs OnArgumentsProcessed(OnArgumentsProcessedInputArgs args)
	{
		_columns = new List<GQIColumn>
		{
			new GQIStringColumn("Name"),
			new GQIStringColumn("Full Name"),
			new GQIStringColumn("Friendly Name"),
			new GQIStringColumn("Connection ID"),
			new GQIDateTimeColumn("Connect Time"),
			new GQIStringColumn("Attributes"),
		};

		resolveSubscriptions = args.GetArgumentValue(_resolveSubscriptions);

		if (resolveSubscriptions)
		{
			_columns.Add(new GQIDoubleColumn("Number Of subscriptions"));
		}

		return new OnArgumentsProcessedOutputArgs();
	}

	public GQIColumn[] GetColumns()
	{
		return _columns.ToArray();
	}

	public GQIPage GetNextPage(GetNextPageInputArgs args)
	{
		var rows = new List<GQIRow>();
		var Connections = GetConnections();

		foreach (var connection in Connections)
		{
			List<GQICell> cells = new List<GQICell>();

			foreach (var column in _columns)
			{
				switch (column.Name)
				{
					case "Name":
						{
							cells.Add(new GQICell() { Value = connection.Name });
							break;
						}

					case "Full Name":
						{
							cells.Add(new GQICell() { Value = connection.FullName });
							break;
						}

					case "Friendly Name":
						{
							cells.Add(new GQICell() { Value = connection.FriendlyName });
							break;
						}

					case "Connection ID":
						{
							cells.Add(new GQICell() { Value = Convert.ToString(connection.ConnectionID) });
							break;
						}

					case "Connect Time":
						{
							cells.Add(new GQICell() { Value = connection.ConnectTime.ToUniversalTime() });
							break;
						}

					case "Attributes":
						{
							cells.Add(new GQICell() { Value = connection.ConnectionAttributes.ToString() });
							break;
						}

					case "Number Of subscriptions":
						{
							cells.Add(new GQICell() { Value = Convert.ToDouble(GetNumberOfSubscriptions(connection.ConnectionID)) });
							break;
						}
				}
			}

			rows.Add(new GQIRow(cells.ToArray()));
		}

		return new GQIPage(rows.ToArray()) { HasNextPage = false };
	}

	private IEnumerable<LoginInfoResponseMessage> GetConnections()
	{
		var request = new GetInfoMessage(InfoType.ClientList);
		var responses = _dms.SendMessages(request);
		return responses.OfType<LoginInfoResponseMessage>();
	}

	private int GetNumberOfSubscriptions(Guid connectionID)
	{
		var request = new DiagnoseMessage(DiagnoseMessageType.OpenConnections, Convert.ToString(connectionID));
		var response = (TextMessage)_dms.SendMessage(request);
		return response.Text.Split(new string[] { "Subscription Set:" }, StringSplitOptions.RemoveEmptyEntries).Length - 1;
	}
}