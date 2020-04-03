using GymEBL.Interfaces;
using GymEModels.Payment;
using Stripe;
using System.Threading.Tasks;

namespace GymEBL.StripeEvents
{
    public class ChargeRefundedEvent : BaseEventProcessor
    {
        IOperationsCRUD<StripeEvent> eventsBL;

        public ChargeRefundedEvent(IOperationsCRUD<StripeEvent> eventsBL) : base(eventsBL) { }
        public override string EventType => Events.ChargeRefunded;

        public override async Task<bool> ProcessEvent(Event stripeEvent)
        {
            Charge charge = stripeEvent.Data.Object as Charge;
            
            //ToDo: Send Email about Refund taking email from charge

            return await Task.FromResult<bool>(true);
        }
    }
}
