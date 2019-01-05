/* MIT License
 *
 * Copyright (c) 2018 LogiQ System
 *
 * Permission is hereby granted, free of charge, to any person obtaining a copy
 * of this software and associated documentation files (the "Software"), to deal
 * in the Software without restriction, including without limitation the rights
 * to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
 * copies of the Software, and to permit persons to whom the Software is
 * furnished to do so, subject to the following conditions:
 *
 * The above copyright notice and this permission notice shall be included in all
 * copies or substantial portions of the Software.
 *
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
 * IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
 * FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
 * AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
 * LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
 * OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
 * SOFTWARE.
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;
using CronQuery.Cron;
using CronQuery.Mvc.Options;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CronQuery.Mvc.Jobs
{
    public sealed class JobRunner : IDisposable
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILoggerFactory _loggerFactory;
        private readonly ICollection<IDisposable> _timers;

        private JobRunnerOptions _options;

        public JobRunner(IOptionsMonitor<JobRunnerOptions> options, IServiceProvider serviceProvider, ILoggerFactory loggerFactory)
        {
            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            if (serviceProvider == null)
            {
                throw new ArgumentNullException(nameof(serviceProvider));
            }

            if (loggerFactory == null)
            {
                throw new ArgumentNullException(nameof(loggerFactory));
            }

            _options = options.CurrentValue;
            _serviceProvider = serviceProvider;
            _loggerFactory = loggerFactory;
            _timers = new List<IDisposable>();

            options.OnChange(Restart);
        }

        public ICollection<Type> Jobs { get; private set; } = new List<Type>();

        public void Dispose()
        {
            foreach (var timer in _timers)
            {
                timer.Dispose();
            }
        }

        public void Enqueue<TJob>() where TJob : IJob
        {
            Jobs.Add(typeof(TJob));
        }

        public void Start()
        {
            if (!_options.Running) return;

            var timezone = TimeZoneInfo.FindSystemTimeZoneById(_options.Timezone ?? "UTC");

            foreach (var job in Jobs)
            {
                var config = _options.Jobs.SingleOrDefault(entry => entry.Name == job.Name);

                if (config == null)
                {
                    var logger = _loggerFactory.CreateLogger(job.FullName);
                    logger.LogWarning($"No job configuration matches '{job.Name}'.");

                    continue;
                }

                if (!config.Running)
                {
                    continue;
                }

                var cron = new CronExpression(config.Cron);

                if (!cron.IsValid)
                {
                    var logger = _loggerFactory.CreateLogger(job.FullName);
                    logger.LogWarning($"Invalid cron expression for '{job.Name}'.");

                    continue;
                }

                var timer = new JobInterval(cron, timezone, async () => await Do(job));

                _timers.Add(timer);

                timer.Run();
            }
        }

        private async Task Do(Type job)
        {
            using (var scope = _serviceProvider.CreateScope())
            {
                var jobInstance = ((IJob)scope.ServiceProvider.GetRequiredService(job));

                try
                {
                    await jobInstance.RunAsync();
                }
                catch (Exception error)
                {
                    var logger = _loggerFactory.CreateLogger(job.FullName);
                    logger.LogError(error, $"Job '{job.Name}' failed during running.");
                }
                finally
                {
                    if (jobInstance is IDisposable disposable)
                    {
                        disposable.Dispose();
                    }
                }
            }
        }

        private void Restart(JobRunnerOptions newOptions)
        {
            _options = newOptions;

            foreach (var timer in _timers)
            {
                timer.Dispose();
            }

            _timers.Clear();

            Start();
        }
    }
}
