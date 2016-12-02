using FirstFloor.ModernUI.Windows.Controls;
using Microsoft.Lync.Model;
using Microsoft.Lync.Model.Conversation;
using System;
using System.Collections.Generic;
using System.Windows;

namespace IfsSvnClient.Classes
{
    internal class NotifierLync
    {
        private const string SUBJECT = "EazyCheckout";
        private LyncClient _Client;

        internal NotifierLync()
        {
            try
            {
                _Client = LyncClient.GetClient();
            }
            catch (Exception)
            {
                throw;
            }
        }

        internal string GetSelfContactDisplayName()
        {
            if (_Client.State == ClientState.SignedIn)
            {
                return _Client.Self.Contact.GetContactInformation(ContactInformationType.DisplayName).ToString();
            }
            return string.Empty;
        }

        internal void SendMessageToSupport()
        {
             MessageBoxResult contact = ModernDialog.ShowMessage(
                                                              "Support does not like to be contacted just for FUN!\r\nDo you really need to contact Me? :| ",
                                                              "Contact Support",
                                                              MessageBoxButton.YesNo);
             if (contact == MessageBoxResult.Yes)
             {
                 this.SendMessage(Properties.Settings.Default.SupportPerson, Properties.Resources.HeaderMessage);
             }
        }

        internal void SendMessage(string contactUri, string imText)
        {
            try
            {
                if (_Client.State == ClientState.SignedIn)
                {
                    ConversationManager myConversationManager = _Client.ConversationManager;
                    Conversation myConversation = myConversationManager.AddConversation();
                    Participant myParticipant = myConversation.AddParticipant(_Client.ContactManager.GetContactByUri(contactUri));

                    if (myParticipant.IsSelf == false)
                    {
                        if (myConversation.State == ConversationState.Active && myConversation.Properties.ContainsKey(ConversationProperty.Subject))
                        {
                            myConversation.Properties[ConversationProperty.Subject] = SUBJECT;
                        }

                        if (myConversation.Modalities.ContainsKey(ModalityTypes.InstantMessage))
                        {
                            InstantMessageModality imModality = (InstantMessageModality)myConversation.Modalities[ModalityTypes.InstantMessage];
                            if (imModality.CanInvoke(ModalityAction.SendInstantMessage))
                            {
                                Dictionary<InstantMessageContentType, string> textMessage = new Dictionary<InstantMessageContentType, string>();
                                textMessage.Add(InstantMessageContentType.PlainText, SUBJECT + " : " + imText);

                                imModality.BeginSendMessage(textMessage, SendMessageCallback, imModality);
                            }
                        }
                    }
                }
            }
            catch
            {
            }
        }

        private void SendMessageCallback(IAsyncResult ar)
        {
            try
            {
                InstantMessageModality imModality = (InstantMessageModality)ar.AsyncState;

                imModality.EndSendMessage(ar);
            }
            catch
            {
            }
        }
    }
}