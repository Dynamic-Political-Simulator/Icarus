using Icarus.Context;
using Icarus.Context.Models;
using Icarus.Utils;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using System;
using System.Collections.Generic;
using System.Formats.Asn1;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace Icarus.Services
{
    public class EconVisualsService
    {
        private readonly GoogleSheetsService _googleSheetsService;
        //private readonly ValueManagementService _valueManagementService;
        public EconVisualsService(GoogleSheetsService googleSheetsService) 
        {

            _googleSheetsService = googleSheetsService;
            //_valueManagementService = valueManagementService;

        }


        public async Task UpdateProvinceView(ValueManagementService _valueManagementService)
        {
            var icarusConfig = ConfigFactory.GetConfig();
            string sheetId = icarusConfig.ValueSheetId;
            SheetContext sheetContext = _googleSheetsService.GenerateContext(sheetId);

            using var db = new IcarusContext();

            foreach(Province province in db.Provinces)
            {
                //First Values
                sheetContext.Update($"{province.Name}!A8:D22", GenerateValueTable(province, _valueManagementService));

                //Next Goods
                sheetContext.Update($"{province.Name}!H4:Q6", GenerateGoodTable(province, _valueManagementService));

                //Next Modifiers
                sheetContext.Update($"{province.Name}!H10:Q22", GenerateModifierTable(province, _valueManagementService));

                //Tax Mod
                sheetContext.Update($"{province.Name}!F9:G21", GenerateTaxModTable(province, _valueManagementService));

                //This province is donzo!
            }

            sheetContext.Update($"Nation!H10:Q22", GenerateGlobalModifierTable(db.Nations.First(), _valueManagementService));
        }

        public List<List<string>> GenEmpty(int row, int col)
        {
            List<List<string>> list = new List<List<string>>();

            for (int i = 0; i < row; i++) 
            {
                List<string> sublist = new List<string>();
                for (int y = 0; y < col; y++)
                {
                    sublist.Add(" ");
                }
                list.Add(sublist);
            }
            return list;
        }

        public List<List<string>> GenerateValueTable(Province province, ValueManagementService _valueManagementService)
        {
            List<List<string>> valueTable = GenEmpty(15,4);
            int row = 0;

            foreach (Value value in province.Values)
            {
                valueTable[row][0] = value.Name;
                valueTable[row][1] = value.CurrentValue.ToString();
                valueTable[row][2] = _valueManagementService.GetValueGoal(value).ToString();
                float change = _valueManagementService.GetValueChange(value);
                if (change > 0)
                {
                    valueTable[row][3] = "'+"+_valueManagementService.GetValueChange(value).ToString();
                }
                else
                {
                    valueTable[row][3] = _valueManagementService.GetValueChange(value).ToString();
                }
                
                row ++;
            }

            return valueTable;
        }

        public List<List<string>> GenerateGoodTable(Province province, ValueManagementService _valueManagementService)
        {
            List<List<string>> goodTable = GenEmpty(3, 10);
            int row = 0;
            foreach(Modifier Good in province.Modifiers.Where(m => m.isGood == true))
            {
                goodTable[row][0] = Good.Name;

                int col = 1;
                foreach(ValueModifier mod in Good.Modifiers)
                {
                    goodTable[row][col] = $"{mod.ValueTag}: {mod.Modifier}";
                    col ++;
                }
                row++;
            }

            return goodTable;
        }

        public List<List<string>> GenerateModifierTable(Province province, ValueManagementService _valueManagementService)
        {
            List<List<string>> modTable = GenEmpty(12, 10);
            int row = 0;
            foreach (Modifier Mod in province.Modifiers.Where(m => m.isGood == false))
            {
                modTable[row][0] = Mod.Name;

                int col = 1;
                if ( Mod.Type == ModifierType.Decaying)
                {
                    foreach (ValueModifier mod in Mod.Modifiers)
                    {
                        modTable[row][col] = $"{mod.ValueTag}: {mod.Modifier} ({mod.Decay})";
                        col ++;
                    }

                    modTable[row][9] = "Decaying";
                }
                else
                {
                    foreach (ValueModifier mod in Mod.Modifiers)
                    {
                        modTable[row][col] = $"{mod.ValueTag}: {mod.Modifier}";
                        col ++;
                    }

                    if ( Mod.Type == ModifierType.Permanent)
                    {
                        modTable[row][9] = "Permanent";
                    }
                    else
                    {
                        modTable[row][9] = Mod.Duration.ToString();
                    }
                }

                row++;
            }

            return modTable;
        }

        public List<List<string>> GenerateGlobalModifierTable(Nation nation, ValueManagementService _valueManagementService)
        {
            List<List<string>> modTable = GenEmpty(12, 10);
            int row = 0;
            foreach (Modifier Mod in nation.Modifiers.Where(m => m.isGood == false))
            {
                modTable[row][0] = Mod.Name;

                int col = 1;
                if (Mod.Type == ModifierType.Decaying)
                {
                    foreach (ValueModifier mod in Mod.Modifiers)
                    {
                        modTable[row][col] = $"{mod.ValueTag}: {mod.Modifier} ({mod.Decay})";
                        col++;
                    }

                    modTable[row][9] = "Decaying";
                }
                else
                {
                    foreach (ValueModifier mod in Mod.Modifiers)
                    {
                        modTable[row][col] = $"{mod.ValueTag}: {mod.Modifier}";
                        col++;
                    }

                    if (Mod.Type == ModifierType.Permanent)
                    {
                        modTable[row][9] = "Permanent";
                    }
                    else
                    {
                        modTable[row][9] = Mod.Duration.ToString();
                    }
                }

                row++;
            }

            return modTable;
        }

        public List<List<string>> GenerateTaxModTable(Province province, ValueManagementService _valueManagementService)
        {
            List<List<string>> modTable = GenEmpty(13, 2);

            int row = 0;
            foreach(Modifier mod in province.Modifiers.Where(m => m.WealthMod != 0))
            {
                modTable[row][0] = mod.Name;
                modTable[row][1] = mod.WealthMod.ToString();
                row++;
            }

            return modTable;
        }

        public string GenMapColorCoded(string ValueTag)
        {
            string DataPath = @"./rawislandmap.svg";
            using var db = new IcarusContext();
            XmlDocument xmlDocument = new XmlDocument();
            xmlDocument.Load(DataPath);
            Dictionary<string, float> values = new Dictionary<string, float>();
            Dictionary<string,Color> map = new Dictionary<string,Color>();
            Color lowColor = new Color(new Abgr32(255,0,0));
            Color highColor = new Color(new Abgr32(0,255,0));
            float lowest = 50;
            float highest = 50;
            foreach ( Province province in db.Provinces)
            {
                Value v = province.Values.FirstOrDefault(v => v.TAG == ValueTag);
                if (v.CurrentValue > highest)
                {
                    highest = v.CurrentValue;
                }
                else if (v.CurrentValue < lowest)
                {
                    lowest = v.CurrentValue;
                }

                values.Add(province.Name, v.CurrentValue);
            }
            foreach(KeyValuePair<string, float> kvp in values)
            {

                map.Add(kvp.Key, InterpolateColor(lowColor, highColor, highest, lowest, kvp.Value));
            }
            foreach(XmlElement layer in xmlDocument.SelectNodes("g"))
            {
                string Province = layer.GetAttribute("inkscape: label");
                foreach(XmlElement path in layer.SelectNodes("path"))
                {
                    path.SetAttribute("fill", map[Province].ToHex());
                }
            }
            
            MemoryStream s = new MemoryStream();
            xmlDocument.Save(s);

            return Convert.ToBase64String(s.ToArray());
        }

        private float Lerp(float from, float to, float step)
        {
            return from + (to - from) * step;
        }

        private Color InterpolateColor(Color from, Color to, float max, float min, float current)
        {
            var fromRgba = from.ToPixel<Rgba32>();
            var toRgba = to.ToPixel<Rgba32>();
            float percent = (current -  min)/max;
            var intervalR = Lerp(fromRgba.R, toRgba.R, percent);
            var intervalG = Lerp(fromRgba.G, toRgba.G, percent);
            var intervalB = Lerp(fromRgba.B, toRgba.B, percent);
            var intervalA = Lerp(fromRgba.A, toRgba.A, percent);
            return Color.FromRgba((byte)intervalR, (byte)intervalG, (byte)intervalB, (byte)intervalA);
        }
    }
}
