using Discord;
using Discord.WebSocket;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Services;
using Google.Apis.Sheets.v4;
using Google.Apis.Sheets.v4.Data;
using Icarus.Context;
using Icarus.Utils;
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
		static readonly string[] Scopes = { SheetsService.Scope.Spreadsheets };

		public GoogleSheetsService()
		{
			InitializeService();
		}

		private void InitializeService()
		{
		}

		private GoogleCredential GetCredentialsFromFile()
		{
			var icarusConfig = ConfigFactory.GetConfig();


			GoogleCredential credential;
			using (var stream = new FileStream(icarusConfig.GoogleAPIKeyLocation, FileMode.Open, FileAccess.Read))
			{
				credential = GoogleCredential.FromStream(stream).CreateScoped(Scopes);
			}
			return credential;
		}

		/// <summary>
		/// Genereates a SheetContext object for manipulations with a specific spreadsheet
		/// </summary>
		/// <param name="spreadsheetID">The ID of the spreadsheet (i.e. 1lKaBBOA6jUIol3UvvNa_dKmWr3GPT7RWxPeZpu0lV04), it's the only incomprehensible string of characters in a Google Sheets link</param>
		/// <returns>A SheetContext object to use for manipulating the spreadsheet</returns>
		public SheetContext GenerateContext(string spreadsheetID)
		{
			return new SheetContext(_googleSheetValues, spreadsheetID);
		}
	}

	/// <summary>
	/// A class spawned by GoogleSheetsService that manages a specific sheet context
	/// </summary>
	public class SheetContext
	{
		private readonly SpreadsheetsResource.ValuesResource _googleSheetValues;

		public readonly string spreadsheetID;

		public SheetContext(SpreadsheetsResource.ValuesResource googleSheetValues, string sheetID)
		{
			_googleSheetValues = googleSheetValues;
			spreadsheetID = sheetID;
		}

		/// <summary>
		/// Gets the value(s) of a specified range in the spreadsheet
		/// </summary>
		/// <param name="range">A range to read (i.e. A1, A1:B4, SubsheetName!A2)</param>
		/// <returns>A two-dimensional string list containing the values of all cells in the range<br/>
		/// For example: <br/>
		/// range = C3:D4<br/>
		/// value = <br/>
		///  \  |  0  |  1  | <br/>
		/// 0   | C3  | D3  | <br/>
		/// 1   | C4  | D4  | <br/>
		/// </returns>
		public List<List<string>> Get(string range)
		{
			// Make the request to the API
			var request = _googleSheetValues.Get(spreadsheetID, range);
			var response = request.Execute();
			var values = response.Values;
			List<List<string>> stringValues = new List<List<string>>();
			foreach (IList<object> row in values)
			{
				List<string> stringRow = new List<string>();
				foreach (object value in row)
				{
					stringRow.Add((string)value);
				}
				stringValues.Add(stringRow);
			}

			return stringValues;
		}


		/// <summary>
		/// Updates the cells in a range with the specified new values
		/// </summary>
		/// <param name="range">A range to update (i.e. A1, A1:B4, SubsheetName!A2)</param>
		/// <param name="newValues">A two-dimensional string list containing the new values of all cells in the range</param>
		public void Update(string range, List<List<string>> newValues)
		{
			List<IList<object>> valueTable = new List<IList<object>>();
			foreach (List<string> row in newValues)
			{
				List<object> valueRow = new List<object>();
				foreach (string value in row)
				{
					valueRow.Add(value);
				}
				valueTable.Add(valueRow);
			}

			// Make the request to the API
			var valueRange = new ValueRange
			{
				Values = valueTable
			};
			var request = _googleSheetValues.Update(valueRange, spreadsheetID, range);
			request.ValueInputOption = UpdateRequest.ValueInputOptionEnum.USERENTERED;
			request.Execute();
		}
	}
}
