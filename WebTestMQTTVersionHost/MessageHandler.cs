using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace WebTestMQTTVersionHost
{
    internal class MessageHandler
    {
        public void HandleMessage(MessageDataJson messageData)
        {
            if(messageData.Type == "text")
            {
                HandleTextData(messageData);
            }
            if(messageData.Type == "file")
            {

            }
        }
        public void HandleFileData(MessageDataJson messageData)
        { 
            byte[] bytes = FileHandler.Base64ToFile(messageData.Value);
            FileHandler.ProcessFile(bytes, messageData.FileName);
        }
        
        public void HandleTextData(MessageDataJson messageData)
        {
            string command = messageData.Message;
            switch (command)
            {
                case "Dupe Enemies":
                    WebTestMQTTVersionHostPlugin.Log.LogInfo("Duplicating enemies");
                    break;
            }
        }

        
    }
}
