using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace AF_ValidateCSVFile
{
    public static class Function1
    {
        /// <summary>
        /// Azure Function to Validate CSV File used in Data Factory for ETL ops
        /// </summary>
        /// <param name="req"></param>
        /// <param name="log"></param>
        /// <returns></returns>
        [FunctionName("CSVFileValidator")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            log.LogInformation("Triggered File Validator");

            try
            {
                string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
                dynamic data = JsonConvert.DeserializeObject(requestBody);
                var fileJson = data?.fileJson;
                var dbJson = data?.dbJson;

                var isValid = true;
                StringBuilder dbColumn = new StringBuilder("");
                StringBuilder fileColumn = new StringBuilder("");
                StringBuilder tableScript = new StringBuilder("");
                StringBuilder selectQuery = new StringBuilder("");

                string cmsTableName = "";
                string failedColumns = "";
                log.LogInformation("fileJson: " + ((JObject)fileJson).ToString());
                foreach (JToken dbObj in dbJson)
                {
                    log.LogInformation("dbJson: " + ((JObject)dbObj).ToString());
                    var columnId = Convert.ToDouble(((JValue)dbObj["PRD_DF_ORDER_INPUT_FILE"]).Value) - 1;
                    string columnName = ((JValue)dbObj["PRD_DF_BUSINESS_NA"]).Value.ToString();
                    string dbColumnName = ((JValue)dbObj["PRD_DF_NM"]).Value.ToString();
                    if (((JValue)dbObj["PRD_DF_SYSTEM"]).Value.ToString().Equals("CMS"))
                    {
                        cmsTableName = ((JValue)dbObj["PRD_DF_REFERENCE_TABLE"]).Value.ToString();
                    }
                    dbColumn.Append(columnName);
                    dbColumn.Append(",");
                    StringBuilder propertyName = new StringBuilder("Prop_");
                    propertyName.Append(columnId);
                    if (fileJson[propertyName.ToString()] != null)
                    {
                        var fileColumnName = ((JValue)fileJson[propertyName.ToString()]).Value.ToString();
                        fileColumn.Append(fileColumnName);
                        fileColumn.Append(",");
                        if (fileColumnName.ToUpper() != columnName.ToUpper())
                        {
                            isValid = false;
                            failedColumns = failedColumns + ", " + fileColumnName;
                        }
                        else
                        {
                            string shrtFileColumnName = "";
                            if (fileColumnName.Length > 128)
                                shrtFileColumnName = fileColumnName.Substring(0, 128);
                            else
                                shrtFileColumnName = fileColumnName;
                            tableScript.Append("[" + shrtFileColumnName);
                            tableScript.Append("] VARCHAR(MAX), ");
                            if (((JValue)dbObj["PRD_DF_SYSTEM"]).Value.ToString().Equals("CMS"))
                            {
                                selectQuery.Append("[" + shrtFileColumnName + "]");
                                selectQuery.Append(" AS [");
                                selectQuery.Append(dbColumnName);
                                selectQuery.Append("] , ");
                            }

                        }
                    }
                    else
                    {
                        isValid = false;
                    }

                }
                List<Column> sourceStructure;
                List<Column> sinkStructure;
                var result = new
                {
                    DbColumns = dbColumn.ToString(),
                    FileColumns = fileColumn.ToString(),
                    TableScript = tableScript.ToString(),
                    SelectQuery = selectQuery.ToString(),
                    RefTableName = cmsTableName,
                    IsValid = isValid,
                    ColumnMappings = JsonConvert.SerializeObject(GetColumnMapping(fileJson, out sourceStructure, out sinkStructure)),
                    SourceStructure = JsonConvert.SerializeObject(sourceStructure),
                    SinkStructure = JsonConvert.SerializeObject(sinkStructure)
                };
                log.LogInformation("Response: " + result.ToString());
                log.LogInformation("Failed Column: " + failedColumns);
                return new OkObjectResult(result);
            }
            catch (Exception ex)
            {
                log.LogError("Exception: " + ex);
                var result = new
                {
                    DbColumns = "",
                    FileColumns = "",
                    TableScript = "",
                    SelectQuery = ex.Message,
                    IsValid = false
                };
                return new OkObjectResult(result);
            }
        }

        /// <summary>
        /// Function to get column mapping for dynamic columns
        /// </summary>
        /// <param name="fileJson"></param>
        /// <param name="sourceStructure"></param>
        /// <param name="sinkStructure"></param>
        /// <returns></returns>
        private static TranslatorMapping GetColumnMapping(dynamic fileJson, out List<Column> sourceStructure, out List<Column> sinkStructure)
        {
            var lstmapping = new Dictionary<string, string>();
            sourceStructure = new List<Column>();
            sinkStructure = new List<Column>();

            int columnId = 0;
            string propName = "Prop_";
            if(fileJson[string.Concat(propName,columnId)] != null)
            {
                do
                {
                    var fileColumnName = ((JValue)fileJson[string.Concat(propName, columnId)]).Value.ToString();
                    string shrtFileColumnName = "";
                    if (fileColumnName.Length > 128)
                        shrtFileColumnName = fileColumnName.Substring(0, 128);
                    else
                        shrtFileColumnName = fileColumnName;
                    lstmapping.Add(fileColumnName,shrtFileColumnName);

                    sourceStructure.Add(new Column { name = fileColumnName, type = "string" });
                    sinkStructure.Add(new Column { name = shrtFileColumnName, type = "string" });

                    columnId++;
                } while (fileJson[string.Concat(propName, columnId)] != null);
            }
            var mappings = new TranslatorMapping {
                type = "TabularTranslator",
                columnMappings = lstmapping
            };
            return mappings;
        }
    }

    #region Classes for Json Serialisation
    class Column
    {
        public string name { get; set; }
        public string type { get; set; }
    }
    class Mapping
    {
        public string source { get; set; }
        public string sink { get; set; }
    }

    class TranslatorMapping
    {
        public string type { get; set; }
        public Dictionary<string, string> columnMappings { get; set; }
    }
    #endregion
}
