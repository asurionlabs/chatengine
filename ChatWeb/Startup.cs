/*
AVA Chat Engine is a chat bot API.

Copyright (C) 2015-2019  Asurion, LLC

AVA Chat Engine is free software: you can redistribute it and/or modify
it under the terms of the GNU General Public License as published by
the Free Software Foundation, either version 3 of the License, or
(at your option) any later version.

AVA Chat Engine is distributed in the hope that it will be useful,
but WITHOUT ANY WARRANTY; without even the implied warranty of
MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
GNU General Public License for more details.

You should have received a copy of the GNU General Public License
along with AVA Chat Engine.  If not, see <https://www.gnu.org/licenses/>.
*/

using System;
using System.IO;
using System.Linq;
using Amazon.Util;
using Amazon.XRay.Recorder.Core;
#if XRAY2
using Amazon.XRay.Recorder.Core.Sampling.Local;
#else
using Amazon.XRay.Recorder.Core.Sampling;
#endif
using Amazon.XRay.Recorder.Core.Strategies;
using Amazon.XRay.Recorder.Handlers.AspNet;
using Amazon.XRay.Recorder.Handlers.AwsSdk;
using AWS.Logger.Log4net;
using ChatEngine.Hubs;
using ChatWeb;
using ChatWeb.Helpers;
using ChatWeb.Services;
using log4net;
using log4net.Repository.Hierarchy;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json.Serialization;

namespace ChatEngine
{
    public class Startup
    {
        public Startup(IConfiguration configuration, IHostingEnvironment env)
        {
            Configuration = configuration;
            HostingEnvironment = env;
        }

        public IConfiguration Configuration { get; }
        public IHostingEnvironment HostingEnvironment { get;  }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            // Set outgoing connection count limit (defaults to 2)
            System.Net.ServicePointManager.DefaultConnectionLimit = ChatConfiguration.OldWebClientConnectionLimit;

            services.AddMvc()
                .AddJsonOptions(options => options.SerializerSettings.ContractResolver = new DefaultContractResolver());

            services.AddSingleton<IConfiguration>(Configuration);

            services.AddDefaultAWSOptions(Configuration.GetAWSOptions());

            // TODO: Are these necessary.  AddDefaultAWSOptions doesn't seem to set them as defaults for Client classes
            Amazon.AWSConfigs.AWSRegion = Configuration.GetAWSOptions().Region.SystemName;
            Amazon.AWSConfigs.AWSProfileName = Configuration.GetAWSOptions().Profile;

            services.AddSignalR(hubOptions => { hubOptions.EnableDetailedErrors = true; })
                .AddJsonProtocol(options => options.PayloadSerializerSettings.ContractResolver = new DefaultContractResolver());

            var chatConfiguration = Configuration.Get<ChatConfiguration>();
            services.AddSingleton<ChatConfiguration>(chatConfiguration);
            services.AddSingleton<UserDataFilterService>();
            services.AddSingleton<FuzzyMatchService>();
            services.AddSingleton<AddressParseService>();
            services.AddSingleton<TextParserService>();
            services.AddSingleton<IExternalDataStorageService, AWSDynamoService>();

            // Flow Provider is scoped per request, so flow steps are only cached per request.
            // No chance for old flows to be loaded.
            services.AddSingleton<FlowStepProvider>();

            services.AddSingleton(new TextClassificationService(Path.Combine(HostingEnvironment.ContentRootPath, chatConfiguration.ClassifierConfigFile),
                new AWSDynamoService(chatConfiguration),
                chatConfiguration.ClassificationMemCacheConfigNode != null ? new MemCacheService(chatConfiguration.ClassificationMemCacheConfigNode) : null
            ));

            services.AddSingleton(new EmojiParser(HostingEnvironment.ContentRootPath));
            services.AddSingleton(new SpellcheckService(chatConfiguration.SpellCheckService));
            services.AddSingleton(new MemCacheService(chatConfiguration.MemCacheConfigNode));

            services.AddScoped<ChatWeb.Services.ChatEngine>();

