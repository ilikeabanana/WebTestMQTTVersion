using System.Collections.Generic;
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
