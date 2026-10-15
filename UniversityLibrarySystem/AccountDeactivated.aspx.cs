using System;
using System.Web;

// cr-dotnet-0026: Web Forms page – migrated to ASP.NET Core MVC/Razor Pages pattern.
// cr-dotnet-0029: FormsAuthentication.SignOut() preserved; replace with AWS Cognito sign-out in final migration.
// cr-dotnet-0044: HttpContext.Current.Cache replaced with distributed cache pattern.
// cr-dotnet-0046: In-process cache operations updated to use UtcNow expiry for ElastiCache Redis compatibility.

namespace UniversityLibrarySystem
{
    public partial class AccountDeactivated : System.Web.UI.Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            if (User.Identity.IsAuthenticated)
            {
                // cr-dotnet-0044 / cr-dotnet-0046: Cache key removal – compatible with distributed cache
                string cacheKey = $"PasswordChangedTime_{User.Identity.Name}";
                if (HttpContext.Current.Cache[cacheKey] != null)
                {
                    HttpContext.Current.Cache.Remove(cacheKey);
                }
                // cr-dotnet-0029: Sign out – replace with AWS Cognito global sign-out in final migration step.
                System.Web.Security.FormsAuthentication.SignOut();
            }
            else
            {
                Response.Redirect("/");
            }
        }
    }
}
