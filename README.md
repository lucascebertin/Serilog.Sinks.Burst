# Serilog.Sinks.Burst

This project helps on timed/batching log dispatch.
It's a netstandard2 library to provide a clean way to send Serilog events based on a batch limit and/or time interval.

[![Linux Build][travis-image]][travis-url]
[![Windows Build][appveyor-image]][appveyor-url]

## Gettins started

### Prerequisites
On Windows, powershell (most up to date, please...)
```
PS C:\Path\To\The\Project> ./build.ps1
```

On Linux (be free!)
```
$ ./build.sh
```

### Using it on your app like a real Sink
```csharp
//Don't forget to add the namespace, ok?
var template = "[{0}] : [{1}] : {2} - {3}";

Action<IEnumerable<LogEvent>> simpleConsoleOutput = lst =>
	lst.ToList().ForEach(x => Console.WriteLine(template, 
		x.Timestamp, x.Level, x.MessageTemplate, x.Exception
	));

var logger = new LoggerConfiguration()
	.MinimumLevel.Debug()
	.WriteTo.Burst(simpleConsoleOutput, LogEventLevel.Debug)
	.CreateLogger();

Enumerable.Range(0, 999)
	.ToList()
	.ForEach(x => logger.Debug($"{x}"));

Console.WriteLine("Almost done... now, wait for 5 seconds, the timer will be fired and messages will pop down here!");
Console.ReadLine();
```

### Using it on your app like a base for any other Sink
```csharp
public static class LoggerConfigurationYourSinkExtensions
{
	public static LoggerConfiguration YourSink(
		this LoggerSinkConfiguration loggerConfiguration, 
		Action<IEnumerable<LogEvent>> action, 
		LogEventLevel restrictedToMinimumLevel = LevelAlias.Minimum,
		bool enableTimer = true,
		double interval = 5000, bool enableBatchLimit = true, int batchLimit = 100
	) => 
		loggerConfiguration.Sink(
			new BurstSink(action, enableTimer, interval, enableBatchLimit, batchLimit), 
			restrictedToMinimumLevel
		);
}
```

## Travis useful notes
At the root you will find a Docker file named `Dockerfile.travis`.
To simulate, run this way:
```
docker build -t travis-ci-burst -f Dockerfile.travis .
docker run -it travis-ci-burst
```

## IMPORTANT NOTES!
This repository and package are in early stages, so, use it on your own and risk but feel free to contribute opening issues or sending pull-requests!

[travis-image]: https://img.shields.io/travis/lucascebertin/Serilog.Sinks.Burst/master.svg?label=linux
[travis-url]: https://travis-ci.org/lucascebertin/Serilog.Sinks.Burst

[appveyor-image]: https://ci.appveyor.com/api/projects/status/0imo52uvl8c6eval?svg=true
[appveyor-url]: https://ci.appveyor.com/project/lcssk8board/serilog-sinks-burst
