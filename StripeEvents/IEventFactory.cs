namespace GymEBL.StripeEvents
{
    public interface IEventFactory
    {
        IEventProcessor GetEventProcessor(string stripeEventType);
    }
}
