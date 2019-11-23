using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.Data;
using System.Data.SqlClient;
using Microsoft.IdentityModel.Clients.ActiveDirectory;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;
using Microsoft.Azure.Storage;
using Microsoft.Azure.Storage.Blob;
using System.Net.Http;
using System.Net;

namespace AF_ValidateCSVFile
{
    public static class ProcessExportRequest
    {
        [FunctionName("ProcessExportRequest")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            log.LogInformation("Triggered Proecess Request Queue execution.");
            string prqid = req.Query["prqid"];
            try { 
           

            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            dynamic data = JsonConvert.DeserializeObject(requestBody);
            prqid = prqid ?? data?.name;
             

            bool retval = await ExecuteExportRequest(int.Parse(prqid));
                var result = new
                {
                    Status = "Success",
                    ErrorMsg = "",
                    PrqHearderID = prqid,

                };
                return new OkObjectResult(result);
            }
            catch(Exception ex)
            {
                log.LogInformation("Exception: " + ex);
                var result = new
                {
                    Status = "fail",
                    ErrorMsg = ex.Message,
                    PrqHearderID = prqid,
                   
                };
                return new OkObjectResult(result);
            }

        }


        #region "Check DB Connectivity"
        private static async  Task<bool> CheckDbConnection()
        {
            bool retval = false;
            try
            {
                string sqlConstr = Environment.GetEnvironmentVariable("sqldb_connection");

                using (SqlConnection connObj = new SqlConnection(sqlConstr))
                {
                    connObj.AccessToken = await GetDatabaseToken();
                    connObj.Open();
                    retval = true;
                }
            }
            catch (Exception ex)
            {
                
                retval = false;
            }
            return  retval;
        }
        public static async Task<string> GetDatabaseToken()
        {
            string Clientid = Environment.GetEnvironmentVariable("DBClientid"); 
            string ClientSecret = Environment.GetEnvironmentVariable("DBClientSecret"); 
            var ctx = new AuthenticationContext("https://login.microsoftonline.com/70e87c82-e436-4315-9df3-d1388b862dc8");
            var result = await ctx.AcquireTokenAsync(" https://database.windows.net/", new ClientCredential(Clientid, ClientSecret));
            return result.AccessToken;
        }
        #endregion

