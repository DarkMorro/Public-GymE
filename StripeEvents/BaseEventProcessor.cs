using GymEBL.Interfaces;
using GymEModels;
using GymEModels.Payment;
using Stripe;
using System.Threading.Tasks;

namespace GymEBL.StripeEvents
{
    public abstract class BaseEventProcessor: IEventProcessor
    {
        IOperationsCRUD<StripeEvent> eventsBL;
        protected ProcessInfo processInfo;

        public BaseEventProcessor(IOperationsCRUD<StripeEvent> eventsBL)
        {
            this.eventsBL = eventsBL;
            this.processInfo = new ProcessInfo("WebHookController-"+ this.GetType().Name);//We will add the name of the Derived class
        }
        public abstract string EventType { get; }

        public abstract Task<bool> ProcessEvent(Event stripeEvent);

        public virtual async Task<bool> RegisterEvent(Event stripeEvent, string description)
        {
            var newEvent = new StripeEvent { Id = stripeEvent.Id, Description = description };
            StripeEvent eventSaved =await eventsBL.Save(newEvent, processInfo);
            if (eventSaved != null)
                return true;
            return false;
        }
    }
}
