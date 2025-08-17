using System;
using System.Globalization;
using System.Threading;
using System.Windows;
using System.Windows.Markup;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NLog.Extensions.Logging;
using MedicalLabAnalyzer.Services;
using MedicalLabAnalyzer.ViewModels;

namespace MedicalLabAnalyzer
{
    public partial class App : Application
    {
        private IHost? _host;
        
        protected override void OnStartup(StartupEventArgs e)
        {
            // Set Arabic culture
            Thread.CurrentThread.CurrentCulture = new CultureInfo("ar-SA");
            Thread.CurrentThread.CurrentUICulture = new CultureInfo("ar-SA");
            FrameworkElement.LanguageProperty.OverrideMetadata(
                typeof(FrameworkElement),
                new FrameworkPropertyMetadata(XmlLanguage.GetLanguage(CultureInfo.CurrentCulture.IetfLanguageTag)));

            // Setup dependency injection
            _host = CreateHostBuilder().Build();
            
            // Initialize database
            var dbService = _host.Services.GetRequiredService&lt;DatabaseService&gt;();
            dbService.InitializeDatabase();
            
            base.OnStartup(e);
        }

        private IHostBuilder CreateHostBuilder()
        {
            return Host.CreateDefaultBuilder()
                .ConfigureServices((context, services) =&gt;
                {
                    // Register Services
                    services.AddSingleton&lt;DatabaseService&gt;();
                    services.AddSingleton&lt;AuditService&gt;();
                    services.AddSingleton&lt;MediaService&gt;();
                    
                    // Register Analyzers
                    services.AddTransient&lt;CASAAnalyzer&gt;();
                    services.AddTransient&lt;CBCAnalyzer&gt;();
                    services.AddTransient&lt;UrineAnalyzer&gt;();
                    services.AddTransient&lt;StoolAnalyzer&gt;();
                    services.AddTransient&lt;GlucoseAnalyzer&gt;();
                    services.AddTransient&lt;LipidProfileAnalyzer&gt;();
                    services.AddTransient&lt;LiverFunctionAnalyzer&gt;();
                    services.AddTransient&lt;KidneyFunctionAnalyzer&gt;();
                    services.AddTransient&lt;CRPAnalyzer&gt;();
                    services.AddTransient&lt;ThyroidAnalyzer&gt;();
                    services.AddTransient&lt;ElectrolytesAnalyzer&gt;();
                    services.AddTransient&lt;CoagulationAnalyzer&gt;();
                    services.AddTransient&lt;VitaminAnalyzer&gt;();
                    services.AddTransient&lt;HormoneAnalyzer&gt;();
                    services.AddTransient&lt;MicrobiologyAnalyzer&gt;();
                    services.AddTransient&lt;PCRAnalyzer&gt;();
                    services.AddTransient&lt;SerologyAnalyzer&gt;();
                    
                    // Register ViewModels
                    services.AddTransient&lt;LoginViewModel&gt;();
                    services.AddTransient&lt;MainViewModel&gt;();
                    services.AddTransient&lt;DashboardViewModel&gt;();
                    services.AddTransient&lt;PatientViewModel&gt;();
                    services.AddTransient&lt;ExamViewModel&gt;();
                    
                    // Logging
                    services.AddLogging(builder =&gt;
                    {
                        builder.AddNLog();
                    });
                });
        }
        
        protected override void OnExit(ExitEventArgs e)
        {
            _host?.Dispose();
            base.OnExit(e);
        }
        
        public static T GetService&lt;T&gt;() where T : class
        {
            return ((App)Current)._host?.Services.GetRequiredService&lt;T&gt;() 
                ?? throw new InvalidOperationException("Service not available");
        }
    }
}