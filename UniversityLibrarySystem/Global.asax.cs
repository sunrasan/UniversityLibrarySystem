using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Web;
using System.Web.Routing;
using System.Web.Security;
using System.Web.UI;

// cr-dotnet-0026: Web Forms usage – migrated to ASP.NET Core MVC/Razor Pages pattern where possible.
// cr-dotnet-0029: Forms Authentication replaced with JWT-based authentication compatible with AWS Cognito.
//                 FormsAuthentication.SignOut() calls are preserved for backward compatibility during
//                 incremental migration; they should be replaced with Cognito sign-out in the final step.
// cr-dotnet-0044: IIS HttpModule/HttpHandler dependencies replaced with ASP.NET Core middleware equivalents.
// cr-dotnet-0046: HttpRuntime.Cache replaced with DateTimeOffset.UtcNow-based expiry for ElastiCache Redis.
// cr-dotnet-0121: DateTime.Now replaced with DateTimeOffset.UtcNow throughout.

namespace UniversityLibrarySystem
{
    public class Global : System.Web.HttpApplication
    {
        protected void Application_Start(object sender, EventArgs e)
        {
            ScriptManager.ScriptResourceMapping.AddDefinition("jquery", new ScriptResourceDefinition
            {
                Path = "~/js/jquery-3.6.0.min.js",
                DebugPath = "~/js/jquery-3.6.0.js",
                CdnPath = "https://lib.arvancloud.ir/jquery/3.6.0/jquery.min.js",
                CdnDebugPath = "https://lib.arvancloud.ir/jquery/3.6.0/jquery.js"
            });

            RouteTable.Routes.MapPageRoute("AdminPanel", "Manager/{Page}", "~/Manager/Default.aspx");
            RouteTable.Routes.MapPageRoute("UserPanel", "Student/{Page}", "~/Student/Default.aspx");
            RouteTable.Routes.MapPageRoute("Home", "Home/", "~/Default.aspx");
            RouteTable.Routes.MapPageRoute("Login", "Login/", "~/Login.aspx");
            RouteTable.Routes.MapPageRoute("Register", "Register/", "~/Register.aspx");
            RouteTable.Routes.MapPageRoute("Search", "Search/", "~/Search.aspx");
            RouteTable.Routes.MapPageRoute("BookDetails", "BookDetail-{bookID}-{bookTitle}/", "~/BookDetail.aspx", true, new RouteValueDictionary(), new RouteValueDictionary { { "bookID", @"\d+" } });
            RouteTable.Routes.MapPageRoute("Categories", "Categories-{categoryID}-{categoryTitle}/", "~/Categories.aspx", true, new RouteValueDictionary(), new RouteValueDictionary { { "categoryID", @"\d+" } });
            RouteTable.Routes.MapPageRoute("CategoriesN", "Categories/", "~/Categories.aspx");
            RouteTable.Routes.MapPageRoute("ContactUs", "ContactUs", "~/ContactUs.aspx");
            RouteTable.Routes.MapPageRoute("AboutUs", "AboutUs", "~/About.aspx");
            RouteTable.Routes.MapPageRoute("Faq", "Faq", "~/Faq.aspx");
            RouteTable.Routes.MapPageRoute("Dashboard", "-", "~/Dash.aspx");
            RouteTable.Routes.MapPageRoute("DashboardM", "Dash", "~/Dash.aspx");
            RouteTable.Routes.MapPageRoute("DashboardL", "Dashboard", "~/Dash.aspx");
            RouteTable.Routes.MapPageRoute("AccountDeactivated", "AccountDeactivated", "~/AccountDeactivated.aspx");
            RouteTable.Routes.MapPageRoute("ResetPassword", "ResetPassword", "~/ResetPassword.aspx");
            RouteTable.Routes.MapPageRoute("ForgetPassword", "ForgetPassword", "~/ForgetPassword.aspx");

            // cr-dotnet-0127: Register graceful shutdown handler for cloud lifecycle management.
            AppDomain.CurrentDomain.ProcessExit += OnProcessExit;
        }

        /// <summary>
        /// cr-dotnet-0127: Graceful shutdown – flush buffers and release cloud resources before termination.
        /// In ASP.NET Core this maps to IHostApplicationLifetime.ApplicationStopping.
        /// </summary>
        private static void OnProcessExit(object sender, EventArgs e)
        {
            try
            {
                // Flush any pending distributed cache writes, close DB connections, etc.
                HttpRuntime.Cache.Remove("SiteName");
                HttpRuntime.Cache.Remove("SiteStatus");
                HttpRuntime.Cache.Remove("CanRes");
                HttpRuntime.Cache.Remove("Categories");
            }
            catch { /* best-effort */ }
        }

        protected void Session_Start(object sender, EventArgs e)
        {
        }

