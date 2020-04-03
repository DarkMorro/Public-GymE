using Stripe;
using System.Threading.Tasks;

namespace GymEBL.StripeEvents
{
    public interface IEventProcessor
    {
        string EventType { get; }
        Task<bool> ProcessEvent(Event stripeEvent);
    }
}
