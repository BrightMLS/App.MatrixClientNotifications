using System;
using System.Collections.Generic;
using System.Configuration;
using System.Globalization;
using System.Linq;
using BrightMls.Enterprise.MatrixClientNotifications.Models;
using BrightMls.Enterprise.MatrixWebApi.Managers;
using BrightMls.Enterprise.Mds;
using BrightMls.Enterprise.Mds.Models.Agent;
using BrightMls.Enterprise.Shared.Helpers;
using Newtonsoft.Json;
using Quiksoft.EasyMail.SMTP;
using EmailMessage = BrightMls.Enterprise.Email.EmailMessage;
using ListingNote = BrightMls.Enterprise.MatrixClientNotifications.Models.ListingNote;

namespace BrightMls.Enterprise.MatrixClientNotifications
{
    public class Program
    {
        private static string MdsSystemUser => ConfigurationManager.AppSettings["MdsSystemUser"];
        private static string JobCode => ConfigurationManager.AppSettings["JobCode"];
        static void Main(string[] args)
        {
            var ei = GetClientNoteEmails(JobCode);

            if (ei == null || ei.Count == 0) return;

            PrepareAndSendEmails(ei);
        }

        private static void PrepareAndSendEmails(IList<EmailInformation> ei)
        {
            try
            {
                var timeDiffInHours = int.Parse(ConfigurationManager.AppSettings["timeDiffInHours"]);

                // read email, listing and note HTML templates
                var currentDir = System.IO.Directory.GetCurrentDirectory();
                var emailTemplate = System.IO.File.ReadAllText(currentDir + "\\HtmlTemplates\\Email.html");
                var listingTemplate = System.IO.File.ReadAllText(currentDir + "\\HtmlTemplates\\Listing.html");
                var noteTemplate = System.IO.File.ReadAllText(currentDir + "\\HtmlTemplates\\Note.html");
                var unsubscribeUrl = ConfigurationManager.AppSettings["unsubscribeUrl"];

                foreach (var emailInformation in ei)
                {
                    var agentPreferredFullName = emailInformation.Agent.PreferredFirstName + " " +
                                                 emailInformation.Agent.PreferredLastName;
                    
                    // replace the following to make the unsubscribeUrl specific for the conctact
                    /*
                    [[CONTACT_ID]]
                    [[AGENT_MEMBER_MLS_ID]]
                    [[CONTACT_EMAIL]]
                    */
                    unsubscribeUrl = unsubscribeUrl
                        .Replace("[[CONTACT_ID]]", emailInformation.ContactId.ToString())
                        .Replace("[[AGENT_MEMBER_MLS_ID]]", emailInformation.AgentMemberMlsId)
                        .Replace("[[CONTACT_EMAIL]]", emailInformation.ContactEmail);

                    // replace tags in HTML templates with specific data
                    /*
                    [[UNSUBSCRIBE_URL]] =  http://web.trendmls.com/Forms/Client/Email/PortalNotificationsClientUnsubscribe.aspx?UnsubscribeEmail=pszajowski%40trendmls.com&amp;ContactID=27530178&amp;MemberID=90032766
                    [[AGENT_MEMBER_MLS_ID]]
                    [[AGENT_NAME]] 
                    [[CLIENT_FIRST_NAME]]  
                    [[AGENT_EMAIL]] = Piotr.Szajowski@trendmls.com
                    [[AGENT_PHONE_NUMBER]] = (610) 783-4650
                    */
                    var formattedPhoneNumber = "";
                    long phoneNumber;
                    if (long.TryParse(emailInformation.Agent.PreferredPhoneNumber, out phoneNumber))
                    {
                        formattedPhoneNumber = $"{phoneNumber:(###) ###-####}";
                    }

                    var email = emailTemplate
                        .Replace("[[UNSUBSCRIBE_URL]]", unsubscribeUrl)
                        .Replace("[[AGENT_MEMBER_MLS_ID]]", emailInformation.AgentMemberMlsId)
                        .Replace("[[AGENT_NAME]]", agentPreferredFullName)
                        .Replace("[[CLIENT_FIRST_NAME]]", emailInformation.ContactFirstName)
                        .Replace("[[AGENT_EMAIL]]", emailInformation.Agent.Email)
                        .Replace("[[AGENT_PHONE_NUMBER]]", formattedPhoneNumber);

                    var listings = "";
                    foreach (var mlsInfo in emailInformation.MlsInformation)
                    {
                        /*
                        [[MLS_NUMBER]] 
                        [[LISTING_ADDRESS]] =  502 Crest Dr Lake Harmony PA 18624
                        */
                        var listing = listingTemplate
                            .Replace("[[MLS_NUMBER]]", mlsInfo.MlsNumber.ToString())
                            .Replace("[[LISTING_ADDRESS]]", mlsInfo.Address);

                        var notes = "";
                        foreach (var listingNote in mlsInfo.Notes)
                        {
                            /*
                            [[NOTE_DATE]] = 09/07/2017 10:52:13
                            [[NOTE_TEXT]]
                            */
                            var note = noteTemplate
                                .Replace("[[NOTE_DATE]]", listingNote.NoteDateTime.AddHours(timeDiffInHours).ToString(CultureInfo.InvariantCulture))
                                .Replace("[[NOTE_TEXT]]", listingNote.NoteText);
                            notes += note;
                        }
                        listing = listing.Replace("[[NOTES_TEMPLATE]]", notes);
                        listings += listing;
                    }

                    email = email.Replace("[[LISTINGS_TEMPLATE]]", listings);

                    // send email to client
                    var subject = "New Listing Notes from " + agentPreferredFullName;
                    SendEmail(email, subject, emailInformation.ContactEmail, emailInformation.Agent.Email, agentPreferredFullName);
                }
            }
            catch (Exception ex)
            {
                var serializedException = JsonConvert.SerializeObject(ex);
                CustomElmahErrorLogger.LogError(System.Reflection.Assembly.GetExecutingAssembly().GetName().Name,
                    "Exception occurred in PrepareAndSendEmails",
                    "Exception message: " + ex.Message,
                    "serializedException: " + serializedException,
                    "MatrixClientNotifications");
            }
        }

