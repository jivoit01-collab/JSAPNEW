using JSAPNEW.Models;
using Microsoft.AspNetCore.Mvc.RazorPages;
using ServiceStack;
using System.ComponentModel.Design;
using System.Net.NetworkInformation;

namespace JSAPNEW.Services.Interfaces
{
    public interface INotificationService
    {
        Task<IEnumerable<NotificationResponse>> DeleteOldNotificationsAsync(int days_old);
        Task<IEnumerable<NotificationResponse>> DeleteOldUserTokensAsync(int days_old);
        Task<IEnumerable<NotificationModels>> GetUnreadNotificationCountAsync(int userId);
        Task<IEnumerable<UserNotificationsModel>> GetUserNotificationsAsync(int userId);
        Task<string?> GetUserTokenAsync(int userId);
        Task<List<UserTokenModel>> GetUserFcmTokenAsync(int userId);
        //Task<IEnumerable<UserTokenModel>> GetUserTokensAsync(int userId);
        Task<IEnumerable<NotificationResponse>> InsertNotificationAsync(InsertNotificationModel request);
        Task<IEnumerable<NotificationResponse>> MarkAllNotificationsAsReadAsync(int userId);
        Task<IEnumerable<NotificationResponse>> MarkNotificationAsReadAsync(int notificationId);
        Task<IEnumerable<NotificationResponse>> SaveUserToken(int userId, string fcmToken, string deviceId);
        Task<IEnumerable<NotificationResponse>> CreatePageAsync(CreatePageRequest request);
        //Task<string> SendPushNotificationAsync(string title, string body, string fcmToken);
        //Task SendPushNotificationAsync(string title, string body, string fcmToken);
        Task SendPushNotificationAsync(string title, string body, string fcmToken, Dictionary<string, string>? data = null);
        Task<IEnumerable<NotificationResponse>> DeleteTokensAsync(int userId, string deviceId);
        Task<IEnumerable<NotificationResponse>> InsertDeviceInfoAsync(int userId, string deviceId, string appVersion);

    }
}
