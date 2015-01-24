﻿using BeekmanLabs.UnitTesting;
using IntegrationEngine.Api.Controllers;
using IntegrationEngine.Model;
using IntegrationEngine.Scheduler;
using Moq;
using NUnit.Framework;
using System;
using System.Collections;
using System.Collections.Generic;

namespace IntegrationEngine.Tests
{
    public class JobTypeControllerTest : TestBase<JobTypeController>
    {
        [Test]
        public void ShouldReturnListOfJobTypes()
        {
            var engineScheduler = new Mock<EngineScheduler>();
            var type = typeof(IntegrationJobFixture);
            var expected = new List<JobType>() {
                new JobType() {
                    FullName = type.FullName,
                    Name = type.Name,
                }
            };
            engineScheduler.SetupGet(x => x.IntegrationJobTypes).Returns(new List<Type>() { type });
            Subject.EngineScheduler = engineScheduler.Object;

            var result = Subject.GetJobTypes();

            Assert.That(result, Is.EqualTo(expected));
        }
    }
}

