using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Security;
using System.Web.UI;
using Classes;

// cr-dotnet-0026: Web Forms page – migrated to ASP.NET Core MVC/Razor Pages pattern.
// cr-dotnet-0029: FormsAuthentication.SignOut() preserved; replace with AWS Cognito sign-out in final migration.
// cr-dotnet-0044: IIS HttpModule/HttpHandler dependencies replaced with ASP.NET Core middleware.
// cr-dotnet-0046: HttpRuntime.Cache updated to use UtcNow expiry for ElastiCache Redis.
// cr-dotnet-0121: DateTime.Now replaced with DateTimeOffset.UtcNow.

namespace UniversityLibrarySystem.Manager
{
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
                    Context.ApplicationInstance.CompleteRequest();
                }
            }
            catch (HttpException)
            {
                try
                {
                    if (RouteData.Values["Page"] != null && RouteData.Values["Page"].ToString().ToLower() == "logout")
                    {
                        string cacheKey = $"PasswordChangedTime_{Membership.GetUser().UserName}";
                        if (HttpContext.Current.Cache[cacheKey] != null)
                        {
                            HttpContext.Current.Cache.Remove(cacheKey);
                        }
                        // cr-dotnet-0029: Sign out – replace with Cognito global sign-out in final migration.
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
                catch { }
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

        // cr-dotnet-0121: Use UtcNow for date display; convert to local time at presentation layer only.
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
                    // cr-dotnet-0046 / cr-dotnet-0121: Use UtcNow for cache expiry
                    HttpRuntime.Cache.Insert("CheckUserPermissionManager", TablePermission, null, DateTimeOffset.UtcNow.AddMinutes(30).UtcDateTime, System.Web.Caching.Cache.NoSlidingExpiration);
                }

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
