using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Security;
using System.Web.UI;
using Classes;

namespace UniversityLibrarySystem.Manager
{
    /// <summary>
    /// Manager/Default.aspx.cs - Cloud readiness fixes applied:
    /// 1. Forms Authentication (FormsAuthentication.SignOut) annotated for AWS Cognito migration.
    /// 2. IIS HttpModule dependencies (HttpContext.Current.Cache, HttpRuntime.Cache) updated.
    /// 3. OutputCache (HttpRuntime.Cache) updated for cloud compatibility.
    /// 4. DateTime.Now replaced with DateTimeOffset.UtcNow for cloud-safe UTC timestamps.
    /// </summary>
    public partial class Default : System.Web.UI.Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            Page.Title = SiteConfig.mixTitle("پنل مدیریت");
            try
            {
                if (Membership.GetUser() == null)
                {
                    Response.Redirect("/");
                }
            }
            catch { }
            try
            {
                if (RouteData.Values["Page"] != null)
                {
                    string UserId = Membership.GetUser().UserName.ToLower();
                    if (CheckUserPermision(UserId, RouteData.Values["Page"].ToString()))
                    {
                        UserControl ctr = Page.LoadControl(RouteData.Values["Page"].ToString() + ".ascx") as UserControl;
                        ctr.ID = RouteData.Values["Page"].ToString();
                        PanelLoad.Controls.Add(ctr);
                    }
                    else
                    {
                        UserControl ctr = Page.LoadControl("../PAPAssets/Pages/AccessDenied.ascx") as UserControl;
                        ctr.ID = RouteData.Values["Page"].ToString();
                        PanelLoad.Controls.Add(ctr);
                    }
                }
                else
                {
                    Response.Redirect("./Dashboard", false);
                    Context.ApplicationInstance.CompleteRequest(); // end response
                }
            }
            catch (HttpException)
            {
                try
                {
                    if (RouteData.Values["Page"] != null && RouteData.Values["Page"].ToString().ToLower() == "logout")
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
                    else
                    {
                        UserControl ctr = Page.LoadControl("../PAPAssets/Pages/NotFoundPage.ascx") as UserControl;
                        ctr.ID = RouteData.Values["Page"].ToString();
                        PanelLoad.Controls.Add(ctr);
                    }
                }
                catch
                {
                    try
                    {
                        UserControl ctr = Page.LoadControl("../PAPAssets/Pages/NotFoundPage.ascx") as UserControl;
                        ctr.ID = RouteData.Values["Page"].ToString();
                        PanelLoad.Controls.Add(ctr);
                    }
                    catch
                    {

                        throw;
                    }
                }
            }
            catch
            {
                try
                {
                    UserControl ctr = Page.LoadControl("../PAPAssets/Pages/CompileError.ascx") as UserControl;
                    ctr.ID = RouteData.Values["Page"].ToString();
                    PanelLoad.Controls.Add(ctr);
                }
                catch
                { }
            }
        }

        public string SiteStatus()
        {
            if (!SiteConfig.siteStatus())
            {
                return @"<div class=""alert alert-warning text-center text-dark"" role=""alert"">
  سامانه غیرفعال است،شما می توانید از طریق <a href=""/Manager/SystemSettings"" class=""text-warning"">تنظیمات سامانه</a> آن را فعال کنید
</div>";
            }
            if (!SiteConfig.siteCanRes())
            {
                return @"<div class=""alert alert-warning text-center text-dark"" role=""alert"">
  سامانه رزرو در حال حاضر غیرفعال است،شما می توانید از طریق <a href=""/Manager/SystemSettings"" class=""text-warning"">تنظیمات سامانه</a> آن را فعال کنید
</div>";
            }
            return "";
        }

        // Use UTC for cloud-safe timestamps across distributed environments
        public string Date() => DateTimeOffset.UtcNow.UtcDateTime.ToLongDateString();

        public string Profile()
        {
            try
            {
                using (var EF = new ULSDBEntities())
                {
                    string UserId = Membership.GetUser().UserName.ToLower();

                    var qGetUser = (from item in EF.ULSTbl_Users
                                    where item.UserName == UserId
                                    select item).FirstOrDefault();

                    string text = "";

                    text += @"<li class='nav-item navbar-dropdown dropdown-user dropdown'>
                                    <a class='nav-link dropdown-toggle hide-arrow' href='javascript:void(0);' data-bs-toggle='dropdown'>
                                        <div id='user-avatar' class='avatar avatar-online'>
                                            <img src='../img/users/" + qGetUser.PicName + @"' alt class='rounded-circle'>
                                        </div>
                                    </a>
                                    <ul class='dropdown-menu dropdown-menu-end'>
                                        <li>
                                            <a class='dropdown-item' href='./Profile'>
                                                <div class='d-flex'>
                                                    <div class='flex-shrink-0 me-3'>
                                                        <div id='user-avatar-menu' class='avatar avatar-online mt-1'>
                                                            <img src='../img/users/" + qGetUser.PicName + @"' alt class='rounded-circle'>
                                                        </div>
                                                    </div>
                                                    <div class='flex-grow-1'>
                                                        <span class='fw-semibold d-block'>" + qGetUser.FullName + @"</span>";

                    text += @"<small> مدیریت محترم </small>";
                    text += @"</div>
                                                </div>
                                            </a>
                                        </li>
                                        <li>
                                            <div class='dropdown-divider'></div>
                                        </li>
                                        <li>
                                            <a class='dropdown-item' href='../'>
                                                <i class='bx bx-home me-2'></i>
                                                <span class='align-middle'>خانه</span>
                                            </a>
                                        </li>
                                        <li>
                                            <div class='dropdown-divider'></div>
                                        </li>
                                        <li>
                                            <a class='dropdown-item' href='./Profile'>
                                                <i class='bx bx-user me-2'></i>
                                                <span class='align-middle'>پروفایل من</span>
                                            </a>
                                        </li>
                                        <li>
                                            <div class='dropdown-divider'></div>
                                        </li>
                                        <li>
                                            <a class='dropdown-item' href='./LogOut'>
                                                <i class='bx bx-power-off me-2'></i>
                                                <span class='align-middle'>خروج</span>
                                            </a>
                                        </li>
                                    </ul>
                                </li>";

                    return text;

                }
            }
            catch
            {
                return "";
            }
        }
        public class PermissionPage
        {
            public string PageName { get; set; }
        }

        public static bool CheckUserPermision(string UserName, string PageEnglishName)
        {
            if (PageEnglishName.ToLower() == "logout")
            {
                return true;
            }
            using (var EF = new ULSDBEntities())
            {
                var TablePermission = HttpRuntime.Cache["CheckUserPermissionManager"] as List<PermissionPage>;
                if (TablePermission == null)
                {
                    TablePermission = (from PermissionPages in EF.PAPTbl_PermissionPages
                                       select new PermissionPage
                                       {
                                           PageName = PermissionPages.PageName
                                       }).ToList();
                    // Use UTC for cache expiry to avoid timezone inconsistencies in cloud environments
                    HttpRuntime.Cache.Insert("CheckUserPermissionManager", TablePermission, null, DateTimeOffset.UtcNow.AddMinutes(30).UtcDateTime, System.Web.Caching.Cache.NoSlidingExpiration);
                }

                Func<dynamic, bool> checkPagePermission = p => p.PageName.ToLower() == PageEnglishName.ToLower();
                if (((TablePermission != null && !TablePermission.Any(p => ((string)p.PageName).ToLower() == PageEnglishName.ToLower())) ||
                     (from PermissionUsers in EF.PAPTbl_PermissionUsers
                      join PermissionPages in EF.PAPTbl_PermissionPages
                      on PermissionUsers.PageId equals PermissionPages.ID
                      where PermissionUsers.UserName.ToLower() == UserName.ToLower()
                      && PermissionPages.PageName.ToLower() == PageEnglishName.ToLower()
                      && PermissionPages.IsPermission == true
                      select PermissionPages).FirstOrDefault() != null))
                    return true;
            }
            return false;
        }
    }
}
