using System;
using System.Linq.Expressions;
using Hangfire;
using Hangfire.Server;
using Hangfire.SqlServer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;


namespace Demo.Hangfire
{    
    public static class BackgroundJobQueueNames
    {
        public const string BelowNormalPriorityQueue = "e-below_normal";
        public const string NormalPriorityQueue = "default";
        public const string Above_NormalPriorityQueue = "c_above_normal";
        public const string HighPriorityQueue = "b_high";
        public const string RealTimePriorityQueue = "a_realtime";
    }

    public class JobContext : IServerFilter
    {
        public void OnPerforming(PerformingContext context)
        {            
            Console.WriteLine($"Starting to perform job {context.BackgroundJob.Id}");
        }
        
        public void OnPerformed(PerformedContext context)
        {            
            Console.WriteLine($"Job {context.BackgroundJob.Id} has been performed");
        }
    }

    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {

            services.AddControllers();

            services.AddHangfire(configuration => configuration
                //.SetDataCompatibilityLevel(CompatibilityLevel.Version_170)
                .UseSimpleAssemblyNameTypeSerializer()                
                .UseRecommendedSerializerSettings()
                .UseSqlServerStorage(Configuration.GetConnectionString("HangfireConnection"), new SqlServerStorageOptions
                {
                    CommandBatchMaxTimeout = TimeSpan.FromMinutes(5),
                    SlidingInvisibilityTimeout = TimeSpan.FromMinutes(5),
                    QueuePollInterval = TimeSpan.Zero,
                    UseRecommendedIsolationLevel = true,
                    DisableGlobalLocks = true
                }));

            GlobalJobFilters.Filters.Add(new AutomaticRetryAttribute { Attempts = 3, DelaysInSeconds = new int[] { 300 } });

            //not use UseColouredConsoleLogProvider in production mode.
            //GlobalConfiguration.Configuration.UseColouredConsoleLogProvider();
            GlobalConfiguration.Configuration.UseFilter(new JobContext());

            services.AddHangfireServer(options =>
            {
                options.WorkerCount = 1;
                options.Queues = new[] { BackgroundJobQueueNames.Above_NormalPriorityQueue, BackgroundJobQueueNames.NormalPriorityQueue, BackgroundJobQueueNames.BelowNormalPriorityQueue };
            });

            services.AddHangfireServer(options =>
            {
                options.WorkerCount = 1;
                options.Queues = new[] { BackgroundJobQueueNames.RealTimePriorityQueue, BackgroundJobQueueNames.HighPriorityQueue};
            });
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IBackgroundJobClient backgroundJobs, IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }            
            
            app.UseHangfireDashboard();

            app.UseHttpsRedirection();
            app.UseRouting();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
                 endpoints.MapHangfireDashboard();
            });

/*
            FireAndForgetJobExample();
            DelayedJobsExample();
            RecurringJobsExample();
            ContinuationJobExample();
            JobFailExample();
*/

            PriorityJobsExample();
        }

