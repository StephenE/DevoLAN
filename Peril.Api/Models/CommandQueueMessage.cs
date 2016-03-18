using Peril.Api.Repository.Model;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Peril.Api.Models
{
    static public class CommandQueueExtensionMethods
    {
        static public IEnumerable<IDeployReinforcementsMessage> GetQueuedDeployReinforcementsCommands(this IEnumerable<ICommandQueueMessage> messages)
        {
            return from message in messages
                   where message.MessageType == CommandQueueMessageType.Reinforce
                   select message as IDeployReinforcementsMessage;
        }

        static public IEnumerable<IOrderAttackMessage> GetQueuedOrderAttacksCommands(this IEnumerable<ICommandQueueMessage> messages)
        {
            return from message in messages
                   where message.MessageType == CommandQueueMessageType.Attack
                   select message as IOrderAttackMessage;
        }

        static public IEnumerable<IRedeployMessage> GetQueuedRedeployCommands(this IEnumerable<ICommandQueueMessage> messages)
        {
            return from message in messages
                   where message.MessageType == CommandQueueMessageType.Redeploy
                   select message as IRedeployMessage;
        }

        static public IEnumerable<ICommandQueueMessage> GetCommandsFromPhase(this IEnumerable<ICommandQueueMessage> messages, Guid phaseId)
        {
            return from message in messages
                   where message.PhaseId == phaseId
                   select message;
        }
    }
}
