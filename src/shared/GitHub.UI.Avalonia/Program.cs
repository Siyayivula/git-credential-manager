﻿using System;
using System.Diagnostics;
using System.Threading;
using Avalonia;
using GitHub.UI.Controls;
using GitHub.UI.Commands;
using GitCredentialManager;
using GitCredentialManager.UI;

namespace GitHub.UI
{
    public static class Program
    {
        public static void Main(string[] args)
        {
            // If we have no arguments then just start the app with the test window.
            if (args.Length == 0)
            {
                BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
                return;
            }

            // Create the dispatcher on the main thread. This is required
            // for some platform UI services such as macOS that mandates
            // all controls are created/accessed on the initial thread
            // created by the process (the process entry thread).
            Dispatcher.Initialize();

            // Run AppMain in a new thread and keep the main thread free
            // to process the dispatcher's job queue.
            var appMain = new Thread(AppMain) {Name = nameof(AppMain)};
            appMain.Start(args);

            // Process the dispatcher job queue (aka: message pump, run-loop, etc...)
            // We must ensure to run this on the same thread that it was created on
            // (the main thread) so we cannot use any async/await calls between
            // Dispatcher.Create and Run.
            Dispatcher.MainThread.Run();

            // Execution should never reach here as AppMain terminates the process on completion.
            throw new InvalidOperationException("Main dispatcher job queue shutdown unexpectedly");
        }

        private static void AppMain(object o)
        {
            string[] args = (string[]) o;

            using (var context = new CommandContext(args))
            using (var app = new HelperApplication(context))
            {
                app.RegisterCommand(new CredentialsCommandImpl(context));
                app.RegisterCommand(new TwoFactorCommandImpl(context));
                app.RegisterCommand(new DeviceCodeCommandImpl(context));

                int exitCode = app.RunAsync(args)
                    .ConfigureAwait(false)
                    .GetAwaiter()
                    .GetResult();

                context.Trace2.Stop(exitCode);
                Environment.Exit(exitCode);
            }
        }

        public static AppBuilder BuildAvaloniaApp()
            => AppBuilder.Configure(() => new AvaloniaApp(() => new TesterWindow()))
                .UsePlatformDetect()
                .LogToTrace();
    }
}