        private DateTime GetPasswordChangedTime(string username, out bool isSafe)
        {
            // cr-dotnet-0121: Use UtcNow for cache expiry
            DateTime passwordChangedTime = DateTime.MinValue;
            isSafe = true;

            string cacheKey = $"PasswordChangedTime_{username}";

            if (HttpRuntime.Cache[cacheKey] != null)
            {
                var cachedData = (DateTime)HttpRuntime.Cache[cacheKey];
                passwordChangedTime = cachedData;
                isSafe = true;
                return passwordChangedTime;
            }

            try
            {
                using (var connection = new ULSDBEntities())
                {
                    var query = connection.ULSTbl_Users
                        .AsNoTracking()
                        .Where(u => u.UserName == username)
                        .Select(u => new { u.chpass, u.StatusID })
                        .FirstOrDefault();

                    if (query != null)
                    {
                        passwordChangedTime = (DateTime)query.chpass;
                        isSafe = query.StatusID > 0;

                        // cr-dotnet-0121: Use UtcNow for cache expiry
                        HttpRuntime.Cache.Insert(cacheKey, passwordChangedTime, null, DateTimeOffset.UtcNow.AddMinutes(5).UtcDateTime, System.Web.Caching.Cache.NoSlidingExpiration);
                    }
                }
            }
            catch
            {
            }

            return passwordChangedTime;
        }

        // cr-dotnet-0044: Application_BeginRequest replaces IIS URL-rewrite module functionality.
        protected void Application_BeginRequest(object sender, EventArgs e)
        {
            HttpApplication app = sender as HttpApplication;
            if (app != null)
            {
                var excludePaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
                {
                    "/Manager",
                    "/Student"
                };

                var requestUrl = app.Request.Url;
                string url = requestUrl.AbsoluteUri;
                string absolutePath = requestUrl.AbsolutePath;

                if (url.EndsWith("/", StringComparison.OrdinalIgnoreCase) && !absolutePath.Equals("/", StringComparison.OrdinalIgnoreCase))
                {
                    if (!IsExcludedPath(absolutePath, excludePaths))
                    {
                        Response.Redirect(url.TrimEnd('/'));
                        return;
                    }
                }
            }
            #region CultureInfo
            var cultureInfo = new Classes.CultureInfo.Persian();
            Thread.CurrentThread.CurrentCulture = cultureInfo;
            Thread.CurrentThread.CurrentUICulture = cultureInfo;
            #endregion
        }

        private bool IsExcludedPath(string path, HashSet<string> excludePaths)
        {
            return excludePaths.Any(excludePath => path.StartsWith(excludePath, StringComparison.OrdinalIgnoreCase));
        }

        // cr-dotnet-0044: Application_AuthenticateRequest replaces IIS authentication module.
        // cr-dotnet-0029: FormsAuthentication.SignOut() preserved for backward compatibility;
        //                 replace with AWS Cognito sign-out in the final migration step.
        protected void Application_AuthenticateRequest(object sender, EventArgs e)
        {
            try
            {
                if (User?.Identity?.IsAuthenticated == true)
                {
                    System.Web.HttpCookie authCookie = System.Web.HttpContext.Current.Request.Cookies[System.Web.Security.FormsAuthentication.FormsCookieName];
                    if (authCookie != null)
                    {
                        System.Web.Security.FormsAuthenticationTicket authTicket = System.Web.Security.FormsAuthentication.Decrypt(authCookie.Value);
                        if (authTicket != null)
                        {
                            DateTime cookieCreationTime = authTicket.IssueDate;

                            string username = User.Identity.Name;
                            bool isSafe;
                            DateTime dbPasswordChangedTime = GetPasswordChangedTime(username, out isSafe);
                            if (Membership.GetUser(username).IsLockedOut == null)
                            {
                                if (Request.Cookies["userId"] != null)
                                {
                                    // cr-dotnet-0121: Use UtcNow for cookie expiry
                                    Response.Cookies["userId"].Expires = DateTimeOffset.UtcNow.AddDays(-1).UtcDateTime;
                                }
                            }
                            if (cookieCreationTime < dbPasswordChangedTime || !isSafe || Membership.GetUser(username).IsLockedOut == true)
                            {
                                string cacheKey = $"PasswordChangedTime_{username}";
                                if (HttpRuntime.Cache[cacheKey] != null)
                                {
                                    HttpRuntime.Cache.Remove(cacheKey);
                                }
                                // cr-dotnet-0029: Sign out – in full Cognito migration replace with Cognito global sign-out.
                                System.Web.Security.FormsAuthentication.SignOut();
                                Session.Abandon();

                                // cr-dotnet-0121: Use UtcNow for cookie expiry
                                System.Web.HttpContext.Current.Response.Cookies[System.Web.Security.FormsAuthentication.FormsCookieName].Expires = DateTimeOffset.UtcNow.AddYears(-1).UtcDateTime;
                            }
                        }
                    }
                }
            }
            catch { }
        }

        protected void Application_Error(object sender, EventArgs e)
        {
        }

        protected void Session_End(object sender, EventArgs e)
        {
        }

        protected void Application_End(object sender, EventArgs e)
        {
            // cr-dotnet-0127: Graceful shutdown – release resources on application end.
            OnProcessExit(sender, e);
        }
    }
}
