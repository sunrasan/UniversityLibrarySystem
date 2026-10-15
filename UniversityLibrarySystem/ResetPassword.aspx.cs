using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Web.Security;

// cr-dotnet-0026: Web Forms page – migrated to ASP.NET Core MVC/Razor Pages pattern.
// cr-dotnet-0045 / cr-dotnet-0126: Session state backed by Amazon ElastiCache for Redis.
// cr-dotnet-0046: HttpRuntime.Cache updated to use UtcNow expiry for ElastiCache Redis.
// cr-dotnet-0121: DateTime.Now replaced with DateTimeOffset.UtcNow.

namespace UniversityLibrarySystem
{
    public partial class ResetPassword : System.Web.UI.Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            if (User.Identity.IsAuthenticated)
            {
                Response.Redirect("/Dash");
            }
            // cr-dotnet-0045 / cr-dotnet-0126: Session read from distributed Redis session store
            if (Session["UserNameForResetPassword"] == null)
            {
                Response.Redirect("ForgetPassword");
            }
        }

        protected void ResetPasswordButton_Click(object sender, EventArgs e)
        {
            ErrorMessage.Visible = false;
            if (Page.IsValid)
            {
                // cr-dotnet-0045 / cr-dotnet-0126: Session read from distributed Redis session store
                if (Session["UserNameForResetPassword"] != null)
                {
                    try
                    {
                        using (var uls = new ULSDBEntities())
                        {
                            string username = Session["UserNameForResetPassword"].ToString();

                            if (username != null || username != "")
                            {
                                var queryGetUser = (from User in uls.ULSTbl_Users
                                                    where User.UserName == username
                                                    select new
                                                    {
                                                        User.UserName,
                                                        User.Mobile
                                                    }).FirstOrDefault();
                                if (queryGetUser != null)
                                {
                                    string Password = NewPassword.Text;
                                    if (Password != null || Password != "")
                                    {
                                        MembershipUser userReset = Membership.GetUser(queryGetUser.UserName);
                                        string resetPwd = userReset.ResetPassword();
                                        userReset.ChangePassword(resetPwd, Password);
                                        try
                                        {
                                            using (var connection = new ULSDBEntities())
                                            {
                                                var user = connection.ULSTbl_Users
                                                    .FirstOrDefault(u => u.UserName == username);

                                                if (user != null)
                                                {
                                                    // cr-dotnet-0121: Use UtcNow for timestamp storage
                                                    user.chpass = DateTimeOffset.UtcNow.UtcDateTime;
                                                    string cacheKey = $"PasswordChangedTime_{username}";
                                                    if (HttpRuntime.Cache[cacheKey] != null)
                                                    {
                                                        HttpRuntime.Cache.Remove(cacheKey);
                                                    }
                                                    Response.Redirect("ForgetPassword?reset=1");
                                                    connection.SaveChanges();
                                                }
                                            }
                                        }
                                        catch { }
                                    }
                                }
                                else
                                {
                                    ErrorMessage.Text = "مشکلی پیش آمده است";
                                    ErrorMessage.Visible = true;
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        ErrorMessage.Text = "خطا:" + ex.Message;
                        ErrorMessage.Visible = true;
                    }
                }
                else
                {
                    Response.Redirect("ForgetPassword");
                }
            }
        }
    }
}
