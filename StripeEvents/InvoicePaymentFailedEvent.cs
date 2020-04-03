using GymEBL.Interfaces;
using GymEModels;
using GymEModels.Payment;
using Stripe;
using System.Threading.Tasks;

namespace GymEBL.StripeEvents
{
    public class InvoicePaymentFailedEvent:BaseEventProcessor
    {
        public override string EventType => Events.InvoicePaymentFailed;

        IOperationsCRUD<StripeEvent> eventsBL;
        IOperationsCRUD<Owner> ownerBL;
        protected CustomerService customerService;

        public InvoicePaymentFailedEvent(IOperationsCRUD<StripeEvent> eventsBL, IOperationsCRUD<Owner> ownerBL) : base(eventsBL)
        {
            this.ownerBL = ownerBL;
            customerService = new CustomerService();
        }

        public override async Task<bool> ProcessEvent(Event stripeEvent)
        {
            Invoice invoice = stripeEvent.Data.Object as Invoice;
            var customer = await customerService.GetAsync(invoice.CustomerId);
            var owners = await ownerBL.GetByNotNullFields(new Owner { Email = customer.Email });

            //ToDo: Send emails for invoice payment fail event
            
            return await Task.FromResult<bool>(true);
        }
    }
}
