using GymEBL.Interfaces;
using GymEModels;
using GymEModels.Payment;
using Stripe;
using System.Threading.Tasks;

namespace GymEBL.StripeEvents
{
    public class InvoicePaymentSucceededEvent : BaseEventProcessor
    {
        public override string EventType => Events.InvoicePaymentSucceeded;

        IOperationsCRUD<StripeEvent> eventsBL;
        IOperationsCRUD<Owner> ownerBL;
        protected CustomerService customerService;

        public InvoicePaymentSucceededEvent(IOperationsCRUD<StripeEvent> eventsBL, IOperationsCRUD<Owner> ownerBL) : base(eventsBL) {
            this.ownerBL = ownerBL;
            customerService = new CustomerService();
        }
        
        public override async Task<bool> ProcessEvent(Event stripeEvent)
        {
            Invoice invoice = stripeEvent.Data.Object as Invoice;
            var customer = await customerService.GetAsync(invoice.CustomerId);
            var owners = await ownerBL.GetByNotNullFields(new Owner { Email = customer.Email });
            //ToDo: Save if we need to know until when is enable the subscription
            //ToDo: Review if subscription is enable to the gym, if not, enable it---optional
            //ToDo: Send Subscription Receipt email taking amount from invoice.AmountDue

            return await Task.FromResult<bool>(true);
        }
    }
}
