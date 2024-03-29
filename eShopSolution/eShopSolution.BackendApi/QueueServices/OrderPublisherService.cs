﻿using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using QueueEngine;
using QueueEngine.Behaviors;
using QueueEngine.Interfaces;
using QueueEngine.Models.QueueData;
using QueueEngine.Models.QueueSetting;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace eShopSolution.BackendApi.QueueServices
{
    public class OrderPublisherService : IHostedService
    {
        private readonly ILogger<OrderPublisherService> _logger;
        private readonly IQueueService<OrderQueue> _queueService;
        private readonly List<IQueuePublisher<OrderQueue>> _publishers;
        private readonly IConfiguration _configuration;
        private readonly IWebHostEnvironment _environment;
        private const int MAX_CONSUME_LENGTH = 100;

        public OrderPublisherService(ILogger<OrderPublisherService> logger,
            IQueueService<OrderQueue> queueService,
            IConfiguration configuration, IWebHostEnvironment environment)
        {
            _logger = logger;
            _queueService = queueService;
            _configuration = configuration;
            _environment = environment;
            _publishers = CreatePublisher();
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            Task.Run(() =>
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    var inQueueItems = _queueService.ConsumeQueue(MAX_CONSUME_LENGTH);

                    if (inQueueItems.Any() && _publishers != null)
                    {
                        Parallel.ForEach(_publishers, async publisher => { await publisher.SendMessages(inQueueItems); });
                    }
                }
            }, cancellationToken);
          
            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        private List<IQueuePublisher<OrderQueue>> CreatePublisher()
        {
            try
            {
                List<IQueuePublisher<OrderQueue>> queuePublishers = new List<IQueuePublisher<OrderQueue>>();
                var providerSetting = _configuration["MessageQueueSetting:Provider"];
                var provider = Common.ParseEnum<QueueProvider>(providerSetting);
                switch (provider)
                {
                    case QueueProvider.GOOGLE:

                        var googleSetting = new GoogleQueueSetting();
                        var settingPath = _configuration.GetSection("MessageQueueSetting:GoogleQueueSetting");
                        settingPath.Bind(googleSetting);
                        googleSetting.CredentialFile = Path.Combine(_environment.ContentRootPath, googleSetting.CredentialFile);

                        //queuePublishers.Add(QueueEngineFactory.CreateGooglePublisher<OrderQueue>(provider, googleSetting, "SmSQueue"));
                        queuePublishers.Add(QueueEngineFactory.CreateGooglePublisher<OrderQueue>(provider, googleSetting, "EmailQueue"));
                        //queuePublishers.Add(QueueEngineFactory.CreateGooglePublisher<OrderQueue>(provider, googleSetting, "StockQueue"));
                        queuePublishers.Add(QueueEngineFactory.CreateGooglePublisher<OrderQueue>(provider, googleSetting, "ShippingQueue"));
                        break;
                }

                return queuePublishers;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
            }

            return default;
        }
    }
}
