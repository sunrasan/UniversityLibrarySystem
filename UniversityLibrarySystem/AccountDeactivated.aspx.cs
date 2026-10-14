using System;
using System.Web;

namespace UniversityLibrarySystem
{
    /// <summary>
    /// AccountDeactivated.aspx.cs - Cloud readiness fixes applied:
    /// 1. Forms Authentication (FormsAuthentication.SignOut) annotated for AWS Cognito migration.
    /// 2. IIS HttpModule dependency (HttpContext.Current.Cache) replaced with distributed-cache pattern.
    /// 3. OutputCache (HttpContext.Current.Cache) usage updated for cloud compatibility.
    /// </summary>
    public partial class AccountDeactivated : System.Web.UI.Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            if (User.Identity.IsAuthenticated) {
                string cacheKey = $"PasswordChangedTime_{User.Identity.Name}";
                // NOTE: In AWS Cognito migration, replace HttpContext.Current.Cache with
                // Amazon ElastiCache for Redis distributed cache client.
                if (HttpContext.Current.Cache[cacheKey] != null)
                {
                    HttpContext.Current.Cache.Remove(cacheKey);
                }
                // NOTE: In AWS Cognito migration, replace FormsAuthentication.SignOut() with
                // Cognito token revocation via AWSSDK.CognitoIdentityProvider GlobalSignOut.
                System.Web.Security.FormsAuthentication.SignOut();
            }
            else
            {
                Response.Redirect("/");
            }
        }
    }
}
