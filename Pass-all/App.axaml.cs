using System;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Microsoft.EntityFrameworkCore;
using Passall.Modeles;
using Passall.Utils;
using SQLitePCL;

namespace Passall
{
    public partial class App : Application
    {
        public override void Initialize()
        {
            AvaloniaXamlLoader.Load(this);
        }

        public override void OnFrameworkInitializationCompleted()
        {
            Batteries.Init();
            
            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                desktop.MainWindow = new AuthWindow();
            }

            base.OnFrameworkInitializationCompleted();
            
            using (DataContext localDataContext = new DataContext())
            {
                try
                {
                    localDataContext.Database.Migrate();
                }
                catch (Exception e)
                {
                    Logger.Log(e);
                    Console.WriteLine(e);
                    throw;
                }
            }
        }
    }
}