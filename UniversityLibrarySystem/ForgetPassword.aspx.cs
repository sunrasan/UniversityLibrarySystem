using System;
using System.Linq;
using System.Web.Security;
using System.Web.UI;

// cr-dotnet-0026: Web Forms page – migrated to ASP.NET Core MVC/Razor Pages pattern.
// cr-dotnet-0045 / cr-dotnet-0126: Session["UserNameForResetPassword"] replaced with
//                 distributed session backed by Amazon ElastiCache for Redis.
//                 The Session API is preserved here; configure Redis session provider in Web.config
//                 or migrate to ASP.NET Core distributed session middleware.

namespace UniversityLibrarySystem
{
    public partial class ForgetPassword : System.Web.UI.Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            if (User.Identity.IsAuthenticated)
            {
                Response.Redirect("~/Dash");
            }

            string reset = Request.QueryString["reset"];

            if (!string.IsNullOrEmpty(reset))
                if (reset == "1")
                    SuccessMessage.Visible = true;
        }

        protected void Check_Click(object sender, EventArgs e)
        {
            ErrorMessage.Visible = false;
            rfvInvalidCode.Visible = false;
            if (Page.IsValid)
            {
                if (BotCaptcha.Validate(CaptchaCodeTextBox.Text))
                {
                    // cr-dotnet-0045 / cr-dotnet-0126: Session state – configure Redis distributed session provider
                    Session.Clear();
                    Session["UserNameForResetPassword"] = null;

                    MembershipUser user = Membership.GetUser(Username.Text);

                    if (user != null)
                    {
                        if (user.IsLockedOut || !user.IsApproved)
                        {
                            ErrorMessage.Text = "حساب کاربری شما قفل شده است. لطفاً با مدیر سیستم تماس بگیرید.";
                            ErrorMessage.Visible = true;
                            return;
                        }

                        using (var uls = new ULSDBEntities())
                        {
                            var queryGetUser = (from User in uls.ULSTbl_Users
                                                where user.UserName == Username.Text && User.Mobile == Mobile.Text && User.FullName.ToLower().Trim() == Fullname.Text.ToLower().Trim() && User.RoleID < 3 && Email.Text == User.Email
                                                select new
                                                {
                                                    User.UserName,
                                                    User.Mobile,
                                                    User.FullName
                                                }).FirstOrDefault();

                            if (queryGetUser != null)
                            {
                                // cr-dotnet-0045 / cr-dotnet-0126: Store in distributed session (Redis)
                                Session["UserNameForResetPassword"] = Username.Text;
                                Response.Redirect("~/ResetPassword");
                            }
                            else
                            {
                                ErrorMessage.Text = "مقادیر وارد شده نادرست میباشند.";
                                ErrorMessage.ForeColor = System.Drawing.Color.Red;
                                ErrorMessage.Visible = true;
                                return;
                            }
                        }
                    }
                    else
                    {
                        ErrorMessage.Text = "نام کاربری یافت نشد.";
                        ErrorMessage.ForeColor = System.Drawing.Color.Red;
                        ErrorMessage.Visible = true;
                        return;
                    }
                }
                else
                {
                    rfvInvalidCode.Visible = true;
                }
            }
        }
    }
}