        #region "Proecess Request Table "
        /// <summary>
        /// Below function has been created to proecess request as per request id.
        /// </summary>
        /// <param name="id">process request queue id</param>
        /// <returns></returns>
        public static async Task<bool> ExecuteExportRequest(int id)
        {

            string sqlConstr = Environment.GetEnvironmentVariable("sqldb_connection");
            int Prq_Header_Id = 0;
            int Prq_Id = 0;
            try
            {

                // Step 1: Check is there any request exists with Pending status in PROCESS_REQUEST_QUEUE table for Specified PRQ_ID and PRQ_Header_ID.
                // Step 2: If not Pending Request Exists then Exit task.
                // Step 3: From Parameter get Program and Universe and from , to Date.
                // Step 4: Update PRQ Status to InProgress and Call SP as per Report type.
                // Step 5: Call SP based on Parameteres and return dataset.
                // Step 6: Define File Name and create file in memory.
                // Step 7: Write DataSet (Step 5) data to File memeory.
                // Step 8: Using File Memory object Upload file to Azure Blob Storage.
                // Step 9: Update PRQ table Status, FileName,FileLink , Updateby Columns.
                // Step 10:Add Notification to User.
                // Step 11:Send email using SendGrid to user to Download Request.


                // Step 1: Check is there any request exists with Pending status in PROCESS_REQUEST_QUEUE table for Specified PRQ_ID and PRQ_Header_ID.

              

                using (SqlConnection connObj = new SqlConnection(sqlConstr))
                {
                    connObj.AccessToken = await GetDatabaseToken();
                    SqlDataAdapter myCommand = new SqlDataAdapter("usp_Get_Process_Request_Queue", connObj);
                    myCommand.SelectCommand.Parameters.Add(new SqlParameter("@PRQ_HEADER_ID", SqlDbType.Int));
                    myCommand.SelectCommand.Parameters["@PRQ_HEADER_ID"].Value = id;
                    myCommand.SelectCommand.CommandType = CommandType.StoredProcedure;
                    DataSet ds = new DataSet();
                    myCommand.Fill(ds);
                    // Step 2: If not Pending Request Exists for requested PRQID then Exit task.
                    if (ds.Tables[0].Rows.Count > 0)
                    {
                        DataTable dt = ds.Tables[0];
                        string UserName = string.Empty;
                        string UsermailID = string.Empty;
                        int UserID=0;
                        string NotificationFlag = string.Empty;
                        

                        foreach (DataRow row in dt.Rows)
                        {
                            // Step 3: From Parameter get Program,start date,end date,reprot type,universe etc.
                             Prq_Id = int.Parse(row[0].ToString());
                            Prq_Header_Id = int.Parse(row[1].ToString());
                            string strPrqParameters = row[5].ToString();
                            string FtrType = row[7].ToString();
                            UserName = row[10].ToString();
                            UserID = int.Parse(row[11].ToString());
                            NotificationFlag = row[13].ToString();
                            UsermailID = row[14].ToString();
                            // Parameter Format (Comma Seperated)  : {programName,fromdate,todate,reportType,FileHeaderFlag,universe}
                            string[] exportParameters = strPrqParameters.Split(',');
                            string ProgramName = exportParameters[0].ToString();
                            string FromDateVal = exportParameters[1].ToString().Replace("12:00:00", " ");
                            string ToDateVal = exportParameters[2].ToString().Replace("12:00:00", " ");
                            string ReportType = exportParameters[3].ToString();
                            string FileHeaderFlag = exportParameters[4].ToString();
                            string UniverseName = exportParameters[5].ToString();
                            

                            // Step 4: Update PRQ Status to InProgress and Call SP as per Report type.
                            myCommand.Dispose();
                            bool retval = UpdatePRQStatus(connObj, Prq_Id, Prq_Header_Id, "INPROGRESS", "", "", "", "");

                            // Step 5: Call SP based on Parameteres and return dataset.
                            DataSet retvalDataSet = GetProcessRequestData(connObj, exportParameters);

                            if (retvalDataSet.Tables[0].Columns[0].ToString() == "ErrorNumber")
                            {
                                throw new Exception(retvalDataSet.Tables[0].Rows[0][5].ToString());
                            }
                            // Step 6: Define File Name and create file in memory. (ProgramName+"_"+UniverseName+"_"UserName + "_" + DateTime.Now.ToString("yyyyMMddHHmmss") + FtrType )
                            string fileName = ProgramName+"_"+ UniverseName+"_"+ ReportType + "_" + DateTime.Now.ToString("yyyyMMddHHmmss") + FtrType;
                            // Step 7: Write DataSet (Step 5) data to File memeory.
                            MemoryStream ms = new MemoryStream();
                           switch (FtrType)
                            {
                                case ".csv":
                                    ms=WriteCsvFile(retvalDataSet.Tables[0], ReportType, FromDateVal, ToDateVal, FileHeaderFlag);
                                    break;
                                case ".xlsx":
                                    using (SpreadsheetDocument document = SpreadsheetDocument.Create(ms, SpreadsheetDocumentType.Workbook))
                                    {
                                        WriteExcelFile(retvalDataSet, document, ReportType, FromDateVal, ToDateVal, FileHeaderFlag);
                                    }
                                    break;

                                case ".txt":
                                    WritetxtFile(retvalDataSet.Tables[0], ms, ReportType, FromDateVal, ToDateVal, FileHeaderFlag);
                                    break;
                                    

                            }
                            
                            //You need to create a storage account and put your azure storage connection string in following place
                            // Step 8: Using File Memory object Upload file to Azure Blob Storage.
                            string Constr = Environment.GetEnvironmentVariable("StorageConnection");
                            CloudStorageAccount storageAccount = CloudStorageAccount.Parse(Constr);
                            CloudBlobClient blobClient = storageAccount.CreateCloudBlobClient();
                            CloudBlobContainer container = blobClient.GetContainerReference("processrequestcontainer");
                            container.CreateIfNotExists();

                            CloudBlockBlob blockBlob = container.GetBlockBlobReference(fileName);
                            ms.Position = 0;
                            blockBlob.UploadFromStream(ms);
                            ms.Flush();

                            string storageUri = Environment.GetEnvironmentVariable("storageUri");
                            string strAccessToken = Environment.GetEnvironmentVariable("AccessToken");

                            string FileLink = storageUri + fileName + strAccessToken;
                            // Step 9: Update PRQ table Status, FileName,FileLink , Updateby Columns.
                           bool retvalupdate = UpdatePRQStatus(connObj, Prq_Id, Prq_Header_Id, "DONE", fileName, FileLink,"","");
                     
                        }

                        // Step 10:Add Notification to User if notification is required by Proecess Request.
                        if (NotificationFlag == "Y")
                        {
                            bool retvalNotification = AddNotification(connObj, UserID, Prq_Header_Id, UserName);
                            // Call Mail Notification SP here and get EQID back.

                            if (retvalNotification == true)
                            { 
                                int EQ_ID = AddEmailNotification(connObj, Environment.GetEnvironmentVariable("mailfromgroupid"), UsermailID, 2, UserName, id);
                                // Web Hook to mail notification service.
                                HttpClient client = new HttpClient();
                                var url = Environment.GetEnvironmentVariable("mailnotificationurl") + EQ_ID + Environment.GetEnvironmentVariable("mailnotificationazurecode");
                                var response = await client.GetAsync(url);
                                string result = await response.Content.ReadAsStringAsync();
                            }


                        }
                    }
                    else
                    {
                        //TODO : Make SQL DB Log.
                        // NO Valid Record exist for requested PRQ ID.
                        throw new Exception("Invalid Request ID.");
                        //return false;
                    }

                }

                return true;
            }
            catch (Exception ex)
            {
                using (SqlConnection connObjError = new SqlConnection(sqlConstr))
                {
                    connObjError.AccessToken = await GetDatabaseToken();
                    bool retval = UpdatePRQStatus(connObjError, Prq_Id, Prq_Header_Id, "ERROR", "", "", "0001", ex.Message + " Request ID : " + Prq_Header_Id);    
                }
                   
              
                throw ex;
            }
        }

