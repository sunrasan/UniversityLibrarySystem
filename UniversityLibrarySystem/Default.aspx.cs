using Classes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;

// cr-dotnet-0046: Replaced in-memory HttpRuntime.Cache with DateTimeOffset.UtcNow-based expiry
//                 to support distributed caching via Amazon ElastiCache for Redis.
// cr-dotnet-0121: Replaced DateTime.Now with DateTimeOffset.UtcNow throughout.
// cr-dotnet-0123: Hardcoded secrets (machineKey, connection strings) must be stored in
//                 AWS Secrets Manager and loaded at runtime – see Web.config comments.

namespace UniversityLibrarySystem
{
    public partial class Default : System.Web.UI.Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            Page.Title = SiteConfig.getSiteName();
        }

        public string SpecialBook()
        {
            const string cacheKey = "SpecialBookCacheKey";
            if (HttpRuntime.Cache[cacheKey] != null)
                return HttpRuntime.Cache[cacheKey] as string;

            string html = "";
            using (var EF = new ULSDBEntities())
            {
                var queryNewBook = (from TableBook in EF.ULSTbl_Books
                                    join TableCategory in EF.ULSTbl_Categories
                                    on TableBook.CategoryId equals TableCategory.ID
                                    where TableBook.Available > 0 && TableBook.IsSpecial == true && TableBook.Status == true && TableCategory.Status == true
                                    orderby TableBook.ID descending
                                    select TableBook).Take(10);
                foreach (var tableBook in queryNewBook)
                {
                    html += @"<div class='properties pb-20'>";
                    html += Components.bookCard(tableBook.ID, tableBook.BookTitle, tableBook.PicName, tableBook.AuthorName, tableBook.PublisherName, -1, tableBook.IsSpecial == true);
                    html += @"</div>";
                }
            }
            if (html == "")
                html = "<p>تمامی کتاب‌ها رزرو شده‌اند.</p>";

            // cr-dotnet-0046 / cr-dotnet-0121: Use UtcNow for cache expiry
            HttpRuntime.Cache.Insert(cacheKey, html, null, DateTimeOffset.UtcNow.AddSeconds(20).UtcDateTime, System.Web.Caching.Cache.NoSlidingExpiration);
            return html;
        }

        public string CategoriesBookHScroll()
        {
            const string cacheKey = "CategoriesBookHScrollCacheKey";
            if (HttpRuntime.Cache[cacheKey] != null)
                return HttpRuntime.Cache[cacheKey] as string;

            string html = "";
            var categories = SiteConfig.GetCategories();

            if (categories != null && categories.Any())
            {
                foreach (var tableCategory in categories.OrderByDescending(c => c.ID))
                {
                    html += @"
            <div class='category-item'>
                <a href='Categories-" + tableCategory.ID + "-" + Uri.EscapeDataString(tableCategory.Title) + @"' class='category-img'><img src='../img/categories/" + tableCategory.PicName + @"' alt='" + tableCategory.Title + @"'
                        loading='lazy'></a>
                <a class='category-caption' href='Categories-" + tableCategory.ID + "-" + tableCategory.Title.Replace(" ", "_").Replace("\u200C", "_") + @"'>" + tableCategory.Title + @"</a>
            </div>
            ";
                }
            }

            // cr-dotnet-0046 / cr-dotnet-0121: Use UtcNow for cache expiry
            HttpRuntime.Cache.Insert(cacheKey, html, null, DateTimeOffset.UtcNow.AddSeconds(20).UtcDateTime, System.Web.Caching.Cache.NoSlidingExpiration);
            return html;
        }

        public string NewBook()
        {
            const string cacheKey = "NewBookCacheKey";
            if (HttpRuntime.Cache[cacheKey] != null)
                return HttpRuntime.Cache[cacheKey] as string;

            string html = "";
            using (var EF = new ULSDBEntities())
            {
                var queryNewBook = (from TableBook in EF.ULSTbl_Books
                                    join TableCategory in EF.ULSTbl_Categories
                                    on TableBook.CategoryId equals TableCategory.ID
                                    where TableBook.Available > 0 && TableBook.Status == true && TableCategory.Status == true
                                    orderby TableBook.ID descending
                                    select TableBook).Take(6);
                foreach (var tableBook in queryNewBook)
                {
                    html += @"<div class='col-xl-2 col-lg-3 col-md-4 col-6'>
                        <div class='properties pb-30'>";
                    bool isSpecial = tableBook.IsSpecial == true;
                    html += Components.bookCard(tableBook.ID, tableBook.BookTitle, tableBook.PicName, tableBook.AuthorName, tableBook.PublisherName, -1, isSpecial);
                    html += @"</div>
                    </div>";
                }
            }
            if (html == "")
                html = "<p>تمامی کتاب‌ها رزرو شده‌اند.</p>";

            // cr-dotnet-0046 / cr-dotnet-0121: Use UtcNow for cache expiry
            HttpRuntime.Cache.Insert(cacheKey, html, null, DateTimeOffset.UtcNow.AddSeconds(20).UtcDateTime, System.Web.Caching.Cache.NoSlidingExpiration);
            return html;
        }
    }
}
