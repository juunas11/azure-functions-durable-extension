﻿// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Logging;
using Xunit.Abstractions;

namespace Microsoft.Azure.WebJobs.Extensions.DurableTask.Tests
{
    internal class TestLogger : ILogger
    {
        private readonly ITestOutputHelper testOutput;
        private readonly Func<string, LogLevel, bool> filter;
        private bool traceToTestOutput;

        public TestLogger(ITestOutputHelper testOutput, string category, Func<string, LogLevel, bool> filter = null)
        {
            this.testOutput = testOutput;
            this.Category = category;
            this.filter = filter;
            this.traceToTestOutput = TestHelpers.LoggerCategoriesForTestOutput.Contains(category);
        }

        public string Category { get; private set; }

        public IList<LogMessage> LogMessages { get; } = new List<LogMessage>();

        public IDisposable BeginScope<TState>(TState state)
        {
            return null;
        }

        public bool IsEnabled(LogLevel logLevel)
        {
            return this.filter?.Invoke(this.Category, logLevel) ?? true;
        }

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
        {
            if (!this.IsEnabled(logLevel))
            {
                return;
            }

            string formattedMessage = formatter(state, exception);
            this.LogMessages.Add(new LogMessage
            {
                Level = logLevel,
                EventId = eventId,
                State = state as IEnumerable<KeyValuePair<string, object>>,
                Exception = exception,
                FormattedMessage = formattedMessage,
                Category = this.Category,
            });

            // Only write traces specific to this extension
            if (this.traceToTestOutput)
            {
                try
                {
                    this.testOutput.WriteLine($"    {DateTime.Now:o}: {formattedMessage}");
                }
                catch (InvalidOperationException)
                {
                    // swallow this exception;
                    // it is thrown by XUnit if the back end traces events while there is no test running.
                }
            }
        }
    }
}