        /// <summary>
        /// UpdatePRQStatus():Below function has been created to Update Process Request Queue table.
        /// </summary>
        /// <param name="connObj">sql connection object</param>
        /// <param name="PRQ_ID">Process Request ID</param>
        /// <param name="Prq_Header_Id">Proecess Request Header ID</param>
        /// <param name="Status">Proecess Request status </param>
        /// <param name="FileName">Azure uploaded file name </param>
        /// <param name="FileLink">Azure uploaded file link</param>
        /// <returns></returns>
        private static bool UpdatePRQStatus(SqlConnection connObj, int PRQ_ID, int Prq_Header_Id, string Status, string FileName, string FileLink,string ErrorCode,string ErrorDesc)
        {

            bool resultSet = false;
            using (SqlCommand sqlRenameCommand = new SqlCommand("usp_Update_Process_Request_Queue", connObj))
            {

                sqlRenameCommand.CommandType = CommandType.StoredProcedure;
                sqlRenameCommand.Parameters.Add("@PRQ_ID", SqlDbType.Int).Value = PRQ_ID;
                sqlRenameCommand.Parameters.Add("@PRQ_HEADER_ID", SqlDbType.Int).Value = Prq_Header_Id;
                sqlRenameCommand.Parameters.Add("@PRQ_STATUS", SqlDbType.VarChar).Value = Status;
                sqlRenameCommand.Parameters.Add("@FILE_NAME", SqlDbType.VarChar).Value = FileName;
                sqlRenameCommand.Parameters.Add("@FILE_LINK", SqlDbType.VarChar).Value = FileLink;
                sqlRenameCommand.Parameters.Add("@ERROR_CODE", SqlDbType.VarChar).Value = ErrorCode;
                sqlRenameCommand.Parameters.Add("@ERROR_DESC", SqlDbType.VarChar).Value = ErrorDesc;
                connObj.Open();
                sqlRenameCommand.ExecuteNonQuery();
                connObj.Close();
                resultSet = true;
            }
            return resultSet;

        }
        /// <summary>
        /// GetProcessRequestData():Below function has been created to Get Reuqested Data for PRQ request  and return DataSet
        /// </summary>
        /// <param name="connObj">sql connection object</param>
        /// <param name="parameters">string array with parameters values.</param>
        /// <returns></returns>
        private static DataSet GetProcessRequestData(SqlConnection connObj, string[] parameters)
        {
            DataSet resultSet = new DataSet();

            SqlDataAdapter myCommand = new SqlDataAdapter("usp_Get_Process_Request_Data", connObj);

            myCommand.SelectCommand.Parameters.Add(new SqlParameter("@program_Name", SqlDbType.VarChar));
            myCommand.SelectCommand.Parameters["@program_Name"].Value = parameters[0].ToString();

            myCommand.SelectCommand.Parameters.Add(new SqlParameter("@start_Date", SqlDbType.DateTime2));
            myCommand.SelectCommand.Parameters["@start_Date"].Value = parameters[1].ToString();

            myCommand.SelectCommand.Parameters.Add(new SqlParameter("@End_Date", SqlDbType.DateTime2));
            myCommand.SelectCommand.Parameters["@End_Date"].Value = parameters[2].ToString();

            myCommand.SelectCommand.Parameters.Add(new SqlParameter("@report_type", SqlDbType.VarChar));
            myCommand.SelectCommand.Parameters["@report_type"].Value = parameters[3].ToString();

            myCommand.SelectCommand.Parameters.Add(new SqlParameter("@universe_Name", SqlDbType.VarChar));
            myCommand.SelectCommand.Parameters["@universe_Name"].Value = parameters[5].ToString();


            myCommand.SelectCommand.CommandType = CommandType.StoredProcedure;
            myCommand.Fill(resultSet);
            myCommand.Dispose();
            return resultSet;
        }
        /// <summary>
        ///  AddNotification(): below function has been created to add notification for download file.
        /// </summary>
        /// <param name="connObj">sql connection object</param>
        /// <param name="User_Id">user id</param>
        /// <param name="Prq_Header_Id">Process Request Header ID</param>
        /// <param name="User_Name">User Name </param>
        /// <returns></returns>
        private static bool AddNotification(SqlConnection connObj, int User_Id, int Prq_Header_Id, string User_Name)
        {
            bool retval = false;

            using (SqlCommand sqlRenameCommand = new SqlCommand("usp_Add_Notification", connObj))
            {

                sqlRenameCommand.CommandType = CommandType.StoredProcedure;
                sqlRenameCommand.Parameters.Add("@user_id", SqlDbType.Int).Value = User_Id;
                sqlRenameCommand.Parameters.Add("@prq_header_id", SqlDbType.Int).Value = Prq_Header_Id;
                sqlRenameCommand.Parameters.Add("@created_by", SqlDbType.VarChar).Value = User_Name;
                connObj.Open();
                sqlRenameCommand.ExecuteNonQuery();
                connObj.Close();
                retval = true;
            }
            return retval;


        }

