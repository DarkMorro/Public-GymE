using GymEModels.Exceptions;
using System.Collections.Generic;
using System.Linq;

namespace GymEBL.StripeEvents
{
    public class EventFactory:IEventFactory
    {
        IEnumerable<IEventProcessor> eventProcessors;
        public EventFactory(IEnumerable<IEventProcessor> eventProcessors)
        {
            this.eventProcessors = eventProcessors;
        }

        public IEventProcessor GetEventProcessor(string stripeEventType)
        {
            var processor = this.eventProcessors.FirstOrDefault(proc => proc.EventType.Equals(stripeEventType));
            if(processor == null)
            {
                throw new CustomException("We dont have implementation for this event type", StatusCode.InternalServerError);
            }
            return processor;
        }
    }
}