#region SamplesTestesJobs
        /// <summary>
        /// Jobs fire-and-forget são executados imediatamente e depois "esquecidos".
        /// Você ainda poderá voltar a executá-los manualmente sempre que quiser
        /// graças ao Dashboard e ao histórico do Hangfire.
        /// </summary>
        public void FireAndForgetJobExample()
        {
            BackgroundJob.Enqueue(() => Console.WriteLine("Exemplo de job Fire-and-forget!"));
        }

        /// <summary>
        /// Jobs delayed são executados em uma data futura pré-definida.
        /// </summary>
        public void DelayedJobsExample()
        {
            BackgroundJob.Schedule(
                () => Console.WriteLine("Exemplo de job Delayed executado 2 minutos após o início da aplicação"),
                TimeSpan.FromMinutes(2));
        }

        /// <summary>
        /// Reurring jobs permitem que você agende a execução de jobs recorrentes utilizando uma notação Cron.
        /// </summary>
        public void RecurringJobsExample()
        {
            RecurringJob.AddOrUpdate(
                "Meu job recorrente",
                () => Console.WriteLine((new Random().Next(1, 200) % 2 == 0)
                    ? "Job recorrente gerou um número par"
                    : "Job recorrente gerou um número ímpar"),
                Cron.Minutely,
                TimeZoneInfo.Local);
        }

        /// <summary>
        /// Esta abordagem permite que você defina para a execução de um job iniciar
        /// apenas após a conclusão de um job pai.
        /// </summary>
        public void ContinuationJobExample()
        {
            var jobId = BackgroundJob.Enqueue(() => Console.WriteLine("Job fire-and-forget pai!"));
            BackgroundJob.ContinueJobWith(jobId, () => Console.WriteLine($"Job fire-and-forget filho! (Continuação do job {jobId})"));
        }

        /// <summary>
        /// Quando um job falha o Hangfire irá adiciona-la a fila para executar novamente.
        /// Por padrão ele irá tentar reexecutar 10 vezes, você pode alterar este comportamento
        /// adicionando a seguinte configuração ao método ConfigureServices:
        /// <code>GlobalJobFilters.Filters.Add(new AutomaticRetryAttribute { Attempts = 3, DelaysInSeconds = new int[] { 300 } });</code>
        /// </summary>
        public void JobFailExample()
        {
            BackgroundJob.Enqueue(() => FalharJob());
        }

        public void FalharJob()
        {
            throw new Exception("Deu ruim hein...");
        }
#endregion

#region Priority Test

        public void TraceMessage(string message, [System.Runtime.CompilerServices.CallerMemberName] string memberName = "")
        {
            System.Diagnostics.Trace.WriteLine(
                DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss") +
                " - " + memberName + " : " + message);            
        }

        public const int waitTime = 20000;
        [Queue(BackgroundJobQueueNames.BelowNormalPriorityQueue)]
        public void BelowNormaPriorityJob()
        {
            TraceMessage("Start");
            Thread.Sleep(waitTime);
            TraceMessage("End");
        }
         
        public void NormaPriorityJob()
        {
            TraceMessage("Start");
            Thread.Sleep(waitTime);
            TraceMessage("End");
        }

        [Queue(BackgroundJobQueueNames.Above_NormalPriorityQueue)]
        public void Above_NormalPriorityJob()
        {
            TraceMessage("Start");
            Thread.Sleep(waitTime);
            TraceMessage("End");
        }

        [Queue(BackgroundJobQueueNames.HighPriorityQueue)]
        public void HighPriorityJob(PerformContext context)
        {            
            TraceMessage($"Start #{context.BackgroundJob.Id}");
            Thread.Sleep(waitTime);
            TraceMessage($"End  #{context.BackgroundJob.Id}");
        }

        
        [Queue(BackgroundJobQueueNames.RealTimePriorityQueue)]
        public void RealTimePriorityJob(PerformContext context)
        {            
            TraceMessage($"Start #{context.BackgroundJob.Id}");
            Thread.Sleep(waitTime);
            TraceMessage($"End  #{context.BackgroundJob.Id}");
        }
        

        public void Enqueue(Expression<Action> methodCall)
        {            
            var call = (MethodCallExpression)methodCall.Body;            
            var jobId = BackgroundJob.Enqueue(methodCall);
            TraceMessage($"#{jobId} - {call.Method.Name}");
        }

        /// <summary>
        /// Jobs fire-and-forget são executados imediatamente e depois "esquecidos".
        /// Você ainda poderá voltar a executá-los manualmente sempre que quiser
        /// graças ao Dashboard e ao histórico do Hangfire.
        /// </summary>
        public void PriorityJobsExample()
        {
            Console.WriteLine("PriorityJobsExample!");
            Enqueue(() => NormaPriorityJob());
            Enqueue(() => BelowNormaPriorityJob());
            Enqueue(() => Above_NormalPriorityJob());
            Enqueue(() => Above_NormalPriorityJob());            
            Enqueue(() => Above_NormalPriorityJob());
            Enqueue(() => Above_NormalPriorityJob());

            Enqueue(() => HighPriorityJob(null));
            Enqueue(() => HighPriorityJob(null));
            Enqueue(() => RealTimePriorityJob(null));
            Enqueue(() => RealTimePriorityJob(null));                        
        }
#endregion

    }
}