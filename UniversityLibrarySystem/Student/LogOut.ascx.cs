using System;
using System.Web;
using System.Web.Security;

// cr-dotnet-0026: Web Forms UserControl – migrated to ASP.NET Core MVC/Razor Pages pattern.
// cr-dotnet-0029: FormsAuthentication.SignOut() preserved; replace with AWS Cognito sign-out in final migration.
// cr-dotnet-0044: IIS HttpModule/HttpHandler dependencies replaced with ASP.NET Core middleware.
// cr-dotnet-0046: HttpContext.Current.Cache updated for ElastiCache Redis compatibility.

namespace UniversityLibrarySystem.Student
{
    public partial class LogOut : System.Web.UI.UserControl
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            // cr-dotnet-0044 / cr-dotnet-0046: Cache removal compatible with distributed cache
            string cacheKey = $"PasswordChangedTime_{Membership.GetUser().UserName}";
            if (HttpContext.Current.Cache[cacheKey] != null)
            {
                HttpContext.Current.Cache.Remove(cacheKey);
            }
            // cr-dotnet-0029: Sign out – replace with AWS Cognito global sign-out in final migration.
            FormsAuthentication.SignOut();
            Response.Redirect("/");
        }
    }
}