        /// <summary>
        ///  AddEmailNotification(): below function has been created to add notification for download file.
        /// </summary>
        /// <param name="connObj">sql connection object</param>
        /// <param name="User_Id">user id</param>
        /// <param name="Prq_Header_Id">Process Request Header ID</param>
        /// <param name="User_Name">User Name </param>
        /// <returns></returns>
        private static int AddEmailNotification(SqlConnection connObj, string from,string To,int MailTemplateId,string CreatedBy,int PRQ_ID)
        {
            int eqid=0;

            using (SqlCommand sqlRenameCommand = new SqlCommand("usp_Prepare_Mail_Queue_Request", connObj))
            {

                sqlRenameCommand.CommandType = CommandType.StoredProcedure;
                sqlRenameCommand.Parameters.Add("@MQ_From", SqlDbType.VarChar).Value = from;
                sqlRenameCommand.Parameters.Add("@MQ_To", SqlDbType.VarChar).Value = To;
                sqlRenameCommand.Parameters.Add("@MQ_TemplateId", SqlDbType.Int).Value = MailTemplateId;
                sqlRenameCommand.Parameters.Add("@MQ_CreatedBy", SqlDbType.VarChar).Value = CreatedBy;
                sqlRenameCommand.Parameters.Add("@PRQ_ID", SqlDbType.Int).Value = PRQ_ID;
                connObj.Open();
               var retvaleqid = sqlRenameCommand.ExecuteScalar();
                eqid = Convert.ToInt32(retvaleqid);
                connObj.Close();
                
            }
            return eqid;


        }

        #endregion

