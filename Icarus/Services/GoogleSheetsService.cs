using Discord;
using Discord.WebSocket;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Services;
using Google.Apis.Sheets.v4;
using Google.Apis.Sheets.v4.Data;
using Icarus.Context;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using static Google.Apis.Sheets.v4.SpreadsheetsResource.ValuesResource;

namespace Icarus.Services
{
	/// <summary>
	/// A service that manages interactions with the Google Sheets API.
	/// </summary>
	public class GoogleSheetsService
	{
		private SheetsService GoogleSheets;
		private readonly SpreadsheetsResource.ValuesResource _googleSheetValues;
		private readonly IConfiguration Configuration;
		static readonly string[] Scopes = { SheetsService.Scope.Spreadsheets };

		public GoogleSheetsService(IConfiguration configuration)
		{
			Configuration = configuration;
			InitializeService();
			_googleSheetValues = GoogleSheets.Spreadsheets.Values;
		}

		private void InitializeService()
		{
			var credential = GetCredentialsFromFile();
			GoogleSheets = new SheetsService(new BaseClientService.Initializer()
			{
				HttpClientInitializer = credential,
				ApplicationName = "tanb-test"
			});
		}

		private GoogleCredential GetCredentialsFromFile()
		{
			var icarusConfig = new IcarusConfig();
			Configuration.GetSection("IcarusConfig").Bind(icarusConfig);

			GoogleCredential credential;
			using (var stream = new FileStream(icarusConfig.GoogleAPIKeyLocation, FileMode.Open, FileAccess.Read))
			{
				credential = GoogleCredential.FromStream(stream).CreateScoped(Scopes);
			}
			return credential;
		}

		public string GetCell(string spreadsheetID, string cellID)
		{
			var request = _googleSheetValues.Get(spreadsheetID, cellID);
			var response = request.Execute();
			var value = response.Values[0][0];

			return (string)value;
		}

		public void UpdateCell(string spreadsheetID, string cellID, string newValue)
		{
			var valueRange = new ValueRange
			{
				Values = new List<IList<object>> { new List<object> { newValue } }
			};
			var request = _googleSheetValues.Update(valueRange, spreadsheetID, cellID);
			request.ValueInputOption = UpdateRequest.ValueInputOptionEnum.USERENTERED;
			request.Execute();
		}
	}
}
