namespace Observability.Core
{
    public interface IMessageConsumer
    {
        void RegisterOnMessageHandlerAndReceiveMessages();
        void CloseSubscriptionClientAsync();
    }
}
