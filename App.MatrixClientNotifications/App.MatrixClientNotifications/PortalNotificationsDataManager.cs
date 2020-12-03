using System;
using System.Linq;
using BrightMls.Data.EntityFramework.BrightData;
using BrightMls.Data.EntityFramework.Mls;
using BrightMls.Enterprise.Shared.Helpers;

namespace BrightMls.Enterprise.MatrixClientNotifications
{
    public static class PortalNotificationsDataManager
    {
        public static DateTime? GetPortalNotificationJobLastRun(string portalNotificationJobCode)
        {
            DateTime? lastRun = null;
            
            try
            {
                using (var entities = new SecurityEntities())
                {
                    var portalNotificationStatus = (from m in entities.PortalNotificationStatus
                                                    where m.PortalNotificationJobCode == portalNotificationJobCode
                                                    select m).FirstOrDefault();
                    if (portalNotificationStatus != null)
                        lastRun = portalNotificationStatus.PortalNotificationJobLastRun;
                }
            }
            catch (Exception ex)
            {
                CustomElmahErrorLogger.LogError(System.Reflection.Assembly.GetExecutingAssembly().GetName().Name,
                    "Exception occurred while getting job last run time.",
                    "Exception message: " + ex.Message, ex.StackTrace, 
                    "MatrixClientNotifications");
                throw;
            }

            return lastRun;
        }

        public static void UpsertJobLastRun(string portalNotificationJobCode)
        {
            using (var entities = new SecurityEntities())
            {
                var portalNotificationStatusEntity = (from portalNotificationStatusQuery in entities.PortalNotificationStatus
                    where portalNotificationStatusQuery.PortalNotificationJobCode == portalNotificationJobCode
                    select portalNotificationStatusQuery).FirstOrDefault();

                if (portalNotificationStatusEntity != null)
                {
                    portalNotificationStatusEntity.PortalNotificationJobLastRun = DateTime.Now;
                    portalNotificationStatusEntity.DateTimeModified = DateTime.Now;
                }
                entities.SaveChanges();
            }
        }

        public static bool IsClientUnsubscribed(int contanctId)
        {
            using (var entities = new SecurityEntities())
            {
                return (from m in entities.PortalNotificationUnsubscribedClients
                                          where m.ContactId == contanctId
                                          select m).Any();
            }
        }

        public static long? GetBrightListingResourceKey(int matrixUniqueId)
        {
            using (var entities = new BrightDataEntities())
            {
                return (from l in entities.Listings
                    where l.Matrix_Unique_ID == matrixUniqueId
                    select l.ListingKey).FirstOrDefault();
            }
        }
    }
}
