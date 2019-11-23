using Microsoft.IdentityModel.Clients.ActiveDirectory;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace EY.COPA.BusinessRuleEngine.Utility
{
    public static class AuthHelper
    {
        public static async Task<string> GetDatabaseToken()
        {
           
            var ctx = new AuthenticationContext(string.Concat("https://login.microsoftonline.com/", ConfigurationReader.TenantId));
            var result = await ctx.AcquireTokenAsync("https://database.windows.net/", new ClientCredential(ConfigurationReader.ClientId, ConfigurationReader.ClientKey));
            return result.AccessToken;
        }

    }
}