            services.AddCors(o => o.AddPolicy("AllowAny", builder =>
            {
                builder.AllowAnyOrigin()
                       .AllowAnyMethod()
                       .AllowAnyHeader();
            }));

            // Enable Node Services
            services.AddNodeServices(options => {
#if DEBUG
                options.LaunchWithDebugging = true;
                options.DebuggingPort = 2626;
                //options.NodeInstanceOutputLogger = loggerFactory.CreateLogger("NodeJS");
#endif
            });

            services.AddSwaggerDocument(config =>
            {
                config.PostProcess = document =>
                {
                    document.Info.Version = VersionService.GetVersion();
                    document.Info.Title = "AVA Chat Engine";
                    document.Info.Description = "REST API for performing chats with AVA Bot.";
                    document.Info.TermsOfService = "";
                    /*document.Info.Contact = new NSwag.SwaggerContact
                    {
                        Name = "Name",
                        Email = string.Empty,
                        Url = "https://twitter.com/name"
                    };
                    document.Info.License = new NSwag.SwaggerLicense
                    {
                        Name = "Use under LICX",
                        Url = "https://example.com/license"
                    };*/
                };
            });


            ConfigureLogging(chatConfiguration);
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            if (AwsUtilityMethods.IsRunningOnAWS)
            {
                app.UseXRay(new DynamicSegmentNamingStrategy("pss-ava-chatengine")); // name of the app
                ConfigureXRaySampling();
                AWSSDKHandler.RegisterXRayForAllServices();
            }

            // Register the Swagger generator and the Swagger UI middlewares
            app.UseSwagger();
            app.UseSwaggerUi3();

            app.UseSignalR(routes =>
            {
                routes.MapHub<NodeJSHub>("/nodeHub");
            });
            app.UseMvc();

            //loggerFactory.AddLog4Net(); // Log Microsoft ILogger to Log4Net
        }

        private void ConfigureLogging(ChatConfiguration chatConfiguration)
        {
            var fileInfo = new System.IO.FileInfo(System.IO.Path.Combine(AppContext.BaseDirectory, chatConfiguration.Log4NetConfigFile));
            log4net.Config.XmlConfigurator.Configure(fileInfo);
            
            // Configure AWS Appender Log Group from Configuration
            Hierarchy hierarchy = (Hierarchy)LogManager.GetRepository();
            foreach (AWSAppender appender in hierarchy.GetAppenders().Where(a => a is AWSAppender))
            {
                appender.LogGroup = chatConfiguration.CloudWatchLogGroup;
                appender.ActivateOptions();
            }

            log4net.GlobalContext.Properties["environment"] = chatConfiguration.ChatEnvironmentName;
            log4net.GlobalContext.Properties["sessionId"] = "00000000000000000000000000000000";
            log4net.GlobalContext.Properties["applicationName"] = "chatengine";
            log4net.GlobalContext.Properties["indexType"] = "engine_log_events";
            log4net.GlobalContext.Properties["indexName"] = "log_events";
            log4net.GlobalContext.Properties["server"] = EC2InstanceMetadata.PrivateIpAddress ?? Environment.MachineName;

            log4net.ILog logger = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
            logger.Info("Startup: Chat Engine");
            logger.Info(chatConfiguration.GetConfigurationSettings());
        }

        private static void ConfigureXRaySampling()
        {
            var sampling = AWSXRayRecorder.Instance.SamplingStrategy as LocalizedSamplingStrategy;
            if (sampling == null)
            {
                log4net.ILog logger = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
                logger.WarnFormat("Startup: Unknown Sampling Strategy.  {0}", AWSXRayRecorder.Instance.SamplingStrategy);
                return;
            }

            // TODO:
            //if (bool.TryParse(ConfigurationManager.AppSettings["XRayALL"], out bool XRayAll) && XRayAll)
            //    sampling.DefaultRule.Rate = 1.0;

            sampling.Rules.Add(new SamplingRule
            {
                Description = "Disable all HTTP OPTIONS tracing",
                HttpMethod = "OPTIONS",
                ServiceName = "*",
                UrlPath = "*",
                FixedTarget = 0,
                Rate = 0.0
            }
            );
        }
    }
}
