# CronQuery

Lightweight job runner for ASP.NET Core.

[![CircleCI](https://circleci.com/gh/logiqsystem/cronquery.svg?style=shield)](https://circleci.com/gh/logiqsystem/cronquery)
[![codecov](https://codecov.io/gh/logiqsystem/cronquery/branch/master/graph/badge.svg)](https://codecov.io/gh/logiqsystem/cronquery)
[![NuGet Version](https://img.shields.io/nuget/v/cronquery.svg)](https://img.shields.io/nuget/v/cronquery.svg)
[![NuGet Downloads](https://img.shields.io/nuget/dt/cronquery.svg)](https://www.nuget.org/packages/cronquery)

## Installation

Package Manager (Visual Studio):

```
Install-Package CronQuery
```

.NET CLI:

```
dotnet add package CronQuery
```

## Creating a job

Synchronous job:

```c#
public class MyJob : IJob
{
    public void Run()
    {
        // Do your magic
    }
}
```

Asynchronous job:

```c#
public class MyJobAsync : IJobAsync
{
    public async Task RunAsync()
    {
        // Do your magic
    }
}
```

## Registering a job

Jobs are registered in the app's `Startup` class:

```c#
public void ConfigureServices(IServiceCollection services)
{
    services.AddCronQuery();

    services.AddTransient<MyFirstJob>();
    services.AddTransient<MySecondJob>();
    services.AddTransient<MyThirdJob>();
}

 public void Configure(IApplicationBuilder app, IApplicationLifetime appLifetime)
 {
     app.UseCronQuery()
      .Enqueue<MyFirstJob>()  // Order matters
      .Enqueue<MySecondJob>()
      .Enqueue<MyThirdJob>()
      .StartWith(appLifetime);
}
```

> Jobs are registered using the ASP.NET Core dependency injection. This means that is possible to use dependency injection in your jobs.

## Setting up a job

Schedule your jobs using [cron expressions](CRON.md) of six fields to a specific timezone. Also, turn on or off CronQuery or a specific job by setting the `Running` property to `true` or `false` respectively.

Save the configuration in your `appsettings.json` like the example below:

- `MyFirstJob`: Runs every second on every day, except Sunday.

- `MySecondJob`: Runs every day at 2:00 A.M.

- `MyThirdJob`: Runs every second between 2:00 P.M. and 6:00 P.M. only on Saturday every 15 days.

```json
{
  "CronQuery": {
    "Running": true,
    "Timezone": "E. South America Standard Time",
    "Jobs": [
      {
        "Name": "MyFirstJob",
        "Running": true,
        "Cron": "* * * * * 2-6"
      },
      {
        "Name": "MySecondJob",
        "Running": true,
        "Cron": "0 0 2 * * *"
      },
      {
        "Name": "MyThirdJob",
        "Running": true,
        "Cron": "* * 14-18 * * 6/15"
      }
    ]
  }
}
```

> Whenever you save the `appsettings.json` CronQuery immediately assumes the new configuration.

Then apply the configuration in the app's `Startup.ConfigureServices` method:

```c#
public void ConfigureServices(IServiceCollection services)
{
    services.AddCronQuery(options => Configuration.GetSection("CronQuery").Bind(options));
}
```

## Contact us

If you have any questions, detect a bug or need a new feature, please, fell free to [open a new issue](https://github.com/logiqsystem/cronquery/issues) on GitHub.