        #region "Write to File(.xlsx,.csv,.txt)"
        public static void WriteExcelFile(DataSet ds, SpreadsheetDocument document, string ReportType, string fromdate, string ToDate, string FileHeaderFlag)
        {
            WorkbookPart workbookPart = document.AddWorkbookPart();
            workbookPart.Workbook = new Workbook();

            WorksheetPart worksheetPart = workbookPart.AddNewPart<WorksheetPart>();
            var sheetData = new SheetData();
            worksheetPart.Worksheet = new Worksheet(sheetData);


            Sheets sheets = workbookPart.Workbook.AppendChild(new Sheets());
            Sheet sheet = new Sheet() { Id = workbookPart.GetIdOfPart(worksheetPart), SheetId = 1, Name = "Sheet1" };

            sheets.Append(sheet);

            Row headerRow = new Row();
            WriteDataTableToExcelWorksheet(ds.Tables[0], worksheetPart, ReportType, fromdate, ToDate, FileHeaderFlag);

            workbookPart.Workbook.Save();
        }
        /// <summary>
        ///  Below function has been created to write data in Excle MS object.
        /// </summary>
        /// <param name="dt">Data Table </param>
        /// <param name="worksheetPart">worksheet object</param>
        private static void WriteDataTableToExcelWorksheet(DataTable dt, WorksheetPart worksheetPart,string ReportType,string fromdate ,string ToDate, string FileHeaderFlag)
        {

            var worksheet = worksheetPart.Worksheet;
            var sheetData = worksheet.GetFirstChild<SheetData>();

            string cellValue = "";
            //  Create a Header Row in our Excel file, containing one header for each Column of data in our DataTable.
            //
            //  We'll also create an array, showing which type each column of data is (Text or Numeric), so when we come to write the actual
            //  cells of data, we'll know if to write Text values or Numeric cell values.
            int numberOfColumns = dt.Columns.Count;
            bool[] IsNumericColumn = new bool[numberOfColumns];

            string[] excelColumnNames = new string[numberOfColumns];
            for (int n = 0; n < numberOfColumns; n++)
                excelColumnNames[n] = GetExcelColumnName(n);

            uint rowIndex = 1;

            if (FileHeaderFlag.ToUpper()=="Y")
            { 
            var headerRowReportTitle = new Row { RowIndex = 1 };  // add a row at the top of spreadsheet
            sheetData.Append(headerRowReportTitle);
            DataColumn coltitle = new DataColumn("Title");
            AppendTextCell( "A1", coltitle.ColumnName, headerRowReportTitle);
            AppendTextCell("B1", ReportType, headerRowReportTitle);

            var headerDateRange = new Row { RowIndex = 2 };  // add a row at the top of spreadsheet
            sheetData.Append(headerDateRange);
            
            AppendTextCell("A2", "Date Range", headerDateRange);
            AppendTextCell("B2",  fromdate + " To " + ToDate, headerDateRange);
            rowIndex = 3;
            }

            //
            //  Create the Header row in our Excel Worksheet
            //
           

            var headerRow = new Row { RowIndex = rowIndex };  // add a row at the top of spreadsheet
            sheetData.Append(headerRow);

            for (int colInx = 0; colInx < numberOfColumns; colInx++)
            {
                DataColumn col = dt.Columns[colInx];
                AppendTextCell(excelColumnNames[colInx] + rowIndex, col.ColumnName, headerRow);
                IsNumericColumn[colInx] = (col.DataType.FullName == "System.Decimal") || (col.DataType.FullName == "System.Int32");
            }

            //
            //  Now, step through each row of data in our DataTable...
            //
            double cellNumericValue = 0;
            foreach (DataRow dr in dt.Rows)
            {
                // ...create a new row, and append a set of this row's data to it.
                ++rowIndex;
                var newExcelRow = new Row { RowIndex = rowIndex };  // add a row at the top of spreadsheet
                sheetData.Append(newExcelRow);

                for (int colInx = 0; colInx < numberOfColumns; colInx++)
                {
                    cellValue = dr.ItemArray[colInx].ToString();

                    // Create cell with data
                    if (IsNumericColumn[colInx])
                    {
                        //  For numeric cells, make sure our input data IS a number, then write it out to the Excel file.
                        //  If this numeric value is NULL, then don't write anything to the Excel file.
                        cellNumericValue = 0;
                        if (double.TryParse(cellValue, out cellNumericValue))
                        {
                            cellValue = cellNumericValue.ToString();
                            AppendNumericCell(excelColumnNames[colInx] + rowIndex.ToString(), cellValue, newExcelRow);
                        }
                    }
                    else
                    {
                        //  For text cells, just write the input data straight out to the Excel file.
                        AppendTextCell(excelColumnNames[colInx] + rowIndex.ToString(), cellValue, newExcelRow);
                    }
                }
            }

        }



        private static void AppendTextCell(string cellReference, string cellStringValue, Row excelRow)
        {
            //  Add a new Excel Cell to our Row 
            Cell cell = new Cell() { CellReference = cellReference, DataType = CellValues.String };
            CellValue cellValue = new CellValue();
            cellValue.Text = cellStringValue;
            cell.Append(cellValue);
            excelRow.Append(cell);
        }