        private static void SendEmail(string emailBody, string emailSubject, string emailTo, string emailFrom, string fromName)
        {
            var em = new EmailMessage(emailFrom);
            var toAddress = new Address
            {
                Email = ConfigurationManager.AppSettings["PortalNotificationTestMode"] == "true"
                    ? ConfigurationManager.AppSettings["PortalNotificationTestModeRecipients"]
                    : emailTo
            };
            
            if (toAddress.Email == null || toAddress.Email.Trim() == "") return;
            em.Recipients.Add(toAddress);

            em.From = new Address(emailFrom, fromName);

            em.Subject = emailSubject;

            em.BodyPartFormat = BodyPartFormat.HTML;
            em.Body = emailBody;

            em.Send();
        }

        public static IList<EmailInformation> GetClientNoteEmails(string portalStatusJobCode)
        {
            var emailInformationList = new List<EmailInformation>();
            try
            {
                var sinceLastRun = PortalNotificationsDataManager.GetPortalNotificationJobLastRun(portalStatusJobCode).GetValueOrDefault();
                
                if (ConfigurationManager.AppSettings["PortalNotificationOnlySubscriberID"].Trim() == "") 
                    PortalNotificationsDataManager.UpsertJobLastRun(portalStatusJobCode);
                else
                    sinceLastRun = Convert.ToDateTime(ConfigurationManager.AppSettings["PortalNotificationOnlySubscriberIDDate"]);
                
                var portalListings = MatrixClientPortalManager.GetClientPortalNotes(sinceLastRun.ToUniversalTime(), "LastAgentNoteTimestamp");
                
                // lists and dictionaries to store potentially repeated values so we can only query poor crappy MDS once
                foreach (var portalListing in portalListings)
                {
                    
                    if (PortalNotificationsDataManager.IsClientUnsubscribed(portalListing.ContactKeyNumeric)) continue;
                    
                    var contactEmail = emailInformationList.Find(cei => cei.ContactId == portalListing.ContactKeyNumeric);
                    if (contactEmail == null)
                    {
                        // new contact.  need to find it in matrix.
                        var contact = MatrixContactManager.GetContact(portalListing.ContactKeyNumeric);
                        if (contact != null)
                        {
                            var newContactEmail = new EmailInformation
                            {
                                ContactId = portalListing.ContactKeyNumeric,
                                ContactEmail = contact.Email,
                                ContactFirstName = contact.FirstName,
                                AgentMemberMlsId = contact.OwnerMemberId,
                                MlsInformation = new List<MlsInformation> {
                                    new MlsInformation
                                    {
                                        ListingKeyNumeric = portalListing.ListingKeyNumeric,
                                        Notes = GetAgentNotes(sinceLastRun, portalListing)
                                    }
                                }
                            };
                            emailInformationList.Add(newContactEmail);
                        }
                        else
                        {
                            var serializedPortalListing = JsonConvert.SerializeObject(portalListing);
                            CustomElmahErrorLogger.LogError(System.Reflection.Assembly.GetExecutingAssembly().GetName().Name,
                                "Unable to find contact via Matrix API",
                                "ContactKeyNumeric: (" + portalListing.ContactKeyNumeric + ")",
                                "serializedPortalListing: " + serializedPortalListing,
                                "MatrixClientNotifications");
                        }
                    }
                    else
                    {
                        contactEmail.MlsInformation.Add(new MlsInformation
                        {
                            ListingKeyNumeric = portalListing.ListingKeyNumeric,
                            Notes = GetAgentNotes(sinceLastRun, portalListing)
                        });
                    }
                }

                if (emailInformationList.Count == 0) return emailInformationList;

                emailInformationList = FillInAgentDetails(emailInformationList);
                
                emailInformationList = FillInListingDetails(emailInformationList);
            }
            catch (Exception ex)
            {
                var serializedException = JsonConvert.SerializeObject(ex);
                CustomElmahErrorLogger.LogError(System.Reflection.Assembly.GetExecutingAssembly().GetName().Name,
                    "Exception occurred in GetClientNoteEmails",
                    "Exception message: " + ex.Message,
                    "serializedException: " + serializedException,
                    "MatrixClientNotifications");
            }
            return emailInformationList;
        }

