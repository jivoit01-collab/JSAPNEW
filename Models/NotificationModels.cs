using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.VisualBasic;
using System.ComponentModel.DataAnnotations;
using System.Formats.Tar;
using System.Security.AccessControl;
using System.Xml.Linq;

namespace JSAPNEW.Models
{
    public class NotificationModels
    {
        public int unread_count { get; set; }
    }

    public class NotificationResponse
    {
        public string Message { get; set; }
        public bool Success { get; set; }
    }




    public class UserNotificationsModel
    {
        public int id { get; set; }
        public string title { get; set; }
        public string message { get; set; }
        public string data { get; set; }
        public string status { get; set; }
        public string sentAt { get; set; }
        public string pageName { get; set; }
        public string pageUrl { get; set; }
    }

    public class UserTokenModel
    {
        public string fcmToken { get; set; }
    }

    public class InsertNotificationModel
    {
        public int userId { get; set; }
        public string title { get; set; }
        public string message { get; set; }
        public int pageId { get; set; }
        public string data { get; set; }
        public int BudgetId { get; set; }
    }

    public class CreatePageRequest
    {
        public string pageName { get; set; }
        public string pageUrl { get; set; }
        public int userId { get; set; }
    }



}