        private static void AppendNumericCell(string cellReference, string cellStringValue, Row excelRow)
        {
            //  Add a new Excel Cell to our Row 
            Cell cell = new Cell() { CellReference = cellReference };
            CellValue cellValue = new CellValue();
            cellValue.Text = cellStringValue;
            cell.Append(cellValue);
            excelRow.Append(cell);
        }

        private static string GetExcelColumnName(int columnIndex)
        {
            //  Convert a zero-based column index into an Excel column reference  (A, B, C.. Y, Y, AA, AB, AC... AY, AZ, B1, B2..)
            //
            //  eg  GetExcelColumnName(0) should return "A"
            //      GetExcelColumnName(1) should return "B"
            if (columnIndex < 26)
                return ((char)('A' + columnIndex)).ToString();

            char firstChar = (char)('A' + (columnIndex / 26) - 1);
            char secondChar = (char)('A' + (columnIndex % 26));

            return string.Format("{0}{1}", firstChar, secondChar);
        }
        /// <summary>
        /// below function is created to write data in.csv file.
        /// </summary>
        /// <param name="dtDataTable"></param>
        /// <param name="strFilePath"></param>
        public static MemoryStream WriteCsvFile(this DataTable dtDataTable, string ReportType, string fromdate, string ToDate, string FileHeaderFlag)
        {
            MemoryStream ReturnStream = new MemoryStream();
            StreamWriter sw = new StreamWriter(ReturnStream);
            //headers 
            if (FileHeaderFlag.ToUpper() == "Y")
            {
                sw.Write("Title : " + ReportType);
                sw.Write(sw.NewLine);
                sw.Write("Date Range : " + fromdate + " To " + ToDate);
                sw.Write(sw.NewLine);
            }
            for (int i = 0; i < dtDataTable.Columns.Count; i++)
            {
                string value = dtDataTable.Columns[i].ToString();
                if (value.Contains(','))
                {
                    value = String.Format("\"{0}\"", value);
                }
                    sw.Write(value);
                if (i < dtDataTable.Columns.Count - 1)
                {
                    sw.Write(",");
                }
            }
          
            sw.Write(sw.NewLine);
          
            foreach (DataRow dr in dtDataTable.Rows)
            {
                               
                for (int i = 0; i < dtDataTable.Columns.Count; i++)
                {
                    if (!Convert.IsDBNull(dr[i]))
                    {
                        string value = dr[i].ToString();
                        if (value.Contains(','))
                        {
                            value = String.Format("\"{0}\"", value);
                            sw.Write(value);
                        }
                        else
                        {
                            sw.Write(dr[i].ToString());
                        }
                    }
                    if (i < dtDataTable.Columns.Count - 1)
                    {
                        sw.Write(",");
                    }
                }
               
                sw.Write(sw.NewLine);
                sw.Flush();
            }

            
            return ReturnStream;


        }
        /// <summary>
        /// below function is created to write data in.txt file.
        /// </summary>
        /// <param name="dtDataTable"></param>
        /// <param name="ms"></param>
        public static void WritetxtFile(this DataTable dtDataTable, MemoryStream ms, string ReportType, string fromdate, string ToDate,string FileHeaderFlag)
        {
            StreamWriter sw = new StreamWriter(ms);
            //headers  
            if (FileHeaderFlag.ToUpper() == "Y")
            { 
            sw.Write("Title : " + ReportType);
            sw.Write(sw.NewLine);
            sw.Write("Date Range : " + fromdate + " To " + ToDate);
            sw.Write(sw.NewLine);
            }
            for (int i = 0; i < dtDataTable.Columns.Count; i++)
            {
                sw.Write(dtDataTable.Columns[i]);
                if (i < dtDataTable.Columns.Count - 1)
                {
                    sw.Write("|");
                }
            }
            sw.Write(sw.NewLine);
            foreach (DataRow dr in dtDataTable.Rows)
            {
                for (int i = 0; i < dtDataTable.Columns.Count; i++)
                {
                    if (!Convert.IsDBNull(dr[i]))
                    {
                        string value = dr[i].ToString();
                        if (value.Contains('|'))
                        {
                            value = String.Format("\"{0}\"", value);
                            sw.Write(value);
                        }
                        else
                        {
                            sw.Write(dr[i].ToString());
                        }
                    }
                    if (i < dtDataTable.Columns.Count - 1)
                    {
                        sw.Write("|");
                    }
                }
                sw.Write(sw.NewLine);
                sw.Flush();
            }

        }

        #endregion

    }
}
