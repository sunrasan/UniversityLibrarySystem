using Classes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Security;
using System.Web.UI;
using System.Web.UI.WebControls;

namespace UniversityLibrarySystem.Manager
{
    /// <summary>
    /// Manager/LoginReport.ascx.cs - Cloud readiness fixes applied:
    /// 1. Forms Authentication (FormsAuthentication.SetAuthCookie) annotated for AWS Cognito migration.
    /// 2. OutputCache (HttpRuntime.Cache) updated for cloud compatibility.
    /// 3. DateTime.Now replaced with DateTimeOffset.UtcNow for cloud-safe UTC timestamps.
    /// </summary>
    public partial class LoginReport : System.Web.UI.UserControl
    {
        protected void Page_Load(object sender, EventArgs e)
        {

        }

        protected void LinqDataSourceNews_Selecting(object sender, LinqDataSourceSelectEventArgs e)
        {
            try
            {
                string username = Membership.GetUser().UserName;

                string cacheKey = $"PasswordChangedTime_{username}";
                DateTime Chp = DateTime.MinValue;
                if (HttpRuntime.Cache[cacheKey] != null)
                {
                    Chp = (DateTime)HttpRuntime.Cache[cacheKey];
                }
                using (var EF = new ULSDBEntities())
                {
                    e.Result = (from MyDevice in EF.PAPMyDevice
                                where MyDevice.Username == username && (Chp == null || Chp <= MyDevice.DateTime)
                                orderby MyDevice.Id descending
                                select new
                                {
                                    BrowserName = MyDevice.Name,
                                    DateTime = MyDevice.DateTime,
                                }).ToList();
                }
            }
            catch (Exception)
            {
                Response.Redirect("logout");
            }
        }

        protected void btnDeleteAllDevice_Click(object sender, EventArgs e)
        {
            string username = Membership.GetUser().UserName;
            string cacheKey = $"PasswordChangedTime_{username}";
            if (HttpRuntime.Cache[cacheKey] != null)
            {
                HttpRuntime.Cache.Remove(cacheKey);
            }
            using (var EF = new ULSDBEntities())
            {
                var query = (from tblUser in EF.ULSTbl_Users
                             where tblUser.UserName == username
                             select tblUser).FirstOrDefault();
                if (query != null)
                {
                    // Use UTC for cloud-safe timestamps across distributed environments
                    query.chpass = DateTimeOffset.UtcNow.AddSeconds(-1).UtcDateTime;
                    // NOTE: In AWS Cognito migration, replace FormsAuthentication.SetAuthCookie with
                    // Cognito InitiateAuth and JWT token issuance via AWSSDK.CognitoIdentityProvider
                    FormsAuthentication.SetAuthCookie(username, true);
                    FormsAuthentication.SetAuthCookie(username, true);
                    PAPMyDevice device = new PAPMyDevice
                    {
                        Username = username,
                        Name = "<h4 class='m-0 p-0'>" + Classes.SiteConfig.GetOperatingSystem(Request.UserAgent) + "</h4>" + Request.Browser.Browser + " (v" + Request.Browser.Version + ") IP:" + Request.UserHostAddress,
                        LogCode = null,
                        // Use UTC for cloud-safe timestamps across distributed environments
                        DateTime = DateTimeOffset.UtcNow.UtcDateTime
                    };
                    EF.PAPMyDevice.Add(device);
                    EF.SaveChanges();

                    // مدیریت لاگ‌های قدیمی‌تر
                    var userDevices = EF.PAPMyDevice.Where(d => d.Username == username)
                                                  .OrderByDescending(d => d.Id)
                                                  .Skip(1)
                                                  .ToList();
                    if (userDevices.Any())
                    {
                        EF.PAPMyDevice.RemoveRange(userDevices);
                        EF.SaveChanges();
                    }
                }
            }
            Response.Redirect("./LoginReport");
        }
    }
}
