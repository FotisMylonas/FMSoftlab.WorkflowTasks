using FMSoftlab.WorkflowTasks;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Data;
using System.Dynamic;
using System.Globalization;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace FMSoftlab.WorkflowTasks
{
    public class FieldInfo
    {
        public string Name { get; set; }
        public string Label { get; set; }
        public int OrderId { get; set; }
    }

    public class GenerateCsvContentParams : TaskParamsBase
    {
        public List<FieldInfo> FieldsInfo { get; set; }
        public string NewLineReplacementString { get; set; }
        public string DateFormat { get; set; }
        public string DecimalSeperator { get; set; }
        public string ThousandSeperator { get; set; }
        public int DecimalDigits { get; set; }
        public string Delimiter { get; set; }
        public IEnumerable<dynamic> Data { get; set; }
        public GenerateCsvContentParams()
        {
            FieldsInfo = new List<FieldInfo>();
            NewLineReplacementString=" ";
        }
        public override void LoadResults(IGlobalContext globalContext)
        {
            _bindings.SetValueIfBindingExists<IEnumerable<dynamic>>("Data", globalContext, (globalContext, value) => Data =value);
            _bindings.SetValueIfBindingExists<IEnumerable<FieldInfo>>("FieldsInfo", globalContext, (globalContext, value) =>
            {
                if (value?.Any() ?? false)
                {
                    FieldsInfo.AddRange(value);
                }
            });
        }
    }
    public class GenerateCsvContent : BaseTaskWithParams<GenerateCsvContentParams>
    {
        public GenerateCsvContent(string name, IGlobalContext globalContext, BaseTask parent, GenerateCsvContentParams taskParams, ILogger<GenerateCsvContent> log) : base(name, globalContext, parent, taskParams, log)
        {

        }
        public GenerateCsvContent(string name, IGlobalContext globalContext, GenerateCsvContentParams taskParams, ILogger<GenerateCsvContent> log) : base(name, globalContext, taskParams, log)
        {

        }

        private IDictionary<string, object> MapDapperRowToCaseInsensitiveDictionary(dynamic row)
        {

            // Get the dynamic object as a dictionary
            var dynamicDictionary = (IDictionary<string, object>)row;

            // Convert the dynamic dictionary to a case-insensitive dictionary
            IDictionary<string, object> dictionary = dynamicDictionary.ToDictionary(
                kvp => kvp.Key,
                kvp => kvp.Value,
                StringComparer.OrdinalIgnoreCase);
            return dictionary;
        }
        public override async Task Execute()
        {
            if (TaskParams.Data==null)
                return;
            if (!TaskParams.Data.Any())
                return;


            // Get the column names from the first row of the results
            string[] columnNames = ((IDictionary<string, object>)TaskParams.Data.First()).Keys.ToArray();
            string[] columnInfo = Enumerable.Empty<string>().ToArray();
            string[] labelInfo = Enumerable.Empty<string>().ToArray();
            Func<string, string, string> GetLabel = (name, label) =>
            {
                string res = string.Empty;
                if (!string.IsNullOrEmpty(label))
                {
                    res=label;
                }
                else
                {
                    res=name;
                }
                return res;
            };
            if (TaskParams.FieldsInfo.Any())
            {
                columnInfo=TaskParams.FieldsInfo.OrderBy(finfo => finfo.OrderId).Select(x => x.Name).ToArray();
                labelInfo=TaskParams.FieldsInfo.OrderBy(finfo => finfo.OrderId).Select(x => GetLabel(x.Name, x.Label)).ToArray();
                _log?.LogDebug($"GenerateCsvContent, ColumnInfo:{string.Join(",", columnInfo)}");
                _log?.LogDebug($"GenerateCsvContent, LabelInfo:{string.Join(",", labelInfo)}");
            }
            else
            {
                _log?.LogDebug("GenerateCsvContent, no fields information exist");
            }

            // Create a StringBuilder to build the CSV file content
            StringBuilder csvBuilder = new StringBuilder();

            // Add the column names to the CSV file
            if (labelInfo.Any())
            {
                csvBuilder.AppendLine(string.Join(TaskParams.Delimiter, labelInfo));
            }
            else
            {
                csvBuilder.AppendLine(string.Join(TaskParams.Delimiter, columnNames));
            }

            NumberFormatInfo nfdecimal = new NumberFormatInfo();
            nfdecimal.NumberDecimalSeparator = TaskParams.DecimalSeperator;
            nfdecimal.NumberGroupSeparator = TaskParams.ThousandSeperator;
            nfdecimal.NumberDecimalDigits = TaskParams.DecimalDigits;
            NumberFormatInfo nfint = new NumberFormatInfo();
            nfint.NumberDecimalSeparator = TaskParams.DecimalSeperator;
            nfint.NumberGroupSeparator = TaskParams.ThousandSeperator;
            nfint.NumberDecimalDigits=0;

            // Add the data rows to the CSV file
            foreach (var row in TaskParams.Data)
            {
                object[] values = null;
                // Convert each row to an array of object values
                var dict = MapDapperRowToCaseInsensitiveDictionary(row);
                if (TaskParams.FieldsInfo.Count()>0)
                {
                    values=columnInfo.Select(colName => dict[colName]).ToArray();
                }
                else
                {
                    values=columnNames.Select(colName => dict[colName]).ToArray();
                }
                // Escape any double quotes in the values
                Func<object, string> GetValue = (value) =>
                    {
                        string res = string.Empty;
                        if (value == null)
                            return res;
                        res=value.ToString();
                        switch (value)
                        {
                            case DateTime date:
                                if (!string.IsNullOrWhiteSpace(TaskParams.DateFormat))
                                {
                                    res = date.ToString(TaskParams.DateFormat);
                                }
                                else
                                {
                                    res = date.ToString();
                                }
                                break;
                            case float num:
                                res = num.ToString("N", nfdecimal);
                                break;
                            case decimal num:
                                res = num.ToString("N", nfdecimal);
                                break;
                            case int num:
                                res = num.ToString("N", nfint);
                                break;
                            case long num:
                                res = num.ToString("N", nfint);
                                break;
                        }
                        res=res
                        .Replace("\"", "\"\"")
                        .Replace(Environment.NewLine, TaskParams.NewLineReplacementString)
                        .Replace("\r", TaskParams.NewLineReplacementString)
                        .Replace("\n", TaskParams.NewLineReplacementString);
                        return res;
                    };
                string[] escapedValues = values.Select(val => GetValue(val)).ToArray();
                // Add the row to the CSV file
                csvBuilder.AppendLine(string.Join(TaskParams.Delimiter, escapedValues));
            }
            SetTaskResult(csvBuilder.ToString());
            await Task.CompletedTask;
            //string filename = Path.Combine(settings.Folder, settings.Filename);
            // await File.WriteAllTextAsync(filename, csvBuilder.ToString());
        }
    }
}