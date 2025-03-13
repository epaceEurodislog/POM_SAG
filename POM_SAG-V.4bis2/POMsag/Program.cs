using POMsag.Models;
using POMsag.Services;
#pragma warning disable CS8618, CS8625, CS8600, CS8602, CS8603, CS8604, CS8601

namespace POMsag;

static class Program
{
    /// <summary>
    ///  The main entry point for the application.
    /// </summary>
    [STAThread]
    static void Main()
    {
        // To customize application configuration such as set high DPI settings or default font,
        // see https://aka.ms/applicationconfiguration.
        ApplicationConfiguration.Initialize();
        Application.Run(new Form1());
    }
}