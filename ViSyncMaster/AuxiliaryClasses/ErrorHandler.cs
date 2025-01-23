using Avalonia.Controls;
using Avalonia.Threading;
using MsBox.Avalonia.Dto;
using MsBox.Avalonia.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ViSyncMaster.AuxiliaryClasses
{
    public class ErrorHandler
    {
        private static bool isErrorDisplayed = false;
        private static readonly object lockObject = new object();

        // Statyczna metoda do wyświetlania komunikatu o błędzie
        public static async Task ShowErrorMessage(string errorMessage, string? errorTitle = null)
        {
            lock (lockObject)
            {
                if (isErrorDisplayed)
                {
                    return;
                }
                isErrorDisplayed = true;
            }

            await Dispatcher.UIThread.InvokeAsync(async () =>
            {
                await MsBox.Avalonia.MessageBoxManager
                    .GetMessageBoxStandard(new MessageBoxStandardParams
                    {
                        ContentTitle = errorTitle,
                        ContentMessage = errorMessage,
                        ButtonDefinitions = ButtonEnum.Ok,
                        Icon = Icon.Error,
                        WindowStartupLocation = WindowStartupLocation.CenterOwner,
                        ShowInCenter = true,
                        Topmost = true
                    })
                    .ShowAsync();

                lock (lockObject)
                {
                    isErrorDisplayed = false;
                }
            });
        }

        // Statyczna metoda do wyświetlania komunikatu o braku pliku
        public static async Task ShowMissingFileError(string filePath)
        {
            string errorMessage = $"{filePath}";
            string errorTitle = "Error";
            await ShowErrorMessage(errorMessage, errorTitle);
        }

        public static async Task ShowErrorNetwork(string message)
        {
            string errorMessage = $"{message}";
            string errorTitle = "Error";
            await ShowErrorMessage(errorMessage, errorTitle);
        }
    }
}