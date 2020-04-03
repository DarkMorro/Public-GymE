using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GymEBL.Interfaces;
using GymEModels;
using GymEModels.Exceptions;
using GymEModels.Payment;
using GymEModels.Resources;
using Microsoft.AspNetCore.Identity.UI.Services;
using Stripe;

namespace GymEBL.StripeEvents
{
    public class CustomerSubscriptionDeletedEvent : BaseEventProcessor
    {
        public override string EventType => Events.CustomerSubscriptionDeleted;

        IEmailSender emailSender;
        IOperationsCRUD<Owner> ownerBL;
        IOperationsCRUD<Gym> gymBL;
        IPaymentBL paymentBL;
        protected CustomerService customerService;
        protected ProductService productService;
        public CustomerSubscriptionDeletedEvent(IEmailSender emailSender, IOperationsCRUD<Owner> ownerBL, IOperationsCRUD<Gym> gymBL, IPaymentBL paymentBL, IOperationsCRUD<StripeEvent> eventsBL) :base(eventsBL)
        {
            this.emailSender = emailSender;
            this.ownerBL = ownerBL;
            this.gymBL = gymBL;
            this.paymentBL = paymentBL;
            customerService = new CustomerService();
            productService = new ProductService();
        }
        
        public override async Task<bool> ProcessEvent(Event stripeEvent)
        {
            Subscription subscription = stripeEvent.Data.Object as Subscription;
            if (subscription.Status.Equals(SubscriptionStatuses.Canceled))
            {
                //Check if Subscription Canceled in mongodb
                var gym = await GetGymFromStripeSubscription(subscription);
                
                //If Subscription not canceled then cancel
                if (IsGymSubscribed(gym, subscription))
                {
                    var metadata = subscription.Plan?.Metadata;
                    if (!metadata.TryGetValue("PackageId", out string packageId))
                        throw new CustomException("PackageId not found in subscription metadata in stripe", StatusCode.NotFound);

                    var customer = await customerService.GetAsync(subscription.CustomerId);
                    if (!customer.Metadata.TryGetValue("UserId", out string userId))
                        throw new CustomException("UserId not in customer Metadata", StatusCode.NotFound);

                    var product = await productService.GetAsync(subscription.Plan.ProductId);

                    if (await paymentBL.CancelSubscription(packageId, gym.Id, userId, processInfo))
                    {
                        //send email to user of service canceled
                        string htmlMessage = string.Format(
                            EmailResources.CancelingSubscription, 
                            customer.Name, 
                            product.Name
                            );
                        await emailSender.SendEmailAsync(
                            email: customer.Email, 
                            subject: EmailResources.CancelingSubscriptionDueUnsuccessfulAutomaticCharge_Subject, 
                            htmlMessage: htmlMessage
                            );

                        //Register Event
                        return await RegisterEvent(
                            stripeEvent, 
                            $"Subscription cancelation '{subscription.Id}' for customer '{subscription.CustomerId}' - Notification sent to: '{customer.Email}'"
                            );
                    }
                    else
                        throw new CustomException("Problems canceling subscription in local DB", StatusCode.InternalServerError);
                }
            }
            return true;
        }

        protected async Task<Gym> GetGymFromStripeSubscription(Subscription subscription)
        {
            if(!subscription.Metadata.TryGetValue("GymId",out string gymId))
                throw new CustomException("GymId not in subsctiption Metadata", StatusCode.NotFound);
            return await gymBL.GetByID(gymId,processInfo);
        }

        protected bool IsGymSubscribed(Gym gym, Subscription subscription)
        {
            if (gym == null)
                return false;
            var package = subscription.Plan.ProductId;
            return gym.AppPackages.Contains(package);
        }

    }
}