        private static List<EmailInformation> FillInListingDetails(List<EmailInformation> emailInformationList)
        {
            // get unique ListingKeyNumeric values for all 
            var uniqueMatrixListingKeys = new Dictionary<long, int>();
            foreach (var emailInfo in emailInformationList)
            {
                foreach (var mlsInfo in emailInfo.MlsInformation)
                {
                    if (uniqueMatrixListingKeys.ContainsValue(mlsInfo.ListingKeyNumeric)) continue;

                    // query repliacted matrix db to get bright listingId so we can query MDS for listing details
                    var brightListingId = PortalNotificationsDataManager.GetBrightListingResourceKey(mlsInfo.ListingKeyNumeric);
                    if(brightListingId != null && brightListingId > 0) uniqueMatrixListingKeys.Add(brightListingId.Value, mlsInfo.ListingKeyNumeric);
                }
            }

            // query MDS for listing data
            using (var mdsClient = new MdsClient(MdsSystemUser))
            {
                var propertyManager = new Mds.Managers.Property(mdsClient);
                var propertiesResponse = propertyManager.GetProperties(uniqueMatrixListingKeys.Select(brightListings => brightListings.Key).ToArray());
                if (propertiesResponse.ResponseStatus.StatusCode == MdsEnums.StatusCode.Success && propertiesResponse.BrightAll?.AllProperty != null)
                {
                    foreach (var property in propertiesResponse.BrightAll.AllProperty)
                    {
                        var uniqueMatrixId = uniqueMatrixListingKeys[property.ResourceKey];

                        // find all listing notes for this listing and update the address and mls number
                        foreach (var emailInfo in emailInformationList)
                        {
                            foreach (var mlsInfo in emailInfo.MlsInformation)
                            {
                                if (mlsInfo.ListingKeyNumeric != uniqueMatrixId) continue;

                                mlsInfo.Address = CommonUtils.FormatBrightAddress(property.LocationAddress);
                                mlsInfo.MlsNumber = int.Parse(property.Listing.ListingId);
                            }
                        }
                    }
                }
                else
                {
                    var serializedMdsResponse = JsonConvert.SerializeObject(propertiesResponse);
                    CustomElmahErrorLogger.LogError(System.Reflection.Assembly.GetExecutingAssembly().GetName().Name,
                        "Exception occurred in FillInListingDetails",
                        "Exception message: " + propertiesResponse.ResponseStatus.StatusMessage,
                        "serializedMdsResponse: " + serializedMdsResponse,
                        "MatrixClientNotifications");
                }
            }

            return emailInformationList;
        }

