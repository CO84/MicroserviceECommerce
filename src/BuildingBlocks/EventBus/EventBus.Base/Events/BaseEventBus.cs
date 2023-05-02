﻿using EventBus.Base.Abstraction;
using EventBus.Base.SubManagers;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EventBus.Base.Events
{
    public abstract class BaseEventBus : IEventBus
    {
        public readonly IServiceProvider ServiceProvider;
        public readonly IEventBusSubscriptionManager SubsManager;

        public EventBusConfig EventBusConfig { get; set; }

        protected BaseEventBus(EventBusConfig eventBusConfig, IServiceProvider serviceProvider)
        {
            EventBusConfig = eventBusConfig;
            ServiceProvider = serviceProvider;
            SubsManager = new InMemoryEventBusSubscriptionManager(ProcessEventName);
        }

        public virtual string ProcessEventName(string eventName)
        {
            if (EventBusConfig.DeleteEventPrefix)
                eventName = eventName.TrimStart(EventBusConfig.EventNamePrefix.ToArray());

            if(EventBusConfig.DeleteEventSuffix)
                eventName = eventName.TrimEnd(EventBusConfig.EventNameSuffix.ToArray());

            return eventName;
        }

        public virtual string GetSubName(string eventName)
        {
            return $"{EventBusConfig.SubscriberClientAppName}.{ProcessEventName(eventName)}";
        }

        public virtual void Dispose()
        {
            EventBusConfig = null;
            SubsManager.Clear();
        }

        public async Task<bool> ProcessEvent(string eventName, string message)
        {
            eventName = ProcessEventName(eventName);
            var processed = false;

            if(SubsManager.HasSubscriptionForEvent(eventName))
            {
                var subscriptions = SubsManager.GetHandlersForEvent(eventName);

                using (var scope = ServiceProvider.CreateScope())
                {
                    foreach (var subscription in subscriptions)
                    {
                        var handler = ServiceProvider.GetService(subscription.HandlerType);
                        if (handler is null) continue;

                        var eventType = SubsManager.GetEventTypeByName($"{EventBusConfig.EventNamePrefix}{eventName}{EventBusConfig.EventNameSuffix}");
                        var integrationEvent = JsonConvert.DeserializeObject(message, eventType);

                     
                            var concreteType = typeof(IIntegrationEventHandler<>).MakeGenericType(eventType);
                            await (Task)concreteType.GetMethod("Handle").Invoke(handler, new object[] { integrationEvent });
                        
                    }
                   
                }
                processed = true;
               
            }
            return processed;
        }

        public abstract void Publish(IntegrationEvent @event);
        //public void Publish(IntegrationEvent @event)
        //{
        //    throw new NotImplementedException();
        //}

        public abstract void Subscribe<T, TH>() where T : IntegrationEvent where TH : IIntegrationEventHandler<T>;
        //public void Subscribe<T, TH>() where T : IntegrationEvent  where TH : IIntegrationEventHandler<T>
        //{
        //    throw new NotImplementedException();
        //}

        public abstract void UnSubscribe<T, TH>() where T : IntegrationEvent where TH : IIntegrationEventHandler<T>;
        //public void UnSubscribe<T, TH>() where T : IntegrationEvent where TH : IIntegrationEventHandler<T>
        //{
        //    throw new NotImplementedException();
        //}
    }
}
