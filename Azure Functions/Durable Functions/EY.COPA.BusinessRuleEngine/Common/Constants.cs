using System;
using System.Collections.Generic;
using System.Text;

namespace EY.COPA.BusinessRuleEngine.Common
{
    public static class Constants
    {

        #region RuleExecuter
        public const string RE_PROTOCOL_SPROC = "[dbo].[usp_Get_{0}_Table]";
        public const string RE_PROTOCOL_TBLRECID = "{0}_EY_ID";
        public const string RE_PROTOCOL_SPROC_INPARAM_FILEID = "@scr_File_Id";
        public const string RE_PROTOCOL_SPROC_INPARAM_CLIENTID = "@client_Id";
        public const string RE_PROTOCOL_SPROC_INPARAM_PRID = "@protocol_Id";
        public const string RE_PROTOCOL_UDU_DF_NM = "UDU_DF_NM";
        public const string RE_PROTOCOL_UDU_DF_VAL = "UDU_DF_VAL";
        public const string RE_PROTOCOL_UDU_FILTER = "BASE_TAB_REC_ID={0}";
        #endregion

        #region Messages
        public const string LOG_MSG = "#COPABRE# {0}";
        public const string LOG_MSG_PROCESS = "#COPABRE# INSTANCE:{0} \r\n PROCESS:{1} \r\n DETAILS:{2}";
        public const string LOG_MSG_VALIDATION = "#COPABRE# INSTANCE:{0} \r\n UNABLE TO  PROCEED DUE TO INVALID REQUEST.\r\n PROCESS:{1} \r\n DETAILS:{2}";
        public const string LOG_MSG_EXCEPTION = "#COPABRE# INSTANCE:{0} \r\nEXCEPTION OCCURED. PROCESS:{1} \r\n DETAILS:{2}";

        #endregion

        

    }
}
