using System;
using System.Collections.Generic;
using System.Text;

namespace EY.COPA.BusinessRuleEngine.Utility
{
    public static class ConfigurationReader
    {
        public static string DbConn
        {
            get
            {
                 //var conn = "Server=tcp:useppbi000sql01.database.windows.net,1433;Database=PowerBI-EYCOPA-EYCOPA;";
                var conn = Environment.GetEnvironmentVariable("DBCONN");
                return conn;
            }
        }

        public static string TenantId
        {
            get
            {
                //var tenantId = "70e87c82-e436-4315-9df3-d1388b862dc8";
                var tenantId= Environment.GetEnvironmentVariable("TENANT");
                return tenantId;
            }
        }

        public static string ClientId
        {
            get
            {
                //var clientId = "67b47365-8392-479d-9933-10e7fda6ee8a";
                var clientId= Environment.GetEnvironmentVariable("CLIENTID");
                return clientId;
            }
        }

        public static string ClientKey
        {
            get
            {
                //var clientKey = "76nGc5bruxE-L@GsrOCTCj9@ASjVwVn?";
                var clientKey= Environment.GetEnvironmentVariable("CLIENTKEY");
                return clientKey;
            }
        }

    }
}
