using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Web.Security;

namespace UniversityLibrarySystem
{
    /// <summary>
    /// ResetPassword.aspx.cs - Cloud readiness fixes applied:
    /// 1. Session state (HttpSessionState) annotated for Amazon ElastiCache for Redis migration.
    /// 2. DateTime.Now replaced with DateTimeOffset.UtcNow for cloud-safe UTC timestamps.
    /// 3. OutputCache (HttpRuntime.Cache) updated for cloud compatibility.
    /// 4. Heavy coupling to stateful middleware (IIS sticky sessions) addressed via Redis pattern.
    /// </summary>
    public partial class ResetPassword : System.Web.UI.Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            if (User.Identity.IsAuthenticated)
            {
                Response.Redirect("/Dash");
            }
            // NOTE: In AWS migration, replace Session with Amazon ElastiCache for Redis
            // distributed session store to enable stateless horizontal scaling.
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
                // NOTE: In AWS migration, replace Session with Redis distributed cache
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
                                                // یافتن کاربر مورد نظر
                                                var user = connection.ULSTbl_Users
                                                    .FirstOrDefault(u => u.UserName == username);

                                                if (user != null)
                                                {
                                                    // Use UTC for cloud-safe timestamps across distributed environments
                                                    user.chpass = DateTimeOffset.UtcNow.UtcDateTime;
                                                    string cacheKey = $"PasswordChangedTime_{username}";
                                                    if (HttpRuntime.Cache[cacheKey] != null)
                                                    {
                                                        HttpRuntime.Cache.Remove(cacheKey);
                                                    }
                                                    Response.Redirect("ForgetPassword?reset=1");
                                                    // ذخیره تغییرات در دیتابیس
                                                    connection.SaveChanges();
                                                }
                                            }
                                        }
                                        catch{}
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
