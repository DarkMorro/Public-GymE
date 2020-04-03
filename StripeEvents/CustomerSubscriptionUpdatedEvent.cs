using GymEBL.Interfaces;
using GymEModels.Exceptions;
using GymEModels.Payment;
using GymEModels.Resources;
using Microsoft.AspNetCore.Identity.UI.Services;
using Stripe;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace GymEBL.StripeEvents
{
    public class CustomerSubscriptionUpdatedEvent : BaseEventProcessor
    {
        public override string EventType => Events.CustomerSubscriptionUpdated;

        IEmailSender emailSender;
        IOperationsCRUD<StripeEvent> eventsBL;
        IPaymentBL paymentBL;
        protected CustomerService customerService;
        protected ProductService productService;
        public CustomerSubscriptionUpdatedEvent(IEmailSender emailSender, IOperationsCRUD<StripeEvent> eventsBL, IPaymentBL paymentBL) :base(eventsBL)
        {
            this.emailSender = emailSender;
            this.eventsBL = eventsBL;
            this.paymentBL = paymentBL;
            customerService = new CustomerService();
            productService = new ProductService();
        }
        
        public override async Task<bool> ProcessEvent(Event stripeEvent)
        {
            Subscription subscription = stripeEvent.Data.Object as Subscription;
            var customer = await customerService.GetAsync(subscription.CustomerId);
            var product = await productService.GetAsync(subscription.Plan.ProductId);
            if (subscription.Status.Equals(SubscriptionStatuses.PastDue))
            {
                string htmlMessage = string.Format(
                    EmailResources.ProblemsChargingSubscription, 
                    customer.Name, 
                    product.Name
                    );

                await emailSender.SendEmailAsync(
                    email: customer.Email, 
                    subject: EmailResources.ProblemsChargingSubscription_Subject, 
                    htmlMessage: htmlMessage
                    );

                return await RegisterEvent(stripeEvent, 
                    $"Problems charging subscription {subscription.Id} to customer{subscription.CustomerId}: Notification sent to{customer.Email}"
                    );
            }

            else if (subscription.Status.Equals(SubscriptionStatuses.Canceled) || subscription.Status.Equals(SubscriptionStatuses.Unpaid))
            {
                var metadata = subscription.Plan?.Metadata;

                if (!metadata.TryGetValue("PackageId", out string packageId))
                    throw new CustomException("PackageId not found in subscription metadata in stripe", StatusCode.NotFound);

                if (!subscription.Metadata.TryGetValue("GymId", out string gymId))
                    throw new CustomException("GymId not in subsctiption Metadata", StatusCode.NotFound);

                if(!customer.Metadata.TryGetValue("UserId", out string userId))
                    throw new CustomException("UserId not in customer Metadata", StatusCode.NotFound);

                await paymentBL.CancelSubscription(packageId, gymId, userId, processInfo);

                //Notify User
                string htmlMessage = string.Format(EmailResources.CancelingSubscriptionDueUnsuccessfulAutomaticCharge, customer.Name, product.Name);
                await emailSender.SendEmailAsync(email: customer.Email, subject: EmailResources.CancelingSubscriptionDueUnsuccessfulAutomaticCharge_Subject, htmlMessage: htmlMessage);

                //Register Event
                return await RegisterEvent(stripeEvent, $"Problems charging subscription {subscription.Id} to customer{subscription.CustomerId}: Notification sent to{customer.Email}");
            }
            return true;
        }
    }
}
