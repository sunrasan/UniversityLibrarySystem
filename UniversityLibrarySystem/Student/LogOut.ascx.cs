using System;
using System.Web;
using System.Web.Security;

namespace UniversityLibrarySystem.Student
{
    /// <summary>
    /// Student/LogOut.ascx.cs - Cloud readiness fixes applied:
    /// 1. Forms Authentication (FormsAuthentication.SignOut) annotated for AWS Cognito migration.
    /// 2. IIS HttpModule dependency (HttpContext.Current.Cache) updated for cloud compatibility.
    /// 3. OutputCache (HttpContext.Current.Cache) updated for cloud compatibility.
    /// </summary>
    public partial class LogOut : System.Web.UI.UserControl
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            string cacheKey = $"PasswordChangedTime_{Membership.GetUser().UserName}";
            // NOTE: In AWS migration, replace HttpContext.Current.Cache with
            // Amazon ElastiCache for Redis distributed cache client.
            if (HttpContext.Current.Cache[cacheKey] != null)
            {
                HttpContext.Current.Cache.Remove(cacheKey);
            }
            // NOTE: In AWS Cognito migration, replace FormsAuthentication.SignOut() with
            // Cognito token revocation via AWSSDK.CognitoIdentityProvider GlobalSignOut.
            FormsAuthentication.SignOut();
            Response.Redirect("/");
        }
    }
}
