namespace Observability.Core
{
    public interface IMessageProducer
    {
        void SendMessage(AuditDataEvent @event);
    }
}