        private static List<EmailInformation> FillInAgentDetails(List<EmailInformation> emailInformationList)
        {
            using (var mdsClient = new MdsClient(MdsSystemUser))
            {
                // get resource keys for all agents that are owners of contacts
                var query = emailInformationList.Aggregate("(MemberMlsId IN ",
                    (current, emailInfo) => current + ("'" + emailInfo.AgentMemberMlsId + "',"));
                query = query.Substring(0, query.Length - 1) + ")";

                var message = mdsClient.Query(query, "Agent");
                var mdsResponse =
                    (BrightAgentsMdsResponse) JsonConvert.DeserializeObject(message, typeof(BrightAgentsMdsResponse));
                if (mdsResponse.ResponseStatus.StatusCode != MdsEnums.StatusCode.Success ||
                    mdsResponse.BrightAgents == null ||
                    mdsResponse.BrightAgents.Agent.Count != 1) return null;

                // limit the fields to only the ones we need.  we want to be nice to the poor performing MDS.  that poor puppy of crappy database
                mdsClient.SearchOptions.SelectFields = new[] { "MemberPreferredFirstName", "MemberPreferredLastName", "MemberPreferredPhone", "MemberEmail", "MemberMlsId" };
                var agentManager = new Mds.Managers.Agent(mdsClient);
                mdsResponse = agentManager.GetAgents(new[] {mdsResponse.BrightAgents.Agent[0].ResourceKey});
                if (mdsResponse.ResponseStatus.StatusCode != MdsEnums.StatusCode.Success ||
                    mdsResponse.BrightAgents == null || mdsResponse.BrightAgents.Agent.Count < 1)
                    return null;

                foreach (var agent in mdsResponse.BrightAgents.Agent)
                {
                    // find all emailInformationList entries for this agent
                    var agentInfos = emailInformationList.FindAll(a => a.AgentMemberMlsId == agent.BrightAgent.MemberMlsId);
                    foreach (var agentInfo in agentInfos)
                    {
                        agentInfo.Agent = new Models.Agent
                        {
                            Email = agent.BrightAgent.MemberEmail,
                            PreferredFirstName = agent.BrightAgent.MemberPreferredFirstName,
                            PreferredLastName = agent.BrightAgent.MemberPreferredLastName,
                            PreferredPhoneNumber = agent.BrightAgent.MemberPreferredPhone,
                        };
                    }
                }
            }
            return emailInformationList;
        }

        private static IList<ListingNote> GetAgentNotes(DateTime sinceLastRun, MatrixWebApi.Models.PortalListing portalListing)
        {
            IList<ListingNote> notesList = new List<ListingNote>();
            // ReSharper disable once LoopCanBeConvertedToQuery - shut up resharper. resulting LINQ isn't very readable
            foreach (var ln in portalListing.ListingNotes.Where(i => i.NotedBy == "Agent" && i.DateNoted > sinceLastRun))
            {
                var notes = new ListingNote { NoteDateTime = ln.DateNoted, NoteText = ln.NoteText };
                notesList.Add(notes);
            }

            return notesList;
        }
    }
}
