﻿using System;
using System.Collections.Generic;
using System.Linq;
using IntegrationEngine.Core.Jobs;
using IntegrationEngine.Model;
using Quartz;
using IntegrationEngine.MessageQueue;

namespace IntegrationEngine.Scheduler
{
    public class EngineScheduler : IEngineScheduler
    {
        public IScheduler Scheduler { get; set; }
        public IList<Type> IntegrationJobTypes { get; set; }
        public IMessageQueueClient MessageQueueClient { get; set; }

        public EngineScheduler()
        {}

        public void Start()
        {
            Scheduler.Start();
        }

        public Type GetRegisteredJobTypeByName(string jobTypeName)
        {
            var jobTypes = IntegrationJobTypes.Where(x => x.FullName == jobTypeName);
            return jobTypes.Any() ? jobTypes.Single() : null;
        }

        public bool IsJobTypeRegistered(string jobTypeName)
        {
            return GetRegisteredJobTypeByName(jobTypeName) != null;
        }

        public IJobDetail JobDetailFactory(Type jobType)
        {
            var integrationJob = Activator.CreateInstance(jobType) as IIntegrationJob;
            var jobDetailsDataMap = new JobDataMap();
            jobDetailsDataMap.Put("MessageQueueClient", MessageQueueClient);
            jobDetailsDataMap.Put("IntegrationJob", integrationJob);
            return JobBuilder.Create<IntegrationJobDispatcherJob>()
                .SetJobData(jobDetailsDataMap)
                .WithIdentity(jobType.Name, jobType.Namespace)
                .Build();
        }

        public virtual void ScheduleJobWithCronTrigger(CronTrigger triggerDefinition)
        {
            var jobType = GetRegisteredJobTypeByName(triggerDefinition.JobType);
            var jobDetail = JobDetailFactory(jobType);
            var trigger = CronTriggerFactory(triggerDefinition, jobType, jobDetail);
            TryScheduleJobWithTrigger(trigger, jobType, jobDetail, triggerDefinition.StateId);
        }

        public void ScheduleJobWithSimpleTrigger(SimpleTrigger triggerDefinition)
        {
            var jobType = GetRegisteredJobTypeByName(triggerDefinition.JobType);
            var jobDetail = JobDetailFactory(jobType);
            var trigger = SimpleTriggerFactory(triggerDefinition, jobType, jobDetail);
            TryScheduleJobWithTrigger(trigger, jobType, jobDetail, triggerDefinition.StateId);
        }

        public void TryScheduleJobWithTrigger(ITrigger trigger, Type jobType, IJobDetail jobDetail, int stateId)
        {
            if (Scheduler.CheckExists(jobDetail.Key))
                Scheduler.RescheduleJob(trigger.Key, trigger);
            else
                Scheduler.ScheduleJob(jobDetail, trigger);
            SetTriggerState(trigger.Key, stateId);
        }

        public void SetTriggerState(TriggerKey triggerKey, int triggerState)
        {
            switch ((TriggerState)triggerState)
            {
                case TriggerState.Paused:
                    Scheduler.PauseTrigger(triggerKey);
                    break;
                case TriggerState.Normal:
                    Scheduler.ResumeTrigger(triggerKey);
                    break;
            }
        }

        public void ScheduleJobsWithTriggers(IEnumerable<IIntegrationJobTrigger> triggerDefs, Type jobType, IJobDetail jobDetail)
        {
            if (!triggerDefs.Any())
                return;
            var triggersForJobs = new Quartz.Collection.HashSet<ITrigger>();
            foreach (var triggerDef in triggerDefs)
            {
                if (triggerDef is CronTrigger)
                    triggersForJobs.Add(CronTriggerFactory(triggerDef as CronTrigger, jobType, jobDetail));
                else if (triggerDef is SimpleTrigger)
                    triggersForJobs.Add(SimpleTriggerFactory(triggerDef as SimpleTrigger, jobType, jobDetail));
            }
            Scheduler.ScheduleJob(jobDetail, triggersForJobs, true);
            foreach (var triggerDef in triggerDefs)
                SetTriggerState(TriggerKeyFactory(triggerDef, jobType), triggerDef.StateId);
        }

        TriggerKey TriggerKeyFactory(IIntegrationJobTrigger integrationJobTrigger, Type jobType)
        {
            return new TriggerKey(integrationJobTrigger.Id, jobType.FullName);
        }

        TriggerBuilder TriggerBuilderFactory(IIntegrationJobTrigger integrationJobTrigger, Type jobType)
        {
            return TriggerBuilder.Create().WithIdentity(TriggerKeyFactory(integrationJobTrigger, jobType));
        }

        public ITrigger SimpleTriggerFactory(SimpleTrigger triggerDefinition, Type jobType, IJobDetail jobDetail)
        {
            var triggerBuilder = TriggerBuilderFactory(triggerDefinition, jobType);
            Action<SimpleScheduleBuilder> simpleScheduleBuilderAction;
            if (triggerDefinition.RepeatCount > 0)
                simpleScheduleBuilderAction = x => x.WithInterval(triggerDefinition.RepeatInterval).WithRepeatCount(triggerDefinition.RepeatCount);
            else
                simpleScheduleBuilderAction = x => x.WithInterval(triggerDefinition.RepeatInterval);
            triggerBuilder.WithSimpleSchedule(simpleScheduleBuilderAction);
            if (!object.Equals(triggerDefinition.StartTimeUtc, default(DateTimeOffset)))
                triggerBuilder.StartAt(triggerDefinition.StartTimeUtc);
            else
                triggerBuilder.StartNow();
            return triggerBuilder.Build();
        }

        public ITrigger CronTriggerFactory(CronTrigger triggerDefinition, Type jobType, IJobDetail jobDetail)
        {
            var triggerBuilder = TriggerBuilderFactory(triggerDefinition, jobType);
            triggerBuilder.WithCronSchedule(triggerDefinition.CronExpressionString, x => x.InTimeZone(triggerDefinition.TimeZoneInfo));
            return triggerBuilder.Build();
        }
    }